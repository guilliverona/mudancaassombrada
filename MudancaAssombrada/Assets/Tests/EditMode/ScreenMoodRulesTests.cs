using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // O "clima" da tela: o perigo (fantasma perto) fecha a vinheta e tira a cor,
    // e as zonas de escuridão apagam um pedaço da casa.
    public class ScreenMoodRulesTests
    {
        // --- Perigo: 0 longe, 1 em cima ---

        [Test]
        public void Perigo_LongeDoFantasma_Zero()
        {
            Assert.AreEqual(0f, ScreenMoodRules.Danger(20f, 3f, 12f));
            Assert.AreEqual(0f, ScreenMoodRules.Danger(12f, 3f, 12f), "no limite de longe ainda é zero");
        }

        [Test]
        public void Perigo_PertoDoFantasma_Um()
        {
            Assert.AreEqual(1f, ScreenMoodRules.Danger(1f, 3f, 12f));
            Assert.AreEqual(1f, ScreenMoodRules.Danger(3f, 3f, 12f));
        }

        [Test]
        public void Perigo_NoMeio_Cresce()
        {
            float meio = ScreenMoodRules.Danger(7.5f, 3f, 12f);
            Assert.That(meio, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.Greater(ScreenMoodRules.Danger(5f, 3f, 12f), meio, "mais perto, mais perigo");
        }

        [Test]
        public void Perigo_SemFantasma_Zero()
        {
            Assert.AreEqual(0f, ScreenMoodRules.Danger(float.PositiveInfinity, 3f, 12f));
        }

        [Test]
        public void Perigo_SobeRapidoEDesceDevagar()
        {
            float sobe = ScreenMoodRules.Smooth(0f, 1f, 0.1f, 4f, 0.5f);
            float desce = 1f - ScreenMoodRules.Smooth(1f, 0f, 0.1f, 4f, 0.5f);
            Assert.Greater(sobe, desce, "o susto chega na hora; o alívio demora");
            Assert.LessOrEqual(ScreenMoodRules.Smooth(0f, 1f, 10f, 4f, 0.5f), 1f, "nunca passa do alvo");
        }

        // --- Zonas de escuridão (o shader DarknessZones faz a mesma conta) ---

        [Test]
        public void Zona_NoCentro_ForcaTotal()
        {
            Assert.AreEqual(0.9f, ScreenMoodRules.ZoneDarkness(0f, 4f, 0.5f, 0.9f), 1e-5f);
        }

        [Test]
        public void Zona_ForaDoRaio_Nada()
        {
            Assert.AreEqual(0f, ScreenMoodRules.ZoneDarkness(4f, 4f, 0.5f, 0.9f));
            Assert.AreEqual(0f, ScreenMoodRules.ZoneDarkness(10f, 4f, 0.5f, 0.9f));
        }

        [Test]
        public void Zona_NaBorda_Esmaece()
        {
            // Borda de 50%: até 2 m é escuro cheio; de 2 m a 4 m vai sumindo.
            Assert.AreEqual(0.9f, ScreenMoodRules.ZoneDarkness(2f, 4f, 0.5f, 0.9f), 1e-5f);
            float meio = ScreenMoodRules.ZoneDarkness(3f, 4f, 0.5f, 0.9f);
            Assert.That(meio, Is.GreaterThan(0f).And.LessThan(0.9f));
        }

        [Test]
        public void Zona_BordaZero_CorteSeco()
        {
            Assert.AreEqual(1f, ScreenMoodRules.ZoneDarkness(3.99f, 4f, 0f, 1f), 1e-5f);
        }

        [Test]
        public void Zonas_Sobrepostas_ValeAMaisEscura()
        {
            Assert.AreEqual(0.8f, ScreenMoodRules.Combine(0.5f, 0.8f), 1e-5f);
            Assert.AreEqual(1f, ScreenMoodRules.Combine(1f, 3f), "nunca passa de breu total");
        }
    }
}
