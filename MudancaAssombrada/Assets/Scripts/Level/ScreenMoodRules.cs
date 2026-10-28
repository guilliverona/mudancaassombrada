using UnityEngine;

namespace MudancaAssombrada
{
    // As contas do "clima" da tela, sem Unity no meio (testadas em ScreenMoodRulesTests).
    //   - Danger/Smooth: o quanto o fantasma está perto (fecha a vinheta e tira a cor).
    //   - ZoneDarkness/Combine: o quanto uma zona de escuridão apaga um ponto da casa.
    //     O shader DarknessZones.shader repete ESTA conta: mudou aqui, muda lá.
    public static class ScreenMoodRules
    {
        // 0 = longe (a partir de "far"), 1 = em cima (até "near"). Sem fantasma = distância infinita.
        public static float Danger(float distance, float near, float far)
        {
            if (float.IsInfinity(distance) || distance >= far) return 0f;
            if (distance <= near) return 1f;
            float t = 1f - (distance - near) / (far - near);
            return t * t; // ao quadrado: quase nada de longe, e aperta de verdade quando chega perto
        }

        // Anda do valor atual até o alvo: sobe na velocidade "rise" e desce na "fall" (por segundo).
        public static float Smooth(float current, float target, float dt, float rise, float fall)
        {
            float speed = target > current ? rise : fall;
            return Mathf.MoveTowards(current, target, speed * dt);
        }

        // Escuridão de UMA zona esférica num ponto a "distance" do centro.
        // softness = parte do raio que é borda (0 = corte seco, 1 = esmaece desde o centro).
        public static float ZoneDarkness(float distance, float radius, float softness, float strength)
        {
            if (distance >= radius) return 0f;
            float inner = radius * (1f - Mathf.Clamp01(softness));
            if (distance <= inner) return strength;
            float t = (distance - inner) / (radius - inner); // 0 na borda de dentro, 1 na de fora
            return strength * (1f - t * t * (3f - 2f * t));  // smoothstep, como no shader
        }

        // Zonas que se cruzam não somam: vale a mais escura (e nunca passa de breu).
        public static float Combine(float a, float b) => Mathf.Min(1f, Mathf.Max(a, b));
    }
}
