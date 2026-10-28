using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // A hotbar (ARQUITETURA.md: "Hotbar"): 3 espaços, teclas 1-2-3, a mesma tecla de novo guarda,
    // e o móvel nas mãos manda o item para o cinto (a lanterna vai para o colete).
    public class HotbarRulesTests
    {
        const int Mao = HotbarRules.EmptyHand;
        const HotbarItem Lanterna = HotbarItem.Flashlight;

        // --- Espaços (os 3 cabem num número só, para ir pela rede) ---

        [Test]
        public void Vazia_NaoTemNada()
        {
            for (int i = 0; i < HotbarRules.Slots; i++) Assert.AreEqual(HotbarItem.None, HotbarRules.Get(0, i));
            Assert.AreEqual(0, HotbarRules.FirstEmpty(0));
            Assert.IsFalse(HotbarRules.Contains(0, Lanterna));
        }

        [Test]
        public void Guardar_NoEspaco2_NaoMexeNosOutros()
        {
            uint bar = HotbarRules.Set(0, 1, Lanterna);
            Assert.AreEqual(HotbarItem.None, HotbarRules.Get(bar, 0));
            Assert.AreEqual(Lanterna, HotbarRules.Get(bar, 1));
            Assert.AreEqual(HotbarItem.None, HotbarRules.Get(bar, 2));
            Assert.AreEqual(1, HotbarRules.Find(bar, Lanterna));
            Assert.AreEqual(0, HotbarRules.FirstEmpty(bar));
        }

        [Test]
        public void Tirar_DevolveOEspacoVazio()
        {
            uint bar = HotbarRules.Set(HotbarRules.Set(0, 0, Lanterna), 0, HotbarItem.None);
            Assert.AreEqual(0u, bar);
        }

        [Test]
        public void Cheia_NaoTemEspacoLivre()
        {
            uint bar = 0;
            for (int i = 0; i < HotbarRules.Slots; i++) bar = HotbarRules.Set(bar, i, Lanterna);
            Assert.AreEqual(-1, HotbarRules.FirstEmpty(bar));
        }

        [Test]
        public void EspacoForaDaBarra_NaoMudaNada()
        {
            Assert.AreEqual(0u, HotbarRules.Set(0, HotbarRules.Slots, Lanterna));
            Assert.AreEqual(0u, HotbarRules.Set(0, -1, Lanterna));
            Assert.AreEqual(HotbarItem.None, HotbarRules.Get(uint.MaxValue, -1));
            Assert.AreEqual(-1, HotbarRules.Find(0, Lanterna));
        }

        // --- Teclas 1, 2, 3 ---

        [Test]
        public void ApertarUmNumero_SeguraAqueleEspaco()
        {
            Assert.AreEqual(0, HotbarRules.Press(Mao, 0));
            Assert.AreEqual(2, HotbarRules.Press(0, 2));
        }

        [Test]
        public void ApertarOMesmoNumero_GuardaEDeixaAMaoVazia()
        {
            Assert.AreEqual(Mao, HotbarRules.Press(1, 1));
        }

        [Test]
        public void TeclaForaDaBarra_NaoMuda()
        {
            Assert.AreEqual(1, HotbarRules.Press(1, 5));
        }

        // --- Rodinha do mouse / direcional ---

        [Test]
        public void Rodinha_DaAVolta()
        {
            Assert.AreEqual(1, HotbarRules.Scroll(0, 1));
            Assert.AreEqual(0, HotbarRules.Scroll(HotbarRules.Slots - 1, 1));
            Assert.AreEqual(HotbarRules.Slots - 1, HotbarRules.Scroll(0, -1));
        }

        [Test]
        public void Rodinha_ComAMaoVazia_ComecaPelaPonta()
        {
            Assert.AreEqual(0, HotbarRules.Scroll(Mao, 1));
            Assert.AreEqual(HotbarRules.Slots - 1, HotbarRules.Scroll(Mao, -1));
            Assert.AreEqual(Mao, HotbarRules.Scroll(Mao, 0));
        }

        // --- O que está na mão ---

        [Test]
        public void NaMao_EOItemDoEspacoEscolhido()
        {
            uint bar = HotbarRules.Set(0, 0, Lanterna);
            Assert.AreEqual(Lanterna, HotbarRules.InHand(bar, 0));
            Assert.AreEqual(HotbarItem.None, HotbarRules.InHand(bar, 1));
            Assert.AreEqual(HotbarItem.None, HotbarRules.InHand(bar, Mao));
        }

        [Test]
        public void PegarItem_ComAMaoVazia_JaSegura()
        {
            Assert.IsTrue(HotbarRules.HandIsFree(0, Mao));
            Assert.IsTrue(HotbarRules.HandIsFree(0, 1));                                // espaço vazio escolhido
            Assert.IsFalse(HotbarRules.HandIsFree(HotbarRules.Set(0, 1, Lanterna), 1)); // já segura algo
        }

        // --- Onde a lanterna fica ---

        [Test]
        public void Lanterna_EscolhidaESemMovel_FicaNaMao()
        {
            Assert.AreEqual(FlashlightPlace.Hand, HotbarRules.PlaceOf(flashlightSelected: true, hasLoad: false));
        }

        [Test]
        public void Lanterna_NaMaoEPegouMovel_VaiParaOColete()
        {
            Assert.AreEqual(FlashlightPlace.Chest, HotbarRules.PlaceOf(flashlightSelected: true, hasLoad: true));
        }

        [Test]
        public void Lanterna_NaoEscolhida_FicaGuardada()
        {
            Assert.AreEqual(FlashlightPlace.Stowed, HotbarRules.PlaceOf(false, false));
            Assert.AreEqual(FlashlightPlace.Stowed, HotbarRules.PlaceOf(false, true));
        }

        // --- Tecla L ---

        [Test]
        public void TeclaL_SemLanterna_NaoFazNada()
        {
            Assert.AreEqual(FlashlightKey.Nothing, HotbarRules.OnFlashlightKey(false, FlashlightPlace.Stowed, false));
        }

        [Test]
        public void TeclaL_NaMaoOuNoColete_LigaEDesliga()
        {
            Assert.AreEqual(FlashlightKey.Toggle, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Hand, false));
            Assert.AreEqual(FlashlightKey.Toggle, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Chest, true));
        }

        [Test]
        public void TeclaL_Guardada_TiraELiga()
        {
            Assert.AreEqual(FlashlightKey.TakeOutAndTurnOn, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Stowed, false));
        }

        [Test]
        public void TeclaL_GuardadaComMovelNasMaos_NaoDaParaTirar()
        {
            Assert.AreEqual(FlashlightKey.Nothing, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Stowed, true));
        }
    }
}
