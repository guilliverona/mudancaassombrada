using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent: o "GPS" da Unity para personagens

namespace MudancaAssombrada
{
    // Em que "momento" o fantasma está. Fica numa NetworkVariable, então TODOS os computadores
    // sabem (o GhostVisual lê isto para animar: orelhas abrindo, máscara virando...).
    // Cada comportamento usa os estados que fazem sentido para ele.
    public enum GhostState : byte
    {
        Patrolling,     // andando à toa
        Investigating,  // indo ver um som
        Hunting,        // perseguindo alguém
        Disguised,      // disfarçado de jogador (Imitador)
        Pulling,        // puxando a vítima (Imitador)
        Fleeing,        // desmascarado, fugindo
    }

    // A BASE comum de todo fantasma: o que todos têm igual.
    //   - registro (Ghost.All), para o GameManager reiniciar todos;
    //   - o corpo que anda (NavMeshAgent) e a casa para onde volta no reinício;
    //   - captura (encostou num jogador = equipe perde);
    //   - passos (para a Cida ouvir ele chegando);
    //   - o ESTADO na rede.
    //
    // O que muda de um fantasma para outro (caçar pelo som, imitar, reger) fica em
    // COMPORTAMENTOS (GhostBehaviour) no mesmo objeto: COMPOSIÇÃO, igual aos personagens.
    // O Inquilino, no final, é só um fantasma com os três comportamentos juntos.
    //
    // EM REDE: o fantasma "pensa" só no host (IsServer). Nos clientes ele é um fantoche:
    // a IA fica desligada, o NetworkTransform copia a posição e a NetworkVariable copia o estado.
    [RequireComponent(typeof(NavMeshAgent))]
    public class Ghost : NetworkBehaviour
    {
        static readonly List<Ghost> all = new();
        public static IReadOnlyList<Ghost> All => all;

