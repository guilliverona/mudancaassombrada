using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Events; // UnityEventTools: liga um "On Used" pelo código, como arrastar no Inspector
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace MudancaAssombrada.Editor
{
    // Capítulo 1: A CASA DO ESPELHO (o Imitador). Cria os prefabs que a casa usa e um RASCUNHO
    // da cena (é a fase que o jogo abre primeiro). Depois de criada, a cena é sua: mova paredes,
    // espelhos e móveis à vontade, e refaça o NavMesh (objeto "NavMesh" > Bake).
    //
    //   Assets/Prefabs/Casa       Parede Lisa, Parede com Porta, Porta, Piso Xadrez (kit do Blender)
    //   Assets/Prefabs/Espelhos   Espelho P, M e G (molduras + componente Mirror)
    //   Assets/Prefabs/Móveis     Penteadeira
    //   Assets/Prefabs/Relíquias  Cartaz do Teatro (pista do Seu Valdemar)
    //   Assets/Prefabs/Fantasmas  Fantasma Imitador
    //
    // Como nos outros prefabs do mundo: só são criados se ainda NÃO existem.
    //
    // A planta (12 x 12 m, peças de 2 m):
    //        z=+6  +-------------------+-------------------+
    //              |  Camarim          |  Corredor dos     |
    //              |  (penteadeira,    |  Espelhos         |
    //              |   cartaz)          (passagem)         |
    //        z= 0  +---- passagem -----+---- passagem -----+
    //              |  Sala (caixas, cadeira, armário)      |
    //        z=-6  +--------- saída (4 m) ---------+-------+   caminhão lá fora (z=-11)
    public static class MirrorHouseBuilder
    {
        public const string SceneName = "CasaDoEspelho";
        public const string ScenePath = EditorTools.ScenesFolder + "/" + SceneName + ".unity";

        const string WallPlainPath = "Assets/Prefabs/Casa/Parede Lisa.prefab";
        const string WallDoorPath = "Assets/Prefabs/Casa/Parede com Porta.prefab";
        const string DoorPath = "Assets/Prefabs/Casa/Porta.prefab";
        const string FloorTilePath = "Assets/Prefabs/Casa/Piso Xadrez.prefab";
        const string MirrorSPath = "Assets/Prefabs/Espelhos/Espelho P.prefab";
        const string MirrorMPath = "Assets/Prefabs/Espelhos/Espelho M.prefab";
        const string MirrorLPath = "Assets/Prefabs/Espelhos/Espelho G.prefab";
        const string VanityPath = "Assets/Prefabs/Móveis/Penteadeira.prefab";
        const string PosterPath = "Assets/Prefabs/Relíquias/Cartaz do Teatro.prefab";
        const string ImitatorPath = "Assets/Prefabs/Fantasmas/Fantasma Imitador.prefab";

        const float WallWidth = 2f, WallHeight = 2.8f, WallThickness = 0.15f; // o kit (tools/blender)
        const float DoorWidth = 0.9f, DoorHeight = 2.1f;
        const float ImitatorHeight = 2.3f; // a ficha do Imitador (ART_BIBLE.md)
        const string ChapterPath = "Assets/Data/Capítulos/1 - A Casa do Espelho.asset";

        [MenuItem("Mudança Assombrada/Criar Cena Casa do Espelho", priority = 3)]
        static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!ConfirmOverwrite()) return;
            LevelPrefabsBuilder.EnsurePrefabs(recreateAll: false);
            BuildScene();
        }

        // Se a cena já existe, pergunta antes de apagar o trabalho feito à mão nela.
        public static bool ConfirmOverwrite()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return true;
            return EditorUtility.DisplayDialog("Recriar a cena Casa do Espelho?",
                "A cena já existe. Recriar apaga tudo que foi feito à mão nela (posições, objetos novos...).",
                "Recriar", "Manter a minha");
        }

        // ---------- Prefabs ----------

        public static void EnsurePrefabs()
        {
            LevelPrefabsBuilder.Build(WallPlainPath, false, () => KitPiece("Parede Lisa", "Environment/ParedeLisa/SM_ENV_ParedeLisa", WallPlainColliders));
            LevelPrefabsBuilder.Build(WallDoorPath, false, () => KitPiece("Parede com Porta", "Environment/ParedePorta/SM_ENV_ParedePorta", WallDoorColliders));
            LevelPrefabsBuilder.Build(DoorPath, false, () => KitPiece("Porta", "Environment/Porta/SM_ENV_Porta", DoorColliders));
            LevelPrefabsBuilder.Build(FloorTilePath, false, () => KitPiece("Piso Xadrez", "Environment/PisoXadrez/SM_ENV_PisoXadrez", null));
            LevelPrefabsBuilder.Build(MirrorSPath, false, () => BuildMirror("Espelho P", "MolduraS", 0.6f));
            LevelPrefabsBuilder.Build(MirrorMPath, false, () => BuildMirror("Espelho M", "MolduraM", 1.2f));
            LevelPrefabsBuilder.Build(MirrorLPath, false, () => BuildMirror("Espelho G", "MolduraL", 2.0f));
            LevelPrefabsBuilder.Build(VanityPath, false, BuildVanity);
            LevelPrefabsBuilder.Build(PosterPath, false, BuildPoster);
            LevelPrefabsBuilder.Build(ImitatorPath, false, BuildImitator);
            AssetDatabase.SaveAssets();
        }

        // Uma peça do kit: raiz vazia com a colisão, e o modelo do Blender como filho "Modelo".
        // As peças têm a origem no meio da BASE e o lado de dentro (papel de parede) virado para +Z.
        static GameObject KitPiece(string name, string art, System.Action<GameObject> colliders)
        {
            var root = new GameObject(name);
            colliders?.Invoke(root);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/{art}.fbx");
            if (model != null) LevelPrefabsBuilder.AddModel(root, model, Vector3.zero);
            else Debug.LogWarning($"{name}: modelo Assets/Art/{art}.fbx não encontrado (rode tools/blender).");
            return root;
        }

        static void Box(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        static void WallPlainColliders(GameObject root) =>
            Box(root, new Vector3(0, WallHeight / 2, 0), new Vector3(WallWidth, WallHeight, WallThickness));

        // Parede com o vão da porta: três caixas (esquerda, direita e em cima do vão).
        static void WallDoorColliders(GameObject root)
        {
            float side = (WallWidth - DoorWidth) / 2;
            Box(root, new Vector3(-(DoorWidth + side) / 2, WallHeight / 2, 0), new Vector3(side, WallHeight, WallThickness));
            Box(root, new Vector3((DoorWidth + side) / 2, WallHeight / 2, 0), new Vector3(side, WallHeight, WallThickness));
            Box(root, new Vector3(0, (WallHeight + DoorHeight) / 2, 0), new Vector3(DoorWidth, WallHeight - DoorHeight, WallThickness));
        }

        // A folha da porta tem a origem na dobradiça e corre para +X.
        static void DoorColliders(GameObject root) =>
            Box(root, new Vector3(DoorWidth / 2, DoorHeight / 2, 0), new Vector3(DoorWidth, DoorHeight, 0.05f));

        // Espelho: a moldura do kit + o componente Mirror (a "porta" do Imitador).
        // O vidro olha para +Z (a seta azul): encoste as costas na parede.
        static GameObject BuildMirror(string name, string art, float height)
        {
            var root = KitPiece(name, $"Environment/{art}/SM_ENV_{art}", r =>
                Box(r, new Vector3(0, height / 2, 0), new Vector3(height * 0.62f, height, 0.06f)));
            root.AddComponent<Mirror>();
            return root;
        }

        // Penteadeira de camarim (1,2 x 1,6 x 0,5 m): um móvel carregável, como o armário.
        static GameObject BuildVanity()
        {
            var size = new Vector3(1.2f, 1.6f, 0.5f);
            var obj = new GameObject("Penteadeira");
            Box(obj, Vector3.zero, size);
            obj.AddComponent<NetworkObject>();
            var furniture = obj.AddComponent<Furniture>(); // adiciona o Rigidbody junto
            obj.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            obj.AddComponent<NetworkRigidbody>();
            obj.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            obj.AddComponent<NavMeshObstacle>().carving = true;
            EditorTools.SetFloat(furniture, "weight", 2.5f);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/Penteadeira/SM_PRP_Penteadeira.fbx");
            if (model != null) LevelPrefabsBuilder.AddModel(obj, model, new Vector3(0, -size.y / 2, 0));
            return obj;
        }

        // Cartaz do teatro com "Valdemar" no elenco: a pista do capítulo (relíquia 1).
        // Interagir (E) = "Pegar Cartaz do Teatro", e o On Used grava o marcador no save.
        static GameObject BuildPoster()
        {
            var obj = new GameObject("Cartaz do Teatro");
            Box(obj, new Vector3(0, 0.5f, 0), new Vector3(0.7f, 1f, 0.08f));
            obj.AddComponent<NetworkObject>();
            var interactable = obj.AddComponent<Interactable>();
            var flag = obj.AddComponent<StoryFlag>();

            var so = new SerializedObject(interactable);
            so.FindProperty("actionName").stringValue = "Pegar";
            so.ApplyModifiedPropertiesWithoutUndo();
            // O UnityEvent é privado: pegamos pela "reflexão" para ligar o StoryFlag.Set nele.
            var onUsed = (UnityEvent)typeof(Interactable)
                .GetField("onUsed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(interactable);
            UnityEventTools.AddPersistentListener(onUsed, flag.Set);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/CartazTeatro/SM_PRP_CartazTeatro.fbx");
            if (model != null) LevelPrefabsBuilder.AddModel(obj, model, Vector3.zero);
            return obj;
        }

        // O Imitador: a mesma base de todo fantasma, SEM respiração, com o comportamento "imitar".
        static GameObject BuildImitator()
        {
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = "Fantasma Imitador";
            ghost.transform.localScale = new Vector3(0.6f, ImitatorHeight / 2f, 0.6f);
            ghost.GetComponent<Collider>().isTrigger = true; // atravessável
            ghost.GetComponent<Renderer>().sharedMaterial = EditorTools.ColorMaterial("Fantasma Imitador", new Color(0.85f, 0.85f, 0.9f));

            var agent = ghost.AddComponent<NavMeshAgent>();
            agent.baseOffset = ImitatorHeight / 2f;
            agent.radius = 0.3f;
            ghost.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            ghost.AddComponent<NetworkObject>();
            ghost.AddComponent<NetworkTransform>(); // autoridade do servidor
            LevelPrefabsBuilder.AddGhostParts(ghost, breathes: false); // NÃO respira: a pista da Cida
            ghost.AddComponent<GhostImitator>();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Ghosts/Imitador/SK_GHO_Imitador.fbx");
            if (model != null) LevelPrefabsBuilder.GhostArt(ghost, model, ImitatorHeight);
            return ghost;
        }

        static GameObject Load(string path) => LevelPrefabsBuilder.Load(path);

        // ---------- Cena ----------

        public static void BuildScene()
        {
            EnsurePrefabs();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // --- Chão: a rua (colisão) e o piso xadrez da casa (só desenho, por cima) ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Chão";
            ground.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30 m
            ground.transform.position = new Vector3(0, -0.01f, -4f); // um fio abaixo do piso: sem "piscar"

            var house = new GameObject("Casa").transform;
            var floor = new GameObject("Piso").transform;
            floor.SetParent(house);
            for (int x = -5; x <= 5; x += 2)
                for (int z = -5; z <= 5; z += 2)
                    Piece(FloorTilePath, floor, new Vector3(x, 0, z), 0);

            // --- Paredes externas (o lado de dentro de cada peça olha para +Z: giramos para dentro) ---
            var walls = new GameObject("Paredes").transform;
            walls.SetParent(house);
            for (int i = -5; i <= 5; i += 2)
            {
                if (i != -1 && i != 1) Piece(WallPlainPath, walls, new Vector3(i, 0, -6), 0); // sul: saída de 4 m no meio
                Piece(i == -3 ? WallDoorPath : WallPlainPath, walls, new Vector3(i, 0, 6), 180); // norte
                Piece(WallPlainPath, walls, new Vector3(-6, 0, i), 90);  // oeste
                Piece(WallPlainPath, walls, new Vector3(6, 0, i), -90);  // leste
            }
            // Porta fechada no fundo do camarim (só cenário: a saída é a da frente).
            // A folha corre para +X da dobradiça; girada 180°, corre para -X: a dobradiça fica do lado +X do vão.
            Piece(DoorPath, walls, new Vector3(-3 + DoorWidth / 2, 0, 6 - 0.05f), 180);

            // --- Paredes internas, com passagens de 2 m (o armário e a penteadeira precisam passar,
            // e o NavMesh encolhe perto das paredes: vão estreito o fantasma não atravessa) ---
            foreach (int x in new[] { -5, -1, 1, 5 }) Piece(WallPlainPath, walls, new Vector3(x, 0, 0), 0); // passagens em x = -3 e 3
            foreach (int z in new[] { 1, 3 }) Piece(WallPlainPath, walls, new Vector3(0, 0, z), 90);        // passagem em z = 5

            // --- Espelhos (as portas do Imitador). Encostados nas paredes, olhando para dentro ---
            float inside = 6 - WallThickness / 2 - 0.04f;
            Place(MirrorLPath, new Vector3(-inside, 0, 3), 90, "Espelho do Camarim");
            Place(MirrorLPath, new Vector3(3, 0, inside), 180, "Espelho do Corredor (fundo)");
            Place(MirrorMPath, new Vector3(inside, 0.5f, 2), -90, "Espelho do Corredor (lado)");
            Place(MirrorMPath, new Vector3(inside, 0.5f, -3), -90, "Espelho da Sala");
            Place(MirrorSPath, new Vector3(-4, 1.2f, -inside), 0, "Espelhinho da Entrada");

            // --- Móveis (objetos de rede ficam na RAIZ da cena) ---
            Place(VanityPath, new Vector3(-4.5f, 0.8f, 5.3f), 180, "Penteadeira");
            Place(LevelPrefabsBuilder.WardrobePath, new Vector3(-1f, 0.75f, 4.5f), 0, "Armário de Figurino");
            Place(LevelPrefabsBuilder.ChairPath, new Vector3(-3f, 0.3f, -3f), 0, null);
            Place(LevelPrefabsBuilder.BoxPath, new Vector3(3f, 0.25f, -2f), 0, null);
            Place(LevelPrefabsBuilder.BoxPath, new Vector3(4f, 0.25f, 4f), 0, "Caixa 2");

            // --- A pista: o cartaz do teatro, encostado na parede do camarim ---
            Place(PosterPath, new Vector3(-inside + 0.05f, 0.9f, 1.2f), 90, "Cartaz do Teatro");

            // --- O Imitador começa no espelho do camarim ---
            Place(ImitatorPath, new Vector3(-4.8f, ImitatorHeight / 2f, 3f), 90, null);

            // --- Caminhão na rua, com a cabine para longe da casa ---
            Place(LevelPrefabsBuilder.TruckPath, new Vector3(0, 0, -11f), 180, null);

            // --- Objetivo: levar todos os móveis ao caminhão ---
            var objectives = new GameObject("Objetivos").transform;
            var deliver = new GameObject("Entregar todos os móveis");
            deliver.transform.SetParent(objectives);
            deliver.AddComponent<DeliverFurnitureObjective>();

            // --- Câmera de espera (vista de cima) ---
            var lobbyCam = Camera.main;
            lobbyCam.name = "Câmera de Espera";
            lobbyCam.tag = "Untagged";
            lobbyCam.transform.position = new Vector3(0, 20f, -10f);
            lobbyCam.transform.rotation = Quaternion.Euler(60f, 0, 0);
            lobbyCam.gameObject.AddComponent<LobbyCamera>();

            // --- Sistemas, noite e interface (iguais em toda fase) ---
            Place(LevelPrefabsBuilder.EchoPath, Vector3.zero, 0, null);
            Place(LevelPrefabsBuilder.SoundRelayPath, Vector3.zero, 0, null);
            Place(LevelPrefabsBuilder.GameManagerPath, Vector3.zero, 0, null);
            Place(LevelPrefabsBuilder.SpawnerPath, new Vector3(0, 1f, -4f), 0, null); // na sala, perto da saída

            var light = Object.FindFirstObjectByType<Light>();
            light.name = "Lua / Sol";
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            Place(LevelPrefabsBuilder.NightPath, Vector3.zero, 0, null);

            UiBuilder.CreateEventSystem();
            Place(LevelPrefabsBuilder.HudPath, Vector3.zero, 0, null);
            Place(LevelPrefabsBuilder.PausePath, Vector3.zero, 0, null);

            // --- NavMesh ---
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string navPath = $"{EditorTools.ScenesFolder}/{SceneName}_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorTools.FixNetworkObjectIds(scene);
            LinkChapter();
            EditorTools.UpdateBuildSettings();
            Debug.Log("Cena Casa do Espelho criada! Abra a cena Menu, aperte Play e escolha o capítulo 1.");
        }

        // A ficha do capítulo aponta para esta cena (é assim que ele sai de "Em produção").
        static void LinkChapter()
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);
            if (chapter == null || chapter.sceneName == SceneName) return;
            chapter.sceneName = SceneName;
            EditorUtility.SetDirty(chapter);
            AssetDatabase.SaveAssets();
        }

        // Peça de cenário dentro de uma "pasta" da Hierarchy.
        static void Piece(string prefabPath, Transform parent, Vector3 position, float yaw)
        {
            var go = Place(prefabPath, position, yaw, null);
            go.transform.SetParent(parent);
        }

        static GameObject Place(string prefabPath, Vector3 position, float yaw, string name)
        {
            var go = EditorTools.Place(Load(prefabPath), position, name);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }
    }
}
