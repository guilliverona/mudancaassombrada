using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS da pianola da Sala de Música (LEVEL_DESIGN_FASE1_MANSAO.md §7, R18), sem cena nem som.
    // A pianola toca a MESMA faixa da cama (a trilha 2D do SoundDirector), no mesmo ponto, mas em 3D:
    // perto dela, a trilha deixa de ser "música" e vira o piano da casa. Para isso a cama 2D abaixa
    // perto da pianola, e na Câmara Secreta tudo para (o susto é a falta do piano).
    // Os testes ficam em AtmosphereRulesTests.
    public static class PianolaRules
    {
        public const float Tolerance = 0.08f; // menos que isso de diferença, ninguém ouve: não pula

        // Quanto a pianola está à frente (+) ou atrás (-) da cama, pelo caminho mais curto na volta da faixa.
        public static float Drift(float mine, float bed, float length)
        {
            if (length <= 0f) return mine - bed;
            float d = Mathf.Repeat(mine - bed, length);
            return d > length / 2f ? d - length : d;
        }

        public static bool NeedsResync(float mine, float bed, float length, float tolerance = Tolerance) =>
            Mathf.Abs(Drift(mine, bed, length)) > tolerance;

        // 1 = colado na pianola (até "near"), 0 = longe (a partir de "far").
        public static float Closeness(float distance, float near, float far) =>
            1f - Mathf.Clamp01((distance - near) / Mathf.Max(0.01f, far - near));

        // Quanto sobra da cama 2D: abaixa perto da pianola (o som passa a vir dela) e some na Câmara.
        public static float BedDuck(float closeness, bool silentZone, float depth = 0.7f) =>
            silentZone ? 0f : 1f - depth * Mathf.Clamp01(closeness);
    }
}
