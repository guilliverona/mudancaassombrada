using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Os estados de um botão (ou campo) do jogo:
    //   - fundo: pinta a Image com a cor de cada estado (normal, mouse em cima, apertado, desligado);
    //   - mouse em cima (ou selecionado pelo teclado/controle): texto e contorno em ectoplasma;
    //   - apertado: a sombra dura some, como se o botão descesse o degrau;
    //   - wobble ligado (só no menu): o botão balança um pouquinho (Theme.HoverTilt).
    // Na partida (pausa) o wobble fica desligado: lá nada balança (DESIGN.md, "Movimento").
    //
    // Por que não o ColorBlock da Unity? Ele tinge a imagem INTEIRA pelo CanvasRenderer, e isso
    // escurece junto o contorno (Outline) e a sombra: a borda clara sumia nos botões escuros.
    public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Clareia o texto no hover. O botão de papelão e os campos desligam.")]
        public bool lightenText = true;
        [Tooltip("Balança no hover. Só nos botões do menu.")]
        public bool wobble;

        [Header("Cores do fundo")]
        public Color normalColor = Color.white;
        public Color hoverColor = Color.white;
        public Color pressedColor = Color.white;
        public Color disabledColor = Color.white;
        public Color selectedColor = Color.white; // opção escolhida (ex.: "Pública" na tela Criar sala)

        // Opção escolhida: fica "acesa" (contorno e texto em ectoplasma) mesmo sem o mouse em cima.
        public bool Selected { get; set; }

        static Color hoverTextColor => Theme.HighlightColor;

        Selectable selectable;
        Image background;
        Text label;          // o primeiro texto (o rótulo; o subtítulo vem depois)
        Color normalTextColor;
        Outline outline;     // contorno fino (acende no hover)
        Shadow step;         // a sombra dura de baixo (some ao apertar)
        Color normalOutlineColor;
        Vector2 normalStep;
        bool hovered, pressed, lit;
        float tilt;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            background = GetComponent<Image>();
            label = GetComponentInChildren<Text>();
            if (label != null) normalTextColor = label.color;
            // Outline É um Shadow (herda dele), então separamos os dois pelo tipo.
            foreach (var effect in GetComponents<Shadow>())
            {
                if (effect is Outline o) outline = o;
                else step = effect;
            }
            if (outline != null) normalOutlineColor = outline.effectColor;
            if (step != null) normalStep = step.effectDistance;
            if (background != null) background.color = normalColor;
        }

        public void OnPointerEnter(PointerEventData _) => hovered = true;
        public void OnPointerExit(PointerEventData _) { hovered = false; pressed = false; }
        public void OnSelect(BaseEventData _) => hovered = true;
        public void OnDeselect(BaseEventData _) => hovered = false;
        public void OnPointerDown(PointerEventData _) => pressed = true;
        public void OnPointerUp(PointerEventData _) => pressed = false;

        void OnDisable()
        {
            // Painel escondido com o mouse em cima: volta ao normal para não reaparecer "aceso".
            hovered = pressed = false;
            if (lightenText && label != null) label.color = normalTextColor;
            SetLit(false);
            if (step != null) step.effectDistance = normalStep;
            if (background != null) background.color = normalColor;
            tilt = 0;
            transform.localRotation = Quaternion.identity;
        }

        void Update()
        {
            bool usable = selectable == null || selectable.IsInteractable();
            bool active = hovered && usable;
            bool lightUp = active || (Selected && usable);

            if (background != null)
            {
                var target = !usable ? disabledColor : pressed && active ? pressedColor : active ? hoverColor
                    : Selected ? selectedColor : normalColor;
                if (background.color != target)
                    background.color = Vector4.MoveTowards(background.color, target, Time.unscaledDeltaTime / Theme.FadeFast);
            }

            if (lightenText && label != null)
            {
                if (lightUp) label.color = hoverTextColor;
                else if (label.color == hoverTextColor) label.color = normalTextColor;
                else normalTextColor = label.color; // guarda a cor atual, caso o menu tenha trocado
            }

            // Só mexe nos efeitos quando o estado muda: cada troca redesenha o botão.
            if (lightUp != lit) SetLit(lightUp);
            if (step != null)
            {
                var distance = pressed && active ? Vector2.zero : normalStep;
                if (step.effectDistance != distance) step.effectDistance = distance;
            }

            if (wobble)
            {
                // Rotação: o layout dos painéis não mexe nela (só em posição e tamanho).
                float target = active ? Theme.HoverTilt : 0f;
                if (!Mathf.Approximately(tilt, target))
                {
                    float speed = Mathf.Abs(Theme.HoverTilt) / Theme.FadeFast; // chega lá em FadeFast segundos
                    tilt = Mathf.MoveTowards(tilt, target, speed * Time.unscaledDeltaTime);
                    transform.localRotation = Theme.Tilt(tilt);
                }
            }
        }

        void SetLit(bool value)
        {
            lit = value;
            if (outline != null) outline.effectColor = value ? Theme.HighlightColor : normalOutlineColor;
        }
    }
}
