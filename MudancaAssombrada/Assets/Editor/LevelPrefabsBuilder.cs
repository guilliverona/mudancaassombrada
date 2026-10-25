using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static MudancaAssombrada.Theme;

namespace MudancaAssombrada.Editor
{
    // Cria os PREFABS do jogo: as "peças de Lego" com que as fases são montadas.
    //
    //   Assets/Prefabs/Casa       Parede, Caminhão
    //   Assets/Prefabs/Móveis     Armário, Cadeira, Caixa
    //   Assets/Prefabs/Fantasmas  Fantasma Bibliotecário
    //   Assets/Prefabs/Sistemas   EchoSystem, SoundRelay, GameManager, PlayerSpawner
    //   Assets/Prefabs/Interface  HUD, Pausa
    //   Assets/Prefabs/Player     (sempre recriado: é montado 100% por código)
    //
    // REGRA: um prefab só é criado se ainda NÃO existe. Depois disso ele é SEU: abra, troque o
    // modelo, mude números no Inspector. Rodar o menu de novo não apaga o que você fez.
    // (Para voltar ao original, apague o prefab e rode de novo, ou use "Recriar Todos os Prefabs".)
    public static class LevelPrefabsBuilder
    {
        public const string PlayerPath = "Assets/Prefabs/Player.prefab";
        public const string WallPath = "Assets/Prefabs/Casa/Parede.prefab";
        public const string TruckPath = "Assets/Prefabs/Casa/Caminhão.prefab";
        public const string WardrobePath = "Assets/Prefabs/Móveis/Armário.prefab";
        public const string ChairPath = "Assets/Prefabs/Móveis/Cadeira.prefab";
        public const string BoxPath = "Assets/Prefabs/Móveis/Caixa.prefab";
        public const string GhostPath = "Assets/Prefabs/Fantasmas/Fantasma Bibliotecário.prefab";
        public const string EchoPath = "Assets/Prefabs/Sistemas/EchoSystem.prefab";
        public const string SoundRelayPath = "Assets/Prefabs/Sistemas/SoundRelay.prefab";
        public const string GameManagerPath = "Assets/Prefabs/Sistemas/GameManager.prefab";
        public const string SpawnerPath = "Assets/Prefabs/Sistemas/PlayerSpawner.prefab";
        public const string HudPath = "Assets/Prefabs/Interface/HUD.prefab";
        public const string PausePath = "Assets/Prefabs/Interface/Pausa.prefab";

        [MenuItem("Mudança Assombrada/Criar Prefabs que Faltam", priority = 21)]
        static void EnsureMenu() => EnsurePrefabs(recreateAll: false);

        [MenuItem("Mudança Assombrada/Recriar Todos os Prefabs", priority = 22)]
        static void RecreateMenu()
        {
            if (!EditorUtility.DisplayDialog("Recriar todos os prefabs?",
                    "Tudo que você mudou à mão nos prefabs (paredes, móveis, fantasma, HUD...) será perdido.\n\n" +
                    "Cópias desses prefabs nas cenas podem perder ajustes de posição: recrie as cenas depois.",
                    "Recriar", "Cancelar")) return;
            EnsurePrefabs(recreateAll: true);
        }

        public static void EnsurePrefabs(bool recreateAll)
        {
            GameDataBuilder.EnsureData();

            // O Player é sempre recriado: ninguém monta ele à mão, e ele nunca fica numa cena
            // (nasce pela rede). Assim, mudanças no código chegam nele sempre.
            BuildPlayer();

            Build(WallPath, recreateAll, BuildWall);
            Build(TruckPath, recreateAll, BuildTruck);
            Build(WardrobePath, recreateAll, () => BuildFurniture("Armário", new Vector3(1f, 1.5f, 0.6f), 3f, new Color(0.45f, 0.3f, 0.2f)));
            Build(ChairPath, recreateAll, () => BuildFurniture("Cadeira", new Vector3(0.5f, 0.6f, 0.5f), 1f, new Color(0.8f, 0.6f, 0.4f)));
            Build(BoxPath, recreateAll, () => BuildFurniture("Caixa", new Vector3(0.5f, 0.5f, 0.5f), 1.5f, new Color(0.7f, 0.55f, 0.35f)));
            Build(GhostPath, recreateAll, BuildGhost);
            Build(EchoPath, recreateAll, BuildEchoSystem);
            Build(SoundRelayPath, recreateAll, BuildSoundRelay);
            Build(GameManagerPath, recreateAll, BuildGameManager);
            Build(SpawnerPath, recreateAll, BuildSpawner);
            Build(HudPath, recreateAll, HudBuilder.Build);
            Build(PausePath, recreateAll, BuildPauseMenu);
            AssetDatabase.SaveAssets();
        }

