using UnityEngine;

namespace MudancaAssombrada
{
    // Um COMPORTAMENTO de fantasma: o "jeito" dele assombrar (caçar pelo som, imitar, reger).
    // Fica no mesmo objeto que o Ghost, que chama os três métodos abaixo, SÓ NO HOST.
    // Nos clientes nada disso roda: eles só veem a posição e o estado que o host manda.
    //
    // Comportamento novo = uma classe que herda desta e preenche os métodos.
    [RequireComponent(typeof(Ghost))]
    public abstract class GhostBehaviour : MonoBehaviour
    {
        protected Ghost Ghost { get; private set; }

        protected virtual void Awake() => Ghost = GetComponent<Ghost>();

        // A partida começou (o fantasma nasceu na rede).
        public virtual void ServerStart() { }

        // Uma vez por frame, enquanto ele não pegou ninguém.
        public abstract void ServerTick();

        // A fase reiniciou: o Ghost já voltou para casa, o comportamento recomeça do zero.
        public virtual void ServerReset() => ServerStart();

        // Um jogador apontou para o fantasma disfarçado: foi desmascarado.
        public virtual void ServerUnmasked() { }
    }
}
