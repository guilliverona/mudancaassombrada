using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Quem TOCA os sons. Até aqui todo som do jogo era só um EVENTO (SoundEvent): virava onda no
    // eco da Cida e chegava ao ouvido dos fantasmas, mas ninguém ouvia de verdade.
    //
    // Este script se inscreve no EchoSystem.SoundEmitted e, para cada som, toca um arquivo da
    // SoundLibrary naquele ponto do mundo (som 3D). Como o SoundRelay já entrega os sons dos
    // outros jogadores em cada computador, cada um ouve todo mundo sem nenhuma rede a mais.
    //
    // Nasce sozinho ao abrir o jogo e vive entre as cenas (como um serviço): ninguém precisa
    // colocar nada na cena. O Tiago (surdo) não ouve nada porque o GameSettings zera o AudioListener.
    //
    // A MIXAGEM (para os sons não se atropelarem), com os números de cada som no Sons.asset:
    //   - intervalo mínimo: o mesmo som não repete antes de 'minGap' segundos;
    //   - no tempo: sons marcados com 'onBeat' esperam o contratempo do relógio (HouseClock);
    //   - abaixar a cama: enquanto toca um som de AMEAÇA, a cama e o ritmo descem (BedGain),
    //     como o "ducking" de um rádio quando o locutor fala;
    //   - a própria respiração soa mais baixa: ela é pista para os OUTROS, não para quem respira.
    //
    // Para tocar um som que não é evento de jogo:  GameAudio.Play("livro_caindo", posição)
    // e na interface (sem posição):                GameAudio.PlayUi(SoundLibrary.UiClick)
    public class GameAudio : MonoBehaviour
    {
        const int Voices = 24;               // quantos sons ao mesmo tempo, no máximo
        const float DuckedGain = 0.45f;      // cama e ritmo durante uma ameaça (uns -7 dB)
        const float DuckSpeed = 3f;          // quão rápido abaixa e volta (por segundo)
        const float OwnBreathVolume = 0.5f;  // a minha respiração, no meu ouvido

        static GameAudio instance;

        // O volume atual da cama e do ritmo (1 = normal). AmbientSound e HouseClock multiplicam por ele.
        public static float BedGain { get; private set; } = 1f;

        // Um som esperando o contratempo.
        struct Pending
        {
            public SoundLibrary.Entry entry;
            public Vector3 position;
            public float volume, time;
        }

        SoundLibrary library;
        readonly List<AudioSource> pool = new();
        int next; // fila circular, como as ondas do EchoSystem
        AudioSource ui;
        readonly Dictionary<string, float> lastPlayed = new(); // id -> Time.time da última vez
        readonly List<Pending> pending = new();
        float duckUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            BedGain = 1f;
            var library = SoundLibrary.Instance;
            if (library == null) return; // sem biblioteca (ainda não foi criada): o jogo segue mudo

            var go = new GameObject("Áudio do Jogo");
            DontDestroyOnLoad(go); // não some ao trocar de cena
            instance = go.AddComponent<GameAudio>();
            instance.library = library;
        }

        void Awake()
        {
            // Um "grupo" de caixas de som reaproveitadas. Criar e destruir uma a cada passo
            // geraria lixo na memória e engasgos.
            for (int i = 0; i < Voices; i++)
            {
                var source = new GameObject("Voz " + i).AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false;
                source.spatialBlend = 1f;                       // 1 = 3D: vem de um ponto do mundo
                source.rolloffMode = AudioRolloffMode.Linear;   // some de vez no maxDistance (previsível)
                source.minDistance = 1f;
                source.dopplerLevel = 0f;
                pool.Add(source);
            }

            ui = gameObject.AddComponent<AudioSource>();
            ui.playOnAwake = false;
            ui.spatialBlend = 0f; // 2D: igual nos dois ouvidos
        }

        void OnEnable() => EchoSystem.SoundEmitted += OnSound;
        void OnDisable() => EchoSystem.SoundEmitted -= OnSound;

        void Update()
        {
            // A cama desce enquanto há ameaça e sobe devagar depois (sem "degrau" no volume).
            float target = Time.time < duckUntil ? DuckedGain : 1f;
            BedGain = Mathf.MoveTowards(BedGain, target, DuckSpeed * Time.deltaTime);

            // Toca os sons que estavam esperando o contratempo.
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].time) continue;
                var p = pending[i];
                pending.RemoveAt(i);
                Voice(p.entry, p.position, p.volume);
            }
        }

        void OnSound(SoundEvent sound)
        {
            string id = SoundVoice.Resolve(sound, library);
            if (id == null) return;
            // A intensidade (0 a 1) deixa o mesmo som mais baixo ou mais alto: passo agachado x correndo.
            float volume = Mathf.Lerp(0.35f, 1f, sound.Intensity);
            if (sound.Is(SoundType.Breath) && IsLocalPlayer(sound.Source)) volume *= OwnBreathVolume;
            PlayAt(id, sound.Position, volume);
        }

        // O boneco DESTE computador? (Sem rede, testando sozinho, o único jogador conta como meu.)
        static bool IsLocalPlayer(GameObject source)
        {
            if (source == null || !source.TryGetComponent(out NetworkPlayer _)) return false;
            return !source.TryGetComponent(out NetworkObject net) || !net.IsSpawned || net.IsOwner;
        }

        public static void Play(string id, Vector3 position, float volume = 1f)
        {
            if (instance != null) instance.PlayAt(id, position, volume);
        }

        public static void PlayUi(string id)
        {
            if (instance == null) return;
            var entry = instance.library.Get(id);
            if (entry == null || !instance.Allow(entry)) return;
            instance.ui.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            instance.ui.PlayOneShot(entry.RandomClip(), entry.volume);
        }

        // Abaixa a cama por mais 'seconds' segundos (ex.: o GhostSounds enquanto o fantasma puxa alguém).
        public static void Duck(float seconds)
        {
            if (instance != null) instance.duckUntil = Mathf.Max(instance.duckUntil, Time.time + seconds);
        }

        void PlayAt(string id, Vector3 position, float volume)
        {
            var entry = library.Get(id);
            if (entry == null || !Allow(entry)) return; // sem arquivo ou cedo demais: silêncio, sem erro

            float wait = entry.onBeat ? HouseClock.DelayToOffBeat() : 0f;
            if (wait > 0.01f) pending.Add(new Pending { entry = entry, position = position, volume = volume, time = Time.time + wait });
            else Voice(entry, position, volume);
        }

        // Intervalo mínimo: marca a hora e diz se pode tocar.
        bool Allow(SoundLibrary.Entry entry)
        {
            if (entry.minGap > 0 && lastPlayed.TryGetValue(entry.id, out float last) && Time.time - last < entry.minGap)
                return false;
            lastPlayed[entry.id] = Time.time;
            return true;
        }

        void Voice(SoundLibrary.Entry entry, Vector3 position, float volume)
        {
            // Pega a próxima caixa livre; se todas estão tocando, a mais antiga é cortada.
            AudioSource source = pool[next];
            for (int i = 0; i < pool.Count; i++)
            {
                var candidate = pool[(next + i) % pool.Count];
                if (!candidate.isPlaying) { source = candidate; next = (next + i) % pool.Count; break; }
            }
            next = (next + 1) % pool.Count;

            source.transform.position = position;
            source.clip = entry.RandomClip();
            source.volume = entry.volume * volume;
            source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            source.maxDistance = entry.maxDistance;
            source.Play();

            if (entry.layer == SoundLayer.Threat) Duck(source.clip.length);
        }
    }
}
