using UnityEngine.InputSystem;

namespace MudancaAssombrada
{
    // Todos os controles do jogo, com NOME (Interagir, Correr, Apito...), num lugar só.
    //
    // Por que não ler o teclado direto (Keyboard.current.eKey)?
    //  - Cada ação pode ter várias teclas/botões (teclado E controle) sem mudar quem usa.
    //  - Trocar uma tecla é mudar UMA linha aqui, e não caçar em todos os scripts.
    //  - A dica na tela pode perguntar "qual é a tecla de Interagir?" (KeyLabel) e mostrar certo.
    //  - No futuro, dá para deixar o jogador remapear as teclas no menu de Opções.
    //
    // Uso: GameInput.Interact.WasPressedThisFrame(), GameInput.Move.ReadValue<Vector2>()...
    public static class GameInput
    {
        public static readonly InputAction Move;
        public static readonly InputAction Look;
        public static readonly InputAction Interact;
        public static readonly InputAction Crouch;
        public static readonly InputAction Sprint;
        public static readonly InputAction Tap;      // bengala da Cida
        public static readonly InputAction Whistle;  // apito do Marcelo
        public static readonly InputAction Pause;
        public static readonly InputAction Restart;
        public static readonly InputAction[] SelectCharacter; // teclas 1, 2, 3... (troca de personagem para testes)

        // Construtor "static": roda sozinho uma vez, na primeira vez que alguém usa o GameInput.
        static GameInput()
        {
            var map = new InputActionMap("Jogo");

            Move = map.AddAction("Mover", InputActionType.Value);
            // Composite = várias teclas formando UM valor: W/S/A/D viram um Vector2 (x, y).
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = map.AddAction("Olhar", InputActionType.Value, "<Mouse>/delta");

            Interact = Button(map, "Interagir", "<Keyboard>/e", "<Gamepad>/buttonWest");
            Crouch = Button(map, "Agachar", "<Keyboard>/leftCtrl", "<Keyboard>/c", "<Gamepad>/buttonEast");
            Sprint = Button(map, "Correr", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Tap = Button(map, "Bengala", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Whistle = Button(map, "Apito", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            Pause = Button(map, "Pausa", "<Keyboard>/escape", "<Gamepad>/start");
            Restart = Button(map, "Reiniciar", "<Keyboard>/r", "<Gamepad>/select");

            SelectCharacter = new InputAction[9];
            for (int i = 0; i < SelectCharacter.Length; i++)
                SelectCharacter[i] = Button(map, $"Personagem {i + 1}", $"<Keyboard>/{i + 1}");

            map.Enable(); // ações só funcionam depois de ligadas
        }

        static InputAction Button(InputActionMap map, string name, params string[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button);
            foreach (var binding in bindings) action.AddBinding(binding);
            return action;
        }

        // O nome da (primeira) tecla da ação, para mostrar na tela: "E", "Space", "F"...
        public static string KeyLabel(InputAction action) => action.GetBindingDisplayString(0);
    }
}
