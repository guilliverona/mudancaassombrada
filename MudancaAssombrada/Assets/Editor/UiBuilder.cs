using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Peças de interface (uGUI) usadas pelos construtores de cena: Canvas, painéis, botões...
// Fica separado para o Menu e a pausa da fase terem exatamente o mesmo visual.
public static class UiBuilder
{
    static Font font;
    static DefaultControls.Resources resources;

    // Fonte e imagens padrão da Unity, as mesmas do menu GameObject > UI.
    static void LoadDefaults()
    {
        if (font != null) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };
    }

    // EventSystem: quem leva os cliques do mouse até os botões.
    // O módulo do Input System novo (o projeto usa ele, não o Input antigo).
    public static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // Canvas: a "folha" onde a interface é desenhada, por cima de tudo.
    public static Transform CreateCanvas(string name)
    {
        LoadDefaults();
        var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // por cima das dicas e do cronômetro (OnGUI fica por cima de tudo, de qualquer jeito)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        // Desenha pensando numa tela 1920x1080 e estica/encolhe para qualquer outra resolução.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo.transform;
    }

    // Um painel: caixa escura no meio da tela que empilha os filhos na vertical.
    public static Transform CreatePanel(string name, Transform parent, float height = 820)
    {
        LoadDefaults();
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -60);
        rect.sizeDelta = new Vector2(760, height);
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

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

    public static Button CreateButton(string label, Transform parent)
    {
        LoadDefaults();
        var go = DefaultControls.CreateButton(resources);
        go.name = "Botão " + label;
        go.transform.SetParent(parent, false);
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 30;
        SetHeight(go, 64);
        return go.GetComponent<Button>();
    }

    public static Text CreateText(string name, Transform parent, string content, int size, FontStyle style = FontStyle.Normal)
    {
        LoadDefaults();
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

    public static InputField CreateInput(Transform parent, string placeholder)
    {
        LoadDefaults();
        var go = DefaultControls.CreateInputField(resources);
        go.transform.SetParent(parent, false);
        var input = go.GetComponent<InputField>();
        foreach (var t in go.GetComponentsInChildren<Text>()) { t.font = font; t.fontSize = 26; }
        ((Text)input.placeholder).text = placeholder;
        SetHeight(go, 56);
        return input;
    }

    public static Slider CreateSlider(Transform parent, float min, float max)
    {
        LoadDefaults();
        var go = DefaultControls.CreateSlider(resources);
        go.transform.SetParent(parent, false);
        var slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        SetHeight(go, 36);
        return slider;
    }

    public static Toggle CreateToggle(Transform parent, string label)
    {
        LoadDefaults();
        var go = DefaultControls.CreateToggle(resources);
        go.transform.SetParent(parent, false);
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 26;
        text.color = Color.white;
        SetHeight(go, 40);
        return go.GetComponent<Toggle>();
    }

    // O painel de Opções completo, já ligado ao script OptionsPanel (usado no Menu e na pausa).
    public static OptionsPanel CreateOptionsPanel(Transform parent)
    {
        var panel = CreatePanel("Painel Opções", parent, 560);
        var sensitivityText = CreateText("Sensibilidade (texto)", panel, "", 26);
        var sensitivitySlider = CreateSlider(panel, 0.2f, 3f);
        var volumeText = CreateText("Volume (texto)", panel, "", 26);
        var volumeSlider = CreateSlider(panel, 0f, 1f);
        var fullscreenToggle = CreateToggle(panel, "Tela cheia");
        var backButton = CreateButton("Voltar", panel);

        var options = panel.gameObject.AddComponent<OptionsPanel>();
        var so = new SerializedObject(options);
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
        so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        so.FindProperty("sensitivityValueText").objectReferenceValue = sensitivityText;
        so.FindProperty("volumeValueText").objectReferenceValue = volumeText;
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.ApplyModifiedPropertiesWithoutUndo();
        return options;
    }

    // LayoutElement: diz ao VerticalLayoutGroup a altura que este item quer ter.
    public static void SetHeight(GameObject go, float height)
    {
        // Não use "??" com componentes da Unity: um componente que falta nem sempre é um null
        // "de verdade" para o C#. TryGetComponent é o jeito seguro de perguntar se existe.
        if (!go.TryGetComponent(out LayoutElement element)) element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }
}
