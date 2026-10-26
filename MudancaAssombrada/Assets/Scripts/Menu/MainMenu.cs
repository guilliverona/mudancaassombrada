using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // O menu do jogo: 4 painéis, e só um aparece por vez.
    //
    //   Inicial ──Jogar──> Salas online ──(criou/entrou)──> Sala de espera ──(líder: Começar)──> Fase
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
        [SerializeField] Text playerListText;           // título "Jogadores (2/3)"
        [SerializeField] Transform playerRows;          // onde ficam as linhas dos jogadores
        [SerializeField] GameObject playerRowTemplate;  // modelo (escondido): [info] [Líder] [Expulsar]
        [SerializeField] Button chapterPrevButton;
        [SerializeField] Button chapterNextButton;
        [SerializeField] Text chapterText;     // nome do capítulo escolhido
        [SerializeField] Text chapterInfoText; // fantasma, destaque, situação (bloqueado, em produção...)
        [SerializeField] Button[] characterButtons; // um por personagem, na ordem do Elenco
        [SerializeField] Button startButton;   // só o líder vê (vira "Cancelar" durante a contagem)
        [SerializeField] Button readyButton;   // os outros veem: "Estou pronto" / "Não estou pronto"
        [SerializeField] Button leaveButton;
        [SerializeField] Button privacyButton; // líder: sala pública <-> privada
        [SerializeField] Button copyCodeButton;
        [SerializeField] Text lobbyInfoText;

        [Header("Chat")]
        [SerializeField] Text chatText;
        [SerializeField] InputField chatInput;
        [SerializeField] Button chatSendButton;

        GameObject optionsPanel => options.gameObject;

        // A lista de salas se atualiza sozinha a cada poucos segundos enquanto a tela está aberta.
        const float RefreshInterval = 5f;
        public const int MaxRoomsShown = 4; // cabe no painel (o construtor do menu usa para a altura)
        float nextRefresh;
        bool busy;       // criando/entrando numa sala: trava os botões para não clicar duas vezes
        bool refreshing; // já tem uma busca de salas em andamento
        readonly List<GameObject> roomButtons = new();
        readonly List<GameObject> rowObjects = new(); // linhas dos jogadores (cópias do modelo)
        float copiedUntil; // até quando mostrar "Código copiado!"

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
            startButton.onClick.AddListener(StartOrCancel);
            readyButton.onClick.AddListener(ToggleReady);
            leaveButton.onClick.AddListener(() => { Session.Leave(); Show(connectPanel); });
            privacyButton.onClick.AddListener(() => LobbyManager.Instance.SetPrivate(!LobbyManager.Instance.IsPrivate));
            copyCodeButton.onClick.AddListener(CopyCode);
            playerRowTemplate.SetActive(false);

            chatSendButton.onClick.AddListener(SendChat);
            chatInput.onSubmit.AddListener(_ => SendChat()); // Enter também envia

            options.Closed += () => Show(mainPanel);

            nameInput.text = GameSettings.PlayerName;

            LobbyManager.Instance.Changed += RefreshLobby;
            LobbyManager.Instance.ChatChanged += RefreshChat;
            NetworkSession.Disconnected += ShowError;

            // Voltou para o menu por causa de um erro (ex.: o host saiu)? Mostra o motivo.
            if (NetworkSession.LastError != null) ShowError();
            else Show(mainPanel);
        }

        void OnDestroy()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.Changed -= RefreshLobby;
                LobbyManager.Instance.ChatChanged -= RefreshChat;
            }
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
            // A contagem muda a cada quadro: só o texto de baixo é redesenhado.
            if (lobbyPanel.activeSelf && LobbyManager.Instance.IsSpawned) RefreshInfo(LobbyManager.Instance);
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

            if (!lobbyPanel.activeSelf)
            {
                Show(lobbyPanel); // acabou de conectar
                chatInput.text = "";
                RefreshChat();
            }

            var net = NetworkManager.Singleton;
            var players = lobby.Players;
            bool amLeader = lobby.AmLeader;
            bool counting = lobby.Countdown != null;
            LobbyPlayer? me = null;
            foreach (var p in players)
                if (p.ClientId == net.LocalClientId) me = p;

            playerListText.text = $"Jogadores ({players.Count}/{NetworkSession.MaxPlayers}):";
            RefreshPlayerRows(lobby, players, amLeader, counting);

            // Personagens: travados enquanto estou pronto (desmarque para trocar) ou durante a contagem.
            var roster = CharacterRoster.Instance;
            bool locked = me == null || me.Value.Ready || counting;
            for (int i = 0; i < characterButtons.Length; i++)
            {
                bool isMine = me != null && me.Value.Character == i;
                bool taken = lobby.IsTaken(i) && !isMine;
                characterButtons[i].interactable = !taken && !isMine && !locked;
                characterButtons[i].GetComponentInChildren<Text>().text =
                    roster.Get(i).FullName + (isMine ? "  ✓" : taken ? "  (ocupado)" : "");
            }

            RefreshChapter(lobby, amLeader && !counting);

            // Líder: [Começar] (ou [Cancelar] durante a contagem). Os outros: [Estou pronto].
            startButton.gameObject.SetActive(amLeader);
            startButton.interactable = counting || lobby.CanStart;
            startButton.GetComponentInChildren<Text>().text = counting ? "Cancelar" : "Começar";
            readyButton.gameObject.SetActive(!amLeader);
            readyButton.interactable = !counting;
            readyButton.GetComponentInChildren<Text>().text =
                me != null && me.Value.Ready ? "Não estou pronto" : "Estou pronto";

            privacyButton.interactable = amLeader;
            privacyButton.GetComponentInChildren<Text>().text = lobby.IsPrivate ? "Sala privada" : "Sala pública";
            copyCodeButton.interactable = NetworkSession.RoomCode != null;

            RefreshInfo(lobby);
        }

        // Uma linha por jogador, refeita a cada mudança (são no máximo 3: não vale complicar).
        void RefreshPlayerRows(LobbyManager lobby, IReadOnlyList<LobbyPlayer> players, bool amLeader, bool counting)
        {
            foreach (var row in rowObjects) Destroy(row);
            rowObjects.Clear();

            var net = NetworkManager.Singleton;
            var roster = CharacterRoster.Instance;
            foreach (var p in players)
            {
                var row = Instantiate(playerRowTemplate, playerRows);
                row.SetActive(true);
                rowObjects.Add(row);

                var character = roster.Get(p.Character);
                string color = ColorUtility.ToHtmlStringRGB(Theme.Muted(character.color));
                bool isMe = p.ClientId == net.LocalClientId;
                bool isLeader = lobby.IsLeader(p.ClientId);
                string tags = (isLeader ? " [líder]" : "") + (isMe ? " (você)" : "");
                // O líder não marca "pronto": ele é quem começa.
                string ready = isLeader ? "" : p.Ready ? "   <color=#8fbf7f>✓ pronto</color>" : "   esperando...";
                string name = p.Name.ToString().Replace("<", "‹"); // o nome é do jogador: nada de rich text
                row.GetComponentInChildren<Text>().text =
                    $"{name}{tags} — <color=#{color}>{character.FullName}</color>{ready}";

                // Os dois botões da linha, na ordem do modelo: [Líder] [Expulsar].
                var buttons = row.GetComponentsInChildren<Button>(true);
                ulong id = p.ClientId; // cópia local: cada botão guarda o SEU jogador
                buttons[0].gameObject.SetActive(amLeader && !isMe && !counting);
                buttons[0].onClick.AddListener(() => lobby.MakeLeader(id));
                // O host não pode ser expulso: sem ele, a partida acaba para todos.
                buttons[1].gameObject.SetActive(amLeader && !isMe && p.ClientId != NetworkManager.ServerClientId);
                buttons[1].onClick.AddListener(() => lobby.Kick(id));
            }
        }

        // O texto de baixo: código da sala, contagem e o que falta para começar.
        void RefreshInfo(LobbyManager lobby)
        {
            string info;
            float? countdown = lobby.Countdown;
            if (countdown != null)
                info = $"Começando em {Mathf.CeilToInt(countdown.Value)}...";
            else if (!lobby.ChapterPlayable)
                info = "Este capítulo ainda não pode ser jogado.";
            else if (!lobby.AllReady)
                info = lobby.AmLeader ? "Esperando todos marcarem \"pronto\"." : "Marque \"Estou pronto\" quando quiser começar.";
            else
                info = lobby.AmLeader ? "Todos prontos! Clique em Começar." : "Esperando o líder começar a partida...";

            if (Time.unscaledTime < copiedUntil) info = "Código copiado! Cole no chat dos seus amigos.";
            if (NetworkSession.RoomCode != null) info = $"Código da sala: {NetworkSession.RoomCode}\n{info}";
            lobbyInfoText.text = info;
        }

        void RefreshChat()
        {
            var lobby = LobbyManager.Instance;
            chatText.text = lobby == null ? "" : string.Join("\n", lobby.Chat);
        }

        void SendChat()
        {
            if (!LobbyManager.Instance.IsSpawned) return;
            LobbyManager.Instance.SendChat(chatInput.text);
            chatInput.text = "";
            chatInput.ActivateInputField(); // continua digitando sem clicar de novo
        }

        void StartOrCancel()
        {
            var lobby = LobbyManager.Instance;
            if (lobby.Countdown != null) lobby.CancelStart();
            else lobby.RequestStart();
        }

        void ToggleReady()
        {
            var lobby = LobbyManager.Instance;
            foreach (var p in lobby.Players)
                if (p.ClientId == NetworkManager.Singleton.LocalClientId) lobby.SetReady(!p.Ready);
        }

        void CopyCode()
        {
            if (NetworkSession.RoomCode == null) return;
            GUIUtility.systemCopyBuffer = NetworkSession.RoomCode;
            copiedUntil = Time.unscaledTime + 2f; // o RefreshInfo mostra o aviso por 2 segundos
        }

        // O capítulo escolhido: só o líder navega (◀ ▶); os outros só veem.
        void RefreshChapter(LobbyManager lobby, bool canChange)
        {
            var campaign = Campaign.Instance;
            int index = lobby.SelectedChapter;
            var chapter = campaign.Get(index);

            chapterPrevButton.gameObject.SetActive(canChange);
            chapterNextButton.gameObject.SetActive(canChange);
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
            else if (NetworkManager.Singleton.IsHost && !campaign.IsUnlocked(index)) info += "\nBloqueado: termine o capítulo anterior";
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
