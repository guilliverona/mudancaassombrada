using UnityEngine;

// A visão do Tiago: ele não ouve, mas VÊ as vibrações.
// Todo som do jogo (passos do fantasma, móveis batendo) vira um anel no chão.
// Desligar este componente (enabled = false) desliga a visão; o CharacterSwitcher faz isso.
public class VibrationVision : MonoBehaviour
{
    [SerializeField] float ringLifetime = 2.5f;
    [SerializeField] Color weakColor = new Color(1f, 0.85f, 0.3f);  // vibração fraca: amarelo
    [SerializeField] Color strongColor = new Color(1f, 0.2f, 0.3f); // vibração forte: vermelho

    // Mesmo evento que o fantasma usa para ouvir. Um evento, vários "ouvintes" diferentes:
    // o fantasma persegue, o Tiago enxerga. Nenhum dos dois sabe que o outro existe.
    void OnEnable() => EchoSystem.SoundEmitted += OnSound;
    void OnDisable() => EchoSystem.SoundEmitted -= OnSound;

    void OnSound(Vector3 position, float range, float intensity, GameObject source)
    {
        if (source == gameObject) return; // os próprios passos não interessam (só poluiriam a tela)

        // Os sons nascem no ar (ex.: ponto de impacto). Projetamos no chão com um raio para baixo.
        Vector3 ground = position;
        if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            ground = hit.point;

        // Tamanho e cor dependem da força: um passo leve é um anel pequeno e amarelo,
        // o fantasma correndo é um anel grande e vermelho.
        float size = Mathf.Lerp(0.3f, 1.5f, intensity);
        Color color = Color.Lerp(weakColor, strongColor, intensity);
        VibrationRing.Spawn(ground + Vector3.up * 0.03f, size, color, ringLifetime);
    }
}
