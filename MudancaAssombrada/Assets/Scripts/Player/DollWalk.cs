using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering; // ShadowCastingMode

namespace MudancaAssombrada
{
    // Animação de caminhada bem simples: balança braços e pernas enquanto o boneco anda.
    // Calcula a velocidade pela mudança de posição, então funciona também nos bonecos
    // dos OUTROS jogadores (que só recebem a posição pela rede).
    //
    // Serve para os dois tipos de boneco:
    //   - boneco de blocos: os "ombros" e "quadris" são ligados no Inspector (ou pelo builder);
    //   - modelo do Blender (FBX Humanoid): se os campos estiverem vazios, ele pergunta ao
    //     Animator "qual osso é o braço esquerdo?", sem depender do nome que veio do Blender.
    public class DollWalk : MonoBehaviour
    {
        [SerializeField] Transform leftArm, rightArm, leftLeg, rightLeg; // os "ombros" e "quadris"
        [SerializeField] float swingAngle = 35f;  // quanto o membro balança, em graus
        [SerializeField] float stepsPerMeter = 1.6f;
        [SerializeField] bool hideBody; // esconde o boneco já no início (ver HideBody)

        // Agachar com um joelho no chão: a perna ESQUERDA vai à frente (coxa deitada, canela em pé)
        // e a DIREITA fica atrás, com o joelho apoiado e a canela deitada para trás.
        [Header("Agachar (um joelho no chão)")]
        [SerializeField] float crouchFrontThigh = 80f; // coxa da frente, para a frente (graus)
        [SerializeField] float crouchFrontKnee = 85f;  // joelho da frente: canela quase em pé
        [SerializeField] float crouchBackThigh = 10f;  // coxa de trás: quase em pé, joelho no chão
        [SerializeField] float crouchBackKnee = 100f;  // canela de trás deitada para trás
        [SerializeField] float crouchLean = 25f;       // tronco para a frente
        [SerializeField] float crouchFrontArm = 35f;   // braço da frente: a mão vai ao joelho
        [SerializeField] float crouchBackArm = 10f;    // braço de trás: solto, um pouco à frente
        [SerializeField] float crouchSpeed = 8f;       // o mesmo ritmo da câmera (PlayerMovement)
        // Andando agachado, as pernas trocam a cada passo: uma vai à frente (como a da frente acima)
        // e a outra fica atrás, com a coxa para trás. O joelho é CALCULADO (ver CrouchLeg) para o pé
        // terminar no chão em qualquer momento do passo.
        [SerializeField] float crouchWalkBackThigh = 20f;  // coxa de trás, para TRÁS (graus)
        [SerializeField] float crouchStepsPerMeter = 0.9f; // agachado o passo é mais lento e comprido
        [SerializeField] float crouchStepLift = 0.06f;     // quanto o pé que vai à frente sobe (metros)

        // Ordem: perna E, perna D, braço E, braço D, canela E, canela D, coluna.
        // As três últimas só existem nos modelos Humanoid (o boneco de blocos não tem joelho).
        Transform[] limbs;
        readonly Quaternion[] restRotations = new Quaternion[7]; // a pose original de cada membro
        readonly Vector3[] swingAxes = new Vector3[7];           // eixo "lado a lado" no espaço do pai
        Transform leftShin, rightShin, spine;

        Vector3 lastPosition;
        float phase;
        float amount; // 0 = parado, 1 = andando (suavizado para não "travar" ao parar)

        NetworkPlayer player; // de onde vem o "está agachado?" (vale para os bonecos dos outros também)
        Vector3 restLocalPosition;
        float crouchDrop;     // quanto o quadril desce agachado, calculado pelo tamanho das pernas
        float crouchWalkDrop; // o mesmo, andando agachado
        float crouchBack;     // quanto o corpo recua para os pés ficarem onde estavam (embaixo do colisor)
        Vector3 backward;     // "para trás" do boneco, no espaço do pai
        float crouch;         // 0 = em pé, 1 = agachado (suavizado)

