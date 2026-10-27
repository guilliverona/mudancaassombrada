using UnityEngine;

namespace MudancaAssombrada
{
    // Faz uma luz TREMER como chama de gás ou de vela. Com "broken" ligado, ela também apaga de
    // vez em quando (a arandela com defeito do corredor). Só visual e só neste computador:
    // cada jogador vê a chama tremer um pouco diferente, e tudo bem.
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [Tooltip("Quanto a intensidade varia (0,25 = até 25% para cima e para baixo).")]
        [SerializeField, Range(0f, 1f)] float amount = 0.25f;
        [SerializeField] float speed = 6f;
        [Tooltip("Defeito: apaga por instantes, de vez em quando.")]
        [SerializeField] bool broken;

        Light lightSource;
        float baseIntensity, seed;

        void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            seed = Random.value * 100f; // cada chama no seu ritmo
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * speed, seed);
            float k = 1f + (n * 2f - 1f) * amount;
            // Defeito: um segundo ruído, lento, decide os apagões (curtos e sem padrão).
            if (broken && Mathf.PerlinNoise(Time.time * 0.9f, seed + 31f) > 0.68f) k = 0.03f;
            lightSource.intensity = baseIntensity * k;
        }
    }
}
