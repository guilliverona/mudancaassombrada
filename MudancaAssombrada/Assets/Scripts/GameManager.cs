using System.Collections.Generic;
using System.Text;
using Unity.Collections; // FixedString512Bytes
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Como a partida está (ou como acabou).
    // ": byte" = guarda o enum em 1 byte só (menos dados pela rede do que um int).
    public enum MatchOutcome : byte { Playing, Won, Dawn, Caught }

    // Controla a partida de um capítulo: relógio até o amanhecer, etapas dos objetivos,
    // vitória, derrota e reinício.
    //
    //  - O CAPÍTULO (ChapterDefinition) vem da cena aberta: Campaign.FindByScene.
    //  - Os OBJETIVOS são componentes Objective espalhados pela cena, agrupados em etapas.
    //
    // EM REDE: só o host conta o tempo e avalia os objetivos. Os clientes recebem o resultado
    // por NetworkVariables. Este script NÃO desenha nada: quem mostra é a HUD.
    public class GameManager : NetworkBehaviour
    {
        [Tooltip("Usado só se a cena não estiver em nenhum capítulo da Campanha (testes).")]
        [SerializeField] float fallbackNightDuration = 180f;

        public static GameManager Instance { get; private set; }

        readonly NetworkVariable<float> timeLeft = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<MatchOutcome> outcome = new(MatchOutcome.Playing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> stars = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Em qual etapa a equipe está (0, 1, 2...): a pianola toca um tema a cada etapa nova.
        readonly NetworkVariable<int> stageShown = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // O pagamento da noite (capítulo + achados − frágeis batidos), só quando Outcome == Won.
        readonly NetworkVariable<int> pay = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // O texto dos objetivos já pronto, montado pelo host (só ele avalia os objetivos).
        // FixedString: texto de tamanho fixo, que dá para mandar pela rede.
        readonly NetworkVariable<FixedString512Bytes> objectiveText = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // As etapas que existem nesta fase (ex.: 0, 1, 2), em ordem, e em qual estamos. Só no host.
        readonly List<int> stages = new();
        int stageIndex;
        readonly StringBuilder sb = new();

        // --- Estado para quem quiser LER (a HUD) ---
        public ChapterDefinition Chapter { get; private set; } // null = cena fora da Campanha
        public float NightDuration => Chapter != null ? Chapter.nightDuration : fallbackNightDuration;
        public bool HasClock => NightDuration > 0;
        public float TimeLeft => timeLeft.Value;
        // 0 = começo da noite, 1 = amanheceu. Sem relógio: sempre 0 (noite o tempo todo).
        public float NightProgress => MatchRules.NightProgress(timeLeft.Value, NightDuration);
        public MatchOutcome Outcome => outcome.Value;
        public int Stars => stars.Value; // 1 a 5, só quando Outcome == Won
        public int StageIndex => stageShown.Value;
        public int Pay => pay.Value;     // só quando Outcome == Won
        public string ObjectiveText => objectiveText.Value.ToString();

        void Awake()
        {
            Instance = this;
            Chapter = Campaign.Instance != null ? Campaign.Instance.FindByScene(gameObject.scene.name) : null;
        }

        // NetworkBehaviour já tem um OnDestroy próprio: "override" + "base" para não apagar o dele.
        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            outcome.OnValueChanged += OnOutcomeChanged;
            if (IsServer) ServerStartMatch();
            ApplyOutcome(outcome.Value); // quem entra no meio de um game over também congela
        }

        public override void OnNetworkDespawn() => outcome.OnValueChanged -= OnOutcomeChanged;

        void OnOutcomeChanged(MatchOutcome previous, MatchOutcome current) => ApplyOutcome(current);

        // Roda em TODOS os computadores quando o resultado muda.
        // Congelar o tempo não trava a rede: o Netcode usa o tempo "real", sem escala.
        static void ApplyOutcome(MatchOutcome o) => Time.timeScale = o == MatchOutcome.Playing ? 1f : 0f;

        // A equipe inteira está caída? (Ninguém sobrou para ajudar: os fantasmas venceram.)
        static bool EveryoneDown()
        {
            int down = 0;
            foreach (var p in NetworkPlayer.All)
                if (p.Condition != null && p.Condition.IsDown) down++;
            return RescueRules.EveryoneDown(down, NetworkPlayer.All.Count);
        }

        void ServerStartMatch()
        {
            // Descobre as etapas que existem na cena (sem repetir, em ordem).
            stages.Clear();
            foreach (var o in Objective.All) stages.Add(o.Stage);
            var ordered = MatchRules.Stages(stages);
            stages.Clear();
            stages.AddRange(ordered);
            if (stages.Count == 0)
                Debug.LogWarning("Esta fase não tem nenhum objetivo (componente Objective): ela nunca vai terminar em vitória.");

            timeLeft.Value = NightDuration;
            stageIndex = 0;
            stageShown.Value = 0;
            stars.Value = 0;
            pay.Value = 0;
            outcome.Value = MatchOutcome.Playing;
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

            if (HasClock)
            {
                timeLeft.Value = Mathf.Max(0f, timeLeft.Value - Time.deltaTime);
                if (timeLeft.Value <= 0) { End(MatchOutcome.Dawn); return; }
            }

            // Pego = caído até um colega ajudar (PlayerCondition). Só perde se não sobrar ninguém de pé.
            if (EveryoneDown()) { End(MatchOutcome.Caught); return; }

            UpdateObjectives();
        }

        void UpdateObjectives()
        {
            if (stages.Count == 0) return;

            int stage = stages[stageIndex];
            bool allDone = true;
            sb.Clear();
            if (stages.Count > 1) sb.Append("Etapa ").Append(stageIndex + 1).Append(" de ").Append(stages.Count).Append('\n');
            foreach (var o in Objective.All)
            {
                if (o.Stage != stage) continue;
                bool done = o.IsComplete;
                allDone &= done;
                sb.Append(done ? "✓ " : "• ").Append(o.Progress).Append('\n');
            }
            SetObjectiveText(sb.ToString().TrimEnd());

            if (!allDone) return;
            stageIndex = MatchRules.AdvanceStage(stageIndex, stages.Count, out bool won); // próxima etapa
            stageShown.Value = stageIndex;
            if (won) End(MatchOutcome.Won);
        }

        void SetObjectiveText(string text)
        {
            // O FixedString tem limite de BYTES (512), e letras acentuadas ocupam mais de um.
            // 150 letras cabem com folga; textos maiores são cortados em vez de dar erro.
            text = MatchRules.FitObjectiveText(text);
            var value = new FixedString512Bytes(text);
            if (!objectiveText.Value.Equals(value)) objectiveText.Value = value; // só manda se mudou
        }

        void End(MatchOutcome o)
        {
            if (outcome.Value != MatchOutcome.Playing) return; // já acabou, ignora
            if (o == MatchOutcome.Won)
            {
                // Mais tempo sobrando = mais estrelas. Sem relógio: 5 estrelas.
                stars.Value = MatchRules.Stars(timeLeft.Value, NightDuration);
                int payment = Chapter != null ? Chapter.payment : 0;
                pay.Value = ServerNightPay(payment);
                ChapterCompletedRpc(pay.Value - payment);
            }
            outcome.Value = o;
        }

        // O que a noite paga: o capítulo, mais os achados de época entregues (D10), menos as pancadas
        // nos frágeis (D13). As contas ficam na PeriodFindRules.
        static int ServerNightPay(int payment)
        {
            var values = new List<int>();
            foreach (var f in PeriodFind.All)
            {
                f.ServerMarkDelivered();
                if (f.Delivered) values.Add(f.Value);
            }
            int hits = 0;
            foreach (var f in Fragile.All) hits += f.Hits;
            return PeriodFindRules.Pay(payment, values, hits, Fragile.DiscountPerHit);
        }

        // SendTo.Everyone: cada jogador grava a conclusão no PRÓPRIO save.
        // "extra" = o que a noite pagou além do capítulo (achados − frágeis).
        [Rpc(SendTo.Everyone)]
        void ChapterCompletedRpc(int extra) => SaveSystem.CompleteChapter(Chapter, extra);

        // Reiniciar SEM recarregar a cena: recarregar no meio de uma partida em rede
        // é complicado (todos precisam recarregar juntos e os objetos de rede renascer).
        // É mais simples colocar cada coisa de volta no lugar.
        [Rpc(SendTo.Server)]
        void RestartRpc()
        {
            if (outcome.Value == MatchOutcome.Playing) return; // dois jogadores apertaram R: ignora o segundo

            foreach (var f in Furniture.All) f.ServerReset();
            foreach (var g in Ghost.All) g.ServerReset();
            foreach (var i in Interactable.All) i.ServerReset();
            foreach (var o in Objective.All) o.ServerReset();
            foreach (var l in FlashlightPickup.All) l.ServerReset(); // lanternas e pilhas de volta ao lugar
            foreach (var d in SecretMirrorDoor.All) d.ServerReset(); // o espelho-porta fecha de novo
            foreach (var t in PortalTrap.All) t.ServerReset();
            foreach (var s in HouseScare.All) s.ServerReset(); // o que caiu volta para o lugar
            foreach (var w in DoorWeb.All) w.ServerReset();    // as teias de vão inteiras de novo
            foreach (var f in PeriodFind.All) f.ServerReset();
            foreach (var f in Fragile.All) f.ServerReset();
            foreach (var p in NetworkPlayer.All)
            {
                if (p.Condition != null) p.Condition.ServerReset(); // todo mundo de pé de novo
                if (p.TryGetComponent(out Flashlight flashlight)) flashlight.ServerReset(); // e de mãos vazias
                p.ResetPositionRpc();
            }

            ServerStartMatch(); // descongela todo mundo
        }
    }
}