        void Awake()
        {
            if (leftLeg == null || rightLeg == null || leftArm == null || rightArm == null)
                FindHumanoidBones();

            limbs = new[] { leftLeg, rightLeg, leftArm, rightArm, leftShin, rightShin, spine };
            for (int i = 0; i < limbs.Length; i++)
            {
                if (limbs[i] == null) continue;
                restRotations[i] = limbs[i].localRotation;
                // Os ossos do Blender têm eixos próprios (cada um aponta para onde quiser), então
                // não dá para girar "em X". Giramos em volta do eixo lateral do personagem,
                // convertido para o espaço do pai de cada membro. No boneco de blocos isso é o X.
                swingAxes[i] = limbs[i].parent.InverseTransformDirection(transform.right);
            }

            player = GetComponentInParent<NetworkPlayer>();
            restLocalPosition = transform.localPosition;
            // Medido no mundo; a descida é aplicada no espaço do pai (que pode ter escala).
            float parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
            var (drop, back) = MeasureCrouch();
            crouchDrop = drop / Mathf.Max(parentScale, 0.0001f);
            crouchBack = back / Mathf.Max(parentScale, 0.0001f);
            // Andando, o quadril fica na altura da perna da frente (é a conta do CrouchLeg).
            crouchWalkDrop = shinLength > 0f
                ? (thighLength + shinLength - walkHipHeight) / Mathf.Max(parentScale, 0.0001f)
                : crouchDrop;
            backward = transform.parent != null ? transform.parent.InverseTransformDirection(-transform.forward).normalized : Vector3.back;
        }

        // Quanto o corpo desce (para o pé da frente e o joelho de trás tocarem o chão) e recua
        // (para o apoio, entre o pé da frente e o joelho de trás, ficar embaixo do corpo).
        // Coxa inclinada "a" para a frente e canela "a - joelho":
        //   perna da frente: quadril a coxa·cos(a) + canela·cos(a - joelho) do chão
        //   perna de trás:   quadril a coxa·cos(a) do chão (apoiada no joelho)
        // A altura usada é a média das duas. Sem joelho (boneco de blocos), a perna é uma peça só.
        (float drop, float back) MeasureCrouch()
        {
            if (leftLeg == null) return (0f, 0f);
            float a1 = crouchFrontThigh * Mathf.Deg2Rad, a2 = crouchBackThigh * Mathf.Deg2Rad;
            if (leftShin != null && animatorFoot != null)
            {
                float thigh = Vector3.Distance(leftLeg.position, leftShin.position);
                float shin = Vector3.Distance(leftShin.position, animatorFoot.position);
                float k1 = crouchFrontKnee * Mathf.Deg2Rad;
                float front = thigh * Mathf.Cos(a1) + shin * Mathf.Cos(a1 - k1);
                thighLength = thigh;
                shinLength = shin;
                walkHipHeight = front; // andando, o quadril fica na altura da perna da frente
                float back = thigh * Mathf.Cos(a2);
                float drop = thigh + shin - (front + back) / 2f;
                float frontFoot = thigh * Mathf.Sin(a1) + shin * Mathf.Sin(a1 - k1);
                float backKnee = thigh * Mathf.Sin(a2);
                return (drop, Mathf.Max(0f, (frontFoot + backKnee) / 2f));
            }
            // Boneco de blocos: o comprimento da perna é a distância do quadril até a sola.
            // (Desligado, o renderer não tem caixa: aí vale a perna padrão do builder, 0,8 m.)
            float sole = float.MaxValue;
            foreach (var r in leftLeg.GetComponentsInChildren<Renderer>(true))
                if (r.bounds.size != Vector3.zero) sole = Mathf.Min(sole, r.bounds.min.y);
            float leg = sole == float.MaxValue ? 0.8f * leftLeg.lossyScale.y : leftLeg.position.y - sole;
            // Apoia na perna de trás; a da frente, reta, vai à frente. Recua só um pouco, para o
            // boneco não sair do colisor.
            return (leg * (1 - Mathf.Cos(a2)), Mathf.Min(leg * Mathf.Sin(a1) / 2f, 0.25f));
        }

        Transform animatorFoot;

