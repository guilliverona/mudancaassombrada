using NUnit.Framework;
using UnityEngine;

namespace MudancaAssombrada.Tests
{
    // O braço da lanterna (ARQUITETURA.md: "Hotbar"): com a lanterna na mão, o braço direito sobe e
    // leva a mão a um ponto à frente da câmera; um IK de dois ossos (ombro e cotovelo) acha o cotovelo.
    public class FlashlightArmRulesTests
    {
        const float Tol = 0.001f;

        // --- Quando a pose vale ---

        [Test]
        public void LanternaNaMao_DePe_LevantaOBraco()
        {
            Assert.AreEqual(1f, HandPoseRules.FlashlightWeight(inHand: true, down: false, gettingUp: false));
        }

        [Test]
        public void LanternaNoColeteOuGuardada_BracoNormal()
        {
            Assert.AreEqual(0f, HandPoseRules.FlashlightWeight(inHand: false, down: false, gettingUp: false));
        }

        [Test]
        public void CaidoOuLevantando_SoltaOBraco()
        {
            Assert.AreEqual(0f, HandPoseRules.FlashlightWeight(true, down: true, gettingUp: false));
            Assert.AreEqual(0f, HandPoseRules.FlashlightWeight(true, down: false, gettingUp: true));
        }

        // --- Onde a mão vai ---

        [Test]
        public void PontoDaMao_SegueOOlhar()
        {
            var offset = new Vector3(0.2f, -0.25f, 0.45f); // direita, abaixo, à frente da câmera
            Vector3 reto = HandPoseRules.HoldPoint(Vector3.zero, Quaternion.identity, offset);
            Assert.That(Vector3.Distance(reto, offset), Is.LessThan(Tol));

            // Olhando 90° para a esquerda, o "à frente" vira -X.
            Vector3 virado = HandPoseRules.HoldPoint(Vector3.zero, Quaternion.Euler(0, -90, 0), offset);
            Assert.That(virado.x, Is.LessThan(-0.4f));
        }

        // --- Carregar natural (o que os colegas veem): braço baixo, cotovelo pouco dobrado ---

        [Test]
        public void Carregar_MaoAbaixoEAFrenteDoOmbro_NaFracaoDoBraco()
        {
            var ombro = new Vector3(0.2f, 1.3f, 0f);
            var fracao = new Vector3(0f, -0.7f, 0.4f); // direita, cima, frente, em braços
            Vector3 mao = HandPoseRules.CarryPoint(ombro, Quaternion.identity, reach: 0.5f, fracao);
            Assert.AreEqual(1.3f - 0.35f, mao.y, Tol);
            Assert.AreEqual(0.2f, mao.z, Tol);
            Assert.That(Vector3.Distance(ombro, mao), Is.LessThan(0.5f)); // cotovelo dobrado: dentro do alcance
        }

        [Test]
        public void Carregar_BracoMaiorSeguraMaisLonge()
        {
            var fracao = new Vector3(0f, -0.7f, 0.4f);
            float tiago = HandPoseRules.CarryPoint(Vector3.zero, Quaternion.identity, 0.46f, fracao).magnitude;
            float marcelo = HandPoseRules.CarryPoint(Vector3.zero, Quaternion.identity, 0.66f, fracao).magnitude;
            Assert.AreEqual(0.66f / 0.46f, marcelo / tiago, Tol);
        }

        [Test]
        public void Carregar_OBracoSegueMetadeDoOlhar()
        {
            Assert.AreEqual(15f, HandPoseRules.ArmPitch(lookPitch: 30f, follow: 0.5f), Tol);
            Assert.AreEqual(-20f, HandPoseRules.ArmPitch(lookPitch: -40f, follow: 0.5f), Tol);
        }

        // --- Punho reto: o giro em volta do antebraço é do antebraço ---

        [Test]
        public void Giro_SoEmVoltaDoAntebraco_NaoDobraOPunho()
        {
            var giro = Quaternion.AngleAxis(60f, Vector3.forward); // antebraço ao longo de Z
            HandPoseRules.SplitTwist(giro, Vector3.forward, out var twist, out var swing);
            Assert.AreEqual(60f, Quaternion.Angle(Quaternion.identity, twist), 0.1f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, swing), 0.1f);
        }

