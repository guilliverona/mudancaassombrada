using UnityEngine;

namespace MudancaAssombrada
{
    // Fica no Player. CARREGAR um móvel: segurar, o peso deixar lento, soltar.
    // Quem mira e aperta a tecla é o PlayerInteractor; o móvel (Furniture) pede para ser pego
    // chamando TryPickUp. Assim este script só sabe de peso e física.
    //
    // EM REDE, pegar tem duas fases:
    //   1. Pedir: "host, posso pegar este móvel?" (RequestPickUp)
    //   2. Esperar a resposta: quando o móvel passa a ser MEU (IsOwner), começo a carregar.
    // Se outro jogador pegou antes, o host recusa e o pedido expira sozinho.
    public class FurnitureCarrier : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 2.5f;          // longe assim do alvo, o móvel escapa da mão
        [SerializeField] float holdDistance = 1.8f;   // onde o móvel fica à sua frente
        [SerializeField] float followStrength = 12f;  // quão "firme" você segura
        [SerializeField] float requestTimeout = 1f;   // segundos esperando a resposta do host

        // Força: divide o peso dos móveis (vem da ficha do personagem).
        public float Strength { get; set; } = 1f;
        // Barulho dos móveis batendo enquanto ESTE jogador carrega.
        public float CarryNoise { get; set; } = 1f;

        public bool IsHolding => held != null;
        // Já pedi um móvel e estou esperando o host? (não dá para pedir dois)
        public bool IsWaiting => requested != null;
        public InteractionPrompt DropPrompt => held != null ? new InteractionPrompt($"Soltar {held.name}", true) : InteractionPrompt.None;

        PlayerMovement movement;
        Furniture held;      // null = mãos vazias
        Furniture requested; // pedi para o host e estou esperando
        float requestTime;

        // Peso que o jogador SENTE: nunca menos que 1 (nada fica mais leve que "sem peso").
        float FeltWeight => Mathf.Max(1f, held.Weight / Strength);

        void Awake() => movement = GetComponent<PlayerMovement>();

        void Update() => WaitForPickUp();

        public void TryPickUp(Furniture furniture)
        {
            // Só pega se estiver de mãos livres e o móvel estiver livre.
            if (held != null || requested != null || furniture == null || furniture.IsHeld) return;

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

        void BeginHold(Furniture furniture)
        {
            held = furniture;
            held.NoiseMultiplier = CarryNoise;
            held.Body.useGravity = false;
            held.Body.linearDamping = 10f; // amortece para não ficar balançando
            movement.SpeedMultiplier = 1f / FeltWeight; // mais pesado = mais lento
        }

        public void Drop()
        {
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
            Vector3 toTarget = target - held.Body.position;

            // Em vez de "teletransportar" o móvel, damos velocidade na direção do alvo.
            // Assim ele ainda colide com paredes e portas, igual a carregar de verdade.
            held.Body.linearVelocity = toTarget * followStrength / FeltWeight;

            // Se prender em algo e ficar longe demais, você solta.
            if (toTarget.magnitude > reach * 1.5f) Drop();
        }
    }
}
