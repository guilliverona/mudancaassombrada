using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // FRÁGIL (LEVEL_DESIGN_FASE1_MANSAO.md §8, D13): a talha de barro, a cristaleira, a lanterna
    // mágica, o espelho de pé. Bater faz barulho (o SoundVoice do móvel troca a batida comum pelo
    // som de vidro e barro) e dá DESCONTO no bilhete do chefe. Nada quebra de vez.
    //
    // Quem conta é o dono do móvel (quem simula a física, como no barulho de batida do Furniture):
    // ele avisa o host, que guarda as pancadas. Quicar logo depois é a mesma (PeriodFindRules).
    [RequireComponent(typeof(Furniture))]
    public class Fragile : NetworkBehaviour
    {
        static readonly List<Fragile> all = new();
        public static IReadOnlyList<Fragile> All => all;

        public const int DiscountPerHit = 5; // R$ por pancada

        readonly NetworkVariable<int> hits = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        float lastHit = PeriodFindRules.Never;

        public int Hits => hits.Value;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void OnCollisionEnter(Collision collision)
        {
            if (IsSpawned && !IsOwner) return;
            float impact = collision.relativeVelocity.magnitude;
            if (!PeriodFindRules.CountsHit(impact, Time.time, lastHit)) return;
            lastHit = Time.time;
            HitRpc();
            GameCues.EmitLocal(new GameCue(transform.position, 0f, Senses.Hearing | Senses.Sight,
                $"Cuidado com {name}! (desconto no bilhete)", gameObject));
        }

        [Rpc(SendTo.Server)]
        void HitRpc()
        {
            var game = GameManager.Instance;
            if (game != null && game.Outcome != MatchOutcome.Playing) return;
            hits.Value++;
        }

        public void ServerReset() => hits.Value = 0;
    }
}
