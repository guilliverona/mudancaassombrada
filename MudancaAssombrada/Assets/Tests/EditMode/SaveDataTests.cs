using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Ler o save do disco: saves antigos e corrompidos não podem travar o jogo.
    // Usa o SaveSystem.Parse (o texto), sem tocar no arquivo de verdade.
    public class SaveDataTests
    {
        [Test]
        public void SaveAntigo_SemMarcadores_CarregaComListaVazia()
        {
            // Como era o save antes de existirem os storyFlags.
            var save = SaveSystem.Parse("{\"version\":1,\"completedChapters\":[\"c1\"],\"money\":100}");
            Assert.IsTrue(save.IsCompleted("c1"));
            Assert.AreEqual(100, save.money);
            Assert.IsNotNull(save.storyFlags);
            Assert.IsFalse(save.HasFlag("qualquer"));
        }

        [Test]
        public void SaveSemNada_ViraSaveNovo()
        {
            var save = SaveSystem.Parse("{}");
            Assert.IsNotNull(save.completedChapters);
            Assert.IsNotNull(save.storyFlags);
            Assert.AreEqual(0, save.money);
        }

        [Test]
        public void SaveCorrompido_ComecaDoZero_SemErro()
        {
            Assert.DoesNotThrow(() => SaveSystem.ParseOrNew("isto não é json {{{"));
            var save = SaveSystem.ParseOrNew("isto não é json {{{");
            Assert.AreEqual(0, save.completedChapters.Count);
        }

        [Test]
        public void SaveVazio_ComecaDoZero()
        {
            Assert.AreEqual(0, SaveSystem.ParseOrNew("").completedChapters.Count);
            Assert.AreEqual(0, SaveSystem.ParseOrNew(null).completedChapters.Count);
        }

        [Test]
        public void ConcluirCapituloNulo_NaoQuebraNemPaga()
        {
            var save = new SaveData();
            Assert.IsFalse(save.Complete(null));
            Assert.AreEqual(0, save.money);
        }
    }
}
