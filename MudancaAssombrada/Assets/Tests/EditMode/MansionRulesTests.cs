using NUnit.Framework;
using UnityEngine;

namespace MudancaAssombrada.Tests
{
    // As regras da Mansão do Espelho (LEVEL_DESIGN_FASE1_MANSAO.md): o espelho escolhido pelo
    // caminho (R03), o espelho-porta da Câmara (D5, R01), o portal (R13) e os espelhos-herói (R04).
    public class MansionRulesTests
    {
        const float Inalcancavel = -1f;

        // --- R03: o espelho mais perto é o mais perto ANDANDO ---

        [Test]
        public void EspelhoMaisPerto_PeloCaminho_NaoEmLinhaReta()
        {
            // O espelho 0 fica logo acima (no andar de cima: 4 m em linha reta, 30 m andando);
            // o 1 fica no mesmo cômodo (6 m em linha reta, 6 m andando).
            float[] caminho = { 30f, 6f };
            float[] reta = { 4f, 6f };
            Assert.AreEqual(1, ImitatorRules.NearestMirror(caminho, reta));
        }

        [Test]
        public void EspelhoMaisPerto_IgnoraOsInalcancaveis()
        {
            float[] caminho = { Inalcancavel, 20f, 12f };
            float[] reta = { 1f, 15f, 10f };
            Assert.AreEqual(2, ImitatorRules.NearestMirror(caminho, reta));
        }

        [Test]
        public void SemNenhumCaminho_UsaALinhaReta()
        {
            float[] caminho = { Inalcancavel, Inalcancavel };
            float[] reta = { 9f, 3f };
            Assert.AreEqual(1, ImitatorRules.NearestMirror(caminho, reta), "sem NavMesh (cena de teste) ainda escolhe um");
        }

        [Test]
        public void EspelhoMaisLonge_PeloCaminho()
        {
            float[] caminho = { 10f, 40f, Inalcancavel };
            float[] reta = { 30f, 12f, 99f };
            Assert.AreEqual(1, ImitatorRules.FarthestMirror(caminho, reta), "fugir para um espelho inalcançável o deixaria preso");
        }

        [Test]
        public void SemEspelho_DevolveMenosUm()
        {
            Assert.AreEqual(-1, ImitatorRules.NearestMirror(new float[0], new float[0]));
            Assert.AreEqual(-1, ImitatorRules.FarthestMirror(new float[0], new float[0]));
        }

        // --- D5 / R01: o espelho-porta da Câmara Secreta ---

        [Test]
        public void ComACida_OEspelhoComecaFechado()
        {
            Assert.AreEqual(SecretDoorState.Closed, SecretDoorRules.Initial(cidaInMatch: true));
        }

        [Test]
        public void SemACida_OEspelhoJaComecaRachado_PlanoB()
        {
            Assert.AreEqual(SecretDoorState.Revealed, SecretDoorRules.Initial(cidaInMatch: false));
        }

        [Test]
        public void Fechado_SoACidaAfina_OsOutrosNemVeem()
        {
            Assert.AreEqual(SecretDoorAction.Tune, SecretDoorRules.ActionFor(SecretDoorState.Closed, hasTuningFork: true));
            Assert.AreEqual(SecretDoorAction.None, SecretDoorRules.ActionFor(SecretDoorState.Closed, hasTuningFork: false));
        }

        [Test]
        public void Revelado_QualquerUmEmpurra()
        {
            Assert.AreEqual(SecretDoorAction.Push, SecretDoorRules.ActionFor(SecretDoorState.Revealed, hasTuningFork: true));
            Assert.AreEqual(SecretDoorAction.Push, SecretDoorRules.ActionFor(SecretDoorState.Revealed, hasTuningFork: false));
        }

        [Test]
        public void Aberto_NaoHaMaisNada()
        {
            Assert.AreEqual(SecretDoorAction.None, SecretDoorRules.ActionFor(SecretDoorState.Open, hasTuningFork: true));
        }

        [Test]
        public void AfinarRevela_EmpurrarAbre_ForaDeOrdemNaoMuda()
        {
            Assert.AreEqual(SecretDoorState.Revealed, SecretDoorRules.After(SecretDoorState.Closed, SecretDoorAction.Tune));
            Assert.AreEqual(SecretDoorState.Open, SecretDoorRules.After(SecretDoorState.Revealed, SecretDoorAction.Push));
            Assert.AreEqual(SecretDoorState.Closed, SecretDoorRules.After(SecretDoorState.Closed, SecretDoorAction.Push), "não se empurra o que não se achou");
            Assert.AreEqual(SecretDoorState.Open, SecretDoorRules.After(SecretDoorState.Open, SecretDoorAction.Tune));
        }

        // --- R13: o portal só puxa quem fica sozinho, depois de mexer no cartaz ---

        const float Aviso = 2f, Puxa = 4f, Raio = 6f;

        static PortalTick Passo(bool armado, bool sozinho, float jaSozinho, float dt = 0.5f) =>
            PortalRules.Tick(armado, sozinho, jaSozinho, dt, Aviso, Puxa);

        [Test]
        public void AntesDeMexerNoCartaz_OPortalNaoFazNada()
        {
            var t = Passo(armado: false, sozinho: true, jaSozinho: 10f);
            Assert.IsFalse(t.Warn);
            Assert.IsFalse(t.Pull);
            Assert.AreEqual(0f, t.AloneTime);
        }

        [Test]
        public void Sozinho_AvisaAos2s_UmaVezSo()
        {
            Assert.IsTrue(Passo(true, true, 1.6f).Warn, "passou dos 2 s agora");
            Assert.IsFalse(Passo(true, true, 2.5f).Warn, "o aviso não repete");
        }

        [Test]
        public void Sozinho_PuxaAos4s()
        {
            Assert.IsFalse(Passo(true, true, 3.0f).Pull);
            Assert.IsTrue(Passo(true, true, 3.6f).Pull);
        }

        [Test]
        public void ChegouUmColega_OTempoZera()
        {
            var t = Passo(true, sozinho: false, jaSozinho: 3.9f);
            Assert.AreEqual(0f, t.AloneTime);
            Assert.IsFalse(t.Pull);
        }

        [Test]
        public void Sozinho_EhNinguemA6m()
        {
            var eu = Vector3.zero;
            Assert.IsTrue(PortalRules.IsAlone(eu, new[] { eu, new Vector3(7, 0, 0) }, Raio), "ele mesmo não conta");
            Assert.IsFalse(PortalRules.IsAlone(eu, new[] { eu, new Vector3(5, 0, 0) }, Raio));
        }

        // --- R04: os espelhos-herói refletem primeiro ---

        [Test]
        public void EspelhoHeroi_TemPrioridade_SobreUmComumMaisPerto()
        {
            Assert.Less(MirrorReflection.Priority(9f, hero: true), MirrorReflection.Priority(3f, hero: false));
        }

        [Test]
        public void EntreComuns_GanhaOMaisPerto()
        {
            Assert.Less(MirrorReflection.Priority(3f, hero: false), MirrorReflection.Priority(5f, hero: false));
        }
    }
}