        // Animações de verdade (clipes do Mixamo, ver AnimationBuilder). null = animação por código.
        Animator clips;
        static readonly int ForwardId = Animator.StringToHash("Frente");   // = AnimationBuilder.ForwardParam
        static readonly int SideId = Animator.StringToHash("Lado");        // = AnimationBuilder.SideParam
        static readonly int CrouchId = Animator.StringToHash("Agachado");  // = AnimationBuilder.CrouchParam
        static readonly int DownId = Animator.StringToHash("Caido");       // = AnimationBuilder.DownParam
        static readonly int RescueId = Animator.StringToHash("Ajuda");     // = AnimationBuilder.RescueParam
        static readonly int GettingUpState = Animator.StringToHash("Levantando");
        static readonly int FallingState = Animator.StringToHash("Caindo");

        // O boneco ainda está no chão ou se levantando (a animação não acabou)? O PlayerCondition
        // usa isto para só soltar o jogador quando ele estiver de pé de verdade.
        // Sem clipes (boneco de blocos), não há animação a esperar: false.
        public bool IsGettingUp
        {
            get
            {
                if (clips == null || !clips.isActiveAndEnabled) return false;
                var now = clips.GetCurrentAnimatorStateInfo(0);
                var next = clips.GetNextAnimatorStateInfo(0); // durante uma transição
                return now.shortNameHash == GettingUpState || now.shortNameHash == FallingState
                    || next.shortNameHash == GettingUpState;
            }
        }

        // Liga os clipes neste boneco (o CharacterSwitcher chama ao criar o modelo da ficha).
        // Só vale para modelos Humanoid; o boneco de blocos continua com a animação por código.
        public void UseClips(RuntimeAnimatorController controller)
        {
            var animator = GetComponent<Animator>();
            if (controller == null || animator == null || animator.avatar == null || !animator.isHuman) return;

            // Desfaz o que a animação por código mexeu (o Awake não mexe, mas por garantia).
            for (int i = 0; i < limbs.Length; i++)
                if (limbs[i] != null) limbs[i].localRotation = restRotations[i];
            transform.localPosition = restLocalPosition;

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; // quem anda é o CharacterController, não a animação
            animator.enabled = true;
            clips = animator;
            caneLayer = animator.GetLayerIndex(CaneLayerName); // -1 = controller sem a camada (versão antiga)
            CollectHandBones(animator);
            // Os clipes foram acertados para um corpo de tamanho 1 (AnimationBuilder.NaturalSpeed):
            // perna mais curta (a Cida) precisa de passos mais rápidos para andar o mesmo tanto.
            stepPace = 1f / Mathf.Max(animator.humanScale, 0.1f);
        }

        // Segura a bengala (ficha: canTap)? Liga a camada "Bengala" do Animator: braço e dedos
        // esquerdos na pega, o resto do corpo segue andando. Quem diz é o CharacterSwitcher.
        public bool HoldsCane { get; set; }
        const string CaneLayerName = "Bengala"; // = AnimationBuilder.CaneLayer
        int caneLayer = -1;
        float caneWeight;

        // Mãos fora do chão (caído e levantando). Os clipes Cair/Levantar apoiam a palma pensando
        // num corpo médio: a mão grande do Marcelo (26 cm, com luva) entrava até 22 cm no chão.
        // Depois da animação (LateUpdate), se o ponto mais baixo da mão fica abaixo do chão, um IK
        // de dois ossos dobra o ombro e o cotovelo para subir a mão o que falta, sem girar a mão.
        // (O IK do Animator não serve: no meio do Levantar o "alvo" dele não coincide com a mão.)
        [Tooltip("Sobe a mão quando ela entraria no chão durante a queda e o levantar.")]
        [SerializeField] bool keepHandsAboveFloor = true;
        Transform[] handBones = new Transform[2], upperArms = new Transform[2], lowerArms = new Transform[2];
        Transform[][] handJoints = new Transform[2][]; // juntas dos dedos de cada mão
        float[] handClearance = new float[2];          // "espessura" abaixo das juntas (dedo, luva)

