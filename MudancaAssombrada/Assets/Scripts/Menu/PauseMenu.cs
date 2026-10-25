using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menu de pausa da fase (Esc): Continuar, Opções e Voltar ao menu.
//
// IMPORTANTE: em multiplayer não dá para "parar o tempo" (os outros continuam jogando!).
// Então a pausa só abre o menu, solta o mouse e trava os SEUS controles.
// Os scripts de controle perguntam "PauseMenu.IsOpen?" antes de ler o teclado.
public class PauseMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject pausePanel;
    [SerializeField] OptionsPanel options;
    [SerializeField] Button resumeButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button leaveButton;
    [SerializeField] Text infoText;

    void Awake()
    {
        IsOpen = false;
        pausePanel.SetActive(false);
        options.gameObject.SetActive(false);
    }

    void Start()
    {
        resumeButton.onClick.AddListener(Close);
        optionsButton.onClick.AddListener(() => Show(options.gameObject));
        leaveButton.onClick.AddListener(Leave);
        options.Closed += () => Show(pausePanel);
    }

    void OnDestroy() => IsOpen = false; // saiu da fase: ninguém fica "pausado" sem querer

    void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (!IsOpen) Open();
        else if (options.gameObject.activeSelf) Show(pausePanel); // Esc nas Opções = voltar
        else Close();
    }

    void Open()
    {
        IsOpen = true;
        Show(pausePanel);
        Cursor.lockState = CursorLockMode.None; // solta o mouse para clicar
        Cursor.visible = true;

        var net = NetworkManager.Singleton;
        infoText.text = net != null && net.IsHost
            ? "Você é o host: sair encerra a partida para todos."
            : "O jogo continua para os outros enquanto este menu está aberto.";
    }

    void Close()
    {
        IsOpen = false;
        Show(null);
        Cursor.lockState = CursorLockMode.Locked; // volta a prender o mouse para mirar
    }

    void Show(GameObject panel)
    {
        pausePanel.SetActive(panel == pausePanel);
        options.gameObject.SetActive(panel == options.gameObject);
    }

    void Leave()
    {
        IsOpen = false;
        NetworkManager.Singleton.GetComponent<NetworkSession>().ReturnToMenu();
    }
}
