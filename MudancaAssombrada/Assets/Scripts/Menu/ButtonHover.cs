using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Clareia o texto do botão quando o mouse passa por cima (ou quando ele é selecionado
    // pelo teclado/controle). O fundo do botão já muda sozinho pelo ColorBlock; isso só cuida do texto.
    public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        static Color hoverTextColor => Theme.HighlightColor;

        Button button;
        Text label;
        Color normalTextColor;
        bool hovered;

        void Awake()
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<Text>();
            normalTextColor = label.color;
        }

        public void OnPointerEnter(PointerEventData _) => hovered = true;
        public void OnPointerExit(PointerEventData _) => hovered = false;
        public void OnSelect(BaseEventData _) => hovered = true;
        public void OnDeselect(BaseEventData _) => hovered = false;

        void OnDisable()
        {
            // Painel escondido com o mouse em cima: volta ao normal para não reaparecer "aceso".
            hovered = false;
            label.color = normalTextColor;
        }

        void Update()
        {
            bool active = hovered && button.interactable;
            if (active) label.color = hoverTextColor;
            else if (label.color == hoverTextColor) label.color = normalTextColor;
            else normalTextColor = label.color; // guarda a cor atual, caso o menu tenha trocado
        }
    }
}
