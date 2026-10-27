using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Um instrumento que dá para TOCAR (Interagir): o piano do hall. Cada toque é um som de
    // MÚSICA como outro qualquer (SoundEvent): todos ouvem (o arquivo vem do SoundVoice do
    // instrumento), a Cida vê a onda no eco, o Tiago vê a vibração... e fantasma que ouve música
    // também ouve. É divertido e é barulho: tocar tem preço.
    //
    // Precisa de um Collider (para a mira) e de um NetworkObject: quem emite o som é o host
    // (dono do instrumento), e o SoundRelay leva para os outros.
    public class PlayableInstrument : NetworkBehaviour, IInteractionTarget
    {
        [Tooltip("O verbo da dica: \"Tocar\" vira \"Tocar Piano\".")]
        [SerializeField] string actionName = "Tocar";
        [SerializeField] float range = 14f;
        [SerializeField, Range(0f, 1f)] float intensity = 0.8f;
        [Tooltip("Segundos entre um toque e outro (evita a metralhadora de notas).")]
        [SerializeField] float cooldown = 1.5f;
        [Tooltip("Altura de onde o som sai (o teclado).")]
        [SerializeField] float soundHeight = 1f;

        float nextPlay; // só no host

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (!IsSpawned) return InteractionPrompt.None;
            // Piano tocando sozinho (fonte de ruído ligada): para quem pode afinar, a dica é "Afinar".
            if (TryGetComponent(out NoiseSource noise) && noise.IsActive &&
                TryGetComponent(out Interactable tune) && tune.CanBeUsedBy(player.Character))
                return InteractionPrompt.None;
            return new InteractionPrompt($"{actionName} {name}", true);
        }

        public void Interact(PlayerInteractor player) => PlayRpc();

        [Rpc(SendTo.Server)]
        void PlayRpc()
        {
            if (Time.time < nextPlay) return;
            nextPlay = Time.time + cooldown;
            EchoSystem.Emit(new SoundEvent(transform.position + Vector3.up * soundHeight, range, intensity, SoundType.Music, gameObject));
        }
    }
}
