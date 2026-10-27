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
    }
}
