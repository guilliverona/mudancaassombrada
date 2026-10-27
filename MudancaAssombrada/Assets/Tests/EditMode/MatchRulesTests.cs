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

        // --- Reforço (parte 3) ---

        [Test]
        public void Estrelas_NasBordas_ArredondamParaCima()
        {
            // Cada estrela = 20% da noite sobrando. Exatamente 40% ainda são 2; um pouco mais já são 3.
            Assert.AreEqual(2, MatchRules.Stars(72f, 180f));    // 40%
            Assert.AreEqual(3, MatchRules.Stars(72.5f, 180f));  // pouco mais de 40%
            Assert.AreEqual(1, MatchRules.Stars(36f, 180f));    // 20%
            Assert.AreEqual(4, MatchRules.Stars(144f, 180f));   // 80%
        }

        [Test]
        public void EtapaFaltandoNoMeio_NaoTrava()
        {
            // Objetivos só nas etapas 0 e 2 (ninguém usou a 1): são duas etapas, em ordem.
            var stages = MatchRules.Stages(new[] { 2, 0 });
            CollectionAssert.AreEqual(new[] { 0, 2 }, stages);
            int next = MatchRules.AdvanceStage(0, stages.Count, out bool won);
            Assert.AreEqual(2, stages[next]);
            Assert.IsFalse(won);
        }

        [Test]
        public void TextoDosObjetivos_CortadoNoLimite()
        {
            Assert.AreEqual("curto", MatchRules.FitObjectiveText("curto"));
            Assert.AreEqual(MatchRules.ObjectiveTextMax,
                MatchRules.FitObjectiveText(new string('a', 400)).Length);
        }

        [Test]
        public void TextoDosObjetivos_NoPiorCaso_CabeNaRede()
        {
            // "✓" ocupa 3 bytes: 150 deles são o pior caso real da HUD. Precisa caber nos 512 bytes.
            string pior = MatchRules.FitObjectiveText(new string('✓', 400));
            Assert.DoesNotThrow(() => new Unity.Collections.FixedString512Bytes(pior));
        }
    }
}
