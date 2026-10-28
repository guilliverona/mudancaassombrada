namespace MudancaAssombrada
{
    // O que cabe num espaço da hotbar. byte: os 3 espaços cabem num número só (ver Get/Set).
    // Item novo = um nome aqui (no fim, para não trocar o número dos que já existem).
    public enum HotbarItem : byte
    {
        None = 0,
        Flashlight = 1,
    }

    // Onde a lanterna está: na mão (aponta para onde você olha), presa no colete (você carrega
    // um móvel: aponta para a frente do corpo, mais fraca) ou guardada no cinto (apagada).
    public enum FlashlightPlace : byte
    {
        Stowed = 0,
        Hand = 1,
        Chest = 2,
    }

    // O que a tecla da lanterna (L) faz agora.
    public enum FlashlightKey
    {
        Nothing,
        Toggle,            // liga/desliga a que está na mão ou no colete
        TakeOutAndTurnOn,  // estava guardada: vai para a mão e já acende
    }

    // As REGRAS da hotbar, sem cena nem rede: o que cada tecla faz, onde a lanterna fica.
    // O Hotbar e o Flashlight (no Player) juntam os dados e perguntam aqui; os testes
    // (Assets/Tests/EditMode/HotbarRulesTests.cs) conferem cada regra sem abrir uma fase.
    //
    // Os espaços vão "empacotados" num uint: 8 bits por espaço (espaço 0 nos bits 0-7, espaço 1
    // nos 8-15...). Assim a barra inteira é UMA variável de rede, e não uma lista.
    public static class HotbarRules
    {
        public const int Slots = 3;
        public const int EmptyHand = -1; // nenhum espaço escolhido: mão vazia

        public static bool IsSlot(int slot) => slot >= 0 && slot < Slots;

        public static HotbarItem Get(uint bar, int slot) =>
            IsSlot(slot) ? (HotbarItem)((bar >> (slot * 8)) & 0xFF) : HotbarItem.None;

        public static uint Set(uint bar, int slot, HotbarItem item)
        {
            if (!IsSlot(slot)) return bar;
            int shift = slot * 8;
            return (bar & ~(0xFFu << shift)) | ((uint)item << shift);
        }

        // O primeiro espaço com este item (ou -1). Com None, é o primeiro espaço livre.
        public static int Find(uint bar, HotbarItem item)
        {
            for (int i = 0; i < Slots; i++)
                if (Get(bar, i) == item) return i;
            return -1;
        }

        public static int FirstEmpty(uint bar) => Find(bar, HotbarItem.None);
        public static bool Contains(uint bar, HotbarItem item) => item != HotbarItem.None && Find(bar, item) >= 0;

        // Tecla de número: segura aquele espaço; a mesma tecla de novo guarda (mão vazia).
        public static int Press(int selected, int pressed)
        {
            if (!IsSlot(pressed)) return selected;
            return pressed == selected ? EmptyHand : pressed;
        }

        // Rodinha / direcional: anda 'steps' espaços dando a volta. De mão vazia, começa pela ponta.
        public static int Scroll(int selected, int steps)
        {
            if (steps == 0) return selected;
            if (!IsSlot(selected)) return steps > 0 ? 0 : Slots - 1;
            int next = (selected + steps) % Slots;
            return next < 0 ? next + Slots : next;
        }

        public static HotbarItem InHand(uint bar, int selected) => Get(bar, selected);

        // Mão livre (nada escolhido, ou o espaço escolhido está vazio)? Então o item que acabou de
        // chegar já vai para a mão.
        public static bool HandIsFree(uint bar, int selected) => InHand(bar, selected) == HotbarItem.None;

        // Móvel nas mãos (primeira ponta ou outra ponta) manda o item da mão para o cinto. A lanterna
        // é a exceção boa: vai presa no colete e continua acesa.
        public static FlashlightPlace PlaceOf(bool flashlightSelected, bool hasLoad)
        {
            if (!flashlightSelected) return FlashlightPlace.Stowed;
            return hasLoad ? FlashlightPlace.Chest : FlashlightPlace.Hand;
        }

        public static FlashlightKey OnFlashlightKey(bool has, FlashlightPlace place, bool hasLoad)
        {
            if (!has) return FlashlightKey.Nothing;
            if (place != FlashlightPlace.Stowed) return FlashlightKey.Toggle;
            // Guardada e com o móvel nas mãos: não tem mão livre para tirar do cinto.
            return hasLoad ? FlashlightKey.Nothing : FlashlightKey.TakeOutAndTurnOn;
        }
    }
}
