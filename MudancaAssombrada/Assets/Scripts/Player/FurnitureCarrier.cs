using UnityEngine;

namespace MudancaAssombrada
{
    // Fica no Player. CARREGAR um móvel: segurar, o peso deixar lento, soltar.
    // Quem mira e aperta a tecla é o PlayerInteractor; o móvel (Furniture) pede para ser pego
    // chamando TryPickUp (ou TryHelp, para a outra ponta). Assim este script só sabe de peso e física.
    //
    // EM REDE, pegar tem duas fases:
    //   1. Pedir: "host, posso pegar este móvel?" (RequestPickUp)
    //   2. Esperar a resposta: quando o móvel passa a ser MEU (IsOwner), começo a carregar.
    // Se outro jogador pegou antes, o host recusa e o pedido expira sozinho.
    //
    // MÓVEL DE DUPLA: quem pegou primeiro segura a primeira ponta e simula a física (é o dono);
    // quem chega depois segura a OUTRA ponta (ajudante): não mexe na física, só anda junto. O móvel
    // vai para o meio das duas mãos, a dupla anda no passo do mais lento, e na escada mais devagar ainda.
    // Sem gente suficiente, o dono só ARRASTA: o móvel fica no chão, devagar e barulhento.
    public class FurnitureCarrier : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 2.5f;          // longe assim do alvo, o móvel escapa da mão
        [SerializeField] float holdDistance = 1.8f;   // onde o móvel fica à sua frente
        [SerializeField] float followStrength = 12f;  // quão "firme" você segura
        [SerializeField] float requestTimeout = 1f;   // segundos esperando a resposta do host

        [Header("Dupla, arrastar e escada")]
        [Tooltip("Arrastando (o móvel pesa mais que a força de quem segura): esta fração da velocidade, " +
                 "dividida ainda pelo excesso de peso. Cida (força 1) arrastando o armário (3,5): 0,35 ÷ 3,5 = 10%.")]
        [SerializeField] float dragSpeed = 0.35f;
        [SerializeField] float stairsSlowdown = 1.5f;     // móvel de dupla na escada: velocidade ÷ isto
        [SerializeField] float stairStepEvery = 0.7f;     // um degrau rangendo a cada tantos metros
        [SerializeField] float stairStepRange = 9f;

        // Força: divide o peso dos móveis (vem da ficha do personagem).
        public float Strength { get; set; } = 1f;
        // Barulho dos móveis batendo enquanto ESTE jogador carrega.
        public float CarryNoise { get; set; } = 1f;

        public bool IsHolding => held != null;
        public bool IsHelping => helping != null;
        // Mãos ocupadas (primeira ponta ou outra ponta): a tecla Interagir vira "Soltar".
        public bool HasLoad => held != null || helping != null;
        // Já pedi um móvel e estou esperando o host? (não dá para pedir dois)
        public bool IsWaiting => requested != null || helpRequested != null;

        public InteractionPrompt DropPrompt
        {
            get
            {
                if (helping != null) return new InteractionPrompt($"Soltar a ponta de {helping.name}", true);
                if (held == null) return InteractionPrompt.None;
                string text = held.CanLift ? $"Soltar {held.name}" : $"Soltar {held.name} (arrastando: falta gente)";
                string other = held.OtherEndName;
                if (other != null) text += $" · {other} na outra ponta";
                return new InteractionPrompt(text, true);
            }
        }

        PlayerMovement movement;
        NetworkPlayer player;
        Furniture held;          // a primeira ponta (sou o dono). null = não
        Furniture helping;       // a outra ponta. null = não
        Furniture requested;     // pedi para pegar e estou esperando
        Furniture helpRequested; // pedi a outra ponta e estou esperando
        float requestTime;
        Vector3 lastStepPosition;
        float stairWalked;

        ulong MyClientId => player != null ? player.OwnerClientId : 0;

