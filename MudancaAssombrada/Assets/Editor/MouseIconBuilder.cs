using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MudancaAssombrada.Theme;

namespace MudancaAssombrada.Editor
{
    // Gera o ícone do mouse da dica de Usar (botão esquerdo aceso): o desenho vem do MouseIconRules e
    // as cores do Theme. Feito por conta (sem câmera), então funciona até com -nographics.
    // Mesmo desenho = mesmo arquivo (só regrava se mudou): sem falsas mudanças no Git.
    public static class MouseIconBuilder
    {
        public const string IconPath = "Assets/Art/Icons/T_ICO_MouseEsq.png";
        const int Scale = 2;   // o PNG sai com o dobro dos pixels do desenho (fica nítido na HUD)
        const int Samples = 4; // 4 × 4 amostras por pixel: bordas suaves

        public static Sprite Ensure()
        {
            int w = MouseIconRules.Width * Scale, h = MouseIconRules.Height * Scale;
            var pixels = new Color[w * h];
            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                Color sum = Color.clear;
                for (int sy = 0; sy < Samples; sy++)
                for (int sx = 0; sx < Samples; sx++)
                {
                    float x = (px + (sx + 0.5f) / Samples) / Scale;
                    float y = (py + (sy + 0.5f) / Samples) / Scale;
                    sum += ColorOf(MouseIconRules.PartAt(x, y));
                }
                // A textura tem Y para cima; o desenho, para baixo.
                pixels[(h - 1 - py) * w + px] = sum / (Samples * Samples);
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            if (!File.Exists(IconPath) || !File.ReadAllBytes(IconPath).SequenceEqual(png))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
                File.WriteAllBytes(IconPath, png);
                AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        }

        static Color ColorOf(MouseIconRules.Part part) => part switch
        {
            MouseIconRules.Part.Outside => Color.clear,
            MouseIconRules.Part.LeftButton => HighlightColor, // o botão a apertar
            MouseIconRules.Part.Body => FieldColor,
            _ => BorderColor,                                 // contorno, linhas e rodinha
        };
    }
}
