using UnityEngine;

namespace MudancaAssombrada
{
    // A PIANOLA da Sala de Música (LEVEL_DESIGN_FASE1_MANSAO.md §7): o piano de rolo de onde a trilha
    // "vem". Toca a MESMA faixa da cama do SoundDirector, no mesmo ponto (R18), mas em 3D: perto
    // dela (~10 m) a trilha deixa de ser "música" e vira o piano da casa, no espaço. Para isso a cama
    // 2D abaixa perto da pianola (BedDuck, que o SoundDirector aplica).
    //
    //  - Quando a cama para (pânico, aparição, vácuo de verdade), a pianola para junto.
    //  - Na Câmara Secreta tudo para: o susto é a falta do piano.
    //  - Afinar (a Cida) tira só o acorde desafinado, que é o NoiseSource do mesmo objeto (D14):
    //    a melodia é este componente, e continua.
    //  - Tirar a caixa de rolos da sala não para a pianola (ela não depende dos rolos).
    //
    // Cada computador toca a sua (a cama também é local): não há nada em rede aqui.
    public class Pianola : MonoBehaviour
    {
        // Quanto sobra da cama 2D agora (1 = normal). Lido pelo SoundDirector.
        public static float BedDuck { get; private set; } = 1f;

        [Tooltip("Até aqui (metros) a pianola soa no máximo e a cama 2D abaixa ao máximo.")]
        [SerializeField] float near = 3f;
        [Tooltip("A partir daqui não se ouve mais a pianola (e a cama volta ao normal).")]
        [SerializeField] float far = 12f;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [Header("Silêncio (a Câmara Secreta)")]
        [SerializeField] Vector3 silentCenter;
        [SerializeField] Vector3 silentSize;

        AudioSource source;
        float nextSyncCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => BedDuck = 1f;

        public void Configure(Vector3 center, Vector3 size)
        {
            silentCenter = center;
            silentSize = size;
        }

        void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = near;
            source.maxDistance = far;
            source.dopplerLevel = 0f;
            source.volume = 0f;
        }

        void OnDestroy() => BedDuck = 1f;

        void Update()
        {
            Vector3 ear = ListenerPosition();
            bool silent = InSilentZone(ear);
            float closeness = PianolaRules.Closeness(Vector3.Distance(ear, transform.position), near, far);
            BedDuck = Mathf.MoveTowards(BedDuck, PianolaRules.BedDuck(closeness, silent), Time.unscaledDeltaTime * 0.8f);

            var director = SoundDirector.Instance;
            bool play = director != null && director.Clip != null && director.BedPlaying && !silent;
            if (play)
            {
                if (source.clip != director.Clip) source.clip = director.Clip;
                if (!source.isPlaying)
                {
                    source.time = director.BedTime;
                    source.Play();
                }
                else if (Time.unscaledTime >= nextSyncCheck)
                {
                    // As duas tocam no mesmo ritmo; só confere de vez em quando se alguém escorregou.
                    nextSyncCheck = Time.unscaledTime + 1f;
                    if (PianolaRules.NeedsResync(source.time, director.BedTime, source.clip.length)) source.time = director.BedTime;
                }
            }
            source.volume = Mathf.MoveTowards(source.volume, play ? volume : 0f, Time.unscaledDeltaTime / 1.5f);
            if (!play && source.volume <= 0f && source.isPlaying) source.Stop();
        }

        static Vector3 ListenerPosition()
        {
            var me = NetworkPlayer.Local;
            if (me != null) return me.transform.position + Vector3.up * 1.6f;
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.one * 9999f;
        }

        bool InSilentZone(Vector3 p)
        {
            if (silentSize == Vector3.zero) return false;
            Vector3 d = p - silentCenter, half = silentSize / 2;
            return Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y && Mathf.Abs(d.z) <= half.z;
        }
    }
}