        void CollectHandBones(Animator animator)
        {
            var hands = new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
            for (int s = 0; s < 2; s++)
            {
                handBones[s] = animator.GetBoneTransform(hands[s]);
                upperArms[s] = animator.GetBoneTransform(s == 0 ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                lowerArms[s] = animator.GetBoneTransform(s == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                var joints = new System.Collections.Generic.List<Transform>();
                // os ossos dos dedos vêm logo depois da mão no enum: polegar, indicador... (3 cada)
                int first = s == 0 ? (int)HumanBodyBones.LeftThumbProximal : (int)HumanBodyBones.RightThumbProximal;
                for (int i = 0; i < 15; i++)
                {
                    var b = animator.GetBoneTransform((HumanBodyBones)(first + i));
                    if (b != null) joints.Add(b);
                }
                handJoints[s] = joints.ToArray();
                // Espessura: 20% do comprimento da palma (fichas: dedo de 1 a 1,4 cm de raio, luva +1,5 cm).
                var middle = animator.GetBoneTransform(s == 0 ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
                handClearance[s] = handBones[s] != null && middle != null
                    ? Vector3.Distance(handBones[s].position, middle.position) * 0.2f : 0.02f;
            }
        }

        // Depois da animação: o Animator reescreve o braço a cada quadro, então aqui sempre partimos
        // da pose "crua" do clipe (sem acumular a correção do quadro anterior).
        void LateUpdate()
        {
            if (clips == null || !keepHandsAboveFloor || !IsGettingUp) return; // só caindo, deitado ou levantando
            float floor = transform.position.y; // os pés do jogador: o chão onde ele está
            for (int s = 0; s < 2; s++)
            {
                if (handBones[s] == null || upperArms[s] == null || lowerArms[s] == null) continue;
                float low = handBones[s].position.y;
                var joints = handJoints[s];
                for (int i = 0; i < joints.Length; i++)
                {
                    low = Mathf.Min(low, joints[i].position.y);
                    // a ponta do dedo: a falange distal continua na direção da média
                    if (i % 3 == 2)
                        low = Mathf.Min(low, (joints[i].position + (joints[i].position - joints[i - 1].position) * 0.9f).y);
                }
                float lift = HandPoseRules.FloorLift(low, floor, handClearance[s]);
                if (lift > 0f) LiftHand(upperArms[s], lowerArms[s], handBones[s], lift);
            }
        }

        // IK de dois ossos (ombro e cotovelo) para levar a mão "lift" metros para cima, no mesmo
        // plano em que o braço já dobra. A mão mantém a rotação que tinha (os dedos não giram).
        static void LiftHand(Transform upper, Transform lower, Transform hand, float lift)
        {
            Vector3 a = upper.position, b = lower.position, c = hand.position;
            Vector3 t = c + Vector3.up * lift;
            Quaternion handRotation = hand.rotation;
            float ab = (b - a).magnitude, bc = (c - b).magnitude;
            float at = Mathf.Clamp((t - a).magnitude, 0.01f, ab + bc - 0.001f);

            // ângulos atuais e os que fecham o triângulo ombro-cotovelo-alvo (lei dos cossenos)
            float acAb0 = Vector3.Angle(c - a, b - a), baBc0 = Vector3.Angle(a - b, c - b), acAt0 = Vector3.Angle(c - a, t - a);
            float acAb1 = Mathf.Acos(Mathf.Clamp((bc * bc - ab * ab - at * at) / (-2f * ab * at), -1f, 1f)) * Mathf.Rad2Deg;
            float baBc1 = Mathf.Acos(Mathf.Clamp((at * at - ab * ab - bc * bc) / (-2f * ab * bc), -1f, 1f)) * Mathf.Rad2Deg;

            Vector3 bend = Vector3.Cross(c - a, b - a);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.Cross(c - a, Vector3.up); // braço reto: dobra para cima
            bend.Normalize();
            Vector3 toTarget = Vector3.Cross(c - a, t - a);

            lower.rotation = Quaternion.AngleAxis(baBc1 - baBc0, bend) * lower.rotation;
            upper.rotation = Quaternion.AngleAxis(acAb1 - acAb0, bend) * upper.rotation;
            if (toTarget.sqrMagnitude > 1e-8f)
                upper.rotation = Quaternion.AngleAxis(acAt0, toTarget.normalized) * upper.rotation;
            hand.rotation = handRotation;
        }

        float stepPace = 1f; // quanto os clipes de andar aceleram por causa do tamanho do personagem
        const float FullPaceSpeed = 1.1f; // a partir desta velocidade (m/s), o ajuste vale inteiro

        float thighLength, shinLength, walkHipHeight; // medidos no mundo (0 = boneco sem joelho)

        void FindHumanoidBones()
        {
            var animator = GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.isHuman)
            {
                Debug.LogWarning($"{name}: sem membros ligados e sem Animator Humanoid; o boneco não vai balançar.", this);
                return;
            }
            leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftShin = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightShin = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            animatorFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);

            // Ainda não há animações: desligado, o Animator não sobrescreve o balanço dos ossos.
            animator.enabled = false;
        }

        void Start()
        {
            lastPosition = transform.position;
            if (hideBody) HideBody();
        }

        // Esconde o boneco da SUA câmera: a câmera fica dentro da cabeça, então você veria
        // braços e o interior do corpo. "ShadowsOnly" deixa ele invisível, mas ainda faz sombra.
        // "true" = inclui as partes desligadas.
        // Os espelhos (MirrorReflection) ligam estes de volta só enquanto desenham o reflexo:
        // assim você se vê no espelho sem ver o interior da própria cabeça.
        public void HideBody()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                if (!HiddenBodies.Contains(renderer)) HiddenBodies.Add(renderer);
            }
            // Ao olhar para baixo você vê o corpo: uma cópia sem cabeça (FirstPersonBody).
            FirstPersonBody.For(this);
        }

