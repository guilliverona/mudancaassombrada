using UnityEngine;

namespace MudancaAssombrada
{
    // O RELÓGIO da casa: toca "tique... taque..." em tempo exato e serve de METRÔNOMO para os sons.
    //
    // Por que um metrônomo: sons que se repetem sozinhos (a respiração de cada um, os sussurros do
    // Imitador) começavam em qualquer instante e caíam em cima do tique, um mascarando o outro.
    // Agora eles esperam o CONTRATEMPO (o meio entre duas batidas): o relógio bate no tempo, a
    // respiração entra no espaço entre as batidas, como instrumentos numa música.
    // Sons que precisam ser instantâneos (passos, batidas, voz, o fantasma atacando) NÃO esperam.
    //
    // Fica no prefab do relógio de pêndulo (o som sai dele, em 3D). Sem relógio na cena, nada espera.
    public class HouseClock : MonoBehaviour
    {
        [Tooltip("Segundos entre uma batida e a outra (tique, depois taque).")]
        [SerializeField] float period = 1f;
        [SerializeField] string tick = "relogio_tique";
        [SerializeField] string tock = "relogio_taque";

        public static HouseClock Instance { get; private set; }

        AudioSource source;
        float nextBeat;
        bool tockNext;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.dopplerLevel = 0f;
            nextBeat = Time.time + period;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Time.time < nextBeat) return;
            nextBeat += period; // soma (e não "agora + período"): o tempo não escorrega com o frame
            if (Time.time > nextBeat) nextBeat = Time.time + period; // jogo travou: retoma sem disparar várias

            var library = SoundLibrary.Instance;
            var entry = library != null ? library.Get(tockNext ? tock : tick) : null;
            tockNext = !tockNext;
            if (entry == null) return;
            source.maxDistance = entry.maxDistance;
            // A cama e o ritmo abaixam enquanto o fantasma age (GameAudio.BedGain).
            source.PlayOneShot(entry.RandomClip(), entry.volume * GameAudio.BedGain);
        }

        // Quantos segundos faltam para o próximo contratempo (o meio entre duas batidas).
        // Sem relógio na cena: 0 (toca na hora).
        public static float DelayToOffBeat()
        {
            if (Instance == null) return 0f;
            return OffBeatDelay(Instance.nextBeat, Instance.period, Time.time);
        }

        // A conta em si, sem relógio na cena (os testes usam esta).
        public static float OffBeatDelay(float nextBeat, float period, float now)
        {
            float offBeat = nextBeat - period / 2f;   // o meio antes da próxima batida
            if (offBeat < now) offBeat += period;     // já passou: o seguinte
            return offBeat - now;
        }
    }
}
