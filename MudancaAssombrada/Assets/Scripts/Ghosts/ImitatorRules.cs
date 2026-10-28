namespace MudancaAssombrada
{
    // As REGRAS de tempo do Imitador, sem cena nem rede: quando aparece, quando sai do espelho,
    // quanto espera lá dentro, quando desiste e quando leva a vítima. O GhostImitator junta os números
    // (relógio, segundos no espelho) e pergunta aqui; os testes (Assets/Tests/EditMode/ImitatorRulesTests.cs)
    // conferem cada regra. A máquina de estados (andar, NavMesh, espelhos) fica no GhostImitator.
    public static class ImitatorRules
    {
        // A aparição programada ainda vai acontecer nesta noite? (Sem relógio, numa cena de teste: não.)
        public static bool AppearancePending(bool appeared, float appearAt, bool hasClock) =>
            !appeared && appearAt > 0 && hasClock;

        // Chegou a hora da aparição?
        public static bool AppearanceDue(bool pending, float nightElapsed, float appearAt) =>
            pending && nightElapsed >= appearAt;

        // No espelho: é hora de agir? Antes da aparição ele não ataca: espera por ela.
        // Depois, sai quando a espera acaba OU quando alguém arrasta sozinho um móvel de dupla por um tempo,
        // mas nunca antes de "minInside" segundos lá dentro.
        public static bool ReadyToAct(bool appearanceDue, bool appearancePending, float inside, float minInside,
                                      float timer, float dragSeen, float dragInterest) =>
            appearanceDue || (!appearancePending && inside >= minInside && (timer <= 0 || dragSeen >= dragInterest));

        // Quanto tempo fica no espelho desta vez: metade no fim da noite, vezes "offendedFactor" se ofendido.
        public static float LurkTime(float lurkTime, float nightProgress, float hurryFrom, bool offended, float offendedFactor)
        {
            float t = lurkTime;
            if (nightProgress >= hurryFrom) t *= 0.5f;
            if (offended) t *= offendedFactor;
            return t;
        }

        // Disfarçado: o tempo para chegar na vítima acabou? Então desiste.
        public static bool GiveUp(float timerLeft) => timerLeft <= 0;

        // Puxando: os colegas não salvaram a tempo? Então o espelho leva.
        public static bool PullDone(float pulled, float pullTime) => pulled >= pullTime;

        // QUAL ESPELHO (R03, LEVEL_DESIGN_FASE1_MANSAO.md): numa casa de dois níveis, o espelho "mais
        // perto" em linha reta pode estar no andar de cima, a 30 m andando. Por isso conta o caminho:
        // pathLengths[i] = metros andando até o espelho i (negativo = não dá para chegar). Se nenhum
        // tem caminho (uma cena sem NavMesh), vale a linha reta. Devolve a posição na lista (-1 = vazia).
        public static int NearestMirror(float[] pathLengths, float[] straight) => PickMirror(pathLengths, straight, nearest: true);

        // O mais LONGE andando (para onde ele foge desmascarado). Inalcançável não conta: fugir para
        // um espelho aonde não se chega o deixaria preso no caminho.
        public static int FarthestMirror(float[] pathLengths, float[] straight) => PickMirror(pathLengths, straight, nearest: false);

        static int PickMirror(float[] path, float[] straight, bool nearest)
        {
            int best = -1;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] < 0) continue;
                if (best < 0 || (nearest ? path[i] < path[best] : path[i] > path[best])) best = i;
            }
            if (best >= 0) return best;
            for (int i = 0; i < straight.Length; i++)
                if (best < 0 || (nearest ? straight[i] < straight[best] : straight[i] > straight[best])) best = i;
            return best;
        }
    }
}
