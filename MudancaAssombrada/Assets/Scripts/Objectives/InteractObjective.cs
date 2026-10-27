using UnityEngine;

namespace MudancaAssombrada
{
    // "Desligue os instrumentos." Cumprido quando TODOS os objetos da lista foram usados.
    public class InteractObjective : Objective
    {
        [SerializeField] string label = "Objetos usados";
        [SerializeField] Interactable[] targets = new Interactable[0];

        int UsedCount
        {
            get
            {
                int count = 0;
                foreach (var t in targets)
                    if (t != null && t.IsUsed) count++;
                return count;
            }
        }

        public override bool IsComplete => MatchRules.AllDone(UsedCount, targets.Length);

        public override string Progress => $"{label}: {UsedCount}/{targets.Length}";
    }
}
