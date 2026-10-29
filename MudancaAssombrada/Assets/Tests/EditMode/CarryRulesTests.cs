using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Peso e dupla (ARQUITETURA.md: "Peso e dupla"). Cada um levanta sozinho até a própria força.
    public class CarryRulesTests
    {
        // Forças das fichas e pesos dos móveis, como no ARQUITETURA.md.
        const float Cida = 1f, Tiago = 1.5f, Marcelo = 3f;
        const float Cadeira = 1f, Caixa = 1.5f, CaixaG = 2f, Penteadeira = 2.5f, Armario = 3.5f;
        const float Ninguem = 0f;

        // --- Levantar sozinho ---

        [Test]
        public void Cida_LevantaACadeira_EArrastaACaixa()
        {
            Assert.IsTrue(CarryRules.CanLift(Cida, Ninguem, Cadeira));
            Assert.IsFalse(CarryRules.CanLift(Cida, Ninguem, Caixa));
        }

        [Test]
        public void Tiago_LevantaACaixa_EArrastaACaixaG()
        {
            Assert.IsTrue(CarryRules.CanLift(Tiago, Ninguem, Caixa));
            Assert.IsFalse(CarryRules.CanLift(Tiago, Ninguem, CaixaG));
        }

        [Test]
        public void Marcelo_LevantaACaixaG_EArrastaOArmario()
        {
            Assert.IsTrue(CarryRules.CanLift(Marcelo, Ninguem, CaixaG));
            Assert.IsFalse(CarryRules.CanLift(Marcelo, Ninguem, Armario));
        }

        // --- Dupla ---

        [Test]
        public void CidaETiago_LevantamAPenteadeira_NoLimiteExato()
        {
            // 1 + 1,5 = 2,5: o limite é "maior ou igual".
            Assert.IsTrue(CarryRules.CanLift(Cida, Tiago, Penteadeira));
        }

        [Test]
        public void Armario_SoSaiComOMarceloNaDupla()
        {
            Assert.IsFalse(CarryRules.CanLift(Cida, Tiago, Armario));
            Assert.IsTrue(CarryRules.CanLift(Cida, Marcelo, Armario));
            Assert.IsTrue(CarryRules.CanLift(Marcelo, Tiago, Armario));
        }

        [Test]
        public void PontaFalsa_ValeZero_EOMovelContinuaArrastando()
        {
            // O Imitador "segura" a outra ponta: entra com limite 0.
            Assert.IsFalse(CarryRules.CanLift(Tiago, Ninguem, Penteadeira));
            Assert.AreEqual(Tiago, CarryRules.CrewStrength(Tiago, Ninguem), 1e-5f);
        }

        // --- Segurar a outra ponta ---

        [Test]
        public void OutraPonta_SoAparece_SeQuemSeguraSenteOPeso()
        {
            Assert.IsFalse(CarryRules.FeelsWeight(Marcelo, CaixaG), "Marcelo não sente a caixa G");
            Assert.IsTrue(CarryRules.FeelsWeight(Cida, Caixa));
            Assert.IsFalse(CarryRules.FeelsWeight(Cida, Cadeira), "peso igual à força não pesa");
        }

        [Test]
        public void OutraPonta_SoAparece_ComPontaLivreESemPontaFalsa()
        {
            Assert.IsTrue(CarryRules.HelpOffered(true, hasHelper: false, hasFakeHelper: false));
            Assert.IsFalse(CarryRules.HelpOffered(true, hasHelper: true, hasFakeHelper: false));
            Assert.IsFalse(CarryRules.HelpOffered(true, hasHelper: false, hasFakeHelper: true));
            Assert.IsFalse(CarryRules.HelpOffered(false, hasHelper: false, hasFakeHelper: false));
        }

        [Test]
        public void Host_AceitaUmColegaLivreNaOutraPonta()
        {
            Assert.IsTrue(CarryRules.CanAcceptHelper(true, true, false, false, sender: 2, owner: 1, senderBusyElsewhere: false));
        }

        [Test]
        public void Host_RecusaODonoComoAjudante_EMovelSolto()
        {
            Assert.IsFalse(CarryRules.CanAcceptHelper(true, true, false, false, sender: 1, owner: 1, senderBusyElsewhere: false));
            Assert.IsFalse(CarryRules.CanAcceptHelper(false, true, false, false, sender: 2, owner: 1, senderBusyElsewhere: false));
        }

        [Test]
        public void Host_RecusaAjudanteQueJaCarregaOutroMovel()
        {
            // Bug: só o cliente impedia. Um cliente modificado (ou dois pedidos ao mesmo tempo)
            // segurava dois móveis de uma vez.
            Assert.IsFalse(CarryRules.CanAcceptHelper(true, true, false, false, sender: 2, owner: 1, senderBusyElsewhere: true));
        }

        [Test]
        public void Host_EntregaMovelSolto_AQuemEstaDeMaosLivres()
        {
            Assert.IsTrue(CarryRules.CanAcceptPickUp(isHeld: false, senderBusyElsewhere: false));
            Assert.IsFalse(CarryRules.CanAcceptPickUp(isHeld: true, senderBusyElsewhere: false), "dois pegaram: o primeiro leva");
        }

        [Test]
        public void Host_RecusaPegarQuemJaCarregaOuAjudaOutroMovel()
        {
            // Bug: só o TryPickUp do cliente impedia. Pedir a outra ponta e, antes da resposta,
            // pegar outro móvel deixava a pessoa com dois móveis.
            Assert.IsFalse(CarryRules.CanAcceptPickUp(isHeld: false, senderBusyElsewhere: true));
        }

        // --- Velocidade ---

        [Test]
        public void Dupla_AndaNoPassoDoMaisLento()
        {
            Assert.AreEqual(0.8f, CarryRules.CrewSpeed(1f, 0.8f), 1e-5f);
            Assert.AreEqual(0.8f, CarryRules.CrewSpeed(0.8f, 1f), 1e-5f);
            Assert.AreEqual(0.9f, CarryRules.CrewSpeed(0.9f, Ninguem), 1e-5f);
            Assert.AreEqual(1f, CarryRules.CrewSpeed(Ninguem, Ninguem), 1e-5f);
        }

        [Test]
        public void ForcaDaDupla_SomaAsDuasPontas_ENuncaZero()
        {
            Assert.AreEqual(2.5f, CarryRules.CrewStrength(Cida, Tiago), 1e-5f);
            Assert.AreEqual(0.1f, CarryRules.CrewStrength(Ninguem, Ninguem), 1e-5f);
        }

        [Test]
        public void PesoSentido_NuncaMenosQue1()
        {
            Assert.AreEqual(1f, CarryRules.FeltWeight(Cadeira, Marcelo), 1e-5f);
            Assert.AreEqual(3.5f, CarryRules.FeltWeight(Armario, Cida), 1e-5f);
            Assert.AreEqual(1.4f, CarryRules.FeltWeight(Armario, Cida + Tiago), 1e-5f);
        }

        [Test]
        public void Arrastando_FicaBemDevagar()
        {
            // Cida arrastando o armário: 0,35 ÷ 3,5 = 10% (o Tooltip do FurnitureCarrier).
            float felt = CarryRules.FeltWeight(Armario, Cida);
            Assert.AreEqual(0.1f, CarryRules.CarrySpeed(true, 0.35f, 1f, 1f, felt, true, false, 1.5f), 1e-5f);
        }

        [Test]
        public void MovelGrandeNaEscada_AndaMaisDevagar()
        {
            Assert.IsFalse(CarryRules.IsBulky(CaixaG), "2 ainda não é grande");
            Assert.IsTrue(CarryRules.IsBulky(Penteadeira));

            float plano = CarryRules.CarrySpeed(false, 0.35f, 1f, 1f, 1f, true, false, 1.5f);
            float escada = CarryRules.CarrySpeed(false, 0.35f, 1f, 1f, 1f, true, true, 1.5f);
            Assert.AreEqual(plano / 1.5f, escada, 1e-5f);
            // Móvel pequeno não muda na escada.
            Assert.AreEqual(plano, CarryRules.CarrySpeed(false, 0.35f, 1f, 1f, 1f, false, true, 1.5f), 1e-5f);
        }

        [Test]
        public void Levantando_SegueOPassoDaDuplaRelativoAMim()
        {
            // Eu ando a 1, o colega a 0,8, peso sentido 1: ando a 80%.
            Assert.AreEqual(0.8f, CarryRules.CarrySpeed(false, 0.35f, 0.8f, 1f, 1f, false, false, 1.5f), 1e-5f);
        }

        // --- Soltar (levantado, o móvel atravessa as paredes) ---

        [Test]
        public void DentroDaParede_NaoDeixaSoltar()
        {
            // A física empurraria o móvel para o lado (para fora da parede): bloqueia.
            Assert.IsTrue(CarryRules.BlocksDrop(UnityEngine.Vector3.right, 0.3f));
        }

        [Test]
        public void EncostandoNoChao_OuQuaseNada_DeixaSoltar()
        {
            // Empurrado para cima (o chão) ou só raspando: solta e a física acerta sozinha.
            Assert.IsFalse(CarryRules.BlocksDrop(UnityEngine.Vector3.up, 0.5f));
            Assert.IsFalse(CarryRules.BlocksDrop(UnityEngine.Vector3.right, 0.02f));
        }
    }
}