        // Escrita "Server": só o host muda; todos leem.
        readonly NetworkVariable<GhostState> state = new(
            GhostState.Patrolling, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // DISFARCE: a posição no Elenco do personagem que ele está copiando (-1 = sem disfarce).
        // Também na rede: todos precisam ver o "Tiago falso".
        readonly NetworkVariable<int> disguise = new(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Ataque")]
        [SerializeField] float catchDistance = 1.2f;

        [Header("Passos (para a Cida ouvir ele chegando)")]
        [Tooltip("Desligado = anda sem fazer barulho (o Imitador).")]
        [SerializeField] bool footsteps = true;
        [SerializeField] float stepDistance = 1.5f;

        float walked;
        Vector3 lastPosition;
        GhostBehaviour[] behaviours;

        public NavMeshAgent Agent { get; private set; }
        public Vector3 Home { get; private set; }
        public bool Caught { get; private set; }
        public GhostState State => state.Value;

        // Avisa quando o estado muda (em TODOS os computadores): (anterior, novo).
        public event Action<GhostState, GhostState> StateChanged;

        public int Disguise => disguise.Value;
        public bool IsDisguised => disguise.Value >= 0;
        // Avisa quando o disfarce muda (em TODOS os computadores). O GhostVisual troca o corpo.
        public event Action<int> DisguiseChanged;

        // Algum fantasma está disfarçado agora? (A dica "Apontar" só aparece quando há um falso.)
        public static bool AnyDisguised
        {
            get
            {
                foreach (var g in all)
                    if (g.IsSpawned && g.IsDisguised) return true;
                return false;
            }
        }

        // Comportamentos podem calar os passos por um tempo (ex.: disfarçado).
        public bool Footsteps { get => footsteps; set => footsteps = value; }

        // Captura ligada? O Imitador, por exemplo, não "pega" encostando: ele puxa para o espelho.
        public bool CatchesOnTouch { get; set; } = true;

        void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Home = transform.position;
            lastPosition = transform.position;
            behaviours = GetComponents<GhostBehaviour>();
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += OnStateChanged;
            disguise.OnValueChanged += OnDisguiseChanged;
            if (IsDisguised) DisguiseChanged?.Invoke(disguise.Value); // quem entra no meio já vê o disfarce
            if (!IsServer)
            {
                // Cliente: quem move o fantasma é o NetworkTransform. Se o NavMeshAgent continuasse
                // ligado, ele "brigaria" com as posições que chegam do host.
                Agent.enabled = false;
                return;
            }
            foreach (var b in behaviours) b.ServerStart();
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnStateChanged;
            disguise.OnValueChanged -= OnDisguiseChanged;
        }

        void OnStateChanged(GhostState previous, GhostState current) => StateChanged?.Invoke(previous, current);
        void OnDisguiseChanged(int previous, int current) => DisguiseChanged?.Invoke(current);

        // Só o host muda o estado; a rede espalha o novo valor.
        public void ServerSetState(GhostState newState)
        {
            if (IsServer) state.Value = newState;
        }

        // Só o host: veste o corpo de um personagem do Elenco (-1 = volta ao próprio corpo).
        public void ServerSetDisguise(int characterIndex)
        {
            if (IsServer) disguise.Value = characterIndex;
        }

        // Um jogador APONTOU para este fantasma ("esse aí é falso!"). Qualquer um pode pedir;
        // o host confere se ele está mesmo disfarçado e avisa os comportamentos.
        public void RequestUnmask() => UnmaskRpc();

        [Rpc(SendTo.Server)]
        void UnmaskRpc()
        {
            if (!IsDisguised || Caught) return;
            foreach (var b in behaviours) b.ServerUnmasked();
        }

        void Update()
        {
            // Antes de conectar, IsServer é false em todos: o fantasma fica parado esperando.
            if (!IsServer || Caught) return;

            foreach (var b in behaviours)
                if (b.enabled) b.ServerTick();

            // Com multiplayer os jogadores aparecem DEPOIS do fantasma (quando conectam),
            // então procuramos o mais próximo a cada frame em vez de guardar um só no Start.
            var player = NearestPlayer();
            if (CatchesOnTouch && player != null && Vector3.Distance(transform.position, player.transform.position) < catchDistance)
                ServerCatch();

            EmitFootsteps();
        }

        // O GameManager (no host) muda o resultado, e a NetworkVariable dele avisa todo mundo:
        // a equipe inteira perde junta.
        public void ServerCatch()
        {
            if (Caught) return;
            Caught = true;
            GameManager.ServerPlayerCaught();
        }

        // Só no host (chamado pelo GameManager ao reiniciar): volta para casa e recomeça.
        public void ServerReset()
        {
            Caught = false;
            CatchesOnTouch = true;
            ServerSetDisguise(-1);
            Agent.Warp(Home); // Warp = teleporte do NavMeshAgent (mover o transform direto confunde ele)
            lastPosition = Home;
            foreach (var b in behaviours) b.ServerReset();
        }

        public NetworkPlayer NearestPlayer()
        {
            NetworkPlayer nearest = null;
            float best = float.MaxValue;
            foreach (var p in NetworkPlayer.All)
            {
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < best) { best = d; nearest = p; }
            }
            return nearest; // null = ninguém conectado ainda
        }

        public bool Arrived() => !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.2f;

        // Escolhe um ponto aleatório e "gruda" ele no NavMesh mais próximo (para não ser dentro de uma parede).
        public static Vector3 RandomPointNear(Vector3 center, float radius)
        {
            Vector3 random = center + UnityEngine.Random.insideUnitSphere * radius;
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
            if (!footsteps) return;
            // Passos mais fortes quando está caçando: a Cida percebe que ele está correndo.
            float intensity = State == GhostState.Hunting ? 0.9f : 0.5f;
            Vector3 feet = transform.position + Vector3.down * Agent.baseOffset;
            EchoSystem.Emit(new SoundEvent(feet, 3.5f, intensity, SoundType.Footstep, gameObject));
        }
    }
}
