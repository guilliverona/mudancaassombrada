// Shader do vidro dos espelhos (Casa do Espelho).
// Quem desenha o reflexo é o MirrorReflection.cs: uma câmera espelhada grava a cena numa textura.
// Aqui só "colamos" essa textura no vidro, usando a posição do pixel NA TELA (e não as UVs
// do modelo): assim o reflexo fica no lugar certo de qualquer ângulo.
Shader "MudancaAssombrada/Espelho"
{
    Properties
    {
        _BaseColor ("Tom do vidro (antigo = meio esverdeado)", Color) = (0.78, 0.84, 0.8, 1)
        _DarkColor ("Cor do espelho sem reflexo (longe ou fora da tela)", Color) = (0.07, 0.08, 0.09, 1)
        _Stains ("Manchas de espelho velho", Range(0, 1)) = 0.35
        [HideInInspector] _ReflectionTex ("Reflexo", 2D) = "black" {}
        [HideInInspector] _Strength ("Reflexo ligado (0 ou 1)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0; // posição na tela (dividida pelo w no fragment)
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DarkColor;
                half  _Stains;
                float _Strength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPos  = ComputeScreenPos(output.positionCS);
                return output;
            }

            // Ruído barato (sem textura): manchas escuras de prata oxidada no vidro.
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float stain(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1, 0, 0)), f.x),
                                 lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), f.x),
                                 lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                // A câmera do reflexo desenha a imagem invertida na horizontal (ver MirrorReflection):
                // desvira aqui.
                uv.x = 1 - uv.x;
                half3 reflection = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb * _BaseColor.rgb;
                half3 color = lerp(_DarkColor.rgb, reflection, saturate(_Strength));

                // Manchas: grandes e suaves, mais escuras onde o ruído passa de um limite.
                float s = stain(input.positionWS * 6.0) * 0.6 + stain(input.positionWS * 17.0) * 0.4;
                color *= 1 - smoothstep(0.55, 0.85, s) * _Stains;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
