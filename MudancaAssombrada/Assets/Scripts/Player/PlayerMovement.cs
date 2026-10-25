using UnityEngine;

namespace MudancaAssombrada
{
    // [RequireComponent] garante que o objeto tenha um CharacterController.
    // Se você arrastar este script para um objeto sem ele, a Unity adiciona sozinha.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        // [SerializeField] deixa o campo privado, mas visível/editável no Inspector.
        [Header("Movimento")]
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float gravity = -20f;

        [Header("Câmera")]
        [SerializeField] Transform cameraPivot;      // a câmera, filha do Player
        [SerializeField] float mouseSensitivity = 0.1f;

        [Header("Agachar")]
        [SerializeField] float crouchHeight = 1.2f;
        [SerializeField] float crouchSpeedMultiplier = 0.5f;
        [SerializeField] float crouchTransitionSpeed = 8f; // quão rápido a câmera desce/sobe

        [Header("Correr")]
        [SerializeField] float runSpeedMultiplier = 1.8f;
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float staminaDrain = 25f;   // por segundo correndo (4s de corrida)
        [SerializeField] float staminaRegen = 15f;   // por segundo sem correr
        [SerializeField] float exhaustedUntil = 30f; // esgotou? só corre de novo com 30 de estamina

        CharacterController controller;
        float stamina;
        float verticalVelocity;  // velocidade de queda (eixo Y)
        float cameraPitch;       // quanto a câmera olha para cima/baixo
        float standHeight;       // altura original do CharacterController
        float standCameraY;      // altura original da câmera

        // Multiplicador que outros scripts podem mudar (ex.: carregando peso = mais lento).
        public float SpeedMultiplier { get; set; } = 1f;

        // Velocidade do PERSONAGEM (vem da ficha dele, ex.: Marcelo é mais lento).
        // Fica separado do SpeedMultiplier porque o carregador sobrescreve aquele ao pegar/soltar.
        public float CharacterSpeed { get; set; } = 1f;

        // "private set": outros scripts podem LER (o CaneTapper e a HUD leem), mas só este script muda.
        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsExhausted { get; private set; }
        public float Stamina01 => stamina / maxStamina; // 0 = vazia, 1 = cheia

        // Awake roda uma vez, antes de Start. Bom lugar para pegar referências.
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            standHeight = controller.height;
            standCameraY = cameraPivot.localPosition.y;
            stamina = maxStamina;
            Cursor.lockState = CursorLockMode.Locked; // prende e esconde o mouse
        }

        // Update roda uma vez por frame.
        void Update()
        {
            Look();
            Crouch();
            Move();
        }

        void Crouch()
        {
            bool wantsCrouch = GameInput.Crouch.IsPressed();

            // Soltou o botão, mas tem algo em cima da cabeça (debaixo de uma mesa)? Continua agachado.
            // O raio sai do centro do jogador para cima e testa se há espaço para ficar em pé.
            if (!wantsCrouch && IsCrouching)
            {
                float room = standHeight - controller.height / 2f + 0.05f;
                if (Physics.Raycast(transform.position, Vector3.up, room, ~0, QueryTriggerInteraction.Ignore))
                    wantsCrouch = true;
            }
            IsCrouching = wantsCrouch;

            // Encolhe o collider mantendo os pés no chão: se a altura diminui X, o centro desce X/2.
            float height = IsCrouching ? crouchHeight : standHeight;
            controller.height = height;
            controller.center = new Vector3(0, (height - standHeight) / 2f, 0);

            // A câmera desce suavemente. Lerp = "ande uma fração do caminho até o alvo" a cada frame.
            float targetY = standCameraY - (IsCrouching ? standHeight - crouchHeight : 0f);
            Vector3 cam = cameraPivot.localPosition;
            cam.y = Mathf.Lerp(cam.y, targetY, crouchTransitionSpeed * Time.deltaTime);
            cameraPivot.localPosition = cam;
        }

        void Look()
        {
            if (PauseMenu.IsOpen) return; // com o menu aberto, o mouse é para clicar, não para mirar

            // Quanto o mouse se moveu neste frame.
            // A sensibilidade escolhida no menu de Opções multiplica a do Inspector.
            Vector2 delta = GameInput.Look.ReadValue<Vector2>() * mouseSensitivity * GameSettings.MouseSensitivity;

            // Girar para os lados: gira o corpo inteiro no eixo Y.
            transform.Rotate(Vector3.up * delta.x);

            // Olhar para cima/baixo: só a câmera, limitado a ±80° para não "dar cambalhota".
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -80f, 80f);
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        void Move()
        {
            Vector2 input = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f); // diagonal não fica mais rápida
            // Pausado: fica parado, mas a gravidade continua (senão flutuaria se pausasse no ar).
            if (PauseMenu.IsOpen) input = Vector2.zero;

            // Converte a direção do controle para a direção para onde o jogador está virado.
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // Gravidade: no chão, mantém uma pequena força para baixo; no ar, acelera a queda.
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            UpdateStamina(input.sqrMagnitude > 0);

            float modifier = IsCrouching ? crouchSpeedMultiplier : IsRunning ? runSpeedMultiplier : 1f;
            Vector3 velocity = move * walkSpeed * CharacterSpeed * SpeedMultiplier * modifier + Vector3.up * verticalVelocity;

            // Time.deltaTime = segundos desde o último frame. Multiplicar por ele deixa
            // o movimento igual em PCs rápidos ou lentos.
            controller.Move(velocity * Time.deltaTime);
        }

        void UpdateStamina(bool isMoving)
        {
            // Só corre se: segurando Correr, andando, em pé e não esgotado.
            bool wantsRun = GameInput.Sprint.IsPressed() && isMoving && !IsCrouching && !PauseMenu.IsOpen;
            IsRunning = wantsRun && !IsExhausted;

            if (IsRunning) stamina -= staminaDrain * Time.deltaTime;
            else stamina += staminaRegen * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);

            // "Histerese": esgota em 0, mas só libera em 30. Sem isso, o jogador ficaria
            // piscando entre correr/não correr com a estamina em 0.1, 0, 0.1, 0...
            if (stamina <= 0) IsExhausted = true;
            else if (IsExhausted && stamina >= exhaustedUntil) IsExhausted = false;
        }
    }
}
