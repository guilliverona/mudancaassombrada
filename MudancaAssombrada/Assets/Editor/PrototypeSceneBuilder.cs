using Unity.AI.Navigation; // NavMeshSurface (pacote AI Navigation)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Monta a cena do Protótipo 1 com os PREFABS (ver LevelPrefabsBuilder).
    //
    // A cena é um RASCUNHO INICIAL: depois de criada, ela é sua. Abra Assets/Scenes/Prototipo1,
    // arraste paredes, móveis e fantasmas dos prefabs, mova, duplique... e salve.
    // Depois de mexer nas paredes, clique no objeto "NavMesh" > Bake (refaz o mapa do fantasma).
    //
    // Rodar este menu de novo RECRIA a cena do zero (por isso ele pergunta antes).
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = EditorTools.ScenesFolder + "/Prototipo1.unity";

        [MenuItem("Mudança Assombrada/Criar Cena Protótipo 1", priority = 2)]
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
            return EditorUtility.DisplayDialog("Recriar a cena Protótipo 1?",
                "A cena já existe. Recriar apaga tudo que foi feito à mão nela (posições, objetos novos...).",
                "Recriar", "Manter a minha");
        }

        public static void BuildScene()
        {
            // Cena nova já com Main Camera e Directional Light.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // --- Chão e paredes ---
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Chão";
            // Plane tem 10x10, então vira 20x30: a casa (z de -10 a 10) mais a rua com o caminhão.
            floor.transform.localScale = new Vector3(2f, 1f, 3f);
            floor.transform.position = new Vector3(0, 0, 5f);

            var house = new GameObject("Casa").transform; // "pasta" na Hierarchy para organizar
            // Parede norte com a saída de 3m para a rua.
            Wall(house, "Parede Norte A", new Vector3(-5.75f, 1.5f, 10), new Vector3(8.5f, 3, 0.2f));
            Wall(house, "Parede Norte B", new Vector3(5.75f, 1.5f, 10), new Vector3(8.5f, 3, 0.2f));
            Wall(house, "Parede Sul", new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.2f));
            Wall(house, "Parede Leste", new Vector3(10, 1.5f, 0), new Vector3(0.2f, 3, 20));
            Wall(house, "Parede Oeste", new Vector3(-10, 1.5f, 0), new Vector3(0.2f, 3, 20));
            // Parede do meio com uma "porta" de 1.8m: teste para passar o armário!
            // Cuidado: o NavMesh "encolhe" 0.5m de cada lado das paredes (raio do agente).
            // Com 1.2m sobrava só 0.2m de caminho, e o fantasma não conseguia passar.
            Wall(house, "Divisória A", new Vector3(-5.45f, 1.5f, 0), new Vector3(9.1f, 3, 0.2f));
            Wall(house, "Divisória B", new Vector3(5.45f, 1.5f, 0), new Vector3(9.1f, 3, 0.2f));

            // --- Caminhão (lá fora, passando pelo lado do fantasma: o perigo é o caminho!) ---
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.TruckPath), new Vector3(0, 0, 15.5f));

            // --- Móveis ---
            // Objetos de rede (móveis, fantasma, sistemas) ficam na raiz da cena, fora de "pastas":
            // o Netcode só aceita um objeto de rede dentro de OUTRO objeto de rede.
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.WardrobePath), new Vector3(2, 0.75f, -4));
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.ChairPath), new Vector3(-2, 0.3f, -4));
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.BoxPath), new Vector3(0, 0.25f, -3));

            // --- Fantasma ---
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.GhostPath), new Vector3(0, 1.2f, 6f));

            // --- Objetivos da fase ---
            // Um objeto por objetivo. Esta fase só tem um: levar TODOS os móveis para o caminhão.
            // Para outras fases: adicione ReachAreaObjective, InteractObjective... e use o campo
            // "Stage" para dividir em etapas (ver ARQUITETURA.md).
            var objectives = new GameObject("Objetivos").transform;
            var deliver = new GameObject("Entregar todos os móveis");
            deliver.transform.SetParent(objectives);
            deliver.AddComponent<DeliverFurnitureObjective>();

            // --- Câmera de espera: o que se vê antes de o seu boneco nascer (vista de cima da casa) ---
            // A câmera padrão da cena vira a "de espera". Ela NÃO é a MainCamera: essa é a do jogador.
            var lobbyCam = Camera.main;
            lobbyCam.name = "Câmera de Espera";
            lobbyCam.tag = "Untagged";
            lobbyCam.transform.position = new Vector3(0, 22f, -4f);
            lobbyCam.transform.rotation = Quaternion.Euler(70f, 0, 0);
            lobbyCam.gameObject.AddComponent<LobbyCamera>(); // se desliga quando o seu boneco nasce

            // --- Sistemas da fase ---
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.EchoPath), Vector3.zero);
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.SoundRelayPath), Vector3.zero);
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.GameManagerPath), Vector3.zero);
            // Os jogadores nascem onde o PlayerSpawner está.
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.SpawnerPath), new Vector3(0, 1f, -6f));

            // --- Interface ---
            UiBuilder.CreateEventSystem();
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.HudPath), Vector3.zero);
            EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.PausePath), Vector3.zero);

            // --- NavMesh: o mapa de onde dá para andar ---
            // A Unity analisa o chão e as paredes e gera uma malha azul com as áreas caminháveis.
            // Isso se chama "bake" (assar): calcula uma vez no editor, e o jogo só consulta.
            // Móveis, fantasma e jogadores têm NavMeshModifier "ignorar": não viram parede no mapa.
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            const string navPath = EditorTools.ScenesFolder + "/Prototipo1_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath); // salva o resultado num arquivo

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorTools.FixNetworkObjectIds(scene); // gera os "RGs" dos objetos de rede e salva de novo
            EditorTools.UpdateBuildSettings();
            Debug.Log("Cena Protótipo 1 criada! Para jogar, abra a cena Menu e aperte Play.");
        }

        // Cada parede é uma cópia do prefab Parede, esticada até o tamanho certo.
        static void Wall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var wall = EditorTools.Place(LevelPrefabsBuilder.Load(LevelPrefabsBuilder.WallPath), position, name);
            wall.transform.localScale = size;
            wall.transform.SetParent(parent);
        }
    }
}
