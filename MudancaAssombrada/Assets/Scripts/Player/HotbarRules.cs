namespace MudancaAssombrada
{
    // O que cabe num espaço da hotbar. byte: os 3 espaços cabem num número só (ver Get/Set).
    // Item novo = um nome aqui (no fim, para não trocar o número dos que já existem).
    public enum HotbarItem : byte
    {
        None = 0,
        Flashlight = 1,
        Battery = 2,  // um pacote de pilhas guardado: "Usar" recarrega a lanterna
        Cane = 3,     // a bengala da Cida (vem da ficha, não se pega no chão)
    }

    // Onde a lanterna está: na mão (aponta para onde você olha), presa no cinto (você carrega
    // um móvel: aponta para a frente do corpo, mais fraca) ou guardada no cinto (apagada).
    public enum FlashlightPlace : byte
    {
        Stowed = 0,
        Hand = 1,
        Chest = 2,
    }

    // O que a tecla própria de um item (L da lanterna, Espaço da bengala) faz agora.
    public enum ItemKey
    {
        Nothing,
        Use,            // está na mão (ou no cinto): usa (liga/desliga, bate a bengala)
        TakeOutAndUse,  // estava guardado: vai para a mão (a lanterna, carregando: o cinto) e já usa
    }

    // As REGRAS da hotbar, sem cena nem rede: o que cada tecla faz, onde a lanterna fica, o kit de
    // cada personagem. O Hotbar, o Flashlight e o CaneTapper (no Player) juntam os dados e perguntam
    // aqui; os testes (Assets/Tests/EditMode/HotbarRulesTests.cs) conferem cada regra sem abrir uma fase.
    //
    // Os espaços vão "empacotados" num uint: 8 bits por espaço (espaço 0 nos bits 0-7, espaço 1
    // nos 8-15...). Assim a barra inteira é UMA variável de rede, e não uma lista.
    // 'slots' = quantos espaços ESTE personagem tem (o Marcelo, desajeitado com coisa pequena, tem 2).
    public static class HotbarRules
    {
        public const int Slots = 3;      // o máximo (as teclas e os quadradinhos da HUD)
        public const int EmptyHand = -1; // nenhum espaço escolhido: mão vazia
        public const float FullCharge = 0.99f; // a partir daqui a lanterna conta como cheia

        public static bool IsSlot(int slot, int slots = Slots) => slot >= 0 && slot < slots && slot < Slots;

        public static HotbarItem Get(uint bar, int slot) =>
            IsSlot(slot) ? (HotbarItem)((bar >> (slot * 8)) & 0xFF) : HotbarItem.None;

        public static uint Set(uint bar, int slot, HotbarItem item)
        {
            if (!IsSlot(slot)) return bar;
            int shift = slot * 8;
            return (bar & ~(0xFFu << shift)) | ((uint)item << shift);
        }

        // O primeiro espaço com este item (ou -1). Com None, é o primeiro espaço livre.
        public static int Find(uint bar, HotbarItem item, int slots = Slots)
        {
            for (int i = 0; i < slots && i < Slots; i++)
                if (Get(bar, i) == item) return i;
            return -1;
        }

        public static int FirstEmpty(uint bar, int slots = Slots) => Find(bar, HotbarItem.None, slots);
        public static bool Contains(uint bar, HotbarItem item) => item != HotbarItem.None && Find(bar, item) >= 0;

        // Tecla de número: segura aquele espaço; a mesma tecla de novo guarda (mão vazia).
        public static int Press(int selected, int pressed, int slots = Slots)
        {
            if (!IsSlot(pressed, slots)) return selected;
            return pressed == selected ? EmptyHand : pressed;
        }

        // Rodinha / direcional: anda 'steps' espaços dando a volta. De mão vazia, começa pela ponta.
        public static int Scroll(int selected, int steps, int slots = Slots)
        {
            if (steps == 0 || slots <= 0) return selected;
            if (!IsSlot(selected, slots)) return steps > 0 ? 0 : slots - 1;
            int next = (selected + steps) % slots;
            return next < 0 ? next + slots : next;
        }

        public static HotbarItem InHand(uint bar, int selected) => Get(bar, selected);

        // Mão livre (nada escolhido, ou o espaço escolhido está vazio)? Então o item que acabou de
        // chegar já vai para a mão.
        public static bool HandIsFree(uint bar, int selected) => InHand(bar, selected) == HotbarItem.None;

        // O KIT do personagem, aplicado ao nascer, ao trocar de personagem (testes) e ao reiniciar:
        //   - itens em espaços que ele não tem (Marcelo: o 3º) vão para um espaço livre, ou caem;
        //   - quem tem bengala na ficha recebe a bengala (de preferência no espaço 1). Ela é a visão
        //     da Cida: com a barra cheia, toma o lugar do item do espaço 1;
        //   - quem não tem bengala na ficha perde a bengala.
        public static uint ApplyKit(uint bar, int slots, bool hasCane)
        {
            for (int i = 0; i < Slots; i++)
                if (Get(bar, i) == HotbarItem.Cane && !hasCane) bar = Set(bar, i, HotbarItem.None);

            for (int i = slots; i < Slots; i++)
            {
                var item = Get(bar, i);
                if (item == HotbarItem.None) continue;
                bar = Set(bar, i, HotbarItem.None);
                int free = FirstEmpty(bar, slots);
                if (free >= 0) bar = Set(bar, free, item);
            }

            if (hasCane && Find(bar, HotbarItem.Cane, slots) < 0)
            {
                int free = Get(bar, 0) == HotbarItem.None ? 0 : FirstEmpty(bar, slots);
                bar = Set(bar, free >= 0 ? free : 0, HotbarItem.Cane);
            }
            return bar;
        }

        // Móvel nas mãos (primeira ponta ou outra ponta) manda o item da mão para o cinto. A lanterna
        // é a exceção boa: vai presa no cinto e continua acesa.
        public static FlashlightPlace PlaceOf(bool flashlightSelected, bool hasLoad)
        {
            if (!flashlightSelected) return FlashlightPlace.Stowed;
            return hasLoad ? FlashlightPlace.Chest : FlashlightPlace.Hand;
        }

        // A tecla própria de um item faz o mesmo que a tecla do número + usar: guardado, ele é
        // escolhido (e já usado). Assim o jogador nunca consegue por um caminho o que o outro nega.
        public static ItemKey OnItemKey(bool has, bool inHand)
        {
            if (!has) return ItemKey.Nothing;
            return inHand ? ItemKey.Use : ItemKey.TakeOutAndUse;
        }

        // Com o móvel nas mãos, a lanterna escolhida vai para o cinto (PlaceOf): conta como "na mão".
        public static ItemKey OnFlashlightKey(bool has, FlashlightPlace place) =>
            OnItemKey(has, place != FlashlightPlace.Stowed);

        // Trocar as pilhas guardadas: precisa de uma lanterna que não esteja cheia e das mãos livres
        // (desatarraxar a tampa com um móvel nos braços, não dá).
        public static bool CanReload(bool hasFlashlight, float charge01, bool hasLoad) =>
            hasFlashlight && charge01 < FullCharge && !hasLoad;

        // A cartela de pilhas aparece na mão direita (HeldItems) com o espaço dela escolhido e as mãos
        // livres; com um móvel nos braços ela fica guardada (não vai para o cinto, como a lanterna).
        public static bool ShowsBatteryInHand(HotbarItem inHand, bool hasLoad) =>
            inHand == HotbarItem.Battery && !hasLoad;
    }
}
