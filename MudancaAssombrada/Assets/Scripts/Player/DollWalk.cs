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

        // Ordem: perna E, perna D, braço E, braço D.
        Transform[] limbs;
        readonly Quaternion[] restRotations = new Quaternion[4]; // a pose original de cada membro
        readonly Vector3[] swingAxes = new Vector3[4];           // eixo "lado a lado" no espaço do pai

        Vector3 lastPosition;
        float phase;
        float amount; // 0 = parado, 1 = andando (suavizado para não "travar" ao parar)

        void Awake()
        {
            if (leftLeg == null || rightLeg == null || leftArm == null || rightArm == null)
                FindHumanoidBones();

            limbs = new[] { leftLeg, rightLeg, leftArm, rightArm };
            for (int i = 0; i < limbs.Length; i++)
            {
                if (limbs[i] == null) continue;
                restRotations[i] = limbs[i].localRotation;
                // Os ossos do Blender têm eixos próprios (cada um aponta para onde quiser), então
                // não dá para girar "em X". Giramos em volta do eixo lateral do personagem,
                // convertido para o espaço do pai de cada membro. No boneco de blocos isso é o X.
                swingAxes[i] = limbs[i].parent.InverseTransformDirection(transform.right);
            }
        }

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
        public void HideBody()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        void Update()
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0; // pular/cair não conta como passo
            lastPosition = transform.position;

            float speed = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            amount = Mathf.MoveTowards(amount, speed > 0.1f ? 1f : 0f, Time.deltaTime * 5f);
            phase += delta.magnitude * stepsPerMeter * Mathf.PI;

            // Seno vai de -1 a 1: perna esquerda para frente enquanto a direita vai para trás,
            // e cada braço acompanha a perna do lado oposto, como gente de verdade.
            float angle = Mathf.Sin(phase) * swingAngle * amount;
            Swing(0, angle);
            Swing(1, -angle);
            Swing(2, -angle);
            Swing(3, angle);
        }

        // Parte da pose original e gira em volta do eixo lateral: o membro balança pela junta.
        void Swing(int i, float angle)
        {
            if (limbs[i] != null)
                limbs[i].localRotation = Quaternion.AngleAxis(angle, swingAxes[i]) * restRotations[i];
        }
    }
}
