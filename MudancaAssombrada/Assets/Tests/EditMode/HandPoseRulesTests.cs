using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Pose de mão por cima da animação (camada "Bengala" do Animator dos jogadores).
    public class HandPoseRulesTests
    {
        [Test]
        public void ComBengala_DePe_SeguraABengala()
        {
            Assert.AreEqual(1f, HandPoseRules.CaneWeight(holdsCane: true, down: false, gettingUp: false));
        }

        [Test]
        public void SemBengala_NuncaFazAPose()
        {
            Assert.AreEqual(0f, HandPoseRules.CaneWeight(false, false, false));
            Assert.AreEqual(0f, HandPoseRules.CaneWeight(false, true, false));
        }

        [Test]
        public void Caida_OuLevantando_SoltaAPose_ParaAQuedaTocarInteira()
        {
            Assert.AreEqual(0f, HandPoseRules.CaneWeight(true, down: true, gettingUp: false));
            Assert.AreEqual(0f, HandPoseRules.CaneWeight(true, down: false, gettingUp: true));
        }

        // --- Mão no chão (caído e levantando) ---

        [Test]
        public void MaoAcimaDoChao_NaoMexe()
        {
            Assert.AreEqual(0f, HandPoseRules.FloorLift(lowest: 0.10f, floor: 0f, clearance: 0.03f));
        }

        [Test]
        public void MaoDentroDoChao_SobeOQueFalta_MaisAEspessura()
        {
            // a ponta do dedo está 24 cm abaixo do chão; a luva tem 3 cm: sobe 27 cm
            Assert.AreEqual(0.27f, HandPoseRules.FloorLift(-0.24f, 0f, 0.03f), 1e-4f);
        }

        [Test]
        public void ChaoEmOutraAltura_ContaAPartirDele()
        {
            // no 1º andar (chão em 3 m), mão em 2,98 m e 1 cm de espessura: sobe 3 cm
            Assert.AreEqual(0.03f, HandPoseRules.FloorLift(2.98f, 3f, 0.01f), 1e-4f);
        }

        [Test]
        public void Suaviza_SemPular_EChegaNoAlvo()
        {
            float w = HandPoseRules.Smooth(0f, 1f, speed: 4f, dt: 0.1f);
            Assert.AreEqual(0.4f, w, 1e-4f, "sobe aos poucos");
            Assert.AreEqual(1f, HandPoseRules.Smooth(0.9f, 1f, 4f, 0.1f), 1e-4f, "não passa do alvo");
            Assert.AreEqual(0f, HandPoseRules.Smooth(0.2f, 0f, 4f, 0.1f), 1e-4f, "desce até zero");
        }
    }
}
