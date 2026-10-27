using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // Qual som tocar para um tipo de SoundEvent. Ex.: Footstep -> "passo_madeira", e se o passo
    // for forte (correndo) -> "passo_madeira_rangendo".
    [Serializable]
    public struct SoundChoice
    {
        public SoundType type;
        public string sound;
        [Tooltip("Opcional: outro som quando a intensidade passa de 'loudFrom' (correndo, batida forte).")]
        public string loudSound;
        [Range(0, 1)] public float loudFrom;

        public string Pick(float intensity) =>
            !string.IsNullOrEmpty(loudSound) && intensity >= loudFrom ? loudSound : sound;

        // A primeira escolha da lista que serve para este tipo.
        public static bool TryFind(SoundChoice[] choices, SoundType type, out SoundChoice found)
        {
            foreach (var c in choices)
                if ((c.type & type) != 0) { found = c; return true; }
            found = default;
            return false;
        }
    }

    // A "VOZ" de um objeto: troca o som padrão da biblioteca só para ele.
    // Ex.: no Bibliotecário, os passos são arrastados e a respiração é rouca.
    // Quem não tem este componente usa os padrões da SoundLibrary.
    //
    // Não muda nada na regra do jogo (alcance, quem ouve): só o que sai na caixa de som.
    // Som VAZIO = mudo: o evento continua no eco e na vibração, mas não toca nada (o pulso das fontes de ruído).
    // Funciona em rede sem nada a mais: o SoundEvent já chega com a fonte (Source) em todo computador.
    public class SoundVoice : MonoBehaviour
    {
        public SoundChoice[] choices = Array.Empty<SoundChoice>();

        // O nome do som que um SoundEvent deve tocar (null = nenhum).
        public static string Resolve(SoundEvent sound, SoundLibrary library)
        {
            // GetComponentInParent: o som pode vir de uma peça filha (ex.: a ponta da bengala).
            var voice = sound.Source != null ? sound.Source.GetComponentInParent<SoundVoice>() : null;
            if (voice != null && SoundChoice.TryFind(voice.choices, sound.Type, out var mine))
                return mine.Pick(sound.Intensity);
            return library.TryGetDefault(sound.Type, out var standard) ? standard.Pick(sound.Intensity) : null;
        }
    }
}
