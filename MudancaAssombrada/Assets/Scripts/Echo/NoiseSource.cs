using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // Uma FONTE DE RUÍDO da casa (LEVEL_DESIGN_FASE1.md, MEC-04): o gramofone travado, o piano que
    // começa a tocar sozinho, a arandela que assobia. Barulho constante BORRA o eco da Cida: perto
    // de uma fonte ligada, a bengala enxerga menos (12 m → 4 m) e as ondas ficam grossas
    // (o EchoSystem calcula o NoiseLevel). E numa casa barulhenta, a falta de respiração do
    // Imitador some no meio do resto.
    //
    // Só a Cida desliga ("Afinar", um Interactable com onlyFor = Cida no mesmo objeto): quando
    // ele é usado, a fonte cala para todos. Reiniciar a fase reacende (o Interactable volta).
    //
    // O som que ela faz: um loop (gramofone, arandela) ou um som que se repete (o acorde do piano).
    // Para o Tiago e para o eco, ela também "pulsa" um SoundEvent de música de tempos em tempos.
    public class NoiseSource : MonoBehaviour
    {
        static readonly List<NoiseSource> all = new();
        public static IReadOnlyList<NoiseSource> All => all;

        [Tooltip("Até onde o ruído atrapalha o eco (metros).")]
        [SerializeField] float radius = 10f;
        [Header("Som")]
        [SerializeField] string loopSound = "";
        [Tooltip("Em vez de loop: um som que se repete a cada 'repeatEvery' segundos.")]
        [SerializeField] string repeatSound = "";
        [SerializeField] float repeatEvery = 6f;
        [SerializeField] string silenceSound = "";
        [Tooltip("Ao calar, apaga também as luzes deste objeto (a arandela do corredor: afinar = escuro).")]
        [SerializeField] bool lightsOffWhenSilenced;
        [Tooltip("Ao calar, liga este outro som no lugar (a cortina de veludo depois do gramofone).")]
        [SerializeField] AmbientSound replaceWith;
        [Header("Começo")]
        [Tooltip("Desligado: só começa quando um móvel carregado chega perto (o piano que toca sozinho).")]
        [SerializeField] bool startsOn = true;
        [SerializeField] float wakeDistance = 4f;

        Interactable tune;       // o "Afinar" da Cida
        AudioSource loop;
        GameObject pulse;        // de onde sai o pulso no eco: tem uma "voz" MUDA (o pulso não é som de verdade)
        bool awake, silenced;
        float nextRepeat, nextPulse;

        // Ligada agora? (acordada e ainda não afinada)
        public bool IsActive => awake && !silenced;
        public float Radius => radius;

        void Awake()
        {
            tune = GetComponent<Interactable>();
            awake = startsOn;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            if (!string.IsNullOrEmpty(loopSound))
            {
                var entry = SoundLibrary.Instance != null ? SoundLibrary.Instance.Get(loopSound) : null;
                if (entry != null)
                {
                    loop = gameObject.AddComponent<AudioSource>();
                    loop.clip = entry.RandomClip();
                    loop.loop = true;
                    loop.volume = entry.volume;
                    loop.spatialBlend = 1f;
                    loop.rolloffMode = AudioRolloffMode.Linear;
                    loop.maxDistance = entry.maxDistance;
                    loop.dopplerLevel = 0f;
                }
            }
            if (replaceWith != null) replaceWith.SetPlaying(false);

            // O pulso de música é só para o eco e a vibração: sem esta voz muda, cada pulso tocava a
            // caixinha de música (o som padrão de Music) a cada 1,5 s, uma por cima da outra.
            pulse = new GameObject("Pulso no eco (mudo)");
            pulse.transform.SetParent(transform, false);
            pulse.transform.localPosition = Vector3.up;
            pulse.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Music, sound = "" } };
            Refresh();
        }

        void Update()
        {
            // Afinado (ou a fase reiniciou e o "Afinar" voltou): segue o estado do Interactable, em todos.
            bool shouldBeSilenced = tune != null && tune.IsUsed;
            if (shouldBeSilenced != silenced)
            {
                silenced = shouldBeSilenced;
                if (silenced) OnSilenced();
                else if (!startsOn) awake = false; // reinício: o piano volta a esperar
                Refresh();
            }

            if (!awake && !silenced && SomeFurnitureCarriedNear())
            {
                awake = true;
                Refresh();
                nextRepeat = 0; // o primeiro acorde, na hora
            }

            if (!IsActive) return;
            if (!string.IsNullOrEmpty(repeatSound) && Time.time >= nextRepeat)
            {
                nextRepeat = Time.time + repeatEvery * Random.Range(0.8f, 1.2f);
                GameAudio.Play(repeatSound, transform.position + Vector3.up);
            }
            // Um "pulso" de música no eco e na vibração (só local: cada computador simula o seu).
            if (Time.time >= nextPulse)
            {
                nextPulse = Time.time + 1.5f;
                EchoSystem.EmitLocal(new SoundEvent(transform.position + Vector3.up, radius * 0.6f, 0.35f, SoundType.Music, pulse));
            }
        }

        bool SomeFurnitureCarriedNear()
        {
            if (startsOn) return false;
            foreach (var f in Furniture.All)
                if (f.IsHeld && Vector3.Distance(f.transform.position, transform.position) < wakeDistance) return true;
            return false;
        }

        void Refresh()
        {
            if (loop != null)
            {
                if (IsActive && !loop.isPlaying) loop.Play();
                else if (!IsActive) loop.Stop();
            }
            if (lightsOffWhenSilenced)
                foreach (var l in GetComponentsInChildren<Light>(true)) l.enabled = !silenced;
            if (replaceWith != null) replaceWith.SetPlaying(silenced);
        }

        void OnSilenced()
        {
            if (!string.IsNullOrEmpty(silenceSound)) GameAudio.Play(silenceSound, transform.position + Vector3.up);
            GameAudio.PlayUi("casa_fica_quieta");
            GameCues.EmitLocal(new GameCue(transform.position, 0f, Senses.Hearing | Senses.Sight,
                $"A casa ficou mais quieta ({name}).", gameObject));
        }

        // O quanto o ruído atrapalha num ponto: 0 = nada, 1 = colado numa fonte ligada.
        public static float LevelAt(Vector3 position)
        {
            float level = 0f;
            foreach (var n in all)
            {
                if (!n.IsActive) continue;
                float d = Vector3.Distance(position, n.transform.position);
                level = Mathf.Max(level, 1f - d / n.radius);
            }
            return Mathf.Clamp01(level);
        }
    }
}
