using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Imitador: quando ele age (GhostImitator). Números do Inspector do fantasma.
    public class ImitatorRulesTests
    {
        const float Aparicao = 45f, MinDentro = 5f, Interesse = 2.5f, Espera = 12f;
        const float Pressa = 0.75f, Ofendido = 2f, Desiste = 40f, Puxar = 6f;

        static bool Pronto(bool due, bool pending, float inside, float timer, float drag) =>
            ImitatorRules.ReadyToAct(due, pending, inside, MinDentro, timer, drag, Interesse);

        // --- Aparição ---

        [Test]
        public void AntesDaAparicao_NaoAtaca_EsperaPorEla()
        {
            bool pending = ImitatorRules.AppearancePending(false, Aparicao, hasClock: true);
            bool due = ImitatorRules.AppearanceDue(pending, 30f, Aparicao);
            Assert.IsTrue(pending);
            Assert.IsFalse(due);
            // Mesmo com a espera acabada e alguém arrastando sozinho.
            Assert.IsFalse(Pronto(due, pending, 60f, 0f, 10f));
        }

        [Test]
        public void Aos45s_AAparicaoChega()
        {
            bool pending = ImitatorRules.AppearancePending(false, Aparicao, true);
            Assert.IsTrue(ImitatorRules.AppearanceDue(pending, 45f, Aparicao));
            Assert.IsTrue(Pronto(true, pending, 0f, 99f, 0f), "a aparição não espera o mínimo no espelho");
        }

        [Test]
        public void SemRelogio_OuJaApareceu_NaoHaAparicao()
        {
            Assert.IsFalse(ImitatorRules.AppearancePending(false, Aparicao, hasClock: false));
            Assert.IsFalse(ImitatorRules.AppearancePending(true, Aparicao, hasClock: true));
            Assert.IsFalse(ImitatorRules.AppearancePending(false, 0f, hasClock: true));
        }

        // --- Sair do espelho ---

        [Test]
        public void DepoisDaAparicao_SaiQuandoAEsperaAcaba()
        {
            Assert.IsFalse(Pronto(false, false, 10f, 1f, 0f));
            Assert.IsTrue(Pronto(false, false, 10f, 0f, 0f));
        }

        [Test]
        public void ArrastarSozinhoPor2e5s_ChamaOImitador()
        {
            Assert.IsFalse(Pronto(false, false, 10f, 8f, 2.4f));
            Assert.IsTrue(Pronto(false, false, 10f, 8f, 2.5f));
        }

        [Test]
        public void NuncaSaiAntesDe5sNoEspelho()
        {
            Assert.IsFalse(Pronto(false, false, 4.9f, 0f, 10f));
            Assert.IsTrue(Pronto(false, false, 5f, 0f, 0f));
        }

        // --- Tempo no espelho ---

        [Test]
        public void EsperaNormal_E12s()
        {
            Assert.AreEqual(12f, ImitatorRules.LurkTime(Espera, 0.5f, Pressa, false, Ofendido), 1e-5f);
        }

        [Test]
        public void FimDaNoite_EsperaMetade()
        {
            Assert.AreEqual(12f, ImitatorRules.LurkTime(Espera, 0.74f, Pressa, false, Ofendido), 1e-5f);
            Assert.AreEqual(6f, ImitatorRules.LurkTime(Espera, 0.75f, Pressa, false, Ofendido), 1e-5f);
        }

        [Test]
        public void Ofendido_EsperaODobro()
        {
            Assert.AreEqual(24f, ImitatorRules.LurkTime(Espera, 0.5f, Pressa, true, Ofendido), 1e-5f);
            Assert.AreEqual(12f, ImitatorRules.LurkTime(Espera, 0.9f, Pressa, true, Ofendido), 1e-5f, "pressa e ofensa se somam");
        }

        // --- Disfarce e puxão ---

        [Test]
        public void Disfarcado40sSemChegar_Desiste()
        {
            Assert.IsFalse(ImitatorRules.GiveUp(Desiste - 39.9f));
            Assert.IsTrue(ImitatorRules.GiveUp(Desiste - 40f));
        }

        [Test]
        public void Puxando_6sParaOsColegasSalvarem()
        {
            Assert.IsFalse(ImitatorRules.PullDone(5.9f, Puxar));
            Assert.IsTrue(ImitatorRules.PullDone(6f, Puxar));
        }
    }
}
