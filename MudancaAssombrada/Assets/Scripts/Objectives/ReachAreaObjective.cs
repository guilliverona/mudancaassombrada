using UnityEngine;

namespace MudancaAssombrada
{
    // "Cheguem ao sótão." A área é o BoxCollider deste objeto (marque Is Trigger).
    // Ajuste o tamanho da caixa verde na cena para cobrir o lugar.
    [RequireComponent(typeof(BoxCollider))]
    public class ReachAreaObjective : Objective
    {
        [SerializeField] string label = "Cheguem ao local";
        [Tooltip("Ligado: a equipe inteira precisa estar lá. Desligado: basta um.")]
        [SerializeField] bool wholeTeam = true;

        BoxCollider area;

        void Awake()
        {
            area = GetComponent<BoxCollider>();
            area.isTrigger = true; // a área não bloqueia ninguém
        }

        // Contamos pela POSIÇÃO de cada jogador (e não por eventos de física): os bonecos dos
        // outros jogadores só são "teleportados" pela rede, e isso não gera eventos de colisão.
        int Inside
        {
            get
            {
                int count = 0;
                foreach (var p in NetworkPlayer.All)
                    if (area.bounds.Contains(p.transform.position)) count++;
                return count;
            }
        }

        public override bool IsComplete => MatchRules.AreaReached(Inside, NetworkPlayer.All.Count, wholeTeam);

        public override string Progress =>
            wholeTeam ? $"{label}: {Inside}/{NetworkPlayer.All.Count}" : label;
    }
}
