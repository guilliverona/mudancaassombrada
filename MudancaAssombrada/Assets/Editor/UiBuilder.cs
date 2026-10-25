using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MudancaAssombrada.Theme; // cores e tamanhos do padrão visual: "TextColor" em vez de "Theme.TextColor"

namespace MudancaAssombrada.Editor
{
    // Peças de interface (uGUI) usadas pelos construtores: Canvas, painéis, botões, textos...
    // REGRA (DESIGN.md): toda interface do jogo sai daqui. Se faltar uma peça, adicione aqui,
    // e todas as telas (menu, pausa, HUD) continuam com o mesmo visual.
    public static class UiBuilder
    {
        static Font font => GameFonts.Body; // sem guardar aqui: o GameFonts já cuida disso
        static DefaultControls.Resources resources;

        // Imagens padrão da Unity, as mesmas do menu GameObject > UI.
        static void LoadDefaults()
        {
            if (resources.standard != null) return;
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

        // Canvas: a "folha" onde a interface é desenhada, por cima do mundo 3D.
        // sortingOrder: canvas com número maior fica na frente (a pausa fica na frente da HUD).
        public static Transform CreateCanvas(string name, int sortingOrder = 10)
        {
            LoadDefaults();
            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            // Desenha pensando numa tela 1920x1080 e estica/encolhe para qualquer outra resolução.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo.transform;
        }

        // Um painel: empilha os filhos na vertical no meio da tela.
        // Com fundo, ganha uma caixa escura translúcida (para ler textos por cima da imagem);
        // sem fundo, os botões ficam "soltos" na tela, como no menu inicial.
        public static Transform CreatePanel(string name, Transform parent, float height = 820, float width = 760, bool background = true)
        {
            LoadDefaults();
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            // Um pouco abaixo do centro: o painel mais alto (820) vai de y=240 a y=1060 (tela 1080),
            // deixando o espaço de cima livre para o título do menu.
            rect.anchoredPosition = new Vector2(0, -110);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>();
            image.color = background ? PanelColor : Color.clear;
            image.raycastTarget = background;

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
            // Bebas Neue (a mesma do título): só tem maiúsculas, então "Jogar" vira "JOGAR" sozinho.
            text.font = GameFonts.Title;
            text.fontSize = ButtonSize;
            text.color = TextColor;
            SetHeight(go, 60);

            // Fundo quase transparente com borda fina. A imagem fica branca e quem dá a cor é o
            // ColorBlock: cada estado (normal, mouse em cima, apertado...) multiplica o branco pela sua cor.
            var button = go.GetComponent<Button>();
            go.GetComponent<Image>().color = Color.white;
            var colors = button.colors;
            colors.normalColor = new Color(0.05f, 0.06f, 0.065f, 0.55f);
            colors.highlightedColor = new Color(0.12f, 0.14f, 0.15f, 0.8f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.2f, 0.23f, 0.24f, 0.9f);
            colors.disabledColor = new Color(0.05f, 0.06f, 0.065f, 0.3f);
            colors.fadeDuration = FadeFast;
            button.colors = colors;
            go.AddComponent<Outline>().effectColor = BorderColor;
            go.AddComponent<ButtonHover>(); // clareia o texto com o mouse em cima
            return button;
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
            text.color = TextColor;
            text.supportRichText = true; // permite <color> e <b> no texto (usado nas listas)
            text.raycastTarget = false;  // texto não precisa receber clique (e assim não "rouba" o do botão)
            SetHeight(go, size + 16);
            return text;
        }

        // Um retângulo de cor (fundo de barra, quadradinho da tecla, mira...).
        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static InputField CreateInput(Transform parent, string placeholder)
        {
            LoadDefaults();
            var go = DefaultControls.CreateInputField(resources);
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<InputField>();
            foreach (var t in go.GetComponentsInChildren<Text>()) { t.font = font; t.fontSize = BodySize; }
            go.GetComponent<Image>().color = FieldColor;
            go.AddComponent<Outline>().effectColor = BorderColor;
            input.textComponent.color = HighlightColor;
            input.caretColor = HighlightColor;
            input.customCaretColor = true;
            input.selectionColor = WithAlpha(BorderColor, 0.4f);
            var placeholderText = (Text)input.placeholder;
            placeholderText.text = placeholder;
            placeholderText.color = WithAlpha(TextColor, 0.35f);
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
            // Peças criadas pelo DefaultControls: Background (trilho), Fill (parte cheia) e Handle (bolinha).
            go.transform.Find("Background").GetComponent<Image>().color = FieldColor;
            go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = TextColor * 0.6f;
            go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color = HighlightColor;
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
            text.fontSize = BodySize;
            text.color = TextColor;
            go.transform.Find("Background").GetComponent<Image>().color = FieldColor;
            go.transform.Find("Background/Checkmark").GetComponent<Image>().color = HighlightColor;
            SetHeight(go, 40);
            return go.GetComponent<Toggle>();
        }

        // O painel de Opções completo, já ligado ao script OptionsPanel (usado no Menu e na pausa).
        public static OptionsPanel CreateOptionsPanel(Transform parent)
        {
            var panel = CreatePanel("Painel Opções", parent, 560);
            var sensitivityText = CreateText("Sensibilidade (texto)", panel, "", BodySize);
            var sensitivitySlider = CreateSlider(panel, 0.2f, 3f);
            var volumeText = CreateText("Volume (texto)", panel, "", BodySize);
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

        // Posiciona um elemento solto na tela (fora de painéis com layout).
        // anchor: em que ponto da TELA ele se prende (0,0 = canto inferior esquerdo; 1,1 = superior direito).
        // pivot: que ponto DELE fica no lugar. position: deslocamento a partir da âncora. size: tamanho.
        public static void Place(Component element, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)element.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Estica um elemento para ocupar todo o pai (ex.: fundo escuro da tela de fim de partida).
        public static void Stretch(Component element)
        {
            var rect = (RectTransform)element.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
}
