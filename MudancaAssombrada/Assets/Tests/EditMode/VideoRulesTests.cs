using NUnit.Framework;
using UnityEngine;

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

        // --- Resolução ---

        [Test]
        public void Resolucoes_SemRepetirTamanho_DaMenorParaAMaior()
        {
            // O monitor lista o mesmo tamanho uma vez por taxa de atualização (60 Hz, 144 Hz...).
            var lista = new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) };
            CollectionAssert.AreEqual(
                new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) },
                VideoRules.UniqueSizes(lista));
        }

        [Test]
        public void ResolucaoSalva_AchaNaLista_SenaoAMaior()
        {
            var lista = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) };
            Assert.AreEqual(1, VideoRules.ResolutionIndex(lista, new Vector2Int(1920, 1080)));
            // Nunca salvou (0x0) ou trocou de monitor: usa a maior, a nativa.
            Assert.AreEqual(2, VideoRules.ResolutionIndex(lista, Vector2Int.zero));
            Assert.AreEqual(2, VideoRules.ResolutionIndex(lista, new Vector2Int(3840, 2160)));
        }

        [Test]
        public void ListaDeResolucoesVazia_NaoQuebra()
        {
            Assert.AreEqual(-1, VideoRules.ResolutionIndex(new Vector2Int[0], new Vector2Int(1920, 1080)));
        }

        // --- Brilho ---

        [Test]
        public void Brilho_FicaEntreMenosUmEMaisUm()
        {
            Assert.AreEqual(-VideoRules.MaxBrightness, VideoRules.ClampBrightness(-5f), 1e-4f);
            Assert.AreEqual(VideoRules.MaxBrightness, VideoRules.ClampBrightness(5f), 1e-4f);
            Assert.AreEqual(0.3f, VideoRules.ClampBrightness(0.3f), 1e-4f);
        }

        [Test]
        public void Brilho_EmPorcentagem_ZeroE100()
        {
            // O brilho é exposição (EV): +1 dobra a luz, -1 corta pela metade.
            Assert.AreEqual(100, VideoRules.BrightnessPercent(0f));
            Assert.AreEqual(200, VideoRules.BrightnessPercent(1f));
            Assert.AreEqual(50, VideoRules.BrightnessPercent(-1f));
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
