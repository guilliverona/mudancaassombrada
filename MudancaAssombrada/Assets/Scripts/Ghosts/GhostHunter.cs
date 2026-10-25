using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent: o "GPS" da Unity para personagens

// Fantasma cego que caça pelo som (base do Bibliotecário da Fase 3).
// Funciona como uma MÁQUINA DE ESTADOS: ele está sempre em UM estado,
// e regras claras decidem quando muda para outro.
//
//   Patrulhando --ouviu algo--> Investigando --ouviu perto--> Caçando
//        ^                           |                           |
//        +------chegou e esperou-----+<----silêncio por um tempo--+
//
// EM REDE: o fantasma "pensa" só no host (IsServer). Nos clientes ele é um fantoche:
// a IA fica desligada e o NetworkTransform só copia a posição que o host manda.
// Assim existe UM fantasma só, igual para todos.
[RequireComponent(typeof(NavMeshAgent))]
public class GhostHunter : NetworkBehaviour
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
    bool caught;
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

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Cliente: quem move o fantasma é o NetworkTransform. Se o NavMeshAgent continuasse
            // ligado, ele "brigaria" com as posições que chegam do host.
            agent.enabled = false;
            return;
        }
        EnterState(State.Patrolling);
    }

    // OnEnable/OnDisable: o jeito certo de se inscrever num evento e se desinscrever dele.
    // Se esquecer de desinscrever, o evento continua chamando um objeto destruído (bug clássico!).
    void OnEnable() => EchoSystem.SoundEmitted += OnSoundHeard;
    void OnDisable() => EchoSystem.SoundEmitted -= OnSoundHeard;

    void OnSoundHeard(Vector3 position, float range, float intensity, GameObject source)
    {
        if (!IsServer) return;            // só o host decide o que o fantasma ouve
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
        // Antes de conectar, IsServer é false em todos: o fantasma fica parado esperando.
        if (!IsServer || caught) return;

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

        // Com multiplayer os jogadores aparecem DEPOIS do fantasma (quando conectam),
        // então procuramos o mais próximo a cada frame em vez de guardar um só no Start.
        player = NearestPlayer();
        if (player != null && Vector3.Distance(transform.position, player.position) < catchDistance)
        {
            // O GameManager (no host) muda o resultado, e a NetworkVariable avisa todo mundo:
            // a equipe inteira perde junta.
            caught = true;
            GameManager.ServerPlayerCaught();
        }

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

    // Só no host (chamado pelo GameManager ao reiniciar): volta para casa e recomeça a patrulha.
    public void ServerReset()
    {
        caught = false;
        agent.Warp(home); // Warp = teleporte do NavMeshAgent (mover o transform direto confunde ele)
        lastPosition = home;
        EnterState(State.Patrolling);
    }

    Transform NearestPlayer()
    {
        Transform nearest = null;
        float best = float.MaxValue;
        foreach (var p in FindObjectsByType<PlayerMovement>())
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best) { best = d; nearest = p.transform; }
        }
        return nearest; // null = ninguém conectado ainda
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
