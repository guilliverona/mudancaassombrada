using Unity.AI.Navigation; // NavMeshSurface, NavMeshModifier (pacote AI Navigation)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Scripts dentro de uma pasta "Editor" só existem no editor da Unity, nunca no jogo final.
// Este cria a cena do Protótipo 1 automaticamente.
public static class PrototypeSceneBuilder
{
    // [MenuItem] adiciona uma opção no menu do topo da Unity.
    [MenuItem("Mudança Assombrada/Criar Cena Protótipo 1")]
    static void Build()
    {
        // Pergunta se quer salvar a cena atual antes de trocar.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Cena nova já com Main Camera e Directional Light.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Chão e paredes ---
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Chão";
        // Plane tem 10x10, então vira 20x30: a casa (z de -10 a 10) mais a rua com o caminhão.
        floor.transform.localScale = new Vector3(2f, 1f, 3f);
        floor.transform.position = new Vector3(0, 0, 5f);

        // Parede norte com a saída de 3m para a rua.
        CreateWall("Parede Norte A", new Vector3(-5.75f, 1.5f, 10), new Vector3(8.5f, 3, 0.2f));
        CreateWall("Parede Norte B", new Vector3(5.75f, 1.5f, 10), new Vector3(8.5f, 3, 0.2f));
        CreateWall("Parede Sul",   new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.2f));
        CreateWall("Parede Leste", new Vector3(10, 1.5f, 0), new Vector3(0.2f, 3, 20));
        CreateWall("Parede Oeste", new Vector3(-10, 1.5f, 0), new Vector3(0.2f, 3, 20));
        // Parede do meio com uma "porta" de 1.8m: teste para passar o armário!
        // Cuidado: o NavMesh "encolhe" 0.5m de cada lado das paredes (raio do agente).
        // Com 1.2m sobrava só 0.2m de caminho, e o fantasma não conseguia passar.
        CreateWall("Divisória A", new Vector3(-5.45f, 1.5f, 0), new Vector3(9.1f, 3, 0.2f));
        CreateWall("Divisória B", new Vector3(5.45f, 1.5f, 0), new Vector3(9.1f, 3, 0.2f));

        // --- Caminhão (lá fora, passando pelo lado do fantasma: o perigo é o caminho!) ---
        CreateWall("Caminhão - Cabine", new Vector3(0, 1.25f, 19f), new Vector3(3f, 2.5f, 2f));
        CreateWall("Caminhão - Caçamba", new Vector3(0, 0.05f, 15.5f), new Vector3(3f, 0.1f, 4f));
        var truckZone = new GameObject("Caminhão - Zona de Entrega");
        truckZone.transform.position = new Vector3(0, 1f, 15.5f);
        var zoneCollider = truckZone.AddComponent<BoxCollider>();
        zoneCollider.size = new Vector3(3f, 2f, 4f);
        zoneCollider.isTrigger = true; // não bloqueia, só detecta
        var truck = truckZone.AddComponent<TruckZone>();

        // --- Player ---
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1f, -6f);
        var movement = player.AddComponent<PlayerMovement>(); // adiciona o CharacterController junto
        var carrier = player.AddComponent<FurnitureCarrier>();
        player.AddComponent<CaneTapper>();

        // --- Sistema de eco (visão da Dona Cida) ---
        var echo = new GameObject("EchoSystem").AddComponent<EchoSystem>();
        SetReference(echo, "echoShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Echo.shader"));

        var cam = Camera.main;
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 0.6f, 0);
        cam.transform.localRotation = Quaternion.identity;

        // Os campos são privados ([SerializeField]), então usamos SerializedObject,
        // que é exatamente o que o Inspector faz quando você arrasta algo para um campo.
        SetReference(movement, "cameraPivot", cam.transform);
        SetReference(carrier, "playerCamera", cam);

        // --- Móveis ---
        CreateFurniture("Armário", new Vector3(2, 0.75f, -4), new Vector3(1f, 1.5f, 0.6f), 3f, new Color(0.45f, 0.3f, 0.2f));
        CreateFurniture("Cadeira", new Vector3(-2, 0.3f, -4), new Vector3(0.5f, 0.6f, 0.5f), 1f, new Color(0.8f, 0.6f, 0.4f));
        CreateFurniture("Caixa",   new Vector3(0, 0.25f, -3), new Vector3(0.5f, 0.5f, 0.5f), 1.5f, new Color(0.7f, 0.55f, 0.35f));

        // O Player não pode virar "buraco" no mapa de caminhos.
        player.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        // --- NavMesh: o mapa de onde dá para andar ---
        // A Unity analisa o chão e as paredes e gera uma malha azul com as áreas caminháveis.
        // Isso se chama "bake" (assar): calcula uma vez no editor, e o jogo só consulta.
        var navGo = new GameObject("NavMesh");
        var surface = navGo.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        const string navPath = "Assets/Scenes/Prototipo1_NavMesh.asset";
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath); // salva o resultado num arquivo

        // --- Fantasma (criado DEPOIS do bake, para não virar obstáculo no mapa) ---
        var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ghost.name = "Fantasma Bibliotecário";
        ghost.transform.position = new Vector3(0, 1.2f, 6f);
        ghost.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f); // alto e magro, como no roteiro
        ghost.GetComponent<Collider>().isTrigger = true; // atravessável: ele "pega", não empurra
        var ghostRenderer = ghost.GetComponent<Renderer>();
        ghostRenderer.sharedMaterial = new Material(ghostRenderer.sharedMaterial) { color = new Color(0.25f, 0.05f, 0.1f) };
        var agent = ghost.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.baseOffset = 1.2f; // distância do centro da cápsula até os pés
        agent.radius = 0.3f;
        ghost.AddComponent<GhostHunter>();

        // --- GameManager ---
        var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        SetReference(gameManager, "truck", truck);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Prototipo1.unity");

        // Reiniciar a fase usa SceneManager.LoadScene, que só funciona com cenas na Build Settings.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Prototipo1.unity", true) };

        Debug.Log("Cena Protótipo 1 criada! Aperte Play.");
    }

    static void CreateWall(string name, Vector3 position, Vector3 size)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = size;
    }

    static void CreateFurniture(string name, Vector3 position, Vector3 size, float weight, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = size;
        var furniture = obj.AddComponent<Furniture>(); // adiciona o Rigidbody junto

        // Móveis se movem, então não entram no "bake". Em vez disso, o NavMeshObstacle
        // abre um buraco no mapa em tempo real, onde quer que o móvel esteja.
        obj.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        obj.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;

        var so = new SerializedObject(furniture);
        so.FindProperty("weight").floatValue = weight;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Material próprio com cor, usando o shader padrão do URP.
        var renderer = obj.GetComponent<Renderer>();
        var mat = new Material(renderer.sharedMaterial) { color = color };
        renderer.sharedMaterial = mat;
    }

    static void SetReference(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
