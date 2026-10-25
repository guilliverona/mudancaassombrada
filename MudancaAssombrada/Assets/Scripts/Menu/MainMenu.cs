using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// O menu do jogo: 4 painéis, e só um aparece por vez.
//
//   Inicial ──Jogar──> Conexão ──(conectou)──> Sala de espera ──(host: Começar)──> Fase
//      └──Opções──> Opções
//
// Os botões, campos e textos foram criados pelo construtor de cena (uGUI / Canvas)
// e ligados aqui pelo Inspector. Este script só diz O QUE cada botão faz.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject networkManagerPrefab;

    [Header("Painéis")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject connectPanel;
    [SerializeField] GameObject lobbyPanel;
    [SerializeField] OptionsPanel options; // o mesmo painel de Opções usado na pausa

    [Header("Inicial")]
    [SerializeField] Button playButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button quitButton;

    [Header("Conexão")]
    [SerializeField] InputField nameInput;
    [SerializeField] InputField addressInput;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button connectBackButton;
    [SerializeField] Text statusText;

    [Header("Sala de espera")]
    [SerializeField] Text playerListText;
    [SerializeField] Button[] characterButtons; // na ordem do enum: Cida, Tiago, Marcelo
    [SerializeField] Button startButton;
    [SerializeField] Button leaveButton;
    [SerializeField] Text lobbyInfoText;

    GameObject optionsPanel => options.gameObject;

    NetworkSession Session => NetworkManager.Singleton.GetComponent<NetworkSession>();

    void Awake()
    {
        // O NetworkManager sobrevive às trocas de cena. Na PRIMEIRA vez que o menu abre ele
        // ainda não existe, então criamos a partir do prefab. Ao voltar da fase, ele já existe,
        // e criar outro daria dois NetworkManagers brigando.
        if (NetworkManager.Singleton == null) Instantiate(networkManagerPrefab);

        // Voltando de uma partida: o jogo pode estar congelado (game over) e o mouse preso.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameSettings.Muted = false; // se jogou de Tiago, o menu volta a ter som
        GameSettings.Apply();
    }

    void Start()
    {
        // AddListener: "quando clicar, chame esta função". O "() =>" cria uma função na hora.
        playButton.onClick.AddListener(() => Show(connectPanel));
        optionsButton.onClick.AddListener(() => Show(optionsPanel));
        quitButton.onClick.AddListener(Quit);

        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        connectBackButton.onClick.AddListener(() => Show(mainPanel));

        for (int i = 0; i < characterButtons.Length; i++)
        {
            var character = (CharacterSwitcher.Character)i; // cópia local: cada botão guarda a SUA
            characterButtons[i].onClick.AddListener(() => LobbyManager.Instance.RequestCharacter(character));
        }
        startButton.onClick.AddListener(() => LobbyManager.Instance.StartGame());
        leaveButton.onClick.AddListener(() => { Session.Leave(); Show(connectPanel); });

        options.Closed += () => Show(mainPanel);

        nameInput.text = GameSettings.PlayerName;
        addressInput.text = GameSettings.HostAddress;

        LobbyManager.Instance.Changed += RefreshLobby;
        NetworkSession.Disconnected += ShowError;

        // Voltou para o menu por causa de um erro (ex.: o host saiu)? Mostra o motivo.
        if (NetworkSession.LastError != null) ShowError();
        else Show(mainPanel);
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance != null) LobbyManager.Instance.Changed -= RefreshLobby;
        NetworkSession.Disconnected -= ShowError;
    }

    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        connectPanel.SetActive(panel == connectPanel);
        lobbyPanel.SetActive(panel == lobbyPanel);
        optionsPanel.SetActive(panel == optionsPanel);
        if (panel == connectPanel) statusText.text = NetworkSession.LastError ?? "";
    }

    void ShowError() => Show(connectPanel); // o Show já coloca o LastError no texto de status

    void SaveConnectionFields()
    {
        string playerName = nameInput.text.Trim();
        GameSettings.PlayerName = playerName.Length > 0 ? playerName : "Carregador";
        GameSettings.HostAddress = addressInput.text.Trim();
        GameSettings.Save();
    }

    void Host()
    {
        SaveConnectionFields();
        Session.Host();
    }

    void Join()
    {
        SaveConnectionFields();
        statusText.text = "Conectando...";
        Session.Join(GameSettings.HostAddress);
    }

    void RefreshLobby()
    {
        var lobby = LobbyManager.Instance;
        if (!lobby.IsSpawned)
        {
            // Saiu da sala (ou ela fechou): se estava na tela da sala, volta para a conexão.
            if (lobbyPanel.activeSelf) Show(connectPanel);
            return;
        }

        if (!lobbyPanel.activeSelf) Show(lobbyPanel); // acabou de conectar

        var net = NetworkManager.Singleton;
        CharacterSwitcher.Character? mine = null;
        string list = "";
        foreach (var p in lobby.Players)
        {
            string color = ColorUtility.ToHtmlStringRGB(CharacterSwitcher.BodyColors[(int)p.Character]);
            string tags = (p.ClientId == NetworkManager.ServerClientId ? " (host)" : "")
                        + (p.ClientId == net.LocalClientId ? " (você)" : "");
            list += $"{p.Name}{tags} — <color=#{color}>{CharacterSwitcher.Names[(int)p.Character]}</color>\n";
            if (p.ClientId == net.LocalClientId) mine = p.Character;
        }
        playerListText.text = $"Jogadores ({lobby.Players.Count}/{NetworkSession.MaxPlayers}):\n{list}";

        for (int i = 0; i < characterButtons.Length; i++)
        {
            var c = (CharacterSwitcher.Character)i;
            bool isMine = mine == c;
            bool taken = lobby.IsTaken(c) && !isMine;
            characterButtons[i].interactable = !taken && !isMine;
            characterButtons[i].GetComponentInChildren<Text>().text =
                CharacterSwitcher.Names[i] + (isMine ? "  ✓" : taken ? "  (ocupado)" : "");
        }

        startButton.gameObject.SetActive(net.IsHost); // só o host começa a partida
        lobbyInfoText.text = net.IsHost
            ? "Quando todos entrarem, clique em Começar."
            : "Esperando o host começar a partida...";
    }

    void Quit()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        // Application.Quit não faz nada dentro do editor; aqui paramos o Play.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
