using Unity.Collections; // FixedString512Bytes
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Em que condição o jogador está. Fica numa NetworkVariable: todos sabem.
    public enum PlayerState : byte
    {
        Normal,
        Pulled,      // sendo arrastado (Imitador para o espelho)
        Hypnotized,  // parado ouvindo a música (Maestrina, Fase 4)
        Down,        // pego por um fantasma: caído até um colega ajudar
    }

    // Fica no Player. A PORTA DE ENTRADA única para tudo que um fantasma (ou a fase) faz COM
    // o jogador: o fantasma só diz "este jogador agora está Caído/Puxado/Hipnotizado", e este
    // componente cuida das consequências (travar o movimento, largar o móvel, avisar a equipe).
    // Mecânica nova que afeta o jogador = um valor novo no PlayerState + o efeito dele aqui.
    //
    // Também é um alvo de interação: olhando para um colega caído, aparece "Ajudar fulano".
    // Regra do roteiro: um falha, os outros ficam vulneráveis. A equipe só perde se TODOS caírem.
    //
    // EM REDE: só o host muda o estado; cada computador aplica o efeito no SEU boneco.
    public class PlayerCondition : NetworkBehaviour, IInteractionTarget
    {
        readonly NetworkVariable<PlayerState> state = new(
            PlayerState.Normal, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Quem causou (ex.: o fantasma que está puxando). Vazio = ninguém.
        readonly NetworkVariable<NetworkObjectReference> source = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] float cueRange = 15f;

        PlayerMovement movement;
        FurnitureCarrier carrier;
        CharacterSwitcher character;

        public PlayerState State => state.Value;
        public bool IsDown => state.Value == PlayerState.Down;
        // Pode agir (andar livre, pegar, usar)?
        public bool IsFree => state.Value == PlayerState.Normal;

        string DisplayName => character != null ? character.Current.displayName : name;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            carrier = GetComponent<FurnitureCarrier>();
            character = GetComponent<CharacterSwitcher>();
        }

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += OnStateChanged;
            source.OnValueChanged += OnSourceChanged;
            ApplyEffects();
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnStateChanged;
            source.OnValueChanged -= OnSourceChanged;
        }

        // ---------- Só no host ----------

        // Muda a condição. "from" = quem causou (pode ser null).
        public void ServerSet(PlayerState newState, NetworkObject from = null)
        {
            if (!IsServer || state.Value == newState) return;
            source.Value = from != null ? new NetworkObjectReference(from) : default;
            state.Value = newState;

            // Avisos para a equipe, saindo deste jogador (o host os manda pelo fantasma/jogador dono).
            if (newState == PlayerState.Down)
            {
                string where = Room.InRoomText(transform.position); // " (Camarim)", se houver cômodo marcado
                Cue(Senses.Sight, $"{DisplayName} caiu{where}! Vá até lá e ajude ({GameInput.KeyLabel(GameInput.Interact)}).");
                Cue(Senses.Hearing, $"{DisplayName} pede socorro{where}!");
                Cue(Senses.Vibration, $"Um baque no chão: {DisplayName} caiu.");
            }
        }

        // Só volta ao normal se ainda estiver NESTA condição (ex.: o Imitador solta quem ele puxava,
        // mas não pode "levantar" alguém que outro fantasma derrubou).
        public void ServerClear(PlayerState only)
        {
            if (state.Value == only) ServerSet(PlayerState.Normal);
        }

        public void ServerReset() => ServerSet(PlayerState.Normal);

        // O SoundRelay só leva avisos de quem é DONO da fonte, e o host não é dono do boneco dos
        // outros. Por isso este aviso vai por um RPC próprio, direto para todos (SendTo.Everyone).
        void Cue(Senses senses, string text) => CueRpc(senses, new FixedString512Bytes(text));

        [Rpc(SendTo.Everyone)]
        void CueRpc(Senses senses, FixedString512Bytes text) =>
            GameCues.EmitLocal(new GameCue(transform.position, cueRange, senses, text.ToString(), gameObject));

        // ---------- Resgate (alvo de interação) ----------

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (!IsDown || player.Player == GetComponent<NetworkPlayer>()) return InteractionPrompt.None;
            return new InteractionPrompt($"Ajudar {DisplayName}", true);
        }

        public void Interact(PlayerInteractor player) => RescueRpc();

        [Rpc(SendTo.Server)]
        void RescueRpc()
        {
            if (!IsDown) return;
            Cue(Senses.Sight | Senses.Hearing, $"{DisplayName} levantou!");
            ServerSet(PlayerState.Normal);
        }

        // ---------- Efeitos (em todos; o movimento só no dono) ----------

        void OnStateChanged(PlayerState previous, PlayerState current) => ApplyEffects();
        void OnSourceChanged(NetworkObjectReference previous, NetworkObjectReference current) => ApplyEffects();

        void ApplyEffects()
        {
            if (!IsOwner || movement == null) return;

            Transform puller = state.Value == PlayerState.Pulled && source.Value.TryGet(out NetworkObject from) ? from.transform : null;
            movement.PulledBy = puller;
            movement.Frozen = state.Value == PlayerState.Down || state.Value == PlayerState.Hypnotized;

            // Caído não segura móvel.
            if (!IsFree && carrier != null) carrier.Drop();
        }
    }
}
