using UnityEngine;

namespace MudancaAssombrada
{
    // Um som que fica tocando sem parar (loop): o clima da casa, o relógio da sala, a caixinha
    // de música, o motor do caminhão. Coloque num objeto da cena e escolha o nome do som.
    //
    // Não é um SoundEvent: o fantasma não ouve e não aparece no eco. Para algo que o fantasma
    // deve ouvir (a música da Maestrina), quem toca também emite o evento pelo EchoSystem.
    public class AmbientSound : MonoBehaviour
    {
        [Tooltip("Nome do som na biblioteca (Assets/Resources/Sons), ex.: casa_noite, relogio_pendulo.")]
        [SerializeField] string sound = "casa_noite";
        [Tooltip("Ligado: o som vem deste ponto do mundo (o relógio). Desligado: toca igual em todo lugar (o clima, a música de fundo).")]
        [SerializeField] bool spatial;
        [Range(0, 1)] [SerializeField] float volume = 0.5f;
        [SerializeField] bool playOnStart = true;

        AudioSource source;

        void Start()
        {
            var library = SoundLibrary.Instance;
            var entry = library != null ? library.Get(sound) : null;
            if (entry == null) return; // som ainda sem arquivo: fica em silêncio

            source = gameObject.AddComponent<AudioSource>();
            source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            source.loop = true;
            source.volume = entry.volume * volume;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = entry.maxDistance;
            source.dopplerLevel = 0f;
            if (playOnStart) source.Play();
        }

        // Ligar e desligar por código (ex.: a caixinha de música que o Tiago desliga).
        public void SetPlaying(bool on)
        {
            if (source == null) return;
            if (on && !source.isPlaying) source.Play();
            else if (!on) source.Stop();
        }
    }
}