        // Cria o prefab só se ele não existe (ou se pediram para recriar tudo).
        static void Build(string path, bool recreate, System.Func<GameObject> create)
        {
            if (!recreate && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            EditorTools.SavePrefab(create(), path);
        }

        public static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        // ---------- Casa ----------

        // Um cubo de 1x1x1. Na cena, cada parede é uma cópia ESTICADA (escala) deste prefab.
        // Trocar o material aqui troca de todas as paredes de uma vez.
        static GameObject BuildWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Parede";
            wall.GetComponent<Renderer>().sharedMaterial = EditorTools.ColorMaterial("Parede", new Color(0.75f, 0.75f, 0.72f));
            return wall;
        }

        static GameObject BuildTruck()
        {
            // A raiz fica no chão, no meio da caçamba.
            var truck = new GameObject("Caminhão");
            var paint = EditorTools.ColorMaterial("Caminhão", new Color(0.35f, 0.4f, 0.45f));

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabine";
            cabin.transform.SetParent(truck.transform, false);
            cabin.transform.localPosition = new Vector3(0, 1.25f, 3.5f);
            cabin.transform.localScale = new Vector3(3f, 2.5f, 2f);
            cabin.GetComponent<Renderer>().sharedMaterial = paint;

            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Caçamba";
            bed.transform.SetParent(truck.transform, false);
            bed.transform.localPosition = new Vector3(0, 0.05f, 0);
            bed.transform.localScale = new Vector3(3f, 0.1f, 4f);
            bed.GetComponent<Renderer>().sharedMaterial = paint;

            var zone = new GameObject("Zona de Entrega");
            zone.transform.SetParent(truck.transform, false);
            zone.transform.localPosition = new Vector3(0, 1f, 0);
            var zoneCollider = zone.AddComponent<BoxCollider>();
            zoneCollider.size = new Vector3(3f, 2f, 4f);
            zoneCollider.isTrigger = true; // não bloqueia, só detecta
            zone.AddComponent<TruckZone>();
            return truck;
        }

        // ---------- Móveis e fantasmas ----------

        static GameObject BuildFurniture(string name, Vector3 size, float weight, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = EditorTools.ColorMaterial(name, color);

            // Em rede: o móvel existe na rede, e quem é DONO dele simula a física e manda a posição.
            obj.AddComponent<NetworkObject>();
            var furniture = obj.AddComponent<Furniture>(); // adiciona o Rigidbody junto
            var netTransform = obj.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            // NetworkRigidbody: liga a física só no dono; nos outros, o Rigidbody fica "kinematic".
            obj.AddComponent<NetworkRigidbody>();

            // Móveis se movem, então não entram no "bake" do NavMesh. Em vez disso, o NavMeshObstacle
            // abre um buraco no mapa em tempo real, onde quer que o móvel esteja.
            obj.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            obj.AddComponent<NavMeshObstacle>().carving = true;

            EditorTools.SetFloat(furniture, "weight", weight);
            return obj;
        }

        static GameObject BuildGhost()
        {
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = "Fantasma Bibliotecário";
            ghost.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f); // alto e magro, como no roteiro
            ghost.GetComponent<Collider>().isTrigger = true; // atravessável: ele "pega", não empurra
            ghost.GetComponent<Renderer>().sharedMaterial = EditorTools.ColorMaterial("Fantasma", new Color(0.25f, 0.05f, 0.1f));

            var agent = ghost.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1.2f; // distância do centro da cápsula até os pés
            agent.radius = 0.3f;
            // Fantasma não é parede: fica fora do "bake" do NavMesh.
            ghost.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            // Em rede: o fantasma existe na rede, e o host (dono) manda a posição para todos.
            ghost.AddComponent<NetworkObject>();
            ghost.AddComponent<NetworkTransform>(); // padrão: autoridade do servidor
            ghost.AddComponent<GhostHunter>();
            return ghost;
        }

        // ---------- Sistemas ----------
        // Cada um é um prefab separado: objetos de rede (NetworkObject) não podem ficar
        // um dentro do outro.

