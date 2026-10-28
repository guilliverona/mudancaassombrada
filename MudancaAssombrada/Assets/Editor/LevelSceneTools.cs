using System.Linq;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace MudancaAssombrada.Editor
{
    // O KIT DE AUTORIA das fases. Uma fase é feita de duas partes:
    //
    //   1. "Sistemas da Fase" (UM prefab, igual em toda fase): partida, rede dos sons e avisos,
    //      eco, noite, nascimento dos jogadores, HUD e pausa. Ninguém monta isso à mão.
    //   2. A CASA (de quem desenha a fase): paredes, móveis, fantasma e os MARCADORES:
    //        - SpawnPoint   onde cada jogador nasce
    //        - GhostHideout esconderijos do fantasma (o Mirror é um tipo)
    //        - Room         cômodos com nome ("Tiago caiu no Camarim!")
    //        - Objective    o que a equipe precisa fazer
    //
    // Menus:
    //   Nova Fase (cena vazia)                 cria uma cena só com os sistemas, chão e marcadores
    //   Usar Sistemas da Fase na cena aberta   troca as peças soltas de uma cena antiga pelo prefab
    public static class LevelSceneTools
    {
        public const string SystemsPath = "Assets/Prefabs/Sistemas/Sistemas da Fase.prefab";

        // ---------- O prefab dos sistemas ----------

        // Raiz = o objeto de rede (GameManager + SoundRelay). Os filhos não são de rede e cada um
        // é uma cópia LIGADA ao seu prefab: mudar o prefab da HUD muda a HUD de todas as fases.
        // (O Netcode não aceita dois objetos de rede soltos dentro de um objeto comum; por isso os
        // dois scripts de rede ficam juntos na raiz.)
        public static void EnsureSystemsPrefab() =>
            LevelPrefabsBuilder.Build(SystemsPath, false, BuildSystems);

        static GameObject BuildSystems()
        {
            var root = new GameObject("Sistemas da Fase");
            root.AddComponent<NetworkObject>();
            root.AddComponent<GameManager>();
            root.AddComponent<SoundRelay>();

            Child(root, LevelPrefabsBuilder.EchoPath);
            Child(root, LevelPrefabsBuilder.NightPath);
            Child(root, LevelPrefabsBuilder.SpawnerPath);
            Child(root, LevelPrefabsBuilder.HudPath);
            Child(root, LevelPrefabsBuilder.PausePath);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            eventSystem.transform.SetParent(root.transform, false);
            return root;
        }

        static void Child(GameObject root, string prefabPath)
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(LevelPrefabsBuilder.Load(prefabPath), root.transform);
            child.transform.localPosition = Vector3.zero;
        }

        // ---------- Nova fase ----------

        [MenuItem("Mudança Assombrada/Nova Fase (cena vazia)", priority = 4)]
        static void NewLevelMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = EditorUtility.SaveFilePanelInProject("Nova fase", "NovaFase", "unity",
                "Nome da cena da fase (sem espaços nem acentos, ex.: Conservatorio).", EditorTools.ScenesFolder);
            if (string.IsNullOrEmpty(path)) return;

            LevelPrefabsBuilder.EnsurePrefabs(recreateAll: false);
            NewLevelScene(path, null);
            Debug.Log($"Fase criada em {path}. Monte a casa, refaça o NavMesh (objeto NavMesh > Bake) e " +
                      $"preencha Scene Name na ficha do capítulo com \"{System.IO.Path.GetFileNameWithoutExtension(path)}\".");
        }

        // Cria e salva uma cena de fase: sistemas, luz, câmera de espera, chão, 3 marcadores de nascer,
        // a pasta de objetivos e o NavMesh. "buildHouse" (opcional) monta o resto antes do bake.
        // "configureNavMesh" (opcional) ajusta o NavMesh antes do bake (ex.: resolução mais fina).
        public static UnityEngine.SceneManagement.Scene NewLevelScene(string scenePath, System.Action buildHouse,
                                                                      System.Action<NavMeshSurface> configureNavMesh = null)
        {
            string before = StableAssets.Snapshot(scenePath); // para não "sujar" o Git se sair igual
            EnsureSystemsPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // A "Directional Light" que vem com toda cena nova vira a lua (e depois o sol nascendo).
            var light = Object.FindFirstObjectByType<Light>();
            light.name = "Lua / Sol";
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;

            // A câmera padrão vira a "de espera" (antes de o seu boneco nascer). Não é a MainCamera.
            var lobbyCam = Camera.main;
            lobbyCam.name = "Câmera de Espera";
            lobbyCam.tag = "Untagged";
            lobbyCam.transform.position = new Vector3(0, 20f, -10f);
            lobbyCam.transform.rotation = Quaternion.Euler(60f, 0, 0);
            lobbyCam.gameObject.AddComponent<LobbyCamera>();

            EditorTools.Place(LevelPrefabsBuilder.Load(SystemsPath), Vector3.zero);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Chão";
            ground.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30 m
            ground.transform.position = new Vector3(0, -0.01f, -4f); // um fio abaixo do piso das casas

            var markers = new GameObject("Marcadores").transform;
            for (int i = 0; i < 3; i++) SpawnMarker(markers, new Vector3(-1.5f + i * 1.5f, 1f, -4f));

            var objectives = new GameObject("Objetivos").transform;
            var deliver = new GameObject("Entregar todos os móveis");
            deliver.transform.SetParent(objectives);
            deliver.AddComponent<DeliverFurnitureObjective>();

            buildHouse?.Invoke();

            // NavMesh: o mapa de onde dá para andar (refaça com Bake depois de mexer na casa).
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            configureNavMesh?.Invoke(surface);
            // Os NavMeshObstacle (móveis, o relógio, o espelho-porta) são do jogo rodando: abrem o
            // buraco onde a coisa ESTÁ. Ligados no bake, o buraco fica gravado no mapa para sempre,
            // mesmo depois que o móvel sai dali (e o espelho-porta aberto continuaria fechando o vão).
            var obstacles = Object.FindObjectsByType<UnityEngine.AI.NavMeshObstacle>().Where(o => o.enabled).ToArray();
            foreach (var o in obstacles) o.enabled = false;
            Debug.Log($"NavMesh: bake com {obstacles.Length} obstáculo(s) desligado(s): {string.Join(", ", obstacles.Select(o => o.name))}");
            surface.BuildNavMesh();
            foreach (var o in obstacles) o.enabled = true;
            SaveNavMesh(surface, scenePath.Replace(".unity", "_NavMesh.asset"));

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorTools.FixNetworkObjectIds(scene); // gera os "RGs" dos objetos de rede e salva de novo

            // Recriou uma cena que já existia e saiu igual (só números internos)? Fica a antiga.
            if (StableAssets.RestoreIfSame(scenePath, before)) scene = EditorSceneManager.OpenScene(scenePath);
            return scene;
        }

        // Grava o NavMesh no MESMO arquivo de antes, se já existir. Apagar e criar de novo daria a
        // ele outra identidade (GUID), e a cena, que aponta para ele, pareceria mudada no Git.
        static void SaveNavMesh(NavMeshSurface surface, string navPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(surface.navMeshData, navPath);
                return;
            }
            EditorUtility.CopySerialized(surface.navMeshData, existing);
            surface.navMeshData = existing;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
        }

        public static void SpawnMarker(Transform parent, Vector3 position)
        {
            var go = new GameObject($"Nascer {parent.childCount + 1}", typeof(SpawnPoint));
            go.transform.SetParent(parent);
            go.transform.position = position;
        }

        public static Room RoomMarker(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var room = new GameObject(name).AddComponent<Room>();
            room.transform.SetParent(parent);
            room.transform.position = center;
            var so = new SerializedObject(room);
            so.FindProperty("size").vector3Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();
            return room;
        }

        // ---------- Migrar uma cena antiga ----------

        // Cenas feitas antes do kit têm EchoSystem, SoundRelay, GameManager, PlayerSpawner, noite,
        // HUD, pausa e EventSystem soltos. Este menu apaga essas peças, põe o prefab no lugar e
        // transforma a posição do antigo PlayerSpawner em marcadores de nascer. A casa não é tocada.
        [MenuItem("Mudança Assombrada/Usar Sistemas da Fase na cena aberta", priority = 5)]
        static void MigrateOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!EditorUtility.DisplayDialog("Usar Sistemas da Fase?",
                    $"Na cena \"{scene.name}\": as peças de sistema soltas (eco, partida, rede, noite, HUD, pausa, " +
                    "nascimento) serão trocadas pelo prefab \"Sistemas da Fase\". Paredes, móveis e fantasmas ficam.",
                    "Trocar", "Cancelar")) return;

            LevelPrefabsBuilder.EnsurePrefabs(recreateAll: false);
            EnsureSystemsPrefab();

            Vector3? oldSpawn = null;
            bool hasSystems = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (IsSystemsInstance(root)) { hasSystems = true; continue; }
                if (root.TryGetComponent(out PlayerSpawner spawner)) oldSpawn = spawner.transform.position;
                if (IsLooseSystem(root)) Object.DestroyImmediate(root);
            }

            if (!hasSystems) EditorTools.Place(LevelPrefabsBuilder.Load(SystemsPath), Vector3.zero);

            if (Object.FindObjectsByType<SpawnPoint>().Length == 0)
            {
                var markers = new GameObject("Marcadores").transform;
                Vector3 at = oldSpawn ?? new Vector3(0, 1f, 0);
                for (int i = 0; i < 3; i++) SpawnMarker(markers, at + Vector3.right * (i * 1.5f));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorTools.FixNetworkObjectIds(scene);
            Debug.Log($"Cena {scene.name} agora usa o prefab Sistemas da Fase. Confira os marcadores de nascer.");
        }

        static bool IsSystemsInstance(GameObject go) =>
            PrefabUtility.GetCorrespondingObjectFromSource(go) == LevelPrefabsBuilder.Load(SystemsPath);

        static bool IsLooseSystem(GameObject go) =>
            go.GetComponent<EchoSystem>() || go.GetComponent<SoundRelay>() || go.GetComponent<GameManager>() ||
            go.GetComponent<PlayerSpawner>() || go.GetComponent<NightLighting>() || go.GetComponent<Hud>() ||
            go.GetComponent<PauseMenu>() || go.GetComponent<EventSystem>();
    }
}
