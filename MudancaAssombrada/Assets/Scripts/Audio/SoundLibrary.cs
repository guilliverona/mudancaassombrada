using System;
using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // As CAMADAS da mixagem, do fundo para a frente. Cada som pertence a uma, e é a camada que
    // decide quem abaixa para quem (ver GameAudio):
    //   Bed       cama: o fundo contínuo (o clima da casa, o motor do caminhão)
    //   Rhythm    ritmo: o relógio, o "metrônomo" da casa
    //   Cue       pista: o que dá informação (passos, respiração, bengala, batidas)
    //   Threat    ameaça: o fantasma agindo. Enquanto toca, cama e ritmo abaixam
    //   Music     música (a Maestrina)
    //   Interface botões e avisos da tela
    public enum SoundLayer { Bed, Rhythm, Cue, Threat, Music, Interface }

    // A BIBLIOTECA de sons: liga cada nome do catálogo (tools/audio/catalogo.json) aos arquivos
    // de Assets/Audio, com volume e alcance. Também diz qual som cada SoundType faz "por padrão".
    //
    // Fica em Assets/Resources/Sons.asset, carregada por nome (como o Elenco e a Campanha).
    // Quem monta é o menu Mudança Assombrada > Atualizar Biblioteca de Sons (SoundLibraryBuilder):
    // ele acha os arquivos sozinho. Volume, alcance e os padrões são ajustados aqui, no Inspector.
    [CreateAssetMenu(menuName = "Mudança Assombrada/Biblioteca de Sons", fileName = "Sons")]
    public class SoundLibrary : ScriptableObject
    {
        public const string ResourceName = "Sons";

        // Sons da interface (o UiSound e quem mais precisar usam estes nomes).
        public const string UiClick = "ui_clique";
        public const string UiHover = "ui_hover";
        public const string UiError = "ui_erro";

        [Serializable]
        public class Entry
        {
            public string id;                                 // o nome no catálogo (= nome do arquivo)
            public AudioClip[] clips = Array.Empty<AudioClip>(); // as variações (sorteia uma a cada vez)
            [Range(0, 1)] public float volume = 1f;
            [Tooltip("Sorteia um tom um pouco diferente a cada vez (0.05 = até 5% mais grave ou agudo).")]
            [Range(0, 0.5f)] public float pitchVariation = 0.05f;
            [Tooltip("A partir de quantos metros não se ouve mais. É só o ÁUDIO: quem decide se o fantasma " +
                     "ouviu é o alcance do SoundEvent (a regra do jogo).")]
            public float maxDistance = 20f;
            public SoundLayer layer = SoundLayer.Cue;
            [Tooltip("Segundos: o mesmo som não toca de novo antes disso (evita a 'metralhadora' de batidas).")]
            public float minGap;
            [Tooltip("Espera o contratempo do relógio (HouseClock) para tocar. Só para sons que se repetem " +
                     "sozinhos e podem atrasar meio segundo (respiração, sussurro).")]
            public bool onBeat;

            // Sorteia uma das variações.
            public AudioClip RandomClip() => clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        [Tooltip("O som de cada tipo quando a fonte não tem um SoundVoice dizendo outro.")]
        public SoundChoice[] defaults = Array.Empty<SoundChoice>();
        public Entry[] sounds = Array.Empty<Entry>();

        static SoundLibrary instance;
        Dictionary<string, Entry> byId; // montado na primeira busca (procurar num dicionário é instantâneo)

        // Sem a biblioteca o jogo continua funcionando, só que mudo (por isso não é erro).
        public static SoundLibrary Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<SoundLibrary>(ResourceName);
                return instance;
            }
        }

        // O som com este nome, ou null se não existe ou ainda não tem arquivo.
        public Entry Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId == null)
            {
                byId = new Dictionary<string, Entry>();
                foreach (var e in sounds) byId[e.id] = e;
            }
            return byId.TryGetValue(id, out var entry) && entry.clips.Length > 0 ? entry : null;
        }

        public bool TryGetDefault(SoundType type, out SoundChoice choice) => SoundChoice.TryFind(defaults, type, out choice);

        // O Inspector mudou a lista: refaz o dicionário na próxima busca.
        void OnValidate() => byId = null;
    }
}
