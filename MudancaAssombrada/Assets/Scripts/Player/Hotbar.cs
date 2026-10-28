using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // A HOTBAR (fica no Player): 3 espaços para itens pequenos, como a lanterna. As teclas 1, 2 e 3
    // seguram o item daquele espaço; a mesma tecla de novo guarda (mão vazia). A rodinha do mouse
    // (ou o direcional, para os lados) passa de um espaço para outro.
    //
    // Móvel nas mãos manda o item para o cinto: ele continua escolhido, mas a mão está ocupada.
    // Quem decide o que isso significa para cada item é o próprio item (a lanterna vai para o colete).
    // As regras ficam em HotbarRules; aqui só junta teclas, rede e o que está em cada espaço.
    //
    // REDE: o host decide O QUE tem em cada espaço (pegar é dele, como um móvel); qual espaço está
    // na mão é do DONO (é ele que aperta a tecla), e todos veem.
    public class Hotbar : NetworkBehaviour
    {
        readonly NetworkVariable<uint> bar = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<sbyte> selected = new(
            HotbarRules.EmptyHand, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        FurnitureCarrier carrier;

        public int Selected => selected.Value;
        public HotbarItem InHand => HotbarRules.InHand(bar.Value, selected.Value);
        public bool HasRoom => HotbarRules.FirstEmpty(bar.Value) >= 0;
        public bool IsEmpty => bar.Value == 0;
        // Mãos ocupadas por um móvel (só vale no dono: é ele que sabe o que carrega).
        public bool HandsBusy => carrier != null && carrier.HasLoad;
        // Hora (Time.time) da última mudança: a HUD mostra a barra por uns segundos e depois some.
        public float ChangedAt { get; private set; } = float.NegativeInfinity;

        public HotbarItem Get(int slot) => HotbarRules.Get(bar.Value, slot);
        public int Find(HotbarItem item) => HotbarRules.Find(bar.Value, item);
        public bool Contains(HotbarItem item) => HotbarRules.Contains(bar.Value, item);

        void Awake() => carrier = GetComponent<FurnitureCarrier>();

        public override void OnNetworkSpawn()
        {
            bar.OnValueChanged += OnChanged;
            selected.OnValueChanged += OnSelectedChanged;
        }

        public override void OnNetworkDespawn()
        {
            bar.OnValueChanged -= OnChanged;
            selected.OnValueChanged -= OnSelectedChanged;
        }

        void OnChanged(uint previous, uint current) => ChangedAt = Time.time;
        void OnSelectedChanged(sbyte previous, sbyte current) => ChangedAt = Time.time;

        void Update()
        {
            if (!IsSpawned || !IsOwner || PauseMenu.IsOpen) return;

            for (int i = 0; i < HotbarRules.Slots && i < GameInput.HotbarSlot.Length; i++)
                if (GameInput.HotbarSlot[i].WasPressedThisFrame()) Select(HotbarRules.Press(selected.Value, i));

            // Rodinha para baixo (valor negativo) = próximo espaço, como na maioria dos jogos.
            float scroll = GameInput.HotbarScroll.ReadValue<float>();
            int steps = scroll < 0f ? 1 : scroll > 0f ? -1 : 0;
            if (GameInput.HotbarNext.WasPressedThisFrame()) steps++;
            if (GameInput.HotbarPrevious.WasPressedThisFrame()) steps--;
            if (steps != 0) Select(HotbarRules.Scroll(selected.Value, steps));
        }

        // Só no dono: segura este espaço (ou EmptyHand para guardar tudo).
        public void Select(int slot)
        {
            if (!IsOwner || slot == selected.Value) return;
            selected.Value = (sbyte)slot;
        }

        // ---------- Só no host (chamados por quem dá ou tira itens, e pelo reinício da fase) ----------

        // Guarda o item no primeiro espaço livre. Mão livre: ele já vai para a mão. false = barra cheia.
        public bool ServerAdd(HotbarItem item)
        {
            int slot = HotbarRules.FirstEmpty(bar.Value);
            if (slot < 0) return false;
            bool handFree = HotbarRules.HandIsFree(bar.Value, selected.Value);
            bar.Value = HotbarRules.Set(bar.Value, slot, item);
            if (handFree) HoldRpc(slot);
            return true;
        }

        public void ServerClear()
        {
            bar.Value = 0;
            HoldRpc(HotbarRules.EmptyHand);
        }

        // Qual espaço está na mão é do dono: o host pede e o computador dele muda.
        [Rpc(SendTo.Owner)]
        void HoldRpc(int slot)
        {
            if (slot != selected.Value) selected.Value = (sbyte)slot;
        }
    }
}
