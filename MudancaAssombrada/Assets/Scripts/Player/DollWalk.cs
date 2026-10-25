using UnityEngine;
using UnityEngine.Rendering; // ShadowCastingMode

namespace MudancaAssombrada
{
    // Animação de caminhada bem simples para o boneco de blocos: balança braços e pernas
    // enquanto ele anda. Calcula a velocidade pela mudança de posição, então funciona
    // também nos bonecos dos OUTROS jogadores (que só recebem a posição pela rede).
    public class DollWalk : MonoBehaviour
    {
        [SerializeField] Transform leftArm, rightArm, leftLeg, rightLeg; // os "ombros" e "quadris"
        [SerializeField] float swingAngle = 35f;  // quanto o membro balança, em graus
        [SerializeField] float stepsPerMeter = 1.6f;
        [SerializeField] bool hideBody; // esconde o boneco já no início (ver HideBody)

        Vector3 lastPosition;
        float phase;
        float amount; // 0 = parado, 1 = andando (suavizado para não "travar" ao parar)

        void Start()
        {
            lastPosition = transform.position;
            if (hideBody) HideBody();
        }

        // Esconde o boneco da SUA câmera: a câmera fica dentro da cabeça, então você veria
        // braços e o interior do corpo. "ShadowsOnly" deixa ele invisível, mas ainda faz sombra.
        public void HideBody()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
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
            Swing(leftLeg, angle);
            Swing(rightLeg, -angle);
            Swing(leftArm, -angle);
            Swing(rightArm, angle);
        }

        static void Swing(Transform limb, float angle)
        {
            if (limb != null) limb.localRotation = Quaternion.Euler(angle, 0, 0);
        }
    }
}
