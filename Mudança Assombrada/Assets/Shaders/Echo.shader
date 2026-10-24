// Shader da visão da Dona Cida.
// Um shader é um programa que roda NA PLACA DE VÍDEO, uma vez para cada pixel da tela.
// A pergunta que ele responde é: "de que cor fica este pixel?"
// Resposta aqui: preto, a não ser que uma onda de som esteja passando por ele.
Shader "MudancaAssombrada/Echo"
{
    Properties
    {
        _BaseColor ("Cor do objeto", Color) = (1, 1, 1, 1)
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

            #define MAX_PULSES 8

            // Dados de cada vértice do modelo (vêm da malha 3D).
            struct Attributes
            {
                float4 positionOS : POSITION; // OS = Object Space (posição local do modelo)
                float3 normalOS   : NORMAL;   // para onde a face aponta
            };

            // Dados que o vertex shader passa para o fragment shader.
            struct Varyings
            {
                float4 positionCS : SV_POSITION; // CS = Clip Space (posição na tela)
                float3 positionWS : TEXCOORD0;   // WS = World Space (posição no mundo)
                float3 normalWS   : TEXCOORD1;
            };

            // Propriedades do material (uma por objeto).
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            // Propriedades GLOBAIS: o script EchoSystem.cs preenche estas variáveis
            // uma vez por frame, e TODOS os materiais com este shader leem os mesmos valores.
            float4 _EchoPulses[MAX_PULSES];   // xyz = origem da onda, w = raio atual
            float4 _EchoSettings[MAX_PULSES]; // x = intensidade (0 = onda desligada)
            half4  _EchoColor;
            float  _EchoRingWidth;

            // VERTEX SHADER: roda uma vez por vértice. Só converte posições.
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            // FRAGMENT SHADER: roda uma vez por pixel. Aqui está a "mágica".
            half4 frag(Varyings input) : SV_Target
            {
                float3 light = 0;

                for (int i = 0; i < MAX_PULSES; i++)
                {
                    float intensity = _EchoSettings[i].x;
                    if (intensity <= 0) continue;

                    float3 origin = _EchoPulses[i].xyz;
                    float radius  = _EchoPulses[i].w;
                    float dist    = distance(input.positionWS, origin);

                    // Anel: brilha forte quando o pixel está bem na "frente" da onda.
                    // abs(dist - radius) = quão longe o pixel está da borda da onda.
                    float ring = 1 - saturate(abs(dist - radius) / _EchoRingWidth);

                    // Rastro: o que a onda já passou continua visível, apagando aos poucos.
                    float trail = dist < radius ? saturate(1 - (radius - dist) / 3.0) * 0.25 : 0;

                    // Faces viradas para o som "refletem" mais (como o eco de verdade).
                    float3 toOrigin = normalize(origin - input.positionWS);
                    float facing = saturate(dot(normalize(input.normalWS), toOrigin)) * 0.6 + 0.4;

                    light += (ring + trail) * intensity * facing;
                }

                // Um leve toque da cor original, para distinguir armário de parede.
                half3 tint = lerp(half3(1, 1, 1), _BaseColor.rgb, 0.35);
                return half4(_EchoColor.rgb * tint * light, 1);
            }
            ENDHLSL
        }
    }
}
