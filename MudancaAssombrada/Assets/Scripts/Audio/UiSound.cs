using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Som dos botões: clique abafado ao apertar e um "tic" quase inaudível com o mouse em cima
    // (DESIGN.md, "Som da interface"). O UiBuilder coloca em todo botão que cria.
    public class UiSound : MonoBehaviour, IPointerEnterHandler
    {
        Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            if (selectable is Button button) button.onClick.AddListener(() => GameAudio.PlayUi(SoundLibrary.UiClick));
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (selectable == null || selectable.interactable) GameAudio.PlayUi(SoundLibrary.UiHover);
        }
    }
}
