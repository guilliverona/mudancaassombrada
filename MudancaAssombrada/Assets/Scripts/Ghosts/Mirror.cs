namespace MudancaAssombrada
{
    // Um espelho da Casa do Espelho: o esconderijo do Imitador. Ele sai de um espelho, puxa a
    // vítima para dentro de um espelho e, desmascarado, foge para OUTRO espelho.
    // Tudo o que um esconderijo faz está em GhostHideout; esta classe só dá o TIPO "espelho",
    // para o Imitador procurar só espelhos (GhostHideout.Nearest<Mirror>).
    public class Mirror : GhostHideout { }
}
