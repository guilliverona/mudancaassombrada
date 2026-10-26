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
    // Também é um alvo de interação: olhando para um colega caído, aparece "Ajudar fulano", e é
    // preciso SEGURAR a tecla até ele terminar de se levantar (ver "Resgate").
    // Regra do roteiro: um falha, os outros ficam vulneráveis. A equipe só perde se TODOS caírem.
    //
    // EM REDE: só o host muda o estado; cada computador aplica o efeito no SEU boneco.
    public class PlayerCondition : NetworkBehaviour, IHoldInteractionTarget
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
            CreateDownHitbox();
        }

        // ---------- Alvo do corpo caído ----------

        // O colisor do Player (CharacterController) é uma cápsula EM PÉ: com o corpo deitado no chão,
        // a mira só achava o "Ajudar" perto do quadril. Este gatilho cobre o corpo inteiro enquanto
        // o jogador está caído, e acompanha a pose da animação (a caixa dos renderers do boneco).
        // É um trigger: não empurra ninguém, só é achado pela mira (o projeto deixa raios acertarem triggers).
        [SerializeField] float downHitboxPadding = 0.2f; // folga em volta do corpo, em metros

        BoxCollider downHitbox;
        readonly Vector3[] corners = new Vector3[8];

        void CreateDownHitbox()
        {
            var go = new GameObject("Alvo do corpo caído");
            go.transform.SetParent(transform, false);
            downHitbox = go.AddComponent<BoxCollider>();
            downHitbox.isTrigger = true;
            downHitbox.enabled = false;
        }

        // Travado: caído, hipnotizado ou AINDA se levantando. Ao ser ajudado o estado volta a Normal
        // na hora, mas o jogador só se mexe quando a animação de levantar acabar (DollWalk.IsGettingUp).
        // Deitado/levantando, o mouse só vira a cabeça (BodyLocked).
        void ApplyFreeze()
        {
            bool gettingUp = character != null && character.Walk != null && character.Walk.IsGettingUp;
            bool down = state.Value == PlayerState.Down || gettingUp;
            movement.Frozen = down || state.Value == PlayerState.Hypnotized;
            movement.BodyLocked = down;
        }

        // Depois da animação (que mexe no corpo entre o Update e o LateUpdate).
        void LateUpdate()
        {
            // A animação de levantar termina sozinha, sem o estado mudar: confere todo frame (só o dono anda).
            if (IsOwner && movement != null) ApplyFreeze();

            bool down = IsSpawned && IsDown;
            if (downHitbox.enabled != down) downHitbox.enabled = down;
            if (down) FitDownHitbox();
        }

        // A caixa dos renderers visíveis do boneco, no espaço do Player (gira junto com ele).
        void FitDownHitbox()
        {
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // Desligados (ex.: o boneco de blocos quando há modelo) não contam: a caixa deles é vazia.
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer) continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    corners[i] = transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) * 2 - 1, ((i >> 1) & 1) * 2 - 1, ((i >> 2) & 1) * 2 - 1)));
                    if (!any) { min = max = corners[i]; any = true; }
                    min = Vector3.Min(min, corners[i]);
                    max = Vector3.Max(max, corners[i]);
                }
            }
            if (!any) return;
            downHitbox.center = (min + max) / 2f;
            downHitbox.size = max - min + Vector3.one * (downHitboxPadding * 2f);
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

        // Levantar um colega é SEGURAR a tecla: enquanto alguém segura (e está perto), o progresso
        // sobe e o caído vai se levantando (a animação acompanha o progresso). Soltou, tirou a mira
        // ou se afastou: o progresso volta e ele desaba de novo. Completo: está de pé, livre.
        // Quem conta o tempo e confere a distância é o HOST (ninguém "trapaceia" pela rede).
        [Header("Resgate")]
        [SerializeField] float rescueDuration = 8.4f;  // o tempo do clipe Levantar, em segundos
        [SerializeField] float rescueFallBack = 1.5f;  // soltou: em quantos segundos o progresso volta a zero
        [SerializeField] float rescueReach = 3.5f;     // longe disto (metros), o resgate para

        const ulong NoRescuer = ulong.MaxValue;
        // Quanto já levantou (0 a 1): a animação e a dica leem daqui.
        readonly NetworkVariable<float> rescue = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Quem está segurando a tecla agora (o ClientId do jogador). NoRescuer = ninguém.
        readonly NetworkVariable<ulong> rescuer = new(
            NoRescuer, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float RescueProgress => rescue.Value;

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (!IsDown || player.Player == GetComponent<NetworkPlayer>()) return InteractionPrompt.None;
            string hold = $"segure {GameInput.KeyLabel(GameInput.Interact)}";
            if (rescuer.Value == NoRescuer) return new InteractionPrompt($"Ajudar {DisplayName} ({hold})", true);
            if (rescuer.Value == player.Player.OwnerClientId)
                return new InteractionPrompt($"Ajudando {DisplayName}... {Mathf.FloorToInt(rescue.Value * 100f)}%", true);
            return new InteractionPrompt($"Alguém já está ajudando {DisplayName}", false);
        }

        public void Interact(PlayerInteractor player) => RescueHoldRpc(true);
        public void HoldStop(PlayerInteractor player) => RescueHoldRpc(false);

        // "RpcParams" diz QUEM mandou: só quem começou pode parar, e um segundo jogador não rouba a vez.
        [Rpc(SendTo.Server)]
        void RescueHoldRpc(bool holding, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (holding && IsDown && rescuer.Value == NoRescuer) rescuer.Value = sender;
            else if (!holding && rescuer.Value == sender) rescuer.Value = NoRescuer;
        }

        // Só no host: sobe o progresso enquanto o colega segura e está perto; senão, ele volta.
        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            if (!IsDown)
            {
                if (rescue.Value != 0f) rescue.Value = 0f;
                if (rescuer.Value != NoRescuer) rescuer.Value = NoRescuer;
                return;
            }

            var helper = RescuerPlayer();
            if (rescuer.Value != NoRescuer && !CanRescue(helper)) rescuer.Value = NoRescuer; // caiu, saiu ou se afastou
            bool helping = rescuer.Value != NoRescuer;

            float speed = helping ? 1f / rescueDuration : -1f / rescueFallBack;
            float next = Mathf.Clamp01(rescue.Value + speed * Time.deltaTime);
            if (next != rescue.Value) rescue.Value = next;

            if (next >= 1f)
            {
                Cue(Senses.Sight | Senses.Hearing, $"{DisplayName} levantou!");
                ServerSet(PlayerState.Normal); // o Update seguinte zera o progresso e quem ajudava
            }
        }

        NetworkPlayer RescuerPlayer()
        {
            if (rescuer.Value == NoRescuer) return null;
            foreach (var p in NetworkPlayer.All)
                if (p.OwnerClientId == rescuer.Value) return p;
            return null;
        }

        bool CanRescue(NetworkPlayer helper) =>
            helper != null && helper.Condition != null && helper.Condition.IsFree
            && Vector3.Distance(helper.transform.position, transform.position) <= rescueReach;

        // ---------- Efeitos (em todos; o movimento só no dono) ----------

        void OnStateChanged(PlayerState previous, PlayerState current) => ApplyEffects();
        void OnSourceChanged(NetworkObjectReference previous, NetworkObjectReference current) => ApplyEffects();

        void ApplyEffects()
        {
            if (!IsOwner || movement == null) return;

            Transform puller = state.Value == PlayerState.Pulled && source.Value.TryGet(out NetworkObject from) ? from.transform : null;
            movement.PulledBy = puller;
            ApplyFreeze();

            // Caído não segura móvel.
            if (!IsFree && carrier != null) carrier.Drop();
        }
    }
}
