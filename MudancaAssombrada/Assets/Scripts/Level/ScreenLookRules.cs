using UnityEngine;

namespace MudancaAssombrada
{
    // As regras do "filtro" da tela (o pós-processamento da noite), sem nada da Unity "viva":
    // dá para testar no EditMode. Quem escreve isso no Volume do URP é o ScreenLook; quem usa o
    // perigo nas lâmpadas é o HauntedLights.
    //
    // A ideia do visual: sombras FRIAS (azul) e luz QUENTE (âmbar da lanterna e do lampião), pouca
    // cor de noite, grão de filme barato. Quando um fantasma chega perto, a tela aperta (vinheta),
    // perde cor e as bordas "racham" (aberração cromática). No amanhecer, tudo esquenta.
    public static class ScreenLookRules
    {
        // --- Perigo: quanto um fantasma assusta, pela distância e pelo que ele está fazendo ---

        public const float DangerNear = 2f;  // m: daqui para dentro, perigo cheio
        public const float DangerFar = 14f;  // m: daqui para fora, nada

        // O peso de cada estado. Disfarçado e dentro do espelho valem ZERO: a tela não pode
        // entregar o Imitador (só a Cida nota o espelho diferente, e o jogo é sobre desconfiar).
        public static float StateWeight(GhostState state)
        {
            switch (state)
            {
                case GhostState.Hunting:
                case GhostState.Pulling: return 1f;
                case GhostState.Performing:
                case GhostState.Bowing: return 0.6f;    // a aparição: à mostra, sem atacar
                case GhostState.Investigating: return 0.5f;
                case GhostState.Patrolling: return 0.3f;
                default: return 0f;                     // Disguised, Fleeing, InMirror, Emerging
            }
        }

        // 0 = longe ou inofensivo, 1 = caçando colado. Cai ao quadrado: só pesa de verdade perto.
        public static float Danger(GhostState state, float distance)
        {
            float near = 1f - Mathf.InverseLerp(DangerNear, DangerFar, distance);
            return StateWeight(state) * near * near;
        }

        // --- Lâmpadas que falham perto do fantasma ---

        // Multiplica a intensidade de uma luz. "noise" é um ruído 0..1 (Perlin) que muda com o tempo.
        // Pouco perigo: nada. Muito: a luz enfraquece e apaga por instantes, cada vez mais vezes.
        public static float Flicker(float danger, float noise)
        {
            if (danger < 0.15f) return 1f;
            if (noise < danger * 0.45f) return 0.08f; // o "apagão" curto
            return Mathf.Lerp(1f, 0.7f, danger);
        }

        // --- O visual da tela ---

        public enum Sight { Normal, Echo, Vibration } // pessoa que vê, Cida (eco), Tiago (vibração)

        public struct Look
        {
            public float saturation;   // -100..100 (ColorAdjustments)
            public float contrast;     // -100..100
            public float temperature;  // -100..100 (WhiteBalance: negativo = frio)
            public float splitBalance; // -100..100 (SplitToning: negativo puxa para as sombras frias)
            public bool grading;       // false = sem tingir (a tela do eco precisa da cor certa)
            public float vignette;     // 0..1
            public Color vignetteColor;
            public float chromatic;    // 0..1
            public float grain;        // 0..1
            public float bloom;        // intensidade do Bloom
        }

        // dawn: 0 noite, 1 amanheceu. danger: 0..1. down: caído ou sendo puxado.
        // effects: false na qualidade Baixa (sem grão, sem aberração e sem bloom).
        public static Look Evaluate(float dawn, float danger, Sight sight, bool down, bool effects)
        {
            dawn = Mathf.Clamp01(dawn);
            danger = Mathf.Clamp01(danger);

            var look = new Look
            {
                grading = true,
                saturation = Mathf.Lerp(-28f, -8f, dawn) - 30f * danger,
                contrast = 6f + 12f * danger, // contraste alto na noite apaga a borda das luzes

                // Frio leve: a luz ambiente já é azul, o filtro só não pode esquentar a noite.
                temperature = Mathf.Lerp(-15f, 14f, dawn),
                splitBalance = Mathf.Lerp(-16f, 20f, dawn), // negativo = pesa mais a sombra fria

                vignette = 0.22f + 0.3f * danger,
                vignetteColor = Color.black,
                chromatic = 0.06f + 0.4f * danger,
                grain = 0.28f,
                bloom = 0.7f,
            };

            switch (sight)
            {
                case Sight.Echo:
                    // A Cida vê pelo shader do eco: tingir de azul/âmbar ou tirar cor estragaria as
                    // ondas. Fica só o que não muda a cor (vinheta, grão e o contraste do susto).
                    look.grading = false;
                    look.saturation = 0f;
                    look.temperature = 0f;
                    look.contrast = 10f * danger;
                    look.chromatic = 0.25f * danger;
                    break;
                case Sight.Vibration:
                    // O Tiago não ouve, mas sente: o tremor do fantasma "racha" a imagem em dobro.
                    look.chromatic = Mathf.Min(1f, 0.06f + 0.8f * danger);
                    break;
            }

            if (down)
            {
                // Caído: o mundo some em cinza, e a borda fica vermelha de sangue.
                look.saturation = Mathf.Min(look.saturation, -70f);
                look.vignette = Mathf.Max(look.vignette, 0.55f);
                look.vignetteColor = new Color(0.35f, 0f, 0f);
            }

            if (!effects)
            {
                look.grain = 0f;
                look.chromatic = 0f;
                look.bloom = 0f;
            }
            return look;
        }
    }
}
