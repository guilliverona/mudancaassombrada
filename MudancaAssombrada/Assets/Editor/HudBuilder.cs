using UnityEngine;
using UnityEngine.UI;
using static MudancaAssombrada.Theme;

namespace MudancaAssombrada.Editor
{
    // Monta o prefab da HUD (Assets/Prefabs/Interface/HUD.prefab) com as peças do UiBuilder
    // e liga cada uma no script Hud. Depois de criado, o prefab pode ser ajustado à mão.
    //
    //   ┌──────────────────────────────────────────────────────┐
    //   │ AMANHECER EM 2:59                    Você: Dona Cida │
    //   │ • Móveis no caminhão: 0/3            [Espaço] Bengala│
    //   │                                                      │
    //   │          ┌ bilhete do Seu Valdemar ┐                 │
    //   │                        ·                             │
    //   │                   [E] Pegar Armário                  │
    //   │ ▬▬▬▬ (estamina)        [1][2][3] (hotbar)            │
    //   └──────────────────────────────────────────────────────┘
    public static class HudBuilder
    {
        const float Margin = 40f; // distância das bordas da tela

        public static GameObject Build()
        {
            // sortingOrder 5: atrás da pausa (10), para o menu de pausa cobrir a HUD.
            var canvas = UiBuilder.CreateCanvas("HUD", sortingOrder: 5);

            // --- Canto superior esquerdo: a partida ---
            var timer = UiBuilder.CreateText("Cronômetro", canvas, "Amanhecer em 3:00", HeaderSize);
            timer.font = GameFonts.Title;
            timer.alignment = TextAnchor.UpperLeft;
            UiBuilder.Place(timer, new Vector2(0, 1), new Vector2(0, 1), new Vector2(Margin, -Margin), new Vector2(700, 60));

            // Uma linha por objetivo da etapa atual (o GameManager monta o texto).
            var objectives = UiBuilder.CreateText("Objetivos", canvas, "", BodySize);
            objectives.alignment = TextAnchor.UpperLeft;
            UiBuilder.Place(objectives, new Vector2(0, 1), new Vector2(0, 1), new Vector2(Margin, -Margin - 60), new Vector2(700, 220));

            // --- Centro, em cima: o bilhete do Seu Valdemar (some sozinho depois de alguns segundos) ---
            var note = UiBuilder.CreateImage("Bilhete", canvas, PanelColor);
            UiBuilder.Place(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(900, 300));
            var noteText = UiBuilder.CreateText("Texto do Bilhete", note.transform, "", BodySize);
            UiBuilder.Stretch(noteText);
            ((RectTransform)noteText.transform).offsetMin = new Vector2(40, 30);  // margem interna
            ((RectTransform)noteText.transform).offsetMax = new Vector2(-40, -30);

            // --- Canto superior direito: personagens ---
            var character = UiBuilder.CreateText("Personagem", canvas, "", NoteSize);
            character.alignment = TextAnchor.UpperRight;
            UiBuilder.Place(character, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -Margin), new Vector2(520, 260));

            // --- Centro: mira e dica de interação ---
            var crosshair = UiBuilder.CreateImage("Mira", canvas, TextColor);
            UiBuilder.Place(crosshair, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));

            var hint = new GameObject("Dica", typeof(RectTransform));
            hint.transform.SetParent(canvas, false);
            UiBuilder.Place(hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(420, 40));

            // A "teclinha": quadrado com borda e a letra dentro.
            var keyBox = UiBuilder.CreateImage("Tecla", hint.transform, FieldColor);
            keyBox.gameObject.AddComponent<Outline>().effectColor = BorderColor;
            UiBuilder.Place(keyBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(40, 40));
            var keyText = UiBuilder.CreateText("Letra", keyBox.transform, "E", BodySize);
            keyText.font = GameFonts.Title;
            UiBuilder.Stretch(keyText);

            var action = UiBuilder.CreateText("Ação", hint.transform, "", NoteSize);
            action.alignment = TextAnchor.MiddleLeft;
            UiBuilder.Place(action, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(368, 40));

            // --- Canto inferior esquerdo: estamina (só aparece quando não está cheia) ---
            var staminaBar = UiBuilder.CreateImage("Estamina", canvas, FieldColor);
            UiBuilder.Place(staminaBar, Vector2.zero, Vector2.zero, new Vector2(Margin, Margin), new Vector2(240, 8));
            var staminaFill = UiBuilder.CreateImage("Estamina (cheia)", staminaBar.transform, TextColor);
            UiBuilder.Stretch(staminaFill); // a Hud muda onde ela termina (anchorMax.x) conforme a estamina

            // --- Canto inferior esquerdo, acima da estamina: o que você percebeu (viu/ouviu/sentiu) ---
            var cues = UiBuilder.CreateText("Avisos", canvas, "", NoteSize);
            cues.alignment = TextAnchor.LowerLeft;
            UiBuilder.Place(cues, Vector2.zero, Vector2.zero, new Vector2(Margin, Margin + 24), new Vector2(760, 180));

