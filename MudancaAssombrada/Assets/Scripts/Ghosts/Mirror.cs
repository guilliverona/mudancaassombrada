using UnityEngine;

namespace MudancaAssombrada
{
    // Um espelho da Casa do Espelho: o esconderijo do Imitador. Ele sai de um espelho, puxa a
    // vítima para dentro de um espelho e, desmascarado, foge para OUTRO espelho.
    // Tudo o que um esconderijo faz está em GhostHideout; esta classe dá o TIPO "espelho", para o
    // Imitador procurar só espelhos (GhostHideout.Nearest<Mirror>), e sabe se ele está DENTRO.
    public class Mirror : GhostHideout
    {
        const float OccupiedDistance = 1.5f; // perto assim da saída, escondido = está "dentro"

        // O Imitador está escondido neste espelho? (O diapasão da Cida ouve o vidro "rachado".)
        // Não precisa de rede: o estado e a posição do fantasma já chegam a todos os computadores.
        public bool IsOccupied
        {
            get
            {
                Vector3 exit = ExitPoint;
                foreach (var ghost in Ghost.All)
                {
                    if (!ghost.IsSpawned || !ghost.IsInsideMirror) continue;
                    if (!ghost.TryGetComponent(out GhostImitator _)) continue;
                    // Distância no chão (o centro do fantasma fica no meio do corpo, acima da saída).
                    Vector3 d = ghost.transform.position - exit;
                    d.y = 0;
                    if (d.magnitude < OccupiedDistance && Mathf.Abs(ghost.transform.position.y - exit.y) < 2.5f) return true;
                }
                return false;
            }
        }
    }
}
