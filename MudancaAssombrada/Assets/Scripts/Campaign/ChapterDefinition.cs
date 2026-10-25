using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // A "ficha" de um capítulo da história: uma casa, um fantasma, um serviço.
    // Os OBJETIVOS ficam na própria cena (componentes Objective); aqui ficam os dados
    // que valem antes, durante e depois da fase: nome, bilhete do chefe, tempo, avaliação.
    //
    // Capítulo novo: Assets/Data/Capítulos > botão direito > Create > Mudança Assombrada > Capítulo,
    // e depois arrastar para a lista da Campanha (Assets/Resources/Campanha.asset).
    [CreateAssetMenu(menuName = "Mudança Assombrada/Capítulo", fileName = "Capítulo")]
    public class ChapterDefinition : ScriptableObject
    {
        // Comentário do Seu Valdemar para UM personagem na ficha de avaliação.
        [Serializable]
        public struct CharacterComment
        {
            public CharacterDefinition character;
            [TextArea] public string comment;
        }

        [Header("Identidade")]
        [Tooltip("Nunca mude depois de lançar: é assim que o save lembra o que foi concluído.")]
        public string id = "capitulo";
        public string title = "Novo capítulo";
        [Tooltip("O fantasma da casa, para o menu e o bilhete.")]
        public string ghostName = "";
        [Tooltip("Personagem que brilha nesta fase (o roteiro dá um para cada casa).")]
        public CharacterDefinition featuredCharacter;

        [Header("Cena")]
        [Tooltip("Nome da cena da fase (sem .unity). Vazio = capítulo ainda em produção.")]
        public string sceneName = "";
        [Tooltip("Sempre disponível, sem precisar terminar o anterior (para testes).")]
        public bool alwaysUnlocked;

        [Header("Partida")]
        [Tooltip("Segundos até amanhecer. 0 = sem relógio (ex.: a fase final).")]
        [Min(0f)] public float nightDuration = 180f;
        [Tooltip("Quanto a empresa recebe ao concluir (para reformar o caminhão e comprar equipamentos).")]
        [Min(0)] public int payment = 100;

        [Header("Seu Valdemar")]
        [Tooltip("O bilhete que aparece no começo da fase.")]
        [TextArea(3, 8)] public string openingNote = "";
        [Tooltip("Ficha de avaliação: um comentário para cada personagem.")]
        public CharacterComment[] ratingComments = new CharacterComment[0];

        public bool IsPlayable => !string.IsNullOrEmpty(sceneName);

        // O comentário do chefe para este personagem (null = não tem).
        public string CommentFor(CharacterDefinition character)
        {
            foreach (var c in ratingComments)
                if (c.character == character) return c.comment;
            return null;
        }
    }
}
