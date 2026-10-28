using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Visão do Tiago: que vibração cada som faz, até onde ele vê e qual marca sai quando enche
    // (VibrationRules). Os fantasmas não passam por aqui: eles ouvem todo SoundEvent.
    public class VibrationRulesTests
    {
        // --- Força de cada som ---

        [Test]
        public void SonsDoFantasma_SaoFortes()
        {
            Assert.AreEqual(VibrationKind.Strong, VibrationRules.Classify(SoundType.Footstep, fromGhost: true, fromMe: false));
            // A música da Maestrina: o Tiago não ouve, então a vibração é a pista dele.
            Assert.AreEqual(VibrationKind.Strong, VibrationRules.Classify(SoundType.Music, true, false));
        }

        [Test]
        public void ImpactoEApito_SaoFortes()
        {
            Assert.AreEqual(VibrationKind.Strong, VibrationRules.Classify(SoundType.Impact, false, false));
            Assert.AreEqual(VibrationKind.Strong, VibrationRules.Classify(SoundType.Whistle, false, false));
        }

        [Test]
        public void ColegasEMoveisArrastados_SaoFracos()
        {
            Assert.AreEqual(VibrationKind.Weak, VibrationRules.Classify(SoundType.Footstep, false, false));
            Assert.AreEqual(VibrationKind.Weak, VibrationRules.Classify(SoundType.Cane, false, false));
            Assert.AreEqual(VibrationKind.Weak, VibrationRules.Classify(SoundType.Carry, false, false));
            Assert.AreEqual(VibrationKind.Weak, VibrationRules.Classify(SoundType.Music, false, false));
        }

        [Test]
        public void OsPropriosSons_EVozERespiracao_NaoVibram()
        {
            Assert.AreEqual(VibrationKind.None, VibrationRules.Classify(SoundType.Footstep, false, fromMe: true));
            Assert.AreEqual(VibrationKind.None, VibrationRules.Classify(SoundType.Voice, true, false));
            Assert.AreEqual(VibrationKind.None, VibrationRules.Classify(SoundType.Breath, true, false));
        }

        // --- Até onde ele vê ---

        [Test]
        public void Fraco_SoAte12m()
        {
            Assert.IsTrue(VibrationRules.InReach(VibrationKind.Weak, 11f, 0f));
            Assert.IsFalse(VibrationRules.InReach(VibrationKind.Weak, 13f, 0f));
        }

        [Test]
        public void Forte_Ate20m()
        {
            Assert.IsTrue(VibrationRules.InReach(VibrationKind.Strong, 19f, 0f));
            Assert.IsFalse(VibrationRules.InReach(VibrationKind.Strong, 21f, 0f));
        }

        [Test]
        public void OutroAndar_NaoAparece()
        {
            // Passos do andar de cima, mesmo bem em cima dele.
            Assert.IsFalse(VibrationRules.InReach(VibrationKind.Strong, 3f, 3f));
            Assert.IsFalse(VibrationRules.InReach(VibrationKind.Weak, 3f, -3f));
            Assert.IsTrue(VibrationRules.InReach(VibrationKind.Weak, 3f, 1f)); // degrau, rampa
        }

        // --- Qual marca sai quando enche ---

        static readonly VibrationKind W = VibrationKind.Weak, S = VibrationKind.Strong;

        [Test]
        public void ComEspaco_NaoTiraNinguem()
        {
            Assert.AreEqual(VibrationRules.Room, VibrationRules.ChooseVictim(new[] { 0.5f, 0.2f }, new[] { W, S }, W, max: 6));
        }

        [Test]
        public void Cheio_TiraAMaisFraca()
        {
            var power = new[] { 0.5f, 0.1f, 0.9f };
            Assert.AreEqual(1, VibrationRules.ChooseVictim(power, new[] { W, W, S }, W, max: 3));
        }

        [Test]
        public void ColegaNuncaTiraOPerigo()
        {
            var power = new[] { 0.1f, 0.8f, 0.6f };
            Assert.AreEqual(2, VibrationRules.ChooseVictim(power, new[] { S, W, W }, W, max: 3));
            Assert.AreEqual(VibrationRules.Skip, VibrationRules.ChooseVictim(new[] { 0.1f, 0.2f }, new[] { S, S }, W, max: 2));
        }

        [Test]
        public void PerigoTiraOColegaPrimeiro()
        {
            // Mesmo um colega forte sai antes de um perigo fraquinho.
            var power = new[] { 0.05f, 0.9f };
            Assert.AreEqual(1, VibrationRules.ChooseVictim(power, new[] { S, W }, S, max: 2));
        }
    }
}
