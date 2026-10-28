using System.Collections.Generic;

namespace MudancaAssombrada
{
    // As REGRAS das coisas que caem na Mansão (LEVEL_DESIGN_FASE1_MANSAO.md §9, R20, R23), sem cena
    // nem rede. A casa responde a quem passa, mas susto demais vira ruído: no máximo um a cada 20 s,
    // nenhum durante o pânico e cada um só uma vez por noite (a estante, duas).
    // Quem usa: o HouseScare (no host) e a DoorWeb. Os testes ficam em AtmosphereRulesTests.
    public static class HouseScareRules
    {
        public const float MinGap = 20f;     // segundos entre dois sustos, na casa inteira
        public const float RunRadius = 1.5f; // correr a menos disto derruba
        public const float Never = float.NegativeInfinity; // "nunca houve susto"

        // Pode cair agora? "lastScare" é a hora do último susto da casa (qualquer um).
        public static bool CanFire(float now, float lastScare, bool panic, int fired, int maxFires) =>
            !panic && fired < maxFires && now - lastScare >= MinGap;

        public static bool RunningNear(float distance, bool running, float radius = RunRadius) =>
            running && distance < radius;

        // Passar perto, com uma chance: sorteia UMA vez só (na primeira vez que alguém entra no raio).
        // "roll" é o número sorteado (0 a 1); cai se ele ficar abaixo da chance.
        public static bool PassNear(float distance, float radius, bool alreadyRolled, float roll, float chance, out bool rolled)
        {
            rolled = alreadyRolled;
            if (alreadyRolled || distance >= radius) return false;
            rolled = true;
            return roll < chance;
        }

        // Ninguém olhando: o rosto de cada jogador (o cosseno entre a frente dele e a direção do objeto)
        // fica abaixo do limite. Um olhando já basta para a cadeira ficar quieta.
        public static bool NobodyLooking(IReadOnlyList<float> facingDots, float cosLimit)
        {
            foreach (float d in facingDots)
                if (d > cosLimit) return false;
            return true;
        }

        // R23: o Imitador flutua e atravessa sem rasgar. Uma teia inteira onde "alguém" passou é a pista.
        public static bool TearsWeb(bool isPlayer) => isPlayer;
    }
}
