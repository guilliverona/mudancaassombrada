using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Eco da Cida: que onda cada som desenha, quando dois sons viram uma onda só e qual onda sai
    // quando a tela enche (EchoRules). Os fantasmas não passam por aqui: eles ouvem todo SoundEvent.
    public class EchoRulesTests
    {
        // --- Que onda cada som desenha ---

        [Test]
        public void BengalaEDiapasao_DesenhamOndaCheia()
        {
            Assert.AreEqual(EchoWave.Full, EchoRules.Classify(SoundType.Cane, 0.9f, fromMe: true));
            Assert.AreEqual(EchoWave.Full, EchoRules.Classify(SoundType.Tuning, 1f, fromMe: true));
            Assert.AreEqual(EchoWave.Full, EchoRules.Classify(SoundType.Cane, 0.9f, fromMe: false));
        }

        [Test]
        public void SonsDosOutros_ViramSoUmPontoNaOrigem()
        {
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Footstep, 0.5f, fromMe: false));
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Impact, 1f, fromMe: false));
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Music, 0.35f, fromMe: false));
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Voice, 0.8f, fromMe: false));
        }

        [Test]
        public void RespiracaoDosOutros_ContinuaAparecendo_EAPistaDoImitador()
        {
            // O Imitador não respira: se a respiração sumisse do eco, a Cida perderia a pista.
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Breath, 0.3f, fromMe: false));
        }

        [Test]
        public void OsPropriosPassosERespiracao_NaoDesenhamNada()
        {
            Assert.AreEqual(EchoWave.None, EchoRules.Classify(SoundType.Footstep, 0.5f, fromMe: true));
            Assert.AreEqual(EchoWave.None, EchoRules.Classify(SoundType.Breath, 0.3f, fromMe: true));
        }

        [Test]
        public void PassoProprioNaTabuaQueRange_AindaAparece()
        {
            // A tábua solta dá intensidade 0,85: é o aviso de "o chão vai ceder".
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Footstep, 0.85f, fromMe: true));
        }

        [Test]
        public void ImpactoProprio_AindaAparece()
        {
            Assert.AreEqual(EchoWave.Spot, EchoRules.Classify(SoundType.Impact, 1f, fromMe: true));
        }

        // --- Juntar sons repetidos ---

        [Test]
        public void MesmoEmissorLogoDepois_ReaproveitaAOnda()
        {
            Assert.IsTrue(EchoRules.ShouldMerge(sameSource: true, sameWave: true, secondsSince: 0.2f));
        }

        [Test]
        public void DepoisDaJanela_OuOutroEmissor_AbreOndaNova()
        {
            Assert.IsFalse(EchoRules.ShouldMerge(true, true, EchoRules.MergeWindow + 0.01f));
            Assert.IsFalse(EchoRules.ShouldMerge(false, true, 0.1f));
            Assert.IsFalse(EchoRules.ShouldMerge(true, false, 0.1f));
        }

        [Test]
        public void OndaCheia_NuncaEJuntada()
        {
            // Cada batida de bengala é uma decisão da Cida: sempre desenha.
            Assert.IsFalse(EchoRules.ShouldMergeWave(EchoWave.Full, true, true, 0.1f));
            Assert.IsTrue(EchoRules.ShouldMergeWave(EchoWave.Spot, true, true, 0.1f));
        }

        // --- Qual vaga a onda nova ocupa ---

        static readonly EchoWave F = EchoWave.Full, S = EchoWave.Spot, N = EchoWave.None;

        [Test]
        public void ComVagaLivre_UsaAVagaLivre()
        {
            var alive = new[] { 0.5f, 0f, 0.9f, 0.2f };
            var kinds = new[] { S, N, F, S };
            Assert.AreEqual(1, EchoRules.ChooseSlot(alive, kinds, F, maxFull: 3));
        }

        [Test]
        public void SemVaga_SubstituiAMaisFraca()
        {
            var alive = new[] { 0.5f, 0.1f, 0.9f, 0.4f };
            var kinds = new[] { S, S, F, S };
            Assert.AreEqual(1, EchoRules.ChooseSlot(alive, kinds, S, maxFull: 3));
        }

        [Test]
        public void NoLimiteDeOndasCheias_ACheiaNovaTiraACheiaMaisFraca()
        {
            // Mesmo com um ponto fraquinho na vaga 3, a 4ª cheia substitui a cheia mais fraca.
            var alive = new[] { 0.8f, 0.3f, 0.6f, 0.05f, 0f };
            var kinds = new[] { F, F, F, S, N };
            Assert.AreEqual(1, EchoRules.ChooseSlot(alive, kinds, F, maxFull: 3));
        }

        [Test]
        public void PontoNovo_NuncaTiraUmaOndaCheia()
        {
            var alive = new[] { 0.1f, 0.8f, 0.6f };
            var kinds = new[] { F, S, S };
            Assert.AreEqual(2, EchoRules.ChooseSlot(alive, kinds, S, maxFull: 3));
        }

        [Test]
        public void TudoCheio_OPontoNovoFicaDeFora()
        {
            var alive = new[] { 0.1f, 0.8f };
            var kinds = new[] { F, F };
            Assert.AreEqual(-1, EchoRules.ChooseSlot(alive, kinds, S, maxFull: 3));
        }
    }
}
