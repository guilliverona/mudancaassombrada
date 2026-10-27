using UnityEngine;

namespace MudancaAssombrada
{
    // A visão de vibrações (Tiago): ele não ouve, mas VÊ as vibrações.
    // Todo som do jogo (passos do fantasma, móveis batendo) vira um anel no chão.
    // Desligar este componente (enabled = false) desliga a visão; o CharacterSwitcher faz isso.
    public class VibrationVision : MonoBehaviour
    {
        [SerializeField] float ringLifetime = 2.5f;
        [SerializeField] Color weakColor = new Color(1f, 0.85f, 0.3f);  // vibração fraca: amarelo
        [SerializeField] Color strongColor = new Color(1f, 0.2f, 0.3f); // vibração forte: vermelho
        [Tooltip("Que sons fazem o chão vibrar. Respiração e voz não fazem.")]
        [SerializeField] SoundType visibleSounds = SoundType.Footstep | SoundType.Impact | SoundType.Cane | SoundType.Whistle | SoundType.Music | SoundType.Carry;

        // Mesmo evento que o fantasma usa para ouvir. Um evento, vários "ouvintes" diferentes:
        // o fantasma persegue, o Tiago enxerga. Nenhum dos dois sabe que o outro existe.
        void OnEnable() => EchoSystem.SoundEmitted += OnSound;
        void OnDisable() => EchoSystem.SoundEmitted -= OnSound;

        void OnSound(SoundEvent sound)
        {
            if (sound.Source == gameObject) return; // os próprios passos não interessam (só poluiriam a tela)
            if (!sound.Is(visibleSounds)) return;

            // Os sons nascem no ar (ex.: ponto de impacto). Projetamos no chão com um raio para baixo.
            Vector3 ground = sound.Position;
            if (Physics.Raycast(sound.Position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                ground = hit.point;

            // Tamanho e cor dependem da força: um passo leve é um anel pequeno e amarelo,
            // o fantasma correndo é um anel grande e vermelho.
            float size = Mathf.Lerp(0.3f, 1.5f, sound.Intensity);
            Color color = Color.Lerp(weakColor, strongColor, sound.Intensity);
            VibrationRing.Spawn(ground + Vector3.up * 0.03f, size, color, ringLifetime);
        }
    }
}
