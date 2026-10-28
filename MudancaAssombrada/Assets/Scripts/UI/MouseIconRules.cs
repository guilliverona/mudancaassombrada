using UnityEngine;

namespace MudancaAssombrada
{
    // O desenho do ícone do mouse da dica de Usar (DESIGN.md: hotbar), sem tela: qual parte do mouse
    // fica em cada pixel. O MouseIconBuilder pinta com as cores do Theme (corpo escuro, contorno fino,
    // botão ESQUERDO aceso em ectoplasma, a rodinha) e os testes (MouseIconRulesTests) conferem.
    // Pixels de um ícone de 64 × 88 (a proporção de um mouse), Y para baixo a partir do topo.
    public static class MouseIconRules
    {
        public enum Part { Outside, Outline, Body, LeftButton, Line, Wheel }

        public const int Width = 64, Height = 88;
        public const float ButtonsBottom = 38f;  // onde os botões terminam (a linha que os separa do corpo)
        const float Corner = 26f;                 // o mouse é quase uma cápsula
        const float Stroke = 3.5f;                // grossura do contorno e das linhas

        public static Part PartAt(float x, float y)
        {
            float d = RoundedBox(x, y, Width / 2f, Height / 2f, Width / 2f - 0.5f, Height / 2f - 0.5f, Corner);
            if (d > 0f) return Part.Outside;
            if (d > -Stroke) return Part.Outline;

            // A rodinha: um tracinho arredondado no meio, entre os botões.
            if (RoundedBox(x, y, Width / 2f, 20f, 3.5f, 8f, 3.5f) <= 0f) return Part.Wheel;
            if (Mathf.Abs(y - ButtonsBottom) <= Stroke / 2f) return Part.Line;                     // botões | corpo
            if (y < ButtonsBottom && Mathf.Abs(x - Width / 2f) <= Stroke / 2f) return Part.Line;   // esquerdo | direito
            return y < ButtonsBottom && x < Width / 2f ? Part.LeftButton : Part.Body;
        }

        // Distância até a borda de uma caixa de cantos arredondados (negativa = dentro).
        static float RoundedBox(float x, float y, float cx, float cy, float halfW, float halfH, float r)
        {
            float qx = Mathf.Abs(x - cx) - (halfW - r), qy = Mathf.Abs(y - cy) - (halfH - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
