using UnityEngine;

namespace MudancaAssombrada
{
    // As fontes do jogo, num lugar só. Ficam em Assets/Resources/Fonts: tudo que está numa pasta
    // "Resources" pode ser carregado por código pelo nome (Resources.Load), sem arrastar no Inspector.
    //   Stamp: Saira Stencil One (letra de carimbo de caixa) só para o título do jogo
    //   Title: Bebas Neue (alta, condensada, só maiúsculas) para cabeçalhos e botões
    //   Body / Bold: Poppins para todo o resto (textos, HUD)
    //   Hand: Permanent Marker (caneta) para os bilhetes do Seu Valdemar e subtítulos de botão
    // Todas são do Google Fonts, livres para usar em jogos (OFL ou Apache; licenças na mesma pasta).
    public static class GameFonts
    {
        static Font stamp, title, body, bold, hand;

        public static Font Stamp => Load(ref stamp, "SairaStencilOne-Regular");
        public static Font Title => Load(ref title, "BebasNeue-Regular");
        public static Font Body => Load(ref body, "Poppins-Regular");
        public static Font Bold => Load(ref bold, "Poppins-Bold");
        public static Font Hand => Load(ref hand, "PermanentMarker-Regular");

        // Carrega uma vez e guarda. Se a fonte não for encontrada, usa a padrão da Unity
        // para não quebrar o jogo, mas NÃO guarda: assim, quando o arquivo aparecer, ela é achada.
        // ("ref" deixa a função trocar a variável de quem chamou: title, body ou bold.)
        static Font Load(ref Font cached, string name)
        {
            if (cached == null) cached = Resources.Load<Font>("Fonts/" + name);
            if (cached != null) return cached;
            Debug.LogWarning($"Fonte '{name}' não encontrada em Assets/Resources/Fonts. Usando a padrão.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
