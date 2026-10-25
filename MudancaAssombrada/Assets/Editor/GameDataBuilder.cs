using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Cria os ARQUIVOS DE DADOS do jogo na primeira vez: as fichas dos personagens (e o Elenco)
    // e as fichas dos capítulos (e a Campanha).
    // Só cria o que falta: depois disso os dados são SEUS, editados no Inspector.
    // Rodar de novo nunca apaga o que você ajustou.
    public static class GameDataBuilder
    {
        const string CharactersFolder = "Assets/Data/Personagens";
        const string ChaptersFolder = "Assets/Data/Capítulos";
        const string RosterPath = "Assets/Resources/" + CharacterRoster.ResourceName + ".asset";
        const string CampaignPath = "Assets/Resources/" + Campaign.ResourceName + ".asset";

        [MenuItem("Mudança Assombrada/Criar Dados que Faltam", priority = 20)]
        public static void EnsureData()
        {
            EditorTools.EnsureFolder("Assets/Resources");
            EnsureRoster();
            EnsureCampaign();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Mudança Assombrada/Apagar Progresso (save)", priority = 40)]
        static void ResetSave()
        {
            if (!EditorUtility.DisplayDialog("Apagar o progresso?",
                    "Os capítulos concluídos, o dinheiro e os marcadores da história deste computador serão apagados.",
                    "Apagar", "Cancelar")) return;
            SaveSystem.ResetProgress();
            Debug.Log("Progresso apagado: a campanha volta para o começo.");
        }

        // ---------- Personagens ----------

        static void EnsureRoster()
        {
            if (AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath) != null) return;
            EditorTools.EnsureFolder(CharactersFolder);

            // Os três personagens do roteiro.
            var roster = ScriptableObject.CreateInstance<CharacterRoster>();
            AssetDatabase.CreateAsset(roster, RosterPath);
            EditorTools.SetReferences(roster, "characters", new Object[] { Cida, Tiago, Marcelo });
            Debug.Log($"Personagens criados em {CharactersFolder} e o Elenco em {RosterPath}.");
        }

        static CharacterDefinition Cida => Character("Cida", c =>
        {
            c.displayName = "Dona Cida";
            c.condition = "cega";
            c.color = new Color(0.6f, 0.4f, 0.9f); // roxo
            c.description = "Ex-afinadora de pianos. Enxerga pelo eco dos sons; a bengala mapeia o ambiente.";
            c.blind = true;
            c.canTap = true;
        });

        static CharacterDefinition Tiago => Character("Tiago", c =>
        {
            c.displayName = "Tiago";
            c.condition = "surdo";
            c.color = new Color(1f, 0.8f, 0.2f); // amarelo
            c.description = "Ex-entregador de bicicleta. Não ouve nada, mas vê as vibrações no chão.";
            c.deaf = true;
            c.vibrationVision = true;
        });

        static CharacterDefinition Marcelo => Character("Marcelo", c =>
        {
            c.displayName = "Marcelo";
            c.condition = "mudo";
            c.color = new Color(0.3f, 0.75f, 0.35f); // verde
            c.description = "\"Marreta\", ex-estivador. Forte e silencioso; o apito atrai o fantasma.";
            c.speed = 0.85f;
            c.strength = 3f;
            c.carryNoise = 0.4f;
            c.stepVolume = 0.5f;
            c.whistle = true;
        });

        // Modelos 3D dos personagens (gerados pelos scripts em tools/blender) e a altura dos olhos
        // medida em cada modelo. A chave é o nome do arquivo da ficha.
        static readonly (string file, float eyeHeight)[] ArtModels =
        {
            ("Cida", 1.09f), ("Tiago", 1.52f), ("Marcelo", 1.74f),
        };

        static string ModelPath(string file) => $"Assets/Art/Characters/{file}/SK_CHR_{file}.fbx";

        // Fichas criadas antes de existirem os modelos ficam com o campo "Modelo" vazio.
        // Aqui só preenchemos o que está VAZIO: um modelo ou altura ajustados à mão não são tocados.
        [MenuItem("Mudança Assombrada/Ligar Modelos 3D nas Fichas", priority = 23)]
        public static void AssignMissingModels()
        {
            foreach (var (file, eyeHeight) in ArtModels)
            {
                var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{CharactersFolder}/{file}.asset");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(file));
                if (character == null || model == null || character.model != null) continue;

                character.model = model;
                // 1.6 é o valor padrão do campo: significa que ninguém mexeu ainda.
                if (Mathf.Approximately(character.eyeHeight, 1.6f)) character.eyeHeight = eyeHeight;
                EditorUtility.SetDirty(character);
                Debug.Log($"Ficha {file}: modelo {ModelPath(file)}, olhos a {character.eyeHeight} m.");
            }
            AssetDatabase.SaveAssets();
        }

        static CharacterDefinition Character(string fileName, System.Action<CharacterDefinition> fill)
        {
            string path = $"{CharactersFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (existing != null) return existing; // a ficha já existe: não mexe nela

            EditorTools.EnsureFolder(CharactersFolder);
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            fill(c);
            AssetDatabase.CreateAsset(c, path);
            return c;
        }

        // ---------- Capítulos ----------
        // Títulos, fantasmas e destaques vêm do roteiro. Os bilhetes e comentários são um
        // RASCUNHO para testar: reescreva no Inspector com o texto final.

        static void EnsureCampaign()
        {
            if (AssetDatabase.LoadAssetAtPath<Campaign>(CampaignPath) != null) return;
            EditorTools.EnsureFolder(ChaptersFolder);

            var prototype = Chapter("0 - Protótipo", c =>
            {
                c.id = "prototipo";
                c.title = "Protótipo";
                c.ghostName = "O Bibliotecário (teste)";
                c.featuredCharacter = Marcelo;
                c.sceneName = "Prototipo1";
                c.alwaysUnlocked = true; // capítulo de teste: fora da fila da história
                c.nightDuration = 180f;
                c.payment = 0;
                c.openingNote = "Serviço de treino. Três móveis, um inquilino que não gosta de barulho. " +
                                "Se arranharem o armário, desconto do salário.";
                c.ratingComments = Comments(
                    "Andou devagar, mas não esbarrou em nada. Raro.",
                    "Chegou primeiro em tudo. Pena que não ouviu eu chamar.",
                    "Carregou o armário sozinho. Sem um pio. Contratado de novo.");
            });

            var mirror = Chapter("1 - A Casa do Espelho", c =>
            {
                c.id = "casa-do-espelho";
                c.title = "A Casa do Espelho";
                c.ghostName = "O Imitador";
                c.featuredCharacter = Cida;
                c.openingNote = "Casa de uma família de atores. Muitos espelhos. Se alguém pedir ajuda " +
                                "com o armário, confiram se é mesmo alguém da equipe.";
            });

            var conservatory = Chapter("2 - O Conservatório", c =>
            {
                c.id = "conservatorio";
                c.title = "O Conservatório";
                c.ghostName = "A Maestrina";
                c.featuredCharacter = Tiago;
                c.openingNote = "Escola de música abandonada. Não fiquem parados ouvindo a apresentação. " +
                                "O Tiago sabe o que fazer.";
            });

            var library = Chapter("3 - A Biblioteca do Silêncio", c =>
            {
                c.id = "biblioteca";
                c.title = "A Biblioteca do Silêncio";
                c.ghostName = "O Bibliotecário";
                c.featuredCharacter = Marcelo;
                c.openingNote = "Uma regra só: silêncio absoluto. Falem como o Marcelo.";
            });

            var finale = Chapter("4 - A Última Mudança", c =>
            {
                c.id = "ultima-mudanca";
                c.title = "A Última Mudança";
                c.ghostName = "O Inquilino";
                c.nightDuration = 0f; // a fase final é por etapas e termina COM o amanhecer
                c.openingNote = "Retirar tudo. Inclusive eu.";
            });

            var campaign = ScriptableObject.CreateInstance<Campaign>();
            AssetDatabase.CreateAsset(campaign, CampaignPath);
            EditorTools.SetReferences(campaign, "chapters", new Object[] { prototype, mirror, conservatory, library, finale });
            Debug.Log($"Capítulos criados em {ChaptersFolder} e a Campanha em {CampaignPath}.");
        }

        // Um comentário para cada personagem, na ordem Cida, Tiago, Marcelo.
        static ChapterDefinition.CharacterComment[] Comments(string cida, string tiago, string marcelo) => new[]
        {
            new ChapterDefinition.CharacterComment { character = Cida, comment = cida },
            new ChapterDefinition.CharacterComment { character = Tiago, comment = tiago },
            new ChapterDefinition.CharacterComment { character = Marcelo, comment = marcelo },
        };

        static ChapterDefinition Chapter(string fileName, System.Action<ChapterDefinition> fill)
        {
            string path = $"{ChaptersFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);
            if (existing != null) return existing;

            var c = ScriptableObject.CreateInstance<ChapterDefinition>();
            fill(c);
            AssetDatabase.CreateAsset(c, path);
            return c;
        }
    }
}
