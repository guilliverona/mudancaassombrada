using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Qual personagem cada jogador está usando (1 = Dona Cida, 2 = Tiago, 3 = Marcelo).
// Agora é um NetworkBehaviour: a escolha de cada jogador é conhecida por TODOS,
// e quem decide se pode ou não é o host (não pode haver duas Cidas na mesma partida).
public class CharacterSwitcher : NetworkBehaviour
{
    public enum Character { Cida, Tiago, Marcelo }

    // Cor do boneco de cada personagem, para todos saberem quem é quem.
    public static readonly Color[] BodyColors =
    {
        new Color(0.6f, 0.4f, 0.9f),  // Cida: roxo
        new Color(1f, 0.8f, 0.2f),    // Tiago: amarelo
        new Color(0.3f, 0.75f, 0.35f) // Marcelo: verde
    };
    public static readonly string[] Names = { "Dona Cida (cega)", "Tiago (surdo)", "Marcelo (mudo)" };
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // NetworkVariable = uma variável que a rede mantém IGUAL em todos os computadores.
    // Permissão de escrita "Server": só o host muda; os clientes só leem (e pedem por RPC).
    readonly NetworkVariable<Character> character = new(
        Character.Cida, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Character Current => character.Value;

    [SerializeField] Renderer[] body; // as partes do boneco que mudam de cor (a "roupa")

    EchoSystem echo;
    VibrationVision vibration;
    CaneTapper cane;
    Whistle whistle;
    PlayerMovement movement;
    FurnitureCarrier carrier;

    void Awake()
    {
        echo = FindFirstObjectByType<EchoSystem>();
        vibration = GetComponent<VibrationVision>();
        cane = GetComponent<CaneTapper>();
        whistle = GetComponent<Whistle>();
        movement = GetComponent<PlayerMovement>();
        carrier = GetComponent<FurnitureCarrier>();
    }

    public override void OnNetworkSpawn()
    {
        // Avise-me quando o valor mudar (em qualquer computador).
        character.OnValueChanged += OnCharacterChanged;

        // O host usa o personagem que o jogador escolheu na sala de espera.
        // (Se não houver escolha, por exemplo testando direto na fase, pega o primeiro livre.)
        if (IsServer)
            character.Value = LobbyManager.ChosenCharacters.TryGetValue(OwnerClientId, out var chosen)
                ? chosen
                : FirstFreeCharacter();

        Apply(character.Value);
    }

    public override void OnNetworkDespawn() => character.OnValueChanged -= OnCharacterChanged;

    void OnCharacterChanged(Character previous, Character current) => Apply(current);

    void Update()
    {
        if (!IsOwner || PauseMenu.IsOpen) return; // só o dono deste boneco pode pedir para trocar

        if (Keyboard.current.digit1Key.wasPressedThisFrame) RequestCharacterRpc(Character.Cida);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) RequestCharacterRpc(Character.Tiago);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) RequestCharacterRpc(Character.Marcelo);
    }

    // RPC = "Remote Procedure Call": chamar um método que roda em OUTRO computador.
    // SendTo.Server: o cliente chama, mas o código abaixo roda no host.
    // Regra do Netcode: o nome do método precisa terminar em "Rpc".
    [Rpc(SendTo.Server)]
    void RequestCharacterRpc(Character wanted)
    {
        if (IsTaken(wanted)) return; // ocupado: o host simplesmente recusa
        character.Value = wanted;    // a rede espalha o novo valor para todos
    }

    bool IsTaken(Character c)
    {
        foreach (var other in FindObjectsByType<CharacterSwitcher>())
            if (other != this && other.IsSpawned && other.character.Value == c) return true;
        return false;
    }

    Character FirstFreeCharacter()
    {
        foreach (Character c in System.Enum.GetValues(typeof(Character)))
            if (!IsTaken(c)) return c;
        return Character.Cida; // 4º jogador em diante: repete (por enquanto)
    }

    void Apply(Character c)
    {
        // TODOS os computadores pintam este boneco com a cor do personagem.
        // MaterialPropertyBlock muda a cor só DESTE objeto, sem criar material novo,
        // e continua valendo mesmo quando a Cida troca os materiais pelos de eco.
        var block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, BodyColors[(int)c]);
        foreach (var part in body)
            part.SetPropertyBlock(block);

        // Se a tela está escura (alguém aqui é a Cida), escurece também este boneco.
        echo.RefreshNewRenderers();

        // Habilidades e visão: só no computador de quem controla este boneco.
        if (IsOwner) Become(c);
    }

    // Cada personagem é só uma combinação diferente dos mesmos componentes ligados/desligados.
    // Isso se chama COMPOSIÇÃO: em vez de uma classe gigante "Tiago" e outra "Cida",
    // juntamos peças pequenas (visão de eco, visão de vibração, bengala, apito) como Lego.
    void Become(Character c)
    {
        bool cida = c == Character.Cida;
        bool tiago = c == Character.Tiago;
        bool marcelo = c == Character.Marcelo;

        echo.SetBlind(cida);           // Cida: tela escura, enxerga pelo som
        cane.CanTap = cida;            // só a Cida tem bengala
        vibration.enabled = tiago;     // Tiago: vê as vibrações no chão
        whistle.enabled = marcelo;     // Marcelo: apito para distrair o fantasma

        // Marcelo: forte (carrega o armário como se fosse uma cadeira), silencioso
        // (passos e batidas de móvel fazem menos barulho), mas mais lento que os outros.
        carrier.Strength = marcelo ? 3f : 1f;
        carrier.CarryNoise = marcelo ? 0.4f : 1f;
        cane.StepVolume = marcelo ? 0.5f : 1f;
        movement.CharacterSpeed = marcelo ? 0.85f : 1f;

        // Tiago é surdo: zera o volume de TODO o jogo. O AudioListener é o "ouvido"
        // da cena (fica na câmera), e AudioListener.volume é o volume geral dele.
        // Para os outros, vale o volume escolhido no menu de Opções.
        // (Fica guardado em GameSettings.Muted para mexer no volume pela pausa não "curar" o Tiago.)
        GameSettings.Muted = tiago;
        GameSettings.Apply();
    }

    void OnGUI()
    {
        GameFonts.ApplyToGUI();
        if (!IsOwner) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperRight, richText = true };
        style.normal.textColor = Color.white;

        string text = $"Você: <b>{Names[(int)Current]}</b>";
        if (Current == Character.Marcelo) text += "\nF: apito";

        // Lista os três, marcando os que outros jogadores já pegaram.
        for (int i = 0; i < Names.Length; i++)
        {
            var c = (Character)i;
            string color = ColorUtility.ToHtmlStringRGB(BodyColors[i]);
            string status = c == Current ? " (você)" : IsTaken(c) ? " (ocupado)" : "";
            text += $"\n<color=#{color}>[{i + 1}] {Names[i]}</color>{status}";
        }
        GUI.Label(new Rect(Screen.width - 420, 20, 400, 140), text, style);
    }
}
