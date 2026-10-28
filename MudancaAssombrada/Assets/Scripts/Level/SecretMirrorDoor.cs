using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // O ESPELHO-PORTA da Câmara Secreta (o E9 dos Camarins, LEVEL_DESIGN_FASE1_MANSAO.md D5).
    // Parece um espelho grande na parede, mas fecha a passagem para a Câmara, onde está o cartaz.
    //
    //   Fechado: só a Cida tem o que fazer aqui ("Afinar Espelho", segurando 2 s). Para os outros é
    //            um espelho comum, sem dica nenhuma. O diapasão revela: o vidro racha.
    //   Revelado: qualquer um vê a rachadura e pode "Empurrar Espelho".
    //   Aberto:   o espelho gira para dentro da Câmara e a passagem fica livre (também para o fantasma).
    //
    // Sem a Cida na partida, o espelho já começa rachado (plano B, R01): a relíquia nunca fica presa.
    // As regras ficam no SecretDoorRules (com testes); aqui é a rede, a dica e o desenho.
    //
    // NavMesh: o espelho fica de fora do bake (a passagem existe no mapa do fantasma) e um
    // NavMeshObstacle fecha o vão enquanto ele está no lugar.
    public class SecretMirrorDoor : NetworkBehaviour, IHoldInteractionTarget
    {
        static readonly List<SecretMirrorDoor> all = new();
        public static IReadOnlyList<SecretMirrorDoor> All => all;

        [Tooltip("Segundos que a Cida segura a tecla afinando.")]
        [SerializeField] float tuneSeconds = 2f;
        [Tooltip("A parte que gira ao abrir (o espelho com a moldura). A dobradiça é a origem dela.")]
        [SerializeField] Transform leaf;
        [SerializeField] float openAngle = -100f;
        [SerializeField] float openSpeed = 60f;      // graus por segundo
        [Tooltip("A rachadura: aparece quando o espelho é revelado.")]
        [SerializeField] GameObject crack;
        [SerializeField] string revealSound = "espelho_rachando";
        [SerializeField] string openSound = "arrastar_movel";

        readonly NetworkVariable<byte> state = new(
            (byte)SecretDoorState.Closed, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public SecretDoorState State => (SecretDoorState)state.Value;

        Collider[] blockers;
        NavMeshObstacle obstacle;
        float angle;           // o giro atual da folha (desenho, em cada computador)
        float holdStart = -1f; // só neste computador: desde quando a Cida segura a tecla
        bool decided;          // o host já olhou se a Cida está na partida

        void Awake()
        {
            blockers = GetComponents<Collider>();
            obstacle = GetComponent<NavMeshObstacle>();
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += Changed;
            Apply(State, effects: false);
            angle = State == SecretDoorState.Open ? openAngle : 0f;
        }

        public override void OnNetworkDespawn() => state.OnValueChanged -= Changed;

        void Changed(byte previous, byte current) => Apply((SecretDoorState)current, effects: true);

        // Roda em TODOS os computadores: rachadura, colisão, obstáculo do fantasma e som.
        void Apply(SecretDoorState s, bool effects)
        {
            if (crack != null) crack.SetActive(s != SecretDoorState.Closed);
            bool open = s == SecretDoorState.Open;
            foreach (var c in blockers) c.enabled = !open;
            if (obstacle != null) obstacle.enabled = !open;
            if (!effects) return;
            if (s == SecretDoorState.Revealed) GameAudio.Play(revealSound, transform.position + Vector3.up);
            if (s == SecretDoorState.Open) GameAudio.Play(openSound, transform.position + Vector3.up);
        }

        void Update()
        {
            // O giro da folha (cada computador desenha o seu).
            float target = State == SecretDoorState.Open ? openAngle : 0f;
            angle = Mathf.MoveTowards(angle, target, openSpeed * Time.deltaTime);
            if (leaf != null) leaf.localRotation = Quaternion.Euler(0, angle, 0);

            // A Cida segurando a tecla.
            if (holdStart >= 0f && Time.time - holdStart >= tuneSeconds)
            {
                holdStart = -1f;
                ActRpc(SecretDoorAction.Tune);
            }

            // O host decide uma vez, quando a partida começa, se o espelho já nasce rachado.
            if (IsServer && !decided)
            {
                var game = GameManager.Instance;
                if (game == null || !game.IsSpawned || game.Outcome != MatchOutcome.Playing || NetworkPlayer.All.Count == 0) return;
                decided = true;
                state.Value = (byte)SecretDoorRules.Initial(CidaInMatch());
            }
        }

        static bool CidaInMatch()
        {
            foreach (var p in NetworkPlayer.All)
                if (p.Character != null && p.Character.Current != null && p.Character.Current.tuningFork) return true;
            return false;
        }

        // ---------- A dica e a tecla ----------

        static bool HasFork(PlayerInteractor player) => player.Character != null && player.Character.tuningFork;

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (!IsSpawned) return InteractionPrompt.None;
            switch (SecretDoorRules.ActionFor(State, HasFork(player)))
            {
                case SecretDoorAction.Tune:
                    if (holdStart >= 0f)
                        return new InteractionPrompt($"Afinando... {Mathf.FloorToInt((Time.time - holdStart) / tuneSeconds * 100f)}%", true);
                    return new InteractionPrompt("Afinar Espelho (segure)", true);
                case SecretDoorAction.Push:
                    return new InteractionPrompt("Empurrar Espelho", true);
                default:
                    return InteractionPrompt.None;
            }
        }

        public void Interact(PlayerInteractor player)
        {
            var action = SecretDoorRules.ActionFor(State, HasFork(player));
            if (action == SecretDoorAction.Tune) holdStart = Time.time;
            else if (action == SecretDoorAction.Push) ActRpc(SecretDoorAction.Push);
        }

        public void HoldStop(PlayerInteractor player) => holdStart = -1f;

        [Rpc(SendTo.Server)]
        void ActRpc(SecretDoorAction action, RpcParams rpcParams = default)
        {
            // O host confere de novo quem pediu: só a Cida afina.
            if (!NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
            var character = client.PlayerObject != null ? client.PlayerObject.GetComponent<CharacterSwitcher>() : null;
            bool fork = character != null && character.Current != null && character.Current.tuningFork;
            if (SecretDoorRules.ActionFor(State, fork) != action) return;

            var next = SecretDoorRules.After(State, action);
            if (next == State) return;
            state.Value = (byte)next;
            if (next == SecretDoorState.Revealed)
            {
                GameCues.Emit(new GameCue(transform.position, 12f, Senses.Hearing, "O espelho do camarim soou oco. Tem alguma coisa atrás dele.", gameObject));
                GameCues.Emit(new GameCue(transform.position, 12f, Senses.Sight, "O espelho do camarim rachou de cima a baixo. Dá para empurrar.", gameObject));
            }
            else if (next == SecretDoorState.Open)
                GameCues.Emit(new GameCue(transform.position, 15f, Senses.Sight | Senses.Hearing | Senses.Vibration, "O espelho girou: atrás dele, uma sala escondida.", gameObject));
        }

        // Só no host (ao reiniciar a fase): fecha e decide de novo.
        public void ServerReset()
        {
            decided = false;
            state.Value = (byte)SecretDoorState.Closed;
        }
    }
}
