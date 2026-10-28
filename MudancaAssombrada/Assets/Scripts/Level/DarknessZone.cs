using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace MudancaAssombrada
{
    // Uma ESFERA de escuridão na casa: tudo o que a câmera vê dentro dela fica mais escuro,
    // sem mexer em luz nenhuma. Serve para o designer dizer "aqui é breu" (porão, corredor,
    // canto atrás da escada). Filho de um fantasma, ela anda junto com ele.
    //
    // Como funciona: antes de cada câmera desenhar, as zonas viram uma lista no shader
    // (DarknessZones.shader, em Resources). Um passe de tela cheia lê a profundidade, descobre
    // onde cada pixel está no mundo e escurece pela distância até cada zona
    // (a conta é a do ScreenMoodRules.ZoneDarkness). Não precisa mexer no asset do URP:
    // o passe entra pela câmera, só enquanto houver zona ligada.
    public class DarknessZone : MonoBehaviour
    {
        // O shader tem espaço para 16 zonas ligadas ao mesmo tempo (as que passarem são ignoradas).
        public const int MaxZones = 16;

        [Min(0.1f)] public float radius = 4f;
        [Tooltip("Parte do raio que é borda esmaecida (0 = corte seco).")]
        [Range(0f, 1f)] public float softness = 0.5f;
        [Tooltip("1 = breu total no miolo.")]
        [Range(0f, 1f)] public float strength = 0.9f;

        static readonly List<DarknessZone> all = new();
        public static IReadOnlyList<DarknessZone> All => all;

        void OnEnable()
        {
            if (all.Count == 0) RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
            if (all.Count == 0) RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawWireSphere(transform.position, radius * (1f - softness));
        }

        // --- Envio para o shader ---

        static readonly int ZonesId = Shader.PropertyToID("_DarkZones");
        static readonly int ParamsId = Shader.PropertyToID("_DarkZoneParams");
        static readonly int CountId = Shader.PropertyToID("_DarkZoneCount");
        static readonly Vector4[] zones = new Vector4[MaxZones];
        static readonly Vector4[] parameters = new Vector4[MaxZones];
        static DarknessPass pass;

        static void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType != CameraType.Game) return; // a janela Scene fica clara para editar
            // Quem enxerga pelo eco já tem a tela preta: escurecer de novo só gastaria.
            if (cam == Camera.main && EchoSystem.Instance != null && EchoSystem.Instance.IsBlind) return;

            int count = Mathf.Min(all.Count, MaxZones);
            for (int i = 0; i < count; i++)
            {
                var z = all[i];
                zones[i] = new Vector4(z.transform.position.x, z.transform.position.y, z.transform.position.z, z.radius);
                parameters[i] = new Vector4(z.strength, z.softness, 0f, 0f);
            }
            Shader.SetGlobalVectorArray(ZonesId, zones);
            Shader.SetGlobalVectorArray(ParamsId, parameters);
            Shader.SetGlobalInt(CountId, count);

            pass ??= DarknessPass.Create();
            if (pass == null) return;
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
        }

        // O passe de tela cheia: copia a imagem passando pelo shader e põe o resultado no lugar.
        class DarknessPass : ScriptableRenderPass
        {
            Material material;

            public static DarknessPass Create()
            {
                var shader = Resources.Load<Shader>("DarknessZones");
                if (shader == null) { Debug.LogWarning("DarknessZones.shader não achado em Resources."); return null; }
                var p = new DarknessPass { material = CoreUtils.CreateEngineMaterial(shader) };
                // Antes do pós: a vinheta, a LUT e o brilho das opções vêm por cima da escuridão.
                p.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                p.requiresIntermediateTexture = true;
                p.ConfigureInput(ScriptableRenderPassInput.Depth);
                return p;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var source = resources.activeColorTexture;
                var desc = renderGraph.GetTextureDesc(source);
                desc.name = "Escuridão das zonas";
                desc.clearBuffer = false;
                var target = renderGraph.CreateTexture(desc);
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, target, material, 0), "Zonas de escuridão");
                resources.cameraColor = target;
            }
        }
    }
}
