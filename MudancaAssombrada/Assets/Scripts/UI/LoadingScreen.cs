using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // Tela de carregamento: cobre a troca de cena (Menu -> Fase) para ninguém ver a cena
    // "montando" (céu azul padrão, luz ainda não ajustada, boneco ainda não nascido).
    //
    //   LoadingScreen.Show("A Casa do Espelho");  -> cobre a tela na hora
    //   ... a fase carrega, o MEU boneco nasce ...
    //   some sozinha com um fade, um instante depois (para a iluminação já estar no lugar)
    //
    // Ela não pertence a nenhuma cena: é criada por código e marcada DontDestroyOnLoad,
    // então sobrevive à troca de cena (senão sumiria justamente na hora em que precisamos dela).
    public class LoadingScreen : MonoBehaviour
    {
        const float HideDelay = 0.4f;   // espera depois de o boneco nascer (a luz se ajusta)
        const float FadeTime = Theme.FadeSlow;
        const float Timeout = 30f;      // nunca fica presa: some sozinha depois disso

        static LoadingScreen instance;

        CanvasGroup group;
        Text titleText;
        Text statusText;
        float shownAt;
        float hideAt = -1f; // < 0 = ainda esperando o boneco nascer

        // Com o "Play rápido" do editor, valores static sobrevivem entre um Play e outro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        public static bool IsVisible => instance != null && instance.gameObject.activeSelf;

        public static void Show(string title)
        {
            if (instance == null) instance = Create();
            instance.titleText.text = title;
            instance.group.alpha = 1f;
            instance.shownAt = Time.unscaledTime;
            instance.hideAt = -1f;
            instance.gameObject.SetActive(true);
        }

        // Some na hora (ex.: voltou para o menu por um erro de conexão).
        public static void HideNow()
        {
            if (instance != null) instance.gameObject.SetActive(false);
        }

        void OnEnable() => NetworkPlayer.LocalSpawned += OnLocalSpawned;
        void OnDisable() => NetworkPlayer.LocalSpawned -= OnLocalSpawned;

        void OnLocalSpawned(NetworkPlayer player) => hideAt = Time.unscaledTime + HideDelay;

        // unscaledTime: o relógio "de verdade" (continua andando mesmo com o jogo congelado).
        void Update()
        {
            float now = Time.unscaledTime;
            if (hideAt < 0 && now - shownAt > Timeout) hideAt = now; // algo deu errado: não trava a tela

            // "Carregando", "Carregando.", "Carregando..": mostra que o jogo não travou.
            int dots = (int)(now * 3f) % 4;
            statusText.text = "Carregando" + new string('.', dots);

            if (hideAt < 0 || now < hideAt) return;
            group.alpha = 1f - (now - hideAt) / FadeTime;
            if (group.alpha <= 0f) gameObject.SetActive(false);
        }

        // Monta a tela por código (como o UiBuilder faz no editor, mas aqui no jogo rodando).
        static LoadingScreen Create()
        {
            var root = new GameObject("Tela de Carregamento", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            DontDestroyOnLoad(root);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // por cima de TUDO (menu, HUD, pausa)
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var background = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = Theme.BackgroundEdge; // quase preto

            var screen = root.AddComponent<LoadingScreen>();
            screen.group = root.GetComponent<CanvasGroup>();
            screen.group.blocksRaycasts = true; // ninguém clica no que está por baixo

            screen.titleText = CreateText(root.transform, GameFonts.Title, Theme.HeaderSize, Theme.TitleColor, new Vector2(0, 30));
            screen.statusText = CreateText(root.transform, GameFonts.Body, Theme.NoteSize, Theme.MutedColor, new Vector2(0, -30));
            return screen;
        }

        static Text CreateText(Transform parent, Font font, int size, Color color, Vector2 position)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(1600, size * 1.5f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
