using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Leva os sons de um computador para os outros.
    // Os passos do Tiago precisam aparecer no eco da Cida, e o fantasma (que pensa no host)
    // precisa ouvir os passos de todo mundo.
    //
    // Fica num objeto da cena com NetworkObject. Objetos de rede que já estão na cena
    // "nascem" sozinhos para todos quando o host inicia a partida.
    public class SoundRelay : NetworkBehaviour
    {
        public override void OnNetworkSpawn() => EchoSystem.NetworkBroadcast = Broadcast;

        public override void OnNetworkDespawn()
        {
            if (EchoSystem.NetworkBroadcast == Broadcast) EchoSystem.NetworkBroadcast = null;
        }

        void Broadcast(Vector3 origin, float range, float intensity, GameObject source)
        {
            // Só quem é DONO da fonte do som manda ele para a rede:
            //  - meus passos, minha bengala, meu apito: o meu boneco é meu  -> envio
            //  - passos do fantasma: o fantasma é do host                    -> o host envia
            // Sons de objetos sem rede ficam só locais.
            // Sem essa regra, cada computador reenviaria o mesmo som e ele chegaria repetido.
            if (source == null || !source.TryGetComponent(out NetworkObject netObj) || !netObj.IsOwner) return;

            SoundRpc(origin, range, intensity, netObj);
        }

        // SendTo.NotMe: roda em todos os OUTROS computadores (eu já tive o som localmente).
        // NetworkObjectReference: não dá para mandar um GameObject pela rede, mas dá para mandar
        // a "identidade de rede" dele, e cada computador encontra a sua própria cópia.
        [Rpc(SendTo.NotMe)]
        void SoundRpc(Vector3 origin, float range, float intensity, NetworkObjectReference sourceRef)
        {
            GameObject source = sourceRef.TryGet(out NetworkObject netObj) ? netObj.gameObject : null;
            EchoSystem.EmitLocal(origin, range, intensity, source);
        }
    }
}
