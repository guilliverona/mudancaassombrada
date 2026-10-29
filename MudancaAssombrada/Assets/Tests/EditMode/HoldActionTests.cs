using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Segurar um botão por um tempo (trocar as pilhas: segurar "Usar item" por 3 s). Soltar antes
    // perde o que já foi; quando completa, acontece UMA vez e só vale de novo depois de soltar.
    public class HoldActionTests
    {
        const float Dt = 0.1f;

        // Segura por "seconds" (em passos de Dt) e diz quantas vezes completou.
        static int Segurar(HoldAction hold, float seconds, bool allowed = true)
        {
            int done = 0;
            for (float t = 0f; t < seconds - 0.0001f; t += Dt)
                if (hold.Tick(true, allowed, Dt)) done++;
            return done;
        }

        [Test]
        public void Segurando3Segundos_Completa()
        {
            var hold = new HoldAction(3f);
            Assert.AreEqual(0, Segurar(hold, 2.9f));
            Assert.IsTrue(hold.Tick(true, true, Dt));
        }

        [Test]
        public void Progresso_SobeAteUm()
        {
            var hold = new HoldAction(3f);
            Assert.AreEqual(0f, hold.Progress01);
            Segurar(hold, 1.5f);
            Assert.AreEqual(0.5f, hold.Progress01, 0.01f);
            Assert.IsTrue(hold.Holding);
        }

        [Test]
        public void SoltarAntes_PerdeOQueFoi()
        {
            var hold = new HoldAction(3f);
            Segurar(hold, 2f);
            Assert.IsFalse(hold.Tick(false, true, Dt));
            Assert.AreEqual(0f, hold.Progress01);
            Assert.IsFalse(hold.Holding);
            Assert.AreEqual(0, Segurar(hold, 2.9f)); // recomeça do zero
        }

        [Test]
        public void DeixarDePoder_NoMeio_PerdeOQueFoi()
        {
            var hold = new HoldAction(3f);
            Segurar(hold, 2f);
            Assert.IsFalse(hold.Tick(true, false, Dt)); // ex.: pegou um móvel, a lanterna encheu
            Assert.AreEqual(0f, hold.Progress01);
        }

        [Test]
        public void SemPoder_NaoComeca()
        {
            var hold = new HoldAction(3f);
            Assert.AreEqual(0, Segurar(hold, 5f, allowed: false));
            Assert.AreEqual(0f, hold.Progress01);
        }

        [Test]
        public void DepoisDeCompletar_SoValeDeNovoSoltando()
        {
            var hold = new HoldAction(3f);
            Assert.AreEqual(1, Segurar(hold, 3f));
            Assert.AreEqual(0, Segurar(hold, 6f));   // continuou segurando: não troca o 2º pacote sozinho
            Assert.AreEqual(0f, hold.Progress01);
            hold.Tick(false, true, Dt);               // soltou
            Assert.AreEqual(1, Segurar(hold, 3f));
        }

        [Test]
        public void ApertarJaSemPoderESoDepoisPoder_Comeca()
        {
            // Segurava antes de as pilhas estarem na mão (trocou de espaço segurando): começa a contar
            // quando passa a poder, sem precisar soltar.
            var hold = new HoldAction(3f);
            Segurar(hold, 1f, allowed: false);
            Assert.AreEqual(1, Segurar(hold, 3f));
        }
    }
}
