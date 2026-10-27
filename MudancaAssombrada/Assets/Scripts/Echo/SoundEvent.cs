using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // QUE TIPO de som é. Cada fantasma (e cada personagem) reage a tipos diferentes:
    //   - O Imitador não tem passos nem respiração: é assim que a Cida descobre o falso.
    //   - A música da Maestrina hipnotiza quem ouve (menos o Tiago, que é surdo).
    //   - O Bibliotecário caça a voz e o barulho.
    //
    // [Flags]: cada tipo é um "bit" (1, 2, 4, 8...). Assim um campo pode guardar VÁRIOS tipos
    // ao mesmo tempo ("ouve Passos e Voz"), e o Inspector mostra uma lista de caixinhas.
    [Flags]
    public enum SoundType
    {
        None = 0,
        Footstep = 1 << 0,  // passos
        Breath = 1 << 1,    // respiração
        Voice = 1 << 2,     // voz, grito
        Impact = 1 << 3,    // móvel batendo, algo caindo
        Cane = 1 << 4,      // bengala batendo no chão
        Whistle = 1 << 5,   // apito
        Music = 1 << 6,     // instrumentos, caixinha de música
        Tuning = 1 << 7,    // diapasão da Cida: no eco, cada superfície responde com o seu timbre
        Carry = 1 << 8,     // móvel na mão: o apoio da dupla (fraco) e o arrastar sozinho (forte)
        Everything = ~0,
    }

    // Um som que aconteceu no mundo. "readonly struct": um pacotinho de dados que não muda.
    // Todo som do jogo passa pelo EchoSystem como um destes.
    public readonly struct SoundEvent
    {
        public readonly Vector3 Position;
        public readonly float Range;      // até onde dá para ouvir (metros)
        public readonly float Intensity;  // 0 = baixinho, 1 = muito alto
        public readonly SoundType Type;
        public readonly GameObject Source; // quem fez o som (null = o ambiente)

        public SoundEvent(Vector3 position, float range, float intensity, SoundType type, GameObject source)
        {
            Position = position;
            Range = range;
            Intensity = intensity;
            Type = type;
            Source = source;
        }

        // Este som é de algum dos tipos da lista? Ex.: sound.Is(SoundType.Footstep | SoundType.Voice)
        public bool Is(SoundType types) => (Type & types) != 0;
    }
}
