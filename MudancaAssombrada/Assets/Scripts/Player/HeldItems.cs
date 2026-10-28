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
        [Tooltip("No colete: quantos metros à frente do osso do peito.")]
        [SerializeField] float chestForward = 0.16f;
        [Tooltip("Cor e força do brilho da lente acesa (3200 K, como a luz). Os colegas veem de longe quem " +
                 "está com a luz ligada, e a lente pisca junto quando a bateria está fraca.")]
        [SerializeField, ColorUsage(false, true)] Color lensGlow = new Color(1f, 0.86f, 0.68f) * 4f;

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

        // Achados no modelo atual (refeitos quando o personagem muda).
        DollWalk walk;
        Transform chest, socket;
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
            if (hotbar == null || character == null) return;
            if (character.Walk != walk) FindParts(character.Walk);

            // Bengala: aparece e "pega" só com o espaço dela escolhido.
            bool caneInHand = hotbar.InHand == HotbarItem.Cane;
            foreach (var r in cane)
                if (r != null) r.enabled = caneInHand;
            if (walk != null) walk.HoldsCane = caneInHand && character.Current.canTap;

            if (lantern == null || flashlight == null) return;
            var place = flashlight.Has ? flashlight.Place : FlashlightPlace.Stowed;
            if (walk != null) walk.HoldsFlashlight = place == FlashlightPlace.Hand;

            float weight = walk != null ? walk.FlashlightWeight : 0f;
            Quaternion aim = flashlight.AimRotation;
            if (place == FlashlightPlace.Hand && weight > 0.01f && arm != null)
            {
                RaiseArm(aim, weight);
                ArmRaised = true;
            }

            if (place == FlashlightPlace.Stowed || (place == FlashlightPlace.Chest ? chest == null : !ArmRaised && socket == null))
            {
                lantern.SetActive(false);
                return;
            }
            lantern.SetActive(true);
            UpdateLens();
            if (place == FlashlightPlace.Chest)
                PlaceLantern(chest.position + transform.forward * chestForward, flashlight.Beam.rotation, moveBeam: false);
            else
                PlaceLantern(ArmRaised ? arm.FistCenter() : socket.position, aim, moveBeam: ArmRaised && weight > 0.99f);
            // Onde ela fica no CORPO: o FirstPersonArm pode levá-la para a sua tela depois, e o espelho
            // devolve para cá enquanto desenha o reflexo (ShowLanternOnBody).
            bodyLantern = new Pose(lantern.transform.position, lantern.transform.rotation);
        }

        // ---------- Espelho ----------

        Pose bodyLantern, viewLantern; // na mão do corpo / no lugar da sua tela (primeira pessoa)
        bool lanternOnBody;

        // O espelho mostra o SEU corpo (o que os colegas veem), não a primeira pessoa: enquanto a câmera
        // do reflexo desenha, a lanterna volta para a mão do corpo; depois, para o lugar da tela.
        // Chamado pelo MirrorReflection (via FirstPersonArm), só no dono.
        public void ShowLanternOnBody(bool onBody)
        {
            if (lantern == null || !lantern.activeSelf || onBody == lanternOnBody) return;
            var t = lantern.transform;
            if (onBody)
            {
                viewLantern = new Pose(t.position, t.rotation);
                t.SetPositionAndRotation(bodyLantern.position, bodyLantern.rotation);
            }
            else t.SetPositionAndRotation(viewLantern.position, viewLantern.rotation);
            lanternOnBody = onBody;
        }

        // O braço do modelo: a mão abaixo e à frente do ombro suavizado (carregar natural), o braço
        // inclinado com parte do olhar; o punho aponta a lanterna para onde o dono olha.
        void RaiseArm(Quaternion aim, float weight)
        {
            Vector3 shoulderLocal = transform.InverseTransformPoint(arm.Upper.position);
            steadyShoulder = hasSteadyShoulder
                ? HandPoseRules.Damp(steadyShoulder, shoulderLocal, steadiness, Time.deltaTime) : shoulderLocal;
            hasSteadyShoulder = true;
            Vector3 shoulder = transform.TransformPoint(steadyShoulder);
            float lookPitch = Vector3.SignedAngle(transform.forward, aim * Vector3.forward, transform.right);
            Quaternion frame = transform.rotation * Quaternion.Euler(HandPoseRules.ArmPitch(lookPitch, armFollowsLook), 0f, 0f);
            Vector3 target = HandPoseRules.CarryPoint(shoulder, frame, arm.Reach, carry);
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

        // O modelo mudou (outro personagem, ou o boneco de blocos): acha o braço, o peito e a bengala.
        // Procura no modelRoot inteiro: a cópia de primeira pessoa também tem bengala.
        void FindParts(DollWalk current)
        {
            walk = current;
            hasSteadyShoulder = false;
            chest = socket = null;
            arm = null;
            cane.Clear();
            if (walk == null) return;

            foreach (var r in walk.transform.parent.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("Bengala")) cane.Add(r);

            socket = FindChild(walk.transform, "SOC_Hand_R");
            var animator = walk.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;
            arm = ArmBones.From(animator);
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
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

            // Quanto o punho dobraria agora para o furo apontar para "forward", sem contar o giro do
            // antebraço (graus). Para medir poses.
            public float WristBend(Vector3 forward)
            {
                Vector3 hole = IndexKnuckle != null && LittleKnuckle != null
                    ? IndexKnuckle.position - LittleKnuckle.position : Hand.up;
                HandPoseRules.SplitTwist(Quaternion.FromToRotation(hole, forward), Hand.position - Lower.position, out _, out var swing);
                return Quaternion.Angle(Quaternion.identity, swing);
            }

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
