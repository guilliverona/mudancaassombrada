using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Uma LANTERNA no caminhão ou um pacote de PILHAS pela casa (Interagir). Some quando pego e
    // volta quando a fase reinicia.
    //   - Lanterna: dá a lanterna (Flashlight), com a bateria cheia. Quem já tem uma não pega outra.
    //   - Pilhas: recarregam um pedaço da lanterna de quem pega (precisa ter uma).
    // A Cida vê a dica "Não preciso." (a vantagem dela: enxerga pelo eco, sem luz nenhuma).
    // ESCONDIDAS (startsHidden): começam invisíveis e só aparecem quando algo as revela (a gaveta
    // que abre sozinha, os livros que caem: o HouseScare chama ServerReveal).
    //
    // Precisa de um Collider (para a mira) e de um NetworkObject. Quem decide é o host.
    public class FlashlightPickup : NetworkBehaviour, IInteractionTarget
    {
        static readonly List<FlashlightPickup> all = new();
        public static IReadOnlyList<FlashlightPickup> All => all;

        [Tooltip("Ligado: é uma lanterna. Desligado: são pilhas.")]
        [SerializeField] bool givesFlashlight = true;
        [Tooltip("Pilhas: quanto recarregam (0,4 = 40% da bateria).")]
        [SerializeField, Range(0f, 1f)] float charge = 0.4f;
        [Tooltip("Ligado: começa escondida e só aparece com ServerReveal (um susto da casa larga a pilha).")]
        [SerializeField] bool startsHidden;

        readonly NetworkVariable<bool> taken = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        readonly NetworkVariable<bool> revealed = new(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        bool Visible => !taken.Value && revealed.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer) revealed.Value = !startsHidden;
            taken.OnValueChanged += OnTakenChanged;
            revealed.OnValueChanged += OnRevealedChanged;
            Show(Visible);
        }

        public override void OnNetworkDespawn()
        {
            taken.OnValueChanged -= OnTakenChanged;
            revealed.OnValueChanged -= OnRevealedChanged;
        }

        // Roda em TODOS: some (ou volta) e faz o barulhinho de pegar.
        void OnTakenChanged(bool previous, bool current)
        {
            Show(Visible);
            if (current) GameAudio.Play(givesFlashlight ? "lanterna_pega" : "pilhas_troca", transform.position);
        }

        void OnRevealedChanged(bool previous, bool current) => Show(Visible);

        void Show(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = visible;
        }

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (!IsSpawned || !Visible) return InteractionPrompt.None;
            if (player.Character != null && player.Character.blind) return new InteractionPrompt("Não preciso.", false);

            var flashlight = player.GetComponent<Flashlight>();
            if (flashlight == null) return InteractionPrompt.None;
            if (givesFlashlight)
                return flashlight.Has ? new InteractionPrompt("Já tenho uma lanterna", false)
                                      : new InteractionPrompt("Pegar lanterna", true);
            if (!flashlight.Has) return new InteractionPrompt("Pilhas (preciso de uma lanterna)", false);
            return flashlight.Charge01 >= 0.99f ? new InteractionPrompt("Pilhas (bateria cheia)", false)
                                                : new InteractionPrompt("Trocar as pilhas", true);
        }

        public void Interact(PlayerInteractor player) => TakeRpc();

        [Rpc(SendTo.Server)]
        void TakeRpc(RpcParams rpcParams = default)
        {
            if (!Visible) return; // dois pegaram ao mesmo tempo (o primeiro leva), ou ainda escondida
            // O host confere de novo quem pediu (como no Interactable).
            if (!NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
            var player = client.PlayerObject;
            if (player == null || !player.TryGetComponent(out Flashlight flashlight)) return;
            if (player.TryGetComponent(out CharacterSwitcher who) && who.Current != null && who.Current.blind) return;

            if (givesFlashlight)
            {
                if (flashlight.Has) return;
                flashlight.ServerGive();
            }
            else
            {
                if (!flashlight.Has) return;
                flashlight.ServerCharge(charge);
            }
            taken.Value = true;
        }

        // Só no host: a pilha escondida aparece (caiu da estante, a gaveta abriu).
        public void ServerReveal() => revealed.Value = true;

        // Só no host (ao reiniciar a fase): volta ao lugar, e a escondida se esconde de novo.
        public void ServerReset()
        {
            taken.Value = false;
            revealed.Value = !startsHidden;
        }
    }
}
