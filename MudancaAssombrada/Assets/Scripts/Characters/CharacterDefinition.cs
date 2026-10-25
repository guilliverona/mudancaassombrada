using UnityEngine;

namespace MudancaAssombrada
{
    // A "ficha" de um personagem. É um ScriptableObject: um ARQUIVO de dados (.asset) que você
    // edita no Inspector, como um prefab, mas só com dados (sem objeto na cena).
    //
    // Criar um personagem novo: botão direito na pasta Assets/Data/Personagens >
    // Create > Mudança Assombrada > Personagem, preencher a ficha e colocar no Elenco
    // (Assets/Resources/Elenco.asset). Nenhum código precisa mudar.
    //
    // Habilidade NOVA (ex.: lanterna) = um componente novo no Player + uma caixinha aqui
    // + uma linha no CharacterSwitcher.Become. Os personagens antigos nem percebem.
    [CreateAssetMenu(menuName = "Mudança Assombrada/Personagem", fileName = "Personagem")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identidade")]
        public string displayName = "Novo personagem";
        [Tooltip("Aparece entre parênteses: \"Dona Cida (cega)\". Pode ficar vazio.")]
        public string condition = "";
        [Tooltip("Cor da roupa do boneco e do nome nas listas.")]
        public Color color = Color.white;
        [TextArea] public string description = "";

        [Header("Corpo")]
        [Tooltip("Modelo 3D (FBX importado como Humanoid, ex.: Assets/Art/Characters/Cida). " +
                 "Vazio = boneco de blocos pintado com a cor acima.")]
        public GameObject model;
        [Tooltip("Altura dos olhos, em metros a partir do chão. É onde a câmera fica.")]
        [Range(0.5f, 2.5f)] public float eyeHeight = 1.6f;

        [Header("Atributos (1 = normal)")]
        [Min(0.1f)] public float speed = 1f;
        [Tooltip("Divide o peso dos móveis. 3 = o armário (peso 3) parece uma cadeira.")]
        [Min(0.1f)] public float strength = 1f;
        [Tooltip("Barulho dos móveis batendo enquanto ele carrega.")]
        [Range(0f, 2f)] public float carryNoise = 1f;
        [Tooltip("Alcance do som dos passos.")]
        [Range(0f, 2f)] public float stepVolume = 1f;

        [Header("Sentidos e habilidades")]
        [Tooltip("Tela escura: só enxerga pelo eco dos sons.")]
        public bool blind;
        [Tooltip("Não ouve nada (volume do jogo zerado).")]
        public bool deaf;
        [Tooltip("Bengala: bate no chão e cria um eco grande.")]
        public bool canTap;
        [Tooltip("Vê os sons como anéis no chão.")]
        public bool vibrationVision;
        [Tooltip("Apito: um som alto para atrair o fantasma.")]
        public bool whistle;

        // "Dona Cida (cega)" ou só "Dona Cida".
        public string FullName => string.IsNullOrEmpty(condition) ? displayName : $"{displayName} ({condition})";
    }
}
