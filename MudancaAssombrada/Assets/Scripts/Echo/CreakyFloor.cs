using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // PISO QUE RANGE (LEVEL_DESIGN_FASE1.md MEC-11): uma área de tábuas soltas. Passo dado aqui
    // RANGE: o som vai até 7 m (em vez de 2,5) e é o rangido alto. A Cida ouve pelo chão onde os
    // colegas estão no andar de cima; o fantasma também. Vale para todo mundo, até o Marcelo.
    //
    // É só uma caixa (sem colisor, para não atrapalhar a mira nem o NavMesh): quem dá o passo
    // (CaneTapper) pergunta "CreakyFloor.At(pé)?".
    public class CreakyFloor : MonoBehaviour
    {
        static readonly List<CreakyFloor> all = new();

        [Tooltip("Tamanho da área (metros), a partir do centro do objeto.")]
        [SerializeField] Vector3 size = new Vector3(2f, 1f, 2f);
        [Tooltip("Até onde se ouve o passo aqui.")]
        [SerializeField] float stepRange = 7f;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        // O pé está numa área que range? Devolve o alcance do passo (0 = não range).
        public static float RangeAt(Vector3 feet)
        {
            foreach (var c in all)
            {
                Vector3 local = c.transform.InverseTransformPoint(feet);
                if (Mathf.Abs(local.x) <= c.size.x / 2 && Mathf.Abs(local.z) <= c.size.z / 2 && local.y >= -0.5f && local.y <= c.size.y)
                    return c.stepRange;
            }
            return 0f;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * size.y / 2, size);
        }
    }
}
