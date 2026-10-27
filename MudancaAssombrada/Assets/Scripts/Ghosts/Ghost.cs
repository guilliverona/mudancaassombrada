using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components; // NetworkTransform
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
        // Novos sempre NO FIM: o número de cada estado fica gravado nos prefabs (GhostSounds).
        Performing,     // em cena, à mostra, sem atacar: a aparição (Imitador)
        Bowing,         // a reverência: parado 2 s, dá para apontar (Imitador)
        InMirror,       // DENTRO de um espelho: invisível e sem corpo; só a Cida nota o espelho diferente (Imitador)
        Emerging,       // ainda dentro do espelho, prestes a sair (os 3 s de espera perto da vítima) (Imitador)
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
        public GhostState State => state.Value;
        // O comportamento que está no "volante" agora (só no host; null = nenhum quer agir).
        public GhostBehaviour Active { get; private set; }

        // Avisa quando o estado muda (em TODOS os computadores): (anterior, novo).
        public event Action<GhostState, GhostState> StateChanged;

        public int Disguise => disguise.Value;
        public bool IsDisguised => disguise.Value >= 0;
        // Dá para APONTAR para ele agora? Disfarçado, ou parado na reverência (a janela de contra-ataque).
        public bool CanBeUnmasked => IsDisguised || state.Value == GhostState.Bowing;
        // Está DENTRO de um espelho (esperando ou prestes a sair)? Sem corpo, invisível.
        public bool IsInsideMirror => state.Value is GhostState.InMirror or GhostState.Emerging;
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

        // Passos e captura são do CORPO: quem está no volante decide (ao ganhar o controle).
        // O Imitador cala os passos e não "pega" encostando (ele puxa para o espelho);
        // o Bibliotecário liga os dois de volta.
        public bool Footsteps { get => footsteps; set => footsteps = value; }
        public bool CatchesOnTouch { get; set; } = true;
        // Como o prefab veio (o Inspector): os comportamentos voltam a isto quando não precisam mudar.
        public bool DefaultFootsteps { get; private set; }

        void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Home = transform.position;
            lastPosition = transform.position;
            behaviours = GetComponents<GhostBehaviour>();
            DefaultFootsteps = footsteps;
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
        // o host confere se ele está mesmo disfarçado (ou na reverência) e avisa os comportamentos.
        public void RequestUnmask() => UnmaskRpc();

        [Rpc(SendTo.Server)]
        void UnmaskRpc()
        {
            if (!CanBeUnmasked) return;
            foreach (var b in behaviours) b.ServerUnmasked();
        }

        void Update()
        {
            // Antes de conectar, IsServer é false em todos: o fantasma fica parado esperando.
            if (!IsServer) return;

            Arbitrate();
            foreach (var b in behaviours)
            {
                if (!b.enabled) continue;
                if (b == Active) b.ServerTick();
                else b.ServerIdleTick();
            }

            // Com multiplayer os jogadores aparecem DEPOIS do fantasma (quando conectam),
            // então procuramos o mais próximo a cada frame em vez de guardar um só no Start.
            var player = NearestPlayer();
            if (CatchesOnTouch && player != null && Vector3.Distance(transform.position, player.transform.position) < catchDistance)
                ServerCatch(player);

            EmitFootsteps();
        }

        // O ÁRBITRO: entrega o "volante" ao comportamento que mais quer o controle agora.
        // Empate fica com quem já está (sem ficar trocando de um frame para o outro).
        void Arbitrate()
        {
            GhostBehaviour best = Active != null && Active.enabled ? Active : null;
            int bestPriority = best != null ? best.ServerPriority() : int.MinValue;
            foreach (var b in behaviours)
            {
                if (!b.enabled || b == best) continue;
                int p = b.ServerPriority();
                if (p > bestPriority) { best = b; bestPriority = p; }
            }
            if (best != null && bestPriority < 0) best = null; // ninguém quer: fica parado

            if (best == Active) return;
            var previous = Active;
            Active = best;
            if (previous != null) previous.ServerLoseControl();
            if (Active != null) Active.ServerGainControl();
        }

        // PEGOU alguém: o jogador fica CAÍDO até um colega ajudar (PlayerCondition).
        // A equipe só perde se todos caírem (quem confere é o GameManager).
        public void ServerCatch(NetworkPlayer player)
        {
            if (player == null || player.Condition == null || player.Condition.IsDown) return;
            player.Condition.ServerSet(PlayerState.Down, NetworkObject);
            foreach (var b in behaviours) b.ServerCaught(player);
        }

        // Só no host (chamado pelo GameManager ao reiniciar): volta para casa e recomeça.
        public void ServerReset()
        {
            CatchesOnTouch = true;
            footsteps = DefaultFootsteps;
            ServerSetDisguise(-1);
            Agent.Warp(Home); // Warp = teleporte do NavMeshAgent (mover o transform direto confunde ele)
            lastPosition = Home;
            Active = null; // o árbitro escolhe de novo no próximo frame
            foreach (var b in behaviours) b.ServerReset();
        }

        // Só no host: TELEPORTE de verdade (de um espelho para outro). Sem avisar o NetworkTransform,
        // os outros computadores veriam o fantasma "deslizar" pela casa até o ponto novo.
        public void ServerTeleport(Vector3 position)
        {
            Agent.Warp(position);
            lastPosition = position;
            if (TryGetComponent(out NetworkTransform net) && net.CanCommitToTransform)
                net.Teleport(position, transform.rotation, transform.localScale);
        }

        public NetworkPlayer NearestPlayer()
        {
            NetworkPlayer nearest = null;
            float best = float.MaxValue;
            foreach (var p in NetworkPlayer.All)
            {
                if (p.Condition != null && p.Condition.IsDown) continue; // caído não é alvo
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < best) { best = d; nearest = p; }
            }
            return nearest; // null = ninguém conectado (ou todos caídos)
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
