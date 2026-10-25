using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MudancaAssombrada.Editor
{
    // Evita "falsas mudanças" no Git quando um construtor RECRIA uma cena ou prefab.
    //
    // O PROBLEMA: cada objeto dentro de um .unity/.prefab tem um número de identidade (fileID),
    // e a Unity SORTEIA esses números quando o objeto é criado. Recriar o Menu do zero, mesmo
    // idêntico, troca todos os números: o Git vê milhares de linhas mudadas, e duas pessoas que
    // recriaram o mesmo arquivo em branches diferentes têm um conflito impossível de resolver.
    //
    // A SOLUÇÃO: antes de regravar, guardamos o arquivo antigo (Snapshot). Depois de regravar,
    // comparamos os dois IGNORANDO os números sorteados (RestoreIfSame). Se só os números mudaram,
    // devolvemos o arquivo antigo, byte a byte: para o Git, nada aconteceu.
    // Se algo mudou de verdade (um botão novo, uma cor), o arquivo novo fica.
    public static class StableAssets
    {
        // O texto atual do arquivo (null = ainda não existe). Caminho do tipo "Assets/...".
        public static string Snapshot(string assetPath) =>
            File.Exists(assetPath) ? File.ReadAllText(assetPath) : null;

        // Para um trabalho que regrava VÁRIOS arquivos em passos (criar o prefab, depois pôr o
        // modelo nele...): guarda todos antes, roda o trabalho e confere cada um no fim.
        public static void Guard(System.Collections.Generic.IEnumerable<string> assetPaths, System.Action work)
        {
            var before = new Dictionary<string, string>();
            foreach (var path in assetPaths) before[path] = Snapshot(path);
            work();
            AssetDatabase.SaveAssets(); // garante que tudo foi para o disco antes de comparar
            foreach (var pair in before) RestoreIfSame(pair.Key, pair.Value);
        }

        // true = só os números mudaram, e o arquivo antigo foi devolvido.
        public static bool RestoreIfSame(string assetPath, string before)
        {
            if (before == null || !File.Exists(assetPath)) return false;
            string after = File.ReadAllText(assetPath);
            if (after == before) return true;
            if (Canonical(after) != Canonical(before)) return false;

            File.WriteAllText(assetPath, before);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        // ---------- Comparar sem os números sorteados ----------
        // Um .unity/.prefab é uma lista de "documentos", um por objeto/componente:
        //   --- !u!1 &115661111        <- tipo (1 = GameObject) e o número sorteado
        //   GameObject: ...            <- o conteúdo, que aponta para outros por {fileID: N}
        // Para comparar, cada documento vira uma "assinatura": o seu conteúdo, com cada referência
        // interna trocada pela assinatura de QUEM ela aponta (algumas rodadas, para descer na
        // hierarquia). Assim um objeto é descrito pelo que ele é e com quem se liga, nunca pelo
        // número sorteado. Depois ordenamos as assinaturas e comparamos as listas.
        // Referências a OUTROS arquivos ({fileID: N, guid: ...}) não são sorteadas e ficam como estão.

        static readonly Regex Header = new(@"^!u!(\d+) &(-?\d+)(.*)$");
        static readonly Regex LocalRef = new(@"\{fileID: (-?\d+)\}");
        // Números que a Unity/o Netcode calculam A PARTIR dos fileIDs: mudam junto, então ignoramos.
        // Aparecem de dois jeitos: direto ("GlobalObjectIdHash: 123") ou como ajuste de uma cópia de
        // prefab na cena ("propertyPath: GlobalObjectIdHash" e, na linha de baixo, "value: 123").
        static readonly Regex Derived = new(@"((?:GlobalObjectIdHash|InScenePlacedSourceGlobalObjectIdHash)(?::|\n\s+value:)) \d+");
        const int Rounds = 8; // quantos "níveis" de ligação entram na assinatura

        static string Canonical(string text)
        {
            text = text.Replace("\r\n", "\n");
            string[] parts = Regex.Split(text, @"(?m)^--- ");
            var ids = new List<string>();
            var bodies = new List<string>(); // tipo + conteúdo (sem os números derivados)
            for (int i = 1; i < parts.Length; i++)
            {
                int newline = parts[i].IndexOf('\n');
                string head = newline < 0 ? parts[i] : parts[i].Substring(0, newline);
                var m = Header.Match(head);
                if (!m.Success) return text; // formato inesperado: compara o texto puro
                string body = newline < 0 ? "" : parts[i].Substring(newline + 1);
                ids.Add(m.Groups[2].Value);
                bodies.Add(m.Groups[1].Value + m.Groups[3].Value + "\n" + Derived.Replace(body, "$1 #"));
            }

            // Rodada 0: ninguém sabe nada dos vizinhos. A cada rodada, cada um "pergunta" aos vizinhos.
            var signature = new Dictionary<string, string>();
            foreach (var id in ids) signature[id] = "";
            var keys = new string[ids.Count];
            for (int round = 0; round <= Rounds; round++)
            {
                for (int i = 0; i < ids.Count; i++)
                    keys[i] = LocalRef.Replace(bodies[i], r =>
                        signature.TryGetValue(r.Groups[1].Value, out var s) ? "{fileID: @" + s + "}" : r.Value);
                for (int i = 0; i < ids.Count; i++) signature[ids[i]] = Hash(keys[i]);
            }

            var sorted = new List<string>(keys);
            sorted.Sort(string.CompareOrdinal);
            return parts[0] + string.Join("\n---\n", sorted);
        }

        // Um "resumo" curto e estável de um texto (o mesmo texto dá sempre o mesmo resumo).
        static string Hash(string s)
        {
            using var sha = System.Security.Cryptography.SHA1.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return System.BitConverter.ToString(bytes, 0, 8).Replace("-", "");
        }    }
}
