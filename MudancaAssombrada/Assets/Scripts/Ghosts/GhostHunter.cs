using UnityEngine;

namespace MudancaAssombrada
{
    // COMPORTAMENTO "caçar pelo som" (o Bibliotecário). Fica junto de um Ghost, que cuida
    // da rede, da captura, dos passos e do reinício; aqui fica só o "jeito" de caçar.
    // Funciona como uma MÁQUINA DE ESTADOS: ele está sempre em UM estado,
    // e regras claras decidem quando muda para outro.
    //
    //   Patrulhando --ouviu algo--> Investigando --ouviu perto--> Caçando
    //        ^                           |                           |
    //        +------chegou e esperou-----+<----silêncio por um tempo--+
    //
    // O estado mora no Ghost (NetworkVariable): todos os computadores veem o mesmo.
    // Fantasma novo = duplicar o prefab (Assets/Prefabs/Fantasmas) e mexer nos números.
    public class GhostHunter : GhostBehaviour
    {
        [Header("Velocidades")]
        [SerializeField] float patrolSpeed = 1.5f;
        [SerializeField] float investigateSpeed = 3f;
        [SerializeField] float huntSpeed = 4.2f;

        [Header("Audição")]
        [Tooltip("Que tipos de som chamam a atenção dele. A música, por exemplo, ele ignora.")]
        [SerializeField] SoundType hears = SoundType.Footstep | SoundType.Voice | SoundType.Impact | SoundType.Cane | SoundType.Whistle;
        [SerializeField] float hearingMultiplier = 1.5f; // ouve sons um pouco além do alcance deles
        [SerializeField] float huntDistance = 6f;        // som mais perto que isso = começa a caçar
        [SerializeField] float giveUpTime = 4f;          // segundos de silêncio para desistir da caçada

        [Header("Patrulha")]
        [SerializeField] float patrolRadius = 8f;
        [SerializeField] float searchWaitTime = 3f;      // tempo "farejando" ao chegar no som

        float lastHeardTime;
        float waitTimer;

        // OnEnable/OnDisable: o jeito certo de se inscrever num evento e se desinscrever dele.
        // Se esquecer de desinscrever, o evento continua chamando um objeto destruído (bug clássico!).
        void OnEnable() => EchoSystem.SoundEmitted += OnSoundHeard;
        void OnDisable() => EchoSystem.SoundEmitted -= OnSoundHeard;

        public override void ServerStart() => EnterState(GhostState.Patrolling);

        void OnSoundHeard(SoundEvent sound)
        {
            if (!Ghost.IsServer || Ghost.Caught) return; // só o host decide o que o fantasma ouve
            if (sound.Source == gameObject) return;      // ignora os próprios passos
            if (!sound.Is(hears)) return;                // tipo de som que não interessa a ele

            float distance = Vector3.Distance(transform.position, sound.Position);
            if (distance > sound.Range * hearingMultiplier) return; // longe demais, não ouviu

            lastHeardTime = Time.time;
            Ghost.Agent.SetDestination(sound.Position); // o NavMesh calcula o caminho desviando das paredes

            if (distance < huntDistance) EnterState(GhostState.Hunting);
            else if (Ghost.State == GhostState.Patrolling) EnterState(GhostState.Investigating);
        }

        public override void ServerTick()
        {
            switch (Ghost.State)
            {
                case GhostState.Patrolling:
                    // Chegou no ponto da patrulha? Escolhe outro.
                    if (Ghost.Arrived()) Ghost.Agent.SetDestination(Ghost.RandomPointNear(Ghost.Home, patrolRadius));
                    break;

                case GhostState.Investigating:
                    // Chegou onde ouviu o som: fica parado "escutando" um pouco.
                    if (Ghost.Arrived())
                    {
                        waitTimer += Time.deltaTime;
                        if (waitTimer >= searchWaitTime) EnterState(GhostState.Patrolling);
                    }
                    break;

                case GhostState.Hunting:
                    // Silêncio por tempo demais: perdeu a pista, volta a investigar.
                    if (Time.time - lastHeardTime > giveUpTime) EnterState(GhostState.Investigating);
                    break;
            }
        }

        void EnterState(GhostState newState)
        {
            Ghost.ServerSetState(newState);
            waitTimer = 0;
            Ghost.Agent.speed = newState switch
            {
                GhostState.Patrolling => patrolSpeed,
                GhostState.Investigating => investigateSpeed,
                _ => huntSpeed, // "_" = qualquer outro caso
            };
            if (newState == GhostState.Patrolling) Ghost.Agent.SetDestination(Ghost.RandomPointNear(Ghost.Home, patrolRadius));
        }
    }
}
