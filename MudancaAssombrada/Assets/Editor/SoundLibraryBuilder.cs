using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Monta a BIBLIOTECA de sons (Assets/Resources/Sons.asset) a partir do catálogo
    // (tools/audio/catalogo.json) e dos arquivos que o tools/audio/gerar_sons.py salvou em Assets/Audio.
    //
    // Pode rodar quantas vezes quiser: só ACRESCENTA sons novos e atualiza a lista de arquivos.
    // Volume, alcance e os padrões por tipo ajustados no Inspector continuam.
    // Também dá a "voz" (SoundVoice) aos fantasmas que ainda não têm.
    public static class SoundLibraryBuilder
    {
        const string LibraryPath = "Assets/Resources/Sons.asset";
        const string AudioFolder = "Assets/Audio";

        // O catálogo fica fora do projeto Unity, na pasta tools da raiz do repositório.
        static string CatalogPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "audio", "catalogo.json"));

        // Só os campos do catálogo que interessam aqui (o JsonUtility ignora os outros).
        [Serializable] class Catalog { public CatalogSound[] sons; public Mix[] mixagem = Array.Empty<Mix>(); }
        [Serializable] class CatalogSound { public string id; public string pasta; }
        // Campo ausente no JSON fica com o valor daqui (-1 = "não mexer no que está no Inspector").
        [Serializable] class Mix { public string id; public string camada; public float volume = -1, alcance = -1, intervaloMin = -1; public bool noTempo; }

        static readonly Dictionary<string, SoundLayer> Layers = new()
        {
            ["cama"] = SoundLayer.Bed, ["ritmo"] = SoundLayer.Rhythm, ["pista"] = SoundLayer.Cue,
            ["ameaca"] = SoundLayer.Threat, ["musica"] = SoundLayer.Music, ["interface"] = SoundLayer.Interface,
        };

        [MenuItem("Mudança Assombrada/Atualizar Biblioteca de Sons", priority = 23)]
        public static void EnsureLibrary()
        {
            if (!File.Exists(CatalogPath))
            {
                Debug.LogWarning($"Catálogo de sons não encontrado: {CatalogPath}");
                return;
            }
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(CatalogPath));

            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                library.defaults = DefaultChoices();
                EditorTools.EnsureFolder("Assets/Resources");
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var existing = library.sounds.Where(e => e != null && !string.IsNullOrEmpty(e.id))
                                         .GroupBy(e => e.id).ToDictionary(g => g.Key, g => g.First());
            var mixes = catalog.mixagem.ToDictionary(m => m.id);
            var entries = new List<SoundLibrary.Entry>();
            var missing = new List<string>();
            foreach (var s in catalog.sons)
            {
                if (!existing.TryGetValue(s.id, out var entry))
                    entry = new SoundLibrary.Entry { id = s.id, volume = DefaultVolume(s), maxDistance = DefaultDistance(s.pasta) };
                if (mixes.TryGetValue(s.id, out var mix)) ApplyMix(entry, mix);
                entry.clips = FindClips(s);
                if (entry.clips.Length == 0) missing.Add(s.id);
                entries.Add(entry);
            }
            library.sounds = entries.ToArray();
            EditorUtility.SetDirty(library);

            GiveVoice(LevelPrefabsBuilder.GhostPath, new[]
            {
                new SoundChoice { type = SoundType.Footstep, sound = "passo_fantasma" },
                new SoundChoice { type = SoundType.Breath, sound = "bibliotecario_respiracao" },
            });
            GiveImitatorSounds();
            AssetDatabase.SaveAssets();

            Debug.Log($"Biblioteca de sons: {entries.Count - missing.Count} de {entries.Count} sons com arquivo." +
                      (missing.Count > 0 ? " Sem arquivo (rode tools/audio/gerar_sons.py): " + string.Join(", ", missing) : ""));
        }

        // A mixagem do catálogo vence o Inspector (é a "medida" versionada, como no Art Bible).
        static void ApplyMix(SoundLibrary.Entry entry, Mix mix)
        {
            if (!string.IsNullOrEmpty(mix.camada))
            {
                if (Layers.TryGetValue(mix.camada, out var layer)) entry.layer = layer;
                else Debug.LogWarning($"Som {mix.id}: camada desconhecida \"{mix.camada}\" (use: {string.Join(", ", Layers.Keys)}).");
            }
            if (mix.volume >= 0) entry.volume = mix.volume;
            if (mix.alcance >= 0) entry.maxDistance = mix.alcance;
            if (mix.intervaloMin >= 0) entry.minGap = mix.intervaloMin;
            entry.onBeat = mix.noTempo;
        }

        // Os arquivos de um som: <id>.ext, ou <id>_01.ext, <id>_02.ext... na pasta dele.
        static AudioClip[] FindClips(CatalogSound s)
        {
            string folder = $"{AudioFolder}/{s.pasta}";
            if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<AudioClip>();
            var name = new Regex("^" + Regex.Escape(s.id) + @"(_\d+)?$");
            return AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => name.IsMatch(Path.GetFileNameWithoutExtension(p)))
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .ToArray();
        }

        // O som de cada tipo de SoundEvent, quando a fonte não tem SoundVoice.
        // As intensidades vêm de quem emite: passos 0.25 agachado / 0.5 andando / 0.9 correndo (CaneTapper),
        // respiração 0.2 / 0.4 correndo (Breathing), batidas de 0 a 1 conforme a força (Furniture).
        static SoundChoice[] DefaultChoices() => new[]
        {
            new SoundChoice { type = SoundType.Footstep, sound = "passo_madeira", loudSound = "passo_madeira_rangendo", loudFrom = 0.8f },
            new SoundChoice { type = SoundType.Breath, sound = "respiracao_calma", loudSound = "respiracao_ofegante", loudFrom = 0.3f },
            new SoundChoice { type = SoundType.Voice, sound = "imitador_voz" },
            new SoundChoice { type = SoundType.Impact, sound = "impacto_leve", loudSound = "impacto_pesado", loudFrom = 0.6f },
            new SoundChoice { type = SoundType.Cane, sound = "bengala" },
            new SoundChoice { type = SoundType.Whistle, sound = "apito" },
            new SoundChoice { type = SoundType.Music, sound = "caixinha_musica" },
        };

        static float DefaultVolume(CatalogSound s)
        {
            if (s.id.Contains("respiracao")) return 0.35f; // respiração é pista, não protagonista
            switch (s.pasta)
            {
                case "Passos": return 0.6f;
                case "Ambiente": return 0.5f;
                case "Musica": return 0.6f;
                case "Interface": return 0.5f;
                default: return 0.9f;
            }
        }

        static float DefaultDistance(string pasta)
        {
            switch (pasta)
            {
                case "Passos":
                case "Personagens": return 15f;
                default: return 25f;
            }
        }

        // Os sons do Imitador pelos estados do GhostImitator:
        //   escondido no espelho (Patrolling)  sussurros de vez em quando
        //   puxando a vítima (Pulling)         o zumbido do espelho, sem parar
        //   puxando -> escondido               chegou no espelho com a vítima: captura
        //   desmascarado (Fleeing)             o vidro trincando
        static void GiveImitatorSounds() => AddIfMissing<GhostSounds>(MirrorHouseBuilder.ImitatorPath, sounds =>
        {
            sounds.transitions = new[]
            {
                new GhostSounds.Transition { from = GhostState.Pulling, to = GhostState.Patrolling, sound = "captura" },
                new GhostSounds.Transition { anyFrom = true, to = GhostState.Fleeing, sound = "espelho_rachando" },
            };
            sounds.loops = new[]
            {
                new GhostSounds.Loop { state = GhostState.Patrolling, sound = "imitador_sussurro", repeatEvery = 9f, volume = 0.6f },
                new GhostSounds.Loop { state = GhostState.Pulling, sound = "espelho_puxando", volume = 1f },
            };
        });

        // Prefab do mundo é de quem edita: aqui só ACRESCENTA o componente se ele ainda não tem.
        static void AddIfMissing<T>(string prefabPath, System.Action<T> fill) where T : Component
        {
            if (LevelPrefabsBuilder.Load(prefabPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.TryGetComponent(out T _)) return;
                fill(root.AddComponent<T>());
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"{typeof(T).Name} adicionado: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void GiveVoice(string prefabPath, SoundChoice[] choices) =>
            AddIfMissing<SoundVoice>(prefabPath, voice => voice.choices = choices);
    }
}
