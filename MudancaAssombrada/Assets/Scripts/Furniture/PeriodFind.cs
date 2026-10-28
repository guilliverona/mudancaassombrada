using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // ACHADO DE ÉPOCA (LEVEL_DESIGN_FASE1_MANSAO.md §8.2, D10): um objeto de 1927 fora da lista do
    // chefe. Fica num móvel carregável (Furniture) e não entra nos objetivos: quem vai atrás dele
    // troca tempo por pagamento. Entregue no caminhão, vale um extra, e o Seu Valdemar comenta.
    //
    // Alguns fazem barulho enquanto são carregados (a cristaleira tilinta: ruído de 6 m).
    // O que cada um faz sozinho (o telefone toca, a cadeira balança) é um HouseScare ao lado.
    [RequireComponent(typeof(Furniture))]
    public class PeriodFind : NetworkBehaviour
    {
        static readonly List<PeriodFind> all = new();
        public static IReadOnlyList<PeriodFind> All => all;

        [SerializeField] string displayName = "Achado";
        [Tooltip("Quanto paga a mais, entregue no caminhão (R$).")]
        [SerializeField, Min(0)] int value = 20;
        [Tooltip("O que o Seu Valdemar escreve sobre ele no bilhete.")]
        [SerializeField, TextArea] string comment = "";
        [Tooltip("Som em loop enquanto alguém carrega (vazio = nenhum).")]
        [SerializeField] string carryLoop = "";

        // Marcado pelo host no fim da noite (a HUD lê para montar o bilhete).
        readonly NetworkVariable<bool> delivered = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        Furniture furniture;
        AudioSource loop;

        public string DisplayName => displayName;
        public int Value => value;
        public string Comment => comment;
        public bool Delivered => delivered.Value;
        public Furniture Furniture => furniture;

        void Awake() => furniture = GetComponent<Furniture>();
        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            if (string.IsNullOrEmpty(carryLoop) || SoundLibrary.Instance == null) return;
            var entry = SoundLibrary.Instance.Get(carryLoop);
            if (entry == null) return;
            loop = gameObject.AddComponent<AudioSource>();
            loop.clip = entry.RandomClip();
            loop.loop = true;
            loop.volume = entry.volume;
            loop.spatialBlend = 1f;
            loop.rolloffMode = AudioRolloffMode.Linear;
            loop.maxDistance = entry.maxDistance;
            loop.dopplerLevel = 0f;
        }

        void Update()
        {
            if (loop == null) return;
            bool carried = furniture.IsHeld;
            if (carried && !loop.isPlaying) loop.Play();
            else if (!carried && loop.isPlaying) loop.Stop();
        }

        // Só no host: no fim da noite, confere se está no caminhão.
        public void ServerMarkDelivered() => delivered.Value = TruckZone.IsDelivered(furniture);

        public void ServerReset() => delivered.Value = false;
    }
}
