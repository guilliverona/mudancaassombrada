using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Gera o "cookie" da lanterna: a textura que a luz projeta (miolo forte, aro do refletor, riscos
    // leves). O desenho vem do FlashlightBeamRules; aqui só vira PNG e é importado como Cookie.
    // Mesmo desenho = mesmo arquivo (só regrava se mudou): sem falsas mudanças no Git.
    // Mudou o desenho no FlashlightBeamRules? Recrie o Player (o construtor chama Ensure).
    public static class FlashlightCookieBuilder
    {
        public const string CookiePath = "Assets/Art/Props/Lanterna/T_PRP_Lanterna_Facho.png";
        const int Size = 256;

        public static Texture2D Ensure()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            var pixels = new Color32[Size * Size];
            float half = (Size - 1) / 2f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = (x - half) / half, dy = (y - half) / half;
                float v = FlashlightBeamRules.Cookie(Mathf.Sqrt(dx * dx + dy * dy), Mathf.Atan2(dy, dx));
                byte b = (byte)Mathf.RoundToInt(v * 255f);
                pixels[y * Size + x] = new Color32(b, b, b, 255);
            }
            tex.SetPixels32(pixels);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string full = Path.Combine(Directory.GetCurrentDirectory(), CookiePath);
            bool changed = !File.Exists(full) || !File.ReadAllBytes(full).SequenceEqual(png);
            if (changed)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, png);
                AssetDatabase.ImportAsset(CookiePath, ImportAssetOptions.ForceUpdate);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(CookiePath);
            // Cookie vira um canal só (Alpha8). O desenho está nos tons de cinza e o PNG tem a
            // transparência toda cheia: sem "alpha dos cinzas", o facho sairia liso (medido em batch).
            if (importer.textureType != TextureImporterType.Cookie || importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
            {
                importer.textureType = TextureImporterType.Cookie; // luz de spot: borda presa (Clamp)
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
        }
    }
}
