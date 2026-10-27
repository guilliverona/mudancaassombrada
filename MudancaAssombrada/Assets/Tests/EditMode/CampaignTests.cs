using NUnit.Framework;
using UnityEngine;

namespace MudancaAssombrada.Tests
{
    // Regras da campanha (ARQUITETURA.md / PLANEJAMENTO.md): capítulos em ordem, um só libera
    // quando o anterior da HISTÓRIA foi concluído; "sempre liberados" (teste) não entram na fila.
    // Tudo com save e campanha na memória: nenhum arquivo de verdade é tocado.
    public class CampaignTests
    {
        static ChapterDefinition Chapter(string id, string scene = "Cena", bool always = false, int payment = 100)
        {
            var c = ScriptableObject.CreateInstance<ChapterDefinition>();
            c.id = id;
            c.sceneName = scene;
            c.alwaysUnlocked = always;
            c.payment = payment;
            return c;
        }

        static Campaign Make(params ChapterDefinition[] chapters)
        {
            var campaign = ScriptableObject.CreateInstance<Campaign>();
            campaign.SetChaptersForTests(chapters);
            return campaign;
        }

        [Test]
        public void PrimeiroCapitulo_JaComecaLiberado()
        {
            var campaign = Make(Chapter("c1"), Chapter("c2"));
            Assert.IsTrue(campaign.IsUnlocked(0, new SaveData()));
        }

        [Test]
        public void SegundoCapitulo_SoLiberaDepoisDoPrimeiro()
        {
            var campaign = Make(Chapter("c1"), Chapter("c2"));
            var save = new SaveData();
            Assert.IsFalse(campaign.IsUnlocked(1, save));

            save.completedChapters.Add("c1");
            Assert.IsTrue(campaign.IsUnlocked(1, save));
        }

        [Test]
        public void CapituloSempreLiberado_NaoContaNaFilaDaHistoria()
        {
            // [teste] [c1] [c2]: o c1 é o primeiro da história, mesmo vindo depois do de teste.
            var campaign = Make(Chapter("teste", always: true), Chapter("c1"), Chapter("c2"));
            var save = new SaveData();
            Assert.IsTrue(campaign.IsUnlocked(0, save));
            Assert.IsTrue(campaign.IsUnlocked(1, save));
            Assert.IsFalse(campaign.IsUnlocked(2, save));
        }

        [Test]
        public void IndiceForaDaLista_NaoEstaLiberado()
        {
            var campaign = Make(Chapter("c1"));
            Assert.IsFalse(campaign.IsUnlocked(-1, new SaveData()));
            Assert.IsFalse(campaign.IsUnlocked(5, new SaveData()));
            Assert.IsFalse(campaign.CanPlay(5, new SaveData()));
        }

        [Test]
        public void CapituloSemCena_EstaLiberadoMasNaoDaParaJogar()
        {
            var campaign = Make(Chapter("c1", scene: ""));
            Assert.IsTrue(campaign.IsUnlocked(0, new SaveData()));
            Assert.IsFalse(campaign.CanPlay(0, new SaveData()));
        }

        [Test]
        public void ConcluirCapitulo_PagaUmaVezSo()
        {
            var save = new SaveData();
            var c1 = Chapter("c1", payment: 150);

            Assert.IsTrue(save.Complete(c1));
            Assert.IsFalse(save.Complete(c1)); // repetir não paga de novo
            Assert.AreEqual(150, save.money);
            Assert.AreEqual(1, save.completedChapters.Count);
        }

        [Test]
        public void MarcadorDaHistoria_NaoDuplica()
        {
            var save = new SaveData();
            save.SetFlag("cartaz");
            save.SetFlag("cartaz");
            Assert.IsTrue(save.HasFlag("cartaz"));
            Assert.AreEqual(1, save.storyFlags.Count);
        }

        [Test]
        public void Save_SobreviveAIdaEVoltaPeloJson()
        {
            var save = new SaveData();
            save.Complete(Chapter("c1", payment: 80));
            save.SetFlag("diario");

            var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            Assert.IsTrue(loaded.IsCompleted("c1"));
            Assert.IsTrue(loaded.HasFlag("diario"));
            Assert.AreEqual(80, loaded.money);
        }
    }
}
