namespace MudancaAssombrada
{
    // Que vibração o Tiago vê de cada som.
    //   Strong = perigo ou chamado (fantasma, impacto, apito): o chão treme forte, em vermelho.
    //   Weak   = vida normal da casa (colegas andando, móvel arrastado): um tremor discreto.
    //   None   = nada (os próprios sons, voz e respiração não fazem o chão vibrar).
    public enum VibrationKind { None, Weak, Strong }

    // As REGRAS da visão de vibrações, sem cena: o VibrationVision pergunta aqui e os testes
    // (Assets/Tests/EditMode/VibrationRulesTests.cs) conferem cada uma. Só mudam o que o Tiago VÊ.
    public static class VibrationRules
    {
        public const float WeakReach = 12f;   // metros
        public const float StrongReach = 20f;
        // Mais que isto de diferença de altura = outro andar (um degrau ou rampa ainda passa).
        public const float FloorHeight = 2.5f;

        // Respostas do ChooseVictim que não são uma marca.
        public const int Room = -1; // ainda cabe: não tira ninguém
        public const int Skip = -2; // não cabe e não pode tirar ninguém: a vibração nova fica de fora

        public static VibrationKind Classify(SoundType type, bool fromGhost, bool fromMe)
        {
            if (fromMe) return VibrationKind.None;
            if ((type & (SoundType.Voice | SoundType.Breath | SoundType.Tuning)) == type) return VibrationKind.None;
            if (fromGhost) return VibrationKind.Strong;
            if ((type & (SoundType.Impact | SoundType.Whistle)) != 0) return VibrationKind.Strong;
            return VibrationKind.Weak;
        }

        public static bool InReach(VibrationKind kind, float distance, float heightDiff)
        {
            if (kind == VibrationKind.None) return false;
            if (heightDiff > FloorHeight || heightDiff < -FloorHeight) return false;
            return distance <= (kind == VibrationKind.Strong ? StrongReach : WeakReach);
        }

        // Já há marcas demais? Qual sai para a nova entrar (índice), Room ou Skip.
        // power = força atual de cada marca. Um tremor fraco nunca tira um forte, e um forte
        // tira primeiro os fracos (mesmo os mais vivos) antes de tirar outro forte.
        public static int ChooseVictim(float[] power, VibrationKind[] kinds, VibrationKind incoming, int max)
        {
            if (power.Length < max) return Room;
            int weakest = -1, weakestWeak = -1;
            for (int i = 0; i < power.Length; i++)
            {
                if (kinds[i] != VibrationKind.Strong && (weakestWeak < 0 || power[i] < power[weakestWeak])) weakestWeak = i;
                if (weakest < 0 || power[i] < power[weakest]) weakest = i;
            }
            if (weakestWeak >= 0) return weakestWeak;
            return incoming == VibrationKind.Strong ? weakest : Skip;
        }
    }
}
