using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // Os sons de um fantasma ligados ao ESTADO dele (Ghost.State): o vidro trincando quando o
    // Imitador é desmascarado, o zumbido do espelho enquanto ele puxa alguém, os sussurros
    // enquanto ele espera escondido...
    //
    // Não precisa de rede própria: o estado já é uma NetworkVariable, e o Ghost.StateChanged
    // avisa em TODOS os computadores. Cada um toca o som no seu, a partir da posição do fantasma.
    // É só ÁUDIO: quem decide o que o jogo "ouve" (eco, outros fantasmas) continua sendo o SoundEvent.
    //
    // Tudo é dado (Inspector): fantasma novo com sons novos = preencher as listas, sem código.
    [RequireComponent(typeof(Ghost))]
    public class GhostSounds : MonoBehaviour
    {
        // Um som que toca UMA vez numa troca de estado. Ex.: Pulling -> Patrolling = "captura".
        [Serializable]
        public struct Transition
        {
            [Tooltip("Ligado: vale vindo de qualquer estado (ignora 'from').")]
            public bool anyFrom;
            public GhostState from;
            public GhostState to;
            public string sound;
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
        }

        public Transition[] transitions = Array.Empty<Transition>();
        public Loop[] loops = Array.Empty<Loop>();

        Ghost ghost;
        AudioSource loopSource; // filho do fantasma: o som anda junto com ele
        Loop current;
        bool hasLoop, loopIsThreat;
        float nextRepeat;

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
            foreach (var t in transitions)
                if (t.to == next && (t.anyFrom || t.from == previous))
                    GameAudio.Play(t.sound, loopSource.transform.position);
            StartLoop(next);
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
                    nextRepeat = Time.time + UnityEngine.Random.Range(0.3f, 1f) * l.repeatEvery;
                }
                return;
            }
        }

        void Update()
        {
            if (!hasLoop) return;
            if (current.repeatEvery <= 0)
            {
                // Loop de ameaça (o espelho puxando): segura a cama abaixada enquanto toca.
                if (loopIsThreat && loopSource.isPlaying) GameAudio.Duck(0.2f);
                return;
            }
            if (Time.time < nextRepeat) return;
            // Pelo GameAudio: respeita o intervalo mínimo e o contratempo do relógio do som.
            GameAudio.Play(current.sound, loopSource.transform.position, current.volume);
            nextRepeat = Time.time + UnityEngine.Random.Range(0.7f, 1.3f) * current.repeatEvery;
        }
    }
}
