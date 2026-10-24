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

    CharacterController controller;
    float verticalVelocity;  // velocidade de queda (eixo Y)
    float cameraPitch;       // quanto a câmera olha para cima/baixo

    // Multiplicador que outros scripts podem mudar (ex.: carregando peso = mais lento).
    public float SpeedMultiplier { get; set; } = 1f;

    // Awake roda uma vez, antes de Start. Bom lugar para pegar referências.
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // prende e esconde o mouse
    }

    // Update roda uma vez por frame.
    void Update()
    {
        Look();
        Move();
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

        Vector3 velocity = move * walkSpeed * SpeedMultiplier + Vector3.up * verticalVelocity;

        // Time.deltaTime = segundos desde o último frame. Multiplicar por ele deixa
        // o movimento igual em PCs rápidos ou lentos.
        controller.Move(velocity * Time.deltaTime);
    }
}
