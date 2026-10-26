using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // O menu do jogo: 4 painéis, e só um aparece por vez.
    //
    //   Inicial ──Jogar──> Salas online ──(criou/entrou)──> Sala de espera ──(host: Começar)──> Fase
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

        [Header("Salas online")]
        [SerializeField] InputField nameInput;
        [SerializeField] Button createRoomButton;
        [SerializeField] Button refreshButton;
        [SerializeField] Transform roomList;          // onde ficam os botões das salas abertas
        [SerializeField] Button roomButtonTemplate;   // modelo (escondido) copiado para cada sala
        [SerializeField] Text emptyRoomsText;         // "Nenhuma sala aberta..." / "Procurando..."
        [SerializeField] InputField codeInput;
        [SerializeField] Button joinCodeButton;
        [SerializeField] Button connectBackButton;
        [SerializeField] Text statusText;

        [Header("Sala de espera")]
        [SerializeField] Text playerListText;
        [SerializeField] Button chapterPrevButton;
        [SerializeField] Button chapterNextButton;
        [SerializeField] Text chapterText;     // nome do capítulo escolhido
        [SerializeField] Text chapterInfoText; // fantasma, destaque, situação (bloqueado, em produção...)
        [SerializeField] Button[] characterButtons; // um por personagem, na ordem do Elenco
        [SerializeField] Button startButton;
        [SerializeField] Button leaveButton;
        [SerializeField] Text lobbyInfoText;

        GameObject optionsPanel => options.gameObject;

        // A lista de salas se atualiza sozinha a cada poucos segundos enquanto a tela está aberta.
        const float RefreshInterval = 5f;
        public const int MaxRoomsShown = 4; // cabe no painel (o construtor do menu usa para a altura)
        float nextRefresh;
        bool busy;       // criando/entrando numa sala: trava os botões para não clicar duas vezes
        bool refreshing; // já tem uma busca de salas em andamento
        readonly List<GameObject> roomButtons = new();

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
            GameSettings.Muted = false; // se jogou com um personagem surdo, o menu volta a ter som
            GameSettings.Apply();
            LoadingScreen.HideNow(); // voltou ao menu (até por erro): nada de tela de carregamento presa
        }

        void Start()
        {
            // AddListener: "quando clicar, chame esta função". O "() =>" cria uma função na hora.
            playButton.onClick.AddListener(() => Show(connectPanel));
            optionsButton.onClick.AddListener(() => Show(optionsPanel));
            quitButton.onClick.AddListener(Quit);

            createRoomButton.onClick.AddListener(CreateRoom);
            refreshButton.onClick.AddListener(() => nextRefresh = 0); // o Update busca na hora
            joinCodeButton.onClick.AddListener(JoinByCode);
            roomButtonTemplate.gameObject.SetActive(false);
            connectBackButton.onClick.AddListener(() => Show(mainPanel));

            for (int i = 0; i < characterButtons.Length; i++)
            {
                int character = i; // cópia local: cada botão guarda a SUA
                characterButtons[i].onClick.AddListener(() => LobbyManager.Instance.RequestCharacter(character));
            }
            chapterPrevButton.onClick.AddListener(() => StepChapter(-1));
            chapterNextButton.onClick.AddListener(() => StepChapter(+1));
            startButton.onClick.AddListener(() => LobbyManager.Instance.StartGame());
            leaveButton.onClick.AddListener(() => { Session.Leave(); Show(connectPanel); });

            options.Closed += () => Show(mainPanel);

            nameInput.text = GameSettings.PlayerName;

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
            if (panel == connectPanel)
            {
                statusText.text = NetworkSession.LastError ?? "";
                SetBusy(false);
                nextRefresh = 0; // abriu a tela: busca as salas já
            }
        }

        void Update()
        {
            if (connectPanel.activeSelf && !busy && !refreshing && Time.unscaledTime >= nextRefresh)
                RefreshRooms();
        }

        void ShowError() => Show(connectPanel); // o Show já coloca o LastError no texto de status

        void SaveName()
        {
            string playerName = nameInput.text.Trim();
            GameSettings.PlayerName = playerName.Length > 0 ? playerName : "Carregador";
            GameSettings.Save();
        }

        void SetBusy(bool value)
        {
            busy = value;
            createRoomButton.interactable = !value;
            joinCodeButton.interactable = !value;
            refreshButton.interactable = !value;
            foreach (var b in roomButtons) b.GetComponent<Button>().interactable = !value;
        }

        // "async void" = começa a esperar o servidor e devolve o controle ao jogo na hora.
        // Se der certo, o LobbyManager avisa (Changed) e o RefreshLobby abre a sala de espera.
        // Se der errado, o NetworkSession dispara Disconnected e o ShowError mostra o motivo.
        async void CreateRoom()
        {
            SaveName();
            SetBusy(true);
            statusText.text = "Criando sala...";
            await Session.CreateRoom($"Sala de {GameSettings.PlayerName}");
            // Já deixa o código copiado (Ctrl+V no chat dos amigos).
            if (NetworkSession.RoomCode != null) GUIUtility.systemCopyBuffer = NetworkSession.RoomCode;
            // A sala de espera pode ter aberto antes de o código chegar: redesenha para mostrá-lo.
            if (LobbyManager.Instance.IsSpawned) RefreshLobby();
        }

        async void JoinRoom(string roomId)
        {
            SaveName();
            SetBusy(true);
            statusText.text = "Entrando na sala...";
            await Session.JoinRoom(roomId);
        }

        async void JoinByCode()
        {
            string code = codeInput.text.Trim();
            if (code.Length == 0) { statusText.text = "Digite o código da sala."; return; }
            SaveName();
            SetBusy(true);
            statusText.text = "Entrando na sala...";
            await Session.JoinByCode(code);
        }

        async void RefreshRooms()
        {
            refreshing = true;
            if (roomButtons.Count == 0) emptyRoomsText.text = "Procurando salas...";
            List<ISessionInfo> rooms;
            try { rooms = await NetworkSession.ListRooms(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("Não foi possível buscar as salas: " + e.Message);
                rooms = null;
            }
            refreshing = false;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            if (this == null || !connectPanel.activeSelf) return; // o menu fechou enquanto esperava

            foreach (var b in roomButtons) Destroy(b);
            roomButtons.Clear();
            if (rooms == null)
            {
                emptyRoomsText.text = "Sem conexão com os servidores. Tentando de novo...";
                emptyRoomsText.gameObject.SetActive(true);
                return;
            }

            foreach (var room in rooms)
            {
                if (roomButtons.Count >= MaxRoomsShown) break;
                var button = Instantiate(roomButtonTemplate, roomList);
                button.gameObject.SetActive(true);
                int players = room.MaxPlayers - room.AvailableSlots;
                button.GetComponentInChildren<Text>().text = $"{room.Name}  ({players}/{room.MaxPlayers})";
                string id = room.Id; // cópia local: cada botão guarda a SUA sala
                button.onClick.AddListener(() => JoinRoom(id));
                button.interactable = !busy;
                roomButtons.Add(button.gameObject);
            }
            emptyRoomsText.text = "Nenhuma sala aberta. Crie uma ou peça o código a um amigo.";
            emptyRoomsText.gameObject.SetActive(rooms.Count == 0);
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
            var roster = CharacterRoster.Instance;
            int? mine = null;
            string list = "";
            foreach (var p in lobby.Players)
            {
                var character = roster.Get(p.Character);
                string color = ColorUtility.ToHtmlStringRGB(Theme.Muted(character.color));
                string tags = (p.ClientId == NetworkManager.ServerClientId ? " (host)" : "")
                            + (p.ClientId == net.LocalClientId ? " (você)" : "");
                list += $"{p.Name}{tags} — <color=#{color}>{character.FullName}</color>\n";
                if (p.ClientId == net.LocalClientId) mine = p.Character;
            }
            playerListText.text = $"Jogadores ({lobby.Players.Count}/{NetworkSession.MaxPlayers}):\n{list}";

            for (int i = 0; i < characterButtons.Length; i++)
            {
                bool isMine = mine == i;
                bool taken = lobby.IsTaken(i) && !isMine;
                characterButtons[i].interactable = !taken && !isMine;
                characterButtons[i].GetComponentInChildren<Text>().text =
                    roster.Get(i).FullName + (isMine ? "  ✓" : taken ? "  (ocupado)" : "");
            }

            RefreshChapter(lobby, net.IsHost);

            startButton.gameObject.SetActive(net.IsHost); // só o host começa a partida
            startButton.interactable = lobby.CanStart;
            lobbyInfoText.text = net.IsHost
                ? "Quando todos entrarem, clique em Começar."
                : "Esperando o host começar a partida...";
            if (NetworkSession.RoomCode != null)
                lobbyInfoText.text = $"Código da sala: {NetworkSession.RoomCode}\n" + lobbyInfoText.text;
        }

        // O capítulo escolhido: só o host navega (◀ ▶); os outros só veem.
        void RefreshChapter(LobbyManager lobby, bool isHost)
        {
            var campaign = Campaign.Instance;
            int index = lobby.SelectedChapter;
            var chapter = campaign.Get(index);

            chapterPrevButton.gameObject.SetActive(isHost);
            chapterNextButton.gameObject.SetActive(isHost);
            chapterPrevButton.interactable = index > 0;
            chapterNextButton.interactable = index < campaign.Count - 1;

            if (chapter == null)
            {
                chapterText.text = "Nenhum capítulo";
                chapterInfoText.text = "";
                return;
            }

            chapterText.text = chapter.title;
            string info = chapter.ghostName;
            if (chapter.featuredCharacter != null)
                info += (info.Length > 0 ? " · " : "") + "Destaque: " + chapter.featuredCharacter.displayName;
            // A situação vem do save do HOST (é ele quem decide o que dá para jogar).
            if (!chapter.IsPlayable) info += "\nEm produção";
            else if (isHost && !campaign.IsUnlocked(index)) info += "\nBloqueado: termine o capítulo anterior";
            chapterInfoText.text = info;
        }

        void StepChapter(int direction)
        {
            var lobby = LobbyManager.Instance;
            lobby.SelectChapter(lobby.SelectedChapter + direction);
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
}