            // --- Embaixo, no meio: a hotbar (caixinhas com fita; aparece ao trocar de item e some sozinha) ---
            var (group, slots, useHint, useAction, useProgress) = BuildHotbar(canvas);
            var icons = ItemIconBuilder.EnsureAll(); // ícones gerados dos modelos do jogo

            // --- Tela de fim de partida (cobre tudo) ---
            var endScreen = UiBuilder.CreateImage("Fim de Partida", canvas, new Color(0, 0, 0, 0.75f));
            UiBuilder.Stretch(endScreen);
            var endText = UiBuilder.CreateText("Mensagem", endScreen.transform, "", HeaderSize);
            UiBuilder.Stretch(endText);
            endScreen.gameObject.SetActive(false);

            // --- Liga tudo no script ---
            var hud = canvas.gameObject.AddComponent<Hud>();
            EditorTools.SetReference(hud, "timerText", timer);
            EditorTools.SetReference(hud, "objectivesText", objectives);
            EditorTools.SetReference(hud, "note", note.gameObject);
            EditorTools.SetReference(hud, "noteText", noteText);
            EditorTools.SetReference(hud, "endScreen", endScreen.gameObject);
            EditorTools.SetReference(hud, "endText", endText);
            EditorTools.SetReference(hud, "crosshair", crosshair.gameObject);
            EditorTools.SetReference(hud, "characterText", character);
            EditorTools.SetReference(hud, "staminaBar", staminaBar.gameObject);
            EditorTools.SetReference(hud, "staminaFill", staminaFill.rectTransform);
            EditorTools.SetReference(hud, "staminaFillImage", staminaFill);
            EditorTools.SetReference(hud, "hint", hint);
            EditorTools.SetReference(hud, "hintKey", keyText);
            EditorTools.SetReference(hud, "hintAction", action);
            EditorTools.SetReference(hud, "cuesText", cues);
            EditorTools.SetReference(hud, "hotbar", group);
            EditorTools.SetReferences(hud, "hotbarSlots", slots);
            EditorTools.SetReference(hud, "useHint", useHint);
            EditorTools.SetReference(hud, "useHintAction", useAction);
            EditorTools.SetReference(hud, "useHintProgress", useProgress.rectTransform);
            EditorTools.SetReference(hud, "flashlightIcon", icons[0]);
            EditorTools.SetReference(hud, "batteryIcon", icons[1]);
            EditorTools.SetReference(hud, "caneIcon", icons[2]);

