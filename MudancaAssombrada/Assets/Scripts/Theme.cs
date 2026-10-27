using UnityEngine;

namespace MudancaAssombrada
{
    // O "padrão visual" do jogo em um lugar só (as regras completas estão no DESIGN.md).
    // Toda cor, tamanho de texto e duração de animação da interface vem daqui:
    // mudar um valor aqui muda o jogo inteiro de uma vez. As fontes ficam no GameFonts.
    //
    // Direção: "terror de mentirinha". A noite azul dá o clima; o papelão, a fita e os
    // bilhetes do Seu Valdemar dão a graça. Os nomes entre parênteses são os do design system.
    //
    // Regra de ouro: cor viva só quando significa algo (Perigo, Amanhecer, personagens).
    // O papelão é a exceção: ele marca a ação principal de cada tela (um botão só).
    public static class Theme
    {
        // Atalho: cor a partir do código hexadecimal do design system ("#151a21").
        static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        // --- Noite: os fundos, do mais escuro ao mais claro ---
        public static readonly Color BackgroundEdge = Hex("#0b0e12");   // (noite-900) borda da vinheta
        public static readonly Color BackgroundCenter = Hex("#151a21"); // (noite-800) meio da vinheta
        public static readonly Color PanelColor = Hex("#1b222b", 0.92f); // (noite-700) caixa atrás de listas e formulários
        public static readonly Color FieldColor = Hex("#232b36");       // (noite-600) campos, cartões, trilho do slider
        public static readonly Color RaisedColor = Hex("#2e3846");      // (noite-500) hover de superfícies escuras
        public static readonly Color BorderColor = Hex("#6b7789");      // (borda) contorno fino, linha de corte
        public static readonly Color ShadowColor = Hex("#06080a");      // (sombra-chao) sombra dura de "brinquedo"

        // --- Papel: caixa de mudança, fita e bilhete ---
        public static readonly Color CardboardColor = Hex("#c4935a");      // (papelao) botão principal e título
        public static readonly Color CardboardLightColor = Hex("#d8a86c"); // (papelao-claro) hover do papelão
        public static readonly Color CardboardDarkColor = Hex("#8a6338");  // (papelao-escuro) dobras, contorno do papelão
        public static readonly Color TapeColor = Hex("#e8d6a0", 0.55f);    // (fita) só decoração, nunca atrás de texto
        public static readonly Color NoteColor = Hex("#f2e3a0");           // (bilhete) papel dos bilhetes

        // --- Texto ---
        public static readonly Color TitleColor = CardboardColor;              // título carimbado
        public static readonly Color TextColor = Hex("#ece6da");               // (tinta) texto normal
        public static readonly Color MutedColor = Hex("#9aa3a8");              // (tinta-suave) notas, dicas secundárias
        public static readonly Color CardboardInkColor = Hex("#1c1612");       // (tinta-caixa) texto sobre papelão
        public static readonly Color NoteInkColor = Hex("#2b2418");            // (tinta-bilhete) caneta dos bilhetes
        public static readonly Color HighlightColor = Hex("#8fe3c4");          // (ectoplasma) hover, foco, selecionado, texto digitado
        public static readonly Color HighlightMistColor = Hex("#8fe3c4", 0.16f); // (ectoplasma-nevoa) fundo do selecionado

        // --- Cores com significado: usar SÓ para isso ---
        public static readonly Color DangerColor = Hex("#ff7a6b"); // fantasma, erro, tempo acabando (sempre com palavra junto)
        public static readonly Color DawnColor = Hex("#f0b45a");   // progresso, vitória, caminhão

        // --- Tamanhos de texto (numa tela 1920x1080). Nada fora desta lista. ---
        public const int TitleSize = 150;  // título do menu
        public const int HeaderSize = 48;  // cabeçalho de painel ("PAUSA")
        public const int ButtonSize = 36;  // botões
        public const int HandSize = 28;    // bilhetes do Seu Valdemar (fonte de caneta)
        public const int BodySize = 26;    // textos normais
        public const int NoteSize = 20;    // notas, status, dicas pequenas, subtítulo de botão

        // --- Forma ---
        public static readonly Vector2 StepShadow = new Vector2(0, -6); // (sombra-degrau) botões e bilhetes
        // Inclinações em graus, no sentido do CSS do design system: positivo = horário.
        // Use sempre Theme.Tilt(...): na Unity o sinal é o contrário (positivo = anti-horário).
        public const float TitleTilt = -2f;  // graus (torto-titulo)
        public const float NoteTilt = 3f;    // graus (torto-bilhete); alternar o sinal entre bilhetes vizinhos
        public const float HoverTilt = -1.5f; // graus (torto-hover): balanço do botão, SÓ no menu

        // --- Movimento: fades, deslizes curtos e (só no menu) o balanço do botão ---
        public const float FadeFast = 0.15f; // hover, aparecer um botão
        public const float FadeSlow = 0.3f;  // trocar de painel, avisos

        // Rotação de interface a partir de uma inclinação do design system (graus, positivo = horário).
        public static Quaternion Tilt(float degrees) => Quaternion.Euler(0, 0, -degrees);

        // Cor do personagem "domada": misturada com o cinza para não destoar da interface.
        public static Color Muted(Color c) => Color.Lerp(c, TextColor, 0.4f);

        // A mesma cor com outra transparência (atalho para "new Color(c.r, c.g, c.b, a)").
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
