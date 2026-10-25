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
        const string AddressKey = "ipDoHost";

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

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(NameKey, "Carregador");
            set => PlayerPrefs.SetString(NameKey, value);
        }

        // O último IP digitado, para não precisar digitar de novo toda vez.
        public static string HostAddress
        {
            get => PlayerPrefs.GetString(AddressKey, "127.0.0.1");
            set => PlayerPrefs.SetString(AddressKey, value);
        }

        // Grava no disco agora (senão a Unity só grava ao fechar o jogo, e um travamento perderia tudo).
        public static void Save() => PlayerPrefs.Save();

        // Mudo enquanto o personagem local é surdo. Não é salvo: vale só nesta partida.
        public static bool Muted { get; set; }

        // Aplica o que dá para aplicar na hora.
        public static void Apply()
        {
            Screen.fullScreen = Fullscreen;
            AudioListener.volume = Muted ? 0f : Volume;
        }
    }
}
