using UnityEngine;

namespace MudancaAssombrada
{
    // COMPORTAMENTO "caçar pelo som" (o Bibliotecário). Fica junto de um Ghost, que cuida
    // da rede, da captura, dos passos e do reinício; quem ouve é o GhostHearing.
    // Aqui fica só o "jeito" de caçar, como uma MÁQUINA DE ESTADOS: ele está sempre em UM estado,
    // e regras claras decidem quando muda para outro.
    //
    //   Patrulhando --ouviu algo--> Investigando --ouviu perto--> Caçando
    //        ^                           |                           |
    //        +------chegou e esperou-----+<----silêncio por um tempo--+
    //
    // O estado que todos veem mora no Ghost (NetworkVariable). Este script guarda o SEU estado
    // ("mode") e só aplica no Ghost quando está no volante (ver GhostBehaviour): no Inquilino,
    // outro comportamento pode estar mandando enquanto este continua ouvindo.
    // Fantasma novo = duplicar o prefab (Assets/Prefabs/Fantasmas) e mexer nos números.
    [RequireComponent(typeof(GhostHearing))]
    public class GhostHunter : GhostBehaviour
    {
        [Header("Velocidades")]
        [SerializeField] float patrolSpeed = 1.5f;
        [SerializeField] float investigateSpeed = 3f;
        [SerializeField] float huntSpeed = 4.2f;

        [Header("Reação ao som")]
        [SerializeField] float huntDistance = 6f;        // som mais perto que isso = começa a caçar
        [SerializeField] float giveUpTime = 4f;          // segundos de silêncio para desistir da caçada

        [Header("Patrulha")]
        [SerializeField] float patrolRadius = 8f;
        [SerializeField] float searchWaitTime = 3f;      // tempo "farejando" ao chegar no som

        [Header("Vontade de agir (quando há outros comportamentos)")]
        [SerializeField] int patrolPriority = 1;
        [SerializeField] int investigatePriority = 5;
        [SerializeField] int huntPriority = 10;

        GhostHearing hearing;
        GhostState mode = GhostState.Patrolling; // o MEU estado (no Ghost só quando estou no volante)
        Vector3 destination;
        float lastHeardTime;
        float waitTimer;

        protected override void Awake()
        {
            base.Awake();
            // Prefab feito antes do GhostHearing existir: ganha um ouvido com os valores padrão.
            // (Rode "Criar Prefabs que Faltam" para ele ir para o prefab e dar para ajustar.)
            if (!TryGetComponent(out hearing)) hearing = gameObject.AddComponent<GhostHearing>();
        }

        void OnEnable() => hearing.Heard += OnHeard;
        void OnDisable() => hearing.Heard -= OnHeard;

        public override void ServerStart() => EnterMode(GhostState.Patrolling);

        public override int ServerPriority() => mode switch
        {
            GhostState.Hunting => huntPriority,
            GhostState.Investigating => investigatePriority,
            _ => patrolPriority,
        };

        // Ganhou o volante: passos e captura do jeito do Bibliotecário, e continua de onde estava.
        public override void ServerGainControl()
        {
            Ghost.Footsteps = Ghost.DefaultFootsteps;
            Ghost.CatchesOnTouch = true;
            Apply();
        }

        // Derrubou alguém: larga o corpo e volta a patrulhar (os outros ainda podem salvar o caído).
        public override void ServerCaught(NetworkPlayer player) => EnterMode(GhostState.Patrolling);

        void OnHeard(SoundEvent sound, float distance)
        {
            lastHeardTime = Time.time;
            destination = sound.Position; // o NavMesh calcula o caminho desviando das paredes

            if (distance < huntDistance) EnterMode(GhostState.Hunting);
            else if (mode == GhostState.Patrolling) EnterMode(GhostState.Investigating);
            else if (HasControl) Ghost.Agent.SetDestination(destination); // já investigando: vai ao som novo
        }

        public override void ServerTick()
        {
            switch (mode)
            {
                case GhostState.Patrolling:
                    // Chegou no ponto da patrulha? Escolhe outro.
                    if (Ghost.Arrived())
                    {
                        destination = Ghost.RandomPointNear(Ghost.Home, patrolRadius);
                        Ghost.Agent.SetDestination(destination);
                    }
                    break;

                case GhostState.Investigating:
                    // Chegou onde ouviu o som: fica parado "escutando" um pouco.
                    if (Ghost.Arrived())
                    {
                        waitTimer += Time.deltaTime;
                        if (waitTimer >= searchWaitTime) EnterMode(GhostState.Patrolling);
                    }
                    break;

                case GhostState.Hunting:
                    // Silêncio por tempo demais: perdeu a pista, volta a investigar.
                    if (Time.time - lastHeardTime > giveUpTime) EnterMode(GhostState.Investigating);
                    break;
            }
        }

        void EnterMode(GhostState newMode)
        {
            mode = newMode;
            waitTimer = 0;
            if (newMode == GhostState.Patrolling) destination = Ghost.RandomPointNear(Ghost.Home, patrolRadius);
            if (HasControl) Apply();
        }

        // Passa o MEU estado para o corpo: o estado que todos veem, a velocidade e o destino.
        void Apply()
        {
            Ghost.ServerSetState(mode);
            Ghost.Agent.speed = mode switch
            {
                GhostState.Patrolling => patrolSpeed,
                GhostState.Investigating => investigateSpeed,
                _ => huntSpeed, // "_" = qualquer outro caso
            };
            Ghost.Agent.SetDestination(destination);
        }
    }
}
