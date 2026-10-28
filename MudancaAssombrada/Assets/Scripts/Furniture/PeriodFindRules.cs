using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS do pagamento da Mansão (LEVEL_DESIGN_FASE1_MANSAO.md §8, D10 e D13), sem cena nem rede.
    //   - Achado de época (PeriodFind) entregue no caminhão = pagamento extra.
    //   - Frágil (Fragile) batido = desconto no bilhete do chefe. Nada quebra de vez.
    // O pagamento nunca fica negativo: o chefe desconta, mas não cobra da equipe.
    // Quem usa: o GameManager (no fim da noite) e o Fragile. Os testes ficam em AtmosphereRulesTests.
    public static class PeriodFindRules
    {
        public const float MinImpact = 1.5f;   // o mesmo limite do barulho de batida do Furniture
        public const float HitCooldown = 1f;   // quicar logo depois é a mesma pancada
        public const float Never = float.NegativeInfinity;

        public static int Pay(int payment, IReadOnlyList<int> findValues, int fragileHits, int discountPerHit)
        {
            int total = payment;
            foreach (int v in findValues) total += v;
            return Mathf.Max(0, total - fragileHits * discountPerHit);
        }

        public static bool CountsHit(float impact, float now, float lastHit) =>
            impact >= MinImpact && now - lastHit >= HitCooldown;
    }
}
