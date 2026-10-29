using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // A barra da hotbar na HUD (DESIGN.md: "HUD da partida"): caixinhas com fita, centralizadas
    // pelo número de espaços, bateria da lanterna, entrada suave e o brilho ao pegar um item.
    public class HotbarHudRulesTests
    {
        const float Tol = 0.001f;

        // --- Centralizar (o Marcelo tem 2 espaços) ---

        [Test]
        public void TresEspacos_OMeioFicaNoCentro()
        {
            Assert.AreEqual(-74f, HotbarHudRules.SlotX(0, 3, size: 64f, gap: 10f), Tol);
            Assert.AreEqual(0f, HotbarHudRules.SlotX(1, 3, 64f, 10f), Tol);
            Assert.AreEqual(74f, HotbarHudRules.SlotX(2, 3, 64f, 10f), Tol);
        }

        [Test]
        public void DoisEspacos_FicamDeCadaLadoDoCentro()
        {
            Assert.AreEqual(-37f, HotbarHudRules.SlotX(0, 2, 64f, 10f), Tol);
            Assert.AreEqual(37f, HotbarHudRules.SlotX(1, 2, 64f, 10f), Tol);
        }

        // --- Bateria ---

        // A carga só aparece na hotbar (saiu do texto do canto): acesa e fraca, a barra fica na tela.
        [Test]
        public void LanternaAcesaEFraca_DeixaAHotbarNaTela()
        {
            Assert.IsTrue(HotbarHudRules.KeepForBattery(isOn: true, charge01: 0.1f));
            Assert.IsFalse(HotbarHudRules.KeepForBattery(isOn: true, charge01: 0.5f));
            Assert.IsFalse(HotbarHudRules.KeepForBattery(isOn: false, charge01: 0.1f));
        }

        [Test]
        public void Bateria_AbaixoDe20_EstaFraca()
        {
            Assert.IsTrue(HotbarHudRules.BatteryLow(0.19f));
            Assert.IsFalse(HotbarHudRules.BatteryLow(0.2f));
            Assert.IsFalse(HotbarHudRules.BatteryLow(1f));
        }

        // --- Entrada suave: sobe enquanto aparece ---

        [Test]
        public void Entrada_ApagadaFicaAbaixo_AcesaNoLugar()
        {
            Assert.AreEqual(-12f, HotbarHudRules.SlideOffset(alpha: 0f, distance: 12f), Tol);
            Assert.AreEqual(-6f, HotbarHudRules.SlideOffset(0.5f, 12f), Tol);
            Assert.AreEqual(0f, HotbarHudRules.SlideOffset(1f, 12f), Tol);
        }

        // --- Pegou um item: o espaço acende um instante ---

        [Test]
        public void ItemNovo_NoEspaco_E_Detectado()
        {
            uint antes = HotbarRules.Set(0, 0, HotbarItem.Flashlight);
            uint depois = HotbarRules.Set(antes, 1, HotbarItem.Battery);
            Assert.IsFalse(HotbarHudRules.Gained(antes, depois, 0)); // a lanterna já estava
            Assert.IsTrue(HotbarHudRules.Gained(antes, depois, 1));  // as pilhas chegaram
            Assert.IsFalse(HotbarHudRules.Gained(antes, depois, 2));
        }

        [Test]
        public void GastouOItem_NaoAcende()
        {
            uint antes = HotbarRules.Set(0, 1, HotbarItem.Battery);
            Assert.IsFalse(HotbarHudRules.Gained(antes, 0, 1));
        }

        [Test]
        public void Brilho_ComecaInteiro_EApagaNoTempo()
        {
            Assert.AreEqual(1f, HotbarHudRules.Flash(secondsSince: 0f, duration: 0.6f), Tol);
            Assert.AreEqual(0.5f, HotbarHudRules.Flash(0.3f, 0.6f), Tol);
            Assert.AreEqual(0f, HotbarHudRules.Flash(0.6f, 0.6f), Tol);
            Assert.AreEqual(0f, HotbarHudRules.Flash(float.PositiveInfinity, 0.6f), Tol);
        }
    }
}
