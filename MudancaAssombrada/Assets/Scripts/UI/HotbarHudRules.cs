using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS da barra da hotbar na HUD, sem tela: onde fica cada caixinha, quando a bateria está
    // fraca, a entrada suave e o brilho de um item novo. A Hud só desenha o que estas contas dizem;
    // os testes (HotbarHudRulesTests) conferem sem abrir uma fase.
    public static class HotbarHudRules
    {
        // Abaixo disto a lanterna está "FRACA": a mesma carga em que a luz começa a falhar (Flashlight).
        public const float LowBattery = 0.2f;

        // Posição X do espaço "slot" com a barra centralizada: com 2 espaços (o Marcelo), os dois
        // ficam de cada lado do meio; com 3, o do meio fica no centro.
        public static float SlotX(int slot, int count, float size, float gap) =>
            (slot - (count - 1) / 2f) * (size + gap);

        public static bool BatteryLow(float charge01) => charge01 < LowBattery;

        // Entrada suave: apagada, a barra fica "distance" pixels abaixo; acesa, no lugar. Ela sobe
        // enquanto aparece (DESIGN.md: deslizes curtos, nada balança).
        public static float SlideOffset(float alpha, float distance) => -(1f - Mathf.Clamp01(alpha)) * distance;

        // Chegou um item NOVO neste espaço (vazio ou outro antes)? Gastar ou guardar não conta.
        public static bool Gained(uint before, uint after, int slot)
        {
            var now = HotbarRules.Get(after, slot);
            return now != HotbarItem.None && now != HotbarRules.Get(before, slot);
        }

        // O brilho do espaço que ganhou um item: inteiro na hora, apaga em "duration" segundos.
        public static float Flash(float secondsSince, float duration) =>
            duration <= 0f ? 0f : Mathf.Clamp01(1f - secondsSince / duration);
    }
}
