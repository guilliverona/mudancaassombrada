using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MudancaAssombrada
{
    // Uma sala aberta, como a tela "Entrar numa sala" mostra. Montada pelo NetworkSession a partir
    // da lista do Lobby da Unity: aqui não há nada da Unity, para os testes rodarem sem rede.
    public struct RoomListing
    {
        public string Id;
        public string Name;
        public string Leader;      // nome do líder ("" se o host ainda não publicou)
        public int Chapter;        // posição na Campanha (-1 = não informado)
        public int[] Characters;   // personagens já escolhidos (posições no Elenco)
        public int Players, MaxPlayers;

        public bool IsFull => Players >= MaxPlayers;
    }

    // As regras da tela "Entrar numa sala" (o MainMenu só chama estas funções).
    public static class RoomBrowserRules
    {
        // Chaves das informações que o host publica na sala (propriedades públicas do Lobby).
        public const string LeaderKey = "lider";
        public const string ChapterKey = "capitulo";
        public const string CharactersKey = "personagens";

        // As salas que batem com a busca (nome da sala ou do líder, sem ligar para acento e
        // maiúscula), com as que têm vaga antes das cheias. Fora isso, a ordem do servidor.
        public static List<RoomListing> Filter(IReadOnlyList<RoomListing> rooms, string query)
        {
            string wanted = Fold(query).Trim();
            var open = new List<RoomListing>();
            var full = new List<RoomListing>();
            foreach (var room in rooms)
            {
                if (wanted.Length > 0 && !Fold(room.Name).Contains(wanted) && !Fold(room.Leader).Contains(wanted)) continue;
                (room.IsFull ? full : open).Add(room);
            }
            open.AddRange(full);
            return open;
        }

        // Quantas páginas (sempre pelo menos 1, mesmo sem salas: "Página 1 / 1").
        public static int PageCount(int count, int perPage) => count <= 0 ? 1 : (count + perPage - 1) / perPage;

        // Mantém a página dentro do que existe (a lista encolhe com a busca ou quando salas fecham).
        public static int ClampPage(int page, int count, int perPage) => Math.Clamp(page, 0, PageCount(count, perPage) - 1);

        public static List<RoomListing> Page(IReadOnlyList<RoomListing> rooms, int page, int perPage)
        {
            var result = new List<RoomListing>();
            for (int i = page * perPage; i < rooms.Count && i < (page + 1) * perPage; i++)
                if (i >= 0) result.Add(rooms[i]);
            return result;
        }

        // Personagens escolhidos <-> texto ("0,2"): as propriedades da sala só guardam texto.
        public static string EncodeCharacters(IEnumerable<int> characters) => string.Join(",", characters);

        public static int[] DecodeCharacters(string text)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(text)) return result.ToArray();
            foreach (var part in text.Split(','))
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= 0)
                    result.Add(value);
            return result.ToArray();
        }

        public static int ParseChapter(string text) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= 0 ? value : -1;

        // O código como o Lobby espera: só letras e números, em maiúsculas.
        // Assim dá para colar "Código: K7Q-2XM" do chat e funcionar.
        public static string CleanCode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text.ToUpperInvariant())
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
            string code = sb.ToString();
            // "CÓDIGO: K7Q2XM" vira "CDIGOK7Q2XM": se colaram com o rótulo, fica só o fim.
            return code.Length > MaxCodeLength ? code.Substring(code.Length - MaxCodeLength) : code;
        }

        public const int MaxCodeLength = 6;

        // Minúsculas e sem acento ("João" -> "joao"), para comparar na busca.
        static string Fold(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
    }
}
