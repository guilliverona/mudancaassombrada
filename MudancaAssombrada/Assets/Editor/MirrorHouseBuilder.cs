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
    // e a cena, seguindo o LEVEL_DESIGN_FASE1.md (planta da §6, cômodos da §7, móveis da §11).
    // Depois de criada, a cena é sua: mova paredes, espelhos e móveis à vontade, e refaça o NavMesh
    // (objeto "NavMesh" > Bake).
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
    // A planta (grid de 2 m; +Z = fundo da casa, -Z = rua; ver o desenho no LEVEL_DESIGN_FASE1.md):
    //   térreo (Y 0):  Sala de Ensaio | Hall dos Retratos (pé-direito duplo ao sul) | Sala de Jantar
    //                  Copa           | Hall norte (sob a galeria: piano, relógio)  |
    //                  Corredor dos Espelhos (16 m) atravessando o fundo
    //   1º andar (Y 3,10, só Z 0..6): Camarim Grande | Galeria / Corredor dos Camarins | Camarim do Suplente
    //   A escada sobe pela parede leste do hall (Z -5 → 0); o caminhão fica na rua, em Z = -12.
    public static class MirrorHouseBuilder
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
        const string StairsPath = "Assets/Prefabs/Casa/Escada.prefab";
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

        const float WallWidth = 2f, WallHeight = 2.8f, WallThickness = 0.15f; // o kit (tools/blender)
        // Portas maiores que as do Art Bible (0,90 × 2,10): o Marcelo (1,98 m, 0,90 de largura)
        // precisa passar, carregando móvel. Medidas do kit em tools/blender/build_env_kit_blockout.py.
        const float DoorWidth = 1.2f, DoorHeight = 2.4f, DoubleDoorWidth = 1.6f;
        const float FloorY = 3.1f;            // piso do 1º andar: pé-direito 2,80 + laje 0,30
        const float StairRun = 5f;            // a escada avança 5 m e sobe 3,10 m (31,8°)
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
            LevelPrefabsBuilder.Build(SlabPath, false, () => KitPiece("Laje", "Environment/Laje/SM_ENV_Laje",
                r => Box(r, new Vector3(0, -0.15f, 0), new Vector3(2f, 0.3f, 2f))));
            LevelPrefabsBuilder.Build(StairsPath, false, BuildStairs);
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
            LevelPrefabsBuilder.Build(PosterPath, false, BuildPoster);
            LevelPrefabsBuilder.Build(RelicPosterPath, false, BuildRelicPoster);
            LevelPrefabsBuilder.Build(ImitatorPath, false, BuildImitator);

            UpgradeClock();
            UpgradeMirrors();
            UpgradeDoorColliders();
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

        // As portas cresceram (1,20 × 2,40) e a colisão de cima vai até 2,50: paredes com porta e a
        // folha da porta feitas antes ganham as colisões novas, sem mexer no resto do prefab.
        static void UpgradeDoorColliders()
        {
            var doors = new (string path, System.Action<GameObject> colliders)[]
            {
                (WallDoorPath, r => WallOpeningColliders(r, DoorWidth)),
                (WallDoubleDoorPath, r => WallOpeningColliders(r, DoubleDoorWidth)),
                (DoorPath, DoorColliders),
            };
            foreach (var (path, colliders) in doors)
            {
                if (Load(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var before = root.GetComponents<BoxCollider>();
                    bool isWall = path != DoorPath;
                    float opening = path == WallDoorPath ? DoorWidth : DoubleDoorWidth;
                    bool upToDate = isWall ? before.Any(b => Mathf.Approximately(b.center.y, (WallHeight + DoorClearance) / 2) &&
                                                             Mathf.Approximately(b.size.x, opening))
                                           : before.Any(b => Mathf.Approximately(b.size.y, DoorHeight));
                    if (upToDate) continue;
                    foreach (var b in before) Object.DestroyImmediate(b);
                    colliders(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Porta maior (1,20 × 2,40, colisão até 2,50): {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
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

        // A escada: o modelo tem degraus, mas quem anda pisa numa RAMPA invisível (31,8°, abaixo
        // dos 45° que o jogador e o NavMesh sobem). Assim o fantasma também sobe, sem NavMeshLink.
        // Debaixo da rampa, três blocos fecham o vão (ninguém entra embaixo da escada).
        static GameObject BuildStairs()
        {
            var root = KitPiece("Escada", "Environment/Escada/SM_ENV_Escada", null);
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
            Box(ramp, Vector3.zero, new Vector3(1.6f, thickness, length));
            foreach (var (from, height) in new[] { (1.6f, 0.95f), (3.2f, 1.95f), (4.2f, 2.55f) })
                Box(root, new Vector3(0, height / 2, (from + StairRun) / 2), new Vector3(1.6f, height, StairRun - from));
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
            LevelSceneTools.NewLevelScene(ScenePath, BuildHouse);
            LinkChapter();
            EditorTools.UpdateBuildSettings();
            Debug.Log("Cena Casa do Espelho criada! Abra a cena Menu, aperte Play e escolha o capítulo 1.");
        }

        // O que cada módulo de 2 m de uma linha de paredes é.
        enum W { Lisa, Porta, Dupla, Vao }

        static void BuildHouse()
        {
            var house = new GameObject("Casa").transform;
            BuildFloors(Group(house, "Pisos e Lajes"));
            BuildWalls(Group(house, "Paredes"));
            BuildStairsAndGallery(Group(house, "Escada e Galeria"));
            var dressing = Group(house, "Cenário");
            BuildDressing(dressing);
            BuildLightItems(dressing);
            DarkenNight();
            BuildMirrors();
            var furniture = PlaceFurniture();
            BuildObjectives(furniture);
            BuildRoomsAndSpawns();

            // O Imitador começa no espelho E2 (hall, parede oeste). Aos 45 s ele atravessa o hall até
            // o "palco" (o meio do hall, de frente para a escada e para a porta), faz a reverência e volta.
            var imitator = Place(ImitatorPath, new Vector3(-2 + Inside + 0.8f, ImitatorHeight / 2f, -1.5f), 90, null);
            var stage = new GameObject("Palco do Imitador (aparição)").transform;
            stage.SetParent(house);
            stage.position = new Vector3(1.4f, 0, -3f);
            var so = new SerializedObject(imitator.GetComponent<GhostImitator>());
            so.FindProperty("stage").objectReferenceValue = stage;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Caminhão na rua, com a cabine para longe da casa
            Place(LevelPrefabsBuilder.TruckPath, new Vector3(0, 0, -12f), 180, null);

            AddAmbience(house);
            AddClock(house);
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            return t;
        }

        // Pisos: xadrez no térreo nobre (hall, ensaio, jantar), tábuas na copa, no corredor, no
        // alpendre e no 1º andar. Lajes: o piso do 1º andar (com colisão: é onde se pisa lá em cima)
        // e o forro de tudo. Pé-direito: 2,80 no térreo, 5,90 no hall sul (duplo).
        static void BuildFloors(Transform parent)
        {
            for (int x = -7; x <= 7; x += 2)
                for (int z = -5; z <= 5; z += 2)
                {
                    bool planks = z >= 5 || (x <= -3 && z >= 1); // corredor e copa
                    Piece(planks ? PlankFloorPath : FloorTilePath, parent, new Vector3(x, 0, z), 0);

                    bool hallSouth = x >= -1 && x <= 3 && z < 0;
                    if (z > 0)
                    {
                        Piece(SlabPath, parent, new Vector3(x, FloorY, z), 0);          // piso do 1º andar
                        Piece(PlankFloorPath, parent, new Vector3(x, FloorY, z), 0);
                        if (!(x == 1 && z == 3)) Piece(SlabPath, parent, new Vector3(x, FloorY * 2, z), 0); // forro de cima
                    }
                    else if (hallSouth) Piece(SlabPath, parent, new Vector3(x, FloorY * 2, z), 0); // forro do hall (5,90)
                    else Piece(SlabPath, parent, new Vector3(x, FloorY, z), 0);           // forro do térreo
                }
            foreach (int x in new[] { -1, 1 }) Piece(PlankFloorPath, parent, new Vector3(x, 0, -7), 0); // alpendre
            Piece(SkylightPath, parent, new Vector3(1, FloorY * 2, 3), 0);
        }

        // Todas as paredes, módulo a módulo. A planta das portas é a §6.2 do LEVEL_DESIGN_FASE1.md,
        // com um ajuste: P2 (hall → ensaio) e P10 (→ camarim do suplente) são DUPLAS. O fantasma só
        // passa em vãos de pelo menos 1 m (raio 0,5 m do NavMesh); com porta simples ali, a Sala de
        // Ensaio, a Copa e o Camarim do Suplente virariam ilhas onde o Imitador não chega a pé.
        static void BuildWalls(Transform p)
        {
            var L = W.Lisa; var S = W.Porta; var D = W.Dupla;
            // --- térreo (Y 0) ---
            WallRow(p, true, -6, -8, 0, 0, L, L, L, L, D, L, L, L);    // frente (P1: entrada dupla)
            WallRow(p, true, 6, -8, 0, 180, L, L, L, L, L, L, L, L);   // fundo do Corredor dos Espelhos
            WallRow(p, false, -8, -6, 0, 90, L, L, L, L, L, L);        // oeste
            WallRow(p, false, 8, -6, 0, -90, L, L, L, L, L, L);        // leste
            WallRow(p, false, -2, -6, 0, 90, L, D, L, L, L);           // hall | ensaio e copa (P2)
            WallRow(p, true, 0, -8, 0, 0, L, D, L);                    // ensaio | copa (P3)
            WallRow(p, true, 4, -8, 0, 0, L, S, L, L, D, L, S, L);     // cômodos | corredor (P4, P5, P6)
            WallRow(p, false, 4, -6, 0, -90, L, L, L, D, L);           // hall | jantar (P7)
            // --- hall sul: a parte de cima do pé-direito duplo ---
            WallRow(p, false, -2, -6, FloorY, 90, L, L, L);
            WallRow(p, true, -6, -2, FloorY, 0, L, L, L);
            WallRow(p, false, 4, -6, FloorY, -90, L, L, L);
            // --- 1º andar (Y 3,10) ---
            WallRow(p, true, 6, -8, FloorY, 180, L, L, L, L, L, L, L, S); // fundo (P12: porta de cenário)
            WallRow(p, false, -8, 0, FloorY, 90, L, L, L);
            WallRow(p, false, 8, 0, FloorY, -90, L, L, L);
            WallRow(p, true, 0, -8, FloorY, 0, L, L, L);               // frente do Camarim Grande
            WallRow(p, true, 0, 4, FloorY, 0, L, L);                   // frente do Camarim do Suplente
            WallRow(p, false, -2, 0, FloorY, -90, D, L, S);            // camarim grande | galeria e corredor (P8, P11)
            WallRow(p, true, 4, -2, FloorY, 0, L, D, L);               // galeria | corredor dos camarins (P9)
            WallRow(p, false, 4, 0, FloorY, 90, L, L, D);              // corredor | camarim do suplente (P10)

            // P12: a porta do fundo do camarim do suplente não abre (é cenário).
            Piece(DoorPath, p, new Vector3(7 + DoorWidth / 2, FloorY, 6 - 0.05f), 180);
        }

        // Uma linha de paredes no grid: 'alongX' = corre em X (senão, em Z), na coordenada fixa 'line',
        // começando em 'from' e indo de 2 em 2 m. yaw: para onde olha o lado do papel de parede.
        static void WallRow(Transform parent, bool alongX, float line, float from, float y, float yaw, params W[] modules)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == W.Vao) continue;
                float c = from + 1 + 2 * i;
                var pos = alongX ? new Vector3(c, y, line) : new Vector3(line, y, c);
                string prefab = modules[i] == W.Lisa ? WallPlainPath : modules[i] == W.Porta ? WallDoorPath : WallDoubleDoorPath;
                Piece(prefab, parent, pos, yaw);
            }
        }

        // A escada sobe pela parede leste do hall (X 2,4..4,0, Z -5 → 0) até a galeria, que debruça
        // sobre o hall com um peitoril (guarda-corpo) aberto só onde a escada chega.
        static void BuildStairsAndGallery(Transform parent)
        {
            Piece(StairsPath, parent, new Vector3(3.2f, 0, -StairRun), 0);

            // O NavMesh trata a lateral da rampa como parede e afasta 0,5 m (o raio do fantasma) dos
            // dois lados: o pé da escada vira uma ilha. O link liga o chão do hall à rampa, um pouco
            // acima do pé, para o Imitador conseguir subir e descer (os jogadores andam pela física).
            var link = new GameObject("Link do Pé da Escada").AddComponent<NavMeshLink>();
            link.transform.SetParent(parent);
            link.startPoint = new Vector3(1.6f, 0f, -4.6f);
            link.endPoint = new Vector3(3.2f, 0.62f, -4f);
            link.width = 1f;
            link.bidirectional = true;

            // Corrimão no lado de dentro da escada (X 2,4), inclinado junto com ela. Começa 1 m acima
            // do pé: o patamar entre a parede da frente e a escada (0,9 m) é estreito demais para
            // entrar de lado carregando um armário (e o NavMesh, com raio 0,5 m, fecharia ali).
            float angle = Mathf.Atan2(FloorY, StairRun);
            float length = Mathf.Sqrt(FloorY * FloorY + StairRun * StairRun);
            const float openFoot = 1f;
            float piece = (length - openFoot) / 3;
            for (int i = 0; i < 3; i++)
            {
                float t = openFoot + (i + 0.5f) * piece;
                var rail = Place(RailingPath, new Vector3(2.4f, Mathf.Sin(angle) * t, -StairRun + Mathf.Cos(angle) * t),
                                 0, "Corrimão da Escada");
                rail.transform.rotation = Quaternion.Euler(0, -90, angle * Mathf.Rad2Deg);
                rail.transform.localScale = new Vector3(piece / 2f, 1, 1);
                rail.transform.SetParent(parent);
            }

            // Peitoril da galeria (Z 0, de X -2 até 2,4).
            foreach (float x in new[] { -1f, 1f }) Piece(RailingPath, parent, new Vector3(x, FloorY, 0), 0);
            var end = Place(RailingPath, new Vector3(2.2f, FloorY, 0), 0, "Guarda-corpo");
            end.transform.localScale = new Vector3(0.2f, 1, 1);
            end.transform.SetParent(parent);
        }

        // Cenário fixo, cômodo a cômodo (§7 e §11.2): o que dá personalidade e as âncoras da Cida.
        static void BuildDressing(Transform p)
        {
            // Hall norte: o piano encostado na parede oeste (objeto de rede: fica na raiz da cena)
            Place(PianoPath, new Vector3(-2 + Inside + 0.3f, 0, 2f), 90, "Piano");
            // Sala de Ensaio: o gramofone no canto (fonte de ruído R1, objeto de rede: na raiz),
            // manequins de figurino junto à parede do fundo
            Place(GramophonePath, new Vector3(-7.3f, 0, -5.3f), 45, "Gramofone");
            Piece(MannequinAPath, p, new Vector3(-7.3f, 0, -0.6f), 180);
            Piece(MannequinBPath, p, new Vector3(-6.6f, 0, -0.6f), 180);
            Piece(MannequinAPath, p, new Vector3(-3.6f, 0, -0.6f), 180);
            Piece(MannequinBPath, p, new Vector3(-2.9f, 0, -0.6f), 180);
            // Corredor dos Espelhos: três manequins, um com o colete da empresa
            Piece(MannequinBPath, p, new Vector3(-7.2f, 0, 4.5f), 0);
            Piece(MannequinCPath, p, new Vector3(3.0f, 0, 4.5f), 0);
            // ...e o do meio TOMBA SOZINHO aos 2:40 (o "vento"), ao comprido do corredor (§8, MEC-10)
            var falling = Place(MannequinAPath, new Vector3(-2.6f, 0, 4.5f), 0, "Manequim que Tomba");
            falling.transform.SetParent(p);
            falling.AddComponent<TimedTopple>();
            falling.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Impact, sound = "manequim_tomba" } };
            // Copa: a pia com a torneira que pinga
            Piece(SinkPath, p, new Vector3(-8 + Inside + 0.2f, 0, 2f), 90);
            // Sala de Jantar: o lustre de cristal pendurado no forro
            Piece(ChandelierPath, p, new Vector3(6f, WallHeight - 0.7f, -1f), 0);

            // Hall dos Retratos: a galeria da Dona Herondina na parte alta das paredes (um virado para a parede)
            Piece(PortraitGPath, p, new Vector3(-2 + Inside, 3.3f, -5f), 90);
            Piece(PortraitMPath, p, new Vector3(-2 + Inside, 3.5f, -3f), 90);
            Piece(PortraitGPath, p, new Vector3(-2 + Inside, 3.3f, -1f), 90);
            Piece(PortraitMPath, p, new Vector3(-1f, 3.5f, -6 + Inside), 0);
            Piece(PortraitPPath, p, new Vector3(3f, 3.7f, -6 + Inside), 0);
            Piece(PortraitMPath, p, new Vector3(4 - Inside, 3.5f, -4f), -90);
            Piece(PortraitGPath, p, new Vector3(4 - Inside - 0.05f, 3.3f, -1.6f), 90); // o retrato VAZIO, virado para a parede

            // Luz: arandelas a gás (hall, corredor, camarim grande); o resto fica no escuro (§7).
            // A do corredor é a fonte de ruído R3: assobia, falha, e afinada apaga (objeto de rede: na raiz).
            Piece(SconcePath, p, new Vector3(-1f, 2.2f, -6 + Inside), 0);
            Piece(SconcePath, p, new Vector3(3.4f, 2.2f, -6 + Inside), 0);
            Place(CorridorSconcePath, new Vector3(-3f, 2.2f, 6 - Inside), 180, "Arandela do Corredor");
            Piece(SconcePath, p, new Vector3(-2 - Inside, FloorY + 2.2f, 3f), -90);

            BuildAbandonment(p);
            BuildSoundMap(p);

            // Corredor dos Camarins: as tábuas soltas (o piso que range vem no lote 2)
            Piece(LoosePlankPath, p, new Vector3(0f, FloorY, 5f), 90);
            Piece(LoosePlankPath, p, new Vector3(2.2f, FloorY, 4.8f), 90);
        }

        // A casa ficou fechada décadas: lençóis nos móveis, teias nos cantos, caliça, papel de parede
        // caindo, janelas pregadas, a mesa de jantar ainda posta (para 9, com uma cadeira caída) e
        // velas derretidas na frente dos espelhos. Nada disso entra no caminho dos móveis: o que
        // fica no chão das rotas não tem colisão (as rotas são medidas depois de gerar a cena).
        static void BuildAbandonment(Transform p)
        {
            float top = WallHeight - 0.02f, upTop = FloorY + WallHeight - 0.02f;

            // --- Sala de Ensaio: a plateia abandonada ---
            Piece(CoveredChairPath, p, new Vector3(-4.6f, 0, -5.3f), 0);
            Piece(TrunkPath, p, new Vector3(-3.1f, 0, -5.45f), 0);
            Piece(BoardedWindowPath, p, new Vector3(-8 + Inside, 1.0f, -5f), 90);
            Piece(CobwebPath, p, new Vector3(-2 - Inside, top, -6 + Inside), 0);
            Piece(PapersPath, p, new Vector3(-6.2f, 0, -2.2f), 30);

            // --- Copa: estante encostada, caixotes empilhados, a janela pregada ---
            Piece(ShelfPath, p, new Vector3(-2 - Inside - 0.2f, 0, 2.6f), -90);
            Piece(SmallBoxPath, p, new Vector3(-7.4f, 0, 0.55f), 10);
            Piece(SmallBoxPath, p, new Vector3(-7.35f, 0.4f, 0.6f), -15);
            Piece(SmallBoxPath, p, new Vector3(-6.9f, 0, 0.5f), 35);
            Piece(BoardedWindowPath, p, new Vector3(-8 + Inside, 1.2f, 3.3f), 90);
            Piece(CobwebPath, p, new Vector3(-8 + Inside, top, 4 - Inside), 180);

            // --- Hall dos Retratos ---
            Piece(CoveredDresserPath, p, new Vector3(-1.45f, 0, 3.3f), 90);
            Piece(PapersPath, p, new Vector3(0.4f, 0, -2.8f), 0);
            Piece(PeelingPaperPath, p, new Vector3(2.7f, 0.9f, -6 + Inside), 0);
            Piece(CandlesPath, p, new Vector3(-1.55f, 0, -0.45f), 0);                 // na frente do E2
            Piece(CobwebPath, p, new Vector3(4 - Inside, FloorY + top, -6 + Inside), 0);

            // --- Sala de Jantar: a mesa posta para 9 que ninguém tirou ---
            Piece(DiningTablePath, p, new Vector3(6f, 0, -2.5f), 90);
            Piece(DiningTablePath, p, new Vector3(6f, 0, -0.5f), 90);
            foreach (float z in new[] { -3f, 0f })
            {
                Piece(DiningChairPath, p, new Vector3(5.25f, 0, z), 90);
                Piece(DiningChairPath, p, new Vector3(6.75f, 0, z), -90);
            }
            Piece(DiningChairPath, p, new Vector3(6.75f, 0, -1.5f), -90);
            Piece(FallenChairPath, p, new Vector3(4.9f, 0, -1.6f), 20);               // a cadeira que caiu
            Piece(DiningChairPath, p, new Vector3(6f, 0, -4.1f), 0);                  // a cabeceira
            Piece(BoardedWindowPath, p, new Vector3(8 - Inside, 1.0f, -4f), -90);
            Piece(BoardedWindowPath, p, new Vector3(8 - Inside, 1.0f, 2.5f), -90);
            Piece(CobwebPath, p, new Vector3(8 - Inside, top, 4 - Inside), -90);
            Piece(PapersPath, p, new Vector3(5.2f, 0, 2.6f), 60);

            // --- Corredor dos Espelhos: o caminho mais longo, com caliça caída do forro ---
            Piece(RubblePath, p, new Vector3(7.2f, 0, 4.55f), 15);
            Piece(PeelingPaperPath, p, new Vector3(-1.6f, 0.9f, 4 + Inside), 180);
            Piece(PeelingPaperPath, p, new Vector3(3.4f, 0.9f, 4 + Inside), 180);

            // --- Camarim Grande ---
            Piece(CoveredDresserPath, p, new Vector3(-4.3f, FloorY, 0.4f), 0);
            Piece(TrunkPath, p, new Vector3(-7.4f, FloorY, 5.6f), 0);
            Piece(BoardedWindowPath, p, new Vector3(-8 + Inside, FloorY + 1.0f, 1.0f), 90);
            Piece(PeelingPaperPath, p, new Vector3(-8 + Inside, FloorY + 0.9f, 5.0f), 90);

            // --- Galeria e Corredor dos Camarins ---
            Piece(PapersPath, p, new Vector3(-0.6f, FloorY, 2.4f), 0);
            Piece(RubblePath, p, new Vector3(-1.2f, FloorY, 5.2f), -10);
            Piece(CobwebPath, p, new Vector3(-2 + Inside, upTop, 6 - Inside), 180);

            // --- Camarim do Suplente: o mais escuro, com as velas na frente do espelho rachado ---
            Piece(CandlesPath, p, new Vector3(7.0f, FloorY, 1.9f), 0);
            Piece(BoardedWindowPath, p, new Vector3(6f, FloorY + 1.0f, Inside), 0);
            Piece(CobwebPath, p, new Vector3(8 - Inside, upTop, 6 - Inside), -90);
            Piece(PapersPath, p, new Vector3(5.6f, FloorY, 4.4f), 120);
        }

        // As 2 lanternas no caminhão (num caixote, perto da lâmpada da caçamba) e 5 pacotes de
        // pilhas pela casa. A bateria cheia dura 100 s; a noite, 300 s: não dá luz para todo mundo
        // o tempo todo, e é isso que a equipe precisa gerir. Objetos de rede: ficam na RAIZ da cena.
        static void BuildLightItems(Transform dressing)
        {
            Piece(SmallBoxPath, dressing, new Vector3(1.9f, 0, -9.4f), 0);
            var lamp = new GameObject("Lâmpada do Caminhão");
            lamp.transform.SetParent(dressing);
            AddLight(lamp, new Vector3(1.9f, 1.2f, -9.4f), new Color(1f, 0.75f, 0.45f), 3.5f, 1.2f);
            Place(FlashlightPath, new Vector3(1.78f, 0.4f, -9.4f), 0, "Lanterna 1");
            Place(FlashlightPath, new Vector3(2.02f, 0.4f, -9.45f), 15, "Lanterna 2");

            Place(BatteriesPath, new Vector3(-7.55f, 0.89f, 2.2f), 30, "Pilhas da Copa");                 // na pia
            Place(BatteriesPath, new Vector3(-3.1f, 0.7f, -5.45f), -20, "Pilhas da Sala de Ensaio");      // no baú
            Place(BatteriesPath, new Vector3(6.3f, 0.79f, 0.3f), 70, "Pilhas da Sala de Jantar");         // na ponta da mesa
            Place(BatteriesPath, new Vector3(-3.3f, FloorY, 3.4f), 0, "Pilhas do Camarim Grande");
            Place(BatteriesPath, new Vector3(2.9f, FloorY, 5.3f), 45, "Pilhas do Corredor dos Camarins");
        }

        // A noite nesta casa é mais escura que o padrão (ambiente e lua mais fracos), para a lanterna
        // fazer diferença. Muda só a cópia da cena (os Sistemas da Fase das outras fases não mudam).
        static void DarkenNight()
        {
            // A cena já nasce escura (a mesma noite que a NightLighting aplica em Play), para o
            // editor mostrar o que o jogador vai ver.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.012f, 0.014f, 0.026f);
            RenderSettings.reflectionIntensity = 0.2f;
            var night = Object.FindAnyObjectByType<NightLighting>();
            if (night == null) return;
            var so = new SerializedObject(night);
            so.FindProperty("nightAmbient").colorValue = new Color(0.012f, 0.014f, 0.026f);
            so.FindProperty("moonIntensity").floatValue = 0.12f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Os 9 espelhos da §7.3. E1 e E2 (parede oeste do hall) são os "heróis": a escada fica na
        // parede da frente e quem sobe ou desce aparece neles. E7 é o espelho rachado do suplente.
        static void BuildMirrors()
        {
            Place(MirrorLPath, new Vector3(-2 + Inside, 0, -4.5f), 90, "E1 Espelho do Hall (herói)");
            Place(MirrorLPath, new Vector3(-2 + Inside, 0, -1.5f), 90, "E2 Espelho do Hall (herói)");
            // E3 fica à esquerda da entrada: do lado direito ele fecharia o pé da escada.
            Place(MirrorMPath, new Vector3(-1.2f, 0.5f, -6 + Inside), 0, "E3 Espelho da Entrada");
            Place(MirrorLPath, new Vector3(-8 + Inside, 0, -3f), 90, "E4 Espelho de Ensaio");
            Place(MirrorMPath, new Vector3(-6f, 0.5f, 6 - Inside), 180, "E5a Corredor");
            Place(MirrorLPath, new Vector3(0f, 0, 6 - Inside), 180, "E5b Corredor");
            Place(MirrorMPath, new Vector3(6f, 0.5f, 6 - Inside), 180, "E5c Corredor");
            Place(MirrorLPath, new Vector3(-8 + Inside, FloorY, 3f), 90, "E6 Espelho do Camarim Grande");
            Place(MirrorCrackedPath, new Vector3(8 - Inside, FloorY, 3f), -90, "E7 Espelho Rachado do Suplente");
        }

        // Os 8 móveis da §11.2, já separados pelas 3 etapas. Objetos de rede ficam na RAIZ da cena.
        // (O armário de figurino tem 1,5 m no prefab do protótipo; a ficha do Art Bible diz 2,0 m.)
        static Furniture[][] PlaceFurniture()
        {
            Furniture F(string path, Vector3 pos, float yaw, string name) => Place(path, pos, yaw, name).GetComponent<Furniture>();

            var ground = new[]
            {
                F(LevelPrefabsBuilder.ChairPath, new Vector3(-5.5f, 0.3f, -3.5f), 0, "Cadeira de Plateia A"),
                F(LevelPrefabsBuilder.ChairPath, new Vector3(-4.3f, 0.3f, -3.5f), 0, "Cadeira de Plateia B"),
                // (1,6; -0,8) e não (1,5; -4,0) como no documento: lá ela fechava o pé da escada.
                F(LevelPrefabsBuilder.BoxPath, new Vector3(1.6f, 0.25f, -0.8f), 0, "Caixa M"),
                F(BigBoxPath, new Vector3(-4.5f, 0.3f, 2f), 0, "Caixa G"),
            };
            var dressingRooms = new[]
            {
                F(RackPath, new Vector3(-6.5f, FloorY + 0.85f, 1.1f), 90, "Arara de Figurino"),
                F(VanityPath, new Vector3(-4f, FloorY + 0.8f, 4.5f), 180, "Penteadeira"),
                F(LevelPrefabsBuilder.WardrobePath, new Vector3(-6.5f, FloorY + 0.75f, 4.8f), 180, "Armário de Figurino"),
            };
            var relic = new[]
            {
                F(RelicPosterPath, new Vector3(7.5f, FloorY + 0.5f, 4.6f), -90, "Cartaz do Teatro"),
            };
            return new[] { ground, dressingRooms, relic };
        }

        // As 3 etapas da §11.1 (no lugar do "entregar todos" que a fase vazia traz).
        // Acha a pasta pelos objetivos que já estão nela: pelo nome não dá, a HUD também tem um "Objetivos".
        static void BuildObjectives(Furniture[][] stages)
        {
            var old = Object.FindObjectsByType<DeliverFurnitureObjective>();
            var root = old.Length > 0 ? old[0].transform.parent : new GameObject("Objetivos").transform;
            foreach (var o in old) Object.DestroyImmediate(o.gameObject);

            string[] labels = { "Tirar os móveis do térreo", "Esvaziar os camarins", "O cartaz do camarim do fundo" };
            for (int stage = 0; stage < stages.Length; stage++)
            {
                var go = new GameObject($"Etapa {stage}: {labels[stage]}");
                go.transform.SetParent(root);
                var objective = go.AddComponent<DeliverFurnitureObjective>();
                var so = new SerializedObject(objective);
                so.FindProperty("label").stringValue = labels[stage];
                so.FindProperty("stage").intValue = stage;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorTools.SetReferences(objective, "targets", stages[stage]);
            }
        }

        // Cômodos com nome (para os avisos: "Tiago caiu no Camarim do Suplente!") e o nascimento no alpendre.
        static void BuildRoomsAndSpawns()
        {
            var markers = GameObject.Find("Marcadores").transform;
            float up = FloorY + WallHeight / 2;
            LevelSceneTools.RoomMarker(markers, "Alpendre", new Vector3(0, 1.4f, -7), new Vector3(4, 2.8f, 2));
            LevelSceneTools.RoomMarker(markers, "Hall dos Retratos", new Vector3(1, 2.95f, -3), new Vector3(6, 5.9f, 6));
            LevelSceneTools.RoomMarker(markers, "Hall dos Retratos (norte)", new Vector3(1, 1.4f, 2), new Vector3(6, 2.8f, 4));
            LevelSceneTools.RoomMarker(markers, "Sala de Ensaio", new Vector3(-5, 1.4f, -3), new Vector3(6, 2.8f, 6));
            LevelSceneTools.RoomMarker(markers, "Copa", new Vector3(-5, 1.4f, 2), new Vector3(6, 2.8f, 4));
            LevelSceneTools.RoomMarker(markers, "Sala de Jantar", new Vector3(6, 1.4f, -1), new Vector3(4, 2.8f, 10));
            LevelSceneTools.RoomMarker(markers, "Corredor dos Espelhos", new Vector3(0, 1.4f, 5), new Vector3(16, 2.8f, 2));
            LevelSceneTools.RoomMarker(markers, "Galeria", new Vector3(1, up, 2), new Vector3(6, 2.8f, 4));
            LevelSceneTools.RoomMarker(markers, "Camarim Grande", new Vector3(-5, up, 3), new Vector3(6, 2.8f, 6));
            LevelSceneTools.RoomMarker(markers, "Corredor dos Camarins", new Vector3(1, up, 5), new Vector3(6, 2.8f, 2));
            LevelSceneTools.RoomMarker(markers, "Camarim do Suplente", new Vector3(6, up, 3), new Vector3(4, 2.8f, 6));

            // A fase vazia nasce com 3 marcadores na frente; aqui eles vão para o alpendre, virados para a casa.
            var spawns = markers.GetComponentsInChildren<SpawnPoint>();
            for (int i = 0; i < spawns.Length; i++)
            {
                spawns[i].transform.position = new Vector3(-1 + i, 1f, -7f);
                spawns[i].transform.rotation = Quaternion.identity;
            }
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

        // Relógio de pêndulo no hall norte, encostado na parede leste e olhando para o hall.
        public static void AddClock(Transform house)
        {
            Piece(ClockPath, house, new Vector3(4 - WallThickness / 2 - 0.17f, 0, 3.2f), -90);
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

        // As âncoras e o piso que range (lote 2). O relógio (hall), o gramofone/cortina (ensaio), o
        // piano (hall norte) e a arandela (corredor) já trazem o próprio som. O Camarim do Suplente
        // fica SEM âncora: o único lugar da casa onde a Cida perde a referência.
        static void BuildSoundMap(Transform p)
        {
            var anchors = Group(p, "Âncoras Sonoras");
            Place(RadioPath, new Vector3(1.5f, 0f, -7.6f), 200, "Rádio do Seu Valdemar").transform.SetParent(anchors);
            Anchor(anchors, "Torneira da Copa", "torneira_pingando", new Vector3(-7.6f, 1.0f, 2f));
            Anchor(anchors, "Lustre da Sala de Jantar", "lustre_tilintando", new Vector3(6f, 2.4f, -1f));
            Anchor(anchors, "Vento na Clarabóia", "vento_janelas", new Vector3(1f, FloorY * 2 - 0.5f, 3f));
            Anchor(anchors, "Cabides do Camarim Grande", "cabides_metal", new Vector3(-6.5f, FloorY + 1.6f, 1.1f));

            // Piso que range: o Corredor dos Camarins inteiro e três tábuas soltas na galeria.
            var creaks = Group(p, "Piso que Range");
            Creak(creaks, "Corredor dos Camarins", new Vector3(1f, FloorY, 5f), new Vector3(6f, 1.5f, 2f));
            foreach (var (x, z) in new[] { (-1.2f, 1.6f), (0.6f, 2.8f), (2.2f, 1.2f) })
            {
                Piece(LoosePlankPath, creaks, new Vector3(x, FloorY, z), 90);
                Creak(creaks, "Tábua da Galeria", new Vector3(x, FloorY, z), new Vector3(2f, 1.5f, 0.6f));
            }
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
        // os textos e a noite do LEVEL_DESIGN_FASE1.md (§3.4, §3.5, §5). Os textos só são trocados
        // enquanto ainda forem o rascunho antigo: depois de reescritos no Inspector, ficam.
        static void LinkChapter()
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);
            if (chapter == null) return;
            chapter.sceneName = SceneName;
            if (chapter.openingNote.StartsWith("Casa de uma família de atores"))
            {
                chapter.nightDuration = 300f;
                chapter.openingNote =
                    "TRANSPORTES DESCANSO ETERNO LTDA.\n" +
                    "ORDEM DE SERVIÇO Nº 014 — CASARÃO TABORDA\n\n" +
                    "Casa de família de teatro. Cuidado com os espelhos: são muitos e são velhos.\n" +
                    "Se alguém da equipe pedir ajuda com um móvel, confiram se a pessoa respira.\n\n" +
                    "Tem um cartaz antigo no camarim do fundo. Esse vem junto.\n" +
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
