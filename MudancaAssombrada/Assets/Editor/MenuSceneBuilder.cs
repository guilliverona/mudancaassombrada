using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada.Editor
{
    // Cria a cena do Menu: um Canvas (uGUI) com os 4 painéis, a sala de espera em rede
    // e o prefab do NetworkManager. As peças de interface vêm do UiBuilder.
    //
    // Diferente da fase, o menu é 100% gerado por código (é só interface, sem nada para
    // "montar à mão"), então recriar ele é sempre seguro.
    public static class MenuSceneBuilder
    {
        public const string ScenePath = EditorTools.ScenesFolder + "/Menu.unity";
        const string NetworkManagerPrefabPath = EditorTools.PrefabsFolder + "/NetworkManager.prefab";

        // Prepara tudo para jogar, na ordem certa. O EnsurePrefabs já cria os dados que faltam,
        // liga os modelos 3D, atualiza os sons e as animações; depois vêm as cenas.
        // Antes, "Manter a minha" pulava a Casa do Espelho em silêncio e parecia que o item
        // "não recriava todas as cenas". Agora o Console diz o que foi recriado e o que ficou.
        // priority 0 = aparece em primeiro no menu.
        [MenuItem("Mudança Assombrada/Preparar Tudo", priority = 0)]
        static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LevelPrefabsBuilder.EnsurePrefabs(recreateAll: false);
            bool mirror = MirrorHouseBuilder.ConfirmOverwrite();
            if (mirror) MirrorHouseBuilder.BuildScene();
            BuildScene(); // o Menu é só código: sempre recriado

            EditorSceneManager.OpenScene(ScenePath); // termina com o Menu aberto, pronto para o Play
            Debug.Log(mirror
                ? "Tudo pronto! Recriadas: Menu e Casa do Espelho. A cena Menu está aberta: aperte Play."
                : "Tudo pronto! Recriada: Menu. A Casa do Espelho foi MANTIDA como estava (você escolheu \"Manter a minha\"). Aperte Play.");
        }

        public static void BuildScene()
        {
            // O Menu é recriado do zero: guardamos o arquivo atual para devolver se nada mudou
            // de verdade (só os números internos). Ver StableAssets.
            string before = StableAssets.Snapshot(ScenePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Theme.BackgroundEdge;

            var netManagerPrefab = CreateNetworkManagerPrefab();

            // --- Sala de espera: objeto de rede na cena ---
            var lobbyGo = new GameObject("LobbyManager");
            lobbyGo.AddComponent<Unity.Netcode.NetworkObject>();
            lobbyGo.AddComponent<LobbyManager>();

            UiBuilder.CreateEventSystem();
            var canvas = UiBuilder.CreateCanvas("Canvas");

            // Fundo: o primeiro filho do Canvas é desenhado primeiro, então fica atrás de tudo.
            var background = new GameObject("Fundo", typeof(RectTransform), typeof(RawImage), typeof(MenuBackground));
            background.transform.SetParent(canvas, false);
            UiBuilder.Stretch(background.transform); // estica para qualquer resolução
            background.GetComponent<RawImage>().raycastTarget = false; // não "rouba" cliques

            // Fantasma de lençol que atravessa o fundo de vez em quando (logo depois do fundo: fica atrás de tudo).
            var ghost = new GameObject("Fantasma", typeof(RectTransform), typeof(RawImage), typeof(MenuGhost));
            ghost.transform.SetParent(canvas, false);
            ghost.GetComponent<RawImage>().color = Color.clear; // invisível até a primeira passada
            UiBuilder.Place(ghost.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2000, 0), new Vector2(192, 224));

            CreateBoxes(canvas);

            // Título pequeno e apagado no topo das OUTRAS telas (a inicial tem o grande).
            // O MainMenu esconde ele na tela inicial.
            var smallTitle = UiBuilder.CreateText("Título Pequeno", canvas, "MUDANÇA ASSOMBRADA", Theme.TitleSize / 2);
            smallTitle.font = GameFonts.Stamp;
            smallTitle.color = Theme.WithAlpha(Theme.TitleColor, 0.5f);
            smallTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            smallTitle.verticalOverflow = VerticalWrapMode.Overflow;
            UiBuilder.Place(smallTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(1200, 90));
            smallTitle.transform.localRotation = Theme.Tilt(Theme.TitleTilt);

            // --- Tela Inicial: título carimbado, botões soltos e um bilhete do Seu Valdemar ---
            // Tudo dentro de um objeto só, que o MainMenu liga e desliga como um painel.
            var mainScreen = new GameObject("Tela Inicial", typeof(RectTransform));
            mainScreen.transform.SetParent(canvas, false);
            UiBuilder.Stretch(mainScreen.transform);

            // Título em duas linhas, em papelão, torto como um carimbo dado com pressa.
            // Duas caixas de texto (uma por linha): a altura de linha da fonte de carimbo é grande demais.
            var title = new GameObject("Título", typeof(RectTransform));
            title.transform.SetParent(mainScreen.transform, false);
            UiBuilder.Place(title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(1600, 340));
            title.transform.localRotation = Theme.Tilt(Theme.TitleTilt);
            CreateTitleLine(title.transform, "MUDANÇA", 85);
            CreateTitleLine(title.transform, "ASSOMBRADA", 245);

            var tagline = UiBuilder.CreateText("Subtítulo", mainScreen.transform, "transportadora valdemar · desde sempre, até de madrugada", Theme.HandSize);
            tagline.font = GameFonts.Hand;
            tagline.color = Theme.MutedColor;
            tagline.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiBuilder.Place(tagline, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -460), new Vector2(1400, 50));

            // Botões estreitos e soltos na tela, sem caixa em volta. Os dois caminhos de jogo já aparecem
            // aqui: CRIAR SALA (o único de papelão) e ENTRAR NUMA SALA. Opções e Sair, menores, lado a lado.
            // 2 botões de 100 + a linha de 64 + 2 espaços de 24 + 80 de margem = 392. Topo em y=540.
            var mainPanel = UiBuilder.CreatePanel("Painel Inicial", mainScreen.transform, 392, 540, background: false);
            ((RectTransform)mainPanel).anchoredPosition = new Vector2(0, -196);
            mainPanel.GetComponent<VerticalLayoutGroup>().spacing = 24;
            var newRoomButton = UiBuilder.CreateButton("Criar sala", mainPanel, UiBuilder.ButtonStyle.Cardboard, "você dirige o caminhão");
            var findRoomButton = UiBuilder.CreateButton("Entrar numa sala", mainPanel, UiBuilder.ButtonStyle.Dark, "pegar carona");
            var smallRow = CreateRow("Opções e Sair", mainPanel, 64);
            smallRow.GetComponent<HorizontalLayoutGroup>().spacing = 24;
            var optionsButton = UiBuilder.CreateButton("Opções", smallRow);
            var quitButton = UiBuilder.CreateButton("Sair", smallRow);
            SetFlexibleWidth(optionsButton.gameObject);
            SetFlexibleWidth(quitButton.gameObject);

            // O texto do bilhete é sorteado pelo MainMenu (ValdemarNotes) cada vez que o menu abre.
            var menuNote = UiBuilder.CreateNote(mainScreen.transform, ValdemarNotes.Menu[0], Theme.NoteTilt);
            UiBuilder.Place(menuNote.transform.parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140, -600), new Vector2(400, 260));

            // --- Tela Criar Sala: nome, quem pode entrar e o botão de papelão ---
            var createScreen = CreateScreen("Tela Criar Sala", canvas);
            // 720 de altura, topo em y=220.
            var createPanel = UiBuilder.CreatePanel("Painel Criar Sala", createScreen, 720, 800);
            ((RectTransform)createPanel).anchoredPosition = new Vector2(0, -40);
            UiBuilder.CreateStamp("Criar sala", createPanel);
            var createAbout = UiBuilder.CreateText("Explicação", createPanel,
                "Você vira o líder: escolhe o capítulo na sala de espera e chama os amigos pelo código.", Theme.BodySize);
            createAbout.alignment = TextAnchor.UpperLeft;
            createAbout.color = Theme.MutedColor;
            UiBuilder.SetHeight(createAbout.gameObject, 80);
            CreateLabel("Nome no crachá", createPanel);
            var nameInput = UiBuilder.CreateInput(createPanel, "Como os outros vão te ver");
            nameInput.characterLimit = 20; // cabe no FixedString32Bytes da sala de espera
            CreateLabel("Quem pode entrar", createPanel);
            var privacyRow = CreateRow("Quem pode entrar", createPanel, 100);
            var publicButton = CreateOption("Pública", "aparece na lista de salas", privacyRow);
            var privateButton = CreateOption("Só com código", "só quem você chamar", privacyRow);
            var createRoomButton = UiBuilder.CreateButton("Criar sala", createPanel, UiBuilder.ButtonStyle.Cardboard, "você dirige o caminhão");
            var createStatusText = UiBuilder.CreateText("Status", createPanel, "", Theme.NoteSize);
            createStatusText.color = Theme.DangerColor;
            UiBuilder.SetHeight(createStatusText.gameObject, 40);

            var createNote = UiBuilder.CreateNote(createScreen, ValdemarNotes.Create[0], -Theme.NoteTilt);
            UiBuilder.Place(createNote.transform.parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140, -340), new Vector2(360, 240));
            var createBackButton = CreateBackButton(createScreen);

            // --- Tela Entrar numa Sala: lista de salas (busca + páginas) e, embaixo, o código ---
            var connectScreen = CreateScreen("Tela Entrar numa Sala", canvas);
            // 1160 x 920, um pouco à esquerda (o bilhete fica à direita). Vai de x=280 a 1440, de y=130 a 1050.
            var connectPanel = UiBuilder.CreatePanel("Painel Salas Abertas", connectScreen, 920, 1160);
            ((RectTransform)connectPanel).anchoredPosition = new Vector2(-100, -50);
            connectPanel.GetComponent<VerticalLayoutGroup>().spacing = 10;

            // Linha: [SALAS ABERTAS] ............ Crachá [ nome ]
            var topRow = CreateRow("Topo", connectPanel, 72);
            var topLayout = topRow.GetComponent<HorizontalLayoutGroup>();
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            var stamp = UiBuilder.CreateStamp("Salas abertas", topRow);
            SetFlexibleWidth(stamp.transform.parent.parent.gameObject); // o "holder" do carimbo ocupa o que sobrar
            var badgeLabel = UiBuilder.CreateText("Crachá", topRow, "Crachá", Theme.BodySize);
            badgeLabel.color = Theme.MutedColor;
            badgeLabel.alignment = TextAnchor.MiddleRight;
            badgeLabel.GetComponent<LayoutElement>().preferredWidth = 110;
            var browserNameInput = UiBuilder.CreateInput(topRow, "Seu nome");
            browserNameInput.characterLimit = 20;
            browserNameInput.GetComponent<LayoutElement>().preferredWidth = 320;

            // Linha: [ procurar por sala ou líder ] [Atualizar]
            var searchRow = CreateRow("Busca", connectPanel, 56);
            var searchInput = UiBuilder.CreateInput(searchRow, "Procurar por sala ou líder");
            searchInput.characterLimit = 30;
            SetFlexibleWidth(searchInput.gameObject);
            var refreshButton = UiBuilder.CreateButton("Atualizar", searchRow, UiBuilder.ButtonStyle.Ghost);
            refreshButton.GetComponent<LayoutElement>().preferredWidth = 200;

            // A lista: sempre as mesmas 4 linhas (uma página); o MainMenu só troca o que elas mostram.
            const float rowHeight = 100, rowGap = 8;
            var roomList = CreateColumn("Lista de Salas", connectPanel, 0);
            roomList.GetComponent<VerticalLayoutGroup>().spacing = rowGap;
            UiBuilder.SetHeight(roomList.gameObject, MainMenu.MaxRoomsShown * rowHeight + (MainMenu.MaxRoomsShown - 1) * rowGap);
            var emptyRoomsText = UiBuilder.CreateText("Sem Salas", roomList, "", Theme.BodySize); // vazio até a primeira busca responder
            emptyRoomsText.color = Theme.MutedColor;
            UiBuilder.SetHeight(emptyRoomsText.gameObject, 80);
            var roomRows = new RoomRow[MainMenu.MaxRoomsShown];
            for (int i = 0; i < roomRows.Length; i++) roomRows[i] = CreateRoomRow(roomList, rowHeight);

            // Linha: [‹ Anterior]   PÁGINA 1 / 2   [Próxima ›]
            var pageRow = CreateRow("Páginas", connectPanel, 56);
            var prevPageButton = UiBuilder.CreateButton("‹ Anterior", pageRow, UiBuilder.ButtonStyle.Ghost);
            prevPageButton.GetComponent<LayoutElement>().preferredWidth = 220;
            var pageText = UiBuilder.CreateText("Página", pageRow, "", Theme.ButtonSize);
            pageText.font = GameFonts.Title;
            pageText.color = Theme.MutedColor;
            SetFlexibleWidth(pageText.gameObject);
            var nextPageButton = UiBuilder.CreateButton("Próxima ›", pageRow, UiBuilder.ButtonStyle.Ghost);
            nextPageButton.GetComponent<LayoutElement>().preferredWidth = 220;

            // Separador: - - - - ou, com o código de um amigo - - - -
            var orRow = CreateRow("Ou", connectPanel, 30);
            var orLayout = orRow.GetComponent<HorizontalLayoutGroup>();
            orLayout.childForceExpandHeight = false;
            orLayout.childAlignment = TextAnchor.MiddleCenter;
            orLayout.spacing = 20;
            CreateDashes(orRow);
            var orText = UiBuilder.CreateText("Texto", orRow, "ou, com o código de um amigo", Theme.NoteSize);
            orText.font = GameFonts.Hand;
            orText.color = Theme.MutedColor;
            orText.horizontalOverflow = HorizontalWrapMode.Overflow;
            orText.GetComponent<LayoutElement>().preferredWidth = 420;
            CreateDashes(orRow);

            // Linha: [ código da sala ] [Colar] [Entrar]
            var codeRow = CreateRow("Entrar por Código", connectPanel, 64);
            var codeInput = UiBuilder.CreateInput(codeRow, "Código da sala");
            // O código em Bebas, como uma etiqueta: fácil de ler em voz alta para um amigo.
            foreach (var t in codeInput.GetComponentsInChildren<Text>()) { t.font = GameFonts.Title; t.fontSize = Theme.ButtonSize; }
            codeInput.characterLimit = 10;
            SetFlexibleWidth(codeInput.gameObject);
            var pasteCodeButton = UiBuilder.CreateButton("Colar", codeRow, UiBuilder.ButtonStyle.Ghost);
            pasteCodeButton.GetComponent<LayoutElement>().preferredWidth = 180;
            var joinCodeButton = UiBuilder.CreateButton("Entrar", codeRow);
            joinCodeButton.GetComponent<LayoutElement>().preferredWidth = 200;

            var statusText = UiBuilder.CreateText("Status", connectPanel, "", Theme.NoteSize);
            statusText.color = Theme.DangerColor; // aqui aparecem os avisos e erros de conexão
            UiBuilder.SetHeight(statusText.gameObject, 30);

            var roomsNote = UiBuilder.CreateNote(connectScreen, ValdemarNotes.Rooms[0], Theme.NoteTilt);
            UiBuilder.Place(roomsNote.transform.parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -600), new Vector2(340, 260));
            var connectBackButton = CreateBackButton(connectScreen);

            // --- Painel Sala de Espera ---
            // Duas colunas: à esquerda a sala (jogadores, capítulo, personagens, botões);
            // à direita o chat. O painel é mais largo que os outros para caber os dois.
            var lobbyPanel = UiBuilder.CreatePanel("Painel Sala de Espera", canvas, 820, 1320);
            var columns = CreateRow("Colunas", lobbyPanel, 740);
            columns.GetComponent<HorizontalLayoutGroup>().spacing = 30;
            var left = CreateColumn("Sala", columns, 640);
            var right = CreateColumn("Chat", columns, 550);

            var playerListText = UiBuilder.CreateText("Título dos Jogadores", left, "", Theme.BodySize);
            playerListText.alignment = TextAnchor.MiddleLeft;
            UiBuilder.SetHeight(playerListText.gameObject, 34);

            // Uma linha por jogador: [nome, personagem, pronto] [Líder] [Expulsar].
            // Só existe o MODELO aqui; o MainMenu copia uma linha para cada jogador.
            var playerRows = CreateColumn("Linhas dos Jogadores", left, 0);
            playerRows.GetComponent<VerticalLayoutGroup>().spacing = 6;
            UiBuilder.SetHeight(playerRows.gameObject, NetworkSession.MaxPlayers * 48 + (NetworkSession.MaxPlayers - 1) * 6);
            var playerRowTemplate = CreateRow("Linha (modelo)", playerRows, 48);
            var rowInfo = UiBuilder.CreateText("Info", playerRowTemplate, "", Theme.NoteSize);
            rowInfo.alignment = TextAnchor.MiddleLeft;
            rowInfo.supportRichText = true;
            SetFlexibleWidth(rowInfo.gameObject);
            var rowLeader = UiBuilder.CreateButton("Líder", playerRowTemplate);
            rowLeader.GetComponent<LayoutElement>().preferredWidth = 110;
            rowLeader.GetComponentInChildren<Text>().fontSize = Theme.BodySize;
            var rowKick = UiBuilder.CreateButton("Expulsar", playerRowTemplate);
            rowKick.GetComponent<LayoutElement>().preferredWidth = 130;
            rowKick.GetComponentInChildren<Text>().fontSize = Theme.BodySize;
            rowKick.GetComponentInChildren<Text>().color = Theme.DangerColor;
            playerRowTemplate.gameObject.SetActive(false);

            // Capítulo: [◀] A Casa do Espelho [▶]. Só o líder navega; os outros só veem.
            var chapterRow = CreateRow("Capítulo", left, 56);
            var chapterPrev = UiBuilder.CreateButton("<", chapterRow);
            var chapterText = UiBuilder.CreateText("Nome do Capítulo", chapterRow, "", Theme.ButtonSize);
            chapterText.font = GameFonts.Title;
            var chapterNext = UiBuilder.CreateButton(">", chapterRow);
            // Larguras: setas fixas e estreitas; o nome ocupa o resto (flexibleWidth).
            chapterPrev.GetComponent<LayoutElement>().preferredWidth = 60;
            chapterNext.GetComponent<LayoutElement>().preferredWidth = 60;
            SetFlexibleWidth(chapterText.gameObject);
            var chapterInfo = UiBuilder.CreateText("Info do Capítulo", left, "", Theme.NoteSize);
            chapterInfo.color = Theme.MutedColor;
            UiBuilder.SetHeight(chapterInfo.gameObject, 48);

            // Um botão por personagem do Elenco, lado a lado: personagem novo = botão novo aqui.
            var roster = CharacterRoster.Instance;
            var characterRow = CreateRow("Personagens", left, 56);
            var characterButtons = new Button[roster.Count];
            for (int i = 0; i < characterButtons.Length; i++)
            {
                var character = roster.Get(i);
                characterButtons[i] = UiBuilder.CreateButton(character.FullName, characterRow);
                characterButtons[i].GetComponent<LayoutElement>().flexibleWidth = 1;
                var label = characterButtons[i].GetComponentInChildren<Text>();
                label.fontSize = Theme.BodySize;
                // O nome aparece na cor do personagem, misturada com o cinza para não destoar do resto.
                label.color = Theme.Muted(character.color);
            }

            // [Estou pronto] (convidados) ou [Começar] (líder): só um dos dois aparece.
            var readyButton = UiBuilder.CreateButton("Estou pronto", left, UiBuilder.ButtonStyle.Cardboard);
            var startButton = UiBuilder.CreateButton("Começar", left, UiBuilder.ButtonStyle.Cardboard);

            // [Sair da sala] [Sala pública] [Copiar código]
            var actionsRow = CreateRow("Ações", left, 56);
            var leaveButton = UiBuilder.CreateButton("Sair da sala", actionsRow, UiBuilder.ButtonStyle.Ghost);
            var privacyButton = UiBuilder.CreateButton("Sala pública", actionsRow);
            var copyCodeButton = UiBuilder.CreateButton("Copiar código", actionsRow);
            foreach (var b in new[] { leaveButton, privacyButton, copyCodeButton })
            {
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                b.GetComponentInChildren<Text>().fontSize = Theme.BodySize;
            }

            var lobbyInfoText = UiBuilder.CreateText("Info", left, "", Theme.NoteSize);
            lobbyInfoText.color = Theme.MutedColor;
            UiBuilder.SetHeight(lobbyInfoText.gameObject, 48);

            // --- Chat (coluna da direita) ---
            var chatTitle = UiBuilder.CreateText("Título do Chat", right, "Chat", Theme.BodySize);
            chatTitle.alignment = TextAnchor.MiddleLeft;
            UiBuilder.SetHeight(chatTitle.gameObject, 34);
            var chatBox = UiBuilder.CreateImage("Fundo do Chat", right, Theme.FieldColor);
            UiBuilder.SetHeight(chatBox.gameObject, 610);
            chatBox.gameObject.AddComponent<RectMask2D>(); // o que passar da caixa (mensagens antigas) fica escondido
            var chatText = UiBuilder.CreateText("Mensagens", chatBox.transform, "", Theme.NoteSize);
            UiBuilder.Stretch(chatText);
            ((RectTransform)chatText.transform).offsetMin = new Vector2(14, 10);
            ((RectTransform)chatText.transform).offsetMax = new Vector2(-14, -10);
            // Ancorado embaixo: as mensagens novas ficam no pé e as antigas sobem (e somem no topo).
            chatText.alignment = TextAnchor.LowerLeft;
            chatText.verticalOverflow = VerticalWrapMode.Overflow;
            chatText.supportRichText = true;
            var chatRow = CreateRow("Enviar", right, 56);
            var chatInput = UiBuilder.CreateInput(chatRow, "Mensagem...");
            chatInput.characterLimit = LobbyManager.ChatMaxLength;
            SetFlexibleWidth(chatInput.gameObject);
            var chatSendButton = UiBuilder.CreateButton("Enviar", chatRow);
            chatSendButton.GetComponent<LayoutElement>().preferredWidth = 130;

            // --- Painel Opções (o mesmo usado na pausa) ---
            var options = UiBuilder.CreateOptionsPanel(canvas);

            // No menu os botões balançam no hover (na pausa da partida, não).
            foreach (var hover in canvas.GetComponentsInChildren<ButtonHover>(true))
                hover.wobble = hover.GetComponent<Button>() != null; // campos de texto não balançam

            // --- O script que dá vida a tudo isso ---
            var menu = canvas.gameObject.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            void Set(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
            Set("networkManagerPrefab", netManagerPrefab);
            Set("mainPanel", mainScreen);
            Set("smallTitle", smallTitle.gameObject);
            Set("menuNoteText", menuNote);
            Set("createNoteText", createNote);
            Set("roomsNoteText", roomsNote);
            Set("createPanel", createScreen.gameObject);
            Set("connectPanel", connectScreen.gameObject);
            Set("lobbyPanel", lobbyPanel.gameObject);
            Set("options", options);
            Set("newRoomButton", newRoomButton);
            Set("findRoomButton", findRoomButton);
            Set("optionsButton", optionsButton);
            Set("quitButton", quitButton);
            Set("nameInput", nameInput);
            Set("publicButton", publicButton);
            Set("privateButton", privateButton);
            Set("createRoomButton", createRoomButton);
            Set("createStatusText", createStatusText);
            Set("createBackButton", createBackButton);
            Set("browserNameInput", browserNameInput);
            Set("searchInput", searchInput);
            Set("refreshButton", refreshButton);
            Set("emptyRoomsText", emptyRoomsText);
            Set("prevPageButton", prevPageButton);
            Set("nextPageButton", nextPageButton);
            Set("pageText", pageText);
            Set("codeInput", codeInput);
            Set("pasteCodeButton", pasteCodeButton);
            Set("joinCodeButton", joinCodeButton);
            Set("connectBackButton", connectBackButton);
            Set("statusText", statusText);
            Set("playerListText", playerListText);
            Set("chapterPrevButton", chapterPrev);
            Set("chapterNextButton", chapterNext);
            Set("chapterText", chapterText);
            Set("chapterInfoText", chapterInfo);
            Set("startButton", startButton);
            Set("readyButton", readyButton);
            Set("playerRows", playerRows);
            Set("playerRowTemplate", playerRowTemplate.gameObject);
            Set("privacyButton", privacyButton);
            Set("copyCodeButton", copyCodeButton);
            Set("chatText", chatText);
            Set("chatInput", chatInput);
            Set("chatSendButton", chatSendButton);
            Set("leaveButton", leaveButton);
            Set("lobbyInfoText", lobbyInfoText);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorTools.SetReferences(menu, "characterButtons", characterButtons);
            EditorTools.SetReferences(menu, "roomRows", roomRows);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorTools.FixNetworkObjectIds(scene); // o LobbyManager é um objeto de rede da cena
            EditorTools.UpdateBuildSettings();

            if (StableAssets.RestoreIfSame(ScenePath, before))
            {
                // A cena aberta é a recém-criada (com números novos): reabre a do disco, que é a antiga.
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("Cena Menu conferida: nada mudou, o arquivo ficou como estava.");
                return;
            }
            Debug.Log("Cena Menu criada! Aperte Play nela para jogar.");
        }

        // Uma "tela": objeto do tamanho da tela inteira que o MainMenu liga e desliga como um painel.
        // Guarda o painel, o bilhete e o VOLTAR (que ficam fora do painel).
        static Transform CreateScreen(string name, Transform canvas)
        {
            var screen = new GameObject(name, typeof(RectTransform));
            screen.transform.SetParent(canvas, false);
            UiBuilder.Stretch(screen.transform);
            return screen.transform;
        }

        // VOLTAR fantasma, solto no canto de baixo à esquerda da tela.
        static Button CreateBackButton(Transform screen)
        {
            var back = UiBuilder.CreateButton("Voltar", screen, UiBuilder.ButtonStyle.Ghost);
            UiBuilder.Place(back, Vector2.zero, Vector2.zero, new Vector2(40, 48), new Vector2(220, 64));
            return back;
        }

        // Rótulo pequeno e apagado em cima de um campo ("Nome no crachá").
        static void CreateLabel(string content, Transform parent)
        {
            var label = UiBuilder.CreateText(content, parent, content, Theme.BodySize);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Theme.MutedColor;
        }

        // Uma das opções de "Quem pode entrar": botão escuro que fica aceso quando escolhido.
        static Button CreateOption(string label, string subtitle, Transform row)
        {
            var option = UiBuilder.CreateButton(label, row, UiBuilder.ButtonStyle.Dark, subtitle);
            option.GetComponent<ButtonHover>().selectedColor = Color.Lerp(Theme.FieldColor, Theme.HighlightColor, 0.16f);
            SetFlexibleWidth(option.gameObject);
            return option;
        }

        // Linha tracejada que ocupa o espaço que sobrar numa linha (o separador "ou").
        static void CreateDashes(Transform row)
        {
            var go = new GameObject("Tracejado", typeof(RectTransform), typeof(RawImage), typeof(DashedLine), typeof(LayoutElement));
            go.transform.SetParent(row, false);
            var image = go.GetComponent<RawImage>();
            image.color = Theme.BorderColor;
            image.raycastTarget = false;
            var element = go.GetComponent<LayoutElement>();
            element.flexibleWidth = 1;
            element.preferredHeight = 2;
        }

        // Uma linha da lista de salas (ver RoomRow):
        //   [Nome / Líder · Capítulo]  ● ● ●  2/3  [ENTRAR]
        // Começa escondida: o MainMenu mostra as que têm sala.
        static RoomRow CreateRoomRow(Transform list, float height)
        {
            var go = new GameObject("Sala", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(CanvasGroup), typeof(RoomRow));
            go.transform.SetParent(list, false);
            go.GetComponent<Image>().color = Theme.FieldColor;
            go.GetComponent<Image>().raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = Theme.ShadowColor;
            shadow.effectDistance = new Vector2(0, -2);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28, 20, 12, 12);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            UiBuilder.SetHeight(go, height);

            var info = CreateColumn("Textos", go.transform, 0);
            info.GetComponent<VerticalLayoutGroup>().spacing = 2;
            SetFlexibleWidth(info.gameObject);
            var nameText = UiBuilder.CreateText("Nome", info, "", Theme.BodySize);
            nameText.font = GameFonts.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            UiBuilder.SetHeight(nameText.gameObject, 36);
            var infoText = UiBuilder.CreateText("Info", info, "", Theme.NoteSize);
            infoText.color = Theme.MutedColor;
            infoText.alignment = TextAnchor.MiddleLeft;
            UiBuilder.SetHeight(infoText.gameObject, 30);

            // Bolinhas dos personagens já escolhidos (no máximo uma por vaga).
            var dotsRow = new GameObject("Bolinhas", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            dotsRow.transform.SetParent(go.transform, false);
            var dotsLayout = dotsRow.GetComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 8;
            dotsLayout.childAlignment = TextAnchor.MiddleRight;
            dotsLayout.childControlWidth = dotsLayout.childControlHeight = true;
            dotsLayout.childForceExpandWidth = dotsLayout.childForceExpandHeight = false;
            dotsRow.GetComponent<LayoutElement>().preferredWidth = NetworkSession.MaxPlayers * 18 + (NetworkSession.MaxPlayers - 1) * 8;
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // círculo
            var dots = new Image[NetworkSession.MaxPlayers];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = UiBuilder.CreateImage("Bolinha", dotsRow.transform, Theme.TextColor);
                dots[i].sprite = knob;
                var element = dots[i].gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.preferredHeight = 18;
            }

            var slotsText = UiBuilder.CreateText("Vagas", go.transform, "", Theme.ButtonSize);
            slotsText.font = GameFonts.Title;
            slotsText.alignment = TextAnchor.MiddleRight;
            slotsText.GetComponent<LayoutElement>().preferredWidth = 70;

            var joinButton = UiBuilder.CreateButton("Entrar", go.transform);
            joinButton.GetComponent<LayoutElement>().preferredWidth = 180;

            var row = go.GetComponent<RoomRow>();
            var so = new SerializedObject(row);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("infoText").objectReferenceValue = infoText;
            so.FindProperty("slotsText").objectReferenceValue = slotsText;
            so.FindProperty("joinButton").objectReferenceValue = joinButton;
            so.FindProperty("group").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorTools.SetReferences(row, "dots", dots);
            go.SetActive(false);
            return row;
        }

        // Uma linha do título grande: centro dela a "centerY" pixels do topo do bloco do título.
        static void CreateTitleLine(Transform title, string content, float centerY)
        {
            var line = UiBuilder.CreateText(content, title, content, Theme.TitleSize);
            line.font = GameFonts.Stamp; // Saira Stencil: letra de carimbo de caixa
            line.color = Theme.TitleColor;
            // Texto do uGUI que não cabe na caixa SOME. Overflow deixa ele passar da borda.
            line.horizontalOverflow = HorizontalWrapMode.Overflow;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            UiBuilder.Place(line, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -centerY), new Vector2(1600, 170));
        }

        // Caixas de mudança empilhadas no canto de baixo, à esquerda: só blocos de cor (papelão e fita),
        // as mesmas da prévia "MenuInicial" do design system (lá em 960 de largura; aqui tudo dobra).
        // Quando o menu tiver um fundo 3D, elas podem virar os modelos de verdade.
        static void CreateBoxes(Transform canvas)
        {
            var group = new GameObject("Caixas", typeof(RectTransform));
            group.transform.SetParent(canvas, false);
            UiBuilder.Place(group.transform, Vector2.zero, Vector2.zero, new Vector2(60, 0), new Vector2(440, 240));
            const float fade = 0.8f; // o grupo todo meio apagado, para não brigar com os botões
            // x, y = canto de baixo à esquerda de cada bloco, dentro do grupo. Na ordem de desenho (de trás para a frente).
            void Box(string name, Color color, float alpha, float x, float y, float w, float h)
            {
                var box = UiBuilder.CreateImage(name, group.transform, Theme.WithAlpha(color, color.a * alpha * fade));
                UiBuilder.Place(box, Vector2.zero, Vector2.zero, new Vector2(x, y), new Vector2(w, h));
            }
            Box("Caixa da esquerda", Theme.CardboardDarkColor, 1f, 20, 0, 180, 140);
            Box("Caixa grande", Theme.CardboardColor, 0.55f, 190, 0, 220, 180);
            Box("Caixa de cima", Theme.CardboardColor, 0.35f, 80, 132, 140, 88);
            Box("Fita", Theme.TapeColor, 1f, 190, 84, 220, 20);
        }

        // Uma linha horizontal (elementos lado a lado) dentro de um painel.
        static Transform CreateRow(string name, Transform parent, float height)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            UiBuilder.SetHeight(row, height);
            return row.transform;
        }

        // Numa linha: este item ocupa a largura que sobrar.
        static void SetFlexibleWidth(GameObject go)
        {
            if (!go.TryGetComponent(out LayoutElement element)) element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = 1;
        }

        // Uma coluna (elementos um embaixo do outro) dentro de uma linha. width 0 = sem largura fixa.
        static Transform CreateColumn(string name, Transform parent, float width)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (width > 0) column.GetComponent<LayoutElement>().preferredWidth = width;
            return column.transform;
        }

        // O NetworkManager vira um prefab: o menu cria uma cópia na primeira vez que abre,
        // e ela sobrevive às trocas de cena (Menu -> Fase -> Menu).
        static GameObject CreateNetworkManagerPrefab()
        {
            var go = new GameObject("NetworkManager");
            var net = go.AddComponent<Unity.Netcode.NetworkManager>();
            // O Transport é o "carteiro": quem de fato manda os pacotes pela rede.
            var transport = go.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            // Desiste de conectar depois de ~10 s (o padrão é 60 s: muito tempo olhando "Conectando...").
            transport.ConnectTimeoutMS = 1000;
            transport.MaxConnectAttempts = 10;

            if (net.NetworkConfig == null) net.NetworkConfig = new Unity.Netcode.NetworkConfig();
            net.NetworkConfig.NetworkTransport = transport;
            net.NetworkConfig.PlayerPrefab = null;        // os bonecos nascem na fase (PlayerSpawner)
            net.NetworkConfig.ConnectionApproval = true;  // o NetworkSession decide quem entra
            // A lista com os prefabs que podem nascer pela rede (o Player está nela).
            // O Netcode costuma colocar essa lista sozinho ao criar o NetworkManager no editor;
            // adicionar de novo registraria o Player duas vezes (erro de "duplicate GlobalObjectIdHash").
            var prefabList = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            var lists = net.NetworkConfig.Prefabs.NetworkPrefabsLists;
            if (prefabList != null && !lists.Contains(prefabList)) lists.Add(prefabList);

            go.AddComponent<NetworkSession>();

            return EditorTools.SavePrefab(go, NetworkManagerPrefabPath); // não regrava se nada mudou
        }
    }
}
