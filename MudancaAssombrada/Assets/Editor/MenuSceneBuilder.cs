using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Cria a cena do Menu: um Canvas (uGUI) com os 4 painéis, a sala de espera em rede
// e o prefab do NetworkManager. Depois de criada, a cena pode ser editada à mão na Unity
// (mudar cores, posições, textos): o MainMenu só precisa que os objetos continuem ligados.
public static class MenuSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Menu.unity";
    const string NetworkManagerPrefabPath = "Assets/Prefabs/NetworkManager.prefab";

    static Font font;
    static DefaultControls.Resources uiResources;

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

        // Fonte e imagens padrão da Unity, as mesmas do menu GameObject > UI.
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiResources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.04f, 0.04f, 0.07f);

        var netManagerPrefab = CreateNetworkManagerPrefab();

        // --- Sala de espera: objeto de rede na cena ---
        var lobbyGo = new GameObject("LobbyManager");
        lobbyGo.AddComponent<Unity.Netcode.NetworkObject>();
        lobbyGo.AddComponent<LobbyManager>();

        // --- EventSystem: quem leva os cliques do mouse até os botões ---
        // O módulo do Input System novo (o projeto usa ele, não o Input antigo).
        var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // --- Canvas: a "folha" onde a interface é desenhada, por cima de tudo ---
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        // Desenha pensando numa tela 1920x1080 e estica/encolhe para qualquer outra resolução.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var canvas = canvasGo.transform;

        var title = CreateText("Título", canvas, "MUDANÇA ASSOMBRADA", 84, FontStyle.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(1600, 120);
        title.color = new Color(0.85f, 0.9f, 1f);

        // --- Painel Inicial ---
        var mainPanel = CreatePanel("Painel Inicial", canvas);
        var playButton = CreateButton("Jogar", mainPanel);
        var optionsButton = CreateButton("Opções", mainPanel);
        var quitButton = CreateButton("Sair", mainPanel);

        // --- Painel Conexão ---
        var connectPanel = CreatePanel("Painel Conexão", canvas);
        CreateText("Seu nome", connectPanel, "Seu nome:", 26);
        var nameInput = CreateInput(connectPanel, "Como os outros vão te ver");
        nameInput.characterLimit = 20; // cabe no FixedString32Bytes da sala de espera
        var hostButton = CreateButton("Hospedar partida", connectPanel);
        CreateText("Separador", connectPanel, "— ou entre na partida de um amigo —", 22).color = Color.gray;
        CreateText("IP", connectPanel, "IP do host (no Radmin: o 26.x.x.x dele):", 24);
        var addressInput = CreateInput(connectPanel, "127.0.0.1");
        var joinButton = CreateButton("Entrar na partida", connectPanel);
        var statusText = CreateText("Status", connectPanel, "", 22);
        statusText.color = new Color(1f, 0.55f, 0.45f);
        SetHeight(statusText.gameObject, 70);
        var connectBackButton = CreateButton("Voltar", connectPanel);

        // --- Painel Sala de Espera ---
        var lobbyPanel = CreatePanel("Painel Sala de Espera", canvas);
        var playerListText = CreateText("Lista de Jogadores", lobbyPanel, "", 26);
        playerListText.alignment = TextAnchor.UpperLeft;
        SetHeight(playerListText.gameObject, 160);
        CreateText("Escolha", lobbyPanel, "Escolha seu personagem:", 24);
        var characterButtons = new Button[CharacterSwitcher.Names.Length];
        for (int i = 0; i < characterButtons.Length; i++)
        {
            characterButtons[i] = CreateButton(CharacterSwitcher.Names[i], lobbyPanel);
            characterButtons[i].GetComponentInChildren<Text>().color = CharacterSwitcher.BodyColors[i] * 0.6f + Color.black * 0.4f;
        }
        var startButton = CreateButton("Começar", lobbyPanel);
        var leaveButton = CreateButton("Sair da sala", lobbyPanel);
        var lobbyInfoText = CreateText("Info", lobbyPanel, "", 22);
        lobbyInfoText.color = Color.gray;
        SetHeight(lobbyInfoText.gameObject, 70);

        // --- Painel Opções ---
        var optionsPanel = CreatePanel("Painel Opções", canvas);
        var sensitivityText = CreateText("Sensibilidade (texto)", optionsPanel, "", 26);
        var sensitivitySlider = CreateSlider(optionsPanel, 0.2f, 3f);
        var volumeText = CreateText("Volume (texto)", optionsPanel, "", 26);
        var volumeSlider = CreateSlider(optionsPanel, 0f, 1f);
        var fullscreenToggle = CreateToggle(optionsPanel, "Tela cheia");
        var optionsBackButton = CreateButton("Voltar", optionsPanel);

        // --- O script que dá vida a tudo isso ---
        var menu = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        void Set(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
        Set("networkManagerPrefab", netManagerPrefab);
        Set("mainPanel", mainPanel.gameObject);
        Set("connectPanel", connectPanel.gameObject);
        Set("lobbyPanel", lobbyPanel.gameObject);
        Set("optionsPanel", optionsPanel.gameObject);
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
        Set("sensitivitySlider", sensitivitySlider);
        Set("volumeSlider", volumeSlider);
        Set("fullscreenToggle", fullscreenToggle);
        Set("sensitivityValueText", sensitivityText);
        Set("volumeValueText", volumeText);
        Set("optionsBackButton", optionsBackButton);
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

    // ---------- Ajudantes de interface ----------

    // Um painel: caixa escura no meio da tela que empilha os filhos na vertical.
    static Transform CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -60);
        rect.sizeDelta = new Vector2(760, 820);
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        // VerticalLayoutGroup: organiza os filhos um embaixo do outro, sem precisar posicionar cada um.
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(50, 50, 40, 40);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go.transform;
    }

    static Button CreateButton(string label, Transform parent)
    {
        var go = DefaultControls.CreateButton(uiResources);
        go.name = "Botão " + label;
        go.transform.SetParent(parent, false);
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 30;
        SetHeight(go, 64);
        return go.GetComponent<Button>();
    }

    static Text CreateText(string name, Transform parent, string content, int size, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.supportRichText = true; // permite <color> e <b> no texto (usado na lista de jogadores)
        SetHeight(go, size + 16);
        return text;
    }

    static InputField CreateInput(Transform parent, string placeholder)
    {
        var go = DefaultControls.CreateInputField(uiResources);
        go.transform.SetParent(parent, false);
        var input = go.GetComponent<InputField>();
        foreach (var t in go.GetComponentsInChildren<Text>()) { t.font = font; t.fontSize = 26; }
        ((Text)input.placeholder).text = placeholder;
        SetHeight(go, 56);
        return input;
    }

    static Slider CreateSlider(Transform parent, float min, float max)
    {
        var go = DefaultControls.CreateSlider(uiResources);
        go.transform.SetParent(parent, false);
        var slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        SetHeight(go, 36);
        return slider;
    }

    static Toggle CreateToggle(Transform parent, string label)
    {
        var go = DefaultControls.CreateToggle(uiResources);
        go.transform.SetParent(parent, false);
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 26;
        text.color = Color.white;
        SetHeight(go, 40);
        return go.GetComponent<Toggle>();
    }

    // LayoutElement: diz ao VerticalLayoutGroup a altura que este item quer ter.
    static void SetHeight(GameObject go, float height)
    {
        // Não use "??" com componentes da Unity: um componente que falta nem sempre é um null
        // "de verdade" para o C#. TryGetComponent é o jeito seguro de perguntar se existe.
        if (!go.TryGetComponent(out LayoutElement element)) element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }
}
