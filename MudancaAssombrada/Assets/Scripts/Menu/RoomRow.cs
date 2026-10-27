using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Uma linha da lista de salas (tela "Entrar numa sala"):
    //   [Nome da sala / Líder Zé · Capítulo 1 · Casa do Espelho]  ● ●  2/3  [ENTRAR]
    // O construtor do menu cria 4 linhas (uma página) e o MainMenu só troca o que elas mostram:
    // nada é criado nem destruído a cada atualização, então a lista não pisca.
    public class RoomRow : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Text infoText;
        [SerializeField] Text slotsText;
        [SerializeField] Image[] dots;      // uma bolinha por personagem já escolhido
        [SerializeField] Button joinButton;
        [SerializeField] CanvasGroup group; // sala cheia: a linha toda fica apagada

        public Button JoinButton => joinButton;

        public void Show(string roomName, string info, string slots, IReadOnlyList<Color> dotColors, bool full, bool busy)
        {
            gameObject.SetActive(true);
            nameText.text = roomName;
            infoText.text = info;
            slotsText.text = slots;
            for (int i = 0; i < dots.Length; i++)
            {
                bool used = i < dotColors.Count;
                dots[i].gameObject.SetActive(used);
                if (used) dots[i].color = dotColors[i];
            }
            group.alpha = full ? 0.4f : 1f;
            joinButton.interactable = !full && !busy;
            joinButton.GetComponentInChildren<Text>().text = full ? "Cheia" : "Entrar";
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
