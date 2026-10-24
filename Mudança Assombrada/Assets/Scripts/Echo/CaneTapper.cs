using UnityEngine;
using UnityEngine.InputSystem;

// A bengala da Dona Cida. Fica no Player.
// Espaço = bate a bengala (onda grande). Andar = passos (ondas pequenas, sempre).
public class CaneTapper : MonoBehaviour
{
    [Header("Bengala")]
    [SerializeField] float tapRange = 12f;
    [SerializeField] float tapCooldown = 0.6f; // evita "metralhar" o espaço

    [Header("Passos")]
    [SerializeField] float stepDistance = 1.2f; // um passo a cada 1.2m andados
    [SerializeField] float stepRange = 2.5f;

    float nextTapTime;
    float walked;
    Vector3 lastPosition;

    void Start() => lastPosition = transform.position;

    void Update()
    {
        // Time.time = segundos desde o início do jogo. Técnica clássica de cooldown.
        if (Keyboard.current.spaceKey.wasPressedThisFrame && Time.time >= nextTapTime)
        {
            nextTapTime = Time.time + tapCooldown;
            // A ponta da bengala bate no chão, um pouco à frente dos pés.
            Vector3 tip = transform.position + transform.forward * 0.6f + Vector3.down * 0.9f;
            EchoSystem.Emit(tip, tapRange, 1f, gameObject);
        }

        // Soma quanto andou no chão (ignora o eixo Y).
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0;
        walked += delta.magnitude;
        lastPosition = transform.position;

        if (walked >= stepDistance)
        {
            walked = 0;
            EchoSystem.Emit(transform.position + Vector3.down * 0.9f, stepRange, 0.5f, gameObject);
        }
    }
}
