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

            // Título grande e "apagado", em cinza: sem brilho nem efeito, só a fonte.
            var title = UiBuilder.CreateText("Título", canvas, "MUDANÇA ASSOMBRADA", Theme.TitleSize);
            title.font = GameFonts.Title; // Bebas Neue: condensada, então cabe grande
            // Texto do uGUI que não cabe na caixa SOME. Overflow deixa ele passar da borda.
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.color = Theme.TitleColor;
            // Respiro em cima; os painéis começam logo abaixo.
            UiBuilder.Place(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(1600, Theme.TitleSize));

            // --- Painel Inicial: botões estreitos e soltos na tela, sem caixa em volta ---
            var mainPanel = UiBuilder.CreatePanel("Painel Inicial", canvas, 260, 320, background: false);
            ((RectTransform)mainPanel).anchoredPosition = new Vector2(0, -200); // mais para baixo
            var playButton = UiBuilder.CreateButton("Jogar", mainPanel);
            var optionsButton = UiBuilder.CreateButton("Opções", mainPanel);
            var quitButton = UiBuilder.CreateButton("Sair", mainPanel);

            // --- Painel Salas Online ---
            // Criar sala, lista de salas abertas (preenchida pelo MainMenu) e entrar por código.
            var connectPanel = UiBuilder.CreatePanel("Painel Salas Online", canvas);
            UiBuilder.CreateText("Seu nome", connectPanel, "Seu nome:", Theme.BodySize);
            var nameInput = UiBuilder.CreateInput(connectPanel, "Como os outros vão te ver");
            nameInput.characterLimit = 20; // cabe no FixedString32Bytes da sala de espera
            var createRoomButton = UiBuilder.CreateButton("Criar sala", connectPanel);

            // Linha: "Salas abertas" ............ [Atualizar]
            var listHeader = CreateRow("Cabeçalho das Salas", connectPanel, 50);
            var roomsLabel = UiBuilder.CreateText("Salas abertas", listHeader, "Salas abertas:", Theme.BodySize);
            roomsLabel.alignment = TextAnchor.MiddleLeft;
            roomsLabel.GetComponent<LayoutElement>().flexibleWidth = 1;
            var refreshButton = UiBuilder.CreateButton("Atualizar", listHeader);
            refreshButton.GetComponent<LayoutElement>().preferredWidth = 160;

            // A lista: uma coluna de botões (um por sala), criados durante o jogo a partir do modelo.
            var roomList = new GameObject("Lista de Salas", typeof(RectTransform), typeof(VerticalLayoutGroup));
            roomList.transform.SetParent(connectPanel, false);
            var listLayout = roomList.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8;
            listLayout.childControlWidth = listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            UiBuilder.SetHeight(roomList, MainMenu.MaxRoomsShown * 48 + (MainMenu.MaxRoomsShown - 1) * 8);
            var emptyRoomsText = UiBuilder.CreateText("Sem Salas", roomList.transform, "Procurando salas...", Theme.NoteSize);
            emptyRoomsText.color = Theme.MutedColor;
            var roomButtonTemplate = UiBuilder.CreateButton("Sala (modelo)", roomList.transform);
            UiBuilder.SetHeight(roomButtonTemplate.gameObject, 48);
            roomButtonTemplate.gameObject.SetActive(false);

            // Linha: [ código da sala ] [Entrar]
            var codeRow = CreateRow("Entrar por Código", connectPanel, 56);
            var codeInput = UiBuilder.CreateInput(codeRow, "Código da sala (ex.: K7Q2XM)");
            codeInput.characterLimit = 10;
            codeInput.GetComponent<LayoutElement>().flexibleWidth = 1;
            var joinCodeButton = UiBuilder.CreateButton("Entrar", codeRow);
            joinCodeButton.GetComponent<LayoutElement>().preferredWidth = 160;

            var statusText = UiBuilder.CreateText("Status", connectPanel, "", Theme.NoteSize);
            statusText.color = Theme.DangerColor; // aqui aparecem os avisos e erros de conexão
            UiBuilder.SetHeight(statusText.gameObject, 50);
            var connectBackButton = UiBuilder.CreateButton("Voltar", connectPanel);

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
            var readyButton = UiBuilder.CreateButton("Estou pronto", left);
            var startButton = UiBuilder.CreateButton("Começar", left);

            // [Sair da sala] [Sala pública] [Copiar código]
            var actionsRow = CreateRow("Ações", left, 56);
            var leaveButton = UiBuilder.CreateButton("Sair da sala", actionsRow);
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

            // --- O script que dá vida a tudo isso ---
            var menu = canvas.gameObject.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            void Set(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
            Set("networkManagerPrefab", netManagerPrefab);
            Set("mainPanel", mainPanel.gameObject);
            Set("connectPanel", connectPanel.gameObject);
            Set("lobbyPanel", lobbyPanel.gameObject);
            Set("options", options);
            Set("playButton", playButton);
            Set("optionsButton", optionsButton);
            Set("quitButton", quitButton);
            Set("nameInput", nameInput);
            Set("createRoomButton", createRoomButton);
            Set("refreshButton", refreshButton);
            Set("roomList", roomList.transform);
            Set("roomButtonTemplate", roomButtonTemplate);
            Set("emptyRoomsText", emptyRoomsText);
            Set("codeInput", codeInput);
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
