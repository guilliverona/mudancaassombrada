using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MudancaAssombrada
{
    // Algo que dá para USAR apertando Interagir (E): um instrumento para desligar, uma porta,
    // um interruptor, um bilhete... Funciona uma vez (fica "usado") até a fase reiniciar.
    //
    // Precisa de um Collider (para a mira acertar) e de um NetworkObject (todos precisam saber
    // que foi usado). Quem decide é o host, como nos móveis.
    //
    // "Ao Usar" (UnityEvent) aparece no Inspector: dá para ligar efeitos SEM código,
    // arrastando objetos (ex.: desligar a luz de um instrumento, tocar um som).
    public class Interactable : NetworkBehaviour, IInteractionTarget
    {
        static readonly List<Interactable> all = new();
        public static IReadOnlyList<Interactable> All => all;

        [Tooltip("O verbo da dica na tela: \"Desligar\" vira \"Desligar Piano\".")]
        [SerializeField] string actionName = "Usar";
        [Tooltip("Personagens que podem usar. Vazio = qualquer um (ex.: só o Tiago, que é imune à música).")]
        [SerializeField] CharacterDefinition[] onlyFor = new CharacterDefinition[0];
        [SerializeField] UnityEvent onUsed = new();

        readonly NetworkVariable<bool> used = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsUsed => used.Value;
        public string Prompt => $"{actionName} {name}";

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn() => used.OnValueChanged += OnUsedChanged;
        public override void OnNetworkDespawn() => used.OnValueChanged -= OnUsedChanged;

        // Roda em TODOS os computadores quando muda: cada um toca os seus efeitos.
        void OnUsedChanged(bool previous, bool current)
        {
            if (current) onUsed.Invoke();
        }

        // Este personagem pode usar? (A dica na tela fica apagada para quem não pode.)
        public bool CanBeUsedBy(CharacterDefinition character)
        {
            if (onlyFor.Length == 0) return true;
            foreach (var c in onlyFor)
                if (c == character) return true;
            return false;
        }

        // Já usado: nada a fazer. Personagem errado: "Não consigo", apagado.
        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (IsUsed || !IsSpawned) return InteractionPrompt.None;
            bool canUse = CanBeUsedBy(player.Character);
            return new InteractionPrompt(canUse ? Prompt : "Não consigo", canUse);
        }

        public void Interact(PlayerInteractor player) => RequestUse();

        public void RequestUse() => UseRpc();

        [Rpc(SendTo.Server)]
        void UseRpc(RpcParams rpcParams = default)
        {
            if (used.Value) return;
            // O host confere de novo quem pediu: não dá para "burlar" pelo cliente.
            if (!NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
            var character = client.PlayerObject != null ? client.PlayerObject.GetComponent<CharacterSwitcher>() : null;
            if (character != null && !CanBeUsedBy(character.Current)) return;
            used.Value = true;
        }

        // Só no host (ao reiniciar a fase).
        public void ServerReset() => used.Value = false;
    }
}
