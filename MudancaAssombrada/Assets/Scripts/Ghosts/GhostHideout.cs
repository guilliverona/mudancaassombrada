using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // MARCADOR: um ESCONDERIJO de fantasma, de onde ele sai e para onde foge.
    // O espelho do Imitador é um tipo de esconderijo (Mirror); a Maestrina pode ter os
    // instrumentos, o Bibliotecário as estantes. Cada fantasma procura o TIPO que é dele:
    //   GhostHideout.Nearest<Mirror>(posição)
    // Esconderijo novo = uma classe vazia que herda desta (ou use esta mesma, genérica).
    public class GhostHideout : MonoBehaviour
    {
        static readonly List<GhostHideout> all = new();
        public static IReadOnlyList<GhostHideout> All => all;

        [Tooltip("Distância à frente do esconderijo onde o fantasma \"sai\" (precisa cair no chão andável).")]
        [SerializeField] float exitDistance = 0.8f;

        protected virtual void OnEnable() => all.Add(this);
        protected virtual void OnDisable() => all.Remove(this);

        // O ponto no chão, na frente do esconderijo. Ele olha para o seu eixo Z (seta azul).
        public Vector3 ExitPoint
        {
            get
            {
                Vector3 p = transform.position + transform.forward * exitDistance;
                // Pendurado no alto (um espelho na parede): procura o chão andável logo abaixo (até 3 m).
                return NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : p;
            }
        }

        // <T>: "do tipo que eu pedir". Nearest<Mirror> só olha espelhos.
        public static T Nearest<T>(Vector3 position, T except = null) where T : GhostHideout
        {
            T best = null;
            float bestDistance = float.MaxValue;
            foreach (var h in all)
            {
                if (h is not T typed || typed == except) continue;
                float d = Vector3.Distance(position, typed.ExitPoint);
                if (d < bestDistance) { bestDistance = d; best = typed; }
            }
            return best;
        }

        public static T Farthest<T>(Vector3 position) where T : GhostHideout
        {
            T best = null;
            float bestDistance = -1f;
            foreach (var h in all)
            {
                if (h is not T typed) continue;
                float d = Vector3.Distance(position, typed.ExitPoint);
                if (d > bestDistance) { bestDistance = d; best = typed; }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            // No editor: uma bolinha mostra onde o fantasma sai daqui.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.forward * exitDistance, 0.25f);
        }
    }
}
