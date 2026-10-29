using UnityEngine;

namespace MudancaAssombrada
{
    // SEGURAR um botão por um tempo para algo acontecer (trocar as pilhas: 3 s segurando "Usar item").
    // Sem tela nem rede: quem usa chama Tick todo quadro e a HUD lê o Progress01.
    //   - conta só enquanto o botão está apertado E a ação é possível ("allowed");
    //   - soltar ou deixar de poder no meio perde o que já foi (recomeça do zero);
    //   - completou: devolve true UMA vez e só conta de novo depois de soltar (quem segura direto
    //     não gasta o 2º pacote de pilhas sem querer).
    // Os testes (HoldActionTests) conferem sem abrir uma fase.
    public sealed class HoldAction
    {
        public float Seconds { get; }
        public float Held { get; private set; }
        public bool Holding => Held > 0f;
        public float Progress01 => Seconds <= 0f ? 0f : Mathf.Clamp01(Held / Seconds);

        bool waitRelease; // completou e o botão ainda não foi solto

        public HoldAction(float seconds) => Seconds = seconds;

        // Um quadro: true no quadro em que completa.
        public bool Tick(bool pressed, bool allowed, float deltaTime)
        {
            if (!pressed) waitRelease = false;
            if (!pressed || !allowed || waitRelease)
            {
                Held = 0f;
                return false;
            }
            Held += deltaTime;
            if (Held < Seconds - 0.0001f) return false; // a folguinha: somar 0,1 trinta vezes dá 2,9999998
            Held = 0f;
            waitRelease = true;
            return true;
        }
    }
}
