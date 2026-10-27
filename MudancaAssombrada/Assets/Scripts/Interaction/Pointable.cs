using UnityEngine;

namespace MudancaAssombrada
{
    // "Esse aí é falso!": dá para APONTAR para quem tem este componente. Fica nos jogadores
    // e nos fantasmas. Só aparece enquanto existe um fantasma disfarçado na casa, e a dica é
    // IGUAL para o amigo de verdade e para a cópia: a tela não entrega quem é o falso.
    //
    // Apontar também AVISA a equipe (GameCue): quem vê, vê o gesto; quem ouve, ouve o grito.
    // É assim que a Cida "guia a equipe", como no roteiro.
    public class Pointable : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] float cueRange = 12f;

        Ghost ghost;                    // se este objeto for um fantasma
        CharacterSwitcher character;    // se for um jogador
        PlayerCondition condition;      // idem

        void Awake()
        {
            ghost = GetComponent<Ghost>();
            character = GetComponent<CharacterSwitcher>();
            condition = GetComponent<PlayerCondition>();
        }

        public InteractionPrompt PromptFor(PlayerInteractor player)
        {
            if (ghost != null)
            {
                // Fantasma: disfarçado ("é falso!") ou parado na reverência, à mostra ("é ele!").
                if (!ghost.CanBeUnmasked) return InteractionPrompt.None;
                return new InteractionPrompt(ghost.IsDisguised ? "Apontar: é falso!" : "Apontar: é ele!", true);
            }
            if (!Ghost.AnyDisguised) return InteractionPrompt.None;
            // Colega sendo puxado: a tecla é para SEGURAR ele (PlayerCondition), não para apontar.
            if (condition != null && condition.IsPulled) return InteractionPrompt.None;
            return new InteractionPrompt("Apontar: é falso!", true);
        }

        public void Interact(PlayerInteractor player)
        {
            string who = player.Character.displayName;
            // Quem aponta é o dono do PRÓPRIO boneco: o aviso sai dele (SoundRelay leva aos outros).
            string text = ghost != null && !ghost.IsDisguised
                ? $"{who} aponta para o fantasma: é ele!"
                : $"{who} aponta para \"{(ghost != null ? DisguiseName() : character != null ? character.Current.displayName : name)}\": é falso!";
            GameCues.Emit(new GameCue(player.transform.position, cueRange, Senses.Sight | Senses.Hearing, text, player.gameObject));

            if (ghost != null && ghost.IsSpawned) ghost.RequestUnmask();
        }

        string DisguiseName()
        {
            var roster = CharacterRoster.Instance;
            return roster != null && roster.IsValid(ghost.Disguise) ? roster.Get(ghost.Disguise).displayName : name;
        }
    }
}
