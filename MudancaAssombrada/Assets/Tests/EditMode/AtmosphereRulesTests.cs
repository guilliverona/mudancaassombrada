using NUnit.Framework;
using UnityEngine;

namespace MudancaAssombrada.Tests
{
    // A atmosfera da Mansão do Espelho (LEVEL_DESIGN_FASE1_MANSAO.md §6 a §9): as coisas que caem
    // (R20), as teias de vão (R23), a pianola sincronizada com a trilha (R18) e o pagamento dos
    // achados de época e dos frágeis (D10, D13).
    public class AtmosphereRulesTests
    {
        // --- R20: no máximo um susto a cada 20 s, nenhum no pânico, cada um uma vez ---

        [Test]
        public void PrimeiroSusto_DisparaNaHora()
        {
            Assert.IsTrue(HouseScareRules.CanFire(5f, HouseScareRules.Never, false, 0, 1));
        }

        [Test]
        public void SegundoSusto_EsperaVinteSegundos()
        {
            Assert.IsFalse(HouseScareRules.CanFire(119.9f, 100f, false, 0, 1));
            Assert.IsTrue(HouseScareRules.CanFire(120f, 100f, false, 0, 1), "no limite já vale");
        }

        [Test]
        public void NoPanico_NadaCai()
        {
            Assert.IsFalse(HouseScareRules.CanFire(500f, HouseScareRules.Never, true, 0, 1));
        }

        [Test]
        public void CadaSusto_UmaVezPorNoite_AEstanteDuas()
        {
            Assert.IsFalse(HouseScareRules.CanFire(500f, 0f, false, 1, 1));
            Assert.IsTrue(HouseScareRules.CanFire(500f, 0f, false, 1, 2), "a estante da biblioteca cai duas vezes");
            Assert.IsFalse(HouseScareRules.CanFire(500f, 0f, false, 2, 2));
        }

        [Test]
        public void Correndo_AMenosDeUmMetroEMeio_Dispara()
        {
            Assert.IsTrue(HouseScareRules.RunningNear(1.4f, true));
            Assert.IsFalse(HouseScareRules.RunningNear(1.4f, false), "andando, não");
            Assert.IsFalse(HouseScareRules.RunningNear(1.6f, true), "longe, não");
        }

        [Test]
        public void PassarPerto_SorteiaUmaVezSo()
        {
            // Entrou no raio: sorteia. Perdeu o sorteio? Não sorteia de novo (é "uma vez").
            Assert.IsFalse(HouseScareRules.PassNear(2f, 3f, false, 0.9f, 0.5f, out bool rolled));
            Assert.IsTrue(rolled);
            Assert.IsFalse(HouseScareRules.PassNear(2f, 3f, true, 0.1f, 0.5f, out _), "já sorteou");
            Assert.IsTrue(HouseScareRules.PassNear(2f, 3f, false, 0.1f, 0.5f, out _));
            Assert.IsFalse(HouseScareRules.PassNear(4f, 3f, false, 0.1f, 0.5f, out bool far));
            Assert.IsFalse(far, "fora do raio nem sorteia");
        }

        [Test]
        public void CadeiraDeBalanco_SoBalancaSemNinguemOlhando()
        {
            // "Olhando" = o rosto do jogador a menos de 60° dela (cosseno > 0,5).
            float cos60 = Mathf.Cos(60f * Mathf.Deg2Rad);
            Assert.IsTrue(HouseScareRules.NobodyLooking(new[] { 0.2f, -1f }, cos60));
            Assert.IsFalse(HouseScareRules.NobodyLooking(new[] { 0.2f, 0.9f }, cos60), "um olhando já basta");
            Assert.IsTrue(HouseScareRules.NobodyLooking(new float[0], cos60), "sem jogadores, ninguém olha");
        }

        // --- R23: só jogador rasga teia (o Imitador flutua e atravessa sem rasgar: a pista) ---

