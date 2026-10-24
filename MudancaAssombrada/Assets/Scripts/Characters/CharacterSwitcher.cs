using UnityEngine;
using UnityEngine.InputSystem;

// Escolhe qual personagem você está jogando (1 = Dona Cida, 2 = Tiago).
// No jogo final cada jogador será um personagem; por enquanto, um só jogador troca entre eles
// para testar as mecânicas. Fica no Player.
public class CharacterSwitcher : MonoBehaviour
{
    public enum Character { Cida, Tiago }

    [SerializeField] Character startingCharacter = Character.Cida;

    public Character Current { get; private set; }

    EchoSystem echo;
    VibrationVision vibration;
    CaneTapper cane;

    void Awake()
    {
        echo = FindFirstObjectByType<EchoSystem>();
        vibration = GetComponent<VibrationVision>();
        cane = GetComponent<CaneTapper>();
    }

    void Start() => Become(startingCharacter);

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) Become(Character.Cida);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) Become(Character.Tiago);
    }

    // Cada personagem é só uma combinação diferente dos mesmos componentes ligados/desligados.
    // Isso se chama COMPOSIÇÃO: em vez de uma classe gigante "Tiago" e outra "Cida",
    // juntamos peças pequenas (visão de eco, visão de vibração, bengala) como Lego.
    void Become(Character character)
    {
        Current = character;
        bool cida = character == Character.Cida;

        echo.SetBlind(cida);           // Cida: tela escura, enxerga pelo som
        cane.CanTap = cida;            // só a Cida tem bengala
        vibration.enabled = !cida;     // Tiago: vê as vibrações no chão

        // Tiago é surdo: zera o volume de TODO o jogo. O AudioListener é o "ouvido"
        // da cena (fica na câmera), e AudioListener.volume é o volume geral dele.
        AudioListener.volume = cida ? 1f : 0f;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperRight };
        style.normal.textColor = Color.white;
        string name = Current == Character.Cida ? "Dona Cida (cega)" : "Tiago (surdo)";
        GUI.Label(new Rect(Screen.width - 420, 20, 400, 60), $"{name}\n[1] Cida   [2] Tiago", style);
    }
}
