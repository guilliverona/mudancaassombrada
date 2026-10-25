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

        // Prepara tudo para jogar: dados, prefabs, a fase (se você deixar) e o menu.
        // priority 0 = aparece em primeiro no menu.
        [MenuItem("Mudança Assombrada/Criar Todas as Cenas", priority = 0)]
        static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LevelPrefabsBuilder.EnsurePrefabs(recreateAll: false); // também cria os dados que faltam
            if (PrototypeSceneBuilder.ConfirmOverwrite()) PrototypeSceneBuilder.BuildScene();
            BuildScene();

            EditorSceneManager.OpenScene(ScenePath); // termina com o Menu aberto, pronto para o Play
            Debug.Log("Tudo pronto! A cena Menu está aberta: aperte Play.");
        }

        [MenuItem("Mudança Assombrada/Criar Cena Menu", priority = 1)]
        static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            GameDataBuilder.EnsureData();
            BuildScene();
        }

        public static void BuildScene()
        {
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

            // --- Painel Conexão ---
            var connectPanel = UiBuilder.CreatePanel("Painel Conexão", canvas);
            UiBuilder.CreateText("Seu nome", connectPanel, "Seu nome:", Theme.BodySize);
            var nameInput = UiBuilder.CreateInput(connectPanel, "Como os outros vão te ver");
            nameInput.characterLimit = 20; // cabe no FixedString32Bytes da sala de espera
            var hostButton = UiBuilder.CreateButton("Hospedar partida", connectPanel);
            UiBuilder.CreateText("Separador", connectPanel, "— ou entre na partida de um amigo —", Theme.NoteSize).color = Theme.MutedColor;
            UiBuilder.CreateText("IP", connectPanel, "IP do host:", Theme.BodySize);
            var addressInput = UiBuilder.CreateInput(connectPanel, "127.0.0.1");
            var joinButton = UiBuilder.CreateButton("Entrar na partida", connectPanel);
            var statusText = UiBuilder.CreateText("Status", connectPanel, "", Theme.NoteSize);
            statusText.color = Theme.DangerColor; // aqui só aparecem erros de conexão
            UiBuilder.SetHeight(statusText.gameObject, 70);
            var connectBackButton = UiBuilder.CreateButton("Voltar", connectPanel);

            // --- Painel Sala de Espera ---
            var lobbyPanel = UiBuilder.CreatePanel("Painel Sala de Espera", canvas);
            var playerListText = UiBuilder.CreateText("Lista de Jogadores", lobbyPanel, "", Theme.BodySize);
            playerListText.alignment = TextAnchor.UpperLeft;
            UiBuilder.SetHeight(playerListText.gameObject, 130);

            // Capítulo: [◀] A Casa do Espelho [▶]. Só o host navega; os outros só veem.
            var chapterRow = new GameObject("Capítulo", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            chapterRow.transform.SetParent(lobbyPanel, false);
            var rowLayout = chapterRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            UiBuilder.SetHeight(chapterRow, 60);
            var chapterPrev = UiBuilder.CreateButton("<", chapterRow.transform);
            var chapterText = UiBuilder.CreateText("Nome do Capítulo", chapterRow.transform, "", Theme.ButtonSize);
            chapterText.font = GameFonts.Title;
            var chapterNext = UiBuilder.CreateButton(">", chapterRow.transform);
            // Larguras: setas fixas e estreitas; o nome ocupa o resto (flexibleWidth).
            chapterPrev.GetComponent<LayoutElement>().preferredWidth = 60;
            chapterNext.GetComponent<LayoutElement>().preferredWidth = 60;
            chapterText.GetComponent<LayoutElement>().flexibleWidth = 1;
            var chapterInfo = UiBuilder.CreateText("Info do Capítulo", lobbyPanel, "", Theme.NoteSize);
            chapterInfo.color = Theme.MutedColor;
            UiBuilder.SetHeight(chapterInfo.gameObject, 50);

            // Um botão por personagem do Elenco: personagem novo no Elenco = botão novo aqui.
            var roster = CharacterRoster.Instance;
            var characterButtons = new Button[roster.Count];
            for (int i = 0; i < characterButtons.Length; i++)
            {
                var character = roster.Get(i);
                characterButtons[i] = UiBuilder.CreateButton(character.FullName, lobbyPanel);
                // O nome aparece na cor do personagem, misturada com o cinza para não destoar do resto.
                characterButtons[i].GetComponentInChildren<Text>().color = Theme.Muted(character.color);
            }
            var startButton = UiBuilder.CreateButton("Começar", lobbyPanel);
            var leaveButton = UiBuilder.CreateButton("Sair da sala", lobbyPanel);
            var lobbyInfoText = UiBuilder.CreateText("Info", lobbyPanel, "", Theme.NoteSize);
            lobbyInfoText.color = Theme.MutedColor;
            UiBuilder.SetHeight(lobbyInfoText.gameObject, 50);

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
            Set("addressInput", addressInput);
            Set("hostButton", hostButton);
            Set("joinButton", joinButton);
            Set("connectBackButton", connectBackButton);
            Set("statusText", statusText);
            Set("playerListText", playerListText);
            Set("chapterPrevButton", chapterPrev);
            Set("chapterNextButton", chapterNext);
            Set("chapterText", chapterText);
            Set("chapterInfoText", chapterInfo);
            Set("startButton", startButton);
            Set("leaveButton", leaveButton);
            Set("lobbyInfoText", lobbyInfoText);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorTools.SetReferences(menu, "characterButtons", characterButtons);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorTools.FixNetworkObjectIds(scene); // o LobbyManager é um objeto de rede da cena
            EditorTools.UpdateBuildSettings();
            Debug.Log("Cena Menu criada! Aperte Play nela para jogar.");
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

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, NetworkManagerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