            // A HUD não tem botões: não precisa "ouvir" cliques (e assim não atrapalha a pausa).
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            return canvas.gameObject;
        }

        // A hotbar no estilo do menu (DESIGN.md: caixinha de mudança com fita, tecla na caixinha com
        // borda como o [E]). Cada espaço é um HotbarSlotView; a Hud centraliza pelo número de espaços.
        //
        //  (mouse) ESQ Segure: trocar as pilhas ← dica do Usar (só com as pilhas na mão)
        //              ▰▰▰▰▱▱▱▱▱▱              ← enche enquanto segura (3 s)
        //     ▱▱▱      ▱▱▱      ▱▱▱            ← fita torta
        //    ┌────┐   ┌────┐   ┌────┐
        //    │ ic │   │ ic │   │    │          ← ícone
        //    │▬▬▬ │   │    │   │    │          ← bateria (só a lanterna)
        //    └────┘   └────┘   └────┘
        //    [1]FRACA  [2]      [3]            ← tecla (e "FRACA" com a bateria acabando)
        const float SlotSize = 64f, KeyRow = 26f;
        static readonly float[] TapeTilt = { -3f, 2f, -1.5f };

        static (CanvasGroup, Object[], GameObject, Text, Image) BuildHotbar(Transform canvas)
        {
            var root = new GameObject("Hotbar", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            UiBuilder.Place(root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, Margin), new Vector2(240, SlotSize + KeyRow + 58));
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var slots = new Object[HotbarRules.Slots];
            for (int i = 0; i < HotbarRules.Slots; i++)
            {
                var slotGo = new GameObject($"Espaço {i + 1}", typeof(RectTransform));
                slotGo.transform.SetParent(root.transform, false);
                UiBuilder.Place(slotGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(SlotSize, SlotSize + KeyRow));

                // A caixinha, acima da linha da tecla.
                var box = UiBuilder.CreateImage("Caixa", slotGo.transform, FieldColor);
                var border = box.gameObject.AddComponent<Outline>();
                border.effectColor = BorderColor;
                UiBuilder.Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, KeyRow), new Vector2(SlotSize, SlotSize));

                // O pedaço de fita torta em cima (só decoração: nunca atrás de texto).
                var tape = UiBuilder.CreateImage("Fita", box.transform, TapeColor);
                UiBuilder.Place(tape, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(40, 11));
                tape.rectTransform.localRotation = Tilt(TapeTilt[i % TapeTilt.Length]);

                var icon = UiBuilder.CreateImage("Ícone", box.transform, TextColor);
                icon.preserveAspect = true;
                UiBuilder.Place(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(42, 42));

                // A barrinha de bateria (só a lanterna), embaixo dentro da caixa.
                var battery = UiBuilder.CreateImage("Bateria", box.transform, ShadowColor);
                battery.gameObject.AddComponent<Outline>().effectColor = BorderColor;
                UiBuilder.Place(battery, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 7), new Vector2(40, 5));
                var fill = UiBuilder.CreateImage("Carga", battery.transform, TextColor);
                UiBuilder.Stretch(fill); // a Hud muda onde ela termina (anchorMax.x), como a estamina

                // A tecla numa caixinha com borda (o mesmo formato do [E] da dica de interação).
                var keyBox = UiBuilder.CreateImage("Tecla", slotGo.transform, FieldColor);
                keyBox.gameObject.AddComponent<Outline>().effectColor = BorderColor;
                UiBuilder.Place(keyBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(22, 22));
                var key = UiBuilder.CreateText("Número", keyBox.transform, (i + 1).ToString(), NoteSize);
                key.font = GameFonts.Title;
                key.verticalOverflow = VerticalWrapMode.Overflow; // a linha da Bebas é mais alta que a caixinha
                UiBuilder.Stretch(key);

                // "FRACA" ao lado da tecla quando a bateria está acabando (perigo sempre com palavra).
                var low = UiBuilder.CreateText("Fraca", slotGo.transform, "FRACA", NoteSize);
                low.font = GameFonts.Title;
                low.color = DangerColor;
                low.alignment = TextAnchor.MiddleLeft;
                low.verticalOverflow = VerticalWrapMode.Overflow;
                UiBuilder.Place(low, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(15, 0), new Vector2(60, 22));
                low.gameObject.SetActive(false);

                var view = slotGo.AddComponent<HotbarSlotView>();
                EditorTools.SetReference(view, "box", box);
                EditorTools.SetReference(view, "border", border);
                EditorTools.SetReference(view, "icon", icon);
                EditorTools.SetReference(view, "battery", battery.gameObject);
                EditorTools.SetReference(view, "batteryFill", fill.rectTransform);
                EditorTools.SetReference(view, "batteryFillImage", fill);
                EditorTools.SetReference(view, "key", key);
                EditorTools.SetReference(view, "low", low);
                slots[i] = view;
            }

            // A dica do botão Usar, logo acima da barra: o mouse com o botão esquerdo aceso (MouseIconBuilder),
            // "ESQ" e a ação. Pequena: é um lembrete, não um aviso.
            var hint = new GameObject("Dica do Usar", typeof(RectTransform));
            hint.transform.SetParent(root.transform, false);
            UiBuilder.Place(hint.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, KeyRow + SlotSize + 24), new Vector2(220, 26)); // 24: a barrinha de segurar não encosta na fita
            var mouse = UiBuilder.CreateImage("Mouse", hint.transform, Color.white);
            mouse.sprite = MouseIconBuilder.Ensure();
            mouse.preserveAspect = true;
            UiBuilder.Place(mouse, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(17, 24));
            var hintKey = UiBuilder.CreateText("Botão", hint.transform, "ESQ", NoteSize);
            hintKey.font = GameFonts.Title;
            hintKey.color = MutedColor;
            hintKey.alignment = TextAnchor.MiddleLeft;
            hintKey.verticalOverflow = VerticalWrapMode.Overflow;
            UiBuilder.Place(hintKey, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(30, 24));
            var hintAction = UiBuilder.CreateText("Ação", hint.transform, "Segure: trocar as pilhas", NoteSize);
            hintAction.alignment = TextAnchor.MiddleLeft;
            hintAction.color = HighlightColor;
            hintAction.verticalOverflow = VerticalWrapMode.Overflow;
            hintAction.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiBuilder.Place(hintAction, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(168, 24));
            // Embaixo do texto, a barrinha que enche enquanto o botão é segurado (3 s), no formato da
            // de bateria: fundo escuro com contorno, carga em ectoplasma (o mesmo aceso do mouse).
            var progress = UiBuilder.CreateImage("Progresso", hint.transform, ShadowColor);
            progress.gameObject.AddComponent<Outline>().effectColor = BorderColor;
            UiBuilder.Place(progress, new Vector2(0, 0f), new Vector2(0, 0.5f), new Vector2(52, -3), new Vector2(150, 4));
            var progressFill = UiBuilder.CreateImage("Carga", progress.transform, HighlightColor);
            UiBuilder.Stretch(progressFill); // a Hud muda onde ela termina (anchorMax.x)
            progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            hint.SetActive(false);

            return (group, slots, hint, hintAction, progressFill);
        }
    }
}