        // Peso que o jogador SENTE: nunca menos que 1 (nada fica mais leve que "sem peso").
        // A força de quem está nas duas pontas se soma (a ponta falsa não soma nada). Conta na CarryRules.
        static float FeltWeight(Furniture f) => f.FeltWeight;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            player = GetComponent<NetworkPlayer>();
        }

        void Update()
        {
            WaitForPickUp();
            WaitForHelp();
            KeepHelping();
            UpdateSpeed();
            StairSteps();
        }

        public void TryPickUp(Furniture furniture)
        {
            // Só pega se estiver de mãos livres e o móvel estiver livre.
            if (HasLoad || IsWaiting || furniture == null || furniture.IsHeld) return;

            // Móvel que não "nasceu" na rede (ex.: cena antiga sem NetworkObject) não pode receber RPC.
            if (!furniture.IsSpawned)
            {
                Debug.LogWarning($"{furniture.name} não está na rede. Recrie a cena pelo menu Mudança Assombrada.");
                return;
            }

            requested = furniture;
            requestTime = Time.time;
            furniture.RequestPickUp();
        }

        // Segurar a outra ponta de um móvel que um colega já pegou.
        public void TryHelp(Furniture furniture)
        {
            if (HasLoad || IsWaiting || furniture == null || !furniture.IsSpawned || !furniture.IsHeld) return;
            helpRequested = furniture;
            requestTime = Time.time;
            furniture.RequestHelp();
        }

        void WaitForPickUp()
        {
            if (requested == null) return;

            // O host aceitou: o móvel agora é meu. Para o host, isso é instantâneo.
            if (requested.IsOwner && requested.IsHeld)
            {
                BeginHold(requested);
                requested = null;
            }
            // Sem resposta a tempo (outro jogador pegou antes): desisto.
            else if (Time.time - requestTime > requestTimeout)
            {
                requested = null;
            }
        }

        void WaitForHelp()
        {
            if (helpRequested == null) return;
            if (helpRequested.HelperClientId == MyClientId)
            {
                helping = helpRequested;
                helpRequested = null;
            }
            else if (Time.time - requestTime > requestTimeout)
            {
                helpRequested = null;
            }
        }

        // Na outra ponta: o host me tirou (o dono soltou, a fase reiniciou) ou me afastei demais.
        void KeepHelping()
        {
            if (helping == null) return;
            if (!helping.IsHeld || helping.HelperClientId != MyClientId) { helping = null; return; }
            Vector3 d = helping.transform.position - transform.position;
            d.y = 0;
            if (d.magnitude > reach * 1.5f) Drop();
        }

        // A velocidade de quem carrega: o peso dividido pela força da dupla, no passo do mais lento.
        // Arrastando, bem devagar. Móvel de dupla na escada, mais devagar ainda.
        void UpdateSpeed()
        {
            var f = held != null ? held : helping;
            if (f == null || !f.IsSpawned) { movement.SpeedMultiplier = 1f; return; }

            // Arrastando: BEM devagar, e mais devagar quanto mais pesado for para quem arrasta.
            // (Depois, arrastar também vai gastar estamina.)
            bool dragging = held != null && !held.CanLift;
            movement.SpeedMultiplier = CarryRules.CarrySpeed(dragging, dragSpeed, f.CrewSpeed, movement.CharacterSpeed,
                                                             FeltWeight(f), f.IsBulky, movement.OnStairs, stairsSlowdown);
        }

        // Móvel de dupla na escada: cada degrau range alto (o fantasma e a Cida ouvem a mudança descendo).
        void StairSteps()
        {
            var f = held != null ? held : helping;
            Vector3 here = transform.position;
            Vector3 delta = here - lastStepPosition;
            lastStepPosition = here;
            if (f == null || !f.IsBulky || !movement.OnStairs) { stairWalked = 0; return; }

            delta.y = 0;
            stairWalked += delta.magnitude;
            if (stairWalked < stairStepEvery) return;
            stairWalked = 0;
            EchoSystem.Emit(new SoundEvent(movement.Feet, stairStepRange, 1f, SoundType.Footstep, gameObject));
        }

        void BeginHold(Furniture furniture)
        {
            held = furniture;
            held.NoiseMultiplier = CarryNoise;
            held.Body.useGravity = false;
            held.Body.linearDamping = 10f; // amortece para não ficar balançando
        }

        // Solta o que estiver na mão (a primeira ponta ou a outra).
        public void Drop()
        {
            if (helping != null)
            {
                helping.RequestLeaveHelp();
                helping = null;
            }
            if (held == null) return;
            // Devolve a física ao normal ANTES de devolver a posse ao host.
            held.Body.useGravity = true;
            held.Body.linearDamping = 0f;
            held.NoiseMultiplier = 1f;
            movement.SpeedMultiplier = 1f;
            held.RequestDrop();
            held = null;
        }

        // FixedUpdate roda no ritmo da física (50x por segundo por padrão).
        // Tudo que mexe em Rigidbody deve ficar aqui, e não no Update.
        void FixedUpdate()
        {
            if (held == null) return;

            // O host tomou o móvel de volta (ex.: a fase reiniciou): só largo localmente,
            // sem pedir nada ao host, porque o móvel já não é meu.
            if (!held.IsOwner)
            {
                movement.SpeedMultiplier = 1f;
                held.NoiseMultiplier = 1f;
                held = null;
                return;
            }

            Vector3 target = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            // Com alguém na outra ponta, o móvel fica no meio das duas mãos (a posição do colega
            // chega pela rede). A ponta falsa não segura nada: o móvel não sai do lugar por ela.
            var other = held.Helper;
            if (other != null)
            {
                Vector3 hand = other.transform.position + other.transform.forward * holdDistance * 0.6f + Vector3.up * 1.1f;
                target = (target + hand) / 2f;
            }
            Vector3 toTarget = target - held.Body.position;

            // Em vez de "teletransportar" o móvel, damos velocidade na direção do alvo.
            // Assim ele ainda colide com paredes e portas, igual a carregar de verdade.
            Vector3 velocity = toTarget * followStrength / FeltWeight(held);
            bool lifting = held.CanLift;
            held.Body.useGravity = !lifting;
            if (!lifting) velocity.y = held.Body.linearVelocity.y; // arrastando: fica no chão
            held.Body.linearVelocity = velocity;

            // Se prender em algo e ficar longe demais, você solta. (Arrastando, o móvel está no chão
            // e a mão no alto: só conta a distância no plano.)
            if (!lifting) toTarget.y = 0;
            if (toTarget.magnitude > reach * 1.5f) Drop();
        }
    }
}
