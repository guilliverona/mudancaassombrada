using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS da partida, sem cena nem rede: relógio, estrelas, etapas e "objetivo cumprido".
    // O GameManager e os Objective perguntam aqui; os testes (Assets/Tests/EditMode/MatchRulesTests.cs)
    // conferem cada regra sem precisar abrir uma fase.
    public static class MatchRules
    {
        public const int MaxStars = 5;

        // 0 = começo da noite, 1 = amanheceu. Sem relógio (duração 0): sempre 0 (noite o tempo todo).
        public static float NightProgress(float timeLeft, float nightDuration) =>
            nightDuration > 0 ? 1f - Mathf.Clamp01(timeLeft / nightDuration) : 0f;

        // Mais tempo sobrando = mais estrelas (1 a 5). Sem relógio: 5 estrelas.
        public static int Stars(float timeLeft, float nightDuration) =>
            nightDuration > 0
                ? Mathf.Clamp(Mathf.CeilToInt(timeLeft / nightDuration * MaxStars), 1, MaxStars)
                : MaxStars;

        // As etapas que existem na fase, sem repetir e em ordem (ex.: 2, 0, 0, 1 → 0, 1, 2).
        public static List<int> Stages(IEnumerable<int> objectiveStages)
        {
            var stages = new List<int>();
            foreach (int s in objectiveStages)
                if (!stages.Contains(s)) stages.Add(s);
            stages.Sort();
            return stages;
        }

        // Todos os objetivos da etapa atual terminaram: vai para a próxima etapa, ou vence se era a última.
        // Devolve a etapa seguinte; "won" = acabou a fase.
        public static int AdvanceStage(int stageIndex, int stageCount, out bool won)
        {
            won = stageIndex + 1 >= stageCount;
            return won ? stageIndex : stageIndex + 1;
        }

        // Objetivo de lista ("leve os 3 móveis", "desligue os instrumentos"): lista vazia nunca cumpre.
        public static bool AllDone(int done, int total) => total > 0 && done >= total;

        // "Cheguem ao sótão": a equipe inteira (wholeTeam) ou basta um. Sem jogadores: não cumpre.
        public static bool AreaReached(int inside, int total, bool wholeTeam) =>
            total > 0 && (wholeTeam ? inside >= total : inside > 0);
    }
}