        [Test]
        public void Teia_SoJogadorRasga()
        {
            Assert.IsTrue(HouseScareRules.TearsWeb(isPlayer: true));
            Assert.IsFalse(HouseScareRules.TearsWeb(isPlayer: false));
        }

        // --- R18: a pianola toca a mesma faixa da cama, no mesmo ponto ---

        [Test]
        public void Pianola_DiferencaPequena_NaoPula()
        {
            Assert.IsFalse(PianolaRules.NeedsResync(10.03f, 10f, 270f));
            Assert.IsTrue(PianolaRules.NeedsResync(10.5f, 10f, 270f));
        }

        [Test]
        public void Pianola_NaVoltaDaFaixa_NaoEstaFora()
        {
            // A cama voltou para o começo (0,02 s) e a pianola ainda está no fim (269,98 s):
            // estão 0,04 s separadas, não 270 s.
            Assert.AreEqual(-0.04f, PianolaRules.Drift(269.98f, 0.02f, 270f), 0.001f);
            Assert.IsFalse(PianolaRules.NeedsResync(269.98f, 0.02f, 270f));
        }

        [Test]
        public void PertoDaPianola_ATrilhaViraOPiano()
        {
            // A cama 2D abaixa perto da pianola (o som passa a vir dela) e some na Câmara.
            Assert.AreEqual(1f, PianolaRules.Closeness(2f, 3f, 10f));
            Assert.AreEqual(0f, PianolaRules.Closeness(12f, 3f, 10f));
            Assert.AreEqual(0.5f, PianolaRules.Closeness(6.5f, 3f, 10f), 0.001f);
            Assert.AreEqual(1f, PianolaRules.BedDuck(0f, false));
            Assert.Less(PianolaRules.BedDuck(1f, false), 0.5f);
            Assert.AreEqual(0f, PianolaRules.BedDuck(0f, true), "na Câmara, o piano para");
        }

        // --- D10 e D13: achados pagam a mais; frágil batido dá desconto ---

        [Test]
        public void Achados_SomamAoPagamento()
        {
            Assert.AreEqual(100 + 60 + 25, PeriodFindRules.Pay(100, new[] { 60, 25 }, 0, 5));
        }

        [Test]
        public void FragilBatido_DaDesconto_MasNuncaNegativo()
        {
            Assert.AreEqual(100 - 3 * 5, PeriodFindRules.Pay(100, new int[0], 3, 5));
            Assert.AreEqual(0, PeriodFindRules.Pay(10, new int[0], 50, 5), "o chefe não cobra da equipe");
        }

        [Test]
        public void FragilBatido_ContaUmaVezPorPancada()
        {
            // Encostar devagar não conta; uma pancada conta; quicar logo depois (menos de 1 s) é a mesma.
            Assert.IsFalse(PeriodFindRules.CountsHit(1.0f, 10f, PeriodFindRules.Never));
            Assert.IsTrue(PeriodFindRules.CountsHit(2.0f, 10f, PeriodFindRules.Never));
            Assert.IsFalse(PeriodFindRules.CountsHit(2.0f, 10.5f, 10f));
            Assert.IsTrue(PeriodFindRules.CountsHit(2.0f, 11f, 10f));
        }

        [Test]
        public void Save_PagaOExtraJuntoComOCapitulo()
        {
            var chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            chapter.id = "f1";
            chapter.payment = 100;
            var save = new SaveData();
            Assert.IsTrue(save.Complete(chapter, 85));
            Assert.AreEqual(185, save.money);
            Assert.IsFalse(save.Complete(chapter, 85), "repetir não paga de novo");
            Assert.AreEqual(185, save.money);
            Object.DestroyImmediate(chapter);
        }

        [Test]
        public void Save_DescontoNaoDeixaODinheiroNegativo()
        {
            var chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            chapter.id = "f1";
            chapter.payment = 50;
            var save = new SaveData { money = 30 };
            save.Complete(chapter, -80);
            Assert.AreEqual(30, save.money, "o capítulo paga zero, não tira do caixa");
            Object.DestroyImmediate(chapter);
        }
    }
}
