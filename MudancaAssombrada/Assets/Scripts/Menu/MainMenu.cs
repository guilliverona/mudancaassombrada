using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // O menu do jogo: 5 telas, e só uma aparece por vez.
    //
    //   Inicial ──Criar sala──────> Criar sala ──(criou)──┐
    //      │                                              ├──> Sala de espera ──(líder: Começar)──> Fase
    //      ├──Entrar numa sala──> Entrar numa sala ─(entrou)┘   (lista de salas, busca, páginas e código)
    //      └──Opções──> Opções
    //
    // Os botões, campos e textos foram criados pelo construtor de cena (uGUI / Canvas)
    // e ligados aqui pelo Inspector. Este script só diz O QUE cada botão faz.
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject networkManagerPrefab;

        [Header("Painéis")]
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject createPanel;  // "Criar sala"
        [SerializeField] GameObject connectPanel; // "Entrar numa sala"
        [SerializeField] GameObject lobbyPanel;
        [SerializeField] OptionsPanel options; // o mesmo painel de Opções usado na pausa

        [Header("Enfeites")]
        [SerializeField] GameObject smallTitle; // título pequeno no topo das outras telas (a inicial tem o grande)
        [SerializeField] Text menuNoteText;     // bilhete do Seu Valdemar na tela inicial
        [SerializeField] Text createNoteText;   // o da tela Criar sala
        [SerializeField] Text roomsNoteText;    // e o da tela Entrar numa sala

        [Header("Inicial")]
        [SerializeField] Button newRoomButton;  // "Criar sala"
        [SerializeField] Button findRoomButton; // "Entrar numa sala"
        [SerializeField] Button optionsButton;
        [SerializeField] Button quitButton;

        [Header("Criar sala")]
        [SerializeField] InputField nameInput;
        [SerializeField] Button publicButton;   // as duas opções de "Quem pode entrar"
        [SerializeField] Button privateButton;
        [SerializeField] Button createRoomButton;
        [SerializeField] Text createStatusText;
        [SerializeField] Button createBackButton;

        [Header("Entrar numa sala")]
        [SerializeField] InputField browserNameInput; // o mesmo nome, no "crachá" pequeno do topo
        [SerializeField] InputField searchInput;
        [SerializeField] Button refreshButton;
        [SerializeField] RoomRow[] roomRows;          // uma página de linhas (MaxRoomsShown)
        [SerializeField] Text emptyRoomsText;         // "Nenhuma sala aberta..." / "Sem conexão..."
        [SerializeField] Button prevPageButton;
        [SerializeField] Button nextPageButton;
        [SerializeField] Text pageText;
        [SerializeField] InputField codeInput;
        [SerializeField] Button pasteCodeButton;
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
        public const int MaxRoomsShown = 4; // salas por página (o construtor do menu cria uma linha para cada)
        float nextRefresh;
        bool busy;       // criando/entrando numa sala: trava os botões para não clicar duas vezes
        bool refreshing; // já tem uma busca de salas em andamento
        readonly List<RoomListing> rooms = new(); // a última resposta do servidor (todas as páginas)
        bool roomsLoaded;  // já chegou alguma resposta desde que a tela abriu?
        bool roomsFailed;  // a última busca falhou (sem internet, serviço fora...)
        int page;          // página atual da lista (0 = primeira)
        readonly string[] rowRoomIds = new string[MaxRoomsShown]; // a sala de cada linha visível
        readonly List<Color> dotColors = new();
        bool createPrivate;      // "Quem pode entrar" na tela Criar sala
        GameObject attemptPanel; // de que tela saiu o último criar/entrar (o erro volta para ela)
        readonly List<GameObject> rowObjects = new(); // linhas dos jogadores (cópias do modelo)
        float copiedUntil; // até quando mostrar "Código copiado!"

        // O último bilhete de cada tela: "static" sobrevive à volta da partida, para não repetir.
        static int lastMenuNote = -1, lastRoomsNote = -1;
        static readonly System.Random noteRandom = new();

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
            newRoomButton.onClick.AddListener(() => Show(createPanel));
            findRoomButton.onClick.AddListener(() => Show(connectPanel));
            optionsButton.onClick.AddListener(() => Show(optionsPanel));
            quitButton.onClick.AddListener(Quit);

            // Criar sala
            publicButton.onClick.AddListener(() => SetCreatePrivate(false));
            privateButton.onClick.AddListener(() => SetCreatePrivate(true));
            createRoomButton.onClick.AddListener(CreateRoom);
            createBackButton.onClick.AddListener(() => Show(mainPanel));

            // Entrar numa sala
            refreshButton.onClick.AddListener(() => nextRefresh = 0); // o Update busca na hora
            searchInput.onValueChanged.AddListener(_ => { page = 0; RenderRooms(); }); // filtra sem ir ao servidor
            prevPageButton.onClick.AddListener(() => { page--; RenderRooms(); });
            nextPageButton.onClick.AddListener(() => { page++; RenderRooms(); });
            for (int i = 0; i < roomRows.Length; i++)
            {
                int row = i; // cópia local: cada botão guarda a SUA linha
                roomRows[i].JoinButton.onClick.AddListener(() => JoinRoom(rowRoomIds[row]));
            }
            pasteCodeButton.onClick.AddListener(() => codeInput.text = RoomBrowserRules.CleanCode(GUIUtility.systemCopyBuffer));
            codeInput.onSubmit.AddListener(_ => JoinByCode()); // Enter também entra
            joinCodeButton.onClick.AddListener(JoinByCode);
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
            leaveButton.onClick.AddListener(() => { Session.Leave(); Show(mainPanel); });
            privacyButton.onClick.AddListener(() => LobbyManager.Instance.SetPrivate(!LobbyManager.Instance.IsPrivate));
            copyCodeButton.onClick.AddListener(CopyCode);
            playerRowTemplate.SetActive(false);

            chatSendButton.onClick.AddListener(SendChat);
            chatInput.onSubmit.AddListener(_ => SendChat()); // Enter também envia

            options.Closed += () => Show(mainPanel);

            PickNotes();

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
            createPanel.SetActive(panel == createPanel);
            connectPanel.SetActive(panel == connectPanel);
            lobbyPanel.SetActive(panel == lobbyPanel);
            optionsPanel.SetActive(panel == optionsPanel);
            if (smallTitle != null) smallTitle.SetActive(panel != mainPanel);
            // O erro da última tentativa aparece na tela de onde ela saiu.
            string error = NetworkSession.LastError ?? "";
            if (panel == createPanel)
            {
                createStatusText.text = attemptPanel == createPanel ? error : "";
                nameInput.text = GameSettings.PlayerName;
                SetCreatePrivate(createPrivate);
                SetBusy(false);
            }
            if (panel == connectPanel)
            {
                statusText.text = attemptPanel != createPanel ? error : "";
                browserNameInput.text = GameSettings.PlayerName;
                SetBusy(false);
                roomsLoaded = false; // até a resposta chegar, a lista fica vazia (sem "Procurando..." piscando)
                page = 0;
                RenderRooms();
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

        // Sorteia os bilhetes do Seu Valdemar (a regra mora no ValdemarNotes, que tem testes).
        void PickNotes()
        {
            lastMenuNote = ValdemarNotes.Pick(ValdemarNotes.Menu.Length, lastMenuNote, noteRandom);
            lastRoomsNote = ValdemarNotes.Pick(ValdemarNotes.Rooms.Length, lastRoomsNote, noteRandom);
            if (menuNoteText != null && lastMenuNote >= 0) menuNoteText.text = ValdemarNotes.Menu[lastMenuNote];
            if (roomsNoteText != null && lastRoomsNote >= 0) roomsNoteText.text = ValdemarNotes.Rooms[lastRoomsNote];
            int create = ValdemarNotes.Pick(ValdemarNotes.Create.Length, -1, noteRandom);
            if (createNoteText != null && create >= 0) createNoteText.text = ValdemarNotes.Create[create];
        }

        // Volta para a tela de onde saiu a tentativa (o Show já coloca o LastError no texto de status).
        void ShowError() => Show(attemptPanel != null ? attemptPanel : connectPanel);

        void SetCreatePrivate(bool value)
        {
            createPrivate = value;
            publicButton.GetComponent<ButtonHover>().Selected = !value;
            privateButton.GetComponent<ButtonHover>().Selected = value;
        }

        // O nome vem do campo da tela em que o jogador está (os dois mostram o mesmo nome).
        void SaveName(InputField field)
        {
            string playerName = field.text.Trim();
            GameSettings.PlayerName = playerName.Length > 0 ? playerName : "Carregador";
            GameSettings.Save();
        }

        void SetBusy(bool value)
        {
            busy = value;
            createRoomButton.interactable = !value;
            publicButton.interactable = !value;
            privateButton.interactable = !value;
            joinCodeButton.interactable = !value;
            pasteCodeButton.interactable = !value;
            refreshButton.interactable = !value;
            RenderRooms(); // os ENTRAR das linhas também travam
        }

        // "async void" = começa a esperar o servidor e devolve o controle ao jogo na hora.
        // Se der certo, o LobbyManager avisa (Changed) e o RefreshLobby abre a sala de espera.
        // Se der errado, o NetworkSession dispara Disconnected e o ShowError mostra o motivo.
        async void CreateRoom()
        {
            SaveName(nameInput);
            attemptPanel = createPanel;
            SetBusy(true);
            createStatusText.text = "Criando sala...";
            await Session.CreateRoom($"Sala de {GameSettings.PlayerName}", createPrivate);
            // Já deixa o código copiado (Ctrl+V no chat dos amigos).
            if (NetworkSession.RoomCode != null) GUIUtility.systemCopyBuffer = NetworkSession.RoomCode;
            // A sala de espera pode ter aberto antes de o código chegar: redesenha para mostrá-lo.
            if (LobbyManager.Instance.IsSpawned) RefreshLobby();
        }

        async void JoinRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;
            SaveName(browserNameInput);
            attemptPanel = connectPanel;
            SetBusy(true);
            statusText.text = "Entrando na sala...";
            await Session.JoinRoom(roomId);
        }

        async void JoinByCode()
        {
            if (busy) return;
            string code = RoomBrowserRules.CleanCode(codeInput.text);
            if (code.Length == 0) { statusText.text = "Digite o código da sala."; return; }
            SaveName(browserNameInput);
            attemptPanel = connectPanel;
            SetBusy(true);
            statusText.text = "Entrando na sala...";
            await Session.JoinByCode(code);
        }

        async void RefreshRooms()
        {
            // Sem aviso de "Procurando...": a busca roda a cada poucos segundos e o aviso ficava piscando.
            // A lista (ou a mensagem de vazia) só muda quando a resposta chega.
            refreshing = true;
            List<RoomListing> result;
            try { result = await NetworkSession.ListRooms(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("Não foi possível buscar as salas: " + e.Message);
                result = null;
            }
            refreshing = false;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            if (this == null || !connectPanel.activeSelf) return; // o menu fechou enquanto esperava

            roomsLoaded = true;
            roomsFailed = result == null;
            rooms.Clear();
            if (result != null) rooms.AddRange(result);
            RenderRooms();
        }

        // Desenha a página atual a partir da última resposta (sem ir ao servidor): usado pela busca,
        // pelas setas de página e quando a resposta chega. As linhas são sempre as mesmas 4.
        void RenderRooms()
        {
            var filtered = RoomBrowserRules.Filter(rooms, searchInput.text);
            page = RoomBrowserRules.ClampPage(page, filtered.Count, MaxRoomsShown);
            var visible = RoomBrowserRules.Page(filtered, page, MaxRoomsShown);

            var campaign = Campaign.Instance;
            var roster = CharacterRoster.Instance;
            for (int i = 0; i < roomRows.Length; i++)
            {
                if (i >= visible.Count) { roomRows[i].Hide(); rowRoomIds[i] = null; continue; }
                var room = visible[i];
                rowRoomIds[i] = room.Id;

                // "Líder Zé   Capítulo 1 · Casa do Espelho" (o que o host ainda não publicou fica de fora).
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(room.Leader)) parts.Add($"Líder <b>{Safe(room.Leader)}</b>");
                var chapter = campaign.Get(room.Chapter);
                if (room.IsFull) parts.Add("Caminhão cheio");
                else if (chapter != null) parts.Add($"Capítulo <b>{room.Chapter + 1} · {chapter.title}</b>");

                dotColors.Clear();
                foreach (int c in room.Characters)
                    if (c < roster.Count) dotColors.Add(roster.Get(c).color);

                roomRows[i].Show(Safe(room.Name), string.Join("     ", parts), $"{room.Players}/{room.MaxPlayers}", dotColors, room.IsFull, busy);
            }

            string empty = "";
            if (!roomsLoaded) empty = "";
            else if (roomsFailed) empty = "Sem conexão com os servidores. Tentando de novo...";
            else if (rooms.Count == 0) empty = "Nenhuma sala aberta. Até os fantasmas foram pra casa. Crie uma ou peça o código a um amigo.";
            else if (filtered.Count == 0) empty = "Nenhuma sala com esse nome ou líder.";
            emptyRoomsText.text = empty;
            emptyRoomsText.gameObject.SetActive(empty.Length > 0);

            int pages = RoomBrowserRules.PageCount(filtered.Count, MaxRoomsShown);
            pageText.text = roomsLoaded ? $"Página {page + 1} / {pages}" : "";
            prevPageButton.interactable = page > 0;
            nextPageButton.interactable = page < pages - 1;
        }

        // Nome digitado por outro jogador: nada de rich text (um "<" quebraria a formatação da linha).
        static string Safe(string text) => (text ?? "").Replace("<", "‹");

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
