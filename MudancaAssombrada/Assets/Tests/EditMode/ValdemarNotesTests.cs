using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Bilhetes do Seu Valdemar no menu (DESIGN.md, "Bilhete"): sorteados a cada vez que a tela
    // abre, sem repetir o último, e curtos o bastante para caber no papel.
    public class ValdemarNotesTests
    {
        [Test]
        public void NuncaRepeteOBilheteAnterior()
        {
            var random = new System.Random(42);
            int previous = 0;
            for (int i = 0; i < 500; i++)
            {
                int next = ValdemarNotes.Pick(5, previous, random);
                Assert.AreNotEqual(previous, next);
                previous = next;
            }
        }

        [Test]
        public void SorteiaTodosOsOutrosBilhetes()
        {
            var random = new System.Random(7);
            var seen = new bool[4];
            for (int i = 0; i < 500; i++) seen[ValdemarNotes.Pick(4, 0, random)] = true;
            Assert.IsFalse(seen[0], "o anterior não pode sair");
            Assert.IsTrue(seen[1] && seen[2] && seen[3], "todos os outros precisam poder sair");
        }

        [Test]
        public void SemAnterior_QualquerUmPodeSair_InclusiveOUltimo()
        {
            var random = new System.Random(3);
            bool lastSeen = false;
            for (int i = 0; i < 500; i++) lastSeen |= ValdemarNotes.Pick(3, -1, random) == 2;
            Assert.IsTrue(lastSeen);
        }

        [Test]
        public void ListaVazia_NaoTemBilhete() => Assert.AreEqual(-1, ValdemarNotes.Pick(0, -1, new System.Random(1)));

        [Test]
        public void UmBilheteSo_SaiSempreEle() => Assert.AreEqual(0, ValdemarNotes.Pick(1, 0, new System.Random(1)));

        [Test]
        public void BilhetesCabemNoPapel()
        {
            foreach (var list in new[] { ValdemarNotes.Menu, ValdemarNotes.Create, ValdemarNotes.Rooms })
            {
                Assert.IsNotEmpty(list);
                foreach (var note in list)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(note));
                    Assert.LessOrEqual(note.Length, ValdemarNotes.MaxLength, note);
                }
            }
        }
    }
}
