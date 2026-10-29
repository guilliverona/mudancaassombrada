using UnityEngine;

namespace MudancaAssombrada
{
    // Faz a NÉVOA RASTEIRA andar: a placa gira bem devagar (os fiapos da textura vão mudando de
    // lugar, como neblina levada pelo vento; a textura é redonda, então girar não mostra a borda) e
    // "respira" (fica mais e menos densa). Cada placa começa num ponto diferente do ciclo e gira
    // para um lado, então as camadas não andam juntas. Só visual: não passa pela rede (cada
    // computador vê a sua névoa, e tanto faz se ela não é igual). Não pode ser Batching Static.
    public class MistDrift : MonoBehaviour
    {
        [Tooltip("Graus por segundo que a placa gira.")]
        [SerializeField] float spin = 1.2f;
        [Tooltip("Quanto a densidade sobe e desce (0 = parada).")]
        [SerializeField, Range(0f, 1f)] float breathe = 0.35f;
        [SerializeField] float breathePeriod = 14f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color baseColor;
        float phase, direction;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            if (renderers.Length > 0 && renderers[0].sharedMaterial != null)
                baseColor = renderers[0].sharedMaterial.GetColor(BaseColor);
            // a posição decide o começo do ciclo e o lado do giro: diferente em cada placa
            phase = Mathf.Repeat(transform.position.x * 0.37f + transform.position.z * 0.71f, 1f);
            direction = phase < 0.5f ? 1f : -1f;
        }

        void Update()
        {
            transform.Rotate(0f, spin * direction * Time.deltaTime, 0f, Space.World);
            float k = 1f - breathe * 0.5f * (1f + Mathf.Sin((Time.time / breathePeriod + phase) * Mathf.PI * 2f));
            var color = baseColor;
            color.a *= k;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
