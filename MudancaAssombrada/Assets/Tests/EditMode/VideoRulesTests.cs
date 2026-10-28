using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Opções de vídeo: o que cada escolha do painel liga no URP (ver VideoRules).
    public class VideoRulesTests
    {
        // --- Ciclar opções (os botões "Qualidade: Alta" etc. andam em roda) ---

        [Test]
        public void Ciclar_AvancaEVoltaAoComeco()
        {
            Assert.AreEqual(1, VideoRules.Next(0, 3));
            Assert.AreEqual(0, VideoRules.Next(2, 3));
        }

        [Test]
        public void IndiceSalvoInvalido_VoltaParaOPadrao()
        {
            // PlayerPrefs editado à mão ou de uma versão com mais opções: não pode quebrar o jogo.
            Assert.AreEqual(2, VideoRules.ClampIndex(-1, VideoRules.QualityNames.Length, 2));
            Assert.AreEqual(2, VideoRules.ClampIndex(99, VideoRules.QualityNames.Length, 2));
            Assert.AreEqual(1, VideoRules.ClampIndex(1, VideoRules.QualityNames.Length, 2));
        }

        // --- Render scale e STP ---

        [Test]
        public void RenderScale_FicaEntreMetadeEInteiro()
        {
            Assert.AreEqual(VideoRules.MinRenderScale, VideoRules.ClampRenderScale(0.1f), 1e-4f);
            Assert.AreEqual(1f, VideoRules.ClampRenderScale(1.7f), 1e-4f);
            Assert.AreEqual(0.75f, VideoRules.ClampRenderScale(0.75f), 1e-4f);
        }

        [Test]
        public void AbaixoDe100PorCento_UsaSTP()
        {
            Assert.IsTrue(VideoRules.UsesStp(0.75f));
            Assert.IsFalse(VideoRules.UsesStp(1f));
        }

        [Test]
        public void ComSTP_OAntialiasingDaCameraEOMsaaDesligam()
        {
            // O STP já é temporal: TAA, SMAA ou MSAA por cima só gastam (e o URP desliga o MSAA).
            var r = VideoRules.Resolve(VideoRules.AaSmaa, 0.75f);
            Assert.AreEqual(VideoRules.CameraAa.None, r.cameraAa);
            Assert.AreEqual(1, r.msaa);
            Assert.IsTrue(r.stp);
        }

        [Test]
        public void SemSTP_CadaOpcaoDeAntialiasingLigaOSeu()
        {
            Assert.AreEqual(VideoRules.CameraAa.None, VideoRules.Resolve(VideoRules.AaNone, 1f).cameraAa);
            Assert.AreEqual(VideoRules.CameraAa.Fxaa, VideoRules.Resolve(VideoRules.AaFxaa, 1f).cameraAa);
            Assert.AreEqual(VideoRules.CameraAa.Smaa, VideoRules.Resolve(VideoRules.AaSmaa, 1f).cameraAa);
            Assert.AreEqual(VideoRules.CameraAa.Taa, VideoRules.Resolve(VideoRules.AaTaa, 1f).cameraAa);
            var msaa = VideoRules.Resolve(VideoRules.AaMsaa4, 1f);
            Assert.AreEqual(4, msaa.msaa);
            Assert.AreEqual(VideoRules.CameraAa.None, msaa.cameraAa);
        }

        [Test]
        public void SoOMsaaLigaOMsaa()
        {
            Assert.AreEqual(1, VideoRules.Resolve(VideoRules.AaTaa, 1f).msaa); // TAA não convive com MSAA
            Assert.AreEqual(1, VideoRules.Resolve(VideoRules.AaFxaa, 1f).msaa);
        }

        // --- VSync e limite de FPS ---

        [Test]
        public void ComVSync_SemLimiteDeFps()
        {
            Assert.AreEqual(-1, VideoRules.TargetFrameRate(true, 1));
        }

        [Test]
        public void SemVSync_UsaOLimiteEscolhido()
        {
            Assert.AreEqual(30, VideoRules.TargetFrameRate(false, 0));
            Assert.AreEqual(60, VideoRules.TargetFrameRate(false, 1));
            Assert.AreEqual(-1, VideoRules.TargetFrameRate(false, VideoRules.FpsNames.Length - 1)); // "Sem limite"
        }

        // --- Qualidade ---

        [Test]
        public void QualidadeMaisAlta_TemSombraMaisLongeENitida()
        {
            var baixa = VideoRules.Quality(0);
            var alta = VideoRules.Quality(VideoRules.QualityNames.Length - 1);
            Assert.Less(baixa.shadowDistance, alta.shadowDistance);
            Assert.Less(baixa.shadowResolution, alta.shadowResolution);
            Assert.LessOrEqual(baixa.cascades, alta.cascades);
        }
    }
}
