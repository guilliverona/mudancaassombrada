using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // A LANTERNA (tecla L). Fica no Player, mas só funciona depois de PEGAR uma das lanternas do
    // caminhão (FlashlightPickup). Só há 2 para a equipe toda: quem fica sem precisa andar junto
    // de quem tem (LEVEL_DESIGN_FASE1.md, vantagem V1). A Cida não precisa: enxerga pelo eco.
    //
    // Ela mora na HOTBAR (um espaço, teclas 1-2-3) e fica em um de três lugares (HotbarRules.PlaceOf):
    //   - na MÃO (espaço escolhido): aponta para onde você olha;
    //   - no COLETE (estava na mão e você pegou um móvel): aponta para a frente do corpo, mais
    //     fraca. Quem carrega fica meio cego; quem está de mão livre ilumina o caminho para ele;
    //   - GUARDADA (outro espaço escolhido): apaga.
    // A tecla L liga/desliga; com a lanterna guardada, tira do cinto e já acende.
    //
    // A BATERIA acaba: ligada, gasta (100 s de luz com a carga cheia); abaixo de 20% a luz começa
    // a falhar, cada vez mais; em 0 apaga sozinha. Pilhas espalhadas pela casa recarregam um
    // pedaço. Gerir a luz (ligar só quando precisa) é parte do jogo.
    //
    // REDE: o host decide QUEM tem lanterna (está na hotbar dele). Ligar, o lugar e a carga são do
    // DONO (é ele que aperta a tecla e carrega o móvel); todos veem a luz acesa, falhando e no colete.
    public class Flashlight : NetworkBehaviour
    {
        [Tooltip("O spot de luz, preso na câmera (a lanterna aponta para onde o jogador olha).")]
        [SerializeField] Light beam;
        [Tooltip("Segundos de luz com a bateria cheia.")]
        [SerializeField] float batterySeconds = 100f;
        [Tooltip("Abaixo desta carga a luz começa a falhar.")]
        [SerializeField, Range(0f, 1f)] float flickerBelow = 0.2f;

        [Header("No colete (carregando um móvel)")]
        [Tooltip("Fração da força da luz presa no colete.")]
        [SerializeField, Range(0f, 1f)] float chestIntensity = 0.6f;
        [Tooltip("Metros abaixo dos olhos onde fica o colete.")]
        [SerializeField] float chestBelowEyes = 0.35f;
        [Tooltip("Graus para baixo: no colete ela aponta um pouco para o chão, à frente.")]
        [SerializeField] float chestTilt = 12f;

        readonly NetworkVariable<bool> on = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<FlashlightPlace> place = new(
            FlashlightPlace.Stowed, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        // A carga em porcentagem inteira: muda pouco, então quase não gasta rede.
        readonly NetworkVariable<byte> percent = new(
            100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        float charge = 1f;   // só no dono: a carga exata (0 a 1)
        float baseIntensity;
        Vector3 handPosition; // onde o spot fica na câmera (na mão)
        Hotbar hotbar;

        public bool Has => hotbar != null && hotbar.Contains(HotbarItem.Flashlight);
        public bool IsOn => on.Value;
        public FlashlightPlace Place => place.Value;
        public float Charge01 => IsOwner ? charge : percent.Value / 100f;

        void Awake()
        {
            hotbar = GetComponent<Hotbar>();
            baseIntensity = beam.intensity;
            handPosition = beam.transform.localPosition;
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
            if (hotbar == null) return; // Player antigo, sem a hotbar: recrie o prefab
            bool has = Has;
            bool busy = hotbar.HandsBusy;
            var now = HotbarRules.PlaceOf(hotbar.InHand == HotbarItem.Flashlight, busy);
            if (now != place.Value) place.Value = now;

            // Sem lanterna, ou guardada no cinto: apagada.
            if (!has || now == FlashlightPlace.Stowed)
            {
                if (on.Value) SetOn(false);
            }

            if (!PauseMenu.IsOpen && GameInput.Flashlight.WasPressedThisFrame())
            {
                switch (HotbarRules.OnFlashlightKey(has, now, busy))
                {
                    case FlashlightKey.Toggle:
                        if (on.Value || charge > 0f) SetOn(!on.Value);
                        break;
                    case FlashlightKey.TakeOutAndTurnOn:
                        hotbar.Select(hotbar.Find(HotbarItem.Flashlight));
                        place.Value = FlashlightPlace.Hand;
                        if (charge > 0f) SetOn(true);
                        break;
                }
            }

            if (on.Value)
            {
                charge = Mathf.Max(0f, charge - Time.deltaTime / batterySeconds);
                if (charge <= 0f) SetOn(false); // acabou: apaga sozinha
            }

            byte p = (byte)Mathf.CeilToInt(charge * 100f);
            if (p != percent.Value) percent.Value = p;
        }

        void SetOn(bool value)
        {
            on.Value = value;
            GameAudio.Play("lanterna_clique", transform.position);
        }

        // Roda em TODOS: acende, apaga e faz a luz falhar com a bateria fraca.
        void UpdateBeam()
        {
            bool lit = IsSpawned && Has && on.Value && place.Value != FlashlightPlace.Stowed;
            beam.enabled = lit;
            if (!lit) return;

            float k = place.Value == FlashlightPlace.Chest ? chestIntensity : 1f;
            float c = Charge01;
            if (c < flickerBelow)
            {
                // Quanto mais vazia, mais escura e mais vezes ela "morre" por um instante.
                float weak = 1f - c / flickerBelow;
                float noise = Mathf.PerlinNoise(Time.time * 9f, NetworkObjectId * 0.37f);
                k *= noise < weak * 0.55f ? 0.05f : Mathf.Lerp(1f, 0.5f, weak);
            }
            beam.intensity = baseIntensity * k;
        }

        // Depois de a câmera girar (PlayerMovement, no Update): no colete, o spot ignora o olhar
        // para cima/baixo e segue o corpo; na mão, volta para a câmera.
        void LateUpdate()
        {
            if (!beam.enabled) return;
            var t = beam.transform;
            if (place.Value == FlashlightPlace.Chest)
            {
                Vector3 forward = Quaternion.AngleAxis(chestTilt, transform.right) * transform.forward;
                t.SetPositionAndRotation(t.parent.position - transform.up * chestBelowEyes + transform.forward * 0.2f,
                                         Quaternion.LookRotation(forward, transform.up));
            }
            else
            {
                t.localPosition = handPosition;
                t.localRotation = Quaternion.identity;
            }
        }

        // ---------- Só no host (chamados pelo FlashlightPickup) ----------

        // Pegou uma lanterna do caminhão: vai para a hotbar, com a bateria cheia. false = hotbar cheia.
        public bool ServerGive()
        {
            if (hotbar == null || !hotbar.ServerAdd(HotbarItem.Flashlight)) return false;
            ChargeRpc(1f, true);
            return true;
        }

        // Trocou as pilhas: soma 'amount' na carga (sem passar de cheia).
        public void ServerCharge(float amount) => ChargeRpc(amount, false);

        // A carga é do dono: o host pede e o computador dele muda.
        [Rpc(SendTo.Owner)]
        void ChargeRpc(float amount, bool full)
        {
            charge = full ? 1f : Mathf.Min(1f, charge + amount);
        }
    }
}
