using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // MEC-T8: o que o Tiago (surdo) "ouve". Só o sub-grave, abafado, sem direção (ver DeafMixRules).
    public class DeafMixRulesTests
    {
        [Test]
        public void QuemOuve_VolumeDoJogadorESemFiltro()
        {
            Assert.AreEqual(0.8f, DeafMixRules.ListenerVolume(false, false, 0.8f), 1e-4f);
            Assert.IsFalse(DeafMixRules.UsesLowPass(false, false));
        }

        [Test]
        public void Surdo_SoOSubGraveAMenos18dB()
        {
            // -18 dB = 10^(-18/20) ≈ 0,126 do volume escolhido.
            Assert.AreEqual(0.126f, DeafMixRules.ListenerVolume(true, false, 1f), 1e-3f);
            Assert.AreEqual(0.063f, DeafMixRules.ListenerVolume(true, false, 0.5f), 1e-3f);
            Assert.IsTrue(DeafMixRules.UsesLowPass(true, false));
            Assert.AreEqual(80f, DeafMixRules.CutoffHz);
        }

        [Test]
        public void SilencioTotal_ZeraComoAntes()
        {
            Assert.AreEqual(0f, DeafMixRules.ListenerVolume(true, true, 1f));
            Assert.IsFalse(DeafMixRules.UsesLowPass(true, true));
        }

        [Test]
        public void SilencioTotal_NaoAfetaQuemOuve()
        {
            // A opção é do Tiago: a Cida e o Marcelo continuam ouvindo normal.
            Assert.AreEqual(1f, DeafMixRules.ListenerVolume(false, true, 1f));
        }
    }
}
