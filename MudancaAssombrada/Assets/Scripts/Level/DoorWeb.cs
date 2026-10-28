using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // TEIA DE VÃO (LEVEL_DESIGN_FASE1_MANSAO.md §6.3): uma porta pouco usada começa fechada por teia.
    // Quem passa rasga (som curto de 2 m) e ela fica rasgada para todos (estado de rede).
    //
    // A PISTA (R23): o Imitador flutua e atravessa SEM rasgar. Uma teia inteira num vão por onde
    // "alguém" acabou de passar quer dizer que foi ele. Leitura do Tiago (visão), melhor com lanterna.
    //
    // Sem colisor: o host confere, a cada quadro, quem está dentro da caixa do vão (posições pela
    // rede). Assim a teia não entra no NavMesh nem na mira de ninguém.
    public class DoorWeb : NetworkBehaviour
    {
        static readonly List<DoorWeb> all = new();
        public static IReadOnlyList<DoorWeb> All => all;

        [Tooltip("A caixa do vão (no espaço do objeto): quem entra nela rasga a teia.")]
        [SerializeField] Vector3 size = new(1.2f, 2.2f, 0.5f);

        readonly NetworkVariable<bool> torn = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsTorn => torn.Value;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            torn.OnValueChanged += OnTornChanged;
            Show(!torn.Value);
        }

        public override void OnNetworkDespawn() => torn.OnValueChanged -= OnTornChanged;

        void Update()
        {
            if (!IsServer || !IsSpawned || torn.Value) return;
            // Só os jogadores são conferidos: o fantasma passa e a teia fica (HouseScareRules.TearsWeb).
            foreach (var p in NetworkPlayer.All)
                if (Inside(p.transform.position + Vector3.up) && HouseScareRules.TearsWeb(isPlayer: true)) { torn.Value = true; return; }
        }

        bool Inside(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world) - new Vector3(0, size.y / 2, 0);
            Vector3 half = size / 2;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        void OnTornChanged(bool previous, bool current)
        {
            Show(!current);
            if (current) GameAudio.Play("teia_rasga", transform.position + Vector3.up * 1.6f);
        }

        void Show(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
        }

        // Só no host (ao reiniciar a fase): a teia volta inteira.
        public void ServerReset() => torn.Value = false;

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.8f, 0.8f, 0.9f, 0.4f);
            Gizmos.DrawWireCube(new Vector3(0, size.y / 2, 0), size);
        }
    }
}
