using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // A HUD da partida: TUDO que aparece na tela durante o jogo fica aqui, num lugar só.
    //
    // Ela não sabe as regras do jogo: só LÊ o estado de quem sabe e desenha.
    //   - GameManager.Instance: cronômetro, móveis entregues, resultado
    //   - NetworkPlayer.Local: estamina, dica de interação, personagem
    // Assim, mudar o visual nunca quebra uma regra, e mudar uma regra não exige mexer na tela.
    // As peças (textos, barras) são criadas pelo HudBuilder (editor) e ligadas aqui pelo Inspector.
    public class Hud : MonoBehaviour
    {
        [Header("Partida")]
        [SerializeField] Text timerText;
        [SerializeField] Text deliveredText;
        [SerializeField] GameObject endScreen;
        [SerializeField] Text endText;

        [Header("Jogador")]
        [SerializeField] GameObject crosshair;
        [SerializeField] Text characterText;
        [SerializeField] GameObject staminaBar;
        [SerializeField] RectTransform staminaFill;
        [SerializeField] Image staminaFillImage;
        [SerializeField] GameObject hint;
        [SerializeField] Text hintKey;
        [SerializeField] Text hintAction;

        [Header("Regras de exibição")]
        [Tooltip("Abaixo disto (em segundos), o cronômetro fica na cor de perigo.")]
        [SerializeField] float dangerTime = 30f;

        // StringBuilder: monta textos grandes sem criar um monte de pedaços de texto no caminho.
        readonly StringBuilder sb = new();

        void Update()
        {
            UpdateMatch(GameManager.Instance);
            UpdatePlayer(NetworkPlayer.Local);
        }

        // ---------- Partida ----------

        void UpdateMatch(GameManager game)
        {
            bool inMatch = game != null && game.IsSpawned;
            timerText.gameObject.SetActive(inMatch);
            deliveredText.gameObject.SetActive(inMatch);
            endScreen.SetActive(inMatch && game.Outcome != MatchOutcome.Playing);
            if (!inMatch) return;

            int minutes = (int)(game.TimeLeft / 60);
            int seconds = (int)(game.TimeLeft % 60);
            // {1:00} = sempre 2 dígitos (5 vira 05)
            SetText(timerText, string.Format("Amanhecer em {0}:{1:00}", minutes, seconds));
            timerText.color = game.TimeLeft <= dangerTime ? Theme.DangerColor : Theme.TextColor;

            SetText(deliveredText, $"Móveis no caminhão: {game.Delivered}/{game.TotalFurniture}");

            if (endScreen.activeSelf) SetText(endText, EndMessage(game));
        }

        string EndMessage(GameManager game)
        {
            string restart = $"\n\n<size={Theme.BodySize}>Aperte {GameInput.KeyLabel(GameInput.Restart)} para jogar de novo</size>";
            return game.Outcome switch
            {
                MatchOutcome.Won => Colored("Mudança concluída!", Theme.DawnColor) + "\n\n" + Rating(game.Stars) + restart,
                MatchOutcome.Dawn => Colored("Amanheceu...", Theme.DawnColor) + "\nO serviço ficou pela metade." + restart,
                _ => Colored("O fantasma pegou alguém da equipe!", Theme.DangerColor) + restart,
            };
        }

        // A "ficha de avaliação" do Seu Valdemar, como no roteiro. Mais tempo sobrando = mais estrelas.
        static string Rating(int stars)
        {
            string comment = stars switch
            {
                5 => "\"Nem parecia uma casa assombrada. Contrato renovado!\"",
                4 => "\"Ótimo serviço. Só não me arranhem o armário de novo.\"",
                3 => "\"Deu certo. Mas eu ouvi a gritaria daqui do escritório.\"",
                2 => "\"Quase amanheceu. O cliente reclamou do barulho.\"",
                _ => "\"No limite! Vou descontar a hora extra do salário.\"",
            };
            // new string('★', 3) = "★★★"
            return new string('★', stars) + new string('☆', 5 - stars) + "\n" + comment + "\n— Seu Valdemar";
        }

        // ---------- Jogador local ----------

        void UpdatePlayer(NetworkPlayer player)
        {
            bool hasPlayer = player != null;
            crosshair.SetActive(hasPlayer && !PauseMenu.IsOpen);
            characterText.gameObject.SetActive(hasPlayer);
            if (!hasPlayer)
            {
                staminaBar.SetActive(false);
                hint.SetActive(false);
                return;
            }

            UpdateStamina(player.Movement);
            UpdateHint(player.Carrier.Prompt);
            UpdateCharacter(player.Character);
        }

        void UpdateStamina(PlayerMovement movement)
        {
            float amount = movement.Stamina01;
            staminaBar.SetActive(amount < 1f); // cheia: some (menos coisa na tela)
            // A barra "enche" mudando onde termina: anchorMax.x vai de 0 (vazia) a 1 (cheia).
            staminaFill.anchorMax = new Vector2(amount, 1f);
            staminaFillImage.color = movement.IsExhausted ? Theme.DangerColor : Theme.TextColor;
        }

        void UpdateHint(InteractionPrompt prompt)
        {
            hint.SetActive(prompt.Visible && !PauseMenu.IsOpen);
            if (!prompt.Visible) return;

            SetText(hintKey, prompt.Available ? GameInput.KeyLabel(GameInput.Interact) : "—");
            SetText(hintAction, prompt.Action);
            Color color = prompt.Available ? Theme.HighlightColor : Theme.MutedColor;
            hintKey.color = color;
            hintAction.color = color;
        }

        void UpdateCharacter(CharacterSwitcher switcher)
        {
            var roster = CharacterRoster.Instance;
            var me = switcher.Current;

            sb.Clear();
            sb.Append("Você: <b>").Append(me.FullName).Append("</b>");
            // As teclas das habilidades vêm do GameInput: se a tecla mudar lá, a dica muda sozinha.
            if (me.canTap) sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Tap)).Append("] Bengala");
            if (me.whistle) sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Whistle)).Append("] Apito");

            // A lista de personagens, marcando quem está com quem.
            sb.Append('\n');
            for (int i = 0; i < roster.Count && i < GameInput.SelectCharacter.Length; i++)
            {
                var c = roster.Get(i);
                string status = i == switcher.CurrentIndex ? " (você)"
                              : CharacterSwitcher.IsTaken(i, switcher) ? " (ocupado)" : "";
                sb.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(Theme.Muted(c.color))).Append('>')
                  .Append('[').Append(i + 1).Append("] ").Append(c.FullName).Append("</color>").Append(status);
            }
            SetText(characterText, sb.ToString());
        }

        // ---------- Ajudantes ----------

        // Só troca o texto se mudou: trocar o texto de um Text faz a Unity redesenhar ele.
        static void SetText(Text text, string value)
        {
            if (text.text != value) text.text = value;
        }

        static string Colored(string text, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
