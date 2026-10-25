using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // MARCADOR: um lugar onde um jogador nasce. Arraste quantos quiser para a cena
    // (GameObject vazio + este componente). O 1º jogador nasce no 1º marcador, e assim por diante
    // (a ordem é a da Hierarchy). Faltou marcador? Os extras nascem ao lado, em fila.
    // Sem nenhum marcador, vale a posição do PlayerSpawner (cenas antigas).
    public class SpawnPoint : MonoBehaviour
    {
        static readonly List<SpawnPoint> all = new();
        public static IReadOnlyList<SpawnPoint> All => all;

        void OnEnable()
        {
            all.Add(this);
            // Ordem da Hierarchy, para "o 1º marcador" ser previsível.
            all.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        void OnDisable() => all.Remove(this);

        // No editor: um boneco de 2 m e uma seta para onde o jogador olha ao nascer.
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 2f, 0.6f));
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}
