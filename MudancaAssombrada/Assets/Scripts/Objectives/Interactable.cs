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
    //
    // "Segurar" (holdSeconds > 0): é preciso manter a tecla apertada e a mira no objeto por um
    // tempo (ex.: a Cida AFINANDO o gramofone, 2 s). Soltar antes cancela.
    public class Interactable : NetworkBehaviour, IHoldInteractionTarget
    {
        static readonly List<Interactable> all = new();
        public static IReadOnlyList<Interactable> All => all;

        [Tooltip("O verbo da dica na tela: \"Desligar\" vira \"Desligar Piano\".")]
        [SerializeField] string actionName = "Usar";
        [Tooltip("Personagens que podem usar. Vazio = qualquer um (ex.: só o Tiago, que é imune à música).")]
        [SerializeField] CharacterDefinition[] onlyFor = new CharacterDefinition[0];
        [Tooltip("Segundos segurando a tecla para usar (0 = um toque).")]
        [SerializeField, Min(0f)] float holdSeconds;
        [Tooltip("Quem não pode usar não vê dica nenhuma (em vez de \"Não consigo\"). Útil quando o " +
                 "objeto tem outra coisa a fazer para os outros (ex.: tocar o piano).")]
        [SerializeField] bool hideFromOthers;
        [SerializeField] UnityEvent onUsed = new();

        float holdStart = -1f; // só neste computador: desde quando a tecla está segurada aqui

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

        // Já usado: nada a fazer. Personagem errado: "Não consigo", apagado (ou nada, com hideFromOthers).
        // Segurando: mostra o progresso.
        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (IsUsed || !IsSpawned) return InteractionPrompt.None;
            bool canUse = CanBeUsedBy(player.Character);
            if (!canUse) return hideFromOthers ? InteractionPrompt.None : new InteractionPrompt("Não consigo", false);
            if (holdStart >= 0f)
                return new InteractionPrompt($"{actionName}... {Mathf.FloorToInt((Time.time - holdStart) / holdSeconds * 100f)}%", true);
            return new InteractionPrompt(holdSeconds > 0f ? $"{Prompt} (segure)" : Prompt, true);
        }

        public void Interact(PlayerInteractor player)
        {
            if (holdSeconds <= 0f) RequestUse();
            else holdStart = Time.time; // o Update conta; o PlayerInteractor avisa se soltar (HoldStop)
        }

        public void HoldStop(PlayerInteractor player) => holdStart = -1f;

        void Update()
        {
            if (holdStart < 0f || Time.time - holdStart < holdSeconds) return;
            holdStart = -1f;
            RequestUse();
        }

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
