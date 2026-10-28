namespace MudancaAssombrada
{
    public enum SecretDoorState : byte { Closed, Revealed, Open }
    public enum SecretDoorAction { None, Tune, Push }

    // As REGRAS do espelho-porta da Câmara Secreta (o E9, LEVEL_DESIGN_FASE1_MANSAO.md D5), sem cena
    // nem rede. O SecretMirrorDoor pergunta aqui; os testes (MansionRulesTests) conferem.
    //
    //   Fechado --a Cida AFINA (o diapasão responde oco)--> Revelado --qualquer um EMPURRA--> Aberto
    //
    // Plano B (R01): sem a Cida na partida, ninguém afinaria, e a Câmara (e a relíquia) nunca abriria.
    // Então o espelho já começa rachado: revelado, à espera de quem empurre.
    public static class SecretDoorRules
    {
        public static SecretDoorState Initial(bool cidaInMatch) =>
            cidaInMatch ? SecretDoorState.Closed : SecretDoorState.Revealed;

        // O que este jogador pode fazer agora (hasTuningFork = é a Cida).
        public static SecretDoorAction ActionFor(SecretDoorState state, bool hasTuningFork) => state switch
        {
            SecretDoorState.Closed => hasTuningFork ? SecretDoorAction.Tune : SecretDoorAction.None,
            SecretDoorState.Revealed => SecretDoorAction.Push,
            _ => SecretDoorAction.None,
        };

        // O estado depois de uma ação. Fora de ordem, nada muda.
        public static SecretDoorState After(SecretDoorState state, SecretDoorAction action) => (state, action) switch
        {
            (SecretDoorState.Closed, SecretDoorAction.Tune) => SecretDoorState.Revealed,
            (SecretDoorState.Revealed, SecretDoorAction.Push) => SecretDoorState.Open,
            _ => state,
        };
    }
}
