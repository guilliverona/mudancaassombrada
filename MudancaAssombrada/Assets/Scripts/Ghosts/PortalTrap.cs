using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // O PORTAL da Câmara Secreta (R13, LEVEL_DESIGN_FASE1_MANSAO.md): a casa do Imitador. Não é um
    // espelho de esconderijo (ele não troca para cá); tem gatilho próprio. Depois que alguém mexe no
    // CARTAZ, quem ficar SOZINHO na Câmara (ninguém a 6 m) por 4 s é puxado: ele sai pelo portal na
    // forma de verdade, sem disfarce. Aos 2 s, o aviso: o vidro escurece e a voz chama.
    // As regras ficam no PortalRules (com testes); aqui é o host olhando quem está lá dentro.
    public class PortalTrap : MonoBehaviour
    {
        static readonly List<PortalTrap> all = new();
        public static IReadOnlyList<PortalTrap> All => all;

        [Tooltip("A Câmara: quem está dentro desta caixa (centro e tamanho, no mundo) conta.")]
        [SerializeField] Vector3 roomCenter;
        [SerializeField] Vector3 roomSize = new(6f, 3.2f, 8f);
        [Tooltip("O cartaz (a relíquia): o portal só arma depois que alguém mexe nele.")]
        [SerializeField] Furniture relic;
        [SerializeField] float warnAt = 2f, pullAt = 4f, aloneRadius = 6f;
        [Tooltip("Depois de puxar alguém, o portal descansa tantos segundos.")]
        [SerializeField] float rest = 20f;
        [SerializeField] float exitDistance = 0.8f;

        readonly Dictionary<NetworkPlayer, float> alone = new();
        readonly List<Vector3> positions = new();
        bool armed;
        float resting;

        public Vector3 ExitPoint => transform.position + transform.forward * exitDistance;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public void Configure(Vector3 center, Vector3 size, Furniture relicFurniture)
        {
            roomCenter = center;
            roomSize = size;
            relic = relicFurniture;
        }

        void Update()
        {
            var net = NetworkManager.Singleton;
            if (net == null || !net.IsServer) return;
            if (!armed && relic != null && relic.IsHeld) armed = true;
            if (resting > 0) { resting -= Time.deltaTime; return; }

            positions.Clear();
            foreach (var p in NetworkPlayer.All)
                if (!p.Condition.IsDown) positions.Add(p.transform.position);

            var bounds = new Bounds(roomCenter, roomSize);
            foreach (var p in NetworkPlayer.All)
            {
                bool inside = !p.Condition.IsDown && bounds.Contains(p.transform.position);
                bool lonely = inside && PortalRules.IsAlone(p.transform.position, positions, aloneRadius);
                alone.TryGetValue(p, out float t);
                var tick = PortalRules.Tick(armed, lonely, t, Time.deltaTime, warnAt, pullAt);
                alone[p] = tick.AloneTime;
                if (tick.Warn)
                {
                    GameCues.Emit(new GameCue(p.transform.position, 8f, Senses.Hearing, "Uma voz rouca, de dentro do portal: \"Fica... só mais um pouco...\"", gameObject));
                    GameCues.Emit(new GameCue(p.transform.position, 8f, Senses.Sight | Senses.Vibration, "O vidro do portal escureceu. Não fique aqui sozinho.", gameObject));
                }
                if (tick.Pull && Pull(p)) { resting = rest; alone.Clear(); return; }
            }
        }

        static bool Pull(NetworkPlayer victim, Vector3 exit)
        {
            foreach (var g in Ghost.All)
                if (g.TryGetComponent(out GhostImitator imitator) && imitator.ServerAmbushFromPortal(victim, exit))
                    return true;
            return false;
        }

        bool Pull(NetworkPlayer victim) => Pull(victim, ExitPoint);

        // Só no host (ao reiniciar a fase).
        public void ServerReset()
        {
            armed = false;
            resting = 0f;
            alone.Clear();
        }
    }
}
