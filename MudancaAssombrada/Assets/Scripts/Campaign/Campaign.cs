using UnityEngine;

namespace MudancaAssombrada
{
    // A campanha: os capítulos da história, NA ORDEM em que são jogados.
    // Um capítulo só libera quando o anterior foi concluído (o save guarda isso).
    //
    // Fica em Assets/Resources/Campanha.asset, carregado por nome (como o Elenco).
    [CreateAssetMenu(menuName = "Mudança Assombrada/Campanha", fileName = "Campanha")]
    public class Campaign : ScriptableObject
    {
        public const string ResourceName = "Campanha";

        [SerializeField] ChapterDefinition[] chapters = new ChapterDefinition[0];

        static Campaign instance;

        public static Campaign Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<Campaign>(ResourceName);
                    if (instance == null)
                        Debug.LogError("Campanha não encontrada em Assets/Resources. " +
                                       "Rode o menu Mudança Assombrada > Criar Todas as Cenas para criar os dados.");
                }
                return instance;
            }
        }

        public int Count => chapters.Length;

        public ChapterDefinition Get(int index) =>
            index >= 0 && index < chapters.Length ? chapters[index] : null;

        // Qual capítulo usa esta cena? (Cada computador descobre o capítulo atual pela cena aberta.)
        public ChapterDefinition FindByScene(string sceneName)
        {
            foreach (var c in chapters)
                if (c != null && c.sceneName == sceneName) return c;
            return null;
        }

        // Liberado = sempre liberado, OU é o primeiro da história, OU o anterior da história foi concluído.
        // (Capítulos "sempre liberados", como o de teste, não contam na fila da história.)
        public bool IsUnlocked(int index) => IsUnlocked(index, SaveSystem.Data);

        // A mesma regra, com o save passado de fora (os testes usam um save de mentira,
        // sem tocar no arquivo de verdade).
        public bool IsUnlocked(int index, SaveData save)
        {
            var chapter = Get(index);
            if (chapter == null) return false;
            if (chapter.alwaysUnlocked) return true;

            for (int i = index - 1; i >= 0; i--)
            {
                var previous = chapters[i];
                if (previous == null || previous.alwaysUnlocked) continue;
                return save.IsCompleted(previous.id);
            }
            return true; // é o primeiro capítulo da história
        }

        // Dá para jogar agora? Precisa estar liberado E ter cena pronta.
        public bool CanPlay(int index) => CanPlay(index, SaveSystem.Data);
        public bool CanPlay(int index, SaveData save) => IsUnlocked(index, save) && Get(index).IsPlayable;

        // Só para os testes montarem uma campanha na memória (o jogo usa a lista do .asset).
        internal void SetChaptersForTests(params ChapterDefinition[] list) => chapters = list;
    }
}
