using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MudancaAssombrada
{
    // O que fica salvo entre uma sessão e outra: o PROGRESSO NA HISTÓRIA.
    // (Configurações como volume ficam no GameSettings, separadas.)
    //
    // [Serializable] deixa a Unity transformar isto em texto JSON e de volta.
    [Serializable]
    public class SaveData
    {
        public int version = 1;                                  // para migrar saves antigos no futuro
        public List<string> completedChapters = new();           // ids dos capítulos concluídos
        public int money;                                        // dinheiro da Transportes Descanso Eterno
        public List<string> storyFlags = new();                  // marcadores da história (pistas, escolhas...)

        public bool IsCompleted(string chapterId) => completedChapters.Contains(chapterId);

        // Marcadores: "achou o cartaz do teatro", "esqueceu o diário"... O final alternativo
        // do roteiro ("Volte para buscar o resto") depende de marcadores assim.
        // Marca o capítulo como concluído e paga. Devolve false se já estava (repetir não paga de novo).
        public bool Complete(ChapterDefinition chapter)
        {
            if (chapter == null || IsCompleted(chapter.id)) return false;
            completedChapters.Add(chapter.id);
            money += chapter.payment;
            return true;
        }

        public bool HasFlag(string flag) => storyFlags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!storyFlags.Contains(flag)) storyFlags.Add(flag);
        }
    }

    // Lê e grava o SaveData num arquivo. Cada jogador tem o seu save no próprio computador;
    // numa partida, quem decide os capítulos liberados é o save do HOST.
    public static class SaveSystem
    {
        // persistentDataPath = a pasta "de dados" do jogo no computador
        // (no Windows: AppData/LocalLow/<empresa>/<jogo>).
        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        static SaveData data;

        public static SaveData Data
        {
            get
            {
                if (data == null) data = Load();
                return data;
            }
        }

        static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath)) return Parse(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                // Save corrompido não pode travar o jogo: avisa e começa do zero.
                Debug.LogWarning($"Não deu para ler o save ({e.Message}). Começando um novo.");
            }
            return new SaveData();
        }

        // Texto do arquivo -> SaveData. Separado do disco para os testes conferirem saves antigos
        // e corrompidos. Texto inválido lança exceção (o Load avisa e começa do zero).
        public static SaveData Parse(string json)
        {
            var save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            // Save antigo, sem um dos campos: a lista vem nula, e o jogo espera uma lista vazia.
            save.completedChapters ??= new List<string>();
            save.storyFlags ??= new List<string>();
            return save;
        }

        // Lê o texto sem lançar exceção: corrompido ou vazio = save novo.
        public static SaveData ParseOrNew(string json)
        {
            try { return string.IsNullOrWhiteSpace(json) ? new SaveData() : Parse(json); }
            catch (Exception) { return new SaveData(); }
        }

        public static void Save()
        {
            // "true" = JSON formatado, fácil de ler e conferir à mão.
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }

        // Chamado em TODOS os computadores quando a equipe conclui um capítulo.
        public static void CompleteChapter(ChapterDefinition chapter)
        {
            if (Data.Complete(chapter)) Save();
        }

        // Apaga o progresso (útil para testar a campanha do começo).
        public static void ResetProgress()
        {
            data = new SaveData();
            Save();
        }

        // Com o "Play rápido" do editor, valores static sobrevivem entre um Play e outro:
        // relê o arquivo no começo de cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => data = null;
    }
}
