using UnityEngine;
using UnityEngine.InputSystem;

// Escolhe qual personagem você está jogando (1 = Dona Cida, 2 = Tiago, 3 = Marcelo).
// No jogo final cada jogador será um personagem; por enquanto, um só jogador troca entre eles
// para testar as mecânicas. Fica no Player.
public class CharacterSwitcher : MonoBehaviour
{
    public enum Character { Cida, Tiago, Marcelo }

    [SerializeField] Character startingCharacter = Character.Cida;

    public Character Current { get; private set; }

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

    void Start() => Become(startingCharacter);

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) Become(Character.Cida);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) Become(Character.Tiago);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) Become(Character.Marcelo);
    }

    // Cada personagem é só uma combinação diferente dos mesmos componentes ligados/desligados.
    // Isso se chama COMPOSIÇÃO: em vez de uma classe gigante "Tiago" e outra "Cida",
    // juntamos peças pequenas (visão de eco, visão de vibração, bengala, apito) como Lego.
    void Become(Character character)
    {
        Current = character;
        bool cida = character == Character.Cida;
        bool tiago = character == Character.Tiago;
        bool marcelo = character == Character.Marcelo;

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
        AudioListener.volume = tiago ? 0f : 1f;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperRight };
        style.normal.textColor = Color.white;
        string name = Current switch
        {
            Character.Cida => "Dona Cida (cega)",
            Character.Tiago => "Tiago (surdo)",
            _ => "Marcelo (mudo) — F: apito",
        };
        GUI.Label(new Rect(Screen.width - 420, 20, 400, 60), $"{name}\n[1] Cida   [2] Tiago   [3] Marcelo", style);
    }
}
