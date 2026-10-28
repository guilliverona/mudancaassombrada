using System.Linq;
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
    // Capítulo 1: A CASA DO ESPELHO (o Casarão Taborda, o Imitador). Cria os prefabs que a casa usa
    // e a cena. A planta é a MANSÃO DO ESPELHO V1.2 (LEVEL_DESIGN_FASE1_MANSAO.md), montada a partir
    // de tools/level/mansao_v1_2.json no MirrorHouseBuilder.Mansao.cs. Depois de criada, a cena é sua:
    // mova paredes, espelhos e móveis à vontade, e refaça o NavMesh (objeto "NavMesh" > Bake).
    //
    //   Assets/Prefabs/Casa       kit do Blender: paredes (lisa, porta, porta dupla), porta, pisos,
    //                             laje, escada, guarda-corpo, arandela, clarabóia, tábua solta, relógio
    //   Assets/Prefabs/Cenário    fixos: piano, gramofone, manequins, retratos, lustre, pia
    //   Assets/Prefabs/Espelhos   Espelho P, M, G e G Rachado (molduras + componente Mirror)
    //   Assets/Prefabs/Móveis     Penteadeira, Arara de Figurino, Caixa G
    //   Assets/Prefabs/Relíquias  Cartaz do Teatro (a pista) e o Cartaz carregável (a relíquia)
    //   Assets/Prefabs/Fantasmas  Fantasma Imitador
    //
    // Como nos outros prefabs do mundo: só são criados se ainda NÃO existem.
    //
    // A planta (grid de 2 m; +Z = fundo da casa, -Z = rua; ver as plantas no LEVEL_DESIGN_FASE1_MANSAO.md):
    //   térreo (Y 0): entrada, grande hall e escadaria (pé-direito duplo), jantar, cozinha, retratos,
    //                 música, depósito, corredores; o caminhão no pátio leste
    //   nível de cima (Y 3,6, sobre o morro): galeria dos espelhos, biblioteca, salão, camarins, câmara
    public static partial class MirrorHouseBuilder
    {
        public const string SceneName = "CasaDoEspelho";
        public const string ScenePath = EditorTools.ScenesFolder + "/" + SceneName + ".unity";

        const string WallPlainPath = "Assets/Prefabs/Casa/Parede Lisa.prefab";
        const string WallDoorPath = "Assets/Prefabs/Casa/Parede com Porta.prefab";
        const string WallDoubleDoorPath = "Assets/Prefabs/Casa/Parede com Porta Dupla.prefab";
        const string DoorPath = "Assets/Prefabs/Casa/Porta.prefab";
        const string FloorTilePath = "Assets/Prefabs/Casa/Piso Xadrez.prefab";
        const string PlankFloorPath = "Assets/Prefabs/Casa/Piso Tábuas.prefab";
        const string SlabPath = "Assets/Prefabs/Casa/Laje.prefab";
        const string StairsPath = "Assets/Prefabs/Casa/Escadaria.prefab";          // a nobre, 3,0 m
        const string ServiceStairsPath = "Assets/Prefabs/Casa/Escada de Serviço.prefab"; // 2,0 m
        const string StagePath = "Assets/Prefabs/Cenário/Palco.prefab";
        const string RailingPath = "Assets/Prefabs/Casa/Guarda-corpo.prefab";
        const string SconcePath = "Assets/Prefabs/Casa/Arandela a Gás.prefab";
        const string SkylightPath = "Assets/Prefabs/Casa/Clarabóia.prefab";
        const string LoosePlankPath = "Assets/Prefabs/Casa/Tábua Solta.prefab";
        const string MirrorSPath = "Assets/Prefabs/Espelhos/Espelho P.prefab";
        const string MirrorMPath = "Assets/Prefabs/Espelhos/Espelho M.prefab";
        const string MirrorLPath = "Assets/Prefabs/Espelhos/Espelho G.prefab";
        const string MirrorCrackedPath = "Assets/Prefabs/Espelhos/Espelho G Rachado.prefab";
        const string VanityPath = "Assets/Prefabs/Móveis/Penteadeira.prefab";
        const string RackPath = "Assets/Prefabs/Móveis/Arara de Figurino.prefab";
        const string BigBoxPath = "Assets/Prefabs/Móveis/Caixa G.prefab";
        const string SmallCarryBoxPath = "Assets/Prefabs/Móveis/Caixa de Figurinos.prefab";
        const string TourTrunkPath = "Assets/Prefabs/Móveis/Baú-camarim.prefab";
        const string CarryArmchairPath = "Assets/Prefabs/Móveis/Poltrona Coberta.prefab";
        const string ReadingTablePath = "Assets/Prefabs/Móveis/Mesa de Leitura.prefab";
        const string PosterPath = "Assets/Prefabs/Relíquias/Cartaz do Teatro.prefab";
        const string RelicPosterPath = "Assets/Prefabs/Relíquias/Cartaz do Teatro (relíquia).prefab";
        const string PianoPath = "Assets/Prefabs/Cenário/Piano de Armário.prefab";
        const string GramophonePath = "Assets/Prefabs/Cenário/Gramofone.prefab";
        const string MannequinAPath = "Assets/Prefabs/Cenário/Manequim A.prefab";
        const string MannequinBPath = "Assets/Prefabs/Cenário/Manequim B.prefab";
        const string MannequinCPath = "Assets/Prefabs/Cenário/Manequim C.prefab";
        const string PortraitPPath = "Assets/Prefabs/Cenário/Retrato P.prefab";
        const string PortraitMPath = "Assets/Prefabs/Cenário/Retrato M.prefab";
        const string PortraitGPath = "Assets/Prefabs/Cenário/Retrato G.prefab";
        const string ChandelierPath = "Assets/Prefabs/Cenário/Lustre.prefab";
        const string SinkPath = "Assets/Prefabs/Cenário/Pia da Copa.prefab";
        // Abandono (a casa ficou fechada décadas)
        const string CoveredChairPath = "Assets/Prefabs/Cenário/Poltrona Coberta.prefab";
        const string CoveredDresserPath = "Assets/Prefabs/Cenário/Cômoda Coberta.prefab";
        const string CobwebPath = "Assets/Prefabs/Cenário/Teia.prefab";
        const string RubblePath = "Assets/Prefabs/Cenário/Entulho.prefab";
        const string BoardedWindowPath = "Assets/Prefabs/Cenário/Janela com Tábuas.prefab";
        const string PeelingPaperPath = "Assets/Prefabs/Cenário/Papel Descascando.prefab";
        const string TrunkPath = "Assets/Prefabs/Cenário/Baú.prefab";
        const string PapersPath = "Assets/Prefabs/Cenário/Papéis no Chão.prefab";
        const string CandlesPath = "Assets/Prefabs/Cenário/Velas.prefab";
        const string DiningTablePath = "Assets/Prefabs/Cenário/Mesa de Jantar.prefab";
        const string DiningChairPath = "Assets/Prefabs/Cenário/Cadeira de Jantar.prefab";
        const string FallenChairPath = "Assets/Prefabs/Cenário/Cadeira Tombada.prefab";
        const string ShelfPath = "Assets/Prefabs/Cenário/Estante.prefab";
        const string SmallBoxPath = "Assets/Prefabs/Cenário/Caixote.prefab";
        // As fontes de ruído e as âncoras sonoras da Cida (lote 2)
        const string CorridorSconcePath = "Assets/Prefabs/Casa/Arandela do Corredor.prefab";
        const string RadioPath = "Assets/Prefabs/Cenário/Rádio.prefab";
        const string CidaPath = "Assets/Data/Personagens/Cida.asset";
        // A luz que a equipe precisa gerir
        const string FlashlightPath = "Assets/Prefabs/Itens/Lanterna.prefab";
        const string BatteriesPath = "Assets/Prefabs/Itens/Pilhas.prefab";
        public const string ImitatorPath = "Assets/Prefabs/Fantasmas/Fantasma Imitador.prefab";
        public const string ClockPath = "Assets/Prefabs/Casa/Relógio de Pêndulo.prefab";

        const float WallWidth = 2f, WallHeight = 3.2f, WallThickness = 0.15f; // o kit (tools/blender)
        // Portas maiores que as do Art Bible (0,90 × 2,10): o Marcelo (1,98 m, 0,90 de largura)
        // precisa passar, carregando móvel. Medidas do kit em tools/blender/build_env_kit_blockout.py.
        const float DoorWidth = 1.2f, DoorHeight = 2.4f, DoubleDoorWidth = 1.6f;
        const float FloorY = 3.6f;            // piso do nível de cima: pé-direito 3,20 + laje 0,40
        const float SlabThickness = 0.4f;
        const float StairRun = 6f;            // as escadas avançam 6 m e sobem 3,60 m (31,0°)
        const float ImitatorHeight = 2.3f;    // a ficha do Imitador (ART_BIBLE.md)
        const float Inside = WallThickness / 2 + 0.04f; // afasta da parede o que encosta nela (espelhos)
        const string ChapterPath = "Assets/Data/Capítulos/1 - A Casa do Espelho.asset";

        // Se a cena já existe, pergunta antes de apagar o trabalho feito à mão nela.
        public static bool ConfirmOverwrite()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return true;
            return EditorUtility.DisplayDialog("Recriar a cena Casa do Espelho?",
                "A cena já existe. Recriar apaga tudo que foi feito à mão nela (posições, objetos novos...).",
                "Recriar", "Manter a minha");
        }

        // ---------- Prefabs ----------

        // Como no LevelPrefabsBuilder: o Guard devolve os arquivos que saíram iguais (só números novos).
        public static void EnsurePrefabs() => StableAssets.Guard(LevelPrefabsBuilder.AllPrefabs(), BuildPrefabs);

        static void BuildPrefabs()
        {
            // Kit da casa
            LevelPrefabsBuilder.Build(WallPlainPath, false, () => KitPiece("Parede Lisa", "Environment/ParedeLisa/SM_ENV_ParedeLisa", WallPlainColliders));
            LevelPrefabsBuilder.Build(WallDoorPath, false, () => KitPiece("Parede com Porta", "Environment/ParedePorta/SM_ENV_ParedePorta", r => WallOpeningColliders(r, DoorWidth)));
            LevelPrefabsBuilder.Build(WallDoubleDoorPath, false, () => KitPiece("Parede com Porta Dupla", "Environment/ParedePortaDupla/SM_ENV_ParedePortaDupla", r => WallOpeningColliders(r, DoubleDoorWidth)));
            LevelPrefabsBuilder.Build(DoorPath, false, () => KitPiece("Porta", "Environment/Porta/SM_ENV_Porta", DoorColliders));
            LevelPrefabsBuilder.Build(FloorTilePath, false, () => KitPiece("Piso Xadrez", "Environment/PisoXadrez/SM_ENV_PisoXadrez", null));
            LevelPrefabsBuilder.Build(PlankFloorPath, false, () => KitPiece("Piso Tábuas", "Environment/PisoTabuas/SM_ENV_PisoTabuas", null));
            LevelPrefabsBuilder.Build(SlabPath, false, () => KitPiece("Laje", "Environment/Laje/SM_ENV_Laje", SlabColliders));
            LevelPrefabsBuilder.Build(StairsPath, false, () => BuildStairs("Escadaria", "Environment/Escada/SM_ENV_Escada", 3f));
            LevelPrefabsBuilder.Build(ServiceStairsPath, false, () => BuildStairs("Escada de Serviço", "Environment/EscadaServico/SM_ENV_EscadaServico", 2f));
            LevelPrefabsBuilder.Build(StagePath, false, BuildStage);
            LevelPrefabsBuilder.Build(RailingPath, false, () => KitPiece("Guarda-corpo", "Environment/GuardaCorpo/SM_ENV_GuardaCorpo",
                r => Box(r, new Vector3(0, 0.5f, 0), new Vector3(2f, 1f, 0.1f))));
            LevelPrefabsBuilder.Build(SconcePath, false, BuildSconce);
            LevelPrefabsBuilder.Build(SkylightPath, false, BuildSkylight);
            LevelPrefabsBuilder.Build(LoosePlankPath, false, () => KitPiece("Tábua Solta", "Environment/TabuaSolta/SM_ENV_TabuaSolta", null));
            LevelPrefabsBuilder.Build(ClockPath, false, BuildClock);

            // Espelhos
            LevelPrefabsBuilder.Build(MirrorSPath, false, () => BuildMirror("Espelho P", "MolduraS", 0.6f));
            LevelPrefabsBuilder.Build(MirrorMPath, false, () => BuildMirror("Espelho M", "MolduraM", 1.2f));
            LevelPrefabsBuilder.Build(MirrorLPath, false, () => BuildMirror("Espelho G", "MolduraL", 2.0f));
            LevelPrefabsBuilder.Build(MirrorCrackedPath, false, () => BuildMirror("Espelho G Rachado", "MolduraLRachada", 2.0f));

            // Cenário fixo (colisão para ninguém atravessar; o NavMesh contorna)
            LevelPrefabsBuilder.Build(PianoPath, false, BuildPiano);
            LevelPrefabsBuilder.Build(GramophonePath, false, BuildGramophone);
            LevelPrefabsBuilder.Build(CorridorSconcePath, false, BuildCorridorSconce);
            LevelPrefabsBuilder.Build(RadioPath, false, BuildRadio);
            LevelPrefabsBuilder.Build(MannequinAPath, false, () => StaticProp("Manequim A", "Props/ManequimA/SM_PRP_ManequimA", new Vector3(0.45f, 1.75f, 0.3f)));
            LevelPrefabsBuilder.Build(MannequinBPath, false, () => StaticProp("Manequim B", "Props/ManequimB/SM_PRP_ManequimB", new Vector3(0.45f, 1.75f, 0.3f)));
            LevelPrefabsBuilder.Build(MannequinCPath, false, () => StaticProp("Manequim C", "Props/ManequimC/SM_PRP_ManequimC", new Vector3(0.45f, 1.75f, 0.3f)));
            LevelPrefabsBuilder.Build(PortraitPPath, false, () => StaticProp("Retrato P", "Props/RetratoP/SM_PRP_RetratoP", Vector3.zero));
            LevelPrefabsBuilder.Build(PortraitMPath, false, () => StaticProp("Retrato M", "Props/RetratoM/SM_PRP_RetratoM", Vector3.zero));
            LevelPrefabsBuilder.Build(PortraitGPath, false, () => StaticProp("Retrato G", "Props/RetratoG/SM_PRP_RetratoG", Vector3.zero));
            LevelPrefabsBuilder.Build(ChandelierPath, false, () => StaticProp("Lustre", "Props/Lustre/SM_PRP_Lustre", Vector3.zero));
            LevelPrefabsBuilder.Build(SinkPath, false, () => StaticProp("Pia da Copa", "Props/Torneira/SM_PRP_Torneira", new Vector3(0.6f, 0.9f, 0.45f)));

            // Abandono. Só o que é grande tem colisão; o resto (teia, papel, entulho baixo) é só visual.
            LevelPrefabsBuilder.Build(CoveredChairPath, false, () => StaticProp("Poltrona Coberta", "Props/MovelCobertoA/SM_PRP_MovelCobertoA", new Vector3(0.9f, 1.0f, 0.85f)));
            LevelPrefabsBuilder.Build(CoveredDresserPath, false, () => StaticProp("Cômoda Coberta", "Props/MovelCobertoB/SM_PRP_MovelCobertoB", new Vector3(1.1f, 1.5f, 0.55f)));
            LevelPrefabsBuilder.Build(CobwebPath, false, () => StaticProp("Teia", "Props/Teia/SM_PRP_Teia", Vector3.zero));
            LevelPrefabsBuilder.Build(RubblePath, false, () => StaticProp("Entulho", "Props/Entulho/SM_PRP_Entulho", Vector3.zero));
            LevelPrefabsBuilder.Build(BoardedWindowPath, false, BuildBoardedWindow);
            LevelPrefabsBuilder.Build(PeelingPaperPath, false, () => StaticProp("Papel Descascando", "Props/PapelDescascando/SM_PRP_PapelDescascando", Vector3.zero));
            LevelPrefabsBuilder.Build(TrunkPath, false, () => StaticProp("Baú", "Props/Bau/SM_PRP_Bau", new Vector3(0.9f, 0.7f, 0.5f)));
            LevelPrefabsBuilder.Build(PapersPath, false, () => StaticProp("Papéis no Chão", "Props/PapeisChao/SM_PRP_PapeisChao", Vector3.zero));
            LevelPrefabsBuilder.Build(CandlesPath, false, BuildCandles);
            LevelPrefabsBuilder.Build(DiningTablePath, false, () => StaticProp("Mesa de Jantar", "Props/MesaLeitura/SM_PRP_MesaLeitura", new Vector3(2.0f, 0.78f, 0.9f)));
            LevelPrefabsBuilder.Build(DiningChairPath, false, () => StaticProp("Cadeira de Jantar", "Props/Cadeira/SM_PRP_Cadeira", new Vector3(0.5f, 0.9f, 0.5f)));
            LevelPrefabsBuilder.Build(FallenChairPath, false, BuildFallenChair);
            LevelPrefabsBuilder.Build(ShelfPath, false, () => StaticProp("Estante", "Props/EstanteBiblioteca/SM_PRP_EstanteBiblioteca", new Vector3(1.0f, 2.4f, 0.4f)));
            LevelPrefabsBuilder.Build(SmallBoxPath, false, () => StaticProp("Caixote", "Props/CaixaP/SM_PRP_CaixaP", new Vector3(0.4f, 0.4f, 0.4f)));

            // Lanterna e pilhas
            LevelPrefabsBuilder.Build(FlashlightPath, false, () => BuildPickup("Lanterna", "Props/Lanterna/SM_PRP_Lanterna", true));
            LevelPrefabsBuilder.Build(BatteriesPath, false, () => BuildPickup("Pilhas", "Props/Pilhas/SM_PRP_Pilhas", false));

            // Móveis e relíquias
            LevelPrefabsBuilder.Build(VanityPath, false, () => CarryProp("Penteadeira", "Props/Penteadeira/SM_PRP_Penteadeira", new Vector3(1.2f, 1.6f, 0.5f), 2.5f));
            LevelPrefabsBuilder.Build(RackPath, false, () => CarryProp("Arara de Figurino", "Props/AraraFigurino/SM_PRP_AraraFigurino", new Vector3(1.4f, 1.7f, 0.5f), 1.5f));
            LevelPrefabsBuilder.Build(BigBoxPath, false, () => CarryProp("Caixa G", "Props/CaixaG/SM_PRP_CaixaG", new Vector3(0.6f, 0.6f, 0.6f), 2f));
            LevelPrefabsBuilder.Build(SmallCarryBoxPath, false, () => CarryProp("Caixa de Figurinos", "Props/CaixaP/SM_PRP_CaixaP", new Vector3(0.4f, 0.4f, 0.4f), 1f));
            LevelPrefabsBuilder.Build(TourTrunkPath, false, () => CarryProp("Baú-camarim de Turnê", "Props/Bau/SM_PRP_Bau", new Vector3(0.9f, 0.7f, 0.5f), 2f));
            LevelPrefabsBuilder.Build(CarryArmchairPath, false, () => CarryProp("Poltrona Coberta", "Props/MovelCobertoA/SM_PRP_MovelCobertoA", new Vector3(0.9f, 1.0f, 0.85f), 2f));
            LevelPrefabsBuilder.Build(ReadingTablePath, false, () => CarryProp("Mesa de Leitura", "Props/MesaLeitura/SM_PRP_MesaLeitura", new Vector3(2.0f, 0.78f, 0.9f), 2.5f));
            LevelPrefabsBuilder.Build(PosterPath, false, BuildPoster);
            LevelPrefabsBuilder.Build(RelicPosterPath, false, BuildRelicPoster);
            LevelPrefabsBuilder.Build(ImitatorPath, false, BuildImitator);

            BuildMansionPrefabs();

            UpgradeClock();
            UpgradeMirrors();
            UpgradeKitColliders();
            UpgradeSurfaceTags();
            LevelPrefabsBuilder.UpgradeGhost(ImitatorPath, breathes: false); // Imitador antigo ganha o que faltar
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

        // Parede com um vão de porta no meio: três caixas (esquerda, direita e em cima do vão).
        // A caixa de cima começa 10 cm acima do vão do modelo: no 1º andar o arredondamento do NavMesh
        // comia a folga e fechava as portas para o fantasma (que precisa de 2,00 m livres).
        const float DoorClearance = DoorHeight + 0.1f;

        static void WallOpeningColliders(GameObject root, float opening)
        {
            float side = (WallWidth - opening) / 2;
            Box(root, new Vector3(-(opening + side) / 2, WallHeight / 2, 0), new Vector3(side, WallHeight, WallThickness));
            Box(root, new Vector3((opening + side) / 2, WallHeight / 2, 0), new Vector3(side, WallHeight, WallThickness));
            Box(root, new Vector3(0, (WallHeight + DoorClearance) / 2, 0), new Vector3(opening, WallHeight - DoorClearance, WallThickness));
        }

        // Laje de 0,40 m com a origem no topo: a colisão desce a espessura inteira.
        static void SlabColliders(GameObject root) =>
            Box(root, new Vector3(0, -SlabThickness / 2, 0), new Vector3(2f, SlabThickness, 2f));

        // O kit muda de medida (portas de 1,20 × 2,40; depois pé-direito de 3,20 e laje de 0,40 da
        // Mansão): os prefabs feitos antes ganham as colisões novas, sem mexer no resto do prefab.
        // Compara com as caixas que o construtor faria hoje; só regrava o que está diferente.
        static void UpgradeKitColliders()
        {
            var kit = new (string path, System.Action<GameObject> colliders)[]
            {
                (WallPlainPath, WallPlainColliders),
                (WallDoorPath, r => WallOpeningColliders(r, DoorWidth)),
                (WallDoubleDoorPath, r => WallOpeningColliders(r, DoubleDoorWidth)),
                (DoorPath, DoorColliders),
                (SlabPath, SlabColliders),
            };
            foreach (var (path, colliders) in kit)
            {
                if (Load(path) == null) continue;
                var expected = new GameObject("medida");
                colliders(expected);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var before = root.GetComponents<BoxCollider>();
                    var want = expected.GetComponents<BoxCollider>();
                    bool upToDate = before.Length == want.Length && want.All(w => before.Any(b =>
                        Vector3.Distance(b.center, w.center) < 0.001f && Vector3.Distance(b.size, w.size) < 0.001f));
                    if (upToDate) continue;
                    foreach (var b in before) Object.DestroyImmediate(b);
                    colliders(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Colisão do kit atualizada (pé-direito {WallHeight}, laje {SlabThickness}, porta {DoorWidth} × {DoorHeight}): {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    Object.DestroyImmediate(expected);
                }
            }
        }

        // O piano do hall: fixo (230 kg, fica na fase) e TOCÁVEL. É objeto de rede porque o som
        // sai do host; o SoundVoice diz qual arquivo a "música" dele toca.
        // Também é a fonte de ruído R2: começa a tocar SOZINHO quando um móvel carregado passa perto
        // (o acorde azedo a cada ~7 s) e só a Cida o abafa ("Afinar Piano").
        static GameObject BuildPiano()
        {
            var root = StaticProp("Piano", "Props/PianoArmario/SM_PRP_PianoArmario", new Vector3(1.5f, 1.3f, 0.6f));
            root.AddComponent<NetworkObject>();
            root.AddComponent<PlayableInstrument>();
            root.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Music, sound = "piano_tocado" } };
            AddTuning(root);
            Noise(root, radius: 12f, loop: "", repeat: "piano_desafinado_acorde", silence: "piano_abafado", startsOn: false, wakeDistance: 6f);
            return root;
        }

        // Gramofone travado (R1, Sala de Ensaio): a mesma meia valsa sem parar. Afinado, para, e a
        // âncora da sala vira a cortina de veludo mexendo (um som calmo no lugar do ruído).
        static GameObject BuildGramophone()
        {
            var root = StaticProp("Gramofone", "Props/Gramofone/SM_PRP_Gramofone", new Vector3(0.5f, 0.8f, 0.5f));
            root.AddComponent<NetworkObject>();
            AddTuning(root);
            var curtain = new GameObject("Cortina de Veludo");
            curtain.transform.SetParent(root.transform, false);
            curtain.transform.localPosition = new Vector3(0, 1.2f, 0);
            var ambient = Ambient(curtain, "cortina_veludo", spatial: true, volume: 1f);
            var noise = Noise(root, radius: 10f, loop: "gramofone_travado", repeat: "", silence: "gramofone_para", startsOn: true, wakeDistance: 0f);
            EditorTools.SetReference(noise, "replaceWith", ambient);
            return root;
        }

        // A arandela do corredor (R3): assobia (gás) e falha. Afinar = fechar o gás: a casa fica
        // quieta... e o corredor fica ESCURO (o trade-off da §11.3).
        static GameObject BuildCorridorSconce()
        {
            var root = KitPiece("Arandela do Corredor", "Environment/ArandelaGas/SM_ENV_ArandelaGas",
                r => Box(r, new Vector3(0, 0.3f, 0.1f), new Vector3(0.4f, 0.6f, 0.4f))); // a mira precisa acertar
            root.AddComponent<NetworkObject>();
            var light = AddLight(root, new Vector3(0, 0.32f, 0.25f), new Color(1f, 0.72f, 0.45f), 4.5f, 0.9f);
            var flicker = new SerializedObject(light.AddComponent<LightFlicker>());
            flicker.FindProperty("broken").boolValue = true;
            flicker.ApplyModifiedPropertiesWithoutUndo();
            AddTuning(root);
            var noise = Noise(root, radius: 14f, loop: "arandela_gas", repeat: "", silence: "arandela_fecha", startsOn: true, wakeDistance: 0f);
            var so = new SerializedObject(noise);
            so.FindProperty("lightsOffWhenSilenced").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // O rádio do Seu Valdemar no alpendre: chiado constante (a âncora do alpendre).
        static GameObject BuildRadio()
        {
            var root = StaticProp("Rádio", "Props/Radio/SM_PRP_Radio", Vector3.zero);
            var hiss = new GameObject("Chiado");
            hiss.transform.SetParent(root.transform, false);
            hiss.transform.localPosition = Vector3.up * 0.2f;
            Ambient(hiss, "radio_chiado", spatial: true, volume: 1f);
            return root;
        }

        // "Afinar" (só a Cida, segurando 2 s): o Interactable que cala a fonte de ruído.
        static void AddTuning(GameObject root)
        {
            var so = new SerializedObject(root.AddComponent<Interactable>());
            so.FindProperty("actionName").stringValue = "Afinar";
            so.FindProperty("holdSeconds").floatValue = 2f;
            so.FindProperty("hideFromOthers").boolValue = true;
            var onlyFor = so.FindProperty("onlyFor");
            onlyFor.arraySize = 1;
            onlyFor.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CidaPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static NoiseSource Noise(GameObject root, float radius, string loop, string repeat, string silence, bool startsOn, float wakeDistance)
        {
            var noise = root.AddComponent<NoiseSource>();
            var so = new SerializedObject(noise);
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("loopSound").stringValue = loop;
            so.FindProperty("repeatSound").stringValue = repeat;
            so.FindProperty("repeatEvery").floatValue = 16f; // espaçado: um acorde sozinho assusta, um a cada 7 s irrita
            so.FindProperty("silenceSound").stringValue = silence;
            so.FindProperty("startsOn").boolValue = startsOn;
            so.FindProperty("wakeDistance").floatValue = wakeDistance;
            so.ApplyModifiedPropertiesWithoutUndo();
            return noise;
        }

        // Do que cada coisa é feita, para o diapasão da Cida (SurfaceTag). Acrescenta só onde falta.
        static void UpgradeSurfaceTags()
        {
            var tags = new (string path, SurfaceType type)[]
            {
                (MirrorSPath, SurfaceType.Glass), (MirrorMPath, SurfaceType.Glass), (MirrorLPath, SurfaceType.Glass),
                (MirrorCrackedPath, SurfaceType.Glass),
                (MannequinAPath, SurfaceType.Wood), (MannequinBPath, SurfaceType.Wood), (MannequinCPath, SurfaceType.Wood),
                (ImitatorPath, SurfaceType.Copy),
                (RackPath, SurfaceType.Metal), (ChandelierPath, SurfaceType.Metal), (SconcePath, SurfaceType.Metal),
                (CorridorSconcePath, SurfaceType.Metal), (ClockPath, SurfaceType.Wood),
                (CoveredChairPath, SurfaceType.Fabric), (CoveredDresserPath, SurfaceType.Fabric),
                (VanityPath, SurfaceType.Wood), (PianoPath, SurfaceType.Wood), (GramophonePath, SurfaceType.Metal),
                (LevelPrefabsBuilder.WardrobePath, SurfaceType.Wood), (LevelPrefabsBuilder.ChairPath, SurfaceType.Wood),
                (DiningTablePath, SurfaceType.Wood), (DiningChairPath, SurfaceType.Wood), (TrunkPath, SurfaceType.Wood),
                // Mansão
                (TourTrunkPath, SurfaceType.Wood), (ReadingTablePath, SurfaceType.Wood), (CarryArmchairPath, SurfaceType.Fabric),
                (StagePath, SurfaceType.Wood), (ShelfPath, SurfaceType.Wood), (BooksPath, SurfaceType.Fabric),
            };
            foreach (var (path, type) in tags)
            {
                if (Load(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.TryGetComponent(out SurfaceTag _)) continue;
                    var so = new SerializedObject(root.AddComponent<SurfaceTag>());
                    so.FindProperty("type").enumValueIndex = (int)type;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        // Janela fechada com tábuas, e um fio de luar frio entrando pelas frestas.
        static GameObject BuildBoardedWindow()
        {
            var root = StaticProp("Janela com Tábuas", "Props/JanelaTabuada/SM_PRP_JanelaTabuada", Vector3.zero);
            AddLight(root, new Vector3(0, 0.7f, 0.35f), new Color(0.55f, 0.65f, 1f), 3.5f, 0.35f);
            return root;
        }

        // Velas derretidas no chão, com uma luz pequena e trêmula.
        static GameObject BuildCandles()
        {
            var root = StaticProp("Velas", "Props/Velas/SM_PRP_Velas", Vector3.zero);
            AddLight(root, new Vector3(0, 0.3f, 0), new Color(1f, 0.7f, 0.4f), 2.5f, 0.7f).AddComponent<LightFlicker>();
            return root;
        }

        // Cadeira caída de lado (o mesmo modelo da cadeira, deitado).
        static GameObject BuildFallenChair()
        {
            var root = StaticProp("Cadeira Tombada", "Props/Cadeira/SM_PRP_Cadeira", Vector3.zero);
            var model = root.transform.Find("Modelo");
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(0, 0, 90);
                model.localPosition = new Vector3(0.45f, 0.25f, 0);
            }
            Box(root, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.5f));
            return root;
        }

        // Lanterna (no caminhão) ou pilhas (pela casa): objeto de rede que some ao ser pego.
        // A caixa de colisão é maior que o modelo, para a mira acertar fácil no escuro.
        static GameObject BuildPickup(string name, string art, bool givesFlashlight)
        {
            var root = KitPiece(name, art, r => Box(r, new Vector3(0, 0.1f, 0), new Vector3(0.35f, 0.25f, 0.35f)));
            root.AddComponent<NetworkObject>();
            var so = new SerializedObject(root.AddComponent<FlashlightPickup>());
            so.FindProperty("givesFlashlight").boolValue = givesFlashlight;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // A folha da porta tem a origem na dobradiça e corre para +X.
        static void DoorColliders(GameObject root) =>
            Box(root, new Vector3(DoorWidth / 2, DoorHeight / 2, 0), new Vector3(DoorWidth, DoorHeight, 0.05f));

        // Uma escada: o modelo tem degraus, mas quem anda pisa numa RAMPA invisível (31,0°, abaixo
        // dos 45° que o jogador e o NavMesh sobem). Assim o fantasma também sobe.
        // Debaixo da rampa, três blocos fecham o vão (ninguém entra embaixo da escada).
        // A escadaria nobre tem 3,0 m de largura; as de serviço, 2,0 m (o kit gera as duas).
        static GameObject BuildStairs(string name, string art, float width)
        {
            var root = KitPiece(name, art, null);
            float angle = Mathf.Atan2(FloorY, StairRun) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(FloorY * FloorY + StairRun * StairRun);
            var ramp = new GameObject("Rampa");
            ramp.transform.SetParent(root.transform, false);
            ramp.transform.localRotation = Quaternion.Euler(-angle, 0, 0);
            // O meio da rampa fica no meio da linha dos degraus; a caixa desce meia espessura.
            // Grossa (1 m) e não uma tábua: sem vão embaixo, a lateral baixa do pé vira degrau
            // que o NavMesh sobe (com a rampa fina, o pé da escada ficava ilhado).
            const float thickness = 1f;
            Vector3 up = ramp.transform.localRotation * Vector3.up;
            ramp.transform.localPosition = new Vector3(0, FloorY / 2, StairRun / 2) - up * (thickness / 2);
            Box(ramp, Vector3.zero, new Vector3(width, thickness, length));
            // Os blocos ficam um pouco abaixo da rampa (5 cm): quem pisa, pisa na rampa.
            foreach (float part in new[] { 0.32f, 0.64f, 0.84f })
            {
                float from = part * StairRun, height = from * FloorY / StairRun - 0.05f;
                Box(root, new Vector3(0, height / 2, (from + StairRun) / 2), new Vector3(width, height, StairRun - from));
            }
            return root;
        }

        // O palco do Salão de Baile: tablado de 0,40 m (sobe-se andando: é menos que o degrau do
        // jogador) e as cortinas da boca de cena, que não têm colisão.
        static GameObject BuildStage()
        {
            var root = KitPiece("Palco", "Environment/Palco/SM_ENV_Palco", r => Box(r, new Vector3(0, 0.2f, 0.8f), new Vector3(8f, 0.4f, 1.6f)));
            return root;
        }

        // Arandela a gás com a luz quente (3200 K) de verdade, fraca e trêmula: é só uma ilha de luz
        // no escuro (a casa é escura de propósito: a lanterna precisa fazer diferença).
        static GameObject BuildSconce()
        {
            var root = KitPiece("Arandela a Gás", "Environment/ArandelaGas/SM_ENV_ArandelaGas", null);
            AddLight(root, new Vector3(0, 0.32f, 0.25f), new Color(1f, 0.72f, 0.45f), 4.5f, 0.9f).AddComponent<LightFlicker>();
            return root;
        }

        // Clarabóia da galeria: o luar entra frio por ela.
        static GameObject BuildSkylight()
        {
            var root = KitPiece("Clarabóia", "Environment/ClaraboiaGaleria/SM_ENV_ClaraboiaGaleria", null);
            AddLight(root, new Vector3(0, -0.8f, 0), new Color(0.6f, 0.7f, 1f), 7f, 0.8f);
            return root;
        }

        static GameObject AddLight(GameObject root, Vector3 position, Color color, float range, float intensity)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None; // muitas luzes pequenas: sombra só da lua e das lanternas
            return go;
        }

        // Cenário fixo: modelo + caixa de colisão (Vector3.zero = sem colisão, ex.: retrato na parede).
        static GameObject StaticProp(string name, string art, Vector3 size) =>
            KitPiece(name, art, size == Vector3.zero ? null : r => Box(r, new Vector3(0, size.y / 2, 0), size));

        // Móvel carregável: a origem fica no MEIO da caixa (como o armário e a cadeira), o modelo desce
        // meia altura. Em rede como os outros móveis; o NavMeshObstacle abre o buraco no mapa do fantasma.
        static GameObject CarryProp(string name, string art, Vector3 size, float weight)
        {
            var obj = new GameObject(name);
            Box(obj, Vector3.zero, size);
            obj.AddComponent<NetworkObject>();
            var furniture = obj.AddComponent<Furniture>(); // adiciona o Rigidbody junto
            obj.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            obj.AddComponent<NetworkRigidbody>();
            obj.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var obstacle = obj.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = size;
            EditorTools.SetFloat(furniture, "weight", weight);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/{art}.fbx");
            if (model != null) LevelPrefabsBuilder.AddModel(obj, model, new Vector3(0, -size.y / 2, 0));
            else Debug.LogWarning($"{name}: modelo Assets/Art/{art}.fbx não encontrado (rode tools/blender).");
            return obj;
        }

        // Espelho: a moldura do kit + o componente Mirror (a "porta" do Imitador).
        // O vidro olha para +Z (a seta azul): encoste as costas na parede.
        static GameObject BuildMirror(string name, string art, float height)
        {
            var root = KitPiece(name, $"Environment/{art}/SM_ENV_{art}", r =>
                Box(r, new Vector3(0, height / 2, 0), new Vector3(height * 0.62f, height, 0.06f)));
            root.AddComponent<Mirror>();
            AddReflection(root);
            return root;
        }

        // Reflexo de verdade (MirrorReflection): troca o material do vidro da moldura pelo do
        // shader Espelho e diz ao componente qual renderer/material é o vidro.
        static bool AddReflection(GameObject root)
        {
            if (root.GetComponent<MirrorReflection>() != null) return false;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains("Espelho")) continue;
                    mats[i] = GlassMaterial();
                    r.sharedMaterials = mats;
                    root.AddComponent<MirrorReflection>().Configure(r, i);
                    return true;
                }
            }
            Debug.LogWarning($"{root.name}: não achei o vidro (material MAT_ENV_Espelho) na moldura.");
            return false;
        }

        const string GlassMaterialPath = "Assets/Prefabs/Materiais/Espelho Vidro.mat";

        // Um material só para todos os espelhos: a textura de cada um vai por MaterialPropertyBlock.
        static Material GlassMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
            if (mat != null) return mat;
            EditorTools.EnsureFolder("Assets/Prefabs/Materiais");
            mat = new Material(Shader.Find("MudancaAssombrada/Espelho"));
            AssetDatabase.CreateAsset(mat, GlassMaterialPath);
            return mat;
        }

        // Os espelhos de antes não tinham reflexo; acrescenta sem mexer no resto do prefab.
        public static void UpgradeMirrors()
        {
            foreach (var path in new[] { MirrorSPath, MirrorMPath, MirrorLPath })
            {
                if (Load(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!AddReflection(root)) continue;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Espelho agora tem reflexo (MirrorReflection): {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
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

        // O cartaz como RELÍQUIA (LEVEL_DESIGN_FASE1.md §11): um móvel leve que vai para o caminhão
        // (é a etapa 2 da fase). O marcador no save ao entregar fica para o lote de acabamento.
        static GameObject BuildRelicPoster()
        {
            var obj = CarryProp("Cartaz do Teatro (relíquia)", "Props/CartazTeatro/SM_PRP_CartazTeatro", new Vector3(0.7f, 1f, 0.12f), 2f);
            obj.AddComponent<StoryFlag>();
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

        // Relógio de pêndulo (0,55 × 0,32 × 2,1 m): cenário fixo, não carregável. O tique-taque sai
        // dele (som 3D na altura do mostrador), então a Cida acha o relógio pelo som. É também o
        // metrônomo da casa (HouseClock): a respiração e os sussurros entram entre as batidas.
        static GameObject BuildClock()
        {
            var root = KitPiece("Relógio de Pêndulo", "Props/RelogioPendulo/SM_PRP_RelogioPendulo",
                r => Box(r, new Vector3(0, 1.08f, 0), new Vector3(0.55f, 2.16f, 0.33f)));
            root.AddComponent<NavMeshObstacle>().carving = true; // o fantasma desvia mesmo sem refazer o NavMesh
            AddTicking(root);
            return root;
        }

        static void AddTicking(GameObject clock)
        {
            var tick = new GameObject("Tique-taque");
            tick.transform.SetParent(clock.transform, false);
            tick.transform.localPosition = new Vector3(0, 1.7f, 0);
            tick.AddComponent<HouseClock>();
        }

        // O prefab de antes tocava um loop (AmbientSound); troca pelo metrônomo, sem mexer no resto.
        public static void UpgradeClock()
        {
            if (Load(ClockPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(ClockPath);
            try
            {
                if (root.GetComponentInChildren<HouseClock>() != null) return;
                var old = root.transform.Find("Tique-taque");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                AddTicking(root);
                PrefabUtility.SaveAsPrefabAsset(root, ClockPath);
                Debug.Log($"Relógio agora é o metrônomo da casa (HouseClock): {ClockPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static GameObject Load(string path) => LevelPrefabsBuilder.Load(path);

        // ---------- Cena ----------

        // A casa por cima de uma fase vazia do kit (LevelSceneTools): os sistemas, a luz, a câmera
        // de espera, o chão, os marcadores e o NavMesh vêm de lá.
        public static void BuildScene()
        {
            EnsurePrefabs();
            LevelSceneTools.NewLevelScene(ScenePath, BuildMansion, FineNavMesh);
            LinkChapter();
            EditorTools.UpdateBuildSettings();
            Debug.Log("Cena Casa do Espelho criada! Abra a cena Menu, aperte Play e escolha o capítulo 1.");
        }

        // O clima da casa (AmbientSound 2D, igual em todo lugar) e o DIRETOR DE SOM com a trilha da
        // fase (quando a música toca e quando a casa se cala). O relógio traz o próprio som.
        // Públicos para dar para acrescentar numa cena já feita à mão, sem recriá-la.
        public static void AddAmbience(Transform house)
        {
            var sounds = new GameObject("Sons").transform;
            sounds.SetParent(house, false);
            var weather = new GameObject("Clima da Casa");
            weather.transform.SetParent(sounds, false);
            Ambient(weather, "casa_noite", spatial: false, volume: 0.5f);

            var director = sounds.gameObject.AddComponent<SoundDirector>();
            var so = new SerializedObject(director);
            so.FindProperty("track").objectReferenceValue = EnsureSoundtrack();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        const string SoundtrackPath = "Assets/Data/Trilhas/Casa do Espelho.asset";

        // A trilha (Dark Piano Migraine, 4:30) cortada em trechos. Os cortes caem nos vales medidos
        // na faixa (volume por 0,5 s): cada trecho entra e sai respirando. Só cria se faltar: depois
        // o asset é de quem ajusta de ouvido.
        //   0:02-0:53 denso e grave .......... final (a corrida)
        //   0:56-1:25 sem grave, médio, baixo  calma (o vale que deixa ouvir a casa)
        //   1:26-1:59 grave e pesado ......... pânico
        //   2:02-2:19 agudo e rarefeito ...... calma
        //   2:23-2:49 cresce .................. crescendo (o vácuo que vira medo)
        //   2:56-3:19 o ponto mais alto ...... pânico
        //   3:20-3:59 médio ................... final
        //   4:06-4:19 3,5 s de nada, um golpe (+22 dB) e o grave arrastado: a aparição dos 45 s
        static Soundtrack EnsureSoundtrack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Soundtrack>(SoundtrackPath);
            if (existing != null) return existing;
            var track = ScriptableObject.CreateInstance<Soundtrack>();
            track.sound = "trilha_casa_espelho";
            Soundtrack.Section S(string name, Soundtrack.Mood mood, float start, float end) =>
                new() { name = name, mood = mood, start = start, end = end };
            track.sections = new[]
            {
                S("Vale grave", Soundtrack.Mood.Calm, 56f, 85f),
                S("Vale agudo", Soundtrack.Mood.Calm, 122.5f, 139.5f),
                S("Crescendo", Soundtrack.Mood.Build, 143f, 169.5f),
                S("Pressão", Soundtrack.Mood.Panic, 86f, 119.5f),
                S("Clímax", Soundtrack.Mood.Panic, 176f, 199.5f),
                S("Aparição", Soundtrack.Mood.Appearance, 246f, 259.5f),
                S("Abertura densa", Soundtrack.Mood.Finale, 1.5f, 53.5f),
                S("Tensão média", Soundtrack.Mood.Finale, 200f, 239.5f),
            };
            EditorTools.EnsureFolder("Assets/Data/Trilhas");
            AssetDatabase.CreateAsset(track, SoundtrackPath);
            return track;
        }

        // Relógio de pêndulo: o metrônomo da casa. A posição vem da planta (no hall da Mansão).
        public static void AddClock(Transform house, Vector3 position, float yaw)
        {
            Piece(ClockPath, house, position, yaw);
        }

        static AmbientSound Ambient(GameObject go, string sound, bool spatial, float volume)
        {
            var ambient = go.AddComponent<AmbientSound>();
            var so = new SerializedObject(ambient);
            so.FindProperty("sound").stringValue = sound;
            so.FindProperty("spatial").boolValue = spatial;
            so.FindProperty("volume").floatValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ambient;
        }

        // Uma ÂNCORA SONORA (§7.4): um som constante e próprio de cada cômodo, para a Cida saber onde
        // está sem gastar bengala (e o clima de cada sala para todo mundo).
        static void Anchor(Transform parent, string name, string sound, Vector3 position)
        {
            var go = new GameObject($"Âncora: {name}");
            go.transform.SetParent(parent);
            go.transform.position = position;
            Ambient(go, sound, spatial: true, volume: 1f);
        }

        static void Creak(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject($"Range: {name}");
            go.transform.SetParent(parent);
            go.transform.position = position;
            var so = new SerializedObject(go.AddComponent<CreakyFloor>());
            so.FindProperty("size").vector3Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A ficha do capítulo aponta para esta cena (é assim que ele sai de "Em produção") e recebe
        // os textos e a noite do planejamento (LEVEL_DESIGN_FASE1_MANSAO.md §10; o texto vem do
        // LEVEL_DESIGN_FASE1.md §3.4). Os textos só são trocados enquanto ainda forem um rascunho
        // conhecido (o antigo ou o da casa de antes): depois de reescritos no Inspector, ficam.
        const float NightSeconds = 330f; // D6: a mansão é 3,8× a casa de antes

        static void LinkChapter()
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);
            if (chapter == null) return;
            chapter.sceneName = SceneName;
            if (Mathf.Approximately(chapter.nightDuration, 300f) || Mathf.Approximately(chapter.nightDuration, 180f))
                chapter.nightDuration = NightSeconds;
            if (chapter.openingNote.StartsWith("Casa de uma família de atores") || chapter.openingNote.Contains("no camarim do fundo"))
            {
                chapter.openingNote =
                    "TRANSPORTES DESCANSO ETERNO LTDA.\n" +
                    "ORDEM DE SERVIÇO Nº 014 — CASARÃO TABORDA\n\n" +
                    "Casa de família de teatro. Cuidado com os espelhos: são muitos e são velhos.\n" +
                    "Se alguém da equipe pedir ajuda com um móvel, confiram se a pessoa respira.\n\n" +
                    "Tem um cartaz antigo em algum lugar desta casa. Esse vem junto.\n" +
                    "Não está na lista. Vem junto.\n\n" +
                    "Não arranhem o piano.\n" +
                    "— V.";
                chapter.ratingComments = new[]
                {
                    Comment("Cida", "A senhora ouviu o que ninguém ouviu. Já trabalhei com gente assim. — V."),
                    Comment("Tiago", "O senhor leu uma coisa que não era para ler. Tudo bem. — V."),
                    Comment("Marcelo", "Obrigado por não deixar ninguém segurar a outra ponta sozinho. — V."),
                };
            }
            EditorUtility.SetDirty(chapter);
            AssetDatabase.SaveAssets();
        }

        static ChapterDefinition.CharacterComment Comment(string character, string text) => new()
        {
            character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"Assets/Data/Personagens/{character}.asset"),
            comment = text,
        };

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
