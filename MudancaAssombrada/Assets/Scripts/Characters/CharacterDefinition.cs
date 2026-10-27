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
        [Tooltip("Altura do corpo (o colisor), em metros. A Cida, baixinha, passa debaixo de mesas.")]
        [Range(1f, 2.5f)] public float height = 1.8f;
        [Tooltip("Raio do colisor (\"largura dos ombros\"), em metros.")]
        [Range(0.2f, 0.6f)] public float radius = 0.4f;

        [Header("Atributos (1 = normal)")]
        [Min(0.1f)] public float speed = 1f;
        [Tooltip("Divide o peso dos móveis, e é o peso mais pesado que ele levanta sozinho (o resto, arrasta).")]
        [Min(0.1f)] public float strength = 1f;

        // O peso mais pesado que ele LEVANTA sozinho: a própria força. Cida 1, Tiago 1,5, Marcelo 3.
        // Mais pesado que isso, sozinho só arrasta: precisa de alguém na outra ponta, e a dupla levanta
        // até a soma das duas forças (Furniture.CanLift). Cida + Tiago = 2,5; o armário (3,5) pede o Marcelo.
        public float LiftLimit => strength;
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
        [Tooltip("Diapasão: no eco, cada superfície responde com o seu material; a cópia não responde.")]
        public bool tuningFork;

        // Por onde este personagem percebe os acontecimentos do jogo (GameCue).
        // Vem das caixinhas acima: ninguém precisa marcar duas vezes.
        public Senses Senses =>
            (blind ? Senses.None : Senses.Sight) |
            (deaf ? Senses.None : Senses.Hearing) |
            (vibrationVision ? Senses.Vibration : Senses.None);

        // "Dona Cida (cega)" ou só "Dona Cida".
        public string FullName => string.IsNullOrEmpty(condition) ? displayName : $"{displayName} ({condition})";
    }
}