        [Test]
        public void Giro_DeLado_ETodoDoPunho()
        {
            var giro = Quaternion.AngleAxis(40f, Vector3.up);
            HandPoseRules.SplitTwist(giro, Vector3.forward, out var twist, out var swing);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, twist), 0.1f);
            Assert.AreEqual(40f, Quaternion.Angle(Quaternion.identity, swing), 0.1f);
        }

        [Test]
        public void Giro_Misturado_RecompoeOGiroInteiro()
        {
            var giro = Quaternion.Euler(25f, 40f, 70f);
            HandPoseRules.SplitTwist(giro, new Vector3(0.3f, -1f, 0.2f), out var twist, out var swing);
            Assert.AreEqual(0f, Quaternion.Angle(giro, swing * twist), 0.1f);
        }

        // --- Correndo: o alvo não pode sacudir junto com a cabeça ---

        [Test]
        public void Suavizar_AndaMetadeNaMeiaVida()
        {
            Vector3 meio = HandPoseRules.Damp(Vector3.zero, new Vector3(1, 0, 0), halfLife: 0.1f, dt: 0.1f);
            Assert.AreEqual(0.5f, meio.x, Tol);
        }

        [Test]
        public void Suavizar_SacudidaRapida_QuaseNaoMexe()
        {
            // Uma sacudida de 5 cm que dura um quadro (60 fps) mexe menos de 1 cm com meia-vida de 0,15 s.
            Vector3 p = HandPoseRules.Damp(Vector3.zero, new Vector3(0, 0.05f, 0), halfLife: 0.15f, dt: 1f / 60f);
            Assert.That(p.y, Is.LessThan(0.01f));
        }

        [Test]
        public void Suavizar_SemMeiaVida_VaiDireto()
        {
            Vector3 alvo = new Vector3(1, 2, 3);
            Assert.That(Vector3.Distance(HandPoseRules.Damp(Vector3.zero, alvo, 0f, 0.016f), alvo), Is.LessThan(Tol));
        }

        [Test]
        public void AlvoLongeDemais_ChegaAteOBracoEsticado()
        {
            Vector3 alvo = HandPoseRules.ClampToReach(Vector3.zero, new Vector3(0, 0, 2f), reach: 0.6f);
            Assert.That(alvo.magnitude, Is.LessThan(0.6f));
            Assert.That(alvo.magnitude, Is.GreaterThan(0.55f));
            Assert.That(alvo.normalized.z, Is.GreaterThan(0.99f)); // mesma direção
        }

        [Test]
        public void AlvoAoAlcance_NaoMuda()
        {
            var perto = new Vector3(0.1f, 0f, 0.3f);
            Assert.That(Vector3.Distance(HandPoseRules.ClampToReach(Vector3.zero, perto, 0.6f), perto), Is.LessThan(Tol));
        }

        // --- IK de dois ossos ---

        [Test]
        public void Cotovelo_MantemOTamanhoDosOssos()
        {
            Vector3 ombro = Vector3.zero, alvo = new Vector3(0.1f, -0.1f, 0.4f);
            Vector3 cotovelo = HandPoseRules.Elbow(ombro, alvo, upper: 0.3f, lower: 0.28f, hint: Vector3.down);
            Assert.AreEqual(0.3f, Vector3.Distance(ombro, cotovelo), Tol);
            Assert.AreEqual(0.28f, Vector3.Distance(cotovelo, alvo), Tol);
        }

        [Test]
        public void Cotovelo_DobraParaOLadoDaDica()
        {
            Vector3 alvo = new Vector3(0f, 0f, 0.4f);
            Vector3 baixo = HandPoseRules.Elbow(Vector3.zero, alvo, 0.3f, 0.28f, Vector3.down);
            Vector3 fora = HandPoseRules.Elbow(Vector3.zero, alvo, 0.3f, 0.28f, Vector3.right);
            Assert.That(baixo.y, Is.LessThan(-0.05f));
            Assert.That(fora.x, Is.GreaterThan(0.05f));
        }

        [Test]
        public void BracoEsticado_CotoveloNaReta()
        {
            Vector3 alvo = new Vector3(0f, 0f, 0.58f); // a soma dos ossos: reto
            Vector3 cotovelo = HandPoseRules.Elbow(Vector3.zero, alvo, 0.3f, 0.28f, Vector3.down);
            Assert.That(Vector3.Distance(cotovelo, new Vector3(0, 0, 0.3f)), Is.LessThan(0.01f));
        }
    }
}