        // Registro das partes escondidas da SUA câmera (o seu próprio corpo).
        public static readonly List<Renderer> HiddenBodies = new();

        void OnDestroy()
        {
            HiddenBodies.RemoveAll(r => r == null || r.transform.IsChildOf(transform));
        }

        void Update()
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0; // pular/cair não conta como passo
            lastPosition = transform.position;

            float speed = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            amount = Mathf.MoveTowards(amount, speed > 0.1f ? 1f : 0f, Time.deltaTime * 5f);
            phase += delta.magnitude * Mathf.Lerp(stepsPerMeter, crouchStepsPerMeter, crouch) * Mathf.PI;

            // Agachar: vai deslizando até a pose, no mesmo ritmo em que a câmera desce.
            bool crouching = player != null && player.IsCrouching;

            // Com os clipes (UseClips), o Animator faz a pose: aqui só dizemos para onde o boneco
            // anda, em relação a ELE (Frente/Lado, em m/s), e se está agachado.
            // O "dampTime" suaviza a velocidade (a posição dos outros chega aos trancos).
            if (clips != null)
            {
                Vector3 local = Time.deltaTime > 0 ? transform.InverseTransformDirection(delta) / Time.deltaTime : Vector3.zero;
                clips.SetFloat(ForwardId, local.z, 0.1f, Time.deltaTime);
                clips.SetFloat(SideId, local.x, 0.1f, Time.deltaTime);
                clips.SetBool(CrouchId, crouching);
                // Caído (pego por um fantasma): vem do PlayerCondition, que já vai pela rede.
                var condition = player != null ? player.Condition : null;
                clips.SetBool(DownId, condition != null && condition.IsDown);
                // Progresso do resgate (o colega segurando a tecla): o tempo do clipe Levantar.
                // Suavizado aqui porque o valor chega pela rede aos degraus.
                // Já de pé: segura o valor até sair do "Levantando" (senão a pose voltaria ao chão no meio
                // da troca) e depois zera (senão uma nova queda "pularia" direto para o levantar).
                bool down = condition != null && condition.IsDown;
                float shown = clips.GetFloat(RescueId);
                if (down) shown = Mathf.MoveTowards(shown, condition.RescueProgress, Time.deltaTime);
                else if (!IsGettingUp) shown = 0f;
                clips.SetFloat(RescueId, shown);
                // Pega da bengala: solta ao cair (a queda e o levantar usam o corpo inteiro).
                if (caneLayer >= 0)
                {
                    float target = HandPoseRules.CaneWeight(HoldsCane, down, IsGettingUp);
                    caneWeight = HandPoseRules.Smooth(caneWeight, target, 4f, Time.deltaTime);
                    clips.SetLayerWeight(caneLayer, caneWeight);
                }
                // Ritmo dos passos pelo tamanho do corpo, só andando: parado, caindo ou levantando,
                // os clipes tocam no tempo original (senão a Cida respiraria e cairia acelerada).
                float moving = down || IsGettingUp ? 0f
                    : Mathf.Clamp01(new Vector2(clips.GetFloat(SideId), clips.GetFloat(ForwardId)).magnitude / FullPaceSpeed);
                clips.speed = Mathf.Lerp(1f, stepPace, moving);
                return;
            }

