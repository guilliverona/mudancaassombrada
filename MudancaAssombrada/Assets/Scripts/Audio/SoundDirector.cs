using UnityEngine;

namespace MudancaAssombrada
{
    // O DIRETOR DE SOM da fase: decide o que a trilha faz em cada momento, QUANDO a casa se cala e
    // quanto o resto do som (clima e âncoras) abaixa. A ideia vem do cinema e dos jogos de terror:
    //
    //   - A trilha é uma CAMA baixinha, sempre presente: a faixa inteira tocando em loop por baixo
    //     da casa, sem cobrir o relógio, o gramofone e os passos. No último quarto da noite ela sobe.
    //   - O SILÊNCIO assusta mais que o barulho: quando o falso está perto, a cama some devagar (o
    //     "vácuo") e só o relógio continua. Depois de alguns segundos de vácuo, um crescendo baixo.
    //   - De vez em quando o vácuo é FALSO: a cama afunda sem motivo. Assim o silêncio não vira
    //     detector de fantasma, e o jogador fica nervoso toda vez que ele acontece.
    //   - O pânico (alguém sendo puxado) é o único momento de música alta. Acabou: CORTE seco para o
    //     silêncio, e a cama volta aos poucos de onde parou. Nunca dois sustos colados.
    //   - A aparição dos 45 s tem trecho próprio: 3 s de nada, um golpe, e o grave arrastado.
    //
    // Roda em CADA computador e lê só o que já chega pela rede (estado dos fantasmas e dos jogadores,
    // o relógio da noite): cada um ouve a SUA trilha, do ponto de vista do seu boneco.
    // Fica no objeto "Sons" da cena. Sem ele, a casa toca como antes (sem trilha, sem vácuo).
    public class SoundDirector : MonoBehaviour
    {
        public enum Moment { Calm, Vacuum, Dread, Panic, Appearance, Release, Ended }

        [SerializeField] Soundtrack track;

        [Header("Ritmo (segundos)")]
        [Tooltip("A cama entra baixinho logo no começo da noite.")]
        [SerializeField] float bedAfter = 3f;
        [Tooltip("De quanto em quanto tempo a cama afunda sem motivo (o vácuo falso), mínimo e máximo.")]
        [SerializeField] Vector2 vacuumEvery = new(60f, 100f);
        [SerializeField] Vector2 vacuumLength = new(8f, 12f);
        [SerializeField, Range(0f, 1f)] float finaleFrom = 0.75f;
        [Tooltip("O falso (disfarçado) a menos disto do MEU boneco: a casa se cala (o vácuo de verdade).")]
        [SerializeField] float dreadDistance = 9f;
        [Tooltip("Segundos de vácuo de verdade até entrar o crescendo, baixinho.")]
        [SerializeField] float dreadBuildAfter = 7f;
        [Tooltip("Depois do pânico ou da aparição: silêncio de música por tanto tempo, e a cama volta.")]
        [SerializeField] float releaseSilence = 8f;

        [Header("Volume da trilha (vezes o volume do som na biblioteca)")]
        [Tooltip("A cama de sempre: presente, mas abaixo da casa.")]
        [SerializeField, Range(0f, 1f)] float bedVolume = 0.55f;
        [Tooltip("A cama no último quarto da noite (a corrida até o amanhecer).")]
        [SerializeField, Range(0f, 1f)] float bedFinaleVolume = 0.75f;
        [Tooltip("No vácuo falso, a cama afunda até isto da cama normal.")]
        [SerializeField, Range(0f, 1f)] float vacuumBed = 0.25f;
        [SerializeField, Range(0f, 1f)] float buildVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] float panicVolume = 1f;
        [SerializeField, Range(0f, 1f)] float appearanceVolume = 1f;

        [Header("O resto da casa (clima e âncoras) em cada momento")]
        [SerializeField, Range(0f, 1f)] float vacuumAmbience = 0.35f;
        [SerializeField, Range(0f, 1f)] float dreadAmbience = 0.2f;
        [SerializeField, Range(0f, 1f)] float panicAmbience = 0.25f;
        [SerializeField, Range(0f, 1f)] float appearanceAmbience = 0.25f;

