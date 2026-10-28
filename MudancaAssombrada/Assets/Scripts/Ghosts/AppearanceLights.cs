using UnityEngine;

namespace MudancaAssombrada
{
    // As luzes do ESPETÁCULO: o lustre do hall e o refletor do palco ficam apagados a noite inteira e
    // acendem sozinhos quando o Imitador entra em cena (a aparição) e enquanto ele faz a reverência.
    // Só lê o estado do fantasma, que já chega a todos os computadores: nada passa pela rede.
    public class AppearanceLights : MonoBehaviour
    {
        [Tooltip("As luzes que acendem (o construtor preenche). A intensidade do Inspector é a de aceso.")]
        [SerializeField] Light[] lights = new Light[0];
        [SerializeField] float fadeIn = 1.5f, fadeOut = 3f;

        float[] full;
        float level; // 0 apagado, 1 aceso
        Ghost imitator;

        public void Configure(Light[] lamps) => lights = lamps;

        void Awake()
        {
            full = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                full[i] = lights[i] != null ? lights[i].intensity : 0f;
                if (lights[i] != null) lights[i].intensity = 0f;
            }
        }

        void Update()
        {
            if (imitator == null)
                foreach (var g in Ghost.All)
                    if (g.TryGetComponent(out GhostImitator _)) { imitator = g; break; }

            bool show = imitator != null && imitator.State is GhostState.Performing or GhostState.Bowing;
            level = Mathf.MoveTowards(level, show ? 1f : 0f, Time.deltaTime / (show ? fadeIn : fadeOut));
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null)
                {
                    lights[i].intensity = full[i] * level;
                    lights[i].enabled = level > 0.001f;
                }
        }
    }
}
