using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MudancaAssombrada
{
    // As regras das opções de vídeo, sem nada da Unity "viva" (dá para testar no EditMode).
    // Quem aplica de verdade no URP é o VideoSettings; aqui só se decide O QUE ligar.
    public static class VideoRules
    {
        // --- Qualidade: mexe nas sombras (o que mais pesa nas casas escuras com lanternas) ---

        public static readonly string[] QualityNames = { "Baixa", "Média", "Alta" };
        public const int DefaultQuality = 2;

        public struct QualityPreset
        {
            public float shadowDistance;  // até onde (m) a luz principal faz sombra
            public int shadowResolution;  // tamanho do mapa de sombra (mais = sombra mais nítida)
            public int cascades;          // fatias da sombra (mais = sombra boa perto E longe)
            public int lampShadowResolution; // mapa das sombras das lanternas e lâmpadas (o que mais aparece na casa)
            public bool effects;          // grão, aberração cromática e bloom do filtro da tela (ScreenLook)
        }

        // "Alta" é o que o PC_RPAsset já usava antes deste painel existir (com as lâmpadas em 4096).
        public static QualityPreset Quality(int index)
        {
            switch (ClampIndex(index, QualityNames.Length, DefaultQuality))
            {
                case 0: return new QualityPreset { shadowDistance = 20f, shadowResolution = 1024, cascades = 1, lampShadowResolution = 1024, effects = false };
                case 1: return new QualityPreset { shadowDistance = 35f, shadowResolution = 2048, cascades = 2, lampShadowResolution = 2048, effects = true };
                default: return new QualityPreset { shadowDistance = 50f, shadowResolution = 2048, cascades = 4, lampShadowResolution = 4096, effects = true };
            }
        }

        // --- Antialiasing ---

        public static readonly string[] AaNames = { "Nenhum", "FXAA", "SMAA", "TAA", "MSAA 4x" };
        public const int AaNone = 0, AaFxaa = 1, AaSmaa = 2, AaTaa = 3, AaMsaa4 = 4;
        public const int DefaultAa = AaSmaa;

        // Espelha o AntialiasingMode do URP (que não dá para usar aqui sem puxar o pipeline).
        public enum CameraAa { None, Fxaa, Smaa, Taa }

        // --- Render scale (e o STP, o upscaler da Unity 6) ---

        public const float MinRenderScale = 0.5f;

        public static float ClampRenderScale(float scale) => Mathf.Clamp(scale, MinRenderScale, 1f);

        // Abaixo de 100% o jogo desenha menor e o STP reconstrói a imagem na resolução da tela.
        public static bool UsesStp(float scale) => ClampRenderScale(scale) < 0.999f;

        public struct Resolved
        {
            public CameraAa cameraAa; // antialiasing da câmera do jogador
            public int msaa;          // 1 = desligado
            public bool stp;
        }

        // Junta antialiasing + render scale no que vai para o URP.
        public static Resolved Resolve(int aa, float renderScale)
        {
            // Com STP o próprio upscaler já suaviza as bordas (é temporal, como o TAA):
            // qualquer outro antialiasing por cima só custaria desempenho.
            if (UsesStp(renderScale)) return new Resolved { cameraAa = CameraAa.None, msaa = 1, stp = true };

            switch (ClampIndex(aa, AaNames.Length, DefaultAa))
            {
                case AaFxaa: return new Resolved { cameraAa = CameraAa.Fxaa, msaa = 1 };
                case AaSmaa: return new Resolved { cameraAa = CameraAa.Smaa, msaa = 1 };
                case AaTaa: return new Resolved { cameraAa = CameraAa.Taa, msaa = 1 }; // TAA não convive com MSAA
                case AaMsaa4: return new Resolved { cameraAa = CameraAa.None, msaa = 4 };
                default: return new Resolved { cameraAa = CameraAa.None, msaa = 1 };
            }
        }

        // --- VSync e limite de FPS ---

        public static readonly string[] FpsNames = { "30", "60", "120", "144", "Sem limite" };
        static readonly int[] FpsValues = { 30, 60, 120, 144, -1 };
        public const int DefaultFps = 4;

        // -1 = sem limite. Com VSync ligado quem manda é o monitor, então o limite fica de fora.
        public static int TargetFrameRate(bool vsync, int fpsIndex) =>
            vsync ? -1 : FpsValues[ClampIndex(fpsIndex, FpsValues.Length, DefaultFps)];

        // --- Resolução da janela ---

        // O monitor repete cada tamanho uma vez por taxa de atualização: aqui fica um de cada,
        // do menor para o maior.
        public static Vector2Int[] UniqueSizes(IEnumerable<Vector2Int> sizes) =>
            sizes.Distinct().OrderBy(s => s.x).ThenBy(s => s.y).ToArray();

        // Posição da resolução salva na lista. Não achou (nunca salvou, outro monitor): a maior,
        // que é a nativa. Lista vazia: -1 (aí o jogo não mexe na resolução).
        public static int ResolutionIndex(IList<Vector2Int> sizes, Vector2Int saved)
        {
            int found = sizes.IndexOf(saved);
            return found >= 0 ? found : sizes.Count - 1;
        }

        // --- Brilho ---

        // O brilho é a exposição do pós-processamento, em EV: +1 dobra a luz, -1 corta pela metade.
        // Mais que isso estraga o escuro da casa (e o susto) ou vira breu.
        public const float MaxBrightness = 1f;

        public static float ClampBrightness(float ev) => Mathf.Clamp(ev, -MaxBrightness, MaxBrightness);

        // Para o texto do painel: 0 EV = 100%.
        public static int BrightnessPercent(float ev) => Mathf.RoundToInt(Mathf.Pow(2f, ClampBrightness(ev)) * 100f);

        // --- Ajudantes dos botões ---

        // O botão de opção anda em roda: depois da última, volta para a primeira.
        public static int Next(int index, int count) => (index + 1) % count;

        // Índice salvo fora da lista (PlayerPrefs mexido, versão antiga) vira o padrão.
        public static int ClampIndex(int index, int count, int fallback) =>
            index >= 0 && index < count ? index : fallback;
    }
}
