using System;
using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // Os sons de um fantasma ligados ao ESTADO dele (Ghost.State): o zumbido do espelho enquanto
    // o Imitador puxa alguém, a voz rouca ("minha casa... minha fama...") enquanto ele espera dentro
    // do espelho, o "Errei... a fala..." quando é desmascarado...
    //
    // Não precisa de rede própria: o estado já é uma NetworkVariable, e o Ghost.StateChanged
    // avisa em TODOS os computadores. Cada um toca o som no seu, a partir da posição do fantasma.
    // É só ÁUDIO: quem decide o que o jogo "ouve" (eco, outros fantasmas) continua sendo o SoundEvent.
    //
    // Tudo é dado (Inspector): fantasma novo com sons novos = preencher as listas, sem código.
    [RequireComponent(typeof(Ghost))]
    public class GhostSounds : MonoBehaviour
    {
        // Um som que toca UMA vez numa troca de estado. Ex.: Pulling -> Bowing = "captura".
        [Serializable]
        public struct Transition
        {
            [Tooltip("Ligado: vale vindo de qualquer estado (ignora 'from').")]
            public bool anyFrom;
            public GhostState from;
            public GhostState to;
            public string sound;
            [Tooltip("Segundos depois da troca (para uma fala não cair em cima de outro som da mesma troca).")]
            public float delay;
            [Tooltip("Chance de NÃO tocar (0 = sempre toca). Aviso que vem sempre vira rotina.")]
            [Range(0, 1)] public float skipChance;
            [Tooltip("Ligado: toca colado em quem ouve, sem direção (não entrega onde o fantasma está).")]
            public bool everywhere;
        }

        // Um som que fica tocando ENQUANTO o fantasma está num estado.
        [Serializable]
        public struct Loop
        {
            public GhostState state;
            public string sound;
            [Tooltip("0 = toca o arquivo em loop sem parar. Maior que 0 = repete o som a cada tantos " +
                     "segundos (com um pouco de sorteio), para sons curtos como sussurros.")]
            public float repeatEvery;
            [Range(0, 1)] public float volume;
            [Tooltip("Ligado: toca colado em quem ouve, sem direção (\"a casa\" sussurra). Para o som " +
                     "não entregar onde o fantasma está (ex.: o Imitador dentro de um espelho).")]
            public bool everywhere;
            [Tooltip("A primeira vez só depois de tantos segundos no estado (0 = um sorteio dentro do primeiro intervalo).")]
            public float firstDelay;
            [Tooltip("No último quarto da noite (a corrida até o amanhecer), repete a cada tantos segundos. 0 = igual.")]
            public float finaleEvery;
        }

        public Transition[] transitions = Array.Empty<Transition>();
        public Loop[] loops = Array.Empty<Loop>();

        const float FinaleFrom = 0.75f; // a mesma virada da luz âmbar e do diretor de som

        Ghost ghost;
        AudioSource loopSource; // filho do fantasma: o som anda junto com ele
        Loop current;
        bool hasLoop, loopIsThreat;
        float nextRepeat;

        // Transições com atraso esperando a hora (o som e quando tocar).
        readonly List<(Transition t, float at)> pending = new();

        void Awake()
        {
            ghost = GetComponent<Ghost>();
            var go = new GameObject("Som do Fantasma");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up; // mais ou menos na altura da cabeça
            loopSource = go.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.spatialBlend = 1f;
            loopSource.rolloffMode = AudioRolloffMode.Linear;
            loopSource.minDistance = 1f;
            loopSource.dopplerLevel = 0f;
        }

        void OnEnable() => ghost.StateChanged += OnStateChanged;
        void OnDisable() => ghost.StateChanged -= OnStateChanged;

        void Start() => StartLoop(ghost.State); // quem entra no meio já ouve o estado atual

        void OnStateChanged(GhostState previous, GhostState next)
        {
            pending.Clear(); // o estado mudou de novo: fala atrasada do estado anterior não vale mais
            foreach (var t in transitions)
            {
                if (t.to != next || (!t.anyFrom && t.from != previous)) continue;
                if (t.skipChance > 0 && UnityEngine.Random.value < t.skipChance) continue;
                if (t.delay > 0) pending.Add((t, Time.time + t.delay));
                else PlayTransition(t);
            }
            StartLoop(next);
        }

        void PlayTransition(Transition t) => GameAudio.Play(t.sound, From(t.everywhere));

        // De onde o som sai: do fantasma, ou colado em quem ouve (sem direção).
        Vector3 From(bool everywhere)
        {
            var cam = Camera.main;
            return everywhere && cam != null ? cam.transform.position : loopSource.transform.position;
        }

        void StartLoop(GhostState state)
        {
            loopSource.Stop();
            hasLoop = false;
            foreach (var l in loops)
            {
                if (l.state != state) continue;
                var entry = SoundLibrary.Instance != null ? SoundLibrary.Instance.Get(l.sound) : null;
                if (entry == null) return; // som sem arquivo: silêncio
                current = l;
                hasLoop = true;
                loopIsThreat = entry.layer == SoundLayer.Threat;
                loopSource.volume = entry.volume * l.volume;
                loopSource.maxDistance = entry.maxDistance;
                if (l.repeatEvery <= 0)
                {
                    loopSource.clip = entry.RandomClip();
                    loopSource.loop = true;
                    loopSource.Play();
                }
                else
                {
                    loopSource.loop = false;
                    nextRepeat = Time.time + (l.firstDelay > 0
                        ? l.firstDelay + UnityEngine.Random.Range(0f, 0.3f) * l.repeatEvery
                        : UnityEngine.Random.Range(0.3f, 1f) * l.repeatEvery);
                }
                return;
            }
        }

        void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].at) continue;
                PlayTransition(pending[i].t);
                pending.RemoveAt(i);
            }

            if (!hasLoop) return;
            if (current.repeatEvery <= 0)
            {
                // Loop de ameaça (o espelho puxando): segura a cama abaixada enquanto toca.
                if (loopIsThreat && loopSource.isPlaying) GameAudio.Duck(0.2f);
                return;
            }
            if (Time.time < nextRepeat) return;
            // Pelo GameAudio: respeita o intervalo mínimo, uma ameaça por vez e o contratempo do relógio.
            GameAudio.Play(current.sound, From(current.everywhere), current.volume);
            nextRepeat = Time.time + UnityEngine.Random.Range(0.7f, 1.3f) * RepeatNow();
        }

        // No último quarto da noite, alguns sons vêm mais seguido (a pressa do fim).
        float RepeatNow()
        {
            var game = GameManager.Instance;
            bool finale = current.finaleEvery > 0 && game != null && game.IsSpawned && game.NightProgress >= FinaleFrom;
            return finale ? current.finaleEvery : current.repeatEvery;
        }
    }
}
