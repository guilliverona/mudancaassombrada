using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Cria a cena do Menu: um Canvas (uGUI) com os 4 painéis, a sala de espera em rede
// e o prefab do NetworkManager. As peças de interface vêm do UiBuilder.
public static class MenuSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Menu.unity";
    const string NetworkManagerPrefabPath = "Assets/Prefabs/NetworkManager.prefab";

    // Recria as duas cenas juntas: assim nunca fica uma cena nova com outra desatualizada.
    // priority 0 = aparece em primeiro no menu.
    [MenuItem("Mudança Assombrada/Criar Todas as Cenas", priority = 0)]
    static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrototypeSceneBuilder.BuildScene();
        BuildScene();
        EditorSceneManager.OpenScene(ScenePath); // termina com o Menu aberto, pronto para o Play
        Debug.Log("Todas as cenas criadas! A cena Menu está aberta: aperte Play.");
    }

    [MenuItem("Mudança Assombrada/Criar Cena Menu")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildScene();
    }

    internal static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.04f, 0.04f, 0.07f);

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
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; // cantos colados nos da tela: estica para qualquer resolução
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        background.GetComponent<RawImage>().raycastTarget = false; // não "rouba" cliques

        // Título grande e "apagado", em cinza: sem brilho nem efeito, só a fonte.
        var title = UiBuilder.CreateText("Título", canvas, "MUDANÇA ASSOMBRADA", Theme.TitleSize);
        title.font = GameFonts.Title; // Bebas Neue: condensada, então cabe grande
        // Texto do uGUI que não cabe na caixa SOME. Overflow deixa ele passar da borda.
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        var titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -100); // respiro em cima; os painéis começam logo abaixo
        titleRect.sizeDelta = new Vector2(1600, Theme.TitleSize);
        title.color = Theme.TitleColor;

        // --- Painel Inicial: botões estreitos e soltos na tela, sem caixa em volta ---
        var mainPanel = UiBuilder.CreatePanel("Painel Inicial", canvas, 260, 320, background: false);
        ((RectTransform)mainPanel).anchoredPosition = new Vector2(0, -200); // mais para baixo, como na referência
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
        UiBuilder.SetHeight(playerListText.gameObject, 160);
        UiBuilder.CreateText("Escolha", lobbyPanel, "Escolha seu personagem:", Theme.BodySize);
        var characterButtons = new Button[CharacterSwitcher.Names.Length];
        for (int i = 0; i < characterButtons.Length; i++)
        {
            characterButtons[i] = UiBuilder.CreateButton(CharacterSwitcher.Names[i], lobbyPanel);
            // O nome aparece na cor do personagem, misturada com o cinza para não destoar do resto.
            characterButtons[i].GetComponentInChildren<Text>().color = Theme.Muted(CharacterSwitcher.BodyColors[i]);
        }
        var startButton = UiBuilder.CreateButton("Começar", lobbyPanel);
        var leaveButton = UiBuilder.CreateButton("Sair da sala", lobbyPanel);
        var lobbyInfoText = UiBuilder.CreateText("Info", lobbyPanel, "", Theme.NoteSize);
        lobbyInfoText.color = Theme.MutedColor;
        UiBuilder.SetHeight(lobbyInfoText.gameObject, 70);

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
        Set("startButton", startButton);
        Set("leaveButton", leaveButton);
        Set("lobbyInfoText", lobbyInfoText);
        var buttons = so.FindProperty("characterButtons");
        buttons.arraySize = characterButtons.Length;
        for (int i = 0; i < characterButtons.Length; i++)
            buttons.GetArrayElementAtIndex(i).objectReferenceValue = characterButtons[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        PrototypeSceneBuilder.FixNetworkObjectIds(scene); // o LobbyManager é um objeto de rede da cena
        PrototypeSceneBuilder.UpdateBuildSettings();
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