        static GameObject BuildEchoSystem()
        {
            var echo = new GameObject("EchoSystem").AddComponent<EchoSystem>();
            EditorTools.SetReference(echo, "echoShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Echo.shader"));
            return echo.gameObject;
        }

        static GameObject BuildSoundRelay()
        {
            // Leva os sons de um computador para os outros.
            var relay = new GameObject("SoundRelay");
            relay.AddComponent<NetworkObject>();
            relay.AddComponent<SoundRelay>();
            return relay;
        }

        static GameObject BuildGameManager()
        {
            var go = new GameObject("GameManager");
            go.AddComponent<NetworkObject>(); // o cronômetro e o resultado existem na rede
            go.AddComponent<GameManager>();
            return go;
        }

        static GameObject BuildSpawner()
        {
            // Os jogadores nascem onde este objeto estiver na cena.
            var spawner = new GameObject("PlayerSpawner").AddComponent<PlayerSpawner>();
            EditorTools.SetReference(spawner, "playerPrefab", Load(PlayerPath));
            return spawner.gameObject;
        }

        // ---------- Interface ----------

        // Menu de pausa (Esc): mesmas peças de interface do Menu principal (UiBuilder).
        static GameObject BuildPauseMenu()
        {
            var canvas = UiBuilder.CreateCanvas("Pausa", sortingOrder: 10); // na frente da HUD

            var panel = UiBuilder.CreatePanel("Painel Pausa", canvas, 460);
            var pauseTitle = UiBuilder.CreateText("Título", panel, "Pausa", HeaderSize);
            pauseTitle.font = GameFonts.Title; // cabeçalho: mesma fonte do título do menu
            var resumeButton = UiBuilder.CreateButton("Continuar", panel);
            var optionsButton = UiBuilder.CreateButton("Opções", panel);
            var leaveButton = UiBuilder.CreateButton("Voltar ao menu", panel);
            var infoText = UiBuilder.CreateText("Info", panel, "", NoteSize);
            infoText.color = MutedColor;
            UiBuilder.SetHeight(infoText.gameObject, 60);

            var options = UiBuilder.CreateOptionsPanel(canvas);

            var pause = canvas.gameObject.AddComponent<PauseMenu>();
            EditorTools.SetReference(pause, "pausePanel", panel.gameObject);
            EditorTools.SetReference(pause, "options", options);
            EditorTools.SetReference(pause, "resumeButton", resumeButton);
            EditorTools.SetReference(pause, "optionsButton", optionsButton);
            EditorTools.SetReference(pause, "leaveButton", leaveButton);
            EditorTools.SetReference(pause, "infoText", infoText);
            return canvas.gameObject;
        }

        // ---------- Player ----------

        static void BuildPlayer()
        {
            var player = new GameObject("Player");

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

            EditorTools.SetReference(movement, "cameraPivot", camGo.transform);
            EditorTools.SetReference(carrier, "playerCamera", cam);
            // A "roupa" (tronco e braços) ganha a cor do personagem.
            EditorTools.SetReferences(switcher, "body", new Object[]
            {
                body.transform.Find("Tronco").GetComponent<Renderer>(),
                body.transform.Find("Braço E/Braço E (malha)").GetComponent<Renderer>(),
                body.transform.Find("Braço D/Braço D (malha)").GetComponent<Renderer>(),
            });

            // --- Componentes de rede ---
            player.AddComponent<NetworkObject>(); // "este objeto existe na rede"
            var netTransform = player.AddComponent<NetworkTransform>();
            // Owner = quem controla o boneco manda a posição dele. Sem isso, só o host poderia mover os jogadores.
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;

            var netPlayer = player.AddComponent<NetworkPlayer>();
            // O CharacterSwitcher NÃO entra: ele precisa rodar em todos para pintar a cor de cada boneco.
            EditorTools.SetReferences(netPlayer, "ownerOnly", new Object[] { movement, carrier, cane, vibration, whistle });

            EditorTools.SavePrefab(player, PlayerPath);
        }

        // Boneco simples de 2m (do mesmo tamanho do CharacterController: pés em y=-1, topo em y=+1).
        // Braços e pernas ficam dentro de um objeto vazio no ombro/quadril: é esse "pivô"
        // que gira, então o membro balança pela junta, e não pelo meio.
        static GameObject CreateDoll()
        {
            var doll = new GameObject("Corpo");
            var skin = EditorTools.ColorMaterial("Boneco Pele", new Color(0.95f, 0.75f, 0.6f));
            var shirt = EditorTools.ColorMaterial("Boneco Camisa", new Color(0.2f, 0.45f, 0.75f));
            var pants = EditorTools.ColorMaterial("Boneco Calça", new Color(0.25f, 0.25f, 0.3f));
            var eyes = EditorTools.ColorMaterial("Boneco Olhos", Color.black);

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
            EditorTools.SetReference(walk, "leftArm", leftArm);
            EditorTools.SetReference(walk, "rightArm", rightArm);
            EditorTools.SetReference(walk, "leftLeg", leftLeg);
            EditorTools.SetReference(walk, "rightLeg", rightLeg);
            return doll;
        }

        static Transform CreateLimb(string name, Transform parent, Vector3 joint, float length, float thickness, Material material)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            CreatePart(PrimitiveType.Cube, name + " (malha)", pivot, new Vector3(0, -length / 2, 0), new Vector3(thickness, length, thickness), material);
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
    }
}
