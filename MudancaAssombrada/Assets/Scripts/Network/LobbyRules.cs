using System.Collections.Generic;

namespace MudancaAssombrada
{
    // As REGRAS da sala de espera, sem rede nenhuma: só recebem a lista de jogadores e respondem
    // "pode?" / "quanto?". O LobbyManager (os RPCs) pergunta aqui antes de mudar qualquer coisa.
    //
    // Por que separado: assim dá para testar cada regra em Edit Mode, sem host nem cliente
    // (ver Assets/Tests/EditMode/LobbyRulesTests.cs). Regra nova da sala = teste novo primeiro.
    public static class LobbyRules
    {
        public const int ChatMaxLength = 120;   // letras por mensagem (o campo do menu também limita)
        public const float ChatCooldown = 0.5f; // anti-flood: intervalo mínimo entre mensagens

        public static int IndexOf(IReadOnlyList<LobbyPlayer> players, ulong clientId)
        {
            for (int i = 0; i < players.Count; i++)
                if (players[i].ClientId == clientId) return i;
            return -1;
        }

        public static bool IsTaken(IReadOnlyList<LobbyPlayer> players, int character)
        {
            foreach (var p in players)
                if (p.Character == character) return true;
            return false;
        }

        // O primeiro personagem livre do Elenco. Mais jogadores que personagens: repete o primeiro.
        public static int FirstFreeCharacter(IReadOnlyList<LobbyPlayer> players, int rosterCount)
        {
            for (int c = 0; c < rosterCount; c++)
                if (!IsTaken(players, c)) return c;
            return 0;
        }

        // Todos prontos? (o líder não precisa marcar: ele é quem começa)
        public static bool AllReady(IReadOnlyList<LobbyPlayer> players, ulong leader)
        {
            foreach (var p in players)
                if (p.ClientId != leader && !p.Ready) return false;
            return true;
        }

        // Trocar de personagem: só quem está na sala, não está pronto, e pede um válido e livre.
        public static bool CanChangeCharacter(IReadOnlyList<LobbyPlayer> players, ulong sender, int wanted, int rosterCount)
        {
            int i = IndexOf(players, sender);
            return i >= 0 && !players[i].Ready && wanted >= 0 && wanted < rosterCount && !IsTaken(players, wanted);
        }

        // Expulsar: só o líder, nunca a si mesmo nem o host (sem o host, a partida acaba para todos),
        // e o alvo precisa estar na sala.
        public static bool CanKick(IReadOnlyList<LobbyPlayer> players, ulong sender, ulong target, ulong leader, ulong host) =>
            sender == leader && target != sender && target != host && IndexOf(players, target) >= 0;

        // Passar a liderança: só o líder, para outro jogador da sala, e não durante a contagem.
        public static bool CanMakeLeader(IReadOnlyList<LobbyPlayer> players, ulong sender, ulong target, ulong leader, bool counting) =>
            sender == leader && target != leader && !counting && IndexOf(players, target) >= 0;

        // Anti-flood: last = hora da última mensagem desse jogador (null = nunca mandou).
        public static bool ChatAllowed(float? last, float now) => last == null || now - last.Value >= ChatCooldown;

        // Limpa o texto do jogador para o chat. null = não manda nada (vazio).
        // Tira o "<" para ninguém conseguir mexer nas cores (rich text) e corta no limite.
        public static string CleanChat(string text)
        {
            if (text == null) return null;
            string clean = text.Replace("<", "‹").Trim();
            if (clean.Length == 0) return null;
            if (clean.Length > ChatMaxLength) clean = clean.Substring(0, ChatMaxLength);
            return clean;
        }
    }
}
