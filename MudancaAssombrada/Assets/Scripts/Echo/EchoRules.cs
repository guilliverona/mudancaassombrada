namespace MudancaAssombrada
{
    // Que desenho cada som faz na tela da Cida.
    //   Full = a onda que varre a sala (bengala, diapasão): é assim que ela "enxerga".
    //   Spot = só um brilho curto onde o som nasceu: diz ONDE está, sem pintar a sala.
    //   None = nada (os próprios passos e a própria respiração, que repetem o tempo todo).
    public enum EchoWave { None, Spot, Full }

    // As REGRAS do desenho do eco, sem cena: o EchoSystem pergunta aqui e os testes
    // (Assets/Tests/EditMode/EchoRulesTests.cs) conferem cada uma. Só mudam o DESENHO:
    // os fantasmas continuam ouvindo todo SoundEvent do mesmo jeito.
    public static class EchoRules
    {
        // Dois sons do mesmo emissor dentro disto (segundos) viram uma onda só (três pessoas andando
        // não enchem a tela de círculos).
        public const float MergeWindow = 0.4f;

        // Passo próprio a partir desta força ainda aparece: a tábua que range (0,85) avisa o chão podre.
        public const float LoudOwnStep = 0.8f;

        public static EchoWave Classify(SoundType type, float intensity, bool fromMe)
        {
            if ((type & (SoundType.Cane | SoundType.Tuning)) != 0) return EchoWave.Full;
            if (fromMe)
            {
                if ((type & SoundType.Breath) != 0) return EchoWave.None;
                if ((type & SoundType.Footstep) != 0 && intensity < LoudOwnStep) return EchoWave.None;
            }
            return EchoWave.Spot;
        }

        public static bool ShouldMerge(bool sameSource, bool sameWave, float secondsSince) =>
            sameSource && sameWave && secondsSince <= MergeWindow;

        // A onda cheia nunca é juntada: cada batida de bengala é uma escolha da Cida.
        public static bool ShouldMergeWave(EchoWave wave, bool sameSource, bool sameWave, float secondsSince) =>
            wave != EchoWave.Full && ShouldMerge(sameSource, sameWave, secondsSince);

        // Qual vaga a onda nova ocupa (-1 = fica de fora). alive = força atual de cada vaga (0 = livre).
        //   1) se já há maxFull ondas cheias e a nova é cheia, sai a cheia mais fraca (mesmo com vaga livre);
        //   2) vaga livre;  3) senão, sai a mais fraca que a nova pode tirar (um ponto nunca tira uma cheia).
        public static int ChooseSlot(float[] alive, EchoWave[] kinds, EchoWave incoming, int maxFull)
        {
            int fullCount = 0, free = -1;
            for (int i = 0; i < alive.Length; i++)
            {
                if (alive[i] <= 0f) { if (free < 0) free = i; }
                else if (kinds[i] == EchoWave.Full) fullCount++;
            }

            bool onlyFull = incoming == EchoWave.Full && fullCount >= maxFull;
            if (!onlyFull && free >= 0) return free;
            int best = -1;
            for (int i = 0; i < alive.Length; i++)
            {
                if (alive[i] <= 0f) continue;
                bool full = kinds[i] == EchoWave.Full;
                if (onlyFull ? !full : (incoming != EchoWave.Full && full)) continue;
                if (best < 0 || alive[i] < alive[best]) best = i;
            }
            return best;
        }
    }
}
