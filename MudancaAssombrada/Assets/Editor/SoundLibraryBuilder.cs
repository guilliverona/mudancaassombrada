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
        [Serializable] class Mix { public string id; public string camada; public float volume = -1, alcance = -1, intervaloMin = -1; public bool noTempo, importante; }

        static readonly Dictionary<string, SoundLayer> Layers = new()
        {
            ["cama"] = SoundLayer.Bed, ["ritmo"] = SoundLayer.Rhythm, ["pista"] = SoundLayer.Cue,
            ["ameaca"] = SoundLayer.Threat, ["musica"] = SoundLayer.Music, ["interface"] = SoundLayer.Interface,
        };

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
            AddNewDefaults(library);

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
            // O falso pedindo ajuda ("Me ajuda a segurar a outra ponta?") fica MUDO no áudio: o padrão de
            // voz (imitador_voz, uma voz abafada de "rádio") não combinava. O pedido continua no eco da
            // Cida, para os fantasmas que ouvem e no texto de quem ouve; quem fala alto é a voz dele (falas).
            GiveVoice(MirrorHouseBuilder.ImitatorPath, new[] { new SoundChoice { type = SoundType.Voice, sound = "" } });
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
            entry.important = mix.importante; // ameaça que é ACONTECIMENTO (toca sempre); sem isto, é clima
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
            new SoundChoice { type = SoundType.Cane, sound = "bengala", loudSound = "cida_chamada", loudFrom = 0.99f },
            new SoundChoice { type = SoundType.Tuning, sound = "diapasao_bate" },
            new SoundChoice { type = SoundType.Carry, sound = "movel_dupla_apoio", loudSound = "arrastar_movel", loudFrom = 0.6f },
            new SoundChoice { type = SoundType.Whistle, sound = "apito" },
            new SoundChoice { type = SoundType.Music, sound = "caixinha_musica" },
        };

        // Bibliotecas criadas antes de um tipo ou som novo existir ganham só o que falta:
        //   - o diapasão (SoundType.Tuning) toca "diapasao_bate";
        //   - a bengala com intensidade 1 é a CHAMADA (três batidas); a batida comum é 0,9.
        // O que alguém ajustou no Inspector continua (só preenche o vazio).
        static void AddNewDefaults(SoundLibrary library)
        {
            var list = library.defaults.ToList();
            if (!list.Any(c => (c.type & SoundType.Tuning) != 0))
                list.Add(new SoundChoice { type = SoundType.Tuning, sound = "diapasao_bate" });
            // Móvel na mão: o apoio da dupla (fraco) e o arrastar sozinho (forte).
            if (!list.Any(c => (c.type & SoundType.Carry) != 0))
                list.Add(new SoundChoice { type = SoundType.Carry, sound = "movel_dupla_apoio", loudSound = "arrastar_movel", loudFrom = 0.6f });
            for (int i = 0; i < list.Count; i++)
                if ((list[i].type & SoundType.Cane) != 0 && string.IsNullOrEmpty(list[i].loudSound))
                    list[i] = new SoundChoice { type = list[i].type, sound = list[i].sound, loudSound = "cida_chamada", loudFrom = 0.99f };
            library.defaults = list.ToArray();
        }

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

        // Os sons do Imitador pelos estados do GhostImitator. Ele é um ator vaidoso: quase tudo é a
        // VOZ dele (falas no catálogo, "imitador_fala_*"), e cada momento tem o seu repertório.
        //   dentro do espelho (InMirror)        "Minha casa..." a cada 25-47 s, sem direção (ImitatorLoops)
        //   espelho -> prestes a sair           "Minha deixa..." sem direção, em metade das vezes
        //   espelho -> em cena (a aparição)     "Senhoras e senhores..." ao sair e outra no meio do caminho
        //   em cena -> reverência               o veludo, e "Obrigado... obrigado..."
        //   puxando -> reverência               a captura, e "Mais um... para o elenco..."
        //   disfarçado/puxando -> fugindo       desmascarado: "Errei... a fala..."
        //   reverência -> fugindo               apontado na reverência: "Plateia... ingrata..."
        //   puxando (Pulling)                   o zumbido do espelho, sem parar (ele não fala no pânico)
        static GhostSounds.Transition[] ImitatorTransitions() => new[]
        {
            new GhostSounds.Transition { from = GhostState.InMirror, to = GhostState.Emerging, sound = "imitador_fala_sai", skipChance = 0.5f, everywhere = true },
            new GhostSounds.Transition { from = GhostState.InMirror, to = GhostState.Performing, sound = "imitador_fala_cena", delay = 0.5f },
            new GhostSounds.Transition { from = GhostState.InMirror, to = GhostState.Performing, sound = "imitador_fala_cena", delay = 6f },
            new GhostSounds.Transition { anyFrom = true, to = GhostState.Bowing, sound = "imitador_reverencia" },
            new GhostSounds.Transition { from = GhostState.Performing, to = GhostState.Bowing, sound = "imitador_fala_obrigado", delay = 0.6f },
            new GhostSounds.Transition { from = GhostState.Pulling, to = GhostState.Bowing, sound = "captura" },
            new GhostSounds.Transition { from = GhostState.Pulling, to = GhostState.Bowing, sound = "imitador_fala_captura", delay = 1f },
            new GhostSounds.Transition { from = GhostState.Disguised, to = GhostState.Fleeing, sound = "imitador_fala_desmascarado" },
            new GhostSounds.Transition { from = GhostState.Pulling, to = GhostState.Fleeing, sound = "imitador_fala_desmascarado" },
            new GhostSounds.Transition { from = GhostState.Bowing, to = GhostState.Fleeing, sound = "imitador_fala_ofendido" },
        };

        static GhostSounds.Loop[] ImitatorLoops() => new[]
        {
            new GhostSounds.Loop { state = GhostState.InMirror, sound = "imitador_fala", repeatEvery = 36f, firstDelay = 30f, finaleEvery = 23f, volume = 0.55f, everywhere = true },
            new GhostSounds.Loop { state = GhostState.Pulling, sound = "espelho_puxando", volume = 1f },
        };

        static void GiveImitatorSounds()
        {
            AddIfMissing<GhostSounds>(MirrorHouseBuilder.ImitatorPath, sounds =>
            {
                sounds.transitions = ImitatorTransitions();
                sounds.loops = ImitatorLoops();
            });
            UpgradeImitatorSounds();
        }

        // Os sons que este construtor já pôs no Imitador em versões anteriores (o vidro trincando, a
        // máscara, o sussurro sem palavras...). Na atualização eles saem e entram as listas novas;
        // o que alguém acrescentou à mão no prefab fica.
        static readonly HashSet<string> OldImitatorSounds = new()
        {
            "captura", "imitador_reverencia", "espelho_rachando", "imitador_mascara_gira", "imitador_sai_ofendido",
            "imitador_sussurro", "imitador_fala", "espelho_puxando",
        };

        static void UpgradeImitatorSounds()
        {
            string path = MirrorHouseBuilder.ImitatorPath;
            if (LevelPrefabsBuilder.Load(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent(out GhostSounds sounds)) return;
                if (sounds.transitions.Any(t => t.sound == "imitador_fala_desmascarado")) return; // já está novo
                bool Mine(string sound) => OldImitatorSounds.Contains(sound) || sound.StartsWith("imitador_fala");
                sounds.transitions = sounds.transitions.Where(t => !Mine(t.sound)).Concat(ImitatorTransitions()).ToArray();
                sounds.loops = sounds.loops.Where(l => !Mine(l.sound)).Concat(ImitatorLoops()).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Falas do Imitador ligadas aos momentos: {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

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
