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
    //   │ ▬▬▬▬ (estamina)                                      │
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
            UiBuilder.Place(timer, new Vector2(0, 1), new Vector2(0, 1), new Vector2(Margin, -30), new Vector2(700, 60));

            // Uma linha por objetivo da etapa atual (o GameManager monta o texto).
            var objectives = UiBuilder.CreateText("Objetivos", canvas, "", BodySize);
            objectives.alignment = TextAnchor.UpperLeft;
            UiBuilder.Place(objectives, new Vector2(0, 1), new Vector2(0, 1), new Vector2(Margin, -90), new Vector2(700, 220));

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
            UiBuilder.Place(character, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -30), new Vector2(520, 260));

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

            // A HUD não tem botões: não precisa "ouvir" cliques (e assim não atrapalha a pausa).
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            return canvas.gameObject;
        }
    }
}
