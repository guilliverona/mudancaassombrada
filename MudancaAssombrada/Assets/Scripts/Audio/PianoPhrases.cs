using UnityEngine;

namespace MudancaAssombrada
{
    // AS FRASES DE PIANO da Mansão (LEVEL_DESIGN_FASE1_MANSAO.md §7, camada 3): frases curtas
    // disparadas por momentos da noite, por cima da cama.
    //   aparição   um glissando que desce junto com ele pela escadaria (3D, de onde ele está)
    //   etapa nova um tema curto
    //   âmbar      o tema volta (a corrida final, aos 75% da noite)
    //   captura    um acorde grave (alguém caiu)
    //   amanhecer  um acorde maior: a ÚNICA vez na noite
    // Fica na pianola. Cada computador toca as suas, lendo o que já chega pela rede (etapa, relógio,
    // quem caiu, o resultado): nada de rede própria.
    public class PianoPhrases : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float amberAt = 0.75f;

        SoundDirector.Moment lastMoment;
        int lastStage = -1, lastDown;
        float lastProgress;
        bool amberPlayed, dawnPlayed;

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsSpawned) return;

            // A fase reiniciou (o relógio voltou): as frases podem tocar de novo.
            if (game.Outcome == MatchOutcome.Playing && game.NightProgress < lastProgress - 0.01f)
            {
                amberPlayed = dawnPlayed = false;
                lastStage = -1;
            }
            lastProgress = game.NightProgress;

            var moment = SoundDirector.Current;
            if (moment == SoundDirector.Moment.Appearance && lastMoment != SoundDirector.Moment.Appearance)
                GameAudio.Play("piano_glissando_descida", PerformerPosition());
            lastMoment = moment;

            if (game.StageIndex != lastStage)
            {
                if (lastStage >= 0 && game.StageIndex > lastStage) GameAudio.PlayUi("piano_tema_etapa");
                lastStage = game.StageIndex;
            }

            if (!amberPlayed && game.HasClock && game.Outcome == MatchOutcome.Playing && game.NightProgress >= amberAt)
            {
                amberPlayed = true;
                GameAudio.PlayUi("piano_tema_etapa");
            }

            int down = 0;
            foreach (var p in NetworkPlayer.All)
                if (p.Condition != null && p.Condition.IsDown) down++;
            if (down > lastDown) GameAudio.PlayUi("piano_acorde_captura");
            lastDown = down;

            if (!dawnPlayed && (game.Outcome == MatchOutcome.Won || game.Outcome == MatchOutcome.Dawn))
            {
                dawnPlayed = true;
                GameAudio.PlayUi("piano_acorde_amanhecer");
            }
        }

        // De onde vem o glissando: do fantasma que está se apresentando (descendo a escadaria).
        Vector3 PerformerPosition()
        {
            foreach (var g in Ghost.All)
                if (g.IsSpawned && g.State == GhostState.Performing) return g.transform.position + Vector3.up;
            return transform.position;
        }
    }
}
