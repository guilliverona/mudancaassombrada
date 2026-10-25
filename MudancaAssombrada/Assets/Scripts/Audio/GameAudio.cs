using System.Collections.Generic;
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
    // Para tocar um som que não é evento de jogo:  GameAudio.Play("livro_caindo", posição)
    // e na interface (sem posição):                GameAudio.PlayUi(SoundLibrary.UiClick)
    public class GameAudio : MonoBehaviour
    {
        const int Voices = 24; // quantos sons ao mesmo tempo, no máximo

        static GameAudio instance;

        SoundLibrary library;
        readonly List<AudioSource> pool = new();
        int next; // fila circular, como as ondas do EchoSystem
        AudioSource ui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
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

        void OnSound(SoundEvent sound)
        {
            string id = SoundVoice.Resolve(sound, library);
            // A intensidade (0 a 1) deixa o mesmo som mais baixo ou mais alto: passo agachado x correndo.
            if (id != null) PlayAt(id, sound.Position, Mathf.Lerp(0.35f, 1f, sound.Intensity));
        }

        public static void Play(string id, Vector3 position, float volume = 1f)
        {
            if (instance != null) instance.PlayAt(id, position, volume);
        }

        public static void PlayUi(string id)
        {
            if (instance == null) return;
            var entry = instance.library.Get(id);
            if (entry == null) return;
            instance.ui.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            instance.ui.PlayOneShot(RandomClip(entry), entry.volume);
        }

        void PlayAt(string id, Vector3 position, float volume)
        {
            var entry = library.Get(id);
            if (entry == null) return; // som ainda sem arquivo: silêncio, sem erro

            // Pega a próxima caixa livre; se todas estão tocando, a mais antiga é cortada.
            AudioSource source = pool[next];
            for (int i = 0; i < pool.Count; i++)
            {
                var candidate = pool[(next + i) % pool.Count];
                if (!candidate.isPlaying) { source = candidate; next = (next + i) % pool.Count; break; }
            }
            next = (next + 1) % pool.Count;

            source.transform.position = position;
            source.clip = RandomClip(entry);
            source.volume = entry.volume * volume;
            source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            source.maxDistance = entry.maxDistance;
            source.Play();
        }

        static AudioClip RandomClip(SoundLibrary.Entry entry) => entry.clips[Random.Range(0, entry.clips.Length)];
    }
}
