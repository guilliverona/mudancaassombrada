using UnityEngine;
using UnityEngine.AI; // NavMeshAgent: o "GPS" da Unity para personagens

// Fantasma cego que caça pelo som (base do Bibliotecário da Fase 3).
// Funciona como uma MÁQUINA DE ESTADOS: ele está sempre em UM estado,
// e regras claras decidem quando muda para outro.
//
//   Patrulhando --ouviu algo--> Investigando --ouviu perto--> Caçando
//        ^                           |                           |
//        +------chegou e esperou-----+<----silêncio por um tempo--+
[RequireComponent(typeof(NavMeshAgent))]
public class GhostHunter : MonoBehaviour
{
    // enum = uma lista fixa de opções com nome. Bem mais legível que usar 0, 1, 2.
    enum State { Patrolling, Investigating, Hunting }

    [Header("Velocidades")]
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float investigateSpeed = 3f;
    [SerializeField] float huntSpeed = 4.2f;

    [Header("Audição")]
    [SerializeField] float hearingMultiplier = 1.5f; // ouve sons um pouco além do alcance deles
    [SerializeField] float huntDistance = 6f;        // som mais perto que isso = começa a caçar
    [SerializeField] float giveUpTime = 4f;          // segundos de silêncio para desistir da caçada

    [Header("Patrulha")]
    [SerializeField] float patrolRadius = 8f;
    [SerializeField] float searchWaitTime = 3f;      // tempo "farejando" ao chegar no som

    [Header("Ataque")]
    [SerializeField] float catchDistance = 1.2f;

    [Header("Passos (para a Cida ouvir ele chegando)")]
    [SerializeField] float stepDistance = 1.5f;

    NavMeshAgent agent;
    Transform player;
    Vector3 home;
    State state;
    float lastHeardTime;
    float waitTimer;
    float walked;
    Vector3 lastPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
        lastPosition = transform.position;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>().transform;
        EnterState(State.Patrolling);
    }

    // OnEnable/OnDisable: o jeito certo de se inscrever num evento e se desinscrever dele.
    // Se esquecer de desinscrever, o evento continua chamando um objeto destruído (bug clássico!).
    void OnEnable() => EchoSystem.SoundEmitted += OnSoundHeard;
    void OnDisable() => EchoSystem.SoundEmitted -= OnSoundHeard;

    void OnSoundHeard(Vector3 position, float range, float intensity, GameObject source)
    {
        if (source == gameObject) return; // ignora os próprios passos

        float distance = Vector3.Distance(transform.position, position);
        if (distance > range * hearingMultiplier) return; // longe demais, não ouviu

        lastHeardTime = Time.time;
        agent.SetDestination(position); // o NavMesh calcula o caminho desviando das paredes

        if (distance < huntDistance) EnterState(State.Hunting);
        else if (state == State.Patrolling) EnterState(State.Investigating);
    }

    void Update()
    {
        switch (state)
        {
            case State.Patrolling:
                // Chegou no ponto da patrulha? Escolhe outro.
                if (Arrived()) agent.SetDestination(RandomPointNear(home, patrolRadius));
                break;

            case State.Investigating:
                // Chegou onde ouviu o som: fica parado "escutando" um pouco.
                if (Arrived())
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= searchWaitTime) EnterState(State.Patrolling);
                }
                break;

            case State.Hunting:
                // Silêncio por tempo demais: perdeu a pista, volta a investigar.
                if (Time.time - lastHeardTime > giveUpTime) EnterState(State.Investigating);
                break;
        }

        if (Vector3.Distance(transform.position, player.position) < catchDistance)
            GameManager.PlayerCaught("O Bibliotecário te pegou!");

        EmitFootsteps();
    }

    void EnterState(State newState)
    {
        state = newState;
        waitTimer = 0;
        agent.speed = newState switch
        {
            State.Patrolling => patrolSpeed,
            State.Investigating => investigateSpeed,
            _ => huntSpeed, // "_" = qualquer outro caso
        };
        if (newState == State.Patrolling) agent.SetDestination(RandomPointNear(home, patrolRadius));
    }

    bool Arrived() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;

    // Escolhe um ponto aleatório e "gruda" ele no NavMesh mais próximo (para não ser dentro de uma parede).
    static Vector3 RandomPointNear(Vector3 center, float radius)
    {
        Vector3 random = center + Random.insideUnitSphere * radius;
        return NavMesh.SamplePosition(random, out NavMeshHit hit, radius, NavMesh.AllAreas) ? hit.position : center;
    }

    void EmitFootsteps()
    {
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0;
        walked += delta.magnitude;
        lastPosition = transform.position;

        if (walked < stepDistance) return;
        walked = 0;
        // Passos mais fortes quando está caçando: a Cida percebe que ele está correndo.
        float intensity = state == State.Hunting ? 0.9f : 0.5f;
        EchoSystem.Emit(transform.position + Vector3.down * 0.9f, 3.5f, intensity, gameObject);
    }
}
