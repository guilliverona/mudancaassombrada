using UnityEngine;

namespace MudancaAssombrada
{
    // A bengala (e os passos). Fica no Player.
    // Bengala (Espaço) = onda grande. Andar = passos (ondas pequenas, sempre).
    public class CaneTapper : MonoBehaviour
    {
        [Header("Bengala")]
        [SerializeField] float tapRange = 12f;
        [SerializeField] float tapCooldown = 0.6f; // evita "metralhar" o botão

        [Header("Passos")]
        [SerializeField] float stepDistance = 1.2f; // um passo a cada 1.2m andados
        [SerializeField] float stepRange = 2.5f;
        [SerializeField] float crouchStepRange = 1f;  // agachado: o fantasma só ouve colado em você
        [SerializeField] float runStepRange = 7f;     // correndo: ouve de longe

        // Só quem tem bengala na ficha bate. Os PASSOS continuam para todos: o fantasma ouve qualquer um.
        public bool CanTap { get; set; } = true;
        // Multiplica o alcance dos passos (vem da ficha do personagem).
        public float StepVolume { get; set; } = 1f;

        PlayerMovement movement;
        float nextTapTime;
        float walked;
        Vector3 lastPosition;

        void Start()
        {
            lastPosition = transform.position;
            movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            // Time.time = segundos desde o início do jogo. Técnica clássica de cooldown.
            if (CanTap && !PauseMenu.IsOpen && GameInput.Tap.WasPressedThisFrame() && Time.time >= nextTapTime)
            {
                nextTapTime = Time.time + tapCooldown;
                // A ponta da bengala bate no chão, um pouco à frente dos pés.
                Vector3 tip = transform.position + transform.forward * 0.6f + Vector3.down * 0.9f;
                EchoSystem.Emit(new SoundEvent(tip, tapRange, 1f, SoundType.Cane, gameObject));
            }

            // Soma quanto andou no chão (ignora o eixo Y).
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0;
            walked += delta.magnitude;
            lastPosition = transform.position;

            if (walked >= stepDistance)
            {
                walked = 0;
                // O mesmo passo, três volumes. Como o fantasma ouve "alcance x 1.5",
                // isto muda direto de quão longe ele te escuta: 1.5m, 3.75m ou 10.5m.
                // E como o som É a visão da Cida: agachada ela também enxerga menos!
                float range = stepRange, intensity = 0.5f;
                if (movement.IsCrouching) { range = crouchStepRange; intensity = 0.25f; }
                else if (movement.IsRunning) { range = runStepRange; intensity = 0.9f; }
                Vector3 feet = transform.position + Vector3.down * 0.9f;
                EchoSystem.Emit(new SoundEvent(feet, range * StepVolume, intensity, SoundType.Footstep, gameObject));
            }
        }
    }
}
