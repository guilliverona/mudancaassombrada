// Passe de tela cheia das ZONAS DE ESCURIDÃO (DarknessZone.cs).
// Para cada pixel: lê a profundidade, reconstrói a posição no mundo e escurece pela distância
// até cada zona. A conta é a mesma do ScreenMoodRules.ZoneDarkness/Combine: mudou lá, muda aqui.
//   _DarkZones[i]      = (centro x, y, z, raio)
//   _DarkZoneParams[i] = (força, borda, -, -)
// Fica em Resources para entrar no build (o DarknessZone carrega por nome).
Shader "Hidden/MudancaAssombrada/DarknessZones"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Zonas de escuridão"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_ZONES 16 // igual a DarknessZone.MaxZones
            float4 _DarkZones[MAX_ZONES];
            float4 _DarkZoneParams[MAX_ZONES];
            int _DarkZoneCount;

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float depth = SampleSceneDepth(uv);
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                float dark = 0;
                for (int i = 0; i < _DarkZoneCount; i++)
                {
                    float radius = _DarkZones[i].w;
                    float strength = _DarkZoneParams[i].x;
                    float inner = radius * (1 - saturate(_DarkZoneParams[i].y));
                    float d = distance(world, _DarkZones[i].xyz);
                    // 1 no miolo, 0 do raio para fora, smoothstep na borda
                    float t = saturate((d - inner) / max(radius - inner, 1e-4));
                    float zone = d >= radius ? 0 : strength * (1 - t * t * (3 - 2 * t));
                    dark = max(dark, zone);
                }

                color.rgb *= 1 - saturate(dark);
                return color;
            }
            ENDHLSL
        }
    }
}
