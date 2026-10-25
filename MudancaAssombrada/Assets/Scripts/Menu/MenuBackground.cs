using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Fundo do menu: cinza-azulado escuro no meio que vai até quase preto nas bordas (vinheta).
    // A imagem é desenhada por código, então não precisa de arquivo.
    // Quando tivermos uma arte de fundo, é só trocar por uma Image com ela (a vinheta pode ficar por cima).
    [RequireComponent(typeof(RawImage))]
    public class MenuBackground : MonoBehaviour
    {
        Texture2D texture;

        void Awake()
        {
            var image = GetComponent<RawImage>();
            Color centerColor = Theme.BackgroundCenter, edgeColor = Theme.BackgroundEdge;
            const int size = 128; // pequena: a placa de vídeo estica e suaviza
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distância do centro: 0 no meio, ~1 nos cantos. O "brilho" fica um pouco acima do meio.
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.55f;
                float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 1.7f);
                pixels[y * size + x] = Color.Lerp(centerColor, edgeColor, t * t);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            image.texture = texture;
        }

        void OnDestroy() => Destroy(texture);
    }
}
