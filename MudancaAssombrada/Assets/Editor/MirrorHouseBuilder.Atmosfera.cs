using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // A ETAPA 3 da Mansão do Espelho (LEVEL_DESIGN_FASE1_MANSAO.md §6 a §9): atmosfera e época.
    //   - a pianola (a trilha em 3D, sincronizada) e as frases de piano;
    //   - a casa reage: o que cai, toca ou balança (HouseScare), e as pilhas escondidas que isso larga;
    //   - as teias de vão (DoorWeb), o lampião de querosene e a luminária verde;
    //   - os achados de época (PeriodFind) e os frágeis (Fragile).
    // Tudo vem da planta (mansao_v1_2.py: ACHADOS, LAMPIOES, QUEDAS, TEIAS_VAO e as pilhas escondidas).
    public static partial class MirrorHouseBuilder
    {
        partial class Plan
        {
            public PlanFind[] achados;
            public PlanLamp[] lampioes;
            public PlanScare[] quedas;
            public PlanWeb[] teiasVao;
        }
        [System.Serializable] class PlanFind { public string nome, prefab; public int nivel; public float x, z, giro; }
        [System.Serializable] class PlanLamp { public int nivel; public float x, z; }
        [System.Serializable] class PlanWeb { public int nivel; public float x, z, largura, giro; }
        [System.Serializable] class PlanScare
        {
            public string nome, modelo, gatilho, som, mov, aviso;
            public int nivel;
            public float x, z, giro, altura, raio, chance, segundo, alcance, dirX, dirY, dirZ, pilhaX, pilhaZ, somEmX, somEmZ;
            public bool susto, temPilha, temSomEm;
        }

        const string ScarePath = "Assets/Prefabs/Cenário/Susto da Casa.prefab";
        const string DoorWebPath = "Assets/Prefabs/Cenário/Teia de Vão.prefab";
        const string StovePath = "Assets/Prefabs/Cenário/Fogão a Lenha.prefab";
        const string CounterPath = "Assets/Prefabs/Cenário/Bancada.prefab";
        const string SideboardPath = "Assets/Prefabs/Cenário/Aparador.prefab";
        const string GreenLampPath = "Assets/Prefabs/Cenário/Luminária Verde.prefab";
        const string KeroseneLampPath = "Assets/Prefabs/Itens/Lampião de Querosene.prefab";
        const string WaterJarPath = "Assets/Prefabs/Móveis/Talha de Barro.prefab";
        const string FindsFolder = "Assets/Prefabs/Achados";

        // Os achados de época (§8.2): chave da planta -> (nome, modelo, caixa, peso, valor, frágil, som carregando, bilhete do chefe).
        static readonly (string key, string name, string art, Vector3 size, float weight, int value, bool fragile, string loop, string comment)[] Finds =
        {
            ("cristaleira", "Cristaleira", "Cristaleira", new Vector3(1.0f, 1.8f, 0.4f), 3f, 60, true, "vidro_tilintando",
                "A cristaleira dos Taborda. Contei as taças: estão todas. Por enquanto."),
            ("telefone", "Telefone de Parede", "TelefoneParede", new Vector3(0.3f, 0.6f, 0.2f), 1f, 25, false, "",
                "Esse telefone não toca desde 1927. Se tocou, não me contem."),
            ("cadeiraBalanco", "Cadeira de Balanço", "CadeiraBalanco", new Vector3(0.6f, 1.05f, 0.9f), 1.5f, 30, false, "",
                "A cadeira da dona da casa. Ela não gostava que sentassem nela."),
            ("rolos", "Rolos de Pianola", "RolosPianola", new Vector3(0.5f, 0.4f, 0.3f), 1f, 20, false, "",
                "Os rolos da pianola. Ela vai continuar tocando sem eles. Sempre tocou."),
            ("cofre", "Cofre da Bilheteria", "Cofre", new Vector3(0.7f, 0.9f, 0.6f), 3.5f, 80, false, "",
                "O borderô da última sessão. Meu nome está no elenco. Guardem bem."),
            ("maquinaEscrever", "Máquina de Escrever", "MaquinaEscrever", new Vector3(0.45f, 0.33f, 0.35f), 1f, 35, false, "",
                "Se ela escreveu alguma coisa sozinha, não leiam. Eu já li."),
            ("lanternaMagica", "Lanterna Mágica", "LanternaMagica", new Vector3(0.35f, 0.4f, 0.3f), 1f, 40, true, "",
                "Não liguem a lanterna mágica. Tem um rosto riscado na lâmina. O meu."),
            ("psiche", "Espelho de Pé", "Psiche", new Vector3(0.6f, 1.6f, 0.5f), 2f, 45, true, "",
                "Um espelho a menos naquela casa. Obrigado."),
            ("maquinaCostura", "Máquina de Costura", "MaquinaCostura", new Vector3(0.9f, 1.06f, 0.45f), 2.5f, 40, false, "",
                "A máquina dos figurinos. Costurou o meu, uma vez."),
            ("biombo", "Biombo de Camarim", "Biombo", new Vector3(1.5f, 1.7f, 0.35f), 1.5f, 20, false, "",
                "O biombo do camarim. Atrás dele eu esperava a minha deixa."),
        };

        static string FindPath(string name) => $"{FindsFolder}/{name}.prefab";

        // Os prefabs da Etapa 3 (só cria o que falta). Chamado pelo BuildMansionPrefabs.
        static void BuildPeriodPrefabs()
        {
            EditorTools.EnsureFolder(FindsFolder);
            LevelPrefabsBuilder.Build(ScarePath, false, BuildScarePrefab);
            LevelPrefabsBuilder.Build(DoorWebPath, false, BuildDoorWeb);
            LevelPrefabsBuilder.Build(StovePath, false, () => StaticProp("Fogão a Lenha", "Props/FogaoLenha/SM_PRP_FogaoLenha", new Vector3(1.2f, 0.85f, 0.7f)));
            LevelPrefabsBuilder.Build(CounterPath, false, () => StaticProp("Bancada", "Props/Bancada/SM_PRP_Bancada", new Vector3(1.6f, 0.9f, 0.6f)));
            LevelPrefabsBuilder.Build(SideboardPath, false, () => StaticProp("Aparador", "Props/Aparador/SM_PRP_Aparador", new Vector3(1.4f, 0.85f, 0.45f)));
            LevelPrefabsBuilder.Build(GreenLampPath, false, BuildGreenLamp);
            LevelPrefabsBuilder.Build(KeroseneLampPath, false, BuildKeroseneLamp);
            LevelPrefabsBuilder.Build(WaterJarPath, false, () =>
                Fragile(CarryProp("Talha de Barro", "Props/Talha/SM_PRP_Talha", new Vector3(0.45f, 0.6f, 0.45f), 1.5f)));
            foreach (var f in Finds)
                LevelPrefabsBuilder.Build(FindPath(f.name), false, () => BuildFind(f.name, f.art, f.size, f.weight, f.value, f.fragile, f.loop, f.comment));
        }

        // O SUSTO DA CASA: só a rede e o HouseScare. O que se mexe (livros, pratos, gaveta...) é
        // posto como filho em cada lugar pelo construtor da cena.
        static GameObject BuildScarePrefab()
        {
            var root = new GameObject("Susto da Casa");
            root.AddComponent<Unity.Netcode.NetworkObject>();
            root.AddComponent<HouseScare>();
            return root;
        }

        // A teia de vão: o modelo (1 m de largura, esticado em cada porta) e o DoorWeb. Sem colisão.
        static GameObject BuildDoorWeb()
        {
            var root = KitPiece("Teia de Vão", "Props/TeiaVao/SM_PRP_TeiaVao", null);
            root.AddComponent<Unity.Netcode.NetworkObject>();
            root.AddComponent<DoorWeb>();
            return root;
        }

        // A luminária verde da Biblioteca: apagada; "Acender" (qualquer um) liga a luz e faz o clique.
        static GameObject BuildGreenLamp()
        {
            var root = KitPiece("Luminária Verde", "Props/LuminariaVerde/SM_PRP_LuminariaVerde",
                r => Box(r, new Vector3(0, 0.22f, 0), new Vector3(0.4f, 0.45f, 0.3f))); // a mira precisa acertar
            root.AddComponent<Unity.Netcode.NetworkObject>();
            var so = new SerializedObject(root.AddComponent<Interactable>());
            so.FindProperty("actionName").stringValue = "Acender";
            so.ApplyModifiedPropertiesWithoutUndo();
            // Nasce apagada (o editor mostra como o jogo começa); o LampSwitch acende.
            AddLight(root, new Vector3(0, 0.3f, 0.04f), new Color(1f, 0.85f, 0.55f), 4f, 1.1f).GetComponent<Light>().enabled = false;
            root.AddComponent<LampSwitch>();
            root.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Impact, sound = "luminaria_clique" } };
            return root;
        }

        // O lampião de querosene (D12): carregado como um móvel (ocupa as mãos), luz quente de 4 m
        // que não acaba. Deixado no chão, é um porto seguro. Não conta na lista do chefe.
        static GameObject BuildKeroseneLamp()
        {
            var root = CarryProp("Lampião de Querosene", "Props/Lampiao/SM_PRP_Lampiao", new Vector3(0.22f, 0.42f, 0.22f), 1f);
            AddLight(root, new Vector3(0, -0.02f, 0), new Color(1f, 0.72f, 0.4f), 4f, 1.3f);
            return root;
        }

        static GameObject BuildFind(string name, string art, Vector3 size, float weight, int value, bool fragile, string loop, string comment)
        {
            var root = CarryProp(name, $"Props/{art}/SM_PRP_{art}", size, weight);
            var so = new SerializedObject(root.AddComponent<PeriodFind>());
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("value").intValue = value;
            so.FindProperty("comment").stringValue = comment;
            so.FindProperty("carryLoop").stringValue = loop;
            so.ApplyModifiedPropertiesWithoutUndo();
            return fragile ? Fragile(root) : root;
        }

        // Frágil (D13): conta as pancadas, e a batida soa a vidro e barro.
        static GameObject Fragile(GameObject root)
        {
            root.AddComponent<Fragile>();
            root.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Impact, sound = "fragil_bate" } };
            return root;
        }

        // ---------- A cena ----------

        // Tudo o que é de rede (sustos, teias, pilhas, achados, lampiões) fica na RAIZ da cena.
        static void BuildAtmosphereFromPlan(Plan plan)
        {
            var hidden = PlaceHiddenBatteries(plan);
            var finds = PlaceFinds(plan);
            foreach (var l in plan.lampioes)
                Place(KeroseneLampPath, new Vector3(l.x, FloorOf(l.nivel) + 0.23f, l.z), 0, null);
            PlaceGreenLamp(plan);
            BuildPianola(plan);
            foreach (var q in plan.quedas) BuildScare(q, finds, hidden);
            foreach (var w in plan.teiasVao) PlaceDoorWeb(w);
        }

        // A teia estica até a largura da porta (só o modelo: a caixa do DoorWeb é em metros).
        static void PlaceDoorWeb(PlanWeb w)
        {
            var web = Place(DoorWebPath, new Vector3(w.x, FloorOf(w.nivel), w.z), w.giro, null);
            web.transform.Find("Modelo").localScale = new Vector3(w.largura * 0.95f, 1.05f, 1f);
            var so = new SerializedObject(web.GetComponent<DoorWeb>());
            so.FindProperty("size").vector3Value = new Vector3(w.largura, 2.3f, 0.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // As pilhas escondidas (§6.2): começam invisíveis; uma queda as revela. Objetos de rede: na raiz.
        static List<FlashlightPickup> PlaceHiddenBatteries(Plan plan)
        {
            var list = new List<FlashlightPickup>();
            int n = 0;
            foreach (var p in plan.pilhas)
            {
                if (p.tipo != "escondida") continue;
                float floor = FloorOf(p.nivel);
                float y = p.altura >= 0 ? floor + p.altura : SurfaceY(p.x, p.z, floor);
                var go = Place(BatteriesPath, new Vector3(p.x, y, p.z), 40 * n, $"Pilhas Escondidas {++n}");
                var pickup = go.GetComponent<FlashlightPickup>();
                var so = new SerializedObject(pickup);
                so.FindProperty("startsHidden").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                list.Add(pickup);
            }
            return list;
        }

        static Dictionary<string, GameObject> PlaceFinds(Plan plan)
        {
            var placed = new Dictionary<string, GameObject>();
            foreach (var a in plan.achados)
            {
                var find = Finds.First(f => f.key == a.prefab);
                // A origem dos móveis fica no MEIO da caixa: sobe meia altura (e 2 cm de folga).
                var go = Place(FindPath(find.name), new Vector3(a.x, FloorOf(a.nivel) + find.size.y / 2 + 0.02f, a.z), a.giro, a.nome);
                placed[a.nome] = go;
            }
            return placed;
        }

        // A luminária verde em cima da mesinha (o caixote) da Biblioteca, no ponto da lista de luzes.
        static void PlaceGreenLamp(Plan plan)
        {
            var l = plan.luzes.FirstOrDefault(x => x.nome.Contains("luminária"));
            if (l == null) return;
            Place(GreenLampPath, new Vector3(l.x, SurfaceY(l.x, l.z, FloorOf(l.nivel)), l.z), 180, "Luminária Verde");
        }

        // O piano da Sala de Música vira a PIANOLA: a trilha em 3D e as frases de piano. Na Câmara, silêncio.
        static void BuildPianola(Plan plan)
        {
            var piano = GameObject.Find("Pianola");
            if (piano == null) return;
            var room = plan.comodos.First(r => r.nome.StartsWith("Câmara"));
            float floor = FloorOf(room.nivel);
            piano.AddComponent<Pianola>().Configure(
                new Vector3((room.x0 + room.x1) / 2, floor + WallHeight / 2, (room.z0 + room.z1) / 2),
                new Vector3(room.x1 - room.x0, WallHeight, room.z1 - room.z0));
            piano.AddComponent<PianoPhrases>();
        }

        // Uma QUEDA da planta: o Susto da Casa no lugar, com o que se mexe como filho (ou o modelo do achado).
        static void BuildScare(PlanScare q, Dictionary<string, GameObject> finds, List<FlashlightPickup> hidden)
        {
            Transform target = null;
            GameObject scare;
            if (q.modelo.StartsWith("achado:"))
            {
                var find = finds[q.modelo.Substring(7)];
                scare = Place(ScarePath, find.transform.position, find.transform.eulerAngles.y, $"Susto: {q.nome}");
                target = find.transform.Find("Modelo");
            }
            else
            {
                var pos = new Vector3(q.x, FloorOf(q.nivel) + q.altura, q.z);
                if (q.modelo.StartsWith("retrato")) pos += Quaternion.Euler(0, q.giro, 0) * Vector3.forward * Inside; // encosta na parede
                scare = Place(ScarePath, pos, q.giro, $"Susto: {q.nome}");
                target = AddScareModel(scare, q.modelo);
            }

            var so = new SerializedObject(scare.GetComponent<HouseScare>());
            so.FindProperty("trigger").intValue = (int)Triggers(q.gatilho);
            so.FindProperty("radius").floatValue = q.raio;
            so.FindProperty("chance").floatValue = q.chance;
            so.FindProperty("atSecond").floatValue = q.segundo;
            so.FindProperty("isScare").boolValue = q.susto;
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("motion").enumValueIndex = (int)MotionOf(q.mov);
            so.FindProperty("direction").vector3Value = new Vector3(q.dirX, q.dirY, q.dirZ);
            so.FindProperty("duration").floatValue = q.mov == "swing" ? 3.5f : q.mov == "slide" ? 1.2f : 0.6f;
            so.FindProperty("swingDegrees").floatValue = q.modelo.Contains("Telefone") ? 4f : 12f;
            so.FindProperty("sound").stringValue = q.som;
            so.FindProperty("soundRange").floatValue = q.alcance;
            so.FindProperty("cue").stringValue = q.aviso;
            if (q.temSomEm)
            {
                var at = new GameObject("De onde vem o som").transform;
                at.SetParent(scare.transform);
                at.position = new Vector3(q.somEmX, FloorOf(q.nivel) + 1.5f, q.somEmZ);
                so.FindProperty("soundAt").objectReferenceValue = at;
            }
            if (q.temPilha)
            {
                var battery = hidden.OrderBy(h => Vector2.Distance(new Vector2(h.transform.position.x, h.transform.position.z), new Vector2(q.pilhaX, q.pilhaZ))).FirstOrDefault();
                so.FindProperty("reveals").objectReferenceValue = battery;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // O que cai: um prefab de cenário (retrato, caixote) ou um modelo do Blender (livros, pratos, gaveta).
        static Transform AddScareModel(GameObject scare, string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            GameObject child;
            switch (key)
            {
                case "retratoM": child = (GameObject)PrefabUtility.InstantiatePrefab(Load(PortraitMPath), scare.transform); break;
                case "caixote": child = (GameObject)PrefabUtility.InstantiatePrefab(Load(SmallBoxPath), scare.transform); break;
                default:
                    string art = key switch
                    {
                        "livrosSoltos" => "LivrosSoltos",
                        "pratos" => "PratosEmpilhados",
                        "gaveta" => "GavetaAparador",
                        _ => throw new System.ArgumentException($"Modelo de queda desconhecido na planta: {key}"),
                    };
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Props/{art}/SM_PRP_{art}.fbx");
                    if (fbx == null) { Debug.LogWarning($"Queda: modelo {art} não encontrado (rode tools/blender)."); return null; }
                    child = LevelPrefabsBuilder.AddModel(scare, fbx, Vector3.zero);
                    break;
            }
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            return child.transform;
        }

        static HouseScare.Trigger Triggers(string words)
        {
            var t = HouseScare.Trigger.None;
            foreach (var w in words.Split(' '))
                t |= w switch
                {
                    "correndo" => HouseScare.Trigger.Running,
                    "movel" => HouseScare.Trigger.Carried,
                    "perto" => HouseScare.Trigger.PassNear,
                    "segundo" => HouseScare.Trigger.AtSecond,
                    "ninguemOlhando" => HouseScare.Trigger.NobodyLooking,
                    _ => throw new System.ArgumentException($"Gatilho desconhecido na planta: {w}"),
                };
            return t;
        }

        static HouseScare.Motion MotionOf(string mov) => mov switch
        {
            "drop" => HouseScare.Motion.Drop,
            "slide" => HouseScare.Motion.Slide,
            "swing" => HouseScare.Motion.Swing,
            "topple" => HouseScare.Motion.Topple,
            _ => HouseScare.Motion.None,
        };
    }
}
