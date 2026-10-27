using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Um fantasma de lençol, bem apagado, que atravessa o fundo do menu de vez em quando.
    // É a única coisa do menu que se mexe sozinha (DESIGN.md, "Movimento"), então precisa ser
    // rara e discreta: se chamar mais atenção que os botões, abaixe "alpha".
    // O desenho é feito por código (como o MenuBackground): não precisa de arquivo.
    [RequireComponent(typeof(RawImage))]
    public class MenuGhost : MonoBehaviour
    {
        [Tooltip("Opacidade máxima no meio da travessia.")]
        [SerializeField, Range(0f, 0.3f)] float alpha = 0.07f;
        [Tooltip("Espera entre uma passada e outra, em segundos (sorteada entre os dois).")]
        [SerializeField] Vector2 waitRange = new Vector2(20f, 40f);
        [Tooltip("Quanto tempo leva para atravessar a tela.")]
        [SerializeField] float crossTime = 14f;

        RawImage image;
        RectTransform rect;
        Texture2D texture;
        float nextPass;
        float progress = -1f; // -1 = parado, fora da tela; 0..1 = atravessando
        float direction, height;

        void Awake()
        {
            image = GetComponent<RawImage>();
            rect = (RectTransform)transform;
            texture = DrawGhost();
            image.texture = texture;
            image.raycastTarget = false;
            SetAlpha(0);
            nextPass = Time.unscaledTime + Random.Range(3f, 8f); // a primeira passada vem logo
        }

        void OnDestroy() => Destroy(texture);

        void Update()
        {
            if (progress < 0f)
            {
                if (Time.unscaledTime < nextPass) return;
                progress = 0f;
                direction = Random.value < 0.5f ? 1f : -1f;
                height = Random.Range(-60f, 160f); // em torno do meio, atrás do título e dos botões
            }

            progress += Time.unscaledDeltaTime / crossTime;
            if (progress >= 1f)
            {
                progress = -1f;
                SetAlpha(0);
                nextPass = Time.unscaledTime + Random.Range(waitRange.x, waitRange.y);
                return;
            }

            // Vai de um lado ao outro da tela, subindo e descendo de leve (como quem flutua).
            float width = ((RectTransform)rect.parent).rect.width;
            float margin = rect.rect.width;
            float t = direction > 0 ? progress : 1f - progress;
            float x = Mathf.Lerp(-width / 2f - margin, width / 2f + margin, t);
            float y = height + Mathf.Sin(progress * Mathf.PI * 6f) * 12f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = new Vector3(direction, 1f, 1f); // olha para onde vai
            SetAlpha(alpha * Mathf.Sin(progress * Mathf.PI)); // aparece e some devagar
        }

        void SetAlpha(float a) => image.color = Theme.WithAlpha(Theme.TextColor, a);

        // Lençol: cabeça redonda, corpo reto, barra ondulada e dois furos de olho.
        static Texture2D DrawGhost()
        {
            const int w = 96, h = 112;
            const float cx = 48f, radius = 40f, headY = 64f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float dx = px - cx;
                bool head = py >= headY && dx * dx + (py - headY) * (py - headY) <= radius * radius;
                // Barra: três ondas ao longo da largura.
                float hem = 6f + 6f * (0.5f + 0.5f * Mathf.Sin((px - 8f) / 80f * Mathf.PI * 6f));
                bool body = py < headY && Mathf.Abs(dx) <= radius && py >= hem;
                bool eye = Hole(px, py, 34f, 66f) || Hole(px, py, 62f, 66f);
                pixels[y * w + x] = (head || body) && !eye ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static bool Hole(float x, float y, float cx, float cy) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= 36f;
    }
}