        [Header("Fades (segundos)")]
        [SerializeField] float fadeIn = 6f;           // a cama entra e volta devagar
        [SerializeField] float fadeOut = 2.5f;
        [SerializeField] float cut = 0.25f;           // o corte seco depois do susto
        [SerializeField] float ambienceDrop = 2.5f;   // a casa some em ~2,5 s...
        [SerializeField] float ambienceReturn = 10f;  // ...e volta devagar, em ~10 s

        // Quanto o clima e as âncoras tocam agora (1 = normal). O AmbientSound multiplica por isto.
        public static float AmbienceGain { get; private set; } = 1f;
        public static Moment Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AmbienceGain = 1f;
            Current = Moment.Calm;
        }

        readonly AudioSource[] decks = new AudioSource[2]; // dois "toca-discos" para trocar com crossfade
        readonly float[] deckTarget = new float[2], deckSpeed = new float[2];
        int deck;
        AudioClip clip;
        float baseVolume;

        Soundtrack.Section section; // o trecho tocando agora (de susto: pânico, aparição, crescendo)
        bool sectionOn;             // um trecho está tocando (e ainda não começou a sair)
        bool bedOn;                 // a cama está tocando (e ainda não começou a sair)
        float bedTime;              // onde a cama parou (volta dali, não do começo)
        string lastSection;
        float bedAt, nextVacuumAt, vacuumUntil, dreadSince, releaseUntil, ambienceTarget = 1f;
        bool appearancePlayed, dreadBuilt;
        float lastElapsed;

        static float Now => Time.unscaledTime; // o fim da partida congela o Time.time; a música não

        void Awake()
        {
            for (int i = 0; i < decks.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D: a trilha é de dentro da cabeça, não da casa
                source.volume = 0f;
                decks[i] = source;
            }
            var entry = track != null && SoundLibrary.Instance != null ? SoundLibrary.Instance.Get(track.sound) : null;
            if (entry != null)
            {
                clip = entry.RandomClip();
                baseVolume = entry.volume;
            }
            Restart();
        }

        void OnDestroy() => AmbienceGain = 1f;

        void Restart()
        {
            StopMusic(cut);
            appearancePlayed = false;
            bedTime = 0f;
            bedAt = Now + bedAfter;
            nextVacuumAt = Now + Random.Range(vacuumEvery.x, vacuumEvery.y);
            vacuumUntil = releaseUntil = 0f;
            Current = Moment.Calm;
            ambienceTarget = 1f;
        }

        void Update()
        {
            // A fase reiniciou (o relógio da noite voltou para trás): começa tudo de novo.
            float elapsed = NightElapsed(out _);
            if (elapsed < lastElapsed - 1f) Restart();
            lastElapsed = elapsed;

            var next = Evaluate();
            // Saindo do pânico ou da aparição: o corte e o respiro, antes de qualquer outra coisa.
            if ((Current == Moment.Panic || Current == Moment.Appearance) && next != Moment.Panic && next != Moment.Appearance)
            {
                releaseUntil = Now + releaseSilence;
                next = Moment.Release;
            }
            if (next != Current) Enter(next);
            Tick();
            UpdateFades();
        }

        // ---------- Que momento é este? ----------

        Moment Evaluate()
        {
            var game = GameManager.Instance;
            if (game != null && game.IsSpawned && game.Outcome != MatchOutcome.Playing) return Moment.Ended;
            if (Panicking()) return Moment.Panic;
            if (Current == Moment.Appearance && sectionOn) return Moment.Appearance; // a aparição vai até o fim do trecho
            if (!appearancePlayed && AnyGhostIn(GhostState.Performing)) return Moment.Appearance;
            if (Now < releaseUntil) return Moment.Release;
            if (MenaceNear()) return Moment.Dread;
            if (Now < vacuumUntil) return Moment.Vacuum;
            if (Current == Moment.Calm && Now >= nextVacuumAt)
            {
                // Hora do vácuo falso (nunca na corrida final: ali a cama não para).
                nextVacuumAt = Now + Random.Range(vacuumEvery.x, vacuumEvery.y);
                NightElapsed(out float progress);
                if (progress < finaleFrom)
                {
                    vacuumUntil = Now + Random.Range(vacuumLength.x, vacuumLength.y);
                    return Moment.Vacuum;
                }
            }
            return Moment.Calm;
        }

        // Alguém sendo puxado (em qualquer lugar: a equipe inteira sente), ou um caçador em cima de mim.
        bool Panicking()
        {
            foreach (var p in NetworkPlayer.All)
                if (p.Condition != null && p.Condition.IsPulled) return true;
            var me = NetworkPlayer.Local;
            foreach (var g in Ghost.All)
            {
                if (!g.IsSpawned) continue;
                if (g.State == GhostState.Pulling) return true;
                if (me != null && g.State == GhostState.Hunting && Vector3.Distance(g.transform.position, me.transform.position) < dreadDistance * 1.5f) return true;
            }
            return false;
        }

        // O falso disfarçado perto do MEU boneco, ou um fantasma investigando por perto.
        bool MenaceNear()
        {
            var me = NetworkPlayer.Local;
            if (me == null) return false;
            foreach (var g in Ghost.All)
            {
                if (!g.IsSpawned) continue;
                float d = Vector3.Distance(g.transform.position, me.transform.position);
                if (g.IsDisguised && d < dreadDistance) return true;
                if (g.State == GhostState.Investigating && d < dreadDistance * 1.5f) return true;
            }
            return false;
        }

        static bool AnyGhostIn(GhostState state)
        {
            foreach (var g in Ghost.All)
                if (g.IsSpawned && g.State == state) return true;
            return false;
        }

        static float NightElapsed(out float progress)
        {
            var game = GameManager.Instance;
            bool clock = game != null && game.IsSpawned && game.HasClock;
            progress = clock ? game.NightProgress : 0f;
            return clock ? game.NightDuration - game.TimeLeft : Time.timeSinceLevelLoad;
        }

        float BedLevel()
        {
            NightElapsed(out float progress);
            return progress >= finaleFrom ? bedFinaleVolume : bedVolume;
        }

        // ---------- Entrando num momento ----------

        void Enter(Moment next)
        {
            var previous = Current;
            Current = next;
            switch (next)
            {
                case Moment.Calm:
                    ambienceTarget = 1f;
                    // Voltando do vácuo ou do crescendo: a cama sobe de novo (ou volta a tocar).
                    if (previous == Moment.Vacuum && bedOn) SetBedLevel(BedLevel(), fadeIn);
                    else if (sectionOn) StopMusic(fadeOut);
                    break;

                case Moment.Vacuum: // a cama afunda sem motivo
                    ambienceTarget = vacuumAmbience;
                    if (bedOn) SetBedLevel(BedLevel() * vacuumBed, fadeOut * 2f);
                    break;

                case Moment.Dread: // o vácuo de verdade: o falso está perto
                    StopMusic(fadeOut);
                    ambienceTarget = dreadAmbience;
                    dreadSince = Now;
                    dreadBuilt = false;
                    break;

                case Moment.Panic:
                    ambienceTarget = panicAmbience;
                    if (!sectionOn || section.mood != Soundtrack.Mood.Panic) PlaySection(Soundtrack.Mood.Panic, 0.3f, panicVolume);
                    break;

                case Moment.Appearance:
                    appearancePlayed = true;
                    ambienceTarget = appearanceAmbience;
                    PlaySection(Soundtrack.Mood.Appearance, 0.05f, appearanceVolume);
                    break;

                case Moment.Release: // corte seco: o silêncio depois do susto
                    StopMusic(cut);
                    ambienceTarget = 1f; // volta, mas devagar (ambienceReturn)
                    bedAt = releaseUntil;
                    nextVacuumAt = releaseUntil + Random.Range(vacuumEvery.x, vacuumEvery.y); // nada de vácuo logo depois do susto
                    break;

                case Moment.Ended:
                    StopMusic(fadeOut);
                    ambienceTarget = 1f;
                    break;
            }
            if (next == Moment.Calm && !bedOn) bedAt = Mathf.Max(bedAt, Now + 2f); // um respiro antes da cama voltar
        }

        // ---------- Enquanto dura ----------

        void Tick()
        {
            // Um trecho de susto chegou perto do fim: sai com fade (o próprio trecho termina num vale de volume).
            if (sectionOn && decks[deck].isPlaying && decks[deck].time >= section.end - fadeOut)
                StopMusic(fadeOut);

            switch (Current)
            {
                case Moment.Calm:
                    if (!bedOn && !sectionOn && Now >= bedAt) PlayBed();
                    else if (bedOn) SetBedLevel(BedLevel(), fadeIn); // a corrida final sobe a cama devagar
                    break;
                case Moment.Dread:
                    // Silêncio primeiro; se o perigo continua, um crescendo baixinho por baixo.
                    if (!dreadBuilt && Now - dreadSince >= dreadBuildAfter)
                    {
                        dreadBuilt = true;
                        PlaySection(Soundtrack.Mood.Build, fadeIn, buildVolume);
                    }
                    break;
                case Moment.Panic:
                    if (!sectionOn) PlaySection(Soundtrack.Mood.Panic, 0.3f, panicVolume); // o pânico durou mais que o trecho
                    break;
            }
        }

        // ---------- Os dois toca-discos ----------

        // A cama: a faixa inteira em loop, a partir de onde parou da última vez.
        void PlayBed()
        {
            if (clip == null) return;
            var source = SwapDeck();
            source.loop = true;
            source.time = Mathf.Clamp(bedTime, 0f, clip.length - 0.1f);
            source.Play();
            bedOn = true;
            SetBedLevel(BedLevel(), fadeIn);
        }

        void SetBedLevel(float volume, float fade)
        {
            float target = baseVolume * volume;
            if (Mathf.Approximately(deckTarget[deck], target)) return;
            float from = decks[deck].volume;
            deckTarget[deck] = target;
            deckSpeed[deck] = Mathf.Max(0.001f, Mathf.Abs(target - from)) / Mathf.Max(0.01f, fade);
        }

        // Um trecho de susto (pânico, aparição, crescendo): troca com o que estiver tocando.
        void PlaySection(Soundtrack.Mood mood, float fade, float volume)
        {
            if (clip == null || track == null || !track.TryPick(mood, lastSection, out var picked)) return;
            var source = SwapDeck();
            source.loop = false;
            source.time = Mathf.Clamp(picked.start, 0f, clip.length - 0.1f);
            source.Play();
            deckTarget[deck] = baseVolume * volume;
            deckSpeed[deck] = deckTarget[deck] / Mathf.Max(0.01f, fade);
            section = picked;
            lastSection = picked.name;
            sectionOn = true;
        }

        // O toca-disco livre, pronto para tocar; o outro sai com fade (crossfade).
        AudioSource SwapDeck()
        {
            RememberBed();
            FadeDeck(deck, fadeOut);
            deck = 1 - deck;
            var source = decks[deck];
            source.clip = clip;
            source.volume = 0f;
            bedOn = sectionOn = false;
            return source;
        }

        void StopMusic(float fade)
        {
            RememberBed();
            bedOn = sectionOn = false;
            for (int i = 0; i < decks.Length; i++) FadeDeck(i, fade);
        }

        // A cama vai parar: guarda onde estava, para voltar dali (e não repetir o começo da faixa).
        void RememberBed()
        {
            if (bedOn && decks[deck].isPlaying) bedTime = decks[deck].time;
        }

        void FadeDeck(int i, float fade)
        {
            if (decks[i] == null) return;
            deckTarget[i] = 0f;
            deckSpeed[i] = Mathf.Max(0.01f, decks[i].volume) / Mathf.Max(0.01f, fade);
        }

        void UpdateFades()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < decks.Length; i++)
            {
                var s = decks[i];
                s.volume = Mathf.MoveTowards(s.volume, deckTarget[i], deckSpeed[i] * dt);
                if (deckTarget[i] <= 0f && s.volume <= 0f && s.isPlaying) s.Stop();
            }
            // A casa some rápido e volta devagar (como o ouvido "procurando" o barulho de novo).
            float speed = ambienceTarget < AmbienceGain ? 1f / ambienceDrop : 1f / ambienceReturn;
            AmbienceGain = Mathf.MoveTowards(AmbienceGain, ambienceTarget, speed * dt);
        }
    }
}
