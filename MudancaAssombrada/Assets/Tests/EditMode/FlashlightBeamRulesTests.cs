using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // O desenho do facho da lanterna (o "cookie" da luz) e o brilho da lente.
    // r = distância do centro do facho (0 = meio, 1 = borda do cone).
    public class FlashlightBeamRulesTests
    {
        [Test]
        public void Facho_ForaDoCone_EEscuro()
        {
            Assert.AreEqual(0f, FlashlightBeamRules.Cookie(1f, 0f), 0.001f);
            Assert.AreEqual(0f, FlashlightBeamRules.Cookie(1.3f, 2f), 0.001f);
        }

        [Test]
        public void Facho_CentroForte()
        {
            Assert.That(FlashlightBeamRules.Cookie(0f, 0f), Is.GreaterThan(0.9f));
            Assert.That(FlashlightBeamRules.Cookie(0f, 0f), Is.GreaterThan(FlashlightBeamRules.Cookie(0.45f, 0f)));
        }

        [Test]
        public void Facho_TemOAroDoRefletor()
        {
            // O aro: mais claro que logo antes dele (lanterna barata: o refletor faz um anel).
            float antes = FlashlightBeamRules.Cookie(FlashlightBeamRules.RingRadius - 0.12f, 0f);
            float aro = FlashlightBeamRules.Cookie(FlashlightBeamRules.RingRadius, 0f);
            Assert.That(aro, Is.GreaterThan(antes + 0.05f));
        }

        [Test]
        public void Facho_SempreEntreZeroEUm()
        {
            for (float r = 0f; r <= 1.2f; r += 0.05f)
            for (float a = 0f; a < 6.3f; a += 0.3f)
            {
                float v = FlashlightBeamRules.Cookie(r, a);
                Assert.That(v, Is.InRange(0f, 1f), $"r={r} ângulo={a}");
            }
        }

        [Test]
        public void Facho_FalhasLeves_MudamComOAngulo()
        {
            // Riscos do refletor: a mesma distância, ângulos diferentes, valores um pouco diferentes.
            float a = FlashlightBeamRules.Cookie(0.5f, 0.3f), b = FlashlightBeamRules.Cookie(0.5f, 2.1f);
            Assert.That(System.Math.Abs(a - b), Is.GreaterThan(0.005f).And.LessThan(0.2f));
        }

        // --- A lente ---

        [Test]
        public void Lente_Apagada_NaoBrilha()
        {
            Assert.AreEqual(0f, FlashlightBeamRules.LensGlow(on: false, lightLevel: 1f));
        }

        [Test]
        public void Lente_AcesaSegueALuz_InclusiveFalhando()
        {
            Assert.AreEqual(1f, FlashlightBeamRules.LensGlow(true, 1f), 0.001f);
            Assert.AreEqual(0.05f, FlashlightBeamRules.LensGlow(true, 0.05f), 0.001f); // a luz "morreu" um instante
        }
    }
}
