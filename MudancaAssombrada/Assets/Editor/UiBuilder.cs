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
            if (background) AddCutLine(go.transform);

            // VerticalLayoutGroup: organiza os filhos um embaixo do outro, sem precisar posicionar cada um.
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(50, 50, background ? 48 : 40, 40); // em cima: espaço da linha de corte
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return go.transform;
        }

        // Os três tipos de botão (DESIGN.md, "Botão"):
        //   Dark: o padrão, escuro com contorno fino;
        //   Cardboard: a etiqueta de papelão, a ação PRINCIPAL da tela (só um por tela);
        //   Ghost: voltar/cancelar, quase invisível até o mouse passar.
        public enum ButtonStyle { Dark, Cardboard, Ghost }

        // subtitle: uma linha escrita à mão embaixo do rótulo ("turno da madrugada").
        // É só a graça: nunca coloque ali algo que o jogador precisa ler.
        public static Button CreateButton(string label, Transform parent, ButtonStyle style = ButtonStyle.Dark, string subtitle = null)
        {
            LoadDefaults();
            bool paper = style == ButtonStyle.Cardboard;
            var go = DefaultControls.CreateButton(resources);
            go.name = "Botão " + label;
            go.transform.SetParent(parent, false);
            // O rótulo é o PRIMEIRO texto do botão: o MainMenu e o ButtonHover procuram ele assim.
            var text = go.GetComponentInChildren<Text>();
            text.text = label;
            // Bebas Neue: só tem maiúsculas, então "Jogar" vira "JOGAR" sozinho.
            text.font = GameFonts.Title;
            text.fontSize = ButtonSize;
            text.color = paper ? CardboardInkColor : TextColor;
            SetHeight(go, subtitle == null ? 64 : 100);

            if (subtitle != null)
            {
                // Rótulo na parte de cima, subtítulo embaixo.
                var labelRect = text.rectTransform;
                labelRect.anchorMin = new Vector2(0, 0.4f);
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                var sub = CreateText("Subtítulo", go.transform, subtitle, NoteSize);
                sub.font = GameFonts.Hand;
                sub.color = paper ? CardboardInkColor : MutedColor;
                var subRect = sub.rectTransform;
                subRect.anchorMin = Vector2.zero;
                subRect.anchorMax = new Vector2(1, 0.45f);
                subRect.offsetMin = new Vector2(0, 10);
                subRect.offsetMax = Vector2.zero;
            }

            // As cores do fundo ficam no ButtonHover (e não no ColorBlock da Unity, que escurecia a borda junto).
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            var hover = go.AddComponent<ButtonHover>(); // fundo, texto, contorno e (no menu) o balanço
            hover.lightenText = !paper;                 // no papelão o texto fica sempre escuro
            switch (style)
            {
                case ButtonStyle.Cardboard:
                    SetColors(hover, CardboardColor, CardboardLightColor, CardboardLightColor, WithAlpha(CardboardColor, 0.4f));
                    break;
                case ButtonStyle.Ghost:
                    SetColors(hover, WithAlpha(RaisedColor, 0f), WithAlpha(RaisedColor, 0.7f), RaisedColor, WithAlpha(RaisedColor, 0f));
                    break;
                default:
                    SetColors(hover, WithAlpha(PanelColor, 1f), RaisedColor, FieldColor, WithAlpha(PanelColor, 0.4f));
                    break;
            }
            go.GetComponent<Image>().color = hover.normalColor;

            // Sombra dura de "brinquedo" (o papelão tem o degrau inteiro; o escuro, um degrau baixo).
            if (style != ButtonStyle.Ghost)
            {
                var step = go.AddComponent<Shadow>();
                step.effectColor = ShadowColor;
                step.effectDistance = paper ? StepShadow : new Vector2(0, -2);
            }
            if (style == ButtonStyle.Ghost)
            {
                // O fantasma tem fundo transparente: o Outline da Unity desenha cópias do retângulo
                // INTEIRO por baixo dele, e elas apareciam como um bloco cinza. Aqui a moldura são
                // quatro tiras finas de verdade.
                AddFrame(go.transform, WithAlpha(BorderColor, 0.6f), 2f);
            }
            else
            {
                // Contorno de 2px: acende em ectoplasma no hover (ButtonHover). useGraphicAlpha desligado
                // para a cor da borda não depender da transparência do fundo.
                var outline = go.AddComponent<Outline>();
                outline.effectColor = paper ? CardboardDarkColor : BorderColor;
                outline.effectDistance = new Vector2(2, -2);
                outline.useGraphicAlpha = false;
            }

            if (paper) AddTape(go.transform);

            go.AddComponent<UiSound>();                 // clique abafado e o "tic" do mouse em cima
            return button;
        }

        static void SetColors(ButtonHover hover, Color normal, Color highlighted, Color pressed, Color disabled)
        {
            hover.normalColor = normal;
            hover.hoverColor = highlighted;
            hover.pressedColor = pressed;
            hover.disabledColor = disabled;
        }

        // Um pedaço de fita adesiva atravessado no canto de cima, à esquerda.
        static void AddTape(Transform parent)
        {
            var tape = CreateImage("Fita", parent, TapeColor);
            var rect = tape.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(4, -2);
            rect.sizeDelta = new Vector2(60, 20);
            rect.localRotation = Tilt(-28f);
        }

        // Bilhete do Seu Valdemar: papel amarelo torto, preso com fita, assinado "— V.".
        // Devolve o TEXTO (o MainMenu troca ele); o papel é o pai dele (text.transform.parent).
        // Não entra no layout dos painéis: posicione com Place no papel.
        public static Text CreateNote(Transform parent, string content, float tilt)
        {
            var paper = CreateImage("Bilhete", parent, NoteColor);
            paper.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            paper.rectTransform.localRotation = Tilt(tilt);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = StepShadow;

            var text = CreateText("Texto", paper.transform, content, HandSize);
            text.font = GameFonts.Hand;
            text.color = NoteInkColor;
            text.alignment = TextAnchor.UpperLeft;
            Stretch(text);
            text.rectTransform.offsetMin = new Vector2(28, 56);  // embaixo fica a assinatura
            text.rectTransform.offsetMax = new Vector2(-24, -40);

            var sign = CreateText("Assinatura", paper.transform, "— V.", HandSize);
            sign.font = GameFonts.Hand;
            sign.color = NoteInkColor;
            sign.alignment = TextAnchor.LowerRight;
            Stretch(sign);
            sign.rectTransform.offsetMin = new Vector2(28, 18);
            sign.rectTransform.offsetMax = new Vector2(-28, -40);

            var tape = CreateImage("Fita", paper.transform, TapeColor);
            var tapeRect = tape.rectTransform;
            tapeRect.anchorMin = tapeRect.anchorMax = new Vector2(0.5f, 1);
            tapeRect.anchoredPosition = new Vector2(0, -2);
            tapeRect.sizeDelta = new Vector2(130, 34);
            tapeRect.localRotation = Tilt(-4f);
            return text;
        }

        // Carimbo: o cabeçalho dos painéis, em papelão, com moldura e um pouco torto.
        public static Text CreateStamp(string label, Transform parent)
        {
            // "holder" ocupa a linha do painel; a moldura dentro dele tem só o tamanho do texto.
            var holder = new GameObject("Carimbo", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            holder.transform.SetParent(parent, false);
            var holderLayout = holder.GetComponent<HorizontalLayoutGroup>();
            holderLayout.childAlignment = TextAnchor.MiddleLeft;
            holderLayout.childControlWidth = holderLayout.childControlHeight = true;
            holderLayout.childForceExpandWidth = holderLayout.childForceExpandHeight = false;
            SetHeight(holder, HeaderSize + 24);

            var frame = new GameObject("Moldura", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            frame.transform.SetParent(holder.transform, false);
            frame.transform.localRotation = Tilt(-1f);
            var frameLayout = frame.GetComponent<HorizontalLayoutGroup>();
            frameLayout.padding = new RectOffset(18, 18, 6, 2);
            frameLayout.childControlWidth = frameLayout.childControlHeight = true;
            frameLayout.childForceExpandWidth = frameLayout.childForceExpandHeight = false;

            var text = CreateText("Texto", frame.transform, label, HeaderSize);
            text.font = GameFonts.Title;
            text.color = CardboardColor;

            AddFrame(frame.transform, CardboardColor, 3f);
            return text;
        }

        // Moldura: quatro tiras finas presas nos lados (fora do layout).
        static void AddFrame(Transform parent, Color color, float line)
        {
            AddEdge(parent, "Borda de cima", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, line), color);
            AddEdge(parent, "Borda de baixo", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, line), color);
            AddEdge(parent, "Borda esquerda", new Vector2(0, 0), new Vector2(0, 1), new Vector2(line, 0), color);
            AddEdge(parent, "Borda direita", new Vector2(1, 0), new Vector2(1, 1), new Vector2(line, 0), color);
        }

        static void AddEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
        {
            var edge = CreateImage(name, parent, color);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        // A linha tracejada de "corte aqui" no topo de um painel (como numa caixa de papelão).
        static void AddCutLine(Transform panel)
        {
            var go = new GameObject("Linha de corte", typeof(RectTransform), typeof(RawImage), typeof(DashedLine), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = go.GetComponent<RawImage>();
            image.color = BorderColor;
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(30, -18);
            rect.offsetMax = new Vector2(-30, -16);
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
            // Fundo e contorno pelo ButtonHover (como no botão): um pouco mais claro e contorno
            // em ectoplasma com o mouse em cima ou digitando. Campo não balança nem clareia o texto.
            input.transition = Selectable.Transition.None;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2, -2);
            outline.useGraphicAlpha = false;
            var hover = go.AddComponent<ButtonHover>();
            hover.lightenText = false;
            SetColors(hover, FieldColor, RaisedColor, RaisedColor, WithAlpha(FieldColor, 0.4f));
            go.GetComponent<Image>().color = FieldColor;
            input.textComponent.color = HighlightColor;
            input.caretColor = HighlightColor;
            input.customCaretColor = true;
            input.selectionColor = WithAlpha(BorderColor, 0.4f);
            var placeholderText = (Text)input.placeholder;
            placeholderText.text = placeholder;
            placeholderText.color = MutedColor;
            placeholderText.fontStyle = FontStyle.Italic;
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
            var panel = CreatePanel("Painel Opções", parent, 820);
            var sensitivityText = CreateText("Sensibilidade (texto)", panel, "", BodySize);
            var sensitivitySlider = CreateSlider(panel, 0.2f, 3f);
            var volumeText = CreateText("Volume (texto)", panel, "", BodySize);
            var volumeSlider = CreateSlider(panel, 0f, 1f);
            var fullscreenToggle = CreateToggle(panel, "Tela cheia");
            // Vídeo: os rótulos dos botões são reescritos pelo OptionsPanel ("Qualidade: Alta"...).
            var qualityButton = CreateButton("Qualidade", panel);
            var antialiasingButton = CreateButton("Antialiasing", panel);
            var renderScaleText = CreateText("Resolução 3D (texto)", panel, "", BodySize);
            var renderScaleSlider = CreateSlider(panel, 10f, 20f); // 50% a 100%, de 5 em 5
            var vsyncToggle = CreateToggle(panel, "VSync");
            var fpsButton = CreateButton("Limite de FPS", panel);
            var backButton = CreateButton("Voltar", panel, ButtonStyle.Ghost);

            var options = panel.gameObject.AddComponent<OptionsPanel>();
            var so = new SerializedObject(options);
            so.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
            so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
            so.FindProperty("sensitivityValueText").objectReferenceValue = sensitivityText;
            so.FindProperty("volumeValueText").objectReferenceValue = volumeText;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("qualityButton").objectReferenceValue = qualityButton;
            so.FindProperty("antialiasingButton").objectReferenceValue = antialiasingButton;
            so.FindProperty("renderScaleSlider").objectReferenceValue = renderScaleSlider;
            so.FindProperty("renderScaleValueText").objectReferenceValue = renderScaleText;
            so.FindProperty("vsyncToggle").objectReferenceValue = vsyncToggle;
            so.FindProperty("fpsButton").objectReferenceValue = fpsButton;
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
