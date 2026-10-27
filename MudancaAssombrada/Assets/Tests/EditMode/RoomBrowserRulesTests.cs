using System.Collections.Generic;
using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // A tela "Entrar numa sala" (DESIGN.md / design system, MenuSalas): busca, ordem,
    // páginas, o que o host publica sobre a sala e o código colado.
    public class RoomBrowserRulesTests
    {
        static RoomListing R(string name, string leader = "", int players = 1, int max = 3) =>
            new RoomListing { Id = name, Name = name, Leader = leader, Chapter = -1, Characters = new int[0], Players = players, MaxPlayers = max };

        static List<RoomListing> Rooms(params RoomListing[] rooms) => new(rooms);

        static string[] Names(List<RoomListing> rooms)
        {
            var names = new string[rooms.Count];
            for (int i = 0; i < rooms.Count; i++) names[i] = rooms[i].Name;
            return names;
        }

        // --- Busca ---

        [Test]
        public void BuscaVazia_MostraTodas()
        {
            var rooms = Rooms(R("A"), R("B"));
            Assert.AreEqual(new[] { "A", "B" }, Names(RoomBrowserRules.Filter(rooms, "  ")));
            Assert.AreEqual(new[] { "A", "B" }, Names(RoomBrowserRules.Filter(rooms, null)));
        }

        [Test]
        public void BuscaPeloNomeDaSala()
        {
            var rooms = Rooms(R("Sala do Zé"), R("Os Carregadores"));
            Assert.AreEqual(new[] { "Os Carregadores" }, Names(RoomBrowserRules.Filter(rooms, "carreg")));
        }

        [Test]
        public void BuscaPeloLider()
        {
            var rooms = Rooms(R("Sala 1", leader: "Bia"), R("Sala 2", leader: "Rafa"));
            Assert.AreEqual(new[] { "Sala 2" }, Names(RoomBrowserRules.Filter(rooms, "rafa")));
        }

        [Test]
        public void BuscaIgnoraAcentoEMaiuscula()
        {
            var rooms = Rooms(R("Sala do João"), R("Mudança"));
            Assert.AreEqual(new[] { "Sala do João" }, Names(RoomBrowserRules.Filter(rooms, "JOAO")));
            Assert.AreEqual(new[] { "Mudança" }, Names(RoomBrowserRules.Filter(rooms, "mudanca")));
        }

        [Test]
        public void SalasComVagaVemAntesDasCheias_SemMudarARestoDaOrdem()
        {
            var rooms = Rooms(R("Cheia", players: 3), R("A"), R("B", players: 2));
            Assert.AreEqual(new[] { "A", "B", "Cheia" }, Names(RoomBrowserRules.Filter(rooms, "")));
        }

        [Test]
        public void SalaCheia_QuandoJogadoresChegamAoMaximo()
        {
            Assert.IsTrue(R("x", players: 3, max: 3).IsFull);
            Assert.IsFalse(R("x", players: 2, max: 3).IsFull);
        }

        // --- Páginas ---

        [Test]
        public void SemSalas_AindaTemUmaPagina() => Assert.AreEqual(1, RoomBrowserRules.PageCount(0, 4));

        [Test]
        public void ContaPaginas()
        {
            Assert.AreEqual(1, RoomBrowserRules.PageCount(4, 4));
            Assert.AreEqual(2, RoomBrowserRules.PageCount(5, 4));
            Assert.AreEqual(3, RoomBrowserRules.PageCount(9, 4));
        }

        [Test]
        public void PaginaForaDoLimite_VoltaParaDentro()
        {
            // Ex.: estava na página 3, a busca deixou só 5 salas (2 páginas).
            Assert.AreEqual(1, RoomBrowserRules.ClampPage(2, 5, 4));
            Assert.AreEqual(0, RoomBrowserRules.ClampPage(-1, 5, 4));
            Assert.AreEqual(0, RoomBrowserRules.ClampPage(3, 0, 4));
        }

        [Test]
        public void PaginaPegaSoAsSalasDela()
        {
            var rooms = Rooms(R("1"), R("2"), R("3"), R("4"), R("5"), R("6"));
            Assert.AreEqual(new[] { "1", "2", "3", "4" }, Names(RoomBrowserRules.Page(rooms, 0, 4)));
            Assert.AreEqual(new[] { "5", "6" }, Names(RoomBrowserRules.Page(rooms, 1, 4)));
            Assert.IsEmpty(RoomBrowserRules.Page(rooms, 5, 4));
        }

        // --- O que o host publica sobre a sala ---

        [Test]
        public void PersonagensVaoEVoltam()
        {
            string text = RoomBrowserRules.EncodeCharacters(new[] { 0, 2 });
            Assert.AreEqual(new[] { 0, 2 }, RoomBrowserRules.DecodeCharacters(text));
        }

        [Test]
        public void PersonagensInvalidosSaoIgnorados()
        {
            Assert.AreEqual(new[] { 1 }, RoomBrowserRules.DecodeCharacters("x,1,-3,"));
            Assert.IsEmpty(RoomBrowserRules.DecodeCharacters(null));
        }

        [Test]
        public void CapituloInvalido_ViraMenosUm()
        {
            Assert.AreEqual(2, RoomBrowserRules.ParseChapter("2"));
            Assert.AreEqual(-1, RoomBrowserRules.ParseChapter("abc"));
            Assert.AreEqual(-1, RoomBrowserRules.ParseChapter(null));
            Assert.AreEqual(-1, RoomBrowserRules.ParseChapter("-5"));
        }

        // --- Código colado ---

        [Test]
        public void CodigoColado_FicaSoComLetrasENumerosEmMaiusculas()
        {
            Assert.AreEqual("K7Q2XM", RoomBrowserRules.CleanCode("  k7q 2xm\n"));
            Assert.AreEqual("K7Q2XM", RoomBrowserRules.CleanCode("Código: K7Q-2XM"));
            Assert.AreEqual("", RoomBrowserRules.CleanCode(null));
        }
    }
}
