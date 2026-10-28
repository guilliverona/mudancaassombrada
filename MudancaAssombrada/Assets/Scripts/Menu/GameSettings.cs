using UnityEngine;

namespace MudancaAssombrada
{
    // Configurações do jogador, salvas entre uma partida e outra.
    // "static" = existe uma só, acessível de qualquer script: GameSettings.Volume.
    //
    // PlayerPrefs é a "gaveta" da Unity para guardar coisas pequenas no computador
    // (no Windows, fica no Registro). Guarda números, textos, e só.
    public static class GameSettings
    {
        const string SensitivityKey = "sensibilidade";
        const string VolumeKey = "volume";
        const string FullscreenKey = "telaCheia";
        const string NameKey = "nome";

        // Multiplicador da sensibilidade do mouse: 1 = padrão, 0.5 = metade, 2 = dobro.
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f); // o 1f é o valor padrão (se nunca salvou)
            set => PlayerPrefs.SetFloat(SensitivityKey, value);
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set => PlayerPrefs.SetFloat(VolumeKey, value);
        }

        // PlayerPrefs não guarda bool, então guardamos 1 (sim) ou 0 (não).
        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set => PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        }

        // --- Vídeo (as escolhas são índices das listas do VideoRules) ---

        const string QualityKey = "qualidade";
        const string AntialiasingKey = "antialiasing";
        const string RenderScaleKey = "escalaRender";
        const string VSyncKey = "vsync";
        const string FpsKey = "limiteFps";

        public static int Quality
        {
            get => VideoRules.ClampIndex(PlayerPrefs.GetInt(QualityKey, VideoRules.DefaultQuality), VideoRules.QualityNames.Length, VideoRules.DefaultQuality);
            set => PlayerPrefs.SetInt(QualityKey, value);
        }

        public static int Antialiasing
        {
            get => VideoRules.ClampIndex(PlayerPrefs.GetInt(AntialiasingKey, VideoRules.DefaultAa), VideoRules.AaNames.Length, VideoRules.DefaultAa);
            set => PlayerPrefs.SetInt(AntialiasingKey, value);
        }

        // 1 = resolução cheia; abaixo disso liga o STP (ver VideoRules).
        public static float RenderScale
        {
            get => VideoRules.ClampRenderScale(PlayerPrefs.GetFloat(RenderScaleKey, 1f));
            set => PlayerPrefs.SetFloat(RenderScaleKey, VideoRules.ClampRenderScale(value));
        }

        public static bool VSync
        {
            get => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
            set => PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
        }

        public static int FpsLimit
        {
            get => VideoRules.ClampIndex(PlayerPrefs.GetInt(FpsKey, VideoRules.DefaultFps), VideoRules.FpsNames.Length, VideoRules.DefaultFps);
            set => PlayerPrefs.SetInt(FpsKey, value);
        }

        // Guardamos o TAMANHO (e não a posição na lista), porque a lista muda de um monitor para outro.
        // 0x0 = nunca escolheu: fica a nativa.
        const string ResolutionWidthKey = "resolucaoLargura";
        const string ResolutionHeightKey = "resolucaoAltura";
        const string BrightnessKey = "brilho";

        public static Vector2Int Resolution
        {
            get => new Vector2Int(PlayerPrefs.GetInt(ResolutionWidthKey, 0), PlayerPrefs.GetInt(ResolutionHeightKey, 0));
            set { PlayerPrefs.SetInt(ResolutionWidthKey, value.x); PlayerPrefs.SetInt(ResolutionHeightKey, value.y); }
        }

        // Exposição em EV (0 = normal). Ver VideoRules.ClampBrightness.
        public static float Brightness
        {
            get => VideoRules.ClampBrightness(PlayerPrefs.GetFloat(BrightnessKey, 0f));
            set => PlayerPrefs.SetFloat(BrightnessKey, VideoRules.ClampBrightness(value));
        }

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(NameKey, "Carregador");
            set => PlayerPrefs.SetString(NameKey, value);
        }

        // Grava no disco agora (senão a Unity só grava ao fechar o jogo, e um travamento perderia tudo).
        public static void Save() => PlayerPrefs.Save();

        // Mudo enquanto o personagem local é surdo. Não é salvo: vale só nesta partida.
        public static bool Muted { get; set; }

        // Aplica o que dá para aplicar na hora.
        public static void Apply()
        {
            VideoSettings.ApplyScreen();
            AudioListener.volume = Muted ? 0f : Volume;
            VideoSettings.Apply();
        }
    }
}
