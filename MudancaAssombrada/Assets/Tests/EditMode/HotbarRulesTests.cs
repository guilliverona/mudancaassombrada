using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // A hotbar (ARQUITETURA.md: "Hotbar"): até 3 espaços (o Marcelo tem 2), teclas 1-2-3, a mesma
    // tecla de novo guarda, e o móvel nas mãos manda o item para o cinto (a lanterna vai para o colete).
    // A Cida começa com a bengala num espaço; pilhas são guardadas e usadas depois.
    public class HotbarRulesTests
    {
        const int Mao = HotbarRules.EmptyHand;
        const HotbarItem Lanterna = HotbarItem.Flashlight;
        const HotbarItem Pilhas = HotbarItem.Battery;
        const HotbarItem Bengala = HotbarItem.Cane;

        static uint Barra(params HotbarItem[] itens)
        {
            uint bar = 0;
            for (int i = 0; i < itens.Length; i++) bar = HotbarRules.Set(bar, i, itens[i]);
            return bar;
        }

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
            Assert.AreEqual(-1, HotbarRules.FirstEmpty(Barra(Lanterna, Pilhas, Pilhas)));
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

        // --- Marcelo: 2 espaços ---

        [Test]
        public void Marcelo_Tecla3_NaoFazNada()
        {
            Assert.AreEqual(0, HotbarRules.Press(0, 2, slots: 2));
        }

        [Test]
        public void Marcelo_RodinhaDaAVoltaEmDois()
        {
            Assert.AreEqual(0, HotbarRules.Scroll(1, 1, slots: 2));
            Assert.AreEqual(1, HotbarRules.Scroll(0, -1, slots: 2));
            Assert.AreEqual(1, HotbarRules.Scroll(Mao, -1, slots: 2));
        }

        [Test]
        public void Marcelo_ComDoisItens_EstaCheio()
        {
            Assert.AreEqual(-1, HotbarRules.FirstEmpty(Barra(Lanterna, Pilhas), slots: 2));
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

        // --- Kit do personagem (ao nascer, ao trocar de personagem e ao reiniciar) ---

        [Test]
        public void Cida_ComecaComABengalaNoEspaco1()
        {
            uint bar = HotbarRules.ApplyKit(0, HotbarRules.Slots, hasCane: true);
            Assert.AreEqual(Bengala, HotbarRules.Get(bar, 0));
            Assert.AreEqual(1, HotbarRules.FirstEmpty(bar)); // os outros dois livres
        }

        [Test]
        public void Kit_NaoDuplicaABengala()
        {
            uint bar = HotbarRules.ApplyKit(Barra(Pilhas, Bengala), HotbarRules.Slots, hasCane: true);
            Assert.AreEqual(Barra(Pilhas, Bengala), bar);
        }

        [Test]
        public void QuemNaoTemBengala_PerdeABengala()
        {
            uint bar = HotbarRules.ApplyKit(Barra(Bengala, Lanterna), HotbarRules.Slots, hasCane: false);
            Assert.IsFalse(HotbarRules.Contains(bar, Bengala));
            Assert.IsTrue(HotbarRules.Contains(bar, Lanterna));
        }

        [Test]
        public void ParaOMarcelo_OItemDoEspaco3VaiParaUmLivre()
        {
            uint bar = HotbarRules.ApplyKit(Barra(HotbarItem.None, Lanterna, Pilhas), 2, hasCane: false);
            Assert.AreEqual(Barra(Pilhas, Lanterna), bar);
        }

        [Test]
        public void ParaOMarcelo_SemEspacoLivre_OItemDoEspaco3Cai()
        {
            uint bar = HotbarRules.ApplyKit(Barra(Lanterna, Pilhas, Pilhas), 2, hasCane: false);
            Assert.AreEqual(Barra(Lanterna, Pilhas), bar);
        }

        [Test]
        public void BengalaComABarraCheia_EntraNoEspaco1()
        {
            uint bar = HotbarRules.ApplyKit(Barra(Lanterna, Pilhas, Pilhas), HotbarRules.Slots, hasCane: true);
            Assert.AreEqual(Bengala, HotbarRules.Get(bar, 0)); // a bengala é a visão dela: nunca fica de fora
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

        // --- Tecla do item (L da lanterna, Espaço da bengala) ---

        [Test]
        public void TeclaDoItem_SemOItem_NaoFazNada()
        {
            Assert.AreEqual(ItemKey.Nothing, HotbarRules.OnItemKey(has: false, inHand: false));
            Assert.AreEqual(ItemKey.Nothing, HotbarRules.OnFlashlightKey(false, FlashlightPlace.Stowed));
        }

        [Test]
        public void TeclaDoItem_NaMao_Usa()
        {
            Assert.AreEqual(ItemKey.Use, HotbarRules.OnItemKey(has: true, inHand: true));
            Assert.AreEqual(ItemKey.Use, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Hand));
            Assert.AreEqual(ItemKey.Use, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Chest));
        }

        [Test]
        public void TeclaDoItem_Guardado_TiraEUsa()
        {
            Assert.AreEqual(ItemKey.TakeOutAndUse, HotbarRules.OnItemKey(has: true, inHand: false));
            Assert.AreEqual(ItemKey.TakeOutAndUse, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Stowed));
            Assert.AreEqual(FlashlightPlace.Hand, HotbarRules.PlaceOf(flashlightSelected: true, hasLoad: false));
        }

        // Igual à tecla do número: com o móvel nas mãos, a lanterna sai do cinto direto para o colete.
        // (Antes a L negava e o número deixava: o jogador conseguia por um o que o outro proibia.)
        [Test]
        public void TeclaL_GuardadaComMovelNasMaos_PrendeNoColeteELiga()
        {
            Assert.AreEqual(ItemKey.TakeOutAndUse, HotbarRules.OnFlashlightKey(true, FlashlightPlace.Stowed));
            Assert.AreEqual(FlashlightPlace.Chest, HotbarRules.PlaceOf(flashlightSelected: true, hasLoad: true));
        }

        // --- Pilhas guardadas ---

        [Test]
        public void Pilhas_RecarregamALanternaQueNaoEstaCheia()
        {
            Assert.IsTrue(HotbarRules.CanReload(hasFlashlight: true, charge01: 0.3f, hasLoad: false));
        }

        [Test]
        public void Pilhas_SemLanterna_NaoTemOQueRecarregar()
        {
            Assert.IsFalse(HotbarRules.CanReload(hasFlashlight: false, charge01: 0f, hasLoad: false));
        }

        [Test]
        public void Pilhas_ComALanternaCheia_NaoGasta()
        {
            Assert.IsFalse(HotbarRules.CanReload(hasFlashlight: true, charge01: 1f, hasLoad: false));
        }

        [Test]
        public void Pilhas_ComMovelNasMaos_NaoDaParaTrocar()
        {
            Assert.IsFalse(HotbarRules.CanReload(hasFlashlight: true, charge01: 0.3f, hasLoad: true));
        }
    }
}
