using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // A LANTERNA (tecla L). Fica no Player, mas só funciona depois de PEGAR uma das lanternas do
    // caminhão (FlashlightPickup). Só há 2 para a equipe toda: quem fica sem precisa andar junto
    // de quem tem (LEVEL_DESIGN_FASE1.md, vantagem V1). A Cida não precisa: enxerga pelo eco.
    //
    // A BATERIA acaba: ligada, gasta (100 s de luz com a carga cheia); abaixo de 20% a luz começa
    // a falhar, cada vez mais; em 0 apaga sozinha. Pilhas espalhadas pela casa recarregam um
    // pedaço. Gerir a luz (ligar só quando precisa) é parte do jogo.
    //
    // REDE: o host decide QUEM tem lanterna (como num móvel: dois pegam ao mesmo tempo, um leva).
    // Ligar e a carga são do DONO (é ele que aperta a tecla); todos veem a luz acesa e falhando.
    public class Flashlight : NetworkBehaviour
    {
        [Tooltip("O spot de luz, preso na câmera (a lanterna aponta para onde o jogador olha).")]
        [SerializeField] Light beam;
        [Tooltip("Segundos de luz com a bateria cheia.")]
        [SerializeField] float batterySeconds = 100f;
        [Tooltip("Abaixo desta carga a luz começa a falhar.")]
        [SerializeField, Range(0f, 1f)] float flickerBelow = 0.2f;

        readonly NetworkVariable<bool> has = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<bool> on = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        // A carga em porcentagem inteira: muda pouco, então quase não gasta rede.
        readonly NetworkVariable<byte> percent = new(
            100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        float charge = 1f;   // só no dono: a carga exata (0 a 1)
        float baseIntensity;

        public bool Has => has.Value;
        public bool IsOn => on.Value;
        public float Charge01 => IsOwner ? charge : percent.Value / 100f;

        void Awake()
        {
            baseIntensity = beam.intensity;
            beam.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            // Sombra só na lanterna de quem joga neste computador: é bonita e pesa (3 sombras de
            // spot por quadro, uma para cada jogador, seria caro demais).
            if (!IsOwner) beam.shadows = LightShadows.None;
        }

        void Update()
        {
            if (IsSpawned && IsOwner) OwnerUpdate();
            UpdateBeam();
        }

        void OwnerUpdate()
        {
            if (!has.Value)
            {
                if (on.Value) on.Value = false;
                return;
            }

            if (!PauseMenu.IsOpen && GameInput.Flashlight.WasPressedThisFrame() && (on.Value || charge > 0f))
            {
                on.Value = !on.Value;
                GameAudio.Play("lanterna_clique", transform.position);
            }

            if (on.Value)
            {
                charge = Mathf.Max(0f, charge - Time.deltaTime / batterySeconds);
                if (charge <= 0f)
                {
                    on.Value = false; // acabou: apaga sozinha
                    GameAudio.Play("lanterna_clique", transform.position);
                }
            }

            byte p = (byte)Mathf.CeilToInt(charge * 100f);
            if (p != percent.Value) percent.Value = p;
        }

        // Roda em TODOS: acende, apaga e faz a luz falhar com a bateria fraca.
        void UpdateBeam()
        {
            bool lit = IsSpawned && has.Value && on.Value;
            beam.enabled = lit;
            if (!lit) return;

            float k = 1f;
            float c = Charge01;
            if (c < flickerBelow)
            {
                // Quanto mais vazia, mais escura e mais vezes ela "morre" por um instante.
                float weak = 1f - c / flickerBelow;
                float noise = Mathf.PerlinNoise(Time.time * 9f, NetworkObjectId * 0.37f);
                k = noise < weak * 0.55f ? 0.05f : Mathf.Lerp(1f, 0.5f, weak);
            }
            beam.intensity = baseIntensity * k;
        }

        // ---------- Só no host (chamados pelo FlashlightPickup e pelo reinício da fase) ----------

        // Pegou uma lanterna do caminhão: vem com a bateria cheia.
        public void ServerGive()
        {
            has.Value = true;
            ChargeRpc(1f, true);
        }

        // Trocou as pilhas: soma 'amount' na carga (sem passar de cheia).
        public void ServerCharge(float amount) => ChargeRpc(amount, false);

        public void ServerReset() => has.Value = false;

        // A carga é do dono: o host pede e o computador dele muda.
        [Rpc(SendTo.Owner)]
        void ChargeRpc(float amount, bool full)
        {
            charge = full ? 1f : Mathf.Min(1f, charge + amount);
        }
    }
}
