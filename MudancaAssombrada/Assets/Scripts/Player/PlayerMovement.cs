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
        [Tooltip("Os olhos ficam à frente do meio do corpo (metros), não dentro do peito.")]
        [SerializeField] float eyeForward = 0.12f;
        [Tooltip("Quanto a cabeça avança ao olhar todo para baixo (o pescoço inclina): assim você vê o corpo de fora.")]
        [SerializeField] float lookDownForward = 0.22f;
        [SerializeField] float nearClip = 0.05f; // perto o bastante para ver o peito sem cortar
        [Tooltip("Do osso da cabeça (na base do crânio) até a altura dos olhos, em metros.")]
        [SerializeField] float headToEyes = 0.1f;
        [Tooltip("Quão rápido a câmera acompanha a cabeça da animação (alto = colada, baixo = mais suave).")]
        [SerializeField] float followHeadSpeed = 15f;

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
        float standHeight;       // altura do CharacterController em pé (vem da ficha do personagem)
        float eyeHeight;         // altura dos olhos acima dos pés, em pé (vem da ficha do personagem)
        float feetY;             // onde ficam os pés, medido a partir do centro do Player (fixo)

        // Multiplicador que outros scripts podem mudar (ex.: carregando peso = mais lento).
        public float SpeedMultiplier { get; set; } = 1f;

        // Velocidade do PERSONAGEM (vem da ficha dele, ex.: Marcelo é mais lento).
        // Fica separado do SpeedMultiplier porque o carregador sobrescreve aquele ao pegar/soltar.
        public float CharacterSpeed { get; set; } = 1f;

        // Quem está ARRASTANDO este jogador (o Imitador puxando para o espelho). null = ninguém.
        // Quem liga e desliga é o PlayerCondition (a condição do jogador), não o fantasma direto.
        public Transform PulledBy { get; set; }
        // Travado (caído ou hipnotizado): não anda nem corre, mas ainda olha em volta.
        public bool Frozen { get; set; }
        [Header("Sendo puxado")]
        [SerializeField] float pullSpeed = 3f;      // mais rápido que andar agachado: não dá para fugir sozinho
        [SerializeField] float pullDistance = 1.2f; // fica "segurando a outra ponta", a esta distância

        // "private set": outros scripts podem LER (o CaneTapper e a HUD leem), mas só este script muda.
        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsExhausted { get; private set; }
        public float Stamina01 => stamina / maxStamina; // 0 = vazia, 1 = cheia

        // Cada personagem tem uma altura de olhos (a Cida é baixinha, o Marcelo é enorme).
        // A câmera vai deslizando até a nova altura, pelo mesmo Lerp do agachar.
        public void SetEyeHeight(float metersAboveFeet) => eyeHeight = metersAboveFeet;

        // Cada personagem tem um corpo do seu tamanho (Cida 1,52 m, Marcelo 1,98 m).
        // Os PÉS ficam sempre no mesmo lugar (feetY): o colisor cresce ou encolhe para cima,
        // e o modelo (que está com os pés em feetY) continua pisando no chão.
        public void SetBodySize(float height, float radius)
        {
            standHeight = height;
            controller.radius = radius;
            controller.stepOffset = Mathf.Min(controller.stepOffset, height * 0.25f);
            ApplyHeight(IsCrouching ? Mathf.Min(crouchHeight, height) : height);
        }

        // Muda a altura do colisor mantendo os pés no chão: o centro fica no meio, entre os pés e o topo.
        void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0, feetY + height / 2f, 0);
        }

        // Awake roda uma vez, antes de Start. Bom lugar para pegar referências.
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            standHeight = controller.height;
            feetY = controller.center.y - controller.height / 2f; // no prefab: centro 0, altura 2 -> pés em -1
            eyeHeight = cameraPivot.localPosition.y - feetY;
            stamina = maxStamina;
            var eye = cameraPivot.GetComponentInChildren<Camera>();
            if (eye != null) eye.nearClipPlane = Mathf.Min(eye.nearClipPlane, nearClip);
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
            // O raio sai do meio do corpo agachado para cima e testa se há espaço para ficar em pé.
            float crouched = Mathf.Min(crouchHeight, standHeight); // quem é baixinho não "cresce" agachando
            if (!wantsCrouch && IsCrouching)
            {
                Vector3 from = transform.position + Vector3.up * (feetY + crouched / 2f);
                float room = standHeight - crouched / 2f + 0.05f;
                if (Physics.Raycast(from, Vector3.up, room, ~0, QueryTriggerInteraction.Ignore))
                    wantsCrouch = true;
            }
            IsCrouching = wantsCrouch;

            // Encolhe o collider mantendo os pés no chão.
            ApplyHeight(IsCrouching ? crouched : standHeight);

        }

        // A câmera vai depois da animação (o Animator mexe nos ossos entre o Update e o LateUpdate).
        void LateUpdate()
        {
            Vector3 cam = cameraPivot.localPosition;
            // Para a frente: os olhos estão no rosto, e o rosto avança quando você olha para baixo
            // (pitch positivo = olhando para baixo). Sem isso, a câmera fica dentro do próprio peito.
            float down = Mathf.Clamp01(cameraPitch / 80f);
            float forward = eyeForward + lookDownForward * down;

            if (head != null)
            {
                // Com animação de verdade: os olhos ficam onde a CABEÇA está (ela abaixa ao agachar,
                // vai à frente ao correr). Medido no espaço do Player, para girar junto com ele.
                Vector3 h = transform.InverseTransformPoint(head.position);
                Vector3 target = new Vector3(0f, h.y + headToEyes, h.z + forward);
                cameraPivot.localPosition = Vector3.Lerp(cam, target, followHeadSpeed * Time.deltaTime);
                return;
            }

            // Sem modelo animado: agachado, os olhos descem na mesma proporção que o corpo,
            // então cada personagem agacha "do seu tamanho".
            float crouched = Mathf.Min(crouchHeight, standHeight);
            float eye = IsCrouching ? eyeHeight * (crouched / standHeight) : eyeHeight;
            // A câmera desce suavemente. Lerp = "ande uma fração do caminho até o alvo" a cada frame.
            cam.y = Mathf.Lerp(cam.y, feetY + eye, crouchTransitionSpeed * Time.deltaTime);
            cam.z = forward;
            cameraPivot.localPosition = cam;
        }

        // O osso da cabeça do modelo animado (o CharacterSwitcher passa ao trocar de personagem).
        // null = boneco sem animação: a câmera usa a altura dos olhos da ficha.
        Transform head;
        public void FollowHead(Transform headBone) => head = headBone;

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
            if (PauseMenu.IsOpen || Frozen) input = Vector2.zero;

            // Converte a direção do controle para a direção para onde o jogador está virado.
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // Gravidade: no chão, mantém uma pequena força para baixo; no ar, acelera a queda.
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            UpdateStamina(input.sqrMagnitude > 0);

            float modifier = IsCrouching ? crouchSpeedMultiplier : IsRunning ? runSpeedMultiplier : 1f;
            Vector3 velocity = move * walkSpeed * CharacterSpeed * SpeedMultiplier * modifier + Vector3.up * verticalVelocity;

            // Sendo puxado: além do próprio passo, é arrastado na direção de quem puxa.
            if (PulledBy != null)
            {
                Vector3 toPuller = PulledBy.position - transform.position;
                toPuller.y = 0;
                if (toPuller.magnitude > pullDistance) velocity += toPuller.normalized * pullSpeed;
            }

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
