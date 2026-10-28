// Shader da visão da Dona Cida.
// Um shader é um programa que roda NA PLACA DE VÍDEO, uma vez para cada pixel da tela.
// A pergunta que ele responde é: "de que cor fica este pixel?"
// Resposta aqui: quase preto, a não ser que uma onda de som esteja passando por ele.
// "Quase": um vulto sem cor bem perto dela (para não tropeçar) e um contorno fraco no que a
// bengala já revelou (a memória do caminho). O que é PISTA (timbre, respiração) só vem pela onda.
//
// Três tipos de onda (o EchoRules escolhe qual):
//   - a de forma (bengala): um anel que mostra a FORMA das coisas;
//   - o ponto (passos, respiração, batidas dos outros): só um brilho curto onde o som nasceu;
//   - a do diapasão: anda devagar e cada superfície "canta" com o seu TIMBRE (_Timbre, vem do
//     SurfaceTag): madeira curta e quente, vidro fria e longa, metal aguda, tecido quase nada,
//     pessoa estremece... e a cópia do Imitador não responde.
// E um brilho que não depende de onda: o espelho com o Imitador dentro "respira" (timbre 3).
Shader "MudancaAssombrada/Echo"
{
    Properties
    {
        _BaseColor ("Cor do objeto", Color) = (1, 1, 1, 1)
        _Timbre ("Timbre (SurfaceType)", Float) = 0
        _Revealed ("Quando a bengala revelou (memória, _Time.y)", Float) = -1000
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
                float _Timbre;
                float _Revealed;
            CBUFFER_END

            // Propriedades GLOBAIS: o script EchoSystem.cs preenche estas variáveis
            // uma vez por frame, e TODOS os materiais com este shader leem os mesmos valores.
            float4 _EchoPulses[MAX_PULSES];   // xyz = origem da onda, w = raio atual
            float4 _EchoSettings[MAX_PULSES]; // x = intensidade (0 = desligada), y = 0 forma/1 diapasão/2 ponto, z = velocidade, w = raio máximo
            half4  _EchoColor;
            float  _EchoRingWidth;
            float3 _EchoListener;             // onde está a câmera (a cabeça da Cida)
            float4 _EchoNear;                 // x = raio do vulto de perto, y = força dele, z = força da memória, w = segundos até ela apagar

            // O timbre de cada material: cor, espessura do anel, quanto tempo "canta" e quanto responde.
            //   0 padrão · 1 madeira · 2 vidro · 3 vidro rachado · 4 metal · 5 tecido · 6 pessoa · 7 cópia
            void Timbre(float t, out float3 color, out float width, out float duration, out float gain)
            {
                color = float3(0.55, 0.8, 1.0); width = 1.0; duration = 0.4; gain = 0.6;      // padrão
                if (t > 0.5 && t < 1.5) { color = float3(0.45, 0.55, 1.0); width = 2.0; duration = 0.8; gain = 1.0; } // madeira
                if (t > 1.5 && t < 2.5) { color = float3(0.6, 1.0, 1.0);   width = 0.5; duration = 2.5; gain = 1.1; } // vidro
                if (t > 2.5 && t < 3.5) { color = float3(0.35, 0.6, 0.55); width = 1.2; duration = 1.2; gain = 1.3; } // rachado
                if (t > 3.5 && t < 4.5) { color = float3(1.0, 1.0, 1.0);   width = 0.4; duration = 1.8; gain = 1.0; } // metal
                if (t > 4.5 && t < 5.5) { color = float3(0.5, 0.6, 0.8);   width = 1.5; duration = 0.3; gain = 0.15; } // tecido
                if (t > 5.5 && t < 6.5) { color = float3(0.85, 0.95, 1.0); width = 1.0; duration = 0.35; gain = 1.4; } // pessoa
                if (t > 6.5)            { color = float3(0, 0, 0);         width = 1.0; duration = 0.0; gain = 0.0; } // a cópia: nada
            }

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

                    // Faces viradas para o som "refletem" mais (como o eco de verdade).
                    float3 toOrigin = normalize(origin - input.positionWS);
                    float facing = saturate(dot(normalize(input.normalWS), toOrigin)) * 0.6 + 0.4;

                    if (_EchoSettings[i].y > 1.5)
                    {
                        // PONTO: uma bolinha de luz que acende e apaga onde o som nasceu.
                        float spot = 1 - saturate(dist / max(_EchoSettings[i].w, 0.01));
                        light += _EchoColor.rgb * spot * spot * intensity * facing;
                        continue;
                    }

                    if (_EchoSettings[i].y > 0.5)
                    {
                        // DIAPASÃO: o anel passa devagar e a superfície continua cantando um pouco.
                        float3 color; float width; float duration; float gain;
                        Timbre(_Timbre, color, width, duration, gain);
                        float ring = 1 - saturate(abs(dist - radius) / (_EchoRingWidth * width));
                        float since = (radius - dist) / max(_EchoSettings[i].z, 0.01); // segundos desde que a onda passou
                        float sing = dist < radius ? saturate(1 - since / max(duration, 0.01)) : 0;
                        // Vidro rachado não canta limpo: treme, "bate-estaca".
                        if (_Timbre > 2.5 && _Timbre < 3.5) sing *= step(0, sin(_Time.y * 38));
                        light += color * (ring + sing * 0.8) * intensity * gain * facing;
                        continue;
                    }

                    // Anel: brilha forte quando o pixel está bem na "frente" da onda.
                    // abs(dist - radius) = quão longe o pixel está da borda da onda.
                    float ringNormal = 1 - saturate(abs(dist - radius) / _EchoRingWidth);

                    // Rastro: o que a onda já passou continua visível, apagando aos poucos.
                    float trail = dist < radius ? saturate(1 - (radius - dist) / 3.0) * 0.25 : 0;

                    light += _EchoColor.rgb * (ringNormal + trail) * intensity * facing;
                }

                // Vulto de perto e memória: cinza, sem timbre nem cor, para nunca entregar uma pista.
                float3 n = normalize(input.normalWS);
                float3 toEye = normalize(_EchoListener - input.positionWS);
                float near = 1 - saturate(distance(input.positionWS, _EchoListener) / max(_EchoNear.x, 0.01));
                float shade = saturate(dot(n, toEye)) * 0.7 + 0.3;
                float rim = pow(1 - saturate(dot(n, toEye)), 3); // borda das formas (contorno)
                // Memória curta: o contorno some em _EchoNear.w segundos depois da última batida.
                float memory = saturate(1 - (_Time.y - _Revealed) / max(_EchoNear.w, 0.01));
                float ghost = near * near * shade * _EchoNear.y + memory * memory * rim * _EchoNear.z;

                // O espelho com o Imitador DENTRO respira um brilho fraco esverdeado, mesmo sem onda
                // nenhuma. Só a Cida vê (só a tela dela usa este shader); quem enxerga vê um espelho comum.
                if (_Timbre > 2.5 && _Timbre < 3.5)
                {
                    float breath = 0.5 + 0.5 * sin(_Time.y * 1.4 + input.positionWS.y * 2.5);
                    light += float3(0.35, 0.6, 0.55) * (0.04 + 0.08 * breath);
                }

                // Um leve toque da cor original, para distinguir armário de parede.
                half3 tint = lerp(half3(1, 1, 1), _BaseColor.rgb, 0.35);
                return half4(tint * light + ghost * half3(0.6, 0.65, 0.7), 1);
            }
            ENDHLSL
        }
    }
}
