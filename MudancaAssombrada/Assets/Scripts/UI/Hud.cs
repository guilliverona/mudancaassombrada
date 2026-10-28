using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // A HUD da partida: TUDO que aparece na tela durante o jogo fica aqui, num lugar só.
    //
    // Ela não sabe as regras do jogo: só LÊ o estado de quem sabe e desenha.
    //   - GameManager.Instance: relógio, objetivos, resultado, capítulo
    //   - NetworkPlayer.Local: estamina, dica de interação, personagem
    // Assim, mudar o visual nunca quebra uma regra, e mudar uma regra não exige mexer na tela.
    // As peças (textos, barras) são criadas pelo HudBuilder (editor) e ligadas aqui pelo Inspector.
    public class Hud : MonoBehaviour
    {
        [Header("Partida")]
        [SerializeField] Text timerText;
        [SerializeField] Text objectivesText;
        [SerializeField] GameObject endScreen;
        [SerializeField] Text endText;

        [Header("Bilhete do Seu Valdemar (começo da fase)")]
        [SerializeField] GameObject note;
        [SerializeField] Text noteText;
        [SerializeField] float noteDuration = 10f; // segundos na tela

        [Header("Jogador")]
        [SerializeField] GameObject crosshair;
        [SerializeField] Text characterText;
        [SerializeField] GameObject staminaBar;
        [SerializeField] RectTransform staminaFill;
        [SerializeField] Image staminaFillImage;
        [SerializeField] GameObject hint;
        [SerializeField] Text hintKey;
        [SerializeField] Text hintAction;
        [Tooltip("Os acontecimentos que VOCÊ percebeu (CueListener), com o sentido por onde chegaram.")]
        [SerializeField] Text cuesText;

        [Header("Hotbar (embaixo, no meio)")]
        [Tooltip("A barra inteira: aparece quando algo muda e some sozinha (a HUD é mínima).")]
        [SerializeField] CanvasGroup hotbar;
        [SerializeField] Image[] hotbarSlots;
        [SerializeField] Text[] hotbarLabels;
        [Tooltip("Segundos na tela depois da última troca de item.")]
        [SerializeField] float hotbarShowSeconds = 3f;

        [Header("Regras de exibição")]
        [Tooltip("Abaixo disto (em segundos), o relógio fica na cor de perigo.")]
        [SerializeField] float dangerTime = 30f;

        // StringBuilder: monta textos grandes sem criar um monte de pedaços de texto no caminho.
        readonly StringBuilder sb = new();
        float noteHideTime = -1f; // < 0 = o bilhete ainda não apareceu nesta partida

        void Update()
        {
            UpdateMatch(GameManager.Instance);
            UpdatePlayer(NetworkPlayer.Local);
        }

        // ---------- Partida ----------

        void UpdateMatch(GameManager game)
        {
            bool inMatch = game != null && game.IsSpawned;
            timerText.gameObject.SetActive(inMatch && game.HasClock);
            objectivesText.gameObject.SetActive(inMatch);
            endScreen.SetActive(inMatch && game.Outcome != MatchOutcome.Playing);
            UpdateNote(inMatch ? game : null);
            if (!inMatch) return;

            if (game.HasClock)
            {
                int minutes = (int)(game.TimeLeft / 60);
                int seconds = (int)(game.TimeLeft % 60);
                // {1:00} = sempre 2 dígitos (5 vira 05)
                SetText(timerText, string.Format("Amanhecer em {0}:{1:00}", minutes, seconds));
                timerText.color = game.TimeLeft <= dangerTime ? Theme.DangerColor : Theme.TextColor;
            }

            SetText(objectivesText, game.ObjectiveText);

            if (endScreen.activeSelf) SetText(endText, EndMessage(game));
        }

        // O bilhete aparece uma vez, quando a partida começa, e some sozinho.
        void UpdateNote(GameManager game)
        {
            var chapter = game != null ? game.Chapter : null;
            if (chapter == null || string.IsNullOrEmpty(chapter.openingNote))
            {
                note.SetActive(false);
                return;
            }

            // unscaledTime: o relógio "de verdade", que continua andando mesmo com o jogo congelado.
            if (noteHideTime < 0)
            {
                noteHideTime = Time.unscaledTime + noteDuration;
                SetText(noteText, $"<size={Theme.HeaderSize}>{chapter.title}</size>\n\n" +
                                  $"\"{chapter.openingNote}\"\n— Seu Valdemar");
            }
            note.SetActive(Time.unscaledTime < noteHideTime && game.Outcome == MatchOutcome.Playing);
        }

        string EndMessage(GameManager game)
        {
            string restart = $"\n\n<size={Theme.NoteSize}>Aperte {GameInput.KeyLabel(GameInput.Restart)} para jogar de novo</size>";
            return game.Outcome switch
            {
                MatchOutcome.Won => Colored("Mudança concluída!", Theme.DawnColor) + "\n\n" + RatingCard(game) + restart,
                MatchOutcome.Dawn => Colored("Amanheceu...", Theme.DawnColor) + "\nO serviço ficou pela metade." + restart,
                _ => Colored("O fantasma pegou a equipe inteira!", Theme.DangerColor) + restart,
            };
        }

        // A ficha de avaliação do Seu Valdemar, como no roteiro: as estrelas da equipe e
        // um comentário diferente para CADA personagem que jogou.
        string RatingCard(GameManager game)
        {
            int stars = game.Stars;
            sb.Clear();
            sb.Append(new string('★', stars)).Append(new string('☆', 5 - stars)); // "★★★☆☆"
            sb.Append($"\n<size={Theme.BodySize}>");
            foreach (var player in NetworkPlayer.All)
            {
                var character = player.Character.Current;
                string comment = game.Chapter != null ? game.Chapter.CommentFor(character) : null;
                if (string.IsNullOrEmpty(comment)) continue;
                sb.Append('\n').Append(Colored(character.displayName, Theme.Muted(character.color)))
                  .Append(": \"").Append(comment).Append('"');
            }
            AppendFinds();
            if (game.Chapter != null) sb.Append($"\n\nPagamento: R$ {game.Pay}");
            sb.Append("\n— Seu Valdemar</size>");
            return sb.ToString();
        }

        // Os achados de época entregues (D10), com o comentário do chefe, e as pancadas nos frágeis (D13).
        void AppendFinds()
        {
            int count = 0, extra = 0;
            foreach (var f in PeriodFind.All)
            {
                if (!f.Delivered) continue;
                count++;
                extra += f.Value;
                if (count <= 3 && !string.IsNullOrEmpty(f.Comment)) sb.Append($"\n{f.DisplayName}: \"{f.Comment}\"");
            }
            if (count > 0) sb.Append($"\nAchados de época: {count} (+R$ {extra})");
            int hits = 0;
            foreach (var f in Fragile.All) hits += f.Hits;
            if (hits > 0) sb.Append($"\nFrágeis batidos: {hits} (−R$ {hits * Fragile.DiscountPerHit})");
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
                if (cuesText != null) cuesText.gameObject.SetActive(false);
                UpdateHotbar(null);
                return;
            }

            UpdateStamina(player.Movement);
            // Caído ou sendo puxado, a dica do centro vira o aviso (apagado: não há o que apertar).
            // Puxado, a contagem: quantos segundos faltam para o espelho levar.
            var condition = player.Condition;
            InteractionPrompt prompt = player.Interactor != null ? player.Interactor.Prompt : InteractionPrompt.None;
            if (condition != null && condition.IsDown) prompt = new InteractionPrompt("Você caiu! Espere um colega te ajudar.", false);
            else if (condition != null && condition.IsPulled && !prompt.Visible)
                prompt = new InteractionPrompt(condition.IsAnchored
                    ? "Seguraram você! Aguente firme..."
                    : $"Puxado para o espelho! {Mathf.CeilToInt(condition.PullLeft)} s: alguém precisa te segurar ou apontar o falso", false);
            UpdateHint(prompt);
            UpdateCharacter(player.Character);
            UpdateCues(player.Cues);
            UpdateHotbar(player.GetComponent<Hotbar>());
        }

        // Um quadradinho por espaço do personagem (o Marcelo tem 2): o número da tecla e o item.
        // O da mão ganha a borda de "selecionado".
        // Só aparece por uns segundos depois de trocar ou pegar algo, e some devagar (FadeSlow).
        void UpdateHotbar(Hotbar bar)
        {
            if (hotbar == null) return; // HUD antiga, sem a peça: recrie os prefabs

            bool show = bar != null && !PauseMenu.IsOpen && !bar.IsEmpty
                        && Time.time - bar.ChangedAt < hotbarShowSeconds;
            hotbar.alpha = Mathf.MoveTowards(hotbar.alpha, show ? 1f : 0f, Time.unscaledDeltaTime / Theme.FadeSlow);
            if (bar == null || hotbar.alpha <= 0f) return;

            var flashlight = bar.GetComponent<Flashlight>();
            for (int i = 0; i < hotbarSlots.Length && i < hotbarLabels.Length; i++)
            {
                bool exists = i < bar.Count;
                if (hotbarSlots[i].gameObject.activeSelf != exists) hotbarSlots[i].gameObject.SetActive(exists);
                if (!exists) continue;

                var item = bar.Get(i);
                bool inHand = i == bar.Selected;
                hotbarSlots[i].GetComponent<Outline>().effectColor = inHand ? Theme.HighlightColor : Theme.BorderColor;
                hotbarSlots[i].color = inHand ? Theme.RaisedColor : Theme.FieldColor;

                sb.Clear();
                sb.Append("<size=").Append(Theme.NoteSize - 4).Append('>')
                  .Append(Colored(GameInput.KeyLabel(GameInput.HotbarSlot[i]), Theme.MutedColor)).Append("</size>\n");
                if (item == HotbarItem.Flashlight && flashlight != null)
                    sb.Append("Lanterna\n").Append(Colored($"{Mathf.CeilToInt(flashlight.Charge01 * 100f)}%",
                        flashlight.Charge01 < 0.2f ? Theme.DangerColor : Theme.MutedColor));
                else if (item == HotbarItem.Battery) sb.Append("Pilhas");
                else if (item == HotbarItem.Cane) sb.Append("Bengala");
                SetText(hotbarLabels[i], sb.ToString());
                hotbarLabels[i].color = inHand ? Theme.HighlightColor : Theme.TextColor;
            }
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
            var me = switcher.Current;

            sb.Clear();
            sb.Append("Você: <b>").Append(me.FullName).Append("</b>");
            // As teclas das habilidades vêm do GameInput: se a tecla mudar lá, a dica muda sozinha.
            if (me.canTap) sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Tap)).Append("] Bengala (segure: chamar)");
            if (me.tuningFork && switcher.TryGetComponent(out TuningFork fork))
                sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Fork)).Append("] Diapasão")
                  .Append(fork.Ready01 < 1f ? Colored($" {Mathf.CeilToInt((1f - fork.Ready01) * 8f)}s", Theme.MutedColor) : "");
            if (me.whistle) sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Whistle)).Append("] Apito");
            // A lanterna só aparece depois de pegar uma no caminhão; a carga fica vermelha quando falha.
            if (switcher.TryGetComponent(out Flashlight flashlight) && flashlight.Has)
            {
                int charge = Mathf.CeilToInt(flashlight.Charge01 * 100f);
                Color color = flashlight.Charge01 < 0.2f ? Theme.DangerColor : flashlight.IsOn ? Theme.HighlightColor : Theme.MutedColor;
                string state = flashlight.Place == FlashlightPlace.Stowed ? " (guardada)"
                             : !flashlight.IsOn ? " (desligada)"
                             : flashlight.Place == FlashlightPlace.Chest ? " (no colete)" : "";
                sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.Flashlight)).Append("] Lanterna ")
                  .Append(Colored($"{charge}%{state}", color));
            }
            // Pilhas na mão e a lanterna pedindo carga: a dica do botão Usar.
            if (flashlight != null && flashlight.CanReload)
                sb.Append("\n[").Append(GameInput.KeyLabel(GameInput.UseItem)).Append("] Trocar as pilhas");

            // Trocar de personagem no meio da partida é só para TESTES (no roteiro, cada jogador
            // é um personagem fixo). Por isso a lista só aparece no editor / build de desenvolvimento.
            if (CharacterSwitcher.DebugSwitchingEnabled)
            {
                var roster = CharacterRoster.Instance;
                sb.Append("\n\n<size=").Append(Theme.NoteSize).Append(">Teste (trocar):");
                for (int i = 0; i < roster.Count && i < GameInput.SelectCharacter.Length; i++)
                {
                    var c = roster.Get(i);
                    string status = i == switcher.CurrentIndex ? " (você)"
                                  : CharacterSwitcher.IsTaken(i, switcher) ? " (ocupado)" : "";
                    sb.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(Theme.Muted(c.color))).Append('>')
                      .Append('[').Append(GameInput.KeyLabel(GameInput.SelectCharacter[i])).Append("] ")
                      .Append(c.displayName).Append("</color>").Append(status);
                }
                sb.Append("</size>");
            }
            SetText(characterText, sb.ToString());
        }

        // Uma linha por acontecimento percebido, marcada pelo sentido: [viu], [ouviu], [sentiu].
        // O Tiago não recebe as falas; a Cida não recebe o que só se vê.
        void UpdateCues(CueListener listener)
        {
            if (cuesText == null) return; // HUD antiga, sem a peça: recrie os prefabs
            var lines = listener != null ? listener.Lines : null;
            bool any = lines != null && lines.Count > 0 && !PauseMenu.IsOpen;
            cuesText.gameObject.SetActive(any);
            if (!any) return;

            sb.Clear();
            foreach (var line in lines)
            {
                string tag = line.Sense switch
                {
                    Senses.Sight => "viu",
                    Senses.Hearing => "ouviu",
                    _ => "sentiu",
                };
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(Colored($"[{tag}]", Theme.MutedColor)).Append(' ').Append(line.Text);
            }
            SetText(cuesText, sb.ToString());
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
