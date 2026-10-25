using UnityEngine;

// O "padrão visual" do jogo em um lugar só (as regras completas estão no DESIGN.md).
// Toda cor, tamanho de texto e duração de animação da interface vem daqui:
// mudar um valor aqui muda o jogo inteiro de uma vez. As fontes ficam no GameFonts.
//
// Regra de ouro: cor viva só quando significa algo (Perigo, Amanhecer, personagens).
// Todo o resto é cinza.
public static class Theme
{
    // --- Cores de base: escuras e "apagadas", cinzas puxados para o azul-esverdeado ---
    public static readonly Color BackgroundCenter = new Color(0.1f, 0.12f, 0.13f);
    public static readonly Color BackgroundEdge = new Color(0.015f, 0.02f, 0.02f);
    public static readonly Color PanelColor = new Color(0.02f, 0.03f, 0.035f, 0.6f);
    public static readonly Color FieldColor = new Color(0.02f, 0.025f, 0.03f, 0.8f);   // campos de texto, trilho do slider
    public static readonly Color BorderColor = new Color(0.55f, 0.6f, 0.6f, 0.35f);

    // --- Texto ---
    public static readonly Color TitleColor = new Color(0.52f, 0.57f, 0.57f);     // título grande, "apagado"
    public static readonly Color TextColor = new Color(0.66f, 0.7f, 0.7f);        // texto normal
    public static readonly Color MutedColor = new Color(0.66f, 0.7f, 0.7f, 0.6f); // notas, dicas secundárias
    public static readonly Color HighlightColor = new Color(0.9f, 0.93f, 0.93f);  // hover, selecionado

    // --- Cores com significado: usar SÓ para isso ---
    public static readonly Color DangerColor = new Color(0.62f, 0.2f, 0.2f); // fantasma, erro, tempo acabando
    public static readonly Color DawnColor = new Color(0.85f, 0.7f, 0.45f);  // progresso, vitória, caminhão

    // --- Tamanhos de texto (numa tela 1920x1080). Nada fora desta lista. ---
    public const int TitleSize = 150;  // título do menu
    public const int HeaderSize = 48;  // cabeçalho de painel ("PAUSA")
    public const int ButtonSize = 36;  // botões
    public const int BodySize = 26;    // textos normais
    public const int NoteSize = 20;    // notas, status, dicas pequenas

    // --- Movimento: só fades e deslizes curtos ---
    public const float FadeFast = 0.15f; // hover, aparecer um botão
    public const float FadeSlow = 0.3f;  // trocar de painel, avisos

    // Cor do personagem "domada": misturada com o cinza para não destoar da interface.
    public static Color Muted(Color c) => Color.Lerp(c, TextColor, 0.4f);

    // A mesma cor com outra transparência (atalho para "new Color(c.r, c.g, c.b, a)").
    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
}
