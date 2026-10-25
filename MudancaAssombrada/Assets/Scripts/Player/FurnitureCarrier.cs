using UnityEngine;

namespace MudancaAssombrada
{
    // O que a dica de interação deve mostrar agora (a HUD lê isto e desenha).
    // "readonly struct" = um pacotinho de dados que não muda depois de criado.
    public readonly struct InteractionPrompt
    {
        public readonly string Action;   // ex.: "Pegar Armário". null = nenhuma dica
        public readonly bool Available;  // false = aparece apagada (ex.: "Ocupado")

        public InteractionPrompt(string action, bool available)
        {
            Action = action;
            Available = available;
        }

        public bool Visible => Action != null;
        public static InteractionPrompt None => default;
    }

    // Fica no Player. Aperte Interagir (E) olhando para um móvel para pegar; de novo para soltar.
    //
    // EM REDE, pegar tem duas fases:
    //   1. Pedir: "host, posso pegar este móvel?" (RequestPickUp)
    //   2. Esperar a resposta: quando o móvel passa a ser MEU (IsOwner), começo a carregar.
    // Se outro jogador pegou antes, o host recusa e o pedido expira sozinho.
    public class FurnitureCarrier : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 2.5f;          // distância máxima para alcançar
        [SerializeField] float holdDistance = 1.8f;   // onde o móvel fica à sua frente
        [SerializeField] float followStrength = 12f;  // quão "firme" você segura
        [SerializeField] float requestTimeout = 1f;   // segundos esperando a resposta do host

        // Força: divide o peso dos móveis (vem da ficha do personagem).
        public float Strength { get; set; } = 1f;
        // Barulho dos móveis batendo enquanto ESTE jogador carrega.
        public float CarryNoise { get; set; } = 1f;

        // A dica que a HUD deve mostrar neste frame.
        public InteractionPrompt Prompt { get; private set; }

        PlayerMovement movement;
        Furniture held;      // null = mãos vazias
        Furniture requested; // pedi para o host e estou esperando
        float requestTime;

        // O móvel na mira AGORA (null = nada). Atualizado todo frame, para a dica na tela
        // aparecer e sumir na hora em que você olha para o móvel ou desvia o olhar.
        Furniture lookTarget;

        // Peso que o jogador SENTE: nunca menos que 1 (nada fica mais leve que "sem peso").
        float FeltWeight => Mathf.Max(1f, held.Weight / Strength);

        void Awake() => movement = GetComponent<PlayerMovement>();

        void OnDisable() => Prompt = InteractionPrompt.None;

        void Update()
        {
            WaitForPickUp();
            lookTarget = held == null ? FurnitureInSight() : null;
            Prompt = BuildPrompt();

            if (PauseMenu.IsOpen || !GameInput.Interact.WasPressedThisFrame()) return;

            if (held != null) Drop();
            else if (requested == null) TryPickUp();
        }

        InteractionPrompt BuildPrompt()
        {
            if (held != null) return new InteractionPrompt($"Soltar {held.name}", true);
            if (lookTarget == null) return InteractionPrompt.None;
            if (lookTarget.IsHeld) return new InteractionPrompt("Ocupado", false);
            return new InteractionPrompt($"Pegar {lookTarget.name}", true);
        }

        Furniture FurnitureInSight()
        {
            // Raycast: um "raio laser" invisível saindo do centro da câmera.
            // Se bater em algo dentro do alcance, 'hit' diz o que foi.
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach)) return null;

            // Só interessa se o objeto atingido tiver o componente Furniture.
            return hit.collider.TryGetComponent(out Furniture furniture) ? furniture : null;
        }

        void TryPickUp()
        {
            // Só pega se tiver um móvel na mira e ele estiver livre.
            Furniture furniture = lookTarget;
            if (furniture == null || furniture.IsHeld) return;

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

        void Drop()
        {
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
