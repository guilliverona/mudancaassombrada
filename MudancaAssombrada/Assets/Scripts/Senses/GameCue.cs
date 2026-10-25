using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // Os SENTIDOS por onde um acontecimento pode chegar até um jogador.
    // [Flags]: um aviso pode ser percebido por vários ("um grito" = audição; "alguém sendo
    // arrastado" = visão e audição).
    [Flags]
    public enum Senses
    {
        None = 0,
        Sight = 1 << 0,     // ver (a Cida não tem)
        Hearing = 1 << 1,   // ouvir (o Tiago não tem)
        Vibration = 1 << 2, // sentir o chão tremer (o Tiago tem)
    }

    // Um ACONTECIMENTO do jogo que os jogadores podem perceber: "o Tiago pediu ajuda",
    // "a cópia foi desmascarada", "a Maria está sendo puxada para o espelho"...
    //
    // É o mesmo padrão dos sons (SoundEvent): quem faz algo só AVISA; cada computador decide,
    // pela ficha do SEU personagem, se percebeu e por qual sentido (CueListener), e a HUD desenha.
    // É isso que faz cada personagem viver a mesma cena de um jeito diferente, como no roteiro.
    public readonly struct GameCue
    {
        public readonly Vector3 Position;
        public readonly float Range;       // até onde dá para perceber (metros). 0 = a casa inteira
        public readonly Senses Senses;     // por quais sentidos chega
        public readonly string Text;       // o que aparece para quem percebeu
        public readonly GameObject Source; // quem causou (null = o ambiente)

        public GameCue(Vector3 position, float range, Senses senses, string text, GameObject source)
        {
            Position = position;
            Range = range;
            Senses = senses;
            Text = text;
            Source = source;
        }

        public bool Reaches(Vector3 listener) => Range <= 0 || Vector3.Distance(listener, Position) <= Range;
    }

    // O "rádio" dos acontecimentos, igual ao EchoSystem para os sons. Qualquer script avisa com
    //   GameCues.Emit(new GameCue(posição, alcance, Senses.Hearing, "texto", gameObject));
    // e qualquer script ouve se inscrevendo em GameCues.Emitted.
    public static class GameCues
    {
        public static event Action<GameCue> Emitted;

        // Quem envia os avisos para os outros computadores (o SoundRelay se cadastra aqui).
        public static Action<GameCue> NetworkBroadcast;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Emitted = null;
            NetworkBroadcast = null;
        }

        public static void Emit(GameCue cue)
        {
            EmitLocal(cue);
            NetworkBroadcast?.Invoke(cue);
        }

        // Só neste computador (o SoundRelay usa ao receber um aviso de outro jogador).
        public static void EmitLocal(GameCue cue) => Emitted?.Invoke(cue);
    }
}
