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
    //   Assets/Prefabs/Sistemas   EchoSystem, PlayerSpawner, Iluminação Noturna e o
    //                             "Sistemas da Fase" (GameManager + SoundRelay + estes; ver LevelSceneTools)
    //   Assets/Prefabs/Interface  HUD, Pausa        (sempre recriados: interface é código)
    //   Assets/Prefabs/Player                       (sempre recriado: é montado 100% por código)
    //
    // REGRA: um prefab do MUNDO (casa, móveis, fantasmas, sistemas) só é criado se ainda NÃO
    // existe. Depois disso ele é SEU: abra, troque o modelo, mude números no Inspector.
    // Rodar o menu de novo não apaga o que você fez.
    // (Para voltar ao original, apague o prefab e rode de novo, ou use "Recriar Todos os Prefabs".)
    // Já a interface e o Player seguem o código, como a cena do Menu: mude no código, não no prefab.
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
        public const string SpawnerPath = "Assets/Prefabs/Sistemas/PlayerSpawner.prefab";
        public const string NightPath = "Assets/Prefabs/Sistemas/Iluminação Noturna.prefab";
        public const string HudPath = "Assets/Prefabs/Interface/HUD.prefab";
        public const string PausePath = "Assets/Prefabs/Interface/Pausa.prefab";

        [MenuItem("Mudança Assombrada/Avançado/Criar Prefabs que Faltam", priority = 103)]
        static void EnsureMenu() => EnsurePrefabs(recreateAll: false);

        [MenuItem("Mudança Assombrada/Avançado/Recriar Todos os Prefabs", priority = 104)]
        static void RecreateMenu()
        {
            if (!EditorUtility.DisplayDialog("Recriar todos os prefabs?",
                    "Tudo que você mudou à mão nos prefabs (paredes, móveis, fantasma, HUD...) será perdido.\n\n" +
                    "Cópias desses prefabs nas cenas podem perder ajustes de posição: recrie as cenas depois.",
                    "Recriar", "Cancelar")) return;
            EnsurePrefabs(recreateAll: true);
        }

        // Os prefabs são regravados em PASSOS (cria o bloco, depois põe o modelo, depois acrescenta
        // peças), e cada passo sorteia números internos novos. O Guard confere TODOS no fim: o que
        // ficou igual por dentro volta ao arquivo antigo e não aparece no Git (ver StableAssets).
        public static void EnsurePrefabs(bool recreateAll) =>
            StableAssets.Guard(AllPrefabs(), () => BuildPrefabs(recreateAll));

        // Todos os .prefab de Assets/Prefabs (com "/" no caminho, como a Unity usa).
        internal static string[] AllPrefabs()
        {
            if (!System.IO.Directory.Exists(EditorTools.PrefabsFolder)) return new string[0];
            var files = System.IO.Directory.GetFiles(EditorTools.PrefabsFolder, "*.prefab", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++) files[i] = files[i].Replace('\\', '/');
            return files;
        }

        static void BuildPrefabs(bool recreateAll)
        {
            GameDataBuilder.EnsureData();
            GameDataBuilder.AssignMissingModels();

            // O Player é sempre recriado: ninguém monta ele à mão, e ele nunca fica numa cena
            // (nasce pela rede). Assim, mudanças no código chegam nele sempre.
            BuildPlayer();

            Build(WallPath, recreateAll, BuildWall);
            Build(TruckPath, recreateAll, BuildTruck);
            Build(WardrobePath, recreateAll, () => BuildFurniture("Armário", new Vector3(1f, 1.5f, 0.6f), 3.5f,new Color(0.45f, 0.3f, 0.2f)));
            Build(ChairPath, recreateAll, () => BuildFurniture("Cadeira", new Vector3(0.5f, 0.6f, 0.5f), 1f, new Color(0.8f, 0.6f, 0.4f)));
            Build(BoxPath, recreateAll, () => BuildFurniture("Caixa", new Vector3(0.5f, 0.5f, 0.5f), 1.5f, new Color(0.7f, 0.55f, 0.35f)));
            Build(GhostPath, recreateAll, BuildGhost);
            Build(EchoPath, recreateAll, BuildEchoSystem);
            Build(SpawnerPath, recreateAll, BuildSpawner);
            UpgradeGhost(GhostPath);
            // Noite que vira amanhecer. As cores ficam no Inspector do prefab.
            Build(NightPath, recreateAll, () => new GameObject("Iluminação Noturna", typeof(NightLighting)));
            Build(HudPath, recreate: true, HudBuilder.Build);
            Build(PausePath, recreate: true, BuildPauseMenu);
            ApplyArtToPrefabs();
            SoundLibraryBuilder.EnsureLibrary(); // a biblioteca de sons e a "voz" dos fantasmas
            LevelSceneTools.EnsureSystemsPrefab(); // o prefab único que toda fase usa (kit de autoria)
            AssetDatabase.SaveAssets();
        }

        // Cria o prefab só se ele não existe (ou se pediram para recriar tudo).
        internal static void Build(string path, bool recreate, System.Func<GameObject> create)
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
            AddGhostParts(ghost);
            ghost.AddComponent<GhostHunter>(); // o comportamento: caçar pelo som
            return ghost;
        }

        // A base comum de todo fantasma (Fase 2): rede, captura, passos e estado (Ghost),
        // o desenho que lê o estado (GhostVisual) e a respiração. O comportamento vem à parte.
        // breathes = false: fantasma que NÃO respira (o Imitador; é a pista da Cida).
        internal static void AddGhostParts(GameObject ghost, bool breathes = true)
        {
            if (!ghost.TryGetComponent(out Ghost _)) ghost.AddComponent<Ghost>();
            if (!ghost.TryGetComponent(out GhostVisual _)) ghost.AddComponent<GhostVisual>();
            if (!ghost.TryGetComponent(out Pointable _)) ghost.AddComponent<Pointable>(); // dá para apontar ("é falso!")
            if (breathes && !ghost.TryGetComponent(out Breathing breathing))
            {
                breathing = ghost.AddComponent<Breathing>();
                EditorTools.SetFloat(breathing, "mouthHeight", 1f); // o Bibliotecário é alto
            }
        }

        // Prefabs de fantasma feitos ANTES da base comum só tinham o GhostHunter.
        // Aqui só ACRESCENTAMOS o que falta: números ajustados no Inspector continuam.
        internal static void UpgradeGhost(string prefabPath, bool breathes = true)
        {
            if (Load(prefabPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Quem caça pelo som precisa do ouvido (GhostHearing), que antes morava dentro do GhostHunter.
                bool needsEar = root.TryGetComponent(out GhostHunter _) && !root.TryGetComponent(out GhostHearing _);
                if (root.TryGetComponent(out Ghost _) && root.TryGetComponent(out GhostVisual _) && root.TryGetComponent(out Pointable _) &&
                    (!breathes || root.TryGetComponent(out Breathing _)) && !needsEar) return;
                AddGhostParts(root, breathes);
                if (needsEar) root.AddComponent<GhostHearing>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"Fantasma atualizado para a base comum: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Arte (modelos do Blender) ----------
        // Os prefabs do mundo nascem como blocos. Quando o modelo do Blender existe em Assets/Art
        // (gerado por tools/blender), o bloco vira só a colisão, invisível, e o modelo entra como
        // filho "Modelo". Só o VISUAL muda: peso, velocidades, tamanho e o que mais foi ajustado
        // no Inspector continuam, e as cópias nas cenas não perdem nada.
        // O "Modelo" fica ligado ao FBX: gerar de novo no Blender atualiza o prefab sozinho.
        // Para voltar ao bloco, apague o filho "Modelo" e religue o MeshRenderer.

        const string ModelChild = "Modelo";
        const float GhostHeight = 2.6f; // o Bibliotecário da ficha (ART_BIBLE.md)

        static void ApplyArtToPrefabs()
        {
            ApplyArt(WardrobePath, "Props/Armario/SM_PRP_Armario", (root, model) => FurnitureArt(root, model, new Vector3(1f, 1.5f, 0.6f)));
            ApplyArt(ChairPath, "Props/Cadeira/SM_PRP_Cadeira", (root, model) => FurnitureArt(root, model, new Vector3(0.5f, 0.6f, 0.5f)));
            ApplyArt(BoxPath, "Props/Caixa/SM_PRP_Caixa", (root, model) => FurnitureArt(root, model, new Vector3(0.5f, 0.5f, 0.5f)));
            ApplyArt(TruckPath, "Props/Caminhao/SM_PRP_Caminhao", TruckArt);
            ApplyArt(GhostPath, "Ghosts/Bibliotecario/SK_GHO_Bibliotecario", (root, model) => GhostArt(root, model, GhostHeight));
        }

        // Abre o prefab, troca o visual e salva. Prefab que já tem "Modelo" não é tocado de novo.
        static void ApplyArt(string prefabPath, string art, System.Action<GameObject, GameObject> apply)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/{art}.fbx");
            if (model == null || Load(prefabPath) == null) return;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.transform.Find(ModelChild) != null) return;
                apply(root, model);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"Arte aplicada: {prefabPath} agora usa {art}.fbx");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Móvel: a raiz era um cubo esticado (escala = tamanho). Agora a escala volta a 1, o
        // tamanho vai para o BoxCollider e o modelo (origem na base) desce até o chão da caixa.
        // Se alguém mudou o tamanho do móvel, o modelo estica junto.
        static void FurnitureArt(GameObject root, GameObject model, Vector3 artSize)
        {
            Vector3 size = root.transform.localScale;
            root.transform.localScale = Vector3.one;
            RemoveMesh(root);
            var box = root.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = size;

            var visual = AddModel(root, model, new Vector3(0, -size.y / 2f, 0));
            visual.transform.localScale = new Vector3(size.x / artSize.x, size.y / artSize.y, size.z / artSize.z);
        }

        // Caminhão: a cabine e a caçamba continuam como colisão (sem desenho); o modelo tem o
        // mesmo formato, com a raiz no chão, no meio da caçamba.
        static void TruckArt(GameObject root, GameObject model)
        {
            foreach (Transform part in root.transform)
                if (part.GetComponent<BoxCollider>() != null && !part.GetComponent<BoxCollider>().isTrigger)
                    RemoveMesh(part.gameObject);
            AddModel(root, model, Vector3.zero);
        }

        // Fantasma: a cápsula esticada vira uma cápsula de colisão do tamanho do modelo, e o
        // NavMeshAgent passa a "flutuar" metade da altura acima do chão (a raiz fica no meio do corpo).
        internal static void GhostArt(GameObject root, GameObject model, float height)
        {
            root.transform.localScale = Vector3.one;
            RemoveMesh(root);
            var capsule = root.GetComponent<CapsuleCollider>();
            capsule.center = Vector3.zero;
            capsule.height = height;
            capsule.radius = 0.3f;
            var agent = root.GetComponent<NavMeshAgent>();
            agent.baseOffset = height / 2f;
            agent.height = height;

            var visual = AddModel(root, model, new Vector3(0, -height / 2f, 0));
            visual.AddComponent<DollWalk>(); // balança braços e pernas pelos ossos do Humanoid
        }

        internal static GameObject AddModel(GameObject root, GameObject model, Vector3 localPosition)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.name = ModelChild;
            visual.transform.localPosition = localPosition;
            return visual;
        }

        // Tira o desenho do bloco e deixa só a colisão.
        internal static void RemoveMesh(GameObject go)
        {
            if (go.TryGetComponent(out MeshRenderer renderer)) Object.DestroyImmediate(renderer);
            if (go.TryGetComponent(out MeshFilter filter)) Object.DestroyImmediate(filter);
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

        const float PlayerFieldOfView = 55f;

        static void BuildPlayer()
        {
            var player = new GameObject("Player");

            // Corpo: "boneco de blocos" (ver CreateDoll). Os OUTROS jogadores precisam te ver;
            // o seu próprio boneco o NetworkPlayer esconde da sua câmera.
            var body = CreateDoll();
            body.transform.SetParent(player.transform, false);

            // Lugar onde nasce o modelo 3D da ficha do personagem (se ela tiver um).
            // Pés em y=-1: o centro do Player é o meio do CharacterController de 2m.
            var modelRoot = new GameObject("Modelo").transform;
            modelRoot.SetParent(player.transform, false);
            modelRoot.localPosition = new Vector3(0, -1f, 0);

            var camGo = new GameObject("Câmera do Jogador", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0.6f, 0);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            // Campo de visão VERTICAL (graus). 55 ≈ 85° na horizontal em 16:9: um pouco mais fechado
            // que o padrão (60), para o escuro e os cantos da casa esconderem mais coisa.
            cam.fieldOfView = PlayerFieldOfView;
            // Nasce com fundo escuro (e não com o céu azul padrão): quem pinta o céu é a NightLighting.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var movement = player.AddComponent<PlayerMovement>(); // adiciona o CharacterController junto
            var carrier = player.AddComponent<FurnitureCarrier>();
            var interactor = player.AddComponent<PlayerInteractor>(); // a mira e a tecla Interagir
            var cues = player.AddComponent<CueListener>();             // o que EU percebi (para a HUD)
            player.AddComponent<PlayerCondition>();                    // normal / puxado / caído (e o "Ajudar")
            player.AddComponent<Pointable>();                          // dá para apontar para mim (Imitador)
            var cane = player.AddComponent<CaneTapper>();
            var vibration = player.AddComponent<VibrationVision>();
            var whistle = player.AddComponent<Whistle>();
            var switcher = player.AddComponent<CharacterSwitcher>();
            player.AddComponent<Breathing>(); // a Cida ouve quem respira (o Imitador não respira)
            var fork = player.AddComponent<TuningFork>();  // o diapasão da Cida (tecla Q)
            player.AddComponent<CaneCall>();               // a chamada da bengala (segurar 2 s), em rede
            // No diapasão, uma pessoa de verdade "estremece" (a cópia do Imitador não responde).
            var so = new SerializedObject(player.AddComponent<SurfaceTag>());
            so.FindProperty("type").enumValueIndex = (int)SurfaceType.Person;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Lanterna: um spot quente (3200 K) preso na câmera, desligado até pegar uma no caminhão.
            var beamGo = new GameObject("Luz da Lanterna", typeof(Light));
            beamGo.transform.SetParent(camGo.transform, false);
            beamGo.transform.localPosition = new Vector3(0.15f, -0.15f, 0.1f); // na mão, um pouco abaixo dos olhos
            var beam = beamGo.GetComponent<Light>();
            beam.type = LightType.Spot;
            beam.spotAngle = 55f;
            beam.innerSpotAngle = 25f;
            beam.range = 18f;
            beam.intensity = 6f; // forte o bastante para a casa escura: é ela que mostra o caminho
            beam.color = new Color(1f, 0.86f, 0.68f);
            beam.shadows = LightShadows.Soft;
            player.AddComponent<Hotbar>(); // até 3 espaços (teclas 1-2-3): lanterna, pilhas, bengala
            EditorTools.SetReference(player.AddComponent<Flashlight>(), "beam", beam);
            // O que está na mão, visto no boneco: o modelo da lanterna e a bengala da Cida.
            EditorTools.SetReference(player.AddComponent<HeldItems>(), "flashlightModel",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/Lanterna/SM_PRP_Lanterna.fbx"));
            player.AddComponent<FirstPersonArm>(); // o braço da lanterna na SUA tela (preso à câmera)
            player.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            EditorTools.SetReference(movement, "cameraPivot", camGo.transform);
            EditorTools.SetReference(carrier, "playerCamera", cam);
            EditorTools.SetReference(interactor, "playerCamera", cam);
            // A "roupa" (tronco e braços) ganha a cor do personagem.
            EditorTools.SetReferences(switcher, "body", new Object[]
            {
                body.transform.Find("Tronco").GetComponent<Renderer>(),
                body.transform.Find("Braço E/Braço E (malha)").GetComponent<Renderer>(),
                body.transform.Find("Braço D/Braço D (malha)").GetComponent<Renderer>(),
            });
            EditorTools.SetReference(switcher, "doll", body);
            EditorTools.SetReference(switcher, "modelRoot", modelRoot);
            // Animações de verdade (Mixamo) para os modelos Humanoid das fichas (ver AnimationBuilder).
            EditorTools.SetReference(switcher, "animations", AnimationBuilder.EnsureController());

            // --- Componentes de rede ---
            player.AddComponent<NetworkObject>(); // "este objeto existe na rede"
            var netTransform = player.AddComponent<NetworkTransform>();
            // Owner = quem controla o boneco manda a posição dele. Sem isso, só o host poderia mover os jogadores.
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;

            var netPlayer = player.AddComponent<NetworkPlayer>();
            // O CharacterSwitcher NÃO entra: ele precisa rodar em todos para pintar a cor de cada boneco.
            EditorTools.SetReferences(netPlayer, "ownerOnly", new Object[] { movement, carrier, interactor, cues, cane, vibration, whistle, fork });

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
