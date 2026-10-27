using UnityEngine;

namespace MudancaAssombrada
{
    // Fica no Player. A MIRA e a tecla Interagir (E), e nada mais:
    //   1. Um raio sai do centro da câmera e acha o que está na frente.
    //   2. Pergunta aos componentes de lá (IInteractionTarget) o que dá para fazer.
    //   3. Expõe a dica (a HUD desenha) e, ao apertar E, chama o alvo.
    // Ele não sabe o que é um móvel ou um fantasma: cada alvo sabe de si (composição).
    //
    // Exceção: de mãos ocupadas, a tecla é do FurnitureCarrier ("Soltar"), olhe para onde olhar.
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 2.5f; // distância máxima para alcançar

        // A dica que a HUD deve mostrar neste frame.
        public InteractionPrompt Prompt { get; private set; }

        // Peças do jogador que os alvos podem precisar (ex.: o móvel pede o Carrier para ser pego).
        public FurnitureCarrier Carrier { get; private set; }
        public NetworkPlayer Player { get; private set; }
        public CharacterDefinition Character => character.Current;
        public Camera Camera => playerCamera;

        CharacterSwitcher character;
        IInteractionTarget target; // o que está na mira AGORA (null = nada)
        IHoldInteractionTarget holding; // alvo em que a tecla está sendo SEGURADA (null = nenhum)

        void Awake()
        {
            Carrier = GetComponent<FurnitureCarrier>();
            Player = GetComponent<NetworkPlayer>();
            character = GetComponent<CharacterSwitcher>();
        }

        void OnDisable()
        {
            Prompt = InteractionPrompt.None;
            StopHolding();
        }

        // Soltou, tirou a mira ou ficou sem alcance: avisa o alvo que segurava (ex.: o colega desaba de novo).
        void StopHolding()
        {
            if (holding == null) return;
            holding.HoldStop(this);
            holding = null;
        }

        void Update()
        {
            // Caído, ainda se levantando (BodyLocked) ou hipnotizado: não alcança nada
            // (puxado ainda pode apontar para quem puxa).
            var state = Player != null && Player.Condition != null ? Player.Condition.State : PlayerState.Normal;
            bool gettingUp = Player != null && Player.Movement != null && Player.Movement.BodyLocked;
            if (state == PlayerState.Down || state == PlayerState.Hypnotized || gettingUp)
            {
                StopHolding();
                target = null;
                Prompt = InteractionPrompt.None;
                return;
            }

            if (Carrier != null && Carrier.HasLoad)
            {
                StopHolding();
                target = null;
                Prompt = Carrier.DropPrompt;
                if (Pressed()) Carrier.Drop();
                return;
            }

            Prompt = FindTarget(out target);

            // Segurando: continua enquanto a tecla estiver apertada E a mira no mesmo alvo.
            if (holding != null && (!Held() || !ReferenceEquals(target, holding))) StopHolding();

            if (Pressed() && target != null && Prompt.Available)
            {
                target.Interact(this);
                if (target is IHoldInteractionTarget hold) holding = hold;
            }
        }

        static bool Pressed() => !PauseMenu.IsOpen && GameInput.Interact.WasPressedThisFrame();
        static bool Held() => !PauseMenu.IsOpen && GameInput.Interact.IsPressed();

        // Raycast: um "raio laser" invisível saindo do centro da câmera.
        // Um objeto pode ter VÁRIOS alvos (ex.: um móvel que também é usável): vale o primeiro
        // que tem algo a dizer. O alvo pode estar no PAI do colisor atingido (ex.: o colisor do
        // corpo caído é um filho do Player, e quem sabe "Ajudar" é o PlayerCondition do Player).
        InteractionPrompt FindTarget(out IInteractionTarget found)
        {
            found = null;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach)) return InteractionPrompt.None;

            foreach (var candidate in hit.collider.GetComponentsInParent<IInteractionTarget>())
            {
                var prompt = candidate.PromptFor(this);
                if (!prompt.Visible) continue;
                found = candidate;
                return prompt;
            }
            return InteractionPrompt.None;
        }
    }
}
