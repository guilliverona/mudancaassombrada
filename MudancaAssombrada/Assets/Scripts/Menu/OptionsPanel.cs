using System;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // O painel de Opções. Fica num script próprio porque aparece em DOIS lugares:
    // no menu principal e no menu de pausa. Escrever a lógica uma vez só evita duas
    // cópias que, com o tempo, ficariam diferentes.
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] Slider sensitivitySlider;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Toggle fullscreenToggle;
        [SerializeField] Text sensitivityValueText;
        [SerializeField] Text volumeValueText;
        [SerializeField] Button backButton;

        [Header("Vídeo")]
        // Botões de opção: cada clique passa para a próxima escolha (Baixa → Média → Alta → Baixa...).
        [SerializeField] Button qualityButton;
        [SerializeField] Button antialiasingButton;
        [SerializeField] Button fpsButton;
        [SerializeField] Slider renderScaleSlider;
        [SerializeField] Text renderScaleValueText;
        [SerializeField] Toggle vsyncToggle;
        [SerializeField] Button resolutionButton;
        [SerializeField] Slider brightnessSlider;
        [SerializeField] Text brightnessValueText;

        [Header("Abas")]
        // Cada botão de aba mostra a página de mesmo índice (aba 0 → página 0...).
        // O marcador (filho "Marcador" do botão) é a faixa em ectoplasma embaixo da aba aberta.
        [SerializeField] Button[] tabButtons;
        [SerializeField] GameObject[] tabPages;

        // Quem abriu o painel escuta isto para saber quando voltar à tela anterior.
        public event Action Closed;

        void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tab = i; // cópia: o "i" do for muda, e o clique precisa lembrar o valor de agora
                tabButtons[i].onClick.AddListener(() => ShowTab(tab));
            }

            sensitivitySlider.onValueChanged.AddListener(v => { GameSettings.MouseSensitivity = v; RefreshLabels(); });
            volumeSlider.onValueChanged.AddListener(v => { GameSettings.Volume = v; GameSettings.Apply(); RefreshLabels(); });
            fullscreenToggle.onValueChanged.AddListener(v => { GameSettings.Fullscreen = v; GameSettings.Apply(); });
            backButton.onClick.AddListener(() => { GameSettings.Save(); Closed?.Invoke(); });

            qualityButton.onClick.AddListener(() => ChangeVideo(() =>
                GameSettings.Quality = VideoRules.Next(GameSettings.Quality, VideoRules.QualityNames.Length)));
            antialiasingButton.onClick.AddListener(() => ChangeVideo(() =>
                GameSettings.Antialiasing = VideoRules.Next(GameSettings.Antialiasing, VideoRules.AaNames.Length)));
            fpsButton.onClick.AddListener(() => ChangeVideo(() =>
                GameSettings.FpsLimit = VideoRules.Next(GameSettings.FpsLimit, VideoRules.FpsNames.Length)));
            // De 5 em 5%: o slider vai de 10 a 20 e a escala é valor/20 (0,5 a 1).
            renderScaleSlider.wholeNumbers = true;
            renderScaleSlider.onValueChanged.AddListener(v => ChangeVideo(() => GameSettings.RenderScale = v / 20f));
            vsyncToggle.onValueChanged.AddListener(v => ChangeVideo(() => GameSettings.VSync = v));
            // De 0,1 em 0,1 EV: o slider vai de -10 a 10 e o brilho é valor/10.
            brightnessSlider.wholeNumbers = true;
            brightnessSlider.onValueChanged.AddListener(v => ChangeVideo(() => GameSettings.Brightness = v / 10f));
            resolutionButton.onClick.AddListener(NextResolution);
        }

        void NextResolution()
        {
            var sizes = VideoSettings.Resolutions();
            if (sizes.Length == 0) return;
            int index = VideoRules.ResolutionIndex(sizes, GameSettings.Resolution);
            GameSettings.Resolution = sizes[VideoRules.Next(index, sizes.Length)];
            GameSettings.Apply();
            RefreshLabels();
        }

        // Toda mudança de vídeo: guarda, aplica na hora e atualiza os textos.
        void ChangeVideo(Action change)
        {
            change();
            VideoSettings.Apply();
            RefreshLabels();
        }

        // OnEnable roda toda vez que o painel aparece: mostra sempre os valores salvos atuais.
        void OnEnable()
        {
            sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            renderScaleSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.RenderScale * 20f));
            vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            brightnessSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.Brightness * 10f));
            ShowTab(0); // abre sempre na primeira aba
            RefreshLabels();
        }

        void ShowTab(int tab)
        {
            for (int i = 0; i < tabPages.Length; i++)
            {
                tabPages[i].SetActive(i == tab);
                var marker = tabButtons[i].transform.Find("Marcador");
                if (marker != null) marker.gameObject.SetActive(i == tab);
            }
        }

        void RefreshLabels()
        {
            sensitivityValueText.text = $"Sensibilidade do mouse: {GameSettings.MouseSensitivity:0.0}x";
            volumeValueText.text = $"Volume: {GameSettings.Volume * 100:0}%";

            float scale = GameSettings.RenderScale;
            renderScaleValueText.text = VideoRules.UsesStp(scale)
                ? $"Resolução 3D: {scale * 100:0}% (STP)"
                : "Resolução 3D: 100%";
            SetLabel(qualityButton, "Qualidade: " + VideoRules.QualityNames[GameSettings.Quality]);
            // Com STP o antialiasing escolhido não vale (o STP já suaviza): o botão avisa.
            SetLabel(antialiasingButton, VideoRules.UsesStp(scale)
                ? "Antialiasing: STP"
                : "Antialiasing: " + VideoRules.AaNames[GameSettings.Antialiasing]);
            antialiasingButton.interactable = !VideoRules.UsesStp(scale);
            SetLabel(fpsButton, "Limite de FPS: " + (GameSettings.VSync ? "VSync" : VideoRules.FpsNames[GameSettings.FpsLimit]));
            fpsButton.interactable = !GameSettings.VSync;
            brightnessValueText.text = $"Brilho: {VideoRules.BrightnessPercent(GameSettings.Brightness)}%";

            var sizes = VideoSettings.Resolutions();
            int index = VideoRules.ResolutionIndex(sizes, GameSettings.Resolution);
            // No Editor a lista pode vir vazia (quem manda é a janela Game): mostra o tamanho atual.
            var size = index >= 0 ? sizes[index] : new Vector2Int(Screen.width, Screen.height);
            SetLabel(resolutionButton, $"Resolução: {size.x} × {size.y}");
        }

        // O rótulo é o primeiro texto do botão (como o UiBuilder monta).
        static void SetLabel(Button button, string label) => button.GetComponentInChildren<Text>().text = label;
    }
}
