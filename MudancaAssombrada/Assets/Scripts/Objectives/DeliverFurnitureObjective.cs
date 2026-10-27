using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // "Leve os móveis para o caminhão."
    // Lista vazia = TODOS os móveis da fase. Com itens = só esses (ex.: na fase final,
    // o cartaz do teatro, a partitura e o diário).
    public class DeliverFurnitureObjective : Objective
    {
        [SerializeField] string label = "Móveis no caminhão";
        [Tooltip("Vazio = todos os móveis da fase.")]
        [SerializeField] Furniture[] targets = new Furniture[0];

        IReadOnlyList<Furniture> Targets => targets.Length > 0 ? (IReadOnlyList<Furniture>)targets : Furniture.All;

        int DeliveredCount
        {
            get
            {
                int count = 0;
                foreach (var f in Targets)
                    if (f != null && TruckZone.IsDelivered(f)) count++;
                return count;
            }
        }

        public override bool IsComplete => MatchRules.AllDone(DeliveredCount, Targets.Count);

        public override string Progress => $"{label}: {DeliveredCount}/{Targets.Count}";
    }
}
