using System.Collections;
using UnityEngine;

namespace MudancaAssombrada
{
    // O DIAPASÃO da Dona Cida (tecla Q, LEVEL_DESIGN_FASE1.md MEC-01). Ex-afinadora de pianos: ela
    // bate o diapasão e escuta como a casa responde. No eco, a onda anda devagar e cada coisa
    // "canta" com o seu material (SurfaceTag): madeira, vidro, metal... a pessoa estremece e a
    // cópia do Imitador fica muda. Os espelhos respondem com um som: tom limpo se estão vazios,
    // zumbido rachado se o Imitador está escondido dentro (MEC-03). É assim que ela audita a casa.
    //
    // Custo: 15 m de som (qualquer um ouve onde ela está) e 8 s até poder bater de novo.
    // Fica no Player, só no computador do dono; desligado para quem não tem diapasão na ficha.
    public class TuningFork : MonoBehaviour
    {
        [SerializeField] float range = 15f;
        [SerializeField] float cooldown = 8f;
        [Tooltip("Até onde os espelhos respondem com som (o raio da onda do diapasão).")]
        [SerializeField] float mirrorRange = 8f;
        [Tooltip("Velocidade da onda do diapasão (igual à do EchoSystem): o espelho responde quando ela chega.")]
        [SerializeField] float waveSpeed = 2f;

        float nextUse;

        // 0 = acabou de usar, 1 = pronto (para a HUD).
        public float Ready01 => Mathf.Clamp01(1f - (nextUse - Time.time) / cooldown);

        void Update()
        {
            if (PauseMenu.IsOpen || !GameInput.Fork.WasPressedThisFrame() || Time.time < nextUse) return;
            nextUse = Time.time + cooldown;

            Vector3 origin = transform.position + Vector3.up * 0.3f;
            // Um som como outro qualquer: o SoundRelay leva para os outros, o GameAudio toca o diapasão.
            EchoSystem.Emit(new SoundEvent(origin, range, 1f, SoundType.Tuning, gameObject));

            // A resposta de cada espelho perto, quando a onda chega nele (só para quem bateu).
            foreach (var hideout in GhostHideout.All)
            {
                if (hideout is not Mirror mirror) continue;
                float d = Vector3.Distance(origin, mirror.transform.position + Vector3.up);
                if (d <= mirrorRange) StartCoroutine(Answer(mirror, d / waveSpeed));
            }
        }

        IEnumerator Answer(Mirror mirror, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (mirror == null) yield break;
            GameAudio.Play(mirror.IsOccupied ? "diapasao_vidro_rachado" : "diapasao_vidro", mirror.transform.position + Vector3.up);
        }
    }
}
