using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Como a partida está (ou como acabou).
    // ": byte" = guarda o enum em 1 byte só (menos dados pela rede do que um int).
    public enum MatchOutcome : byte { Playing, Won, Dawn, Caught }

    // Controla o estado geral da partida: cronômetro até o amanhecer, vitória, derrota e reinício.
    //
    // EM REDE: só o host conta o tempo e decide quando a partida acaba. Os clientes recebem
    // os valores por NetworkVariables. Assim todo mundo vê o MESMO cronômetro e o MESMO final.
    //
    // Este script NÃO desenha nada: ele só guarda o estado (TimeLeft, Delivered, Outcome...).
    // Quem mostra isso na tela é a HUD. Separar "lógica" de "tela" deixa trocar o visual
    // sem arriscar quebrar as regras do jogo (e vice-versa).
    public class GameManager : NetworkBehaviour
    {
        [SerializeField] float nightDuration = 180f; // segundos até amanhecer

        public static GameManager Instance { get; private set; }

        readonly NetworkVariable<float> timeLeft = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> delivered = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<MatchOutcome> outcome = new(MatchOutcome.Playing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> stars = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // --- Estado para quem quiser LER (a HUD) ---
        public float TimeLeft => timeLeft.Value;
        public int Delivered => delivered.Value;
        public int TotalFurniture => Furniture.All.Count;
        public MatchOutcome Outcome => outcome.Value;
        public int Stars => stars.Value; // 1 a 5, só quando Outcome == Won

        void Awake() => Instance = this;

        // NetworkBehaviour já tem um OnDestroy próprio: "override" + "base" para não apagar o dele.
        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            outcome.OnValueChanged += OnOutcomeChanged;
            if (IsServer) timeLeft.Value = nightDuration;
            ApplyOutcome(outcome.Value); // quem entra no meio de um game over também congela
        }

        public override void OnNetworkDespawn() => outcome.OnValueChanged -= OnOutcomeChanged;

        void OnOutcomeChanged(MatchOutcome previous, MatchOutcome current) => ApplyOutcome(current);

        // Roda em TODOS os computadores quando o resultado muda.
        // Congelar o tempo não trava a rede: o Netcode usa o tempo "real", sem escala.
        static void ApplyOutcome(MatchOutcome o) => Time.timeScale = o == MatchOutcome.Playing ? 1f : 0f;

        // Chamado pelo fantasma, que só pensa no host.
        public static void ServerPlayerCaught()
        {
            if (Instance != null && Instance.IsServer) Instance.End(MatchOutcome.Caught);
        }

        void Update()
        {
            if (!IsSpawned) return; // antes de conectar não existe partida

            if (outcome.Value != MatchOutcome.Playing)
            {
                // Qualquer jogador pode pedir para jogar de novo; quem executa é o host.
                if (GameInput.Restart.WasPressedThisFrame()) RestartRpc();
                return;
            }

            if (!IsServer) return; // daqui pra baixo, só o host decide

            timeLeft.Value = Mathf.Max(0f, timeLeft.Value - Time.deltaTime);
            delivered.Value = TruckZone.TotalDelivered;

            if (timeLeft.Value <= 0) End(MatchOutcome.Dawn);
            else if (delivered.Value >= TotalFurniture) End(MatchOutcome.Won);
        }

        void End(MatchOutcome o)
        {
            if (outcome.Value != MatchOutcome.Playing) return; // já acabou, ignora
            if (o == MatchOutcome.Won)
                stars.Value = Mathf.Clamp(Mathf.CeilToInt(timeLeft.Value / nightDuration * 5f), 1, 5);
            outcome.Value = o;
        }

        // Reiniciar SEM recarregar a cena: recarregar no meio de uma partida em rede
        // é complicado (todos precisam recarregar juntos e os objetos de rede renascer).
        // É mais simples colocar cada coisa de volta no lugar.
        [Rpc(SendTo.Server)]
        void RestartRpc()
        {
            if (outcome.Value == MatchOutcome.Playing) return; // dois jogadores apertaram R: ignora o segundo

            foreach (var f in Furniture.All) f.ServerReset();
            foreach (var g in GhostHunter.All) g.ServerReset();
            foreach (var p in NetworkPlayer.All) p.ResetPositionRpc();

            timeLeft.Value = nightDuration;
            delivered.Value = 0;
            outcome.Value = MatchOutcome.Playing; // descongela todo mundo
        }
    }
}
