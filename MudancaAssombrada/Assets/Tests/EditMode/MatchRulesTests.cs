using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Regras da partida (ARQUITETURA.md: objetivos em etapas; roteiro: avaliação em estrelas).
    public class MatchRulesTests
    {
        // --- Relógio da noite ---

        [Test]
        public void Noite_ComecaEm0_EAmanheceEm1()
        {
            Assert.AreEqual(0f, MatchRules.NightProgress(180f, 180f));
            Assert.AreEqual(0.5f, MatchRules.NightProgress(90f, 180f), 1e-5f);
            Assert.AreEqual(1f, MatchRules.NightProgress(0f, 180f));
        }

        [Test]
        public void SemRelogio_ENoiteOTempoTodo()
        {
            Assert.AreEqual(0f, MatchRules.NightProgress(0f, 0f));
        }

        // --- Estrelas ---

        [Test]
        public void TerminarComTempoSobrando_DaMaisEstrelas()
        {
            Assert.AreEqual(5, MatchRules.Stars(180f, 180f), "terminou na hora");
            Assert.AreEqual(3, MatchRules.Stars(90f, 180f), "metade da noite");
            Assert.AreEqual(1, MatchRules.Stars(1f, 180f), "no último segundo");
        }

        [Test]
        public void Estrelas_NuncaPassamDe1a5()
        {
            Assert.AreEqual(1, MatchRules.Stars(0f, 180f));
            Assert.AreEqual(5, MatchRules.Stars(999f, 180f));
        }

        [Test]
        public void FaseSemRelogio_SempreCincoEstrelas()
        {
            Assert.AreEqual(5, MatchRules.Stars(0f, 0f));
        }

        // --- Etapas ---

        [Test]
        public void Etapas_SemRepetir_EmOrdem()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, MatchRules.Stages(new[] { 2, 0, 0, 1 }));
            CollectionAssert.IsEmpty(MatchRules.Stages(new int[0]));
        }

        [Test]
        public void FaseDeUmaEtapa_VenceAoCumprir()
        {
            MatchRules.AdvanceStage(0, 1, out bool won);
            Assert.IsTrue(won);
        }

        [Test]
        public void FaseFinal_PassaPelasTresEtapasAntesDeVencer()
        {
            int stage = MatchRules.AdvanceStage(0, 3, out bool won);
            Assert.AreEqual(1, stage);
            Assert.IsFalse(won);

            stage = MatchRules.AdvanceStage(stage, 3, out won);
            Assert.AreEqual(2, stage);
            Assert.IsFalse(won);

            MatchRules.AdvanceStage(stage, 3, out won);
            Assert.IsTrue(won);
        }

        // --- Objetivos ---

        [Test]
        public void ObjetivoDeLista_SoCumpreComTodos()
        {
            Assert.IsFalse(MatchRules.AllDone(2, 3));
            Assert.IsTrue(MatchRules.AllDone(3, 3));
        }

        [Test]
        public void ObjetivoComListaVazia_NuncaCumpre()
        {
            // Fase sem móveis não pode "vencer sozinha" logo no começo.
            Assert.IsFalse(MatchRules.AllDone(0, 0));
        }

        [Test]
        public void ChegarAoLocal_EquipeInteiraOuBastaUm()
        {
            Assert.IsFalse(MatchRules.AreaReached(2, 3, wholeTeam: true));
            Assert.IsTrue(MatchRules.AreaReached(3, 3, wholeTeam: true));
            Assert.IsTrue(MatchRules.AreaReached(1, 3, wholeTeam: false));
            Assert.IsFalse(MatchRules.AreaReached(0, 3, wholeTeam: false));
        }

        [Test]
        public void ChegarAoLocal_SemJogadores_NaoCumpre()
        {
            Assert.IsFalse(MatchRules.AreaReached(0, 0, wholeTeam: true));
        }

        // --- Metrônomo do relógio da casa ---

        [Test]
        public void Contratempo_EOMeioEntreDuasBatidas()
        {
            // Próxima batida em 10 s, período 1 s: o contratempo é 9,5 s.
            Assert.AreEqual(0.5f, HouseClock.OffBeatDelay(10f, 1f, 9f), 1e-5f);
        }

        [Test]
        public void ContratempoQueJaPassou_EsperaOSeguinte()
        {
            // Agora = 9,7 s: o de 9,5 já foi, o próximo é 10,5.
            Assert.AreEqual(0.8f, HouseClock.OffBeatDelay(10f, 1f, 9.7f), 1e-5f);
        }
    }
}
