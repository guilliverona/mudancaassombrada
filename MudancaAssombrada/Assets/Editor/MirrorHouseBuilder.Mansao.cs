using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // A MANSÃO DO ESPELHO V1.2 (LEVEL_DESIGN_FASE1_MANSAO.md): a planta da Fase 1 montada a partir
    // de tools/level/mansao_v1_2.json. Quem decide a planta é o script Python (mansao_v1_2.py): ele
    // confere a geometria e já entrega as paredes módulo a módulo, os pisos, as lajes, as escadas,
    // as luzes, as pilhas e os móveis. Aqui só se coloca cada peça do kit no lugar.
    //
    // A casa fica na ENCOSTA: o térreo em Y 0 e os fundos (Z >= 7) em cima do morro, em Y 3,6.
    // O hall e a escadaria são um volume só, de 6,8 m, e a escadaria nobre sobe até a galeria.
    //
    // Mudou a planta? python tools/level/mansao_v1_2.py --json, e recrie a cena.
    public static partial class MirrorHouseBuilder
    {
        // A planta como o Python a grava (os nomes dos campos são os do JSON).
        [System.Serializable] partial class Plan
        {
            public float nivelDeCima, alturaParede, morroZ;
            public PlanRoom[] comodos;
            public PlanWall[] paredes;
            public PlanTile[] pisos, forros;
            public PlanStair[] escadas;
            public PlanLight[] luzes;
            public PlanBattery[] pilhas;
            public PlanItem[] moveis;
            public PlanPoint[] nascimento;
            public PlanPose caminhao, relogio;
            public PlanMirror[] espelhos;
            public PlanImitator imitador;
            public PlanNoise[] ruido;
            public PlanAnchor[] ancoras;
            public PlanCreak[] range;
            public PlanDecor[] cenario;
        }
        [System.Serializable] class PlanMirror { public string id, tamanho, tipo; public int nivel; public float x, z, giro; }
        [System.Serializable] class PlanImitator { public string espelho, saidaDaAparicao; public float palcoX, palcoZ; }
        [System.Serializable] class PlanNoise { public string prefab; public int nivel; public float x, z, giro; }
        [System.Serializable] class PlanAnchor { public string nome, som; public int nivel; public float x, altura, z; }
        [System.Serializable] class PlanCreak { public string nome; public int nivel; public float x, z, largura, profundidade; }
        [System.Serializable] class PlanDecor { public string prefab; public int nivel; public float x, z, giro, altura; public bool colide; }
        [System.Serializable] class PlanRoom { public string id, nome; public int nivel; public float x0, x1, z0, z1; public bool duplo, exterior, plataforma; }
        [System.Serializable] class PlanWall { public string tipo, dono; public float x, y, z, giro, largura, escalaY; }
        [System.Serializable] class PlanTile { public string tipo; public float x, y, z; }
        [System.Serializable] class PlanStair { public string nome; public float x, z, largura, avanco; }
        [System.Serializable] class PlanLight { public int nivel; public float x, z; public string tipo, nome; }
        [System.Serializable] class PlanBattery { public int nivel; public float x, z, altura; public string tipo; }
        [System.Serializable] class PlanItem { public string nome, prefab; public int nivel; public float x, z, giro; public int etapa; }
        [System.Serializable] class PlanPoint { public float x, z; }
        [System.Serializable] class PlanPose { public float x, z, giro; }

        static string PlanFile => Path.GetFullPath(Path.Combine(Application.dataPath, "../../tools/level/mansao_v1_2.json"));

        static Plan LoadPlan()
        {
            if (!File.Exists(PlanFile))
                throw new FileNotFoundException($"Planta não encontrada: {PlanFile}. Rode: python tools/level/mansao_v1_2.py --json");
            return JsonUtility.FromJson<Plan>(File.ReadAllText(PlanFile));
        }

        static float FloorOf(int level) => level == 0 ? 0f : FloorY;

        static void BuildMansion()
        {
            var plan = LoadPlan();
            var house = new GameObject("Casa").transform;
            BuildShell(plan, Group(house, "Paredes"), Group(house, "Pisos e Lajes"));
            BuildTerrain(plan, house);
            BuildStairsFromPlan(plan, Group(house, "Escadas"));
            var dressing = Group(house, "Cenário");
            Piece(StagePath, dressing, new Vector3(-1f, FloorY, 17.4f), 0); // o palco do Salão, encostado no fundo
            BuildPlanLights(plan, Group(house, "Luzes"));
            BuildLightItemsFromPlan(plan, dressing);
            DarkenNight();
            var stages = PlaceFurnitureFromPlan(plan);
            BuildObjectives(stages);
            BuildRoomsAndSpawnsFromPlan(plan);

            // Etapa 2: os espelhos, o Imitador, o ruído, as âncoras, o piso que range e o abandono.
            var mirrors = BuildMirrorsFromPlan(plan, stages);
            BuildImitatorFromPlan(plan, mirrors, house);
            BuildSoundFromPlan(plan, dressing);
            BuildDressingFromPlan(plan, dressing);

            // Etapa 3: a pianola, a casa que reage, as teias de vão, os achados de época e a luz que se carrega.
            BuildAtmosphereFromPlan(plan);

            var truck = plan.caminhao;
            Place(LevelPrefabsBuilder.TruckPath, new Vector3(truck.x, 0, truck.z), truck.giro, null);
            AddAmbience(house);
            AddClock(house, new Vector3(plan.relogio.x, 0, plan.relogio.z), plan.relogio.giro);
        }

        // O NavMesh afasta 0,5 m (o raio do fantasma) de cada lado de uma porta simples de 1,20 m:
        // sobram 20 cm de passagem, e na resolução padrão (17 cm) isso fecha. Com 5 cm a passagem
        // aparece, e a Sala dos Retratos e a Câmara (que só têm portas simples) não viram ilhas.
        static void FineNavMesh(Unity.AI.Navigation.NavMeshSurface surface)
        {
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.05f;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            return t;
        }

        // ---------- Paredes, pisos e lajes ----------

        // As paredes vêm prontas da planta: cada módulo diz o tipo (lisa, porta, dupla), onde fica,
        // para onde olha o papel de parede (giro) e a escala (meio módulo; a fileira de cima de um
        // cômodo de pé-direito duplo é esticada até 6,8 m).
        static void BuildShell(Plan plan, Transform walls, Transform floors)
        {
            foreach (var w in plan.paredes)
            {
                string path = w.tipo == "porta" ? WallDoorPath : w.tipo == "dupla" ? WallDoubleDoorPath : WallPlainPath;
                var go = Place(path, new Vector3(w.x, w.y, w.z), w.giro, null);
                go.transform.SetParent(walls);
                go.transform.localScale = new Vector3(w.largura / WallWidth, w.escalaY, 1f);
            }
            foreach (var t in plan.pisos)
            {
                string path = t.tipo == "xadrez" ? FloorTilePath : t.tipo == "laje" ? SlabPath : PlankFloorPath;
                Piece(path, floors, new Vector3(t.x, t.y, t.z), 0);
            }
            foreach (var t in plan.forros) Piece(SlabPath, floors, new Vector3(t.x, t.y, t.z), 0);

            // O forro do patamar é a clarabóia (o luar cai em quem sobe); dos lados, laje comum.
            float top = 2 * WallHeight + 2 * SlabThickness;
            Piece(SkylightPath, floors, new Vector3(-1f, top, 6f), 0);
            Piece(SkylightPath, floors, new Vector3(1f, top, 6f), 0);
            // Uma luz de luar basta (o filho de uma instância de prefab não pode ser apagado: desliga).
            var second = floors.GetChild(floors.childCount - 1).GetComponentInChildren<Light>();
            if (second != null) second.enabled = false;
            foreach (float x in new[] { -3f, 3f }) Piece(SlabPath, floors, new Vector3(x, top, 6f), 0);
        }

        // O MORRO: um bloco de terra do Z 7 para o norte, com o topo em Y 3,6. É o chão do nível de
        // cima (galeria, biblioteca, salão, camarins, câmara) e a parede de arrimo dos fundos do
        // térreo. O chão de fora cresce para cobrir a casa toda, a calçada e o pátio.
        static void BuildTerrain(Plan plan, Transform house)
        {
            float minX = plan.comodos.Min(r => r.x0) - 2f, maxX = plan.comodos.Max(r => r.x1) + 2f;
            float maxZ = plan.comodos.Max(r => r.z1) + 2f, minZ = plan.comodos.Min(r => r.z0) - 4f;
            var hill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hill.name = "Morro (encosta)";
            hill.transform.SetParent(house);
            hill.transform.position = new Vector3((minX + maxX) / 2, FloorY / 2, (plan.morroZ + maxZ) / 2);
            hill.transform.localScale = new Vector3(maxX - minX, FloorY, maxZ - plan.morroZ);
            hill.GetComponent<Renderer>().sharedMaterial = EditorTools.ColorMaterial("Morro", new Color(0.14f, 0.125f, 0.11f));
            GameObjectUtility.SetStaticEditorFlags(hill, StaticEditorFlags.NavigationStatic);

            var ground = GameObject.Find("Chão");
            if (ground == null) return;
            ground.transform.position = new Vector3((minX + maxX) / 2, -0.01f, (minZ + maxZ) / 2);
            ground.transform.localScale = new Vector3((maxX - minX) / 10f, 1f, (maxZ - minZ) / 10f); // o Plane tem 10 m
            // Ele fica 1 cm abaixo das tábuas: se fizesse sombra, ela "vazava" por cima delas em
            // triângulos que piscavam com a lanterna perto. É o chão mais baixo: não precisa de sombra.
            if (ground.TryGetComponent(out MeshRenderer groundRenderer))
                groundRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- Escadas ----------

        // A nobre (3,0 m) e as duas de serviço (2,0 m), todas subindo para +Z. O NavMesh afasta 0,5 m
        // das laterais da rampa e o pé vira uma ilha: um link liga o chão à rampa, um pouco acima do pé,
        // para o Imitador subir e descer (os jogadores andam pela física).
        static void BuildStairsFromPlan(Plan plan, Transform parent)
        {
            foreach (var s in plan.escadas)
            {
                bool noble = s.largura >= 2.9f;
                var stairs = Place(noble ? StairsPath : ServiceStairsPath, new Vector3(s.x, 0, s.z), 0, s.nome);
                stairs.transform.SetParent(parent);

                var link = new GameObject($"Link do Pé: {s.nome}").AddComponent<NavMeshLink>();
                link.transform.SetParent(parent);
                link.startPoint = new Vector3(s.x, 0f, s.z - 0.6f);
                link.endPoint = new Vector3(s.x, FloorY / StairRun * 1.0f, s.z + 1.0f);
                link.width = Mathf.Max(0.8f, s.largura - 1.0f);
                link.bidirectional = true;

                if (noble) BuildNobleRailings(s, parent);
            }
        }

        // Corrimões dos dois lados da escadaria (ela fica no meio do hall, sem parede) e o peitoril do
        // patamar, aberto só onde a escada chega. Os corrimões começam 1 m acima do pé.
        static void BuildNobleRailings(PlanStair s, Transform parent)
        {
            float angle = Mathf.Atan2(FloorY, StairRun);
            float length = Mathf.Sqrt(FloorY * FloorY + StairRun * StairRun);
            const float openFoot = 1f;
            float piece = (length - openFoot) / 3;
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 3; i++)
                {
                    float t = openFoot + (i + 0.5f) * piece;
                    var rail = Place(RailingPath, new Vector3(s.x + side * s.largura / 2, Mathf.Sin(angle) * t, s.z + Mathf.Cos(angle) * t), 0, "Corrimão da Escadaria");
                    rail.transform.rotation = Quaternion.Euler(0, -90, angle * Mathf.Rad2Deg);
                    rail.transform.localScale = new Vector3(piece / 2f, 1, 1);
                    rail.transform.SetParent(parent);
                }

            // Peitoril do patamar (Z = topo da escada), de cada lado da chegada, até as paredes do hall.
            float edgeZ = s.z + s.avanco, half = s.largura / 2, wall = 4f;
            foreach (float side in new[] { -1f, 1f })
            {
                float from = s.x + side * half, to = s.x + side * wall;
                var rail = Place(RailingPath, new Vector3((from + to) / 2, FloorY, edgeZ), 0, "Peitoril do Patamar");
                rail.transform.localScale = new Vector3(Mathf.Abs(to - from) / 2f, 1, 1);
                rail.transform.SetParent(parent);
            }
        }

        // ---------- Luz ----------

        // As 15 luzes fixas (LEVEL_DESIGN_FASE1_MANSAO.md §6.1); o resto da casa é breu.
        //   acesa/falha  arandela na parede mais perto (acesa = sem tremer); longe de parede: lâmpada pendurada
        //   luar         janela com tábuas na parede (o luar entra pelas frestas); a clarabóia já veio no forro
        //   apagada      o lustre do hall (só acende na aparição); refletor e luminária verde: etapas 2 e 3
        //   ciano        o portal da Câmara: etapa 2
        static void BuildPlanLights(Plan plan, Transform parent)
        {
            var show = new System.Collections.Generic.List<Light>(); // acendem na aparição
            foreach (var l in plan.luzes)
            {
                float floor = FloorOf(l.nivel);
                if (l.nome.Contains("caminhão"))
                {
                    var lamp = new GameObject("Faróis do Caminhão");
                    lamp.transform.SetParent(parent);
                    AddLight(lamp, new Vector3(l.x, 1.2f, l.z), new Color(1f, 0.75f, 0.45f), 4f, 1.2f);
                    continue;
                }
                switch (l.tipo)
                {
                    case "acesa":
                    case "falha":
                        // A arandela da galeria é a fonte de ruído nº 3 (assobia; a Cida afina e ela
                        // APAGA). Objeto de rede: fica na raiz da cena.
                        if (l.nome.Contains("galeria") && SnapToWall(plan, l.nivel, l.x, l.z, out var gpos, out float gyaw))
                        {
                            Place(CorridorSconcePath, new Vector3(gpos.x, floor + 2.2f, gpos.z), gyaw, "Arandela da Galeria");
                            break;
                        }
                        if (!SnapToWall(plan, l.nivel, l.x, l.z, out var pos, out float yaw))
                        {
                            var bulb = new GameObject($"Luz: {l.nome}");
                            bulb.transform.SetParent(parent);
                            AddLight(bulb, new Vector3(l.x, floor + 2.9f, l.z), new Color(1f, 0.78f, 0.5f), 5f, 0.8f).AddComponent<LightFlicker>();
                            break;
                        }
                        var sconce = Place(SconcePath, new Vector3(pos.x, floor + 2.2f, pos.z), yaw, $"Luz: {l.nome}");
                        sconce.transform.SetParent(parent);
                        if (l.tipo == "acesa")
                            foreach (var f in sconce.GetComponentsInChildren<LightFlicker>()) Object.DestroyImmediate(f);
                        break;
                    case "luar":
                        if (l.nome.Contains("clarabóia")) break;
                        if (SnapToWall(plan, l.nivel, l.x, l.z, out var wpos, out float wyaw))
                            Piece(BoardedWindowPath, parent, new Vector3(wpos.x, floor + 1.0f, wpos.z), wyaw);
                        break;
                    case "apagada":
                        if (l.nome.Contains("lustre"))
                        {
                            float y = 2 * WallHeight + SlabThickness - 0.7f;
                            Piece(ChandelierPath, parent, new Vector3(l.x, y, l.z), 0);
                            var glow = new GameObject("Luz do Lustre");
                            glow.transform.SetParent(parent);
                            show.Add(AddLight(glow, new Vector3(l.x, y - 0.2f, l.z), new Color(1f, 0.8f, 0.55f), 11f, 2.2f).GetComponent<Light>());
                        }
                        else if (l.nome.Contains("refletor"))
                        {
                            // O refletor do espetáculo: no patamar, apontado para o palco da aparição.
                            var spot = new GameObject("Refletor do Espetáculo");
                            spot.transform.SetParent(parent);
                            spot.transform.position = new Vector3(l.x, FloorOf(l.nivel) + 2.8f, l.z);
                            var stage = new Vector3(plan.imitador.palcoX, 0, plan.imitador.palcoZ);
                            spot.transform.rotation = Quaternion.LookRotation(stage - spot.transform.position);
                            var light = spot.AddComponent<Light>();
                            light.type = LightType.Spot;
                            light.color = new Color(1f, 0.9f, 0.75f);
                            light.range = 20f;
                            light.spotAngle = 34f;
                            light.intensity = 4f;
                            light.shadows = LightShadows.Soft;
                            show.Add(light);
                        }
                        // a luminária verde da biblioteca (acender com um clique) é posta com a atmosfera (etapa 3)
                        break;
                }
            }
            // As luzes do espetáculo ficam apagadas e acendem com a aparição (AppearanceLights).
            var lights = new GameObject("Luzes do Espetáculo").AddComponent<AppearanceLights>();
            lights.transform.SetParent(parent);
            lights.Configure(show.ToArray());
            EditorUtility.SetDirty(lights);
        }

        // Encosta um ponto na parede mais perto do cômodo onde ele está (até 1,5 m dela): devolve a
        // posição colada na parede e o giro que faz o objeto olhar para dentro do cômodo.
        static bool SnapToWall(Plan plan, int level, float x, float z, out Vector3 position, out float yaw)
        {
            position = default; yaw = 0;
            var room = plan.comodos.FirstOrDefault(r => !r.plataforma && (r.nivel == level || r.duplo) &&
                                                        r.x0 <= x && x <= r.x1 && r.z0 <= z && z <= r.z1);
            if (room == null) return false;
            var edges = new (float dist, Vector3 pos, float yaw)[]
            {
                (z - room.z0, new Vector3(x, 0, room.z0 + Inside), 0f),
                (room.z1 - z, new Vector3(x, 0, room.z1 - Inside), 180f),
                (x - room.x0, new Vector3(room.x0 + Inside, 0, z), 90f),
                (room.x1 - x, new Vector3(room.x1 - Inside, 0, z), -90f),
            };
            var best = edges.OrderBy(e => e.dist).First();
            if (best.dist > 1.5f) return false;
            // No cômodo de fora (a calçada), "para dentro" é para a rua: a luz fica na fachada.
            position = best.pos; yaw = best.yaw;
            return true;
        }

        // As 2 lanternas no estribo do caminhão (num caixote, perto da lâmpada da caçamba) e as
        // pilhas que ficam à vista. As escondidas (que caem de algo) são postas com a atmosfera.
        // Objetos de rede ficam na RAIZ da cena.
        static void BuildLightItemsFromPlan(Plan plan, Transform dressing)
        {
            var t = plan.caminhao;
            var box = new Vector3(t.x + 1.9f, 0, t.z + 2.6f);
            Piece(SmallBoxPath, dressing, box, 0);
            Place(FlashlightPath, box + new Vector3(-0.12f, 0.4f, 0), 0, "Lanterna 1");
            Place(FlashlightPath, box + new Vector3(0.12f, 0.4f, -0.05f), 15, "Lanterna 2");

            int n = 0;
            foreach (var p in plan.pilhas)
                if (p.tipo == "fixa")
                    Place(BatteriesPath, new Vector3(p.x, SurfaceY(p.x, p.z, FloorOf(p.nivel)), p.z), 30 * n++, $"Pilhas {n}");
        }

        // A altura de onde se apoia algo pequeno ali (o chão, ou o palco, uma mesa...).
        static float SurfaceY(float x, float z, float floor)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(new Vector3(x, floor + 2f, z), Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y : floor;
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

        // ---------- Móveis e objetivos ----------

        // A lista do chefe (14 entregas), separada pelas 3 etapas da planta. O prefab de cada uma é
        // um nome curto no JSON. Objetos de rede ficam na RAIZ da cena.
        static Furniture[][] PlaceFurnitureFromPlan(Plan plan)
        {
            string PathOf(string key) => key switch
            {
                "cadeira" => LevelPrefabsBuilder.ChairPath,
                "caixaM" => LevelPrefabsBuilder.BoxPath,
                "talha" => WaterJarPath,
                "caixaP" => SmallCarryBoxPath,
                "caixaG" => BigBoxPath,
                "bau" => TourTrunkPath,
                "poltrona" => CarryArmchairPath,
                "mesaLeitura" => ReadingTablePath,
                "armario" => LevelPrefabsBuilder.WardrobePath,
                "penteadeira" => VanityPath,
                "arara" => RackPath,
                "cartaz" => RelicPosterPath,
                _ => throw new System.ArgumentException($"Prefab desconhecido na planta: {key}"),
            };
            int stageCount = plan.moveis.Max(m => m.etapa) + 1;
            var stages = new System.Collections.Generic.List<Furniture>[stageCount];
            for (int i = 0; i < stageCount; i++) stages[i] = new();
            foreach (var m in plan.moveis)
            {
                string path = PathOf(m.prefab);
                var prefab = Load(path);
                // A origem dos móveis fica no MEIO da caixa: sobe meia altura (e 2 cm de folga).
                var box = prefab.GetComponent<BoxCollider>();
                float half = box != null ? (box.size.y / 2 - box.center.y) * prefab.transform.localScale.y : 0.5f;
                var go = Place(path, new Vector3(m.x, FloorOf(m.nivel) + half + 0.02f, m.z), m.giro, m.nome);
                stages[m.etapa].Add(go.GetComponent<Furniture>());
            }
            return stages.Select(s => s.ToArray()).ToArray();
        }

        // As 3 etapas (no lugar do "entregar todos" que a fase vazia traz).
        // Acha a pasta pelos objetivos que já estão nela: pelo nome não dá, a HUD também tem um "Objetivos".
        static readonly string[] StageLabels = { "Esvaziar o térreo", "Esvaziar a ala do teatro", "O cartaz" };

        static void BuildObjectives(Furniture[][] stages)
        {
            var old = Object.FindObjectsByType<DeliverFurnitureObjective>();
            var root = old.Length > 0 ? old[0].transform.parent : new GameObject("Objetivos").transform;
            foreach (var o in old) Object.DestroyImmediate(o.gameObject);

            for (int stage = 0; stage < stages.Length; stage++)
            {
                string label = stage < StageLabels.Length ? StageLabels[stage] : $"Etapa {stage + 1}";
                var go = new GameObject($"Etapa {stage}: {label}");
                go.transform.SetParent(root);
                var objective = go.AddComponent<DeliverFurnitureObjective>();
                var so = new SerializedObject(objective);
                so.FindProperty("label").stringValue = label;
                so.FindProperty("stage").intValue = stage;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorTools.SetReferences(objective, "targets", stages[stage]);
            }
        }

        // ---------- Cômodos e nascimento ----------

        // Um marcador por cômodo, POR ANDAR: o do hall vai só até o piso de cima, e o patamar tem o
        // dele (senão o aviso diria "caiu no Grande Hall" para quem caiu no patamar).
        static void BuildRoomsAndSpawnsFromPlan(Plan plan)
        {
            var markers = GameObject.Find("Marcadores").transform;
            foreach (var r in plan.comodos)
            {
                float floor = FloorOf(r.nivel);
                float height = r.duplo ? FloorY : WallHeight;
                LevelSceneTools.RoomMarker(markers, r.nome,
                    new Vector3((r.x0 + r.x1) / 2, floor + height / 2, (r.z0 + r.z1) / 2),
                    new Vector3(r.x1 - r.x0, height, r.z1 - r.z0));
            }

            // A fase vazia nasce com 3 marcadores; aqui eles vão para a calçada, virados para a casa.
            var spawns = markers.GetComponentsInChildren<SpawnPoint>();
            for (int i = 0; i < spawns.Length && i < plan.nascimento.Length; i++)
            {
                spawns[i].transform.position = new Vector3(plan.nascimento[i].x, 1f, plan.nascimento[i].z);
                spawns[i].transform.rotation = Quaternion.identity;
            }
        }

        // ---------- Etapa 2: espelhos e o Imitador ----------

        const string SecretMirrorPath = "Assets/Prefabs/Espelhos/Espelho-porta (E9).prefab";
        const string PortalPath = "Assets/Prefabs/Espelhos/Portal da Câmara.prefab";
        const string BooksPath = "Assets/Prefabs/Cenário/Pilha de Livros.prefab";
        const string FlameCandlesPath = "Assets/Prefabs/Cenário/Velas (só chama).prefab";

        // Os prefabs que só a Mansão usa. Chamado pelo BuildPrefabs (só cria o que falta).
        static void BuildMansionPrefabs()
        {
            LevelPrefabsBuilder.Build(SecretMirrorPath, false, BuildSecretMirror);
            LevelPrefabsBuilder.Build(PortalPath, false, BuildPortal);
            LevelPrefabsBuilder.Build(BooksPath, false, () => StaticProp("Pilha de Livros", "Props/PilhaLivros/SM_PRP_PilhaLivros", Vector3.zero));
            LevelPrefabsBuilder.Build(FlameCandlesPath, false, () => StaticProp("Velas", "Props/Velas/SM_PRP_Velas", Vector3.zero));
            BuildPeriodPrefabs();
        }

        // O E9: um espelho G que fecha o vão da porta dos Camarins para a Câmara. A FOLHA gira pela
        // dobradiça (a origem dela, na borda esquerda do vão); a rachadura aparece quando a Cida
        // revela. Fica de fora do bake do NavMesh; o obstáculo fecha o vão enquanto está no lugar.
        static GameObject BuildSecretMirror()
        {
            var root = new GameObject("Espelho-porta (E9)");
            Box(root, new Vector3(0, DoorHeight / 2, 0), new Vector3(DoorWidth, DoorHeight, 0.12f));
            root.AddComponent<Unity.Netcode.NetworkObject>();
            var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.center = new Vector3(0, DoorHeight / 2, 0);
            obstacle.size = new Vector3(DoorWidth, DoorHeight, 0.4f);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var tag = new SerializedObject(root.AddComponent<SurfaceTag>());
            tag.FindProperty("type").enumValueIndex = (int)SurfaceType.Glass;
            tag.ApplyModifiedPropertiesWithoutUndo();

            var leaf = new GameObject("Folha").transform;
            leaf.SetParent(root.transform, false);
            leaf.localPosition = new Vector3(-DoorWidth / 2, 0, 0);
            var frame = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/MolduraL/SM_ENV_MolduraL.fbx");
            var cracked = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/MolduraLRachada/SM_ENV_MolduraLRachada.fbx");
            if (frame != null) LevelPrefabsBuilder.AddModel(leaf.gameObject, frame, new Vector3(DoorWidth / 2, 0.2f, 0));
            GameObject crack = null;
            if (cracked != null)
            {
                crack = LevelPrefabsBuilder.AddModel(leaf.gameObject, cracked, new Vector3(DoorWidth / 2, 0.2f, 0.008f));
                crack.name = "Rachadura";
                crack.SetActive(false);
            }

            var door = root.AddComponent<SecretMirrorDoor>();
            var so = new SerializedObject(door);
            so.FindProperty("leaf").objectReferenceValue = leaf;
            so.FindProperty("crack").objectReferenceValue = crack;
            so.FindProperty("openAngle").floatValue = 100f; // gira para dentro da Câmara (para trás do vidro)
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // O PORTAL da Câmara: a moldura GG com o vidro ciano rachado, uma luz ciano fraca (o único
        // ciano da casa, fora os olhos dele) e o gatilho próprio (PortalTrap). Não é esconderijo.
        static GameObject BuildPortal()
        {
            var root = KitPiece("Portal da Câmara", "Environment/MolduraPortal/SM_ENV_MolduraPortal",
                r => Box(r, new Vector3(0, 1.2f, 0), new Vector3(1.5f, 2.4f, 0.08f)));
            AddLight(root, new Vector3(0, 1.4f, 0.4f), new Color(0.5f, 0.9f, 0.8f), 3.5f, 0.9f).AddComponent<LightFlicker>();
            var tag = new SerializedObject(root.AddComponent<SurfaceTag>());
            tag.FindProperty("type").enumValueIndex = (int)SurfaceType.Glass;
            tag.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<PortalTrap>();
            return root;
        }

        // Os 14 espelhos da planta. Os de esconderijo (heróis e comuns) são o prefab do tamanho certo;
        // o E9 é o espelho-porta (rede: na raiz) e o portal fica preso à Câmara e ao cartaz.
        static System.Collections.Generic.Dictionary<string, Mirror> BuildMirrorsFromPlan(Plan plan, Furniture[][] stages)
        {
            var found = new System.Collections.Generic.Dictionary<string, Mirror>();
            foreach (var e in plan.espelhos)
            {
                float floor = FloorOf(e.nivel);
                var inward = Quaternion.Euler(0, e.giro, 0) * Vector3.forward;
                var onWall = new Vector3(e.x, floor, e.z);
                switch (e.tipo)
                {
                    case "porta":
                        Place(SecretMirrorPath, onWall, e.giro, $"{e.id} Espelho-porta (Câmara)");
                        break;
                    case "portal":
                    {
                        var portal = Place(PortalPath, onWall + inward * Inside, e.giro, $"{e.id} da Câmara");
                        var room = plan.comodos.First(r => r.nome.StartsWith("Câmara"));
                        var relic = stages.SelectMany(s => s).FirstOrDefault(f => f != null && f.name.Contains("Cartaz"));
                        portal.GetComponent<PortalTrap>().Configure(
                            new Vector3((room.x0 + room.x1) / 2, floor + WallHeight / 2, (room.z0 + room.z1) / 2),
                            new Vector3(room.x1 - room.x0, WallHeight, room.z1 - room.z0), relic);
                        EditorUtility.SetDirty(portal.GetComponent<PortalTrap>());
                        break;
                    }
                    default:
                    {
                        string path = e.tamanho == "P" ? MirrorSPath : e.tamanho == "M" ? MirrorMPath : MirrorLPath;
                        float lift = e.tamanho == "P" ? 1.1f : e.tamanho == "M" ? 0.5f : 0f;
                        var go = Place(path, onWall + inward * Inside + Vector3.up * lift, e.giro, $"{e.id} Espelho{(e.tipo == "heroi" ? " (herói)" : "")}");
                        if (e.tipo == "heroi" && go.TryGetComponent(out MirrorReflection reflection))
                        {
                            var so = new SerializedObject(reflection);
                            so.FindProperty("hero").boolValue = true;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                        found[e.id] = go.GetComponent<Mirror>();
                        break;
                    }
                }
            }
            return found;
        }

        // O Imitador começa dentro do E1 (no alto da escadaria). Aos 45 s ele desce até o palco
        // (debaixo do lustre), faz a reverência e volta pelo E2.
        static void BuildImitatorFromPlan(Plan plan, System.Collections.Generic.Dictionary<string, Mirror> mirrors, Transform house)
        {
            if (!mirrors.TryGetValue(plan.imitador.espelho, out var start)) return;
            var exit = start.ExitPoint;
            var imitator = Place(ImitatorPath, new Vector3(exit.x, exit.y + ImitatorHeight / 2, exit.z), start.transform.eulerAngles.y, null);
            var stage = new GameObject("Palco do Imitador (aparição)").transform;
            stage.SetParent(house);
            stage.position = new Vector3(plan.imitador.palcoX, 0, plan.imitador.palcoZ);
            var so = new SerializedObject(imitator.GetComponent<GhostImitator>());
            so.FindProperty("stage").objectReferenceValue = stage;
            if (mirrors.TryGetValue(plan.imitador.saidaDaAparicao, out var back))
                so.FindProperty("appearanceExit").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Etapa 2: o som da casa ----------

        // As fontes de ruído (o piano e o gramofone; a arandela da galeria vem com as luzes), o rádio
        // do chefe na calçada, as âncoras de cada cômodo e o piso que range.
        static void BuildSoundFromPlan(Plan plan, Transform dressing)
        {
            foreach (var n in plan.ruido)
            {
                string path = n.prefab == "piano" ? PianoPath : GramophonePath;
                Place(path, new Vector3(n.x, FloorOf(n.nivel), n.z), n.giro, n.prefab == "piano" ? "Pianola" : "Gramofone");
            }
            var anchors = Group(dressing, "Âncoras Sonoras");
            Place(RadioPath, new Vector3(2.2f, 0f, -18.2f), 200, "Rádio do Seu Valdemar").transform.SetParent(anchors);
            foreach (var a in plan.ancoras)
                Anchor(anchors, a.nome, a.som, new Vector3(a.x, FloorOf(a.nivel) + a.altura, a.z));
            var creaks = Group(dressing, "Piso que Range");
            foreach (var r in plan.range)
            {
                float floor = FloorOf(r.nivel);
                Creak(creaks, r.nome, new Vector3(r.x, floor, r.z), new Vector3(r.largura, 1.5f, r.profundidade));
                Piece(LoosePlankPath, creaks, new Vector3(r.x, floor, r.z), 90);
            }
        }

        // ---------- Etapa 2: o abandono ----------

        // O cenário da planta (mesa posta, retratos, estantes, manequins, velas...) e as teias de
        // canto: duas por cômodo, nos cantos do forro. Os manequins e o que tem colisão nunca
        // ficam na frente de porta (o script da planta confere).
        static void BuildDressingFromPlan(Plan plan, Transform dressing)
        {
            var decor = Group(dressing, "Abandono");
            foreach (var c in plan.cenario)
            {
                float floor = FloorOf(c.nivel);
                var pos = new Vector3(c.x, floor + c.altura, c.z);
                bool onWall = c.prefab.StartsWith("retrato") || c.prefab == "papelDescascando" || c.prefab == "janela";
                if (onWall) pos += Quaternion.Euler(0, c.giro, 0) * Vector3.forward * Inside;
                if (c.prefab == "manequimQueTomba")
                {
                    // O manequim que TOMBA sozinho aos 2:40 (o "vento"), ao comprido da galeria.
                    var falling = Place(MannequinAPath, pos, c.giro, "Manequim que Tomba");
                    falling.transform.SetParent(decor);
                    falling.AddComponent<TimedTopple>();
                    falling.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Impact, sound = "manequim_tomba" } };
                    continue;
                }
                var go = Place(DecorPath(c.prefab), pos, c.giro, null);
                go.transform.SetParent(decor);
            }

            var webs = Group(dressing, "Teias");
            foreach (var r in plan.comodos)
            {
                if (r.exterior || r.plataforma) continue;
                float top = FloorOf(r.nivel) + (r.duplo ? 2 * WallHeight + SlabThickness : WallHeight) - 0.02f;
                Piece(CobwebPath, webs, new Vector3(r.x0 + Inside, top, r.z1 - Inside), 180); // noroeste
                Piece(CobwebPath, webs, new Vector3(r.x1 - Inside, top, r.z0 + Inside), 0);   // sudeste
            }
        }

        static string DecorPath(string key) => key switch
        {
            "mesaJantar" => DiningTablePath,
            "cadeiraJantar" => DiningChairPath,
            "cadeiraTombada" => FallenChairPath,
            "papeis" => PapersPath,
            "pia" => SinkPath,
            "estante" => ShelfPath,
            "caixote" => SmallBoxPath,
            "retratoG" => PortraitGPath,
            "retratoM" => PortraitMPath,
            "retratoP" => PortraitPPath,
            "comodaCoberta" => CoveredDresserPath,
            "poltronaCoberta" => CoveredChairPath,
            "entulho" => RubblePath,
            "papelDescascando" => PeelingPaperPath,
            "manequimA" => MannequinAPath,
            "manequimB" => MannequinBPath,
            "manequimC" => MannequinCPath,
            "velas" => FlameCandlesPath,
            "pilhaLivros" => BooksPath,
            "janela" => BoardedWindowPath,
            "fogao" => StovePath,
            "bancada" => CounterPath,
            "aparador" => SideboardPath,
            _ => throw new System.ArgumentException($"Cenário desconhecido na planta: {key}"),
        };
    }
}
