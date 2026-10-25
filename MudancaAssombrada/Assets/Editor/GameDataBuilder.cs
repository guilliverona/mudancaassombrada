using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Cria os ARQUIVOS DE DADOS do jogo na primeira vez (as fichas dos personagens e o Elenco).
    // Só cria o que falta: depois disso os dados são SEUS, editados no Inspector.
    // Rodar de novo nunca apaga o que você ajustou.
    public static class GameDataBuilder
    {
        const string CharactersFolder = "Assets/Data/Personagens";
        const string RosterPath = "Assets/Resources/" + CharacterRoster.ResourceName + ".asset";

        [MenuItem("Mudança Assombrada/Criar Dados que Faltam", priority = 20)]
        public static void EnsureData()
        {
            if (AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath) != null) return;

            EditorTools.EnsureFolder(CharactersFolder);
            EditorTools.EnsureFolder("Assets/Resources");

            // Os três personagens do roteiro. Os números são os mesmos de antes do Elenco existir.
            var cida = Character("Cida", c =>
            {
                c.displayName = "Dona Cida";
                c.condition = "cega";
                c.color = new Color(0.6f, 0.4f, 0.9f); // roxo
                c.description = "Enxerga pelo eco dos sons. A bengala cria um eco grande.";
                c.blind = true;
                c.canTap = true;
            });
            var tiago = Character("Tiago", c =>
            {
                c.displayName = "Tiago";
                c.condition = "surdo";
                c.color = new Color(1f, 0.8f, 0.2f); // amarelo
                c.description = "Não ouve nada, mas vê as vibrações no chão.";
                c.deaf = true;
                c.vibrationVision = true;
            });
            var marcelo = Character("Marcelo", c =>
            {
                c.displayName = "Marcelo";
                c.condition = "mudo";
                c.color = new Color(0.3f, 0.75f, 0.35f); // verde
                c.description = "Forte e silencioso, mas mais lento. O apito atrai o fantasma.";
                c.speed = 0.85f;
                c.strength = 3f;
                c.carryNoise = 0.4f;
                c.stepVolume = 0.5f;
                c.whistle = true;
            });

            var roster = ScriptableObject.CreateInstance<CharacterRoster>();
            AssetDatabase.CreateAsset(roster, RosterPath);
            EditorTools.SetReferences(roster, "characters", new Object[] { cida, tiago, marcelo });
            AssetDatabase.SaveAssets();
            Debug.Log($"Dados criados: fichas em {CharactersFolder} e o Elenco em {RosterPath}.");
        }

        static CharacterDefinition Character(string fileName, System.Action<CharacterDefinition> fill)
        {
            string path = $"{CharactersFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (existing != null) return existing; // a ficha já existe: não mexe nela

            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            fill(c);
            AssetDatabase.CreateAsset(c, path);
            return c;
        }
    }
}
