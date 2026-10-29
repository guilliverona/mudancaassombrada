using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MudancaAssombrada
{
    // Aplica as opções de vídeo (salvas no GameSettings) no URP.
    // O VideoRules decide o que ligar; aqui só se escreve isso na Unity.
    //
    // Cuidado: mexer no PC_RPAsset direto mudaria o ARQUIVO no Editor (e sujaria o Git).
    // Por isso trabalhamos numa CÓPIA do asset, criada na primeira vez e desfeita ao sair.
    public static class VideoSettings
    {
        static UniversalRenderPipelineAsset runtimeAsset;
        static RenderPipelineAsset originalAsset;
        static AntialiasingMode cameraAa = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        public static void Apply()
        {
            var resolved = VideoRules.Resolve(GameSettings.Antialiasing, GameSettings.RenderScale);
            var quality = VideoRules.Quality(GameSettings.Quality);

            var asset = RuntimeAsset();
            if (asset != null)
            {
                asset.renderScale = GameSettings.RenderScale;
                asset.upscalingFilter = resolved.stp ? UpscalingFilterSelection.STP : UpscalingFilterSelection.Auto;
                asset.msaaSampleCount = resolved.msaa;
                asset.shadowDistance = quality.shadowDistance;
                asset.shadowCascadeCount = quality.cascades;
                asset.mainLightShadowmapResolution = quality.shadowResolution;
                asset.additionalLightsShadowmapResolution = quality.lampShadowResolution;
            }

            cameraAa = ToUrp(resolved.cameraAa);
            BrightnessOverride().postExposure.Override(GameSettings.Brightness);
            QualitySettings.vSyncCount = GameSettings.VSync ? 1 : 0;
            Application.targetFrameRate = VideoRules.TargetFrameRate(GameSettings.VSync, GameSettings.FpsLimit);
        }

        // As resoluções do monitor, uma de cada tamanho (do menor para o maior).
        public static Vector2Int[] Resolutions() =>
            VideoRules.UniqueSizes(Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)));

        // Resolução + tela cheia. Só chama o SetResolution se algo mudou: ele pisca a tela,
        // e o GameSettings.Apply roda em outros momentos (troca de personagem...).
        public static void ApplyScreen()
        {
            var sizes = Resolutions();
            int index = VideoRules.ResolutionIndex(sizes, GameSettings.Resolution);
            var mode = GameSettings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (index < 0) { Screen.fullScreenMode = mode; return; }

            var size = sizes[index];
            if (Screen.width != size.x || Screen.height != size.y || Screen.fullScreenMode != mode)
                Screen.SetResolution(size.x, size.y, mode);
        }

        // Um Volume global, por cima dos das casas (prioridade alta), que só mexe na exposição.
        // Os outros efeitos de cada casa continuam valendo: o Volume só sobrescreve o que marca.
        static ColorAdjustments brightness;

        static ColorAdjustments BrightnessOverride()
        {
            if (brightness != null) return brightness;
            var go = new GameObject("Brilho (opções)");
            Object.DontDestroyOnLoad(go); // vale no menu e em todas as casas
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            brightness = volume.profile.Add<ColorAdjustments>();
            return brightness;
        }

        // A cópia do asset do URP que pode ser mexida à vontade.
        static UniversalRenderPipelineAsset RuntimeAsset()
        {
            if (runtimeAsset != null) return runtimeAsset;
            var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (current == null) return null;

            originalAsset = QualitySettings.renderPipeline;
            runtimeAsset = Object.Instantiate(current);
            runtimeAsset.name = current.name + " (opções)";
            QualitySettings.renderPipeline = runtimeAsset;

            // As câmeras nascem a qualquer hora (a do jogador só quando o boneco aparece):
            // em vez de caçar cada uma, acertamos o antialiasing logo antes de ela desenhar.
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            Application.quitting += Restore; // no Editor, sair do Play também dispara
            return runtimeAsset;
        }

        static void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            // Só a câmera que vai para a tela: a do espelho desenha numa textura e tem regra própria.
            if (camera.cameraType != CameraType.Game || camera.targetTexture != null) return;
            // A câmera do jogador não vem com os dados do URP salvos: GetUniversalAdditionalCameraData
            // cria se faltar. O pós-processamento precisa estar ligado para o brilho (e o FXAA/SMAA/TAA).
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = cameraAa;
        }

        // Devolve o asset original (senão o Editor sairia do Play ainda usando a cópia).
        static void Restore()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            Application.quitting -= Restore;
            QualitySettings.renderPipeline = originalAsset;
            if (runtimeAsset != null) Object.Destroy(runtimeAsset);
            runtimeAsset = null;
        }

        static AntialiasingMode ToUrp(VideoRules.CameraAa aa)
        {
            switch (aa)
            {
                case VideoRules.CameraAa.Fxaa: return AntialiasingMode.FastApproximateAntialiasing;
                case VideoRules.CameraAa.Smaa: return AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                case VideoRules.CameraAa.Taa: return AntialiasingMode.TemporalAntiAliasing;
                default: return AntialiasingMode.None;
            }
        }
    }
}
