using UnityEngine;

namespace MudancaAssombrada
{
    // O elenco: a lista de personagens jogáveis, NA ORDEM (o 1º é a tecla 1, e assim por diante).
    // Pela rede viaja só a POSIÇÃO do personagem nesta lista (um número), não a ficha inteira.
    //
    // Fica em Assets/Resources/Elenco.asset: tudo numa pasta "Resources" pode ser carregado
    // por nome, de qualquer script, com Resources.Load.
    [CreateAssetMenu(menuName = "Mudança Assombrada/Elenco", fileName = "Elenco")]
    public class CharacterRoster : ScriptableObject
    {
        public const string ResourceName = "Elenco";

        [SerializeField] CharacterDefinition[] characters = new CharacterDefinition[0];

        static CharacterRoster instance;

        // Carrega na primeira vez que alguém pede, e guarda.
        public static CharacterRoster Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CharacterRoster>(ResourceName);
                    if (instance == null)
                        Debug.LogError("Elenco não encontrado em Assets/Resources. " +
                                       "Rode o menu Mudança Assombrada > Criar Todas as Cenas para criar os dados.");
                }
                return instance;
            }
        }

        public int Count => characters.Length;

        // Número fora da lista (ex.: elenco mudou) cai no primeiro personagem, em vez de quebrar.
        public CharacterDefinition Get(int index) =>
            index >= 0 && index < characters.Length ? characters[index] : characters[0];

        public bool IsValid(int index) => index >= 0 && index < characters.Length;
    }
}
