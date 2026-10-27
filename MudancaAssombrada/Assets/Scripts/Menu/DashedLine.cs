using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Linha tracejada de "corte aqui", como a de uma caixa de papelão (topo dos painéis).
    // A textura tem 2 pixels (um branco, um transparente) e se repete ao longo da linha;
    // a cor vem do RawImage. Desenhada por código: não precisa de arquivo de imagem.
    [RequireComponent(typeof(RawImage))]
    public class DashedLine : MonoBehaviour
    {
        [Tooltip("Comprimento de cada traço (e de cada espaço), em pixels da tela 1920x1080.")]
        [SerializeField] float dash = 12f;

        static Texture2D texture; // uma só para todas as linhas

        RawImage image;

        void Awake()
        {
            image = GetComponent<RawImage>();
            if (texture == null)
            {
                texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point, // borda seca, sem degradê entre traço e espaço
                    hideFlags = HideFlags.DontSave,
                };
                texture.SetPixels(new[] { Color.white, Color.clear });
                texture.Apply();
            }
            image.texture = texture;
            UpdateRepeat();
        }

        // Chamado pela Unity quando o tamanho muda (ex.: outra resolução).
        void OnRectTransformDimensionsChange()
        {
            if (image != null) UpdateRepeat();
        }

        // Quantas vezes o par traço+espaço cabe na largura.
        void UpdateRepeat()
        {
            float width = ((RectTransform)transform).rect.width;
            image.uvRect = new Rect(0, 0, Mathf.Max(1f, width / (dash * 2f)), 1);
        }
    }
}
