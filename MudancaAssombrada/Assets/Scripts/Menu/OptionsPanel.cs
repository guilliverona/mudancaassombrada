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

        // Quem abriu o painel escuta isto para saber quando voltar à tela anterior.
        public event Action Closed;

        void Awake()
        {
            sensitivitySlider.onValueChanged.AddListener(v => { GameSettings.MouseSensitivity = v; RefreshLabels(); });
            volumeSlider.onValueChanged.AddListener(v => { GameSettings.Volume = v; GameSettings.Apply(); RefreshLabels(); });
            fullscreenToggle.onValueChanged.AddListener(v => { GameSettings.Fullscreen = v; GameSettings.Apply(); });
            backButton.onClick.AddListener(() => { GameSettings.Save(); Closed?.Invoke(); });
        }

        // OnEnable roda toda vez que o painel aparece: mostra sempre os valores salvos atuais.
        void OnEnable()
        {
            sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            RefreshLabels();
        }

        void RefreshLabels()
        {
            sensitivityValueText.text = $"Sensibilidade do mouse: {GameSettings.MouseSensitivity:0.0}x";
            volumeValueText.text = $"Volume: {GameSettings.Volume * 100:0}%";
        }
    }
}
