namespace MudancaAssombrada
{
    // Os bilhetes do Seu Valdemar que aparecem colados no menu. Ele é o chefe fantasma:
    // mão de vaca, seco, mais preocupado com os móveis do que com os funcionários.
    // A assinatura "— V." é desenhada pelo bilhete, não faz parte do texto.
    //
    // Bilhete novo = uma linha nas listas abaixo (até MaxLength letras, para caber no papel).
    // Nunca colocar aqui algo que o jogador PRECISA saber para jogar: é só a graça.
    public static class ValdemarNotes
    {
        public const int MaxLength = 90;

        // Tela inicial.
        public static readonly string[] Menu =
        {
            "Caminhão sai antes do sol. Não quebrem nada. Nem ninguém.",
            "Hora extra não existe. Fantasma extra, sim.",
            "Quem quebrar, paga. Quem sumir, também.",
            "Lanterna é da empresa. Pilha é por conta de vocês.",
            "Ouviu passos? Ande mais rápido. Os móveis não se carregam sozinhos. Ainda.",
            "Cliente satisfeito é cliente que não assombra a gente depois.",
        };

        // Tela "Criar sala".
        public static readonly string[] Create =
        {
            "Trio completo carrega o dobro. Matemática da firma.",
            "Quem cria a sala escolhe o capítulo. E leva a culpa.",
            "Sala pública é porta aberta: entra qualquer um. Até eu.",
        };

        // Tela "Entrar numa sala".
        public static readonly string[] Rooms =
        {
            "Tem amigo sem sala? Passa o código. Fantasma não lê.",
            "Sala cheia não aceita jeitinho.",
            "Escolhe logo. O caminhão não espera.",
        };

        // Sorteia o índice de um bilhete entre "count", sem repetir o "previous"
        // (o que apareceu da última vez; -1 = nenhum). Lista vazia devolve -1.
        public static int Pick(int count, int previous, System.Random random)
        {
            if (count <= 0) return -1;
            if (count == 1) return 0;
            if (previous < 0 || previous >= count) return random.Next(count);
            // Sorteia entre os OUTROS (count - 1) e pula por cima do anterior.
            int i = random.Next(count - 1);
            return i >= previous ? i + 1 : i;
        }
    }
}
