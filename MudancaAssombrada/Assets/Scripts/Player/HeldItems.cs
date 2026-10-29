using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // O que a hotbar tem NA MÃO, visto no boneco (fica no Player e roda em TODOS os computadores:
    // os colegas veem a sua lanterna e a sua bengala).
    //   - LANTERNA NA MÃO: o braço direito CARREGA a lanterna baixo, à frente do quadril, cotovelo pouco
    //     dobrado e punho quase reto (em "braços" a partir do ombro: cada personagem na altura e no
    //     alcance dele; acompanha metade do olhar), a mão fecha no cabo (camada "Lanterna" do Animator:
    //     a PegaLanterna, tools/blender/build_pega_lanterna.py) e gira para a lanterna apontar para onde
    //     o dono olha (o olhar vai pela rede: Flashlight.AimRotation). A luz sai da lente.
    //     O ombro é SUAVIZADO: correndo, ele sobe e desce ~10 cm com o clipe; a mão, ~2,5 cm.
    //     Na SUA tela, o FirstPersonArm esconde o braço e deixa só a ponta da lanterna aparecer.
    //   - LANTERNA NO COLETE (carregando um móvel): o modelo vai para o peito; o braço segue a animação.
    //   - PILHAS NA MÃO: o mesmo braço com IK, mais alto e mais perto do corpo (na altura da barriga,
    //     como quem olha o pacote); a mão fecha na aba da cartela (a mesma pega da lanterna) e a cartela
    //     fica deitada, inclinada para o rosto. Com um móvel nos braços, ela fica guardada.
    //     Na SUA tela, o FirstPersonArm leva a cartela para o canto de baixo.
    //   - BENGALA (Cida): a bengala do modelo (SM_PRP_Bengala, presa na mão esquerda) some quando ela
    //     escolhe outro espaço, e a mão solta a pega (camada "Bengala" do DollWalk).
    // Só visual: quem decide o que está na mão é a Hotbar, e onde a lanterna está é o Flashlight.
    // As contas (ponto da mão, alcance, cotovelo, suavizar) ficam em HandPoseRules, com testes.
    //
    // Roda DEPOIS do Flashlight (ordem 100), que vira a luz no LateUpdate, e ANTES do FirstPersonBody
    // (200), que copia a pose para o corpo que você vê, e do FirstPersonArm (300).
    [DefaultExecutionOrder(100)]
    public class HeldItems : MonoBehaviour
    {
        [Tooltip("O modelo da lanterna (FBX). É criado como filho do Player e levado até a mão a cada quadro.")]
        [SerializeField] GameObject flashlightModel;
        [Tooltip("O ponto do modelo que fica na palma (o meio do cabo), em metros, no espaço do modelo " +
                 "(build_lanterna: tudo no eixo a 5,5 cm do chão, lente em +Z a 0,11, cabo de −0,12 a +0,06).")]
        [SerializeField] Vector3 flashlightGrip = new(0f, 0.055f, -0.03f);
        [Tooltip("De onde sai a luz, no espaço do modelo: um pouco à frente da lente (o braço e a própria " +
                 "lanterna ficam atrás da luz e não fazem sombra nela).")]
        [SerializeField] Vector3 flashlightLens = new(0f, 0.055f, 0.14f);
        [Header("No cinto (carregando um móvel)")]
        [Tooltip("De que lado da cintura a lanterna fica: +1 = DIREITO do personagem, −1 = esquerdo " +
                 "(o Tiago tem a bolsa no quadril esquerdo).")]
        [SerializeField] float beltSide = 1f;
        [Tooltip("Metros para a frente (+) ou para trás (−) do meio do quadril, bem do lado da cintura.")]
        [SerializeField] float beltForward = 0.02f;
        [Tooltip("Graus para baixo: no cinto ela aponta um pouco para o chão, à frente.")]
        [SerializeField] float beltTilt = 10f;
        [Tooltip("Folga entre o corpo e o meio do cabo (o raio do cabo é 3,2 cm).")]
        [SerializeField] float beltGap = 0.036f;
        [Tooltip("Cor e força do brilho da lente acesa (3200 K, como a luz). Os colegas veem de longe quem " +
                 "está com a luz ligada, e a lente pisca junto quando a bateria está fraca.")]
        [SerializeField, ColorUsage(false, true)] Color lensGlow = new Color(1f, 0.86f, 0.68f) * 4f;

        [Header("Pilhas na mão")]
        [Tooltip("O modelo da cartela de pilhas (FBX). Criado como filho do Player, como a lanterna.")]
        [SerializeField] GameObject batteryModel;
        [Tooltip("O ponto da cartela que fica no punho: o meio da aba de pendurar, no espaço do modelo " +
                 "(build_pilhas: cartão de 3 mm, a aba atrás, de −0,05 a −0,072 em Z; os polos para +Z).")]
        [SerializeField] Vector3 batteryGrip = new(0f, 0.0015f, -0.062f);
        [Tooltip("Onde a mão fica em relação ao ombro, em BRAÇOS (direita, cima, frente): mais alta e mais " +
                 "perto do meio do corpo que a lanterna, na altura da barriga.")]
        [SerializeField] Vector3 batteryCarry = new(0.05f, -0.65f, 0.5f);
        [Tooltip("Graus para CIMA (negativo = a frente da cartela sobe): as pilhas viram para o rosto de quem segura.")]
        [SerializeField] float batteryTilt = -30f;
        [Tooltip("Graus para dentro (para a esquerda do personagem): a cartela vira para o meio do corpo.")]
        [SerializeField] float batteryTurn = -20f;

        [Header("Braço da lanterna (IK)")]
        [Tooltip("Onde a mão fica em relação ao ombro, em BRAÇOS (direita, cima, frente × o tamanho do braço " +
                 "de cada um): baixa, ao lado do quadril, um pouco à frente e afastada do corpo. Medido em batch: " +
                 "o braço fica a 5-18 cm do corpo (junto ao corpo, o antebraço entrava 3 cm no Tiago e encostava " +
                 "na barriga do Marcelo) e o punho dobra ~30° olhando reto (na altura do ombro, 60-80°).")]
        [SerializeField] Vector3 carry = new(0.2f, -0.83f, 0.27f);
        [Tooltip("Quanto do olhar para cima/baixo o braço acompanha (o punho faz o resto).")]
        [SerializeField, Range(0f, 1f)] float armFollowsLook = 0.5f;
        [Tooltip("Firmeza: meia-vida (s) do ombro suavizado. Correndo, o ombro sacode com a animação; " +
                 "a mão segue só o movimento de verdade. 0 = sem suavizar (o braço balança).")]
        [SerializeField] float steadiness = 0.15f;
        [Tooltip("Para onde o cotovelo dobra, no espaço do corpo: para trás e um pouco para fora (rente ao " +
                 "corpo). Mais para fora afasta o cotovelo; mais para trás o aproxima, mas o punho dobra mais.")]
        [SerializeField] Vector3 elbowHint = new(0.45f, 0f, -1f);

        Hotbar hotbar;
        Flashlight flashlight;
        CharacterSwitcher character;
        GameObject lantern;
        GameObject batteries;

        // Achados no modelo atual (refeitos quando o personagem muda).
        DollWalk walk;
        Transform hips, socket;
        float beltWidth; // até onde vai o corpo na altura do cinto, do lado da lanterna (m)
        ArmBones arm;
        Vector3 steadyShoulder;                   // o ombro suavizado, no espaço do corpo
        bool hasSteadyShoulder;
        readonly List<Renderer> cane = new();

        // O braço do modelo (que os colegas veem) foi erguido neste quadro? O FirstPersonArm só ergue
        // o da cópia junto.
        public bool ArmRaised { get; private set; }
        public DollWalk Walk => walk;
        // Onde está o cabo da lanterna neste quadro (o FirstPersonArm parte daqui ao levar para a tela).
        public Vector3 LanternGrip { get; private set; }
        // A cartela de pilhas está na mão neste quadro (o braço erguido é para ela, não para a lanterna)?
        public bool BatteryInHand { get; private set; }
        // Onde está a aba da cartela neste quadro (o FirstPersonArm parte daqui ao levar para a tela).
        public Vector3 BatteryGrip { get; private set; }

        void Awake()
        {
            hotbar = GetComponent<Hotbar>();
            flashlight = GetComponent<Flashlight>();
            character = GetComponent<CharacterSwitcher>();
            if (flashlightModel != null)
            {
                // Um vazio por fora: o FBX chega com a rotação da importação (90°, 180°) na raiz, e girar
                // a raiz dele direto deixaria a lanterna de lado. Dentro do vazio, a frente (a lente) é +Z.
                lantern = new GameObject("Lanterna (na mão)");
                lantern.transform.SetParent(transform, false);
                Instantiate(flashlightModel, lantern.transform, false);
                lantern.SetActive(false);
                lensMaterial = GlowingLens(lantern);
            }
            if (batteryModel != null)
            {
                // O mesmo vazio por fora (a rotação da importação fica dentro dele).
                batteries = new GameObject("Pilhas (na mão)");
                batteries.transform.SetParent(transform, false);
                Instantiate(batteryModel, batteries.transform, false);
                batteries.SetActive(false);
            }
        }

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        Material lensMaterial; // cópia só desta lanterna: as do caminhão não acendem

        // Troca o material "Lente" (do Blender) por uma cópia com brilho ligado.
        static Material GlowingLens(GameObject model)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].name.Contains("Lente")) continue;
                    var glow = new Material(materials[i]) { name = "Lente (acesa)" };
                    glow.EnableKeyword("_EMISSION");
                    glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    glow.SetColor(EmissionColorId, Color.black);
                    materials[i] = glow;
                    r.sharedMaterials = materials;
                    return glow;
                }
            }
            return null;
        }

        void UpdateLens()
        {
            if (lensMaterial == null) return;
            float glow = FlashlightBeamRules.LensGlow(flashlight.IsOn, flashlight.LightLevel);
            lensMaterial.SetColor(EmissionColorId, lensGlow * glow);
        }

        void OnDestroy()
        {
            if (lensMaterial != null) Destroy(lensMaterial);
        }

        void LateUpdate()
        {
            ArmRaised = false;
            BatteryInHand = false;
            if (hotbar == null || character == null) return;
            if (character.Walk != walk) FindParts(character.Walk);

            // Bengala: aparece e "pega" só com o espaço dela escolhido.
            bool caneInHand = hotbar.InHand == HotbarItem.Cane;
            foreach (var r in cane)
                if (r != null) r.enabled = caneInHand;
            if (walk != null) walk.HoldsCane = caneInHand && character.Current.canTap;

            if (lantern == null || flashlight == null) return;
            var place = flashlight.Has ? flashlight.Place : FlashlightPlace.Stowed;
            bool batteryChosen = batteries != null && HotbarRules.ShowsBatteryInHand(hotbar.InHand, hotbar.HandsBusy);
            // A mão fecha (camada "Lanterna") para a lanterna e para a aba da cartela.
            if (walk != null) walk.HoldsFlashlight = place == FlashlightPlace.Hand || batteryChosen;

            float weight = walk != null ? walk.FlashlightWeight : 0f;
            UpdateBatteries(batteryChosen, weight);

            Quaternion aim = flashlight.AimRotation;
            if (place == FlashlightPlace.Hand && weight > 0.01f && arm != null)
            {
                RaiseArm(aim, weight, carry, armFollowsLook);
                ArmRaised = true;
            }

            if (place == FlashlightPlace.Stowed || (place == FlashlightPlace.Chest ? hips == null : !ArmRaised && socket == null))
            {
                lantern.SetActive(false);
                return;
            }
            lantern.SetActive(true);
            UpdateLens();
            if (place == FlashlightPlace.Chest)
            {
                // Bem do lado da cintura (direita), encostada no corpo; aponta para a frente e um pouco
                // para baixo, e a luz sai dela (para os colegas e no seu reflexo, é o mesmo lugar).
                Quaternion belt = transform.rotation * Quaternion.Euler(beltTilt, 0f, 0f);
                Vector3 point = hips.position + transform.right * (Mathf.Sign(beltSide) * (beltWidth + beltGap))
                              + transform.forward * beltForward;
                PlaceLantern(point, belt, moveBeam: true);
            }
            else
                PlaceLantern(ArmRaised ? arm.FistCenter() : socket.position, aim, moveBeam: ArmRaised && weight > 0.99f);
            // Onde ela fica no CORPO: o FirstPersonArm pode levá-la para a sua tela depois, e o espelho
            // devolve para cá enquanto desenha o reflexo (ShowHeldOnBody).
            bodyPose = new Pose(lantern.transform.position, lantern.transform.rotation);
        }

        // A cartela de pilhas: o braço sobe para a barriga e a mão fecha na aba; a cartela fica deitada,
        // um pouco virada para dentro e inclinada para o rosto (não segue o olhar: quem segura um pacote
        // olha para ele, não aponta).
        void UpdateBatteries(bool chosen, float weight)
        {
            bool show = chosen && weight > 0.01f && (arm != null || socket != null);
            if (batteries.activeSelf != show) batteries.SetActive(show);
            if (!show) return;
            BatteryInHand = true;
            Quaternion facing = transform.rotation * Quaternion.Euler(batteryTilt, batteryTurn, 0f);
            if (arm != null)
            {
                RaiseArm(facing, weight, batteryCarry, follow: 0f);
                ArmRaised = true;
            }
            // A aba fica na beira do punho (do lado do indicador), não no meio: a cartela é pequena, e na
            // luva do Marcelo (26 cm) ela sumia dentro da mão.
            Vector3 fist = arm != null ? arm.FistCenter() + facing * Vector3.forward * arm.FistHalfWidth() : socket.position;
            PlaceBatteries(fist, facing);
            bodyPose = new Pose(batteries.transform.position, batteries.transform.rotation);
        }

        // Põe a aba da cartela no ponto "fist", com a cartela virada para "rotation".
        public void PlaceBatteries(Vector3 fist, Quaternion rotation)
        {
            BatteryGrip = fist;
            batteries.transform.SetPositionAndRotation(fist - rotation * batteryGrip, rotation);
        }

        // ---------- Espelho ----------

        Pose bodyPose, viewPose; // o que está na mão: na mão do corpo / no lugar da sua tela (primeira pessoa)
        bool shownOnBody;

        // O que está na mão neste quadro (a lanterna erguida/no cinto ou a cartela; nunca os dois).
        GameObject Shown => BatteryInHand ? batteries : lantern != null && lantern.activeSelf ? lantern : null;

        // O espelho mostra o SEU corpo (o que os colegas veem), não a primeira pessoa: enquanto a câmera
        // do reflexo desenha, o que está na mão volta para a mão do corpo; depois, para o lugar da tela.
        // Chamado pelo MirrorReflection (via FirstPersonArm), só no dono.
        public void ShowHeldOnBody(bool onBody)
        {
            var shown = Shown;
            if (shown == null || onBody == shownOnBody) return;
            var t = shown.transform;
            if (onBody)
            {
                viewPose = new Pose(t.position, t.rotation);
                t.SetPositionAndRotation(bodyPose.position, bodyPose.rotation);
            }
            else t.SetPositionAndRotation(viewPose.position, viewPose.rotation);
            shownOnBody = onBody;
        }

        // O braço do modelo: a mão no ponto "fraction" (em braços) a partir do ombro suavizado, o braço
        // inclinado com "follow" do olhar; o punho aponta o item para "aim" (a lanterna, para onde o
        // dono olha; a cartela, para o rosto).
        void RaiseArm(Quaternion aim, float weight, Vector3 fraction, float follow)
        {
            Vector3 shoulderLocal = transform.InverseTransformPoint(arm.Upper.position);
            steadyShoulder = hasSteadyShoulder
                ? HandPoseRules.Damp(steadyShoulder, shoulderLocal, steadiness, Time.deltaTime) : shoulderLocal;
            hasSteadyShoulder = true;
            Vector3 shoulder = transform.TransformPoint(steadyShoulder);
            float lookPitch = Vector3.SignedAngle(transform.forward, aim * Vector3.forward, transform.right);
            Quaternion frame = transform.rotation * Quaternion.Euler(HandPoseRules.ArmPitch(lookPitch, follow), 0f, 0f);
            Vector3 target = HandPoseRules.CarryPoint(shoulder, frame, arm.Reach, fraction);
            arm.Aim(target, aim * Vector3.forward, transform.TransformDirection(elbowHint), weight);
        }

        // Põe o cabo da lanterna no ponto "fist", apontando para "rotation". moveBeam: a luz sai da
        // lente (com a lanterna erguida; nos colegas, assim a luz também segue o olhar da rede).
        public void PlaceLantern(Vector3 fist, Quaternion rotation, bool moveBeam)
        {
            LanternGrip = fist;
            lantern.transform.SetPositionAndRotation(fist - rotation * flashlightGrip, rotation);
            if (moveBeam) flashlight.Beam.SetPositionAndRotation(lantern.transform.TransformPoint(flashlightLens), rotation);
        }

        // O modelo mudou (outro personagem, ou o boneco de blocos): acha o braço, o quadril e a bengala.
        // Procura no modelRoot inteiro: a cópia de primeira pessoa também tem bengala.
        void FindParts(DollWalk current)
        {
            walk = current;
            hasSteadyShoulder = false;
            hips = socket = null;
            arm = null;
            cane.Clear();
            if (walk == null) return;

            foreach (var r in walk.transform.parent.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("Bengala")) cane.Add(r);

            socket = FindChild(walk.transform, "SOC_Hand_R");
            var animator = walk.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;
            arm = ArmBones.From(animator);
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            MeasureBelt(animator);
        }

        // Mede o corpo na altura do cinto (o osso do quadril), uma vez por personagem: até onde ele vai
        // para o lado da lanterna. Sem os braços (eles também passam ali). HandPoseRules.BodySide.
        // Só a faixa do meio (frente-trás) conta: a barriga do Marcelo e a bolsa do Tiago ficam fora.
        void MeasureBelt(Animator animator)
        {
            beltWidth = 0f;
            if (hips == null) return;
            var armBones = new HashSet<Transform>();
            foreach (var side in new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm })
            {
                var top = animator.GetBoneTransform(side);
                if (top != null) foreach (var t in top.GetComponentsInChildren<Transform>(true)) armBones.Add(t);
            }

            float hipsZ = transform.InverseTransformPoint(hips.position).z;
            var sides = new List<Vector3>();  // X para a direita (esquerda = negativo)
            var baked = new Mesh();
            foreach (var skin in walk.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.BakeMesh(baked, true);
                var vertices = baked.vertices;
                var weights = skin.sharedMesh.boneWeights;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i < weights.Length && armBones.Contains(skin.bones[weights[i].boneIndex0])) continue;
                    Vector3 p = transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                    if (Mathf.Abs(p.z - hipsZ - beltForward) <= 0.06f) sides.Add(p); // só o lado, perto do ponto
                }
            }
            Destroy(baked);

            float height = transform.InverseTransformPoint(hips.position).y;
            beltWidth = HandPoseRules.BodySide(sides.ToArray(), height, band: 0.04f, side: Mathf.Sign(beltSide));
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // ---------- Os ossos do braço direito e o IK ----------

        public class ArmBones
        {
            public Transform Upper, Lower, Hand, IndexKnuckle, LittleKnuckle;
            public Transform[][] Fingers; // indicador a mindinho: proximal, média, distal

            public float Reach => Vector3.Distance(Upper.position, Lower.position) + Vector3.Distance(Lower.position, Hand.position);

            static readonly HumanBodyBones[] FingerStarts =
            {
                HumanBodyBones.RightIndexProximal, HumanBodyBones.RightMiddleProximal,
                HumanBodyBones.RightRingProximal, HumanBodyBones.RightLittleProximal,
            };

            public static ArmBones From(Animator animator)
            {
                var arm = new ArmBones
                {
                    Upper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                    Lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                    Hand = animator.GetBoneTransform(HumanBodyBones.RightHand),
                    IndexKnuckle = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                    LittleKnuckle = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
                };
                if (arm.Upper == null || arm.Lower == null || arm.Hand == null) return null;
                var fingers = new List<Transform[]>();
                foreach (var start in FingerStarts)
                {
                    var chain = new[] { animator.GetBoneTransform(start), animator.GetBoneTransform(start + 1), animator.GetBoneTransform(start + 2) };
                    if (System.Array.TrueForAll(chain, t => t != null)) fingers.Add(chain);
                }
                arm.Fingers = fingers.ToArray();
                return arm;
            }

            // IK de dois ossos até "target" e o furo do punho virado para "forward", por cima da pose do
            // Animator deste quadro ("weight": a pose entrando ou saindo). O giro que falta se divide como
            // num braço de verdade (HandPoseRules.SplitTwist): o que é em volta do antebraço, o antebraço
            // gira (o pulso "vira"); só o resto dobra o punho. handWeight 0: só o braço (para medir).
            public void Aim(Vector3 target, Vector3 forward, Vector3 hint, float weight, float handWeight = 1f)
            {
                // O "furo" do punho, por onde passa o cabo: a linha dos nós dos dedos, do mindinho para o
                // indicador. Guardado no espaço da mão ANTES do IK, para depois virar esse furo para a frente.
                Vector3 hole = IndexKnuckle != null && LittleKnuckle != null
                    ? IndexKnuckle.position - LittleKnuckle.position : Hand.up;
                Vector3 holeLocal = Quaternion.Inverse(Hand.rotation) * hole.normalized;

                float upper = Vector3.Distance(Upper.position, Lower.position);
                float lower = Vector3.Distance(Lower.position, Hand.position);
                target = HandPoseRules.ClampToReach(Upper.position, target, upper + lower);
                target = Vector3.Lerp(Hand.position, target, weight);
                Vector3 elbow = HandPoseRules.Elbow(Upper.position, target, upper, lower, hint);

                // Gira o braço para o cotovelo cair no lugar, e o antebraço para a mão cair no alvo.
                Upper.rotation = Quaternion.FromToRotation(Lower.position - Upper.position, elbow - Upper.position) * Upper.rotation;
                Lower.rotation = Quaternion.FromToRotation(Hand.position - Lower.position, target - Lower.position) * Lower.rotation;

                // O giro mínimo para o furo do punho apontar para onde você olha: a parte em volta do
                // antebraço vai para o antebraço; o resto, para o punho.
                Quaternion turn = Quaternion.FromToRotation(Hand.rotation * holeLocal, forward);
                HandPoseRules.SplitTwist(turn, Hand.position - Lower.position, out var twist, out var swing);
                float w = weight * handWeight;
                Quaternion hand = Hand.rotation;
                Lower.rotation = Quaternion.Slerp(Quaternion.identity, twist, w) * Lower.rotation;
                Hand.rotation = Quaternion.Slerp(Quaternion.identity, swing * twist, w) * hand;
            }

            // Metade da largura da mão (do nó do indicador ao do mindinho): do meio do punho até a beira
            // por onde sai o que está preso nele.
            public float FistHalfWidth() =>
                IndexKnuckle != null && LittleKnuckle != null ? Vector3.Distance(IndexKnuckle.position, LittleKnuckle.position) / 2f : 0f;

            // O meio do punho fechado, onde o cabo passa: a média das juntas de cada dedo (nó, meio,
            // ponta do meio) e da ponta estimada (a distal continua mais um tanto na mesma direção).
            // Só os nós ficaria em cima dos dedos; com as pontas, cai dentro da curva.
            public Vector3 FistCenter()
            {
                if (Fingers == null || Fingers.Length == 0) return Hand.position;
                Vector3 sum = Vector3.zero;
                int n = 0;
                foreach (var chain in Fingers)
                {
                    if (chain[0] == null || chain[1] == null || chain[2] == null) continue;
                    Vector3 tip = chain[2].position + (chain[2].position - chain[1].position) * 0.9f;
                    sum += chain[0].position + chain[1].position + chain[2].position + tip;
                    n += 4;
                }
                return n > 0 ? sum / n : Hand.position;
            }
        }
    }
}
