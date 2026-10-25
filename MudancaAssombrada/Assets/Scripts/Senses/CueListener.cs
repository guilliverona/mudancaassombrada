using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // Fica no Player (só no MEU boneco). Decide o que EU percebi dos acontecimentos do jogo,
    // pelos sentidos da ficha do meu personagem, e guarda as últimas linhas para a HUD mostrar.
    // "Lógica não desenha": aqui só se decide; quem escreve na tela é a Hud.
    public class CueListener : MonoBehaviour
    {
        [SerializeField] int maxLines = 4;
        [SerializeField] float lineDuration = 6f; // segundos que cada linha fica na tela

        public readonly struct Line
        {
            public readonly string Text;
            public readonly Senses Sense; // o sentido por onde chegou (para a HUD marcar)
            public readonly float Time;

            public Line(string text, Senses sense, float time)
            {
                Text = text;
                Sense = sense;
                Time = time;
            }
        }

        readonly List<Line> lines = new();
        CharacterSwitcher character;

        // As linhas que ainda valem, da mais antiga para a mais nova.
        public IReadOnlyList<Line> Lines
        {
            get
            {
                // unscaledTime: as linhas somem mesmo com o jogo congelado (fim de partida).
                lines.RemoveAll(l => UnityEngine.Time.unscaledTime - l.Time > lineDuration);
                return lines;
            }
        }

        void Awake() => character = GetComponent<CharacterSwitcher>();

        void OnEnable() => GameCues.Emitted += OnCue;
        void OnDisable() => GameCues.Emitted -= OnCue;

        void OnCue(GameCue cue)
        {
            if (character == null || !cue.Reaches(transform.position)) return;

            // Qual dos MEUS sentidos pega este aviso? A visão primeiro (é o mais detalhado).
            Senses mine = character.Current.Senses & cue.Senses;
            Senses sense = (mine & Senses.Sight) != 0 ? Senses.Sight
                         : (mine & Senses.Hearing) != 0 ? Senses.Hearing
                         : (mine & Senses.Vibration) != 0 ? Senses.Vibration
                         : Senses.None;
            if (sense == Senses.None) return; // nenhum sentido meu alcança: não fiquei sabendo

            lines.Add(new Line(cue.Text, sense, UnityEngine.Time.unscaledTime));
            if (lines.Count > maxLines) lines.RemoveAt(0);
        }
    }
}
