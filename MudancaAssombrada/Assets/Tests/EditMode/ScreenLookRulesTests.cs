using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // O filtro da tela (ScreenLook) e as lâmpadas que falham com o fantasma (HauntedLights).
    public class ScreenLookRulesTests
    {
        static ScreenLookRules.Look Noite(float danger = 0f, ScreenLookRules.Sight sight = ScreenLookRules.Sight.Normal,
            bool down = false, bool effects = true) =>
            ScreenLookRules.Evaluate(0f, danger, sight, down, effects);

        // --- Perigo ---

        [Test]
        public void Perigo_CacandoColado_ECheio()
        {
            Assert.AreEqual(1f, ScreenLookRules.Danger(GhostState.Hunting, 1f), 1e-4f);
        }

        [Test]
        public void Perigo_Longe_EZero()
        {
            Assert.AreEqual(0f, ScreenLookRules.Danger(GhostState.Hunting, ScreenLookRules.DangerFar + 1f));
        }

        [Test]
        public void Perigo_CaiComADistancia()
        {
            Assert.Greater(ScreenLookRules.Danger(GhostState.Hunting, 4f), ScreenLookRules.Danger(GhostState.Hunting, 8f));
        }

        [TestCase(GhostState.Disguised)]
        [TestCase(GhostState.InMirror)]
        [TestCase(GhostState.Emerging)]
        public void Perigo_ImitadorEscondido_NaoEntregaPelaTela(GhostState state)
        {
            Assert.AreEqual(0f, ScreenLookRules.Danger(state, 1f));
        }

        [Test]
        public void Perigo_PatrulhandoAssustaMenosQueCacando()
        {
            Assert.Less(ScreenLookRules.Danger(GhostState.Patrolling, 3f), ScreenLookRules.Danger(GhostState.Hunting, 3f));
        }

        // --- Lâmpadas ---

        [Test]
        public void Lampada_SemPerigo_NaoMexe()
        {
            Assert.AreEqual(1f, ScreenLookRules.Flicker(0f, 0f));
        }

        [Test]
        public void Lampada_PerigoCheio_ApagaNoRuidoBaixo()
        {
            Assert.Less(ScreenLookRules.Flicker(1f, 0.1f), 0.2f);
            Assert.Greater(ScreenLookRules.Flicker(1f, 0.9f), 0.5f, "fora do apagão ela só enfraquece");
        }

        // --- O visual ---

        [Test]
        public void Perigo_ApertaAVinhetaETiraCor()
        {
            var calmo = Noite();
            var perigo = Noite(1f);
            Assert.Greater(perigo.vignette, calmo.vignette);
            Assert.Less(perigo.saturation, calmo.saturation);
            Assert.Greater(perigo.chromatic, calmo.chromatic);
        }

        [Test]
        public void Amanhecer_EsquentaATela()
        {
            Assert.Less(Noite().temperature, 0f, "noite fria");
            Assert.Greater(ScreenLookRules.Evaluate(1f, 0f, ScreenLookRules.Sight.Normal, false, true).temperature, 0f, "dia quente");
        }

        [Test]
        public void Eco_NaoTingeATela()
        {
            // As ondas da Cida têm cor própria (Echo.shader): o filtro não pode mudar ela.
            var eco = Noite(0f, ScreenLookRules.Sight.Echo);
            Assert.IsFalse(eco.grading);
            Assert.AreEqual(0f, eco.saturation);
        }

        [Test]
        public void Vibracao_RachaMaisAImagem()
        {
            Assert.Greater(Noite(0.5f, ScreenLookRules.Sight.Vibration).chromatic, Noite(0.5f).chromatic);
        }

        [Test]
        public void Caido_FicaCinzaComBordaVermelha()
        {
            var caido = Noite(0f, down: true);
            Assert.LessOrEqual(caido.saturation, -70f);
            Assert.Greater(caido.vignetteColor.r, caido.vignetteColor.g);
        }

        [Test]
        public void QualidadeBaixa_DesligaOsEfeitosCaros()
        {
            var baixa = Noite(1f, effects: false);
            Assert.AreEqual(0f, baixa.grain);
            Assert.AreEqual(0f, baixa.chromatic);
            Assert.AreEqual(0f, baixa.bloom);
            Assert.IsFalse(VideoRules.Quality(0).effects);
            Assert.IsTrue(VideoRules.Quality(VideoRules.DefaultQuality).effects);
        }
    }
}
