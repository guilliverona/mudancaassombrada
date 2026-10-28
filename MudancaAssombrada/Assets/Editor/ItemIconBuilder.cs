using System.IO;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Gera os ÍCONES da hotbar a partir dos próprios modelos do jogo (lanterna, pilhas, a bengala da
    // Cida): fotografa cada um num palco isolado (PreviewRenderUtility), tira o fundo e salva uma
    // silhueta clara com sombreado leve (T_ICO_<Nome>.png). Na HUD, a Hud pinta a silhueta com a cor
    // do Theme (texto, ou ectoplasma na mão). Assim o ícone tem a cara da arte e acompanha o modelo.
    //
    // Fundo transparente sem depender do alpha da câmera: fotografa duas vezes, no preto e no branco.
    // O que não muda é o objeto; o quanto muda diz a transparência.
    //
    // Fotografar precisa de placa de vídeo: com -nographics, os ícones que já existem são só lidos.
    // Mudou um modelo? Menu "Mudança Assombrada/Avançado/Recriar Ícones da Hotbar" (ou apague os PNG).
    public static class ItemIconBuilder
    {
        const string Folder = "Assets/Art/Icons/";
        const int Size = 256;

        // Nome, modelo, peça (parte do nome do renderer; vazio = todas), o giro do objeto, a inclinação
        // NA TELA (graus, anti-horário) e quanto engrossar o traço (px; peças finas como a bengala).
        static readonly (string name, string model, string part, Vector3 euler, float tilt, int thicken)[] Icons =
        {
            // Lanterna de lado (a frente do modelo é +Z), a lente para cima e para a direita.
            ("Lanterna", "Assets/Art/Props/Lanterna/SM_PRP_Lanterna.fbx", "", new Vector3(0f, 90f, 0f), 35f, 0),
            // Pilhas vistas um pouco de cima, para as quatro aparecerem.
            ("Pilhas", "Assets/Art/Props/Pilhas/SM_PRP_Pilhas.fbx", "", new Vector3(-50f, 0f, 0f), 0f, 0),
            // A bengala da Cida (só ela, do modelo da Cida), de lado; 2 cm de grossura: o traço engrossa.
            ("Bengala", "Assets/Art/Characters/Cida/SK_CHR_Cida.fbx", "Bengala", new Vector3(0f, 90f, 0f), 0f, 7),
        };

        public static string PathOf(string name) => $"{Folder}T_ICO_{name}.png";

        [MenuItem("Mudança Assombrada/Avançado/Recriar Ícones da Hotbar", priority = 106)]
        static void RebuildMenu() => EnsureAll(force: true);

        // Garante os PNG (gera os que faltam) e devolve os sprites na ordem de Icons.
        public static Sprite[] EnsureAll(bool force = false)
        {
            bool canRender = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            var sprites = new Sprite[Icons.Length];
            for (int i = 0; i < Icons.Length; i++)
            {
                var (name, model, part, euler, tilt, thicken) = Icons[i];
                string path = PathOf(name);
                if (canRender && (force || !File.Exists(path))) Render(name, model, part, euler, tilt, thicken, path);
                sprites[i] = Import(path);
            }
            return sprites;
        }

        static void Render(string name, string modelPath, string part, Vector3 euler, float tilt, int thicken, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (asset == null) { Debug.LogWarning($"Ícone {name}: falta o modelo {modelPath}."); return; }

            var stage = new PreviewRenderUtility();
            try
            {
                // Dentro de um vazio: a raiz do FBX tem a rotação da importação, e girar ela direto
                // deixaria o objeto de lado (armadilha do CLAUDE.md). Gira o vazio.
                var go = new GameObject($"Ícone {name}");
                var model = Object.Instantiate(asset, go.transform, false);
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    r.enabled = part.Length == 0 || r.name.Contains(part);
                // A câmera olha para +Z: girar em volta de Z depois inclina o objeto na tela.
                go.transform.rotation = Quaternion.AngleAxis(tilt, Vector3.forward) * Quaternion.Euler(euler);
                stage.AddSingleGO(go);

                // Enquadra o que ficou visível, de frente (câmera em −Z olhando +Z), ortográfica.
                var bounds = new Bounds();
                bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled) continue;
                    if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                }
                var cam = stage.camera;
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f;
                cam.transform.SetPositionAndRotation(bounds.center - Vector3.forward * (bounds.extents.z + 2f), Quaternion.identity);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = bounds.size.z + 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                stage.lights[0].intensity = 1.3f;
                stage.lights[0].transform.rotation = Quaternion.Euler(40f, 30f, 0f);
                stage.lights[1].intensity = 0.6f;
                stage.ambientColor = new Color(0.35f, 0.35f, 0.38f);

                var black = Shoot(stage, Color.black);
                var white = Shoot(stage, Color.white);
                var icon = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                for (int i = 0; i < black.Length; i++)
                {
                    Color b = black[i], w = white[i];
                    float alpha = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
                    // Silhueta clara: o sombreado do modelo vira tons de 55% a 100% (a Hud pinta a cor).
                    float lum = alpha > 0.01f ? Mathf.Clamp01((b.r * 0.3f + b.g * 0.59f + b.b * 0.11f) / alpha) : 0f;
                    float shade = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(lum * 1.8f));
                    black[i] = new Color(shade, shade, shade, alpha);
                }
                if (thicken > 0) black = Thicken(black, thicken);
                icon.SetPixels(black);
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(path, icon.EncodeToPNG());
                Object.DestroyImmediate(icon);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Ícone da hotbar criado: {path}");
            }
            finally { stage.Cleanup(); }
        }

        // Engrossa o traço: cada pixel vira o mais opaco num círculo de raio "radius" (peças finas,
        // como a bengala de 2 cm, sumiriam no ícone de 42 px).
        static Color[] Thicken(Color[] src, int radius)
        {
            var dst = new Color[src.Length];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                Color best = src[y * Size + x];
                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int sx = x + dx, sy = y + dy;
                    if (dx * dx + dy * dy > radius * radius || sx < 0 || sy < 0 || sx >= Size || sy >= Size) continue;
                    var c = src[sy * Size + sx];
                    if (c.a > best.a) best = c;
                }
                dst[y * Size + x] = best;
            }
            return dst;
        }

        static Color[] Shoot(PreviewRenderUtility stage, Color background)
        {
            stage.camera.backgroundColor = background;
            stage.BeginStaticPreview(new Rect(0, 0, Size, Size));
            stage.camera.Render();
            var tex = stage.EndStaticPreview();
            var pixels = tex.GetPixels();
            Object.DestroyImmediate(tex);
            return pixels;
        }

        // Importa como Sprite com transparência (o Image da HUD usa).
        static Sprite Import(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
