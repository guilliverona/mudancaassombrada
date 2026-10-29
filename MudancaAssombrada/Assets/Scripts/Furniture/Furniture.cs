using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MudancaAssombrada
{
    // Marca um objeto como "móvel carregável". Fica nos prefabs de móveis (Assets/Prefabs/Móveis).
    // Móvel novo = duplicar um prefab, trocar o modelo e o peso. Sem código.
    //
    // EM REDE: quem simula a física de um móvel é o DONO dele.
    //  - Parado no chão: o dono é o host.
    //  - Alguém pega: o host passa a posse para essa pessoa, e o computador DELA simula
    //    o móvel enquanto carrega (sem atraso, como se estivesse jogando sozinho).
    //  - Soltou: a posse volta para o host.
    // Nos outros computadores o Rigidbody fica "kinematic" (sem física própria) e só copia
    // a posição que chega pela rede. Quem faz essa troca é o componente NetworkRigidbody.
    //
    // MÓVEL PESADO: cada um LEVANTA sozinho até a própria força (CharacterDefinition.LiftLimit): Cida 1,
    // Tiago 1,5, Marcelo 3. Mais pesado, quem pega é a primeira ponta (o dono) e um colega pode
    // "Segurar a outra ponta" (o ajudante); a dupla levanta até a soma dos dois limites.
    // Faltou gente? Dá para pegar, mas só ARRASTANDO: no chão, devagar e fazendo barulho.
    // (Num móvel que a pessoa levanta mas sente o peso, a outra ponta também vale: a dupla anda mais rápido.)
    // O Imitador disfarçado pode fingir que segura a outra ponta (a ponta FALSA): o nome do colega
    // aparece, mas ele não vale nada. O móvel continua arrastando, sem o "toc" do apoio (as pistas).
    // As contas (quem levanta, peso sentido, passo da dupla, quem pode ajudar) ficam na CarryRules.
    [RequireComponent(typeof(Rigidbody))]
    public class Furniture : NetworkBehaviour, IInteractionTarget
    {
        static readonly List<Furniture> all = new();

        // Todos os móveis da fase (o GameManager conta e reinicia a partir daqui).
        public static IReadOnlyList<Furniture> All => all;

        // 1 = leve (cadeira), 3 = pesado (armário). A força do personagem divide isto.
        // Mais que a força de quem pega = só arrasta sozinho (Cida 1, Tiago 1,5, Marcelo 3).
        [SerializeField, Range(1f, 5f)] float weight = 1f;

        [Header("Arrastar e dupla")]
        [SerializeField] float dragNoiseRange = 10f;   // arrastar sozinho se ouve longe
        [SerializeField] float dragNoiseEvery = 1.2f;  // um "rasgo" no chão a cada tantos segundos andando
        [SerializeField] float helperReach = 3.5f;     // ajudante mais longe que isto solta a ponta

        public const ulong NoHelper = ulong.MaxValue;

        // Todos precisam saber se o móvel está sendo carregado: o caminhão só conta móvel SOLTO,
        // e ninguém pode pegar um móvel que já está na mão de outro.
        readonly NetworkVariable<bool> isHeld = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Quem segura a OUTRA ponta (o ClientId do colega). NoHelper = ninguém.
        readonly NetworkVariable<ulong> helper = new(
            NoHelper, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // A ponta FALSA: o personagem que a cópia está fingindo ser (-1 = nenhuma).
        readonly NetworkVariable<int> fakeHelper = new(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float Weight => weight;
        // Móvel GRANDE (peso acima de 2: penteadeira, armário): na escada anda mais devagar e cada
        // degrau range. Só serve para a escada; quem levanta o quê sai da força (CanLift).
        public bool IsBulky => CarryRules.IsBulky(weight);
        public Rigidbody Body { get; private set; }
        public bool IsHeld => isHeld.Value;
        public ulong HelperClientId => helper.Value;
        public bool HasFakeHelper => fakeHelper.Value >= 0;
        public float NoiseMultiplier { get; set; } = 1f; // < 1 = batidas mais silenciosas

        Vector3 startPosition;
        Quaternion startRotation;
        Vector3 lastNoisePosition;
        float nextDragNoise;
        Collider[] solids;       // os colliders que bloqueiam (os que já eram trigger ficam de fora)
        bool ghostly;            // levantado: os colliders viram trigger e o móvel atravessa tudo

        // LEVANTADO (na mão, não arrastando): o móvel não bate em nada (portas, paredes, jogadores).
        // Assim ninguém fica preso no batente carregando. Continua sendo trigger (não desligado)
        // para a mira achar ele ("Segurar a outra ponta") e o caminhão saber que ele está dentro.
        public bool IsLifted => isHeld.Value && CanLift;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            var list = new List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) list.Add(c);
            solids = list.ToArray();
            startPosition = transform.position; // guarda onde o móvel começa, para o reinício
            startRotation = transform.rotation;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn() => helper.OnValueChanged += OnHelperChanged;
        public override void OnNetworkDespawn() => helper.OnValueChanged -= OnHelperChanged;

        // ---------- Quem está carregando (todos os computadores sabem) ----------

        // O jogador que segura a primeira ponta (o dono do móvel). null = solto.
        public NetworkPlayer Holder => isHeld.Value ? PlayerOf(OwnerClientId) : null;
        public NetworkPlayer Helper => helper.Value != NoHelper ? PlayerOf(helper.Value) : null;

        static NetworkPlayer PlayerOf(ulong clientId)
        {
            foreach (var p in NetworkPlayer.All)
                if (p.OwnerClientId == clientId) return p;
            return null;
        }

        static float LimitOf(NetworkPlayer p) => p != null ? p.Character.Current.LiftLimit : 0f;
        static float StrengthOf(NetworkPlayer p) => p != null ? p.Character.Current.strength : 0f;
        static float SpeedOf(NetworkPlayer p) => p != null ? p.Character.Current.speed : 0f;

        // Dá para levantar? A soma do que cada ponta levanta sozinha tem que vencer o peso.
        // A ponta falsa vale ZERO: é a mentira da isca. Senão, quem segura só arrasta.
        public bool CanLift => CarryRules.CanLift(LimitOf(Holder), LimitOf(Helper), weight);
        // Quem segura sente o peso (é mais pesado que a força dele)? Então cabe alguém na outra ponta.
        bool HolderFeelsWeight => CarryRules.FeelsWeight(StrengthOf(Holder), weight);
        // A força somada de quem segura (divide o peso). A ponta falsa não ajuda em nada.
        public float CrewStrength => CarryRules.CrewStrength(StrengthOf(Holder), StrengthOf(Helper));
        // A dupla anda no passo do mais lento.
        public float CrewSpeed => CarryRules.CrewSpeed(SpeedOf(Holder), SpeedOf(Helper));
        // Peso que quem carrega sente (nunca menos que 1).
        public float FeltWeight => CarryRules.FeltWeight(weight, CrewStrength);

        // Quem está na outra ponta, como quem carrega vê: o colega, o "colega" falso ou ninguém.
        public string OtherEndName
        {
            get
            {
                var h = Helper;
                if (h != null) return h.Character.Current.displayName;
                var roster = CharacterRoster.Instance;
                if (HasFakeHelper && roster != null && roster.IsValid(fakeHelper.Value)) return roster.Get(fakeHelper.Value).displayName;
                return null;
            }
        }

        // O móvel que este jogador está segurando pela primeira ponta (no host: para a isca do Imitador).
        public static Furniture HeldBy(ulong clientId)
        {
            foreach (var f in all)
                if (f.IsSpawned && f.IsHeld && f.OwnerClientId == clientId) return f;
            return null;
        }

        // Este jogador está com as mãos ocupadas em OUTRO móvel (primeira ponta ou outra ponta)?
        bool BusyElsewhere(ulong clientId)
        {
            foreach (var f in all)
                if (f != this && f.IsSpawned && f.IsHeld && (f.OwnerClientId == clientId || f.helper.Value == clientId))
                    return true;
            return false;
        }

        // ---------- Só no host ----------

        // Só no host (chamado pelo GameManager ao reiniciar): devolve o móvel ao lugar inicial.
        public void ServerReset()
        {
            helper.Value = NoHelper;
            fakeHelper.Value = -1;
            if (isHeld.Value)
            {
                isHeld.Value = false;
                NetworkObject.RemoveOwnership(); // tira da mão de quem estava carregando
            }
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = startPosition;
            // Teleport: "pule para lá sem deslizar". Sem ele, os outros computadores veriam o
            // móvel atravessar a casa voando até o lugar inicial (a interpolação "suaviza" o salto).
            GetComponent<NetworkTransform>().Teleport(startPosition, startRotation, transform.localScale);
        }

        // O Imitador disfarçado "segura a outra ponta" (characterIndex = quem ele copia; -1 = largou).
        public void ServerSetFakeHelper(int characterIndex)
        {
            if (!IsServer) return;
            if (characterIndex >= 0 && (!isHeld.Value || helper.Value != NoHelper)) return; // ponta ocupada
            fakeHelper.Value = characterIndex;
        }

        void Update()
        {
            if (!IsSpawned) return;
            // Roda em TODOS os computadores: em cada um o móvel levantado deixa os jogadores passarem.
            SetGhostly(IsLifted);
            if (IsServer) ServerCheckHelper();
            if (IsOwner) DragNoise();
        }

        static readonly Collider[] nearby = new Collider[16];

        // Dá para soltar aqui? Levantado, o móvel pode estar dentro de uma parede (atravessa tudo);
        // aí não solta (a conta é a CarryRules.BlocksDrop). Os jogadores não contam: saem da frente.
        public bool HasRoomToDrop()
        {
            if (!ghostly) return true; // arrastando, a física já não deixa entrar na parede
            foreach (var mine in solids)
            {
                Bounds b = mine.bounds;
                int count = Physics.OverlapBoxNonAlloc(b.center, b.extents, nearby, Quaternion.identity,
                                                       ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var other = nearby[i];
                    if (other.transform.IsChildOf(transform) || other.GetComponentInParent<NetworkPlayer>() != null) continue;
                    if (Physics.ComputePenetration(mine, mine.transform.position, mine.transform.rotation,
                                                   other, other.transform.position, other.transform.rotation,
                                                   out Vector3 direction, out float distance)
                        && CarryRules.BlocksDrop(direction, distance))
                        return false;
                }
            }
            return true;
        }

        void SetGhostly(bool on)
        {
            if (on == ghostly) return;
            ghostly = on;
            foreach (var c in solids) c.isTrigger = on;
        }

        // O ajudante caiu, foi puxado, saiu da partida ou se afastou: a ponta fica livre.
        void ServerCheckHelper()
        {
            if (helper.Value == NoHelper) return;
            var h = Helper;
            bool ok = isHeld.Value && h != null && h.Condition != null && h.Condition.IsFree
                      && Vector3.Distance(h.transform.position, transform.position) <= helperReach;
            if (!ok) helper.Value = NoHelper;
        }

        // Arrastando (faltou gente): o dono faz o barulho, que a rede leva aos outros (SoundRelay).
        void DragNoise()
        {
            if (!isHeld.Value || CanLift) { lastNoisePosition = transform.position; return; }
            if (Time.time < nextDragNoise) return;
            float moved = Vector3.Distance(transform.position, lastNoisePosition);
            if (moved < 0.3f) return; // parado não faz barulho
            lastNoisePosition = transform.position;
            nextDragNoise = Time.time + dragNoiseEvery;
            EchoSystem.Emit(new SoundEvent(transform.position, dragNoiseRange * NoiseMultiplier, 0.9f, SoundType.Carry, gameObject));
        }

        // Um colega de verdade pegou a outra ponta: o "toc" do apoio, em todos os computadores
        // (cada um recebe a mudança da NetworkVariable e faz o som no seu). A ponta falsa NÃO faz.
        void OnHelperChanged(ulong previous, ulong current)
        {
            if (current == NoHelper) return;
            EchoSystem.EmitLocal(new SoundEvent(transform.position, 8f, 0.4f, SoundType.Carry, gameObject));
        }

        // ---------- Interação ----------

        // Mirando o móvel: "Pegar Armário", "Arrastar...", "Segurar a outra ponta" ou "Ocupado".
        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (player.Carrier == null) return InteractionPrompt.None;
            if (player.Carrier.IsWaiting) return new InteractionPrompt("Ocupado", false);
            if (IsHeld)
            {
                bool free = CarryRules.HelpOffered(HolderFeelsWeight, helper.Value != NoHelper, HasFakeHelper);
                return free ? new InteractionPrompt($"Segurar a outra ponta ({name})", true) : new InteractionPrompt("Ocupado", false);
            }
            if (player.Character.LiftLimit < weight)
                return new InteractionPrompt($"Arrastar {name} (pesado: chame alguém para a outra ponta)", true);
            return new InteractionPrompt($"Pegar {name}", true);
        }

        public void Interact(PlayerInteractor player)
        {
            if (IsHeld) player.Carrier.TryHelp(this);
            else player.Carrier.TryPickUp(this);
        }

        public void RequestPickUp() => PickUpRpc();
        public void RequestDrop() => DropRpc();
        public void RequestHelp() => HelpRpc(true);
        public void RequestLeaveHelp() => HelpRpc(false);

        // RpcParams traz informações sobre a chamada, como QUEM pediu (SenderClientId).
        [Rpc(SendTo.Server)]
        void PickUpRpc(RpcParams rpcParams = default)
        {
            // Dois jogadores pegaram ao mesmo tempo: o primeiro leva. E quem já está com as mãos
            // ocupadas em outro móvel não pega este (o cliente confere, mas quem decide é o host).
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!CarryRules.CanAcceptPickUp(isHeld.Value, BusyElsewhere(sender))) return;
            isHeld.Value = true;
            if (sender != OwnerClientId) NetworkObject.ChangeOwnership(sender); // o host já é dono
        }

        [Rpc(SendTo.Server)]
        void DropRpc(RpcParams rpcParams = default)
        {
            // Só quem está segurando pode soltar (evita um cliente "arrancar" o móvel de outro).
            if (OwnerClientId != rpcParams.Receive.SenderClientId) return;
            isHeld.Value = false;
            helper.Value = NoHelper; // soltou a primeira ponta: a outra cai junto
            fakeHelper.Value = -1;
            NetworkObject.RemoveOwnership(); // devolve a posse para o host
        }

        [Rpc(SendTo.Server)]
        void HelpRpc(bool holding, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (holding)
            {
                // O cliente já confere as mãos livres, mas quem decide é o host (pedidos ao mesmo tempo).
                if (CarryRules.CanAcceptHelper(isHeld.Value, HolderFeelsWeight, helper.Value != NoHelper, fakeHelper.Value >= 0,
                                               sender, OwnerClientId, BusyElsewhere(sender)))
                    helper.Value = sender;
            }
            else if (helper.Value == sender) helper.Value = NoHelper;
        }

        // Chamado pela Unity quando este objeto bate em algo.
        // Móvel caindo ou batendo na parede faz barulho, e a Cida "vê" o barulho.
        void OnCollisionEnter(Collision collision)
        {
            // Só quem simula a física (o dono) faz o som. O SoundRelay manda para os outros.
            // Sem isso, cada computador faria o mesmo barulho e ele chegaria repetido.
            if (IsSpawned && !IsOwner) return;

            float impact = collision.relativeVelocity.magnitude;
            if (impact < 1.5f) return; // encostar devagar não faz barulho

            Vector3 point = collision.GetContact(0).point;
            float range = Mathf.Clamp(impact * 1.5f, 3f, 10f) * NoiseMultiplier;
            EchoSystem.Emit(new SoundEvent(point, range, Mathf.Clamp01(impact / 5f), SoundType.Impact, gameObject));
        }
    }
}
