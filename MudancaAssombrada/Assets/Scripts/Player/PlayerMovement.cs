using UnityEngine;
using UnityEngine.InputSystem; // Input System novo (o projeto já vem com ele)

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
    [SerializeField] Transform cameraPivot;      // a Main Camera, filha do Player
    [SerializeField] float mouseSensitivity = 0.1f;

    [Header("Agachar (segure Ctrl ou C)")]
    [SerializeField] float crouchHeight = 1.2f;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchTransitionSpeed = 8f; // quão rápido a câmera desce/sobe

    [Header("Correr (segure Shift)")]
    [SerializeField] float runSpeedMultiplier = 1.8f;
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaDrain = 25f;   // por segundo correndo (4s de corrida)
    [SerializeField] float staminaRegen = 15f;   // por segundo sem correr
    [SerializeField] float exhaustedUntil = 30f; // esgotou? só corre de novo com 30 de estamina

    CharacterController controller;
    float stamina;
    bool exhausted;
    float verticalVelocity;  // velocidade de queda (eixo Y)
    float cameraPitch;       // quanto a câmera olha para cima/baixo
    float standHeight;       // altura original do CharacterController
    float standCameraY;      // altura original da câmera

    // Multiplicador que outros scripts podem mudar (ex.: carregando peso = mais lento).
    public float SpeedMultiplier { get; set; } = 1f;

    // Velocidade do PERSONAGEM (ex.: Marcelo é grandalhão e mais lento).
    // Fica separado do SpeedMultiplier porque o carregador sobrescreve aquele ao pegar/soltar.
    public float CharacterSpeed { get; set; } = 1f;

    // "private set": outros scripts podem LER (o CaneTapper lê), mas só este script muda.
    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }

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
        var kb = Keyboard.current;
        bool wantsCrouch = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;

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
        // Mouse.current.delta = quanto o mouse se moveu neste frame.
        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // Girar para os lados: gira o corpo inteiro no eixo Y.
        transform.Rotate(Vector3.up * delta.x);

        // Olhar para cima/baixo: só a câmera, limitado a ±80° para não "dar cambalhota".
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -80f, 80f);
        cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void Move()
    {
        var kb = Keyboard.current;
        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.aKey.isPressed) input.x -= 1;
        input = Vector2.ClampMagnitude(input, 1f); // diagonal não fica mais rápida

        // Converte a direção do teclado para a direção para onde o jogador está virado.
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
        // Só corre se: segurando Shift, andando, em pé e não esgotado.
        bool wantsRun = Keyboard.current.leftShiftKey.isPressed && isMoving && !IsCrouching;
        IsRunning = wantsRun && !exhausted;

        if (IsRunning) stamina -= staminaDrain * Time.deltaTime;
        else stamina += staminaRegen * Time.deltaTime;
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // "Histerese": esgota em 0, mas só libera em 30. Sem isso, o jogador ficaria
        // piscando entre correr/não correr com a estamina em 0.1, 0, 0.1, 0...
        if (stamina <= 0) exhausted = true;
        else if (exhausted && stamina >= exhaustedUntil) exhausted = false;
    }

    // Barra de estamina no canto inferior esquerdo (só aparece quando não está cheia).
    void OnGUI()
    {
        if (stamina >= maxStamina) return;

        const float width = 200f, height = 12f;
        var rect = new Rect(20, Screen.height - 40, width, height);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0, 0, 0, 0.5f), 0, 0);
        rect.width = width * (stamina / maxStamina);
        Color bar = exhausted ? new Color(0.9f, 0.3f, 0.2f) : new Color(0.9f, 0.85f, 0.4f); // vermelho = esgotado
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, bar, 0, 0);
    }
}
