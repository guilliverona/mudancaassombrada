using UnityEngine;
using UnityEngine.InputSystem;

// Fica no Player. Aperte E olhando para um móvel para pegar; E de novo para soltar.
public class FurnitureCarrier : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] float reach = 2.5f;          // distância máxima para alcançar
    [SerializeField] float holdDistance = 1.8f;   // onde o móvel fica à sua frente
    [SerializeField] float followStrength = 12f;  // quão "firme" você segura

    PlayerMovement movement;
    Furniture held; // null = mãos vazias

    void Awake() => movement = GetComponent<PlayerMovement>();

    void Update()
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (held != null) Drop();
        else TryPickUp();
    }

    void TryPickUp()
    {
        // Raycast: um "raio laser" invisível saindo do centro da câmera.
        // Se bater em algo dentro do alcance, 'hit' diz o que foi.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, reach)) return;

        // Só pega se o objeto atingido tiver o componente Furniture.
        if (!hit.collider.TryGetComponent(out Furniture furniture)) return;

        held = furniture;
        held.IsHeld = true;
        held.Body.useGravity = false;
        held.Body.linearDamping = 10f; // amortece para não ficar balançando
        movement.SpeedMultiplier = 1f / held.Weight; // mais pesado = mais lento
    }

    void Drop()
    {
        held.Body.useGravity = true;
        held.Body.linearDamping = 0f;
        movement.SpeedMultiplier = 1f;
        held.IsHeld = false;
        held = null;
    }

    // FixedUpdate roda no ritmo da física (50x por segundo por padrão).
    // Tudo que mexe em Rigidbody deve ficar aqui, e não no Update.
    void FixedUpdate()
    {
        if (held == null) return;

        Vector3 target = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        Vector3 toTarget = target - held.Body.position;

        // Em vez de "teletransportar" o móvel, damos velocidade na direção do alvo.
        // Assim ele ainda colide com paredes e portas, igual a carregar de verdade.
        held.Body.linearVelocity = toTarget * followStrength / held.Weight;

        // Se prender em algo e ficar longe demais, você solta.
        if (toTarget.magnitude > reach * 1.5f) Drop();
    }
}
