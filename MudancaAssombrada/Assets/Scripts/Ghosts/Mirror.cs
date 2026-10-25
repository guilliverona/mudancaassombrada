using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // Um espelho da Casa do Espelho: a "porta" do Imitador. Ele sai de um espelho, puxa a vítima
    // para dentro de um espelho e, desmascarado, foge para OUTRO espelho.
    // Coloque em qualquer moldura da cena; o fantasma acha todos pelo registro (Mirror.All).
    public class Mirror : MonoBehaviour
    {
        static readonly List<Mirror> all = new();
        public static IReadOnlyList<Mirror> All => all;

        [Tooltip("Distância à frente do espelho onde o fantasma \"sai\" (precisa cair no chão andável).")]
        [SerializeField] float exitDistance = 0.8f;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        // O ponto no chão, na frente do vidro. O espelho olha para o seu eixo Z (seta azul).
        public Vector3 ExitPoint
        {
            get
            {
                Vector3 p = transform.position + transform.forward * exitDistance;
                // Espelho pendurado no alto: procura o chão andável logo abaixo (até 3 m).
                return NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : p;
            }
        }

        public static Mirror Nearest(Vector3 position, Mirror except = null)
        {
            Mirror best = null;
            float bestDistance = float.MaxValue;
            foreach (var m in all)
            {
                if (m == except) continue;
                float d = Vector3.Distance(position, m.ExitPoint);
                if (d < bestDistance) { bestDistance = d; best = m; }
            }
            return best;
        }

        public static Mirror Farthest(Vector3 position)
        {
            Mirror best = null;
            float bestDistance = -1f;
            foreach (var m in all)
            {
                float d = Vector3.Distance(position, m.ExitPoint);
                if (d > bestDistance) { bestDistance = d; best = m; }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            // No editor: uma bolinha mostra onde o fantasma sai deste espelho.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ExitPoint, 0.25f);
        }
    }
}
