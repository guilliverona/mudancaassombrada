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

        // --- Sistema de eco (visão da Dona Cida) ---
        var echo = new GameObject("EchoSystem").AddComponent<EchoSystem>();
        SetReference(echo, "echoShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Echo.shader"));

        // --- Câmera de espera: o que se vê antes de conectar (vista de cima da casa) ---
        // A câmera padrão da cena vira a "de espera". Ela NÃO é a MainCamera: essa é a do jogador.
        var lobbyCam = Camera.main;
        lobbyCam.name = "Câmera de Espera";
        lobbyCam.tag = "Untagged";
        lobbyCam.transform.position = new Vector3(0, 22f, -4f);
        lobbyCam.transform.rotation = Quaternion.Euler(70f, 0, 0);

        // --- Player: agora é um PREFAB (um "molde") ---
        // Na cena não existe mais Player nenhum. Cada jogador que conecta ganha uma cópia do molde.
        CreatePlayerPrefab();

        // --- Móveis ---
        CreateFurniture("Armário", new Vector3(2, 0.75f, -4), new Vector3(1f, 1.5f, 0.6f), 3f, new Color(0.45f, 0.3f, 0.2f));
        CreateFurniture("Cadeira", new Vector3(-2, 0.3f, -4), new Vector3(0.5f, 0.6f, 0.5f), 1f, new Color(0.8f, 0.6f, 0.4f));
        CreateFurniture("Caixa",   new Vector3(0, 0.25f, -3), new Vector3(0.5f, 0.5f, 0.5f), 1.5f, new Color(0.7f, 0.55f, 0.35f));

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
        // Em rede: o fantasma existe na rede, e o host (dono) manda a posição para todos.
        ghost.AddComponent<Unity.Netcode.NetworkObject>();
        ghost.AddComponent<Unity.Netcode.Components.NetworkTransform>(); // padrão: autoridade do servidor
        ghost.AddComponent<GhostHunter>();

        // --- Rede dos sons: leva os sons de um computador para os outros ---
        var relay = new GameObject("SoundRelay");
        relay.AddComponent<Unity.Netcode.NetworkObject>();
        relay.AddComponent<SoundRelay>();

        // --- GameManager ---
        var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        SetReference(gameManager, "truck", truck);

        // --- Rede: o NetworkManager é o "cérebro" da conexão ---
        var netGo = new GameObject("NetworkManager");
        var net = netGo.AddComponent<Unity.Netcode.NetworkManager>();
        // O Transport é o "carteiro": quem de fato manda os pacotes pela rede.
        var transport = netGo.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (net.NetworkConfig == null) net.NetworkConfig = new Unity.Netcode.NetworkConfig();
        net.NetworkConfig.NetworkTransport = transport;
        net.NetworkConfig.PlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        netGo.AddComponent<ConnectionMenu>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Prototipo1.unity");

        // Objetos de rede da cena precisam de um "RG" (GlobalObjectIdHash) para o host e os
        // clientes saberem que o fantasma de um é o MESMO fantasma do outro. O Netcode calcula
        // esse número a partir do arquivo da cena, então só funciona DEPOIS do primeiro save.
        // O método que calcula (OnValidate) é interno do Netcode; chamamos por "reflexão",
        // um jeito do C# de acessar métodos que normalmente não estariam disponíveis.
        var validate = typeof(Unity.Netcode.NetworkObject).GetMethod("OnValidate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var netObj in Object.FindObjectsByType<Unity.Netcode.NetworkObject>())
            validate.Invoke(netObj, null);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); // salva de novo, agora com os números certos

        // Reiniciar a fase usa SceneManager.LoadScene, que só funciona com cenas na Build Settings.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Prototipo1.unity", true) };

        Debug.Log("Cena Protótipo 1 criada! Aperte Play.");
    }

    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    static void CreatePlayerPrefab()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1f, -6f); // onde os jogadores nascem

        // Corpo: "boneco de blocos" (ver CreateDoll). Os OUTROS jogadores precisam te ver;
        // o seu próprio boneco o NetworkPlayer esconde da sua câmera.
        var body = CreateDoll();
        body.transform.SetParent(player.transform, false);

        var camGo = new GameObject("Câmera do Jogador", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0, 0.6f, 0);
        var cam = camGo.GetComponent<Camera>();
        cam.nearClipPlane = 0.05f;

        var movement = player.AddComponent<PlayerMovement>(); // adiciona o CharacterController junto
        var carrier = player.AddComponent<FurnitureCarrier>();
        var cane = player.AddComponent<CaneTapper>();
        var vibration = player.AddComponent<VibrationVision>();
        var whistle = player.AddComponent<Whistle>();
        var switcher = player.AddComponent<CharacterSwitcher>();
        player.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        // Os campos são privados ([SerializeField]), então usamos SerializedObject,
        // que é exatamente o que o Inspector faz quando você arrasta algo para um campo.
        SetReference(movement, "cameraPivot", camGo.transform);
        SetReference(carrier, "playerCamera", cam);
        // A "roupa" (tronco e braços) ganha a cor do personagem.
        var clothes = new SerializedObject(switcher).FindProperty("body");
        Renderer[] shirtParts =
        {
            body.transform.Find("Tronco").GetComponent<Renderer>(),
            body.transform.Find("Braço E/Braço E (malha)").GetComponent<Renderer>(),
            body.transform.Find("Braço D/Braço D (malha)").GetComponent<Renderer>(),
        };
        clothes.arraySize = shirtParts.Length;
        for (int i = 0; i < shirtParts.Length; i++)
            clothes.GetArrayElementAtIndex(i).objectReferenceValue = shirtParts[i];
        clothes.serializedObject.ApplyModifiedPropertiesWithoutUndo();

        // --- Componentes de rede ---
        player.AddComponent<Unity.Netcode.NetworkObject>(); // "este objeto existe na rede"
        var netTransform = player.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        // Owner = quem controla o boneco manda a posição dele. Sem isso, só o host poderia mover os jogadores.
        netTransform.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Owner;

        var netPlayer = player.AddComponent<NetworkPlayer>();
        var so = new SerializedObject(netPlayer);
        var list = so.FindProperty("ownerOnly");
        // O CharacterSwitcher NÃO entra: ele precisa rodar em todos para pintar a cor de cada boneco.
        Behaviour[] ownerOnly = { movement, carrier, cane, vibration, whistle };
        list.arraySize = ownerOnly.Length;
        for (int i = 0; i < ownerOnly.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = ownerOnly[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // Salva o molde como arquivo e apaga a cópia que estava na cena.
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
    }

    // Boneco simples de 2m (do mesmo tamanho do CharacterController: pés em y=-1, topo em y=+1).
    // Braços e pernas ficam dentro de um objeto vazio no ombro/quadril: é esse "pivô"
    // que gira, então o membro balança pela junta, e não pelo meio.
    static GameObject CreateDoll()
    {
        var doll = new GameObject("Corpo");
        var skin = DollMaterial("Pele", new Color(0.95f, 0.75f, 0.6f));
        var shirt = DollMaterial("Camisa", new Color(0.2f, 0.45f, 0.75f));
        var pants = DollMaterial("Calça", new Color(0.25f, 0.25f, 0.3f));
        var eyes = DollMaterial("Olhos", Color.black);

        CreatePart(PrimitiveType.Cube, "Tronco", doll.transform, new Vector3(0, 0.15f, 0), new Vector3(0.5f, 0.7f, 0.3f), shirt);
        var head = CreatePart(PrimitiveType.Sphere, "Cabeça", doll.transform, new Vector3(0, 0.75f, 0), Vector3.one * 0.4f, skin);
        // Olhos na frente (+Z) para os outros saberem para onde você está olhando.
        CreatePart(PrimitiveType.Sphere, "Olho E", head, new Vector3(-0.2f, 0.1f, 0.45f), Vector3.one * 0.15f, eyes);
        CreatePart(PrimitiveType.Sphere, "Olho D", head, new Vector3(0.2f, 0.1f, 0.45f), Vector3.one * 0.15f, eyes);

        var leftArm = CreateLimb("Braço E", doll.transform, new Vector3(-0.33f, 0.45f, 0), 0.65f, 0.15f, shirt);
        var rightArm = CreateLimb("Braço D", doll.transform, new Vector3(0.33f, 0.45f, 0), 0.65f, 0.15f, shirt);
        var leftLeg = CreateLimb("Perna E", doll.transform, new Vector3(-0.12f, -0.2f, 0), 0.8f, 0.2f, pants);
        var rightLeg = CreateLimb("Perna D", doll.transform, new Vector3(0.12f, -0.2f, 0), 0.8f, 0.2f, pants);

        var walk = doll.AddComponent<DollWalk>();
        SetReference(walk, "leftArm", leftArm);
        SetReference(walk, "rightArm", rightArm);
        SetReference(walk, "leftLeg", leftLeg);
        SetReference(walk, "rightLeg", rightLeg);
        return doll;
    }

    // Um prefab só guarda REFERÊNCIAS a materiais, então eles precisam existir como arquivo.
    // (Na cena funciona sem isso porque a cena guarda o material dentro dela.)
    static Material DollMaterial(string name, Color color)
    {
        const string folder = "Assets/Prefabs/Materiais";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Materiais");
        string path = $"{folder}/Boneco {name}.mat";
        AssetDatabase.DeleteAsset(path);
        // Copia o material padrão de um cubo temporário (o mesmo truque dos móveis).
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mat = new Material(temp.GetComponent<Renderer>().sharedMaterial) { color = color };
        Object.DestroyImmediate(temp);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Transform CreateLimb(string name, Transform parent, Vector3 joint, float length, float thickness, Material color)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = joint;
        CreatePart(PrimitiveType.Cube, name + " (malha)", pivot, new Vector3(0, -length / 2, 0), new Vector3(thickness, length, thickness), color);
        return pivot;
    }

    static Transform CreatePart(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        // Sem collider: o CharacterController do Player já cuida das colisões.
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
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
        // Em rede: o móvel existe na rede, e quem é DONO dele simula a física e manda a posição.
        obj.AddComponent<Unity.Netcode.NetworkObject>();
        var furniture = obj.AddComponent<Furniture>(); // adiciona o Rigidbody junto
        var netTransform = obj.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        netTransform.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Owner;
        // NetworkRigidbody: liga a física só no dono; nos outros, o Rigidbody fica "kinematic".
        obj.AddComponent<Unity.Netcode.Components.NetworkRigidbody>();

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
