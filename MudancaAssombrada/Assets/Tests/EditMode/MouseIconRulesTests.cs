using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // O ícone do mouse da dica de Usar (botão esquerdo aceso), desenhado por código.
    // Coordenadas em pixels do ícone (64 × 88), Y para baixo a partir do topo.
    public class MouseIconRulesTests
    {
        [Test]
        public void Canto_ForaDoMouse()
        {
            Assert.AreEqual(MouseIconRules.Part.Outside, MouseIconRules.PartAt(0f, 0f));
            Assert.AreEqual(MouseIconRules.Part.Outside, MouseIconRules.PartAt(63f, 87f));
        }

        [Test]
        public void EmCimaAEsquerda_EOBotaoEsquerdo()
        {
            Assert.AreEqual(MouseIconRules.Part.LeftButton, MouseIconRules.PartAt(16f, 22f));
        }

        [Test]
        public void EmCimaADireita_EOBotaoDireito_Apagado()
        {
            Assert.AreEqual(MouseIconRules.Part.Body, MouseIconRules.PartAt(48f, 22f));
        }

        [Test]
        public void Embaixo_EOCorpo()
        {
            Assert.AreEqual(MouseIconRules.Part.Body, MouseIconRules.PartAt(32f, 70f));
        }

        [Test]
        public void TemARodinha_EADivisaoDosBotoes()
        {
            Assert.AreEqual(MouseIconRules.Part.Wheel, MouseIconRules.PartAt(32f, 20f));
            Assert.AreEqual(MouseIconRules.Part.Line, MouseIconRules.PartAt(20f, MouseIconRules.ButtonsBottom));
        }

        [Test]
        public void ABorda_EOContorno()
        {
            Assert.AreEqual(MouseIconRules.Part.Outline, MouseIconRules.PartAt(1f, 44f));
        }
    }
}
