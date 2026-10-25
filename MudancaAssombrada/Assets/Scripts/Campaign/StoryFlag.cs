using UnityEngine;

namespace MudancaAssombrada
{
    // Grava um MARCADOR DA HISTÓRIA no save quando chamado (ex.: "achou-cartaz-teatro").
    // Ligue o método Set no "On Used" de um Interactable, sem código: como o On Used roda em
    // TODOS os computadores, cada jogador grava a pista no PRÓPRIO save.
    // Quem lê os marcadores: o final alternativo (Fase 6) e as pistas do Seu Valdemar.
    public class StoryFlag : MonoBehaviour
    {
        [Tooltip("Nome do marcador no save. Use letras minúsculas e hífens.")]
        [SerializeField] string flag = "achou-cartaz-teatro";

        public string Flag => flag;

        public void Set()
        {
            if (SaveSystem.Data.HasFlag(flag)) return;
            SaveSystem.Data.SetFlag(flag);
            SaveSystem.Save();
            Debug.Log($"Pista da história encontrada: {flag}");
        }
    }
}
