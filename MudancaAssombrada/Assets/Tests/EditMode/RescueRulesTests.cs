using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Resgate de colega (PlayerCondition: "Resgate"). Números do Inspector do Player.
    public class RescueRulesTests
    {
        const float Levantar = 8.4f, Voltar = 1.5f, Ancora = 1.5f, Alcance = 3.5f;
        const ulong Ninguem = RescueRules.NoRescuer;

        // Simula "segundos" de resgate em passos pequenos, como o Update faz.
        static float Run(float progress, bool helping, float duration, float seconds)
        {
            const float dt = 0.01f;
            int steps = UnityEngine.Mathf.RoundToInt(seconds / dt);
            for (int i = 0; i < steps; i++)
                progress = RescueRules.Step(progress, helping, duration, Voltar, dt);
            return progress;
        }

        // --- Tempo ---

        [Test]
        public void ColegaCaido_Levanta_Em8e4Segundos()
        {
            float d = RescueRules.Duration(false, Levantar, Ancora);
            Assert.AreEqual(Levantar, d);
            Assert.Less(Run(0f, true, d, 8.3f), 1f, "ainda não levantou");
            Assert.AreEqual(1f, Run(0f, true, d, 8.4f), 1e-3f);
        }

        [Test]
        public void ColegaPuxado_OFalsoLarga_Em1e5Segundo()
        {
            float d = RescueRules.Duration(true, Levantar, Ancora);
            Assert.AreEqual(Ancora, d);
            Assert.AreEqual(1f, Run(0f, true, d, 1.5f), 1e-3f);
        }

        [Test]
        public void Soltou_OProgressoVoltaAZero_Em1e5Segundo()
        {
            Assert.AreEqual(0.5f, Run(1f, false, Levantar, 0.75f), 1e-3f);
            Assert.AreEqual(0f, Run(1f, false, Levantar, 1.5f), 1e-3f);
        }

        [Test]
        public void Progresso_FicaEntre0e1()
        {
            Assert.AreEqual(1f, RescueRules.Step(0.99f, true, Ancora, Voltar, 1f));
            Assert.AreEqual(0f, RescueRules.Step(0.01f, false, Levantar, Voltar, 1f));
        }

        // --- Quem ajuda ---

        [Test]
        public void PrimeiroQueSegura_ComecaOResgate()
        {
            Assert.AreEqual(2ul, RescueRules.Claim(Ninguem, 2, holding: true, inNeed: true));
        }

        [Test]
        public void SegundoJogador_NaoRoubaAVez_NemParaOPrimeiro()
        {
            Assert.AreEqual(2ul, RescueRules.Claim(2, 3, holding: true, inNeed: true));
            Assert.AreEqual(2ul, RescueRules.Claim(2, 3, holding: false, inNeed: true));
        }

        [Test]
        public void SoQuemComecou_PodeParar()
        {
            Assert.AreEqual(Ninguem, RescueRules.Claim(2, 2, holding: false, inNeed: true));
        }

        [Test]
        public void VitimaDePe_NinguemComecaResgate()
        {
            Assert.AreEqual(Ninguem, RescueRules.Claim(Ninguem, 2, holding: true, inNeed: false));
        }

        [Test]
        public void QuemAjuda_PrecisaEstarDePeEPerto()
        {
            Assert.IsTrue(RescueRules.CanRescue(true, true, 3.5f, Alcance), "no limite ainda vale");
            Assert.IsFalse(RescueRules.CanRescue(true, true, 3.6f, Alcance), "longe demais");
            Assert.IsFalse(RescueRules.CanRescue(true, false, 1f, Alcance), "quem ajuda caiu");
            Assert.IsFalse(RescueRules.CanRescue(false, true, 1f, Alcance), "saiu da partida");
        }

        // --- Derrota ---

        [Test]
        public void EquipeInteiraCaida_EDerrota()
        {
            Assert.IsTrue(RescueRules.EveryoneDown(3, 3));
            Assert.IsTrue(RescueRules.EveryoneDown(1, 1), "sozinho e caído");
        }

        [Test]
        public void UmDePe_APartidaSegue()
        {
            Assert.IsFalse(RescueRules.EveryoneDown(2, 3));
        }

        [Test]
        public void SemJogadores_NinguemPerdeu()
        {
            Assert.IsFalse(RescueRules.EveryoneDown(0, 0));
        }
    }
}
