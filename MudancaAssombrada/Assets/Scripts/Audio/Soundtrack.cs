using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // A TRILHA de uma fase, cortada em TRECHOS. A música não toca do começo ao fim: o SoundDirector
    // escolhe o trecho que serve ao momento (calma, crescendo, pânico, aparição, final) e deixa
    // silêncio entre eles. Uma faixa longa vira um "banco" de momentos, como numa trilha de filme
    // editada em cima da cena.
    //
    // Os tempos vêm de ouvir (e medir) a faixa: cada trecho começa e termina num lugar que respira
    // (um vale de volume), para entrar e sair sem "degrau". Fase nova = um asset destes com a faixa dela.
    [CreateAssetMenu(menuName = "Mudança Assombrada/Trilha Sonora", fileName = "Trilha")]
    public class Soundtrack : ScriptableObject
    {
        // Para que serve cada trecho (o SoundDirector pede pelo humor do momento).
        public enum Mood
        {
            Calm,        // explorando: baixo, esparso, deixa a casa falar
            Build,       // algo se aproxima: crescendo depois do silêncio
            Panic,       // o fantasma agindo: o trecho mais pesado
            Appearance,  // a aparição dos 45 s: silêncio, golpe e o grave arrastado
            Finale,      // o último quarto da noite: a corrida
        }

        [Serializable]
        public struct Section
        {
            public string name;
            public Mood mood;
            [Tooltip("Segundos na faixa.")] public float start, end;
            public float Length => end - start;
        }

        [Tooltip("O nome do som na biblioteca (Assets/Resources/Sons), ex.: trilha_casa_espelho.")]
        public string sound = "trilha_casa_espelho";
        public Section[] sections = Array.Empty<Section>();

        // Um trecho deste humor, sorteado, evitando repetir o último que tocou (se houver outro).
        public bool TryPick(Mood mood, string avoid, out Section picked)
        {
            int count = 0, others = 0;
            foreach (var s in sections)
                if (s.mood == mood) { count++; if (s.name != avoid) others++; }
            picked = default;
            if (count == 0) return false;

            bool skipLast = others > 0;
            int target = UnityEngine.Random.Range(0, skipLast ? others : count);
            foreach (var s in sections)
            {
                if (s.mood != mood || (skipLast && s.name == avoid)) continue;
                if (target-- == 0) { picked = s; return true; }
            }
            return false;
        }
    }
}
