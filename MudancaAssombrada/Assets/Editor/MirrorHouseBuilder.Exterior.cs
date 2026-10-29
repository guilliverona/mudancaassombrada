using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // O LADO DE FORA da Mansão do Espelho (documentação "Mansão e cenário externo não urbano"): a
    // propriedade isolada na floresta. Os modelos vêm de tools/blender/build_exterior_blockout.py;
    // aqui eles viram prefabs (com colisão onde é sólido) e são postos em volta da casa, SEM mexer
    // na planta dos cômodos.
    //
    //   frente (-Z):  o portão de ferro, a grade, o caminho de pedra com luminárias, o pátio redondo
    //                 com a fonte (o ponto de orientação) e a calçada da casa
    //   leste:        o portão de serviço, o caminho do caminhão, o galpão e a árvore retorcida
    //   oeste:        o lago escuro com a doca e o barco, canteiros, ruínas
    //   fundos (+Z):  o muro de arrimo do morro e a floresta fechada lá em cima
    //   limites:      muro de pedra nas laterais e a floresta (com névoa) do lado de fora
    //
    // Tudo é sorteado com uma semente fixa: recriar a cena dá o mesmo mapa.
    public static partial class MirrorHouseBuilder
    {
        const string ExteriorFolder = "Assets/Prefabs/Exterior";
        const string StoneWallPath = ExteriorFolder + "/Muro de Pedra.prefab";
        const string RetainingWallPath = ExteriorFolder + "/Muro de Arrimo.prefab";
        const string RuinWallPath = ExteriorFolder + "/Muro em Ruína.prefab";
        const string GatePillarPath = ExteriorFolder + "/Pilar do Portão.prefab";
        const string WallPillarPath = ExteriorFolder + "/Pilar do Muro.prefab";
        const string IronGatePath = ExteriorFolder + "/Portão de Ferro.prefab";
        const string IronFencePath = ExteriorFolder + "/Grade de Ferro.prefab";
        const string PathStonePath = ExteriorFolder + "/Laje do Caminho.prefab";
        const string FlowerBedPath = ExteriorFolder + "/Canteiro.prefab";
        const string LeavesPath = ExteriorFolder + "/Folhas no Chão.prefab";
        const string LampPostPath = ExteriorFolder + "/Luminária.prefab";
        const string FountainPath = ExteriorFolder + "/Fonte.prefab";
        const string TwistedTreePath = ExteriorFolder + "/Árvore Retorcida.prefab";
        const string ShedPath = ExteriorFolder + "/Galpão.prefab";
        const string DockPath = ExteriorFolder + "/Doca.prefab";
        const string BoatPath = ExteriorFolder + "/Barco.prefab";
        const string LakePath = ExteriorFolder + "/Lago.prefab";
        static readonly string[] PinePaths = { ExteriorFolder + "/Pinheiro A.prefab", ExteriorFolder + "/Pinheiro B.prefab", ExteriorFolder + "/Pinheiro C.prefab" };
        static readonly string[] BroadleafPaths = { ExteriorFolder + "/Árvore A.prefab", ExteriorFolder + "/Árvore B.prefab" };
        static readonly string[] BushPaths = { ExteriorFolder + "/Arbusto A.prefab", ExteriorFolder + "/Arbusto B.prefab" };
        static readonly string[] RockPaths = { ExteriorFolder + "/Pedra A.prefab", ExteriorFolder + "/Pedra B.prefab" };
        // A fachada da casa (a "casca" de fora das paredes do kit) e a névoa rasteira
        const string FacadeFolder = ExteriorFolder + "/Fachada";
        static string FacadePath(string art) => $"{FacadeFolder}/{art}.prefab";
        static readonly string[] FacadeArts = { "FachadaLisa", "FachadaJanela", "FachadaPorta", "FachadaDupla", "FachadaLisaAlta", "FachadaJanelaAlta", "Cunhal", "Cornija" };
        const string MistPath = ExteriorFolder + "/Névoa Rasteira.prefab";
        const string MistTexturePath = "Assets/Art/Textures/Detail/T_Nevoa.png";

        // As medidas do kit de fora (tools/blender/build_exterior_blockout.py).
        const float ModuleLength = 4f;        // muro, arrimo e grade
        const float PillarHalf = 0.35f;
        const float GateWidth = 4.4f;

        // O terreno: a propriedade (dentro do muro) e até onde vai a floresta.
        const float PropertyWest = -50f, PropertyEast = 60f, PropertyFront = -48f;
        const float ForestWest = -82f, ForestEast = 92f, ForestFront = -82f, ForestBack = 56f;
        static readonly Vector3 PatioCenter = new(0f, 0f, -30f);
        const float PatioRadius = 7.5f;
        const float ServiceX = 18f;           // o portão e o caminho de serviço (o caminhão fica em X 18,5)
        static readonly Vector3 LakeCenter = new(-31f, 0f, -31f);
        static readonly Vector3 ShedSpot = new(34f, 0f, -22f);
        static readonly Vector3 TwistedTreeSpot = new(45f, 0f, -41f);

        static readonly Color LampColor = new(1f, 0.72f, 0.42f);

        // ---------- Prefabs ----------

        static void BuildExteriorPrefabs()
        {
            LevelPrefabsBuilder.Build(StoneWallPath, false, () => KitPiece("Muro de Pedra", "Environment/MuroPedra/SM_ENV_MuroPedra",
                r => Box(r, new Vector3(0, 0.85f, 0), new Vector3(ModuleLength, 1.7f, 0.55f))));
            LevelPrefabsBuilder.Build(RetainingWallPath, false, () => KitPiece("Muro de Arrimo", "Environment/MuroArrimo/SM_ENV_MuroArrimo",
                r => Box(r, new Vector3(0, 1.8f, 0.05f), new Vector3(ModuleLength, 3.6f, 0.6f))));
            LevelPrefabsBuilder.Build(RuinWallPath, false, () => KitPiece("Muro em Ruína", "Environment/MuroRuina/SM_ENV_MuroRuina",
                r => Box(r, new Vector3(-1f, 0.6f, 0), new Vector3(2f, 1.2f, 0.5f))));
            LevelPrefabsBuilder.Build(GatePillarPath, false, BuildGatePillar);
            LevelPrefabsBuilder.Build(WallPillarPath, false, () => KitPiece("Pilar do Muro", "Environment/PilarMuro/SM_ENV_PilarMuro",
                r => Box(r, new Vector3(0, 1.1f, 0), new Vector3(0.84f, 2.2f, 0.84f))));
            LevelPrefabsBuilder.Build(IronGatePath, false, () => KitPiece("Portão de Ferro", "Environment/PortaoFerro/SM_ENV_PortaoFerro",
                r => Box(r, new Vector3(0, 1.3f, 0), new Vector3(GateWidth, 2.6f, 0.12f))));
            LevelPrefabsBuilder.Build(IronFencePath, false, () => KitPiece("Grade de Ferro", "Environment/GradeFerro/SM_ENV_GradeFerro",
                r => Box(r, new Vector3(0, 1.1f, 0), new Vector3(ModuleLength, 2.2f, 0.4f))));
            // O chão de fora é o "Chão" da fase: as lajes, o canteiro e as folhas são só desenho.
            LevelPrefabsBuilder.Build(PathStonePath, false, () => KitPiece("Laje do Caminho", "Environment/LajeCaminho/SM_ENV_LajeCaminho", null));
            LevelPrefabsBuilder.Build(FlowerBedPath, false, () => KitPiece("Canteiro", "Environment/Canteiro/SM_ENV_Canteiro", null));
            LevelPrefabsBuilder.Build(LeavesPath, false, () => KitPiece("Folhas no Chão", "Environment/FolhasChao/SM_ENV_FolhasChao", null));
            LevelPrefabsBuilder.Build(LampPostPath, false, BuildLampPost);
            LevelPrefabsBuilder.Build(FountainPath, false, BuildFountain);
            LevelPrefabsBuilder.Build(TwistedTreePath, false, () => KitPiece("Árvore Retorcida", "Environment/ArvoreRetorcida/SM_ENV_ArvoreRetorcida", TrunkCollider(0.5f)));
            LevelPrefabsBuilder.Build(ShedPath, false, () => KitPiece("Galpão", "Environment/Galpao/SM_ENV_Galpao",
                r => Box(r, new Vector3(0, 1.6f, 0), new Vector3(4.2f, 3.2f, 3.2f))));
            LevelPrefabsBuilder.Build(DockPath, false, () => KitPiece("Doca", "Environment/Doca/SM_ENV_Doca",
                r => Box(r, new Vector3(0, 0.12f, 3.5f), new Vector3(1.8f, 0.24f, 7f))));
            LevelPrefabsBuilder.Build(BoatPath, false, () => KitPiece("Barco", "Environment/Barco/SM_ENV_Barco", null));
            LevelPrefabsBuilder.Build(LakePath, false, () => KitPiece("Lago", "Environment/Lago/SM_ENV_Lago", null));
            foreach (var (path, letter) in PinePaths.Zip("ABC", (p, l) => (p, l)))
                LevelPrefabsBuilder.Build(path, false, () => WithLod(KitPiece($"Pinheiro {letter}", $"Environment/Pinheiro{letter}/SM_ENV_Pinheiro{letter}", null)));
            LevelPrefabsBuilder.Build(BroadleafPaths[0], false, () => WithLod(KitPiece("Árvore A", "Environment/FolhosaA/SM_ENV_FolhosaA", null)));
            LevelPrefabsBuilder.Build(BroadleafPaths[1], false, () => WithLod(KitPiece("Árvore B", "Environment/FolhosaB/SM_ENV_FolhosaB", null)));
            // A fachada é só desenho: quem segura é a parede do kit, logo atrás dela.
            foreach (var art in FacadeArts)
                LevelPrefabsBuilder.Build(FacadePath(art), false, () => KitPiece(art, $"Environment/{art}/SM_ENV_{art}", null));
            LevelPrefabsBuilder.Build(MistPath, false, BuildMist);
            LevelPrefabsBuilder.Build(BushPaths[0], false, () => KitPiece("Arbusto A", "Environment/ArbustoA/SM_ENV_ArbustoA", null));
            LevelPrefabsBuilder.Build(BushPaths[1], false, () => KitPiece("Arbusto B", "Environment/ArbustoB/SM_ENV_ArbustoB", null));
            LevelPrefabsBuilder.Build(RockPaths[0], false, () => KitPiece("Pedra A", "Environment/PedraA/SM_ENV_PedraA", null));
            LevelPrefabsBuilder.Build(RockPaths[1], false, () => KitPiece("Pedra B", "Environment/PedraB/SM_ENV_PedraB",
                r => Box(r, new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.8f, 1.3f))));
        }

        // A colisão de árvore é só o tronco: a copa fica acima de quem anda (Documentação §5:
        // "simplificar colisores de vegetação").
        static System.Action<GameObject> TrunkCollider(float radius) => r =>
        {
            var c = r.AddComponent<CapsuleCollider>();
            c.radius = radius;
            c.height = 4f;
            c.center = new Vector3(0, 2f, 0);
        };

        // As árvores chegam com dois níveis (_LOD0 e _LOD1, tools/blender) e o Unity cria o LODGroup.
        // Aqui só acertamos quando trocar: o LOD0 enquanto a árvore ocupa mais de 15% da altura da
        // tela, o LOD1 até 1,5%, e depois some (a névoa da noite já apagou ela antes disso).
        static GameObject WithLod(GameObject root)
        {
            var group = root.GetComponentInChildren<LODGroup>();
            if (group == null)
            {
                Debug.LogWarning($"{root.name}: o FBX veio sem LODGroup (rode tools/blender/build_exterior_blockout.py).");
                return root;
            }
            var lods = group.GetLODs();
            if (lods.Length >= 2)
            {
                lods[0].screenRelativeTransitionHeight = 0.15f;
                lods[1].screenRelativeTransitionHeight = 0.015f;
                group.SetLODs(lods);
            }
            return root;
        }

        // A NÉVOA RASTEIRA: uma placa deitada de 12 × 12 m com a textura de névoa (transparente,
        // sem luz) que gira e respira devagar (MistDrift). Em camadas sobre o lago e junto do muro, ela
        // dá o "chão de neblina" que a névoa da noite (igual em todo lugar) não dá.
        static GameObject BuildMist()
        {
            var root = new GameObject("Névoa Rasteira");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Placa";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // deitada, olhando para cima
            quad.transform.localScale = new Vector3(12f, 12f, 1f);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MistMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            root.AddComponent<MistDrift>();
            return root;
        }

        // Material transparente sem luz (Particles/Unlit do URP): cinza-azulado ESCURO (sem luz, ela
        // não escurece com a noite; clara, brilhava como neon na foto noturna). Somadas,
        // as camadas ficam densas onde se cruzam e ralas nas pontas.
        static Material MistMaterial()
        {
            const string path = "Assets/Prefabs/Materiais/Névoa Rasteira.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MistTexturePath));
            mat.SetColor("_BaseColor", new Color(0.09f, 0.1f, 0.13f, 0.5f));
            mat.SetFloat("_Surface", 1f);                 // transparente
            mat.SetFloat("_Blend", 0f);                   // alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);                    // as duas faces (dá para ver de baixo, no lago)
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // O pilar do portão: o lampião de cima acende (luz quente, alcance curto).
        static GameObject BuildGatePillar()
        {
            var root = KitPiece("Pilar do Portão", "Environment/PilarPortao/SM_ENV_PilarPortao",
                r => Box(r, new Vector3(0, 1.25f, 0), new Vector3(0.84f, 2.5f, 0.84f)));
            AddLight(root, new Vector3(0, 2.75f, 0), LampColor, 7f, 1.3f).AddComponent<LightFlicker>();
            return root;
        }

        // A luminária do caminho: o lampião fica na ponta do braço, 55 cm para a frente (+Z).
        static GameObject BuildLampPost()
        {
            var root = KitPiece("Luminária", "Environment/Poste/SM_ENV_Poste", TrunkCollider(0.18f));
            AddLight(root, new Vector3(0, 2.6f, 0.55f), LampColor, 8f, 1.4f);
            return root;
        }

        // A fonte: colisão redonda da mureta (dá para encostar, não para entrar) e uma luz fraca na
        // água, que faz dela o ponto de referência do pátio.
        static GameObject BuildFountain()
        {
            var root = KitPiece("Fonte", "Environment/Fonte/SM_ENV_Fonte", r =>
            {
                var c = r.AddComponent<CapsuleCollider>();
                c.radius = 2.3f;
                c.height = 4.6f;            // a cápsula com raio = metade da altura vira uma esfera...
                c.center = new Vector3(0, -1.6f, 0); // ...enterrada: em cima do chão sobra a mureta (0,7 m)
                Box(r, new Vector3(0, 1.6f, 0), new Vector3(0.9f, 3.2f, 0.9f)); // a coluna e a estátua
            });
            AddLight(root, new Vector3(0, 1.3f, 0), new Color(1f, 0.8f, 0.55f), 5f, 0.5f);
            return root;
        }

        // ---------- Terreno ----------

        // O chão da fase vira grama (com o detalhe de grama em metros) e cresce até a floresta; o
        // morro cresce para os lados e para o fundo, e ganha a mesma grama por cima.
        static void ExtendTerrain(Plan plan, GameObject hill)
        {
            float hillWest = ForestWest, hillEast = ForestEast;
            hill.transform.position = new Vector3((hillWest + hillEast) / 2, FloorY / 2, (plan.morroZ + ForestBack) / 2);
            hill.transform.localScale = new Vector3(hillEast - hillWest, FloorY, ForestBack - plan.morroZ);
            hill.GetComponent<Renderer>().sharedMaterial = GroundMaterial("Gramado do Morro", new Color(0.12f, 0.16f, 0.11f),
                new Vector2(hillEast - hillWest, ForestBack - plan.morroZ));

            var ground = GameObject.Find("Chão");
            if (ground == null) return;
            ground.transform.position = new Vector3((ForestWest + ForestEast) / 2, -0.01f, (ForestFront + ForestBack) / 2);
            ground.transform.localScale = new Vector3((ForestEast - ForestWest) / 10f, 1f, (ForestBack - ForestFront) / 10f);
            ground.GetComponent<Renderer>().sharedMaterial = GroundMaterial("Gramado", new Color(0.12f, 0.16f, 0.11f),
                new Vector2(ForestEast - ForestWest, ForestBack - ForestFront));
        }

        // Material de chão grande (o Plane e o cubo do morro têm UV 0..1 na face inteira): a textura
        // de detalhe repete uma vez por metro, como nos modelos do Blender.
        static Material GroundMaterial(string name, Color color, Vector2 meters)
        {
            var mat = EditorTools.ColorMaterial(name, color);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Detail/T_Detail_Grama.png");
            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Detail/T_Detail_Grama_N.png");
            if (albedo == null || bump == null || !mat.HasProperty("_DetailAlbedoMap")) return mat;
            mat.SetTexture("_DetailAlbedoMap", albedo);
            mat.SetTexture("_DetailNormalMap", bump);
            mat.SetTextureScale("_DetailAlbedoMap", meters * 0.5f); // 2 m por repetição: o chão é visto de longe
            mat.SetFloat("_DetailAlbedoMapScale", 0.7f);
            mat.SetFloat("_DetailNormalMapScale", 0.5f);
            mat.SetFloat("_Smoothness", 0.15f);                     // grama úmida: um brilho fraco do luar
            mat.EnableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- O lado de fora ----------

        static void BuildExterior(Plan plan, Transform house)
        {
            var root = Group(house, "Exterior");
            var rng = new System.Random(1927); // o ano da casa (LEVEL_DESIGN_FASE1_MANSAO.md)
            var taken = new List<(Vector2 c, float r)>(); // o que já ocupa o chão (para a vegetação não cair em cima)

            BuildBoundary(plan, Group(root, "Muros e Portões"), taken);
            BuildPaths(Group(root, "Caminhos"), rng, taken);
            BuildYard(Group(root, "Pátio e Construções"), rng, taken);
            BuildLake(Group(root, "Lago"), taken);
            BuildLamps(Group(root, "Luminárias"));
            BuildVegetation(plan, Group(root, "Vegetação"), Group(root, "Floresta"), rng, taken);
            BuildRoad(root);
            BuildFacade(plan, Group(root, "Fachada"));

            foreach (Transform t in root.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            BuildMistLayers(plan, Group(root, "Névoa")); // depois: a névoa gira (MistDrift), não pode ser estática
        }

        static GameObject Put(string path, Transform parent, Vector3 position, float yaw, float scale = 1f)
        {
            var go = Place(path, position, yaw, null);
            go.transform.SetParent(parent);
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
            return go;
        }

        // Enche um trecho reto (de a até b) com módulos de 4 m, esticados para fechar sem fresta.
        static void Run(string path, Transform parent, Vector3 a, Vector3 b, float yawOffset = 0f)
        {
            float length = Vector3.Distance(a, b);
            if (length < 0.3f) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(length / ModuleLength));
            float stretch = length / (n * ModuleLength);
            var dir = (b - a) / length;
            float yaw = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg + yawOffset; // o módulo corre em X: o giro leva +X até dir
            for (int i = 0; i < n; i++)
            {
                var go = Put(path, parent, a + dir * ((i + 0.5f) * length / n), yaw);
                go.transform.localScale = new Vector3(stretch, 1f, 1f);
            }
        }

        // O muro: grade com mureta na frente (com os dois portões), muro de pedra nas laterais e o
        // arrimo do morro no fundo. Os pilares marcam os cantos, os portões e a cada ~12 m.
        static void BuildBoundary(Plan plan, Transform parent, List<(Vector2, float)> taken)
        {
            float gateHalf = GateWidth / 2 + PillarHalf;
            var stops = new List<float> { PropertyWest, -gateHalf, gateHalf, ServiceX - gateHalf, ServiceX + gateHalf, PropertyEast };
            // a frente: grade entre os pilares (de 12 em 12 m), sem fechar os portões
            for (int s = 0; s < stops.Count - 1; s += 2)
            {
                float from = stops[s], to = stops[s + 1];
                int pieces = Mathf.Max(1, Mathf.RoundToInt((to - from) / 12f));
                for (int k = 0; k < pieces; k++)
                {
                    float x0 = from + (to - from) * k / pieces, x1 = from + (to - from) * (k + 1) / pieces;
                    Run(IronFencePath, parent, new Vector3(x0 + PillarHalf, 0, PropertyFront), new Vector3(x1 - PillarHalf, 0, PropertyFront));
                    if (k > 0) Put(WallPillarPath, parent, new Vector3(x0, 0, PropertyFront), 0);
                }
            }
            foreach (float x in new[] { 0f, ServiceX })
            {
                Put(IronGatePath, parent, new Vector3(x, 0, PropertyFront), 0);
                Put(GatePillarPath, parent, new Vector3(x - GateWidth / 2 - PillarHalf, 0, PropertyFront), 0);
                Put(GatePillarPath, parent, new Vector3(x + GateWidth / 2 + PillarHalf, 0, PropertyFront), 0);
            }
            foreach (float x in new[] { PropertyWest, PropertyEast })
            {
                Put(WallPillarPath, parent, new Vector3(x, 0, PropertyFront), 0);
                // as laterais: muro de pedra até o morro, com pilar a cada 12 m
                int pieces = Mathf.RoundToInt((plan.morroZ - PropertyFront) / 12f);
                for (int k = 0; k < pieces; k++)
                {
                    float z0 = PropertyFront + (plan.morroZ - PropertyFront) * k / pieces;
                    float z1 = PropertyFront + (plan.morroZ - PropertyFront) * (k + 1) / pieces;
                    Run(StoneWallPath, parent, new Vector3(x, 0, z0 + PillarHalf), new Vector3(x, 0, z1 - PillarHalf));
                    if (k > 0) Put(WallPillarPath, parent, new Vector3(x, 0, z0), 0);
                }
            }
            // o arrimo do morro, dos dois lados da casa, com as pedras viradas para a frente (-Z)
            float houseWest = plan.comodos.Where(r => r.nivel == 0 && !r.exterior).Min(r => r.x0);
            float houseEast = plan.comodos.Where(r => r.nivel == 0 && !r.exterior && r.z1 >= plan.morroZ).Max(r => r.x1);
            Run(RetainingWallPath, parent, new Vector3(ForestWest, 0, plan.morroZ), new Vector3(houseWest, 0, plan.morroZ), 180f);
            Run(RetainingWallPath, parent, new Vector3(houseEast, 0, plan.morroZ), new Vector3(ForestEast, 0, plan.morroZ), 180f);
            // o muro inteiro ocupa uma faixa de 2 m (nada de árvore nascendo dentro dele)
            for (float x = PropertyWest; x <= PropertyEast; x += 2f) taken.Add((new Vector2(x, PropertyFront), 1.6f));
            for (float z = PropertyFront; z <= plan.morroZ; z += 2f)
            {
                taken.Add((new Vector2(PropertyWest, z), 1.6f));
                taken.Add((new Vector2(PropertyEast, z), 1.6f));
            }
        }

        // O caminho principal (portão → pátio → casa, em curvas suaves), o pátio redondo, o caminho
        // de serviço (portão de serviço → caminhão) e os ramais para o lago e para o serviço.
        static void BuildPaths(Transform parent, System.Random rng, List<(Vector2, float)> taken)
        {
            void Stone(float x, float z)
            {
                Put(PathStonePath, parent, new Vector3(x, 0, z), 90 * rng.Next(4));
                taken.Add((new Vector2(x, z), 1.6f));
            }
            // portão → pátio: a curva desvia no máximo 1,2 m (largo o bastante para três jogadores)
            for (float z = PropertyFront + 1f; z < PatioCenter.z - PatioRadius + 1f; z += 2f)
            {
                float xc = MainPathX(z);
                Stone(xc - 1f, z);
                Stone(xc + 1f, z);
            }
            // o pátio: lajes de 2 m dentro do círculo (menos embaixo da fonte)
            for (int i = -4; i <= 4; i++)
                for (int j = -4; j <= 4; j++)
                {
                    var off = new Vector2(i * 2f, j * 2f);
                    if (off.magnitude > PatioRadius || off.magnitude < 2f) continue;
                    Stone(PatioCenter.x + off.x, PatioCenter.z + off.y);
                }
            // pátio → calçada da casa
            for (float z = PatioCenter.z + PatioRadius + 0.5f; z < -21f; z += 2f)
            {
                Stone(-1f, z);
                Stone(1f, z);
            }
            // portão de serviço → pátio do caminhão
            for (float z = PropertyFront + 1f; z < -21f; z += 2f)
            {
                Stone(ServiceX - 1f, z);
                Stone(ServiceX + 1f, z);
            }
            // ramais: pátio → caminho de serviço (leste) e pátio → doca (oeste)
            for (float x = PatioCenter.x + PatioRadius + 1.5f; x < ServiceX - 1.5f; x += 2f) Stone(x, PatioCenter.z);
            for (float x = PatioCenter.x - PatioRadius - 1.5f; x > LakeCenter.x + 12.5f; x -= 2f) Stone(x, PatioCenter.z);
        }

        static float MainPathX(float z) => 1.2f * Mathf.Sin((z - PropertyFront) * 0.35f);

        // A fonte no meio do pátio, o galpão no canto de serviço, a árvore retorcida, canteiros e ruínas.
        static void BuildYard(Transform parent, System.Random rng, List<(Vector2, float)> taken)
        {
            Put(FountainPath, parent, PatioCenter, 0);
            taken.Add((new Vector2(PatioCenter.x, PatioCenter.z), PatioRadius + 0.5f));

            Put(ShedPath, parent, ShedSpot, -90); // a porta olha para o caminho de serviço (oeste)
            taken.Add((new Vector2(ShedSpot.x, ShedSpot.z), 4f));
            Put(TwistedTreePath, parent, TwistedTreeSpot, 200);
            taken.Add((new Vector2(TwistedTreeSpot.x, TwistedTreeSpot.z), 5f));

            // canteiros abandonados na frente da cozinha e dos lados do pátio
            foreach (var (x, z, yaw) in new[] { (-11f, -19.5f, 0f), (-11f, -15.5f, 0f), (-9.6f, -24.6f, 55f), (9.6f, -24.6f, -55f) })
            {
                Put(FlowerBedPath, parent, new Vector3(x, 0, z), yaw);
                taken.Add((new Vector2(x, z), 1.8f));
            }
            // ruínas: um muro velho que caiu, perto dos limites
            foreach (var (x, z, yaw) in new[] { (-42f, -12f, 35f), (52f, -4f, -20f), (-44f, -42f, 80f), (40f, -2f, 110f) })
            {
                Put(RuinWallPath, parent, new Vector3(x, 0, z), yaw);
                taken.Add((new Vector2(x, z), 3f));
            }
        }

        // O lago: a água escura, a doca saindo da margem leste e o barco amarrado na ponta. A água tem
        // colisão (ninguém anda nela), menos o corredor da doca.
        static void BuildLake(Transform parent, List<(Vector2, float)> taken)
        {
            Put(LakePath, parent, LakeCenter, 0);
            var dockStart = new Vector3(LakeCenter.x + 12f, 0, LakeCenter.z + 0.5f);
            Put(DockPath, parent, dockStart, -90); // a doca avança para -X, lago adentro
            var boat = Put(BoatPath, parent, new Vector3(dockStart.x - 6.4f, -0.1f, dockStart.z + 1.9f), -75);
            boat.transform.rotation *= Quaternion.Euler(2f, 0, -3f); // parado na água, um pouco adernado

            // colisão da água: três caixas em volta do corredor da doca (Z ±1,1 da doca)
            float halfX = 8.2f, halfZ = 5.8f, dockZ = dockStart.z;
            void Water(string name, float x0, float x1, float z0, float z1)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent);
                go.transform.position = new Vector3((x0 + x1) / 2, 0.6f, (z0 + z1) / 2);
                go.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, 1.2f, z1 - z0);
            }
            float west = LakeCenter.x - halfX, east = LakeCenter.x + halfX;
            Water("Água (colisão) sul", west, east, LakeCenter.z - halfZ, dockZ - 1.1f);
            Water("Água (colisão) norte", west, east, dockZ + 1.1f, LakeCenter.z + halfZ);
            Water("Água (colisão) oeste", west, dockStart.x - 7.2f, dockZ - 1.1f, dockZ + 1.1f);
            taken.Add((new Vector2(LakeCenter.x, LakeCenter.z), 11.5f));
        }

        // As luminárias: ao longo do caminho (alternando os lados), em volta do pátio e no caminho
        // de serviço. Uma em cada três tremula. O braço aponta para o caminho.
        static void BuildLamps(Transform parent)
        {
            int n = 0;
            void Lamp(Vector3 at, Vector3 lookAt)
            {
                var d = lookAt - at;
                var go = Put(LampPostPath, parent, at, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                if (n++ % 3 == 1)
                    foreach (var light in go.GetComponentsInChildren<Light>()) light.gameObject.AddComponent<LightFlicker>();
            }
            foreach (float z in new[] { -44f, -41f })
            {
                bool left = z < -42f;
                float xc = MainPathX(z);
                var at = new Vector3(xc + (left ? -3.2f : 3.2f), 0, z);
                Lamp(at, new Vector3(xc, 0, z));
            }
            for (int k = 0; k < 4; k++)
            {
                float a = (45f + 90f * k) * Mathf.Deg2Rad;
                Lamp(PatioCenter + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (PatioRadius + 1.2f), PatioCenter);
            }
            foreach (float z in new[] { -40f, -29f })
                Lamp(new Vector3(ServiceX - 3.2f, 0, z), new Vector3(ServiceX, 0, z));
            Lamp(new Vector3(LakeCenter.x + 12.6f, 0, LakeCenter.z - 1.8f), new Vector3(LakeCenter.x + 10f, 0, LakeCenter.z + 0.5f));
            Lamp(new Vector3(ShedSpot.x - 3.4f, 0, ShedSpot.z + 2.6f), ShedSpot);
        }

        // Vegetação dentro da propriedade (árvores com colisão só no tronco, arbustos, pedras,
        // folhas) e a FLORESTA de fora (sem colisão nem sombra longe do muro: ninguém chega lá, e
        // fica mais leve). Varia espécie, altura e densidade para tapar a vista do resto do mundo.
        static void BuildVegetation(Plan plan, Transform yard, Transform forest, System.Random rng, List<(Vector2 c, float r)> taken)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            bool Free(Vector2 p, float r)
            {
                foreach (var (c, cr) in taken)
                    if ((p - c).sqrMagnitude < (r + cr) * (r + cr)) return false;
                return true;
            }
            // a casa (com folga) e o pátio do caminhão
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var r in plan.comodos)
            {
                x0 = Mathf.Min(x0, r.x0); x1 = Mathf.Max(x1, r.x1);
                z0 = Mathf.Min(z0, r.z0); z1 = Mathf.Max(z1, r.z1);
            }
            bool InHouse(Vector2 p, float pad) => p.x > x0 - pad && p.x < x1 + pad && p.y > z0 - pad && p.y < z1 + pad;
            bool InProperty(Vector2 p) => p.x > PropertyWest && p.x < PropertyEast && p.y > PropertyFront && p.y < plan.morroZ;

            GameObject Tree(Transform parent, Vector2 p, float baseY, bool solid, bool shadows)
            {
                bool pine = rng.NextDouble() < 0.62;
                string path = pine ? PinePaths[rng.Next(PinePaths.Length)] : BroadleafPaths[rng.Next(BroadleafPaths.Length)];
                var go = Put(path, parent, new Vector3(p.x, baseY, p.y), R(0, 360), R(0.8f, 1.3f));
                if (solid)
                {
                    var c = go.AddComponent<CapsuleCollider>();
                    c.radius = 0.35f;
                    c.height = 4f;
                    c.center = new Vector3(0, 2f, 0);
                }
                if (!shadows)
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                taken.Add((p, 2f));
                return go;
            }

            // dentro do muro: árvores mais esparsas nas laterais (longe dos caminhos e da casa)
            for (int tries = 0, placed = 0; tries < 4000 && placed < 46; tries++)
            {
                var p = new Vector2(R(PropertyWest + 2.5f, PropertyEast - 2.5f), R(PropertyFront + 2.5f, plan.morroZ - 2.5f));
                bool side = p.x < -16f || p.x > 26f;
                if (!side || InHouse(p, 5f) || !Free(p, 3.2f)) continue;
                Tree(yard, p, 0f, solid: true, shadows: true);
                placed++;
            }
            // arbustos: ao pé do muro, em volta do pátio, na frente da casa e ao pé do arrimo
            for (int tries = 0, placed = 0; tries < 6000 && placed < 110; tries++)
            {
                Vector2 p;
                switch (tries % 4)
                {
                    case 0: p = new Vector2(R(PropertyWest + 1.5f, PropertyEast - 1.5f), PropertyFront + R(1.4f, 3f)); break;
                    case 1:
                        float a = R(0, Mathf.PI * 2);
                        p = new Vector2(PatioCenter.x, PatioCenter.z) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(PatioRadius + 1.3f, PatioRadius + 2.6f);
                        break;
                    case 2: p = new Vector2(R(x0 - 2f, x1 + 2f), R(-23.5f, -21.5f)); break;
                    default: p = new Vector2(R(PropertyWest + 1f, PropertyEast - 1f), plan.morroZ - R(0.9f, 2.4f)); break;
                }
                if (InHouse(p, 0.6f) || !Free(p, 0.7f)) continue;
                var go = Put(BushPaths[rng.Next(BushPaths.Length)], yard, new Vector3(p.x, 0, p.y), R(0, 360), R(0.8f, 1.5f));
                taken.Add((p, 0.8f));
                placed++;
            }
            // pedras e folhas caídas espalhadas pela propriedade
            for (int tries = 0, placed = 0; tries < 3000 && placed < 40; tries++)
            {
                var p = new Vector2(R(PropertyWest + 1.5f, PropertyEast - 1.5f), R(PropertyFront + 1.5f, plan.morroZ - 1.5f));
                if (InHouse(p, 1f) || !Free(p, 0.9f)) continue;
                bool big = rng.NextDouble() < 0.25;
                Put(RockPaths[big ? 1 : 0], yard, new Vector3(p.x, 0, p.y), R(0, 360), R(0.7f, 1.3f));
                taken.Add((p, big ? 1.2f : 0.5f));
                placed++;
            }
            for (int tries = 0, placed = 0; tries < 2000 && placed < 45; tries++)
            {
                var p = new Vector2(R(PropertyWest + 1f, PropertyEast - 1f), R(PropertyFront + 1f, plan.morroZ - 1f));
                if (InHouse(p, 0.5f)) continue;
                Put(LeavesPath, yard, new Vector3(p.x, 0, p.y), R(0, 360), R(0.8f, 1.6f)); // pode cair no caminho (é folha)
                placed++;
            }

            // a floresta: do lado de fora do muro e lá em cima do morro, bem fechada
            var road = new Rect(ForestWest, PropertyFront - 8.5f, ForestEast - ForestWest, 5f);
            for (int tries = 0, placed = 0; tries < 30000 && placed < 620; tries++)
            {
                var p = new Vector2(R(ForestWest, ForestEast), R(ForestFront, ForestBack));
                bool hill = p.y > plan.morroZ + 1f;
                if (!hill && (InProperty(p) || p.x > PropertyWest - 2f && p.x < PropertyEast + 2f && p.y > PropertyFront - 2f && p.y < plan.morroZ)) continue;
                if (road.Contains(p) || InHouse(p, 4f) || !Free(p, 1.9f)) continue;
                if (!hill && Mathf.Abs(p.y - plan.morroZ) < 1.5f) continue;     // não na beirada do arrimo
                // perto do muro, a sombra ajuda a ler o limite; longe, só pesa
                float edge = Mathf.Min(Mathf.Abs(p.x - PropertyWest), Mathf.Abs(p.x - PropertyEast), Mathf.Abs(p.y - PropertyFront));
                Tree(forest, p, hill ? FloorY : 0f, solid: false, shadows: edge < 10f || InHouse(p, 12f));
                placed++;
            }
            // arbustos e pedras do lado de fora, junto do muro (tapam a base dos troncos)
            for (int tries = 0, placed = 0; tries < 4000 && placed < 90; tries++)
            {
                var p = new Vector2(R(PropertyWest - 8f, PropertyEast + 8f), R(PropertyFront - 9f, plan.morroZ));
                if (p.x > PropertyWest - 1f && p.x < PropertyEast + 1f && p.y > PropertyFront - 1f) continue;
                if (road.Contains(p) || !Free(p, 0.6f)) continue;
                string path = rng.NextDouble() < 0.7 ? BushPaths[rng.Next(2)] : RockPaths[rng.Next(2)];
                Put(path, forest, new Vector3(p.x, 0, p.y), R(0, 360), R(0.9f, 1.7f));
                taken.Add((p, 0.7f));
                placed++;
            }
        }

        // ---------- Fachada ----------

        // Um ponto (x, z) num nível está DENTRO da casa? Conta o cômodo daquele nível e, no nível de
        // cima, o vão dos cômodos de pé-direito duplo do térreo. Varanda e quintal (exterior) não contam.
        static bool InsideHouse(Plan plan, int level, float x, float z)
        {
            foreach (var r in plan.comodos)
            {
                if (r.exterior) continue;
                bool here = r.nivel == level || (level == 1 && r.nivel == 0 && r.duplo);
                if (here && x > r.x0 && x < r.x1 && z > r.z0 && z < r.z1) return true;
            }
            return false;
        }

        // A casca de fora da casa. Cada módulo de parede da planta é testado dos dois lados: se um
        // lado é cômodo e o outro é rua (ou o alto do morro), ele ganha um módulo de fachada do lado
        // de fora, do mesmo tamanho e escala (o térreo atrás do morro fica enterrado: nada). Depois:
        // a cornija em cima da última fileira e o cunhal de pedra em cada ponta solta (as quinas).
        static void BuildFacade(Plan plan, Transform parent)
        {
            var ends = new List<(Vector3 point, Vector3 normal, float y, float height, float yaw)>();
            foreach (var w in plan.paredes)
            {
                // a fileira de cima dos cômodos de pé-direito duplo (Y 3,2) já está acima do telhado
                // do térreo: testa como o nível de cima (o cômodo vizinho do térreo não a esconde)
                int level = w.y > 0.01f ? 1 : 0;
                var rot = Quaternion.Euler(0, w.giro, 0);
                Vector3 forward = rot * Vector3.forward, along = rot * Vector3.right;
                var center = new Vector3(w.x, w.y, w.z);
                Vector3? outward = null;
                foreach (float side in new[] { 1f, -1f })
                {
                    var probe = center + forward * (side * 0.3f);
                    var back = center - forward * (side * 0.3f);
                    bool buried = level == 0 && probe.z > plan.morroZ - 0.01f;
                    if (!buried && !InsideHouse(plan, level, probe.x, probe.z) && InsideHouse(plan, level, back.x, back.z))
                        outward = forward * side;
                }
                if (outward == null) continue;
                var n = outward.Value;
                float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg; // o +Z do módulo vai para fora
                float height = WallHeight * w.escalaY;
                bool ground = w.y < 0.01f || (level == 1 && (center + n * 0.3f).z > plan.morroZ);
                bool window = w.largura > 1.9f && Mathf.Abs(Mathf.RoundToInt((w.x + w.z) / 2f)) % 2 == 0;
                string art = w.tipo switch
                {
                    "porta" => "FachadaPorta",
                    "dupla" => "FachadaDupla",
                    _ => (window ? "FachadaJanela" : "FachadaLisa") + (ground ? "" : "Alta"),
                };
                var face = Put(FacadePath(art), parent, center + n * (WallThickness / 2 + 0.002f), yaw);
                face.transform.localScale = new Vector3(w.largura / WallWidth, w.escalaY, 1f);

                // a cornija: só onde não há outra fileira de parede em cima desta
                bool top = !plan.paredes.Any(o => Mathf.Abs(o.x - w.x) < 0.01f && Mathf.Abs(o.z - w.z) < 0.01f
                                                  && o.giro == w.giro && o.y > w.y + 0.1f);
                if (top)
                {
                    var cornice = Put(FacadePath("Cornija"), parent, center + Vector3.up * height + n * (WallThickness / 2), yaw);
                    cornice.transform.localScale = new Vector3(w.largura / WallWidth, 1f, 1f);
                }
                foreach (float s in new[] { -1f, 1f })
                    ends.Add((center + along * (s * w.largura / 2), n, w.y, height + (top ? 0.84f : 0f), yaw));
            }

            // Cunhal: na ponta de fachada que não emenda em outra fachada da mesma direção (a quina).
            var placed = new List<Vector3>();
            foreach (var e in ends)
            {
                bool joined = ends.Count(o => (o.point - e.point).sqrMagnitude < 0.01f && Vector3.Dot(o.normal, e.normal) > 0.9f
                                              && Mathf.Abs(o.y - e.y) < 0.01f) > 1;
                if (joined || placed.Any(p => (p - e.point).sqrMagnitude < 0.01f)) continue;
                placed.Add(e.point);
                var quoin = Put(FacadePath("Cunhal"), parent, e.point, e.yaw);
                quoin.transform.localScale = new Vector3(1f, e.height / WallHeight, 1f);
            }
        }

        // ---------- Névoa rasteira ----------

        // Camadas de névoa (MistPath) em duas alturas: sobre o lago (a mais densa), ao longo do muro
        // por dentro e por fora (esconde onde a floresta começa) e no pé do arrimo. Cada placa gira e
        // escorrega de um jeito, então as camadas não se repetem.
        static void BuildMistLayers(Plan plan, Transform parent)
        {
            var rng = new System.Random(7);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            void Sheet(float x, float z, float y, float scale) =>
                Put(MistPath, parent, new Vector3(x, y, z), R(0, 360), scale);

            for (int i = 0; i < 9; i++)                                       // o lago
            {
                float a = i * 40f * Mathf.Deg2Rad;
                Sheet(LakeCenter.x + Mathf.Cos(a) * R(0, 6), LakeCenter.z + Mathf.Sin(a) * R(0, 4), R(0.25f, 0.9f), R(0.9f, 1.3f));
            }
            for (float x = PropertyWest; x <= PropertyEast; x += 8f)           // frente: dos dois lados da grade
            {
                Sheet(x + R(-2, 2), PropertyFront + R(2, 5), R(0.3f, 0.8f), R(0.7f, 1f));
                Sheet(x + R(-2, 2), PropertyFront - R(10, 16), R(0.4f, 1.2f), R(1f, 1.4f));
            }
            for (float z = PropertyFront; z <= plan.morroZ; z += 8f)            // laterais
                foreach (float x in new[] { PropertyWest, PropertyEast })
                {
                    float sign = x < 0 ? -1 : 1;
                    Sheet(x - sign * R(2, 5), z + R(-2, 2), R(0.3f, 0.8f), R(0.7f, 1f));
                    Sheet(x + sign * R(6, 12), z + R(-2, 2), R(0.4f, 1.2f), R(1f, 1.4f));
                }
            for (float x = PropertyWest + 4; x < PropertyEast; x += 10f)        // o pé do arrimo e o alto do morro
            {
                Sheet(x + R(-2, 2), plan.morroZ - R(2, 4), R(0.3f, 0.7f), R(0.7f, 1f));
                Sheet(x + R(-3, 3), plan.morroZ + R(8, 20), FloorY + R(0.4f, 1.2f), R(1f, 1.4f));
            }
        }

        // A estrada de terra que passa na frente do portão (de onde o caminhão veio).
        static void BuildRoad(Transform parent)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
            road.name = "Estrada de Terra";
            road.transform.SetParent(parent);
            road.transform.position = new Vector3((ForestWest + ForestEast) / 2, 0.005f, PropertyFront - 6f);
            road.transform.localScale = new Vector3((ForestEast - ForestWest) / 10f, 1f, 0.5f);
            Object.DestroyImmediate(road.GetComponent<Collider>()); // o chão de baixo já segura
            var mat = GroundMaterial("Estrada de Terra", new Color(0.16f, 0.13f, 0.1f), new Vector2(ForestEast - ForestWest, 5f));
            road.GetComponent<Renderer>().sharedMaterial = mat;
            road.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
