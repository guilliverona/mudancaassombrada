using UnityEngine;
using UnityEngine.InputSystem;

// Fica no Player. Aperte E olhando para um móvel para pegar; E de novo para soltar.
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

    // Força: divide o peso dos móveis. Marcelo = 3, então o armário (peso 3) parece peso 1.
    public float Strength { get; set; } = 1f;
    // Barulho dos móveis batendo enquanto ESTE jogador carrega (Marcelo carrega com cuidado).
    public float CarryNoise { get; set; } = 1f;

    PlayerMovement movement;
    Furniture held;      // null = mãos vazias
    Furniture requested; // pedi para o host e estou esperando
    float requestTime;

    // Peso que o jogador SENTE: nunca menos que 1 (nada fica mais leve que "sem peso").
    float FeltWeight => Mathf.Max(1f, held.Weight / Strength);

    void Awake() => movement = GetComponent<PlayerMovement>();

    // O móvel na mira AGORA (null = nada). Atualizado todo frame, para a dica na tela
    // aparecer e sumir na hora em que você olha para o móvel ou desvia o olhar.
    Furniture lookTarget;

    void Update()
    {
        WaitForPickUp();
        lookTarget = held == null ? FurnitureInSight() : null;

        if (PauseMenu.IsOpen || !Keyboard.current.eKey.wasPressedThisFrame) return;

        if (held != null) Drop();
        else if (requested == null) TryPickUp();
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

    // Dica de interação: um quadradinho com a tecla "E" e o que ela faz, logo abaixo da mira.
    // Como este script só fica ligado no MEU boneco, a dica só aparece na minha tela.
    void OnGUI()
    {
        GameFonts.ApplyToGUI();
        float cx = Screen.width / 2f, cy = Screen.height / 2f;

        // Mira: um pontinho no centro, para saber exatamente para onde o raio aponta.
        GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);

        string action;
        bool available = true;
        if (held != null) action = $"Soltar {held.name}";
        else if (lookTarget == null) return; // olhando para nada: sem dica
        else if (lookTarget.IsHeld) { action = "Ocupado"; available = false; }
        else action = $"Pegar {lookTarget.name}";

        // Tecla desenhada como uma "teclinha": fundo escuro com a letra no meio.
        var keyStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        keyStyle.normal.textColor = available ? Color.white : Color.gray;
        var textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
        textStyle.normal.textColor = available ? Color.white : Color.gray;

        const float keySize = 36f, gap = 8f, textWidth = 220f;
        float top = cy + 40;
        float left = cx - (keySize + gap + textWidth) / 2f;
        GUI.Box(new Rect(left, top, keySize, keySize), available ? "E" : "—", keyStyle);
        GUI.Label(new Rect(left + keySize + gap, top, textWidth, keySize), action, textStyle);
    }
}
