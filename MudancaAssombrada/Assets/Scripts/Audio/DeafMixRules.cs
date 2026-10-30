using UnityEngine;

namespace MudancaAssombrada
{
    // MEC-T8 (LEVEL_DESIGN_FASE1.md §8.2): o que o personagem surdo "ouve".
    // Surdez profunda não é silêncio: o corpo sente o grave. Então o Tiago recebe só o sub-grave
    // (passa-baixa em 80 Hz), bem baixo (-18 dB). Isso devolve a ATMOSFERA (o peso do piano, um
    // impacto) sem devolver INFORMAÇÃO: abaixo de 80 Hz não dá para reconhecer passo, voz ou de que
    // lado vem. Quem preferir pode ligar "Silêncio total" nas Opções e volta ao mudo de antes.
    //
    // Regras puras (sem cena), testadas em Assets/Tests/EditMode/DeafMixRulesTests.cs.
    public static class DeafMixRules
    {
        public const float CutoffHz = 80f;
        public const float DeafGainDb = -18f;

        // O filtro só existe para o surdo que não escolheu o silêncio total.
        public static bool UsesLowPass(bool deaf, bool totalSilence) => deaf && !totalSilence;

        // Volume final do AudioListener. volume = o slider de Volume do jogador (0 a 1).
        public static float ListenerVolume(bool deaf, bool totalSilence, float volume)
        {
            if (!deaf) return volume;
            if (totalSilence) return 0f;
            return volume * Mathf.Pow(10f, DeafGainDb / 20f); // dB → fator: -18 dB ≈ 0,126
        }
    }
}
