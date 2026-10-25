using UnityEngine;

namespace MudancaAssombrada
{
    // A câmera de espera da fase: o que se vê antes de o seu boneco nascer (vista de cima).
    // Quando o MEU jogador nasce, ela se desliga sozinha: a partir daí vemos pelos olhos dele.
    //
    // Antes, o NetworkPlayer procurava esta câmera pelo NOME ("Câmera de Espera").
    // Se alguém renomeasse o objeto, quebrava sem aviso. Agora a câmera é que escuta o evento.
    [RequireComponent(typeof(Camera))]
    public class LobbyCamera : MonoBehaviour
    {
        void OnEnable() => NetworkPlayer.LocalSpawned += OnLocalSpawned;
        void OnDisable() => NetworkPlayer.LocalSpawned -= OnLocalSpawned;

        // No Start (e não no OnEnable): a Unity não deixa desligar um objeto no meio do "ligar".
        void Start()
        {
            if (NetworkPlayer.Local != null) gameObject.SetActive(false); // o jogador já tinha nascido
        }

        void OnLocalSpawned(NetworkPlayer player) => gameObject.SetActive(false);
    }
}
