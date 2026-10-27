using System.Collections.Generic;
using NUnit.Framework;

namespace MudancaAssombrada.Tests
{
    // Regras da sala de espera (ARQUITETURA.md, "Uma regra na sala de espera").
    // Cliente 0 = host. O líder começa sendo o host, mas pode ser qualquer um.
    public class LobbyRulesTests
    {
        const ulong Host = 0;
        const int Roster = 3; // Cida, Tiago, Marcelo

        static LobbyPlayer P(ulong id, int character, bool ready = false) =>
            new LobbyPlayer { ClientId = id, Character = character, Ready = ready };

        static List<LobbyPlayer> Room(params LobbyPlayer[] players) => new(players);

        // --- Pronto ---

        [Test]
        public void LiderNaoPrecisaMarcarPronto()
        {
            var room = Room(P(Host, 0), P(1, 1, ready: true));
            Assert.IsTrue(LobbyRules.AllReady(room, leader: Host));
        }

        [Test]
        public void UmConvidadoNaoPronto_SeguraAPartida()
        {
            var room = Room(P(Host, 0), P(1, 1, ready: true), P(2, 2));
            Assert.IsFalse(LobbyRules.AllReady(room, leader: Host));
        }

        [Test]
        public void LiderQueNaoEOHost_OHostPrecisaMarcarPronto()
        {
            var room = Room(P(Host, 0), P(1, 1));
            Assert.IsFalse(LobbyRules.AllReady(room, leader: 1));
        }

        // --- Personagens ---

        [Test]
        public void PersonagemLivre_EOPrimeiroQueNinguemPegou()
        {
            Assert.AreEqual(0, LobbyRules.FirstFreeCharacter(Room(), Roster));
            Assert.AreEqual(2, LobbyRules.FirstFreeCharacter(Room(P(Host, 0), P(1, 1)), Roster));
        }

        [Test]
        public void SalaCheiaDePersonagens_RepeteOPrimeiro()
        {
            var room = Room(P(Host, 0), P(1, 1), P(2, 2));
            Assert.AreEqual(0, LobbyRules.FirstFreeCharacter(room, Roster));
        }

        [Test]
        public void TrocarPersonagem_SoParaUmLivreEValido()
        {
            var room = Room(P(Host, 0), P(1, 1));
            Assert.IsTrue(LobbyRules.CanChangeCharacter(room, 1, 2, Roster));
            Assert.IsFalse(LobbyRules.CanChangeCharacter(room, 1, 0, Roster), "ocupado");
            Assert.IsFalse(LobbyRules.CanChangeCharacter(room, 1, 3, Roster), "fora do Elenco");
            Assert.IsFalse(LobbyRules.CanChangeCharacter(room, 1, -1, Roster), "negativo");
            Assert.IsFalse(LobbyRules.CanChangeCharacter(room, 9, 2, Roster), "não está na sala");
        }

        [Test]
        public void QuemEstaPronto_NaoTrocaDePersonagem()
        {
            var room = Room(P(Host, 0), P(1, 1, ready: true));
            Assert.IsFalse(LobbyRules.CanChangeCharacter(room, 1, 2, Roster));
        }

        // --- Expulsar ---

        [Test]
        public void LiderExpulsaConvidado()
        {
            var room = Room(P(Host, 0), P(1, 1));
            Assert.IsTrue(LobbyRules.CanKick(room, Host, 1, leader: Host, host: Host));
        }

        [Test]
        public void NinguemExpulsaOHost_NemASiMesmo()
        {
            var room = Room(P(Host, 0), P(1, 1), P(2, 2));
            Assert.IsFalse(LobbyRules.CanKick(room, 1, Host, leader: 1, host: Host), "o host");
            Assert.IsFalse(LobbyRules.CanKick(room, 1, 1, leader: 1, host: Host), "a si mesmo");
        }

        [Test]
        public void SoOLiderExpulsa()
        {
            var room = Room(P(Host, 0), P(1, 1), P(2, 2));
            Assert.IsFalse(LobbyRules.CanKick(room, 1, 2, leader: Host, host: Host));
        }

        [Test]
        public void ExpulsarQuemNaoEstaNaSala_NaoFazNada()
        {
            var room = Room(P(Host, 0));
            Assert.IsFalse(LobbyRules.CanKick(room, Host, 7, leader: Host, host: Host));
        }

        // --- Liderança ---

        [Test]
        public void LiderPassaALideranca()
        {
            var room = Room(P(Host, 0), P(1, 1));
            Assert.IsTrue(LobbyRules.CanMakeLeader(room, Host, 1, leader: Host, counting: false));
        }

        [Test]
        public void NaoPassaLiderancaDuranteAContagem_NemSemSerLider()
        {
            var room = Room(P(Host, 0), P(1, 1), P(2, 2));
            Assert.IsFalse(LobbyRules.CanMakeLeader(room, Host, 1, leader: Host, counting: true), "contagem");
            Assert.IsFalse(LobbyRules.CanMakeLeader(room, 1, 2, leader: Host, counting: false), "não é líder");
            Assert.IsFalse(LobbyRules.CanMakeLeader(room, Host, Host, leader: Host, counting: false), "já é líder");
            Assert.IsFalse(LobbyRules.CanMakeLeader(room, Host, 9, leader: Host, counting: false), "fora da sala");
        }

        // --- Chat ---

        [Test]
        public void Chat_PrimeiraMensagemSemprePassa_DepoisRespeitaOIntervalo()
        {
            Assert.IsTrue(LobbyRules.ChatAllowed(null, 0f));
            Assert.IsFalse(LobbyRules.ChatAllowed(10f, 10.2f));
            Assert.IsTrue(LobbyRules.ChatAllowed(10f, 10f + LobbyRules.ChatCooldown));
        }

        [Test]
        public void Chat_NaoDeixaUsarRichText()
        {
            Assert.AreEqual("‹color=red>oi", LobbyRules.CleanChat("<color=red>oi"));
        }

        [Test]
        public void Chat_VazioOuSoEspacos_NaoEnvia()
        {
            Assert.IsNull(LobbyRules.CleanChat("   "));
            Assert.IsNull(LobbyRules.CleanChat(""));
            Assert.IsNull(LobbyRules.CleanChat(null));
        }

        [Test]
        public void Chat_CortaNoLimite()
        {
            string longo = new string('a', LobbyRules.ChatMaxLength + 30);
            Assert.AreEqual(LobbyRules.ChatMaxLength, LobbyRules.CleanChat(longo).Length);
        }
    }
}
