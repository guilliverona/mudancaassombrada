namespace MudancaAssombrada
{
    // O que a dica de interação deve mostrar agora (a HUD lê isto e desenha).
    // "readonly struct" = um pacotinho de dados que não muda depois de criado.
    public readonly struct InteractionPrompt
    {
        public readonly string Action;   // ex.: "Pegar Armário". null = nenhuma dica
        public readonly bool Available;  // false = aparece apagada (ex.: "Ocupado")

        public InteractionPrompt(string action, bool available)
        {
            Action = action;
            Available = available;
        }

        public bool Visible => Action != null;
        public static InteractionPrompt None => default;
    }

    // Qualquer coisa que o jogador alcança com a tecla Interagir (E): um móvel, um objeto
    // usável, uma pessoa para apontar... Cada componente DIZ o que dá para fazer com ele;
    // o PlayerInteractor só mira, mostra a dica e chama Interact.
    //
    // "interface" = um contrato: "quem assina promete ter estes dois métodos".
    // Mecânica nova (acordar um hipnotizado, soltar quem está sendo puxado) = um componente
    // novo que assina o contrato. O jogador não muda.
    public interface IInteractionTarget
    {
        // A dica para ESTE jogador, agora. InteractionPrompt.None = não há nada a fazer aqui.
        InteractionPrompt PromptFor(PlayerInteractor player);

        // O jogador apertou Interagir olhando para cá (só é chamado se a dica estava disponível).
        void Interact(PlayerInteractor player);
    }
}
