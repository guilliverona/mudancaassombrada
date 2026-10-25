using UnityEngine;

namespace MudancaAssombrada
{
    // Um COMPORTAMENTO de fantasma: o "jeito" dele assombrar (caçar pelo som, imitar, reger).
    // Fica no mesmo objeto que o Ghost, que chama os métodos abaixo, SÓ NO HOST.
    // Nos clientes nada disso roda: eles só veem a posição e o estado que o host manda.
    //
    // VÁRIOS COMPORTAMENTOS NO MESMO FANTASMA (o Inquilino): só UM pode mexer no corpo por vez
    // (andar, mudar o estado, ligar os passos). A cada frame o Ghost pergunta a todos
    // "quanto você quer o controle agora?" (ServerPriority) e entrega o volante a quem quer mais.
    //   - Quem está no volante: recebe ServerTick e pode mandar no Ghost (HasControl = true).
    //   - Os outros: continuam PERCEBENDO (ouvindo, contando tempo pelos eventos), só não agem.
    // Com um comportamento só (Bibliotecário, Imitador) ele está sempre no volante.
    //
    // Comportamento novo = uma classe que herda desta e preenche os métodos.
    [RequireComponent(typeof(Ghost))]
    public abstract class GhostBehaviour : MonoBehaviour
    {
        protected Ghost Ghost { get; private set; }

        // Este comportamento está no volante agora?
        protected bool HasControl => Ghost.Active == this;

        protected virtual void Awake() => Ghost = GetComponent<Ghost>();

        // A partida começou (o fantasma nasceu na rede). Ainda sem volante: só prepare o estado.
        public virtual void ServerStart() { }

        // Quanto quer o controle AGORA (maior ganha; empate fica com quem já está).
        // Ex.: patrulhando = 1, caçando = 10. Menor que 0 = não quer de jeito nenhum.
        public virtual int ServerPriority() => 1;

        // Ganhou o volante: aplique no Ghost o que estava fazendo (estado, velocidade, destino, passos).
        public virtual void ServerGainControl() { }

        // Perdeu o volante para outro comportamento.
        public virtual void ServerLoseControl() { }

        // Uma vez por frame, SÓ para quem está no volante.
        public abstract void ServerTick();

        // Uma vez por frame para quem NÃO está no volante: dá para contar tempo e se preparar
        // (e então pedir o volante pela prioridade), mas não mexa no corpo aqui.
        public virtual void ServerIdleTick() { }

        // A fase reiniciou: o Ghost já voltou para casa, o comportamento recomeça do zero.
        public virtual void ServerReset() => ServerStart();

        // Um jogador apontou para o fantasma disfarçado: foi desmascarado. (Todos ficam sabendo.)
        public virtual void ServerUnmasked() { }

        // O fantasma acabou de derrubar este jogador (ele ficou caído). (Todos ficam sabendo.)
        public virtual void ServerCaught(NetworkPlayer player) { }
    }
}