            crouch = Mathf.Lerp(crouch, crouching ? 1f : 0f, crouchSpeed * Time.deltaTime);
            float drop = Mathf.Lerp(crouchDrop, crouchWalkDrop, amount);
            transform.localPosition = restLocalPosition + (Vector3.down * drop + backward * crouchBack) * crouch;

            // Seno vai de -1 a 1: perna esquerda para frente enquanto a direita vai para trás,
            // e cada braço acompanha a perna do lado oposto, como gente de verdade.
            // Agachado, quem anda são as pernas dobradas (abaixo), não este balanço.
            float angle = Mathf.Sin(phase) * swingAngle * amount * (1f - crouch);

            // Pernas e braços apontam para BAIXO: ângulo negativo = para a frente (ver Swing).
            // Parado: perna esquerda à frente e joelho direito no chão.
            // Andando: as pernas trocam de lugar a cada passo (front = 1 é a perna da frente).
            // A perna que está indo à frente (o seno subindo: cosseno positivo) levanta o pé.
            float left = 0.5f + 0.5f * Mathf.Sin(phase);
            float leftLift = crouchStepLift * Mathf.Max(0f, Mathf.Cos(phase));
            float rightLift = crouchStepLift * Mathf.Max(0f, -Mathf.Cos(phase));
            var (leftThigh, leftKnee) = CrouchLeg(left, true, leftLift);
            var (rightThigh, rightKnee) = CrouchLeg(1f - left, false, rightLift);
            Swing(0, -leftThigh * crouch + angle);
            Swing(1, -rightThigh * crouch - angle);
            Swing(4, leftKnee * crouch);                   // canelas dobram para trás
            Swing(5, rightKnee * crouch);

            // Braços: parado, a mão esquerda vai ao joelho da frente; andando, cada braço acompanha
            // a perna do lado oposto.
            float armSwing = Mathf.Sin(phase) * swingAngle * 0.5f * amount * crouch;
            Swing(2, -Mathf.Lerp(crouchFrontArm, crouchBackArm, amount) * crouch - angle - armSwing);
            Swing(3, -crouchBackArm * crouch + angle + armSwing);
            // A coluna aponta para CIMA: aí o ângulo positivo é que leva o tronco para a frente.
            Swing(6, crouchLean * crouch);
        }

        // Coxa e joelho de uma perna agachada (positivo = coxa para a frente).
        // "front" vai de 0 (perna atrás) a 1 (perna à frente). Parado, a esquerda fica à frente e a
        // direita apoia o joelho no chão; andando (amount), elas trocam conforme o passo.
        // Andando, o joelho sai da conta "o pé precisa ficar na altura do chão":
        //   coxa·cos(a) + canela·cos(a - joelho) = altura do quadril (menos o pé levantado)
        // Sem essa conta, no meio do passo a perna fica comprida demais e o pé entra no chão.
        (float thigh, float knee) CrouchLeg(float front, bool isLeft, float lift)
        {
            float stillThigh = isLeft ? crouchFrontThigh : crouchBackThigh;
            float stillKnee = isLeft ? crouchFrontKnee : crouchBackKnee;
            float walkThigh = Mathf.Lerp(-crouchWalkBackThigh, crouchFrontThigh, front);
            float walkKnee = crouchFrontKnee;
            if (shinLength > 0f)
            {
                float a = walkThigh * Mathf.Deg2Rad;
                float c = Mathf.Clamp((walkHipHeight - lift - thighLength * Mathf.Cos(a)) / shinLength, -1f, 1f);
                walkKnee = walkThigh + Mathf.Acos(c) * Mathf.Rad2Deg; // canela sempre para trás
            }
            return (Mathf.Lerp(stillThigh, walkThigh, amount), Mathf.Lerp(stillKnee, walkKnee, amount));
        }

        // Parte da pose original e gira em volta do eixo lateral: o membro balança pela junta.
        // Girar +X (eixo lateral) leva um membro que aponta para BAIXO para trás, e um que aponta
        // para CIMA (a coluna) para a frente.
        void Swing(int i, float angle)
        {
            if (limbs[i] != null)
                limbs[i].localRotation = Quaternion.AngleAxis(angle, swingAxes[i]) * restRotations[i];
        }
    }
}
