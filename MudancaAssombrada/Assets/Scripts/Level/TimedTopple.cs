using UnityEngine;

namespace MudancaAssombrada
{
    // Um objeto que TOMBA SOZINHO num momento da noite (o manequim do Corredor dos Espelhos, aos 2:40).
    // É susto e é comédia: o "vento" derruba um manequim, e todo mundo acha que foi o fantasma.
    //
    // Sem rede própria: o relógio da noite (GameManager.TimeLeft) já chega a todos os computadores,
    // então cada um derruba o seu na mesma hora. Reiniciou a fase (o relógio voltou)? Fica de pé de novo.
    // Na queda: uma batida (SoundEvent de impacto, que vira eco, anel de vibração e som) e um aviso
    // para quem ouve ou sente. QUAL som toca vem de um SoundVoice no objeto (Impact -> "manequim_tomba").
    public class TimedTopple : MonoBehaviour
    {
        [Tooltip("Segundos de noite até cair.")]
        [SerializeField] float atSecond = 160f;
        [Tooltip("Para onde cai, no espaço do próprio objeto (X = para a direita dele).")]
        [SerializeField] Vector3 fallDirection = Vector3.right;
        [Tooltip("Metade da base: o objeto gira pela quina do lado da queda.")]
        [SerializeField] float pivotOffset = 0.2f;
        [SerializeField] float duration = 0.8f;
        [SerializeField] float soundRange = 12f;
        [SerializeField] string cue = "Algo de madeira caiu no corredor.";

        Vector3 restPosition;
        Quaternion restRotation;
        float fall = -1f; // -1 = de pé; 0..1 = caindo; 1 = no chão

        void Awake()
        {
            restPosition = transform.position;
            restRotation = transform.rotation;
        }

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsSpawned || !game.HasClock) return;
            float elapsed = game.NightDuration - game.TimeLeft;

            if (fall < 0)
            {
                if (elapsed < atSecond || game.Outcome != MatchOutcome.Playing) return;
                // Quem entrou na partida depois da queda já encontra o manequim no chão, sem barulho.
                fall = elapsed > atSecond + 3f ? 1f : 0f;
                Pose(fall);
                return;
            }
            if (elapsed < atSecond - 1f) { StandUp(); return; } // a fase reiniciou
            if (fall >= 1f) return;

            fall = Mathf.Min(1f, fall + Time.deltaTime / duration);
            Pose(fall * fall); // ao quadrado: começa devagar e acelera, como quem cai de verdade
            if (fall >= 1f) Land();
        }

        void Pose(float t)
        {
            Vector3 dir = restRotation * fallDirection;
            dir.y = 0;
            dir.Normalize();
            Vector3 pivot = restPosition + dir * pivotOffset;
            var turn = Quaternion.AngleAxis(90f * t, Vector3.Cross(Vector3.up, dir));
            transform.SetPositionAndRotation(pivot + turn * (restPosition - pivot), turn * restRotation);
        }

        void StandUp()
        {
            fall = -1f;
            transform.SetPositionAndRotation(restPosition, restRotation);
        }

        // Bateu no chão: som, eco (a Cida "vê" a batida), vibração (o Tiago vê o anel) e o aviso.
        void Land()
        {
            Vector3 at = restPosition + Vector3.up * 0.3f;
            EchoSystem.EmitLocal(new SoundEvent(at, soundRange, 0.8f, SoundType.Impact, gameObject));
            if (!string.IsNullOrEmpty(cue))
                GameCues.EmitLocal(new GameCue(at, soundRange, Senses.Hearing | Senses.Vibration, cue, gameObject));
        }
    }
}
