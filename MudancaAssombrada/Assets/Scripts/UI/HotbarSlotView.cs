using UnityEngine;
using UnityEngine.UI;

namespace MudancaAssombrada
{
    // UMA caixinha da hotbar na HUD (estilo do menu: caixinha de mudança com um pedaço de fita torta
    // em cima). Só desenha o que a Hud manda: o ícone do item, a borda (ectoplasma na mão), a barrinha
    // de bateria da lanterna (vermelha + "FRACA" quando está acabando) e o brilho de um item novo.
    // As peças são criadas e ligadas pelo HudBuilder.
    public class HotbarSlotView : MonoBehaviour
    {
        [SerializeField] Image box;
        [SerializeField] Outline border;
        [SerializeField] Image icon;
        [SerializeField] GameObject battery;
        [SerializeField] RectTransform batteryFill;
        [SerializeField] Image batteryFillImage;
        [SerializeField] Text key;
        [SerializeField] Text low;

        public RectTransform Rect => (RectTransform)transform;

        // sprite: o ícone (null = espaço vazio). inHand: o espaço escolhido. battery: carga da lanterna
        // (0 a 1) ou negativo para "sem bateria". flash: o brilho de item novo (0 a 1).
        public void Draw(Sprite sprite, bool inHand, float battery01, float flash, string keyLabel)
        {
            icon.enabled = sprite != null;
            if (sprite != null && icon.sprite != sprite) icon.sprite = sprite;

            Color accent = inHand ? Theme.HighlightColor : Theme.BorderColor;
            border.effectColor = Color.Lerp(accent, Theme.HighlightColor, flash);
            box.color = Color.Lerp(inHand ? Theme.RaisedColor : Theme.FieldColor, Theme.HighlightMistColor, flash * 0.5f);
            icon.color = inHand ? Theme.HighlightColor : Theme.TextColor;

            bool hasBattery = battery01 >= 0f;
            if (battery.activeSelf != hasBattery) battery.SetActive(hasBattery);
            bool weak = hasBattery && HotbarHudRules.BatteryLow(battery01);
            if (hasBattery)
            {
                // A barrinha "enche" mudando onde termina, como a estamina.
                batteryFill.anchorMax = new Vector2(Mathf.Clamp01(battery01), 1f);
                batteryFillImage.color = weak ? Theme.DangerColor : Theme.TextColor;
            }
            if (low.gameObject.activeSelf != weak) low.gameObject.SetActive(weak); // perigo sempre com palavra

            if (key.text != keyLabel) key.text = keyLabel;
            key.color = inHand ? Theme.HighlightColor : Theme.MutedColor;
        }
    }
}
