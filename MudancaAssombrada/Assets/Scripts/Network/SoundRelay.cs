using Unity.Collections; // FixedString512Bytes
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Leva os SONS e os AVISOS (GameCue) de um computador para os outros.
    // Os passos do Tiago precisam aparecer no eco da Cida, o fantasma (que pensa no host)
    // precisa ouvir os passos de todo mundo, e o "socorro!" de um precisa chegar aos outros.
    //
    // Fica no prefab "Sistemas da Fase", junto do GameManager (o mesmo objeto de rede).
    // Objetos de rede que já estão na cena "nascem" sozinhos para todos quando o host inicia a partida.
    public class SoundRelay : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            EchoSystem.NetworkBroadcast = Broadcast;
            GameCues.NetworkBroadcast = BroadcastCue;
        }

        public override void OnNetworkDespawn()
        {
            if (EchoSystem.NetworkBroadcast == Broadcast) EchoSystem.NetworkBroadcast = null;
            if (GameCues.NetworkBroadcast == BroadcastCue) GameCues.NetworkBroadcast = null;
        }

        // Só quem é DONO da fonte manda para a rede:
        //  - meus passos, minha bengala, meu apito: o meu boneco é meu  -> envio
        //  - passos do fantasma: o fantasma é do host                    -> o host envia
        // Sons de objetos sem rede ficam só locais.
        // Sem essa regra, cada computador reenviaria o mesmo som e ele chegaria repetido.
        static bool IsMine(GameObject source, out NetworkObject netObj)
        {
            netObj = null;
            return source != null && source.TryGetComponent(out netObj) && netObj.IsOwner;
        }

        void Broadcast(SoundEvent sound)
        {
            if (!IsMine(sound.Source, out var netObj)) return;
            SoundRpc(sound.Position, sound.Range, sound.Intensity, sound.Type, netObj);
        }

        void BroadcastCue(GameCue cue)
        {
            if (!IsMine(cue.Source, out var netObj)) return;
            // FixedString: texto de tamanho fixo, que dá para mandar pela rede (150 letras cabem com folga, mesmo com acentos).
            string text = cue.Text.Length > 150 ? cue.Text.Substring(0, 150) : cue.Text;
            CueRpc(cue.Position, cue.Range, cue.Senses, new FixedString512Bytes(text), netObj);
        }

        // SendTo.NotMe: roda em todos os OUTROS computadores (eu já tive o som localmente).
        // NetworkObjectReference: não dá para mandar um GameObject pela rede, mas dá para mandar
        // a "identidade de rede" dele, e cada computador encontra a sua própria cópia.
        [Rpc(SendTo.NotMe)]
        void SoundRpc(Vector3 position, float range, float intensity, SoundType type, NetworkObjectReference sourceRef)
        {
            GameObject source = sourceRef.TryGet(out NetworkObject netObj) ? netObj.gameObject : null;
            EchoSystem.EmitLocal(new SoundEvent(position, range, intensity, type, source));
        }

        [Rpc(SendTo.NotMe)]
        void CueRpc(Vector3 position, float range, Senses senses, FixedString512Bytes text, NetworkObjectReference sourceRef)
        {
            GameObject source = sourceRef.TryGet(out NetworkObject netObj) ? netObj.gameObject : null;
            GameCues.EmitLocal(new GameCue(position, range, senses, text.ToString(), source));
        }
    }
}
