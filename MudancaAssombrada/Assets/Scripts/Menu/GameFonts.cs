using UnityEngine;

// As fontes do jogo, num lugar só. Ficam em Assets/Resources/Fonts: tudo que está numa pasta
// "Resources" pode ser carregado por código pelo nome (Resources.Load), sem arrastar no Inspector.
//   Title: Bebas Neue (alta, condensada, só maiúsculas) para títulos
//   Body / Bold: Poppins para todo o resto (botões, textos, HUD)
// Todas são do Google Fonts com licença OFL: livres para usar em jogos (licenças na mesma pasta).
public static class GameFonts
{
    static Font title, body, bold;

    public static Font Title => Load(ref title, "BebasNeue-Regular");
    public static Font Body => Load(ref body, "Poppins-Regular");
    public static Font Bold => Load(ref bold, "Poppins-Bold");

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

    // Para os textos desenhados com OnGUI (HUD): chame no começo do OnGUI.
    // Os GUIStyle criados a partir do GUI.skin usam a fonte do skin.
    public static void ApplyToGUI() => GUI.skin.font = Body;
}
