using UnityEngine;
using UnityEngine.UI;

// Faz um texto ou imagem piscar como uma lâmpada velha: quase sempre aceso,
// mas de vez em quando dá umas "falhadas" rápidas. Usado no título do menu.
public class Flicker : MonoBehaviour
{
    [SerializeField] float minInterval = 2f, maxInterval = 6f; // tempo entre uma falha e outra
    [SerializeField] float dimAlpha = 0.25f;                   // quão apagado fica na falha

    Graphic graphic;
    float baseAlpha;
    float nextFlicker;
    int blinksLeft;
    float blinkTimer;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseAlpha = graphic.color.a;
        nextFlicker = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (blinksLeft == 0)
        {
            nextFlicker -= dt;
            if (nextFlicker <= 0)
            {
                blinksLeft = Random.Range(2, 5) * 2; // cada piscada = apaga + acende
                nextFlicker = Random.Range(minInterval, maxInterval);
            }
            // Fora das falhas, um "zumbido" leve no brilho, como luz fluorescente.
            SetAlpha(baseAlpha * (0.92f + 0.08f * Mathf.PerlinNoise(Time.unscaledTime * 3f, 0)));
            return;
        }

        blinkTimer -= dt;
        if (blinkTimer > 0) return;
        blinkTimer = Random.Range(0.03f, 0.09f);
        blinksLeft--;
        SetAlpha(blinksLeft % 2 == 1 ? baseAlpha * dimAlpha : baseAlpha);
    }

    void SetAlpha(float a)
    {
        var c = graphic.color;
        c.a = a;
        graphic.color = c;
    }
}
