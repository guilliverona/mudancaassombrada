using UnityEngine;

namespace MudancaAssombrada
{
    // Como a LUZ da lanterna se desenha, sem cena: o "cookie" (a textura que a luz projeta, como um
    // slide num projetor) e o brilho da lente. O construtor gera a textura daqui (FlashlightCookieBuilder)
    // e o HeldItems acende a lente; os testes (FlashlightBeamRulesTests) conferem o desenho.
    //
    // O facho de uma lanterna barata não é um disco liso: o centro é forte, o refletor faz um ARO mais
    // claro perto da borda, e os riscos do refletor deixam faixas leves. É isso que faz a luz no chão
    // parecer de lanterna (e não de um holofote de palco).
    public static class FlashlightBeamRules
    {
        public const float RingRadius = 0.72f; // onde fica o aro (fração do raio do cone)
        const float RingWidth = 0.07f;
        const float RingStrength = 0.4f;
        const float CenterFalloff = 0.5f;      // até onde vai o miolo forte
        const float CenterDrop = 0.5f;         // quanto o miolo cai até lá (fica uma faixa mais escura antes do aro)
        const float EdgeStart = 0.82f;         // daqui até a borda, o facho apaga

        // Brilho do facho (0 a 1) a "r" do centro (0 = meio, 1 = borda do cone) e no ângulo "angle"
        // (radianos) em volta do centro.
        public static float Cookie(float r, float angle)
        {
            if (r >= 1f) return 0f;
            float hot = 1f - Smooth(0f, CenterFalloff, r) * CenterDrop;              // miolo forte
            float ring = RingStrength * Mathf.Exp(-Sq((r - RingRadius) / RingWidth)); // o aro do refletor
            float fade = 1f - Smooth(EdgeStart, 1f, r);                              // a borda apaga
            // Riscos do refletor: somem perto do meio (lá todos os ângulos se encontram e virariam uma estrela).
            float streaks = (0.04f * Mathf.Sin(angle * 7f + r * 3f) + 0.03f * Mathf.Sin(angle * 13f)) * Smooth(0.05f, 0.3f, r);
            return Mathf.Clamp01((hot + ring) * fade * (1f + streaks));
        }

        // Brilho da lente (0 a 1): apagada não brilha; acesa, segue a luz (inclusive quando ela falha
        // com a bateria fraca: lightLevel é a força da luz neste quadro, 0 a 1).
        public static float LensGlow(bool on, float lightLevel) => on ? Mathf.Clamp01(lightLevel) : 0f;

        static float Sq(float x) => x * x;

        static float Smooth(float from, float to, float x)
        {
            float t = Mathf.Clamp01((x - from) / (to - from));
            return t * t * (3f - 2f * t);
        }
    }
}
