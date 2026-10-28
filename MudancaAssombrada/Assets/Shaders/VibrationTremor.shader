// Shader da visão do Tiago: o CHÃO TREMENDO onde algo vibra.
// Fica num quadrado deitado no chão (VibrationMark). Em vez de desenhar um anel, ele pega a imagem
// que já está na tela atrás dele (_CameraOpaqueTexture, ligada no PC_RPAsset) e a entorta com
// ondinhas que saem do centro, como água tremendo num copo. Um brilho fraco da cor vai junto,
// para o tremor não sumir numa sala sem luz.
//   _Strength: 0 = parado (invisível), 1 = tremendo forte.
//   _Color: amarelo (colega, casa) ou vermelho (perigo).
Shader "MudancaAssombrada/VibrationTremor"
{
    Properties
    {
        _Color ("Cor do brilho", Color) = (1, 0.85, 0.3, 1)
        _Strength ("Força do tremor", Range(0, 1)) = 1
        _Seed ("Semente (cada marca treme fora de fase)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0; // 0..1 no quadrado: o centro (0,5; 0,5) é a fonte
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1; // onde este pixel cai na tela (para ler a imagem de trás)
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Strength;
                float _Seed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 c = input.uv * 2 - 1;        // -1..1, centro = 0
                float r = length(c);                // 0 no centro, 1 na borda
                if (r >= 1) discard;

                float fall = 1 - r;                 // mais forte no centro
                fall *= fall;
                // Ondinhas saindo do centro: sin da distância menos o tempo = anéis que andam para fora.
                float wave = sin(r * 22 - _Time.y * 16 + _Seed);
                // Um "chacoalhar" rápido por cima, para parecer tremor e não água calma.
                float shake = sin(_Time.y * 47 + _Seed * 3.1);

                float2 dir = r > 0.001 ? c / r : float2(0, 0);
                float2 offset = dir * wave * 0.006 * _Strength * fall + float2(shake, -shake) * 0.0015 * _Strength * fall;

                float2 uv = input.screenPos.xy / input.screenPos.w;
                half3 scene = SampleSceneColor(uv + offset);

                // Brilho nas cristas das ondinhas.
                float crest = pow(saturate(wave * 0.5 + 0.5), 4);
                half3 glow = _Color.rgb * crest * fall * _Strength * 0.6;

                // Borda macia: perto da borda do quadrado o tremor some (sem círculo marcado).
                float alpha = saturate(fall * 4) * saturate(_Strength * 3);
                return half4(scene + glow, alpha);
            }
            ENDHLSL
        }
    }
}
