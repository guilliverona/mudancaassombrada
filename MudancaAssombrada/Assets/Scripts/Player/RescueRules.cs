using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS do resgate de colega, sem cena nem rede: quem pode ajudar, quanto o progresso sobe
    // ou volta, e quando a equipe inteira caiu. O PlayerCondition e o GameManager perguntam aqui;
    // os testes (Assets/Tests/EditMode/RescueRulesTests.cs) conferem cada regra sem abrir uma fase.
    public static class RescueRules
    {
        public const ulong NoRescuer = ulong.MaxValue;

        // Caído: levantar leva o clipe inteiro. Puxado: segurar firme por um instante basta.
        public static float Duration(bool pulled, float rescueDuration, float anchorDuration) =>
            pulled ? anchorDuration : rescueDuration;

        // O progresso (0 a 1) depois de "dt" segundos: sobe enquanto alguém ajuda, volta a zero
        // em "fallBack" segundos quando ninguém ajuda.
        public static float Step(float progress, bool helping, float duration, float fallBack, float dt)
        {
            float speed = helping ? 1f / duration : -1f / fallBack;
            return Mathf.Clamp01(progress + speed * dt);
        }

        // Quem fica ajudando depois de um pedido. Só começa se a vítima precisa e ninguém está ajudando;
        // só quem começou pode parar (um segundo jogador não rouba a vez nem tira o primeiro).
        public static ulong Claim(ulong rescuer, ulong sender, bool holding, bool inNeed)
        {
            if (holding && inNeed && rescuer == NoRescuer) return sender;
            if (!holding && rescuer == sender) return NoRescuer;
            return rescuer;
        }

        // Quem ajuda precisa estar na partida, de pé (livre) e perto.
        public static bool CanRescue(bool helperExists, bool helperFree, float distance, float reach) =>
            helperExists && helperFree && distance <= reach;

        // A equipe inteira caída = derrota. Sem jogadores, ninguém perdeu.
        public static bool EveryoneDown(int downCount, int playerCount) =>
            playerCount > 0 && downCount >= playerCount;
    }
}
