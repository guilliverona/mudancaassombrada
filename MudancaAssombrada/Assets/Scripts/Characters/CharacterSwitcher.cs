using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Qual personagem cada jogador está usando. Fica no Player.
    // A escolha é conhecida por TODOS (NetworkVariable), e quem decide se pode é o host
    // (não pode haver duas Cidas na mesma partida).
    //
    // Os personagens em si NÃO estão aqui: cada um é uma ficha (CharacterDefinition) no Elenco.
    // Este script só aplica a ficha escolhida nos componentes do boneco.
    public class CharacterSwitcher : NetworkBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Todos os bonecos na cena, para saber quais personagens já estão em uso
        // (mais rápido e mais claro do que procurar na cena inteira toda vez).
        static readonly List<CharacterSwitcher> all = new();

        // NetworkVariable = uma variável que a rede mantém IGUAL em todos os computadores.
        // Guarda a POSIÇÃO do personagem no Elenco. Escrita "Server": só o host muda.
        readonly NetworkVariable<int> characterIndex = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] Renderer[] body; // as partes do boneco que mudam de cor (a "roupa")

        VibrationVision vibration;
        CaneTapper cane;
        Whistle whistle;
        PlayerMovement movement;
        FurnitureCarrier carrier;

        public int CurrentIndex => characterIndex.Value;
        public CharacterDefinition Current => CharacterRoster.Instance.Get(characterIndex.Value);

        void Awake()
        {
            vibration = GetComponent<VibrationVision>();
            cane = GetComponent<CaneTapper>();
            whistle = GetComponent<Whistle>();
            movement = GetComponent<PlayerMovement>();
            carrier = GetComponent<FurnitureCarrier>();
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            characterIndex.OnValueChanged += OnCharacterChanged;

            // O host usa o personagem escolhido na sala de espera.
            // (Sem escolha, por exemplo testando direto na fase, pega o primeiro livre.)
            if (IsServer)
                characterIndex.Value = LobbyManager.ChosenCharacters.TryGetValue(OwnerClientId, out int chosen)
                    ? chosen
                    : FirstFreeCharacter();

            Apply(Current);
        }

        public override void OnNetworkDespawn() => characterIndex.OnValueChanged -= OnCharacterChanged;

        void OnCharacterChanged(int previous, int current) => Apply(Current);

        void Update()
        {
            if (!IsOwner || PauseMenu.IsOpen) return; // só o dono deste boneco pode pedir para trocar

            int count = Mathf.Min(CharacterRoster.Instance.Count, GameInput.SelectCharacter.Length);
            for (int i = 0; i < count; i++)
                if (GameInput.SelectCharacter[i].WasPressedThisFrame()) RequestCharacterRpc(i);
        }

        // RPC = chamar um método que roda em OUTRO computador. SendTo.Server: roda no host.
        // Regra do Netcode: o nome do método precisa terminar em "Rpc".
        [Rpc(SendTo.Server)]
        void RequestCharacterRpc(int wanted)
        {
            if (!CharacterRoster.Instance.IsValid(wanted) || IsTaken(wanted, this)) return; // o host recusa
            characterIndex.Value = wanted; // a rede espalha o novo valor para todos
        }

        // Algum OUTRO boneco já está com este personagem?
        public static bool IsTaken(int index, CharacterSwitcher except = null)
        {
            foreach (var other in all)
                if (other != except && other.IsSpawned && other.characterIndex.Value == index) return true;
            return false;
        }

        int FirstFreeCharacter()
        {
            for (int i = 0; i < CharacterRoster.Instance.Count; i++)
                if (!IsTaken(i, this)) return i;
            return 0; // mais jogadores que personagens: repete o primeiro
        }

        void Apply(CharacterDefinition character)
        {
            // TODOS os computadores pintam este boneco com a cor do personagem.
            // MaterialPropertyBlock muda a cor só DESTE objeto, sem criar material novo,
            // e continua valendo mesmo quando a Cida troca os materiais pelos de eco.
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, character.color);
            foreach (var part in body)
                part.SetPropertyBlock(block);

            // Se a tela está escura (alguém aqui é cego), escurece também este boneco.
            if (EchoSystem.Instance != null) EchoSystem.Instance.RefreshNewRenderers();

            // Habilidades e sentidos: só no computador de quem controla este boneco.
            if (IsOwner) Become(character);
        }

        // Cada personagem é só uma combinação diferente dos mesmos componentes ligados/desligados
        // (COMPOSIÇÃO). A ficha diz quais; este método só liga as peças.
        void Become(CharacterDefinition c)
        {
            if (EchoSystem.Instance != null) EchoSystem.Instance.SetBlind(c.blind); // tela escura, vê pelo som
            cane.CanTap = c.canTap;
            vibration.enabled = c.vibrationVision;
            whistle.enabled = c.whistle;

            carrier.Strength = c.strength;
            carrier.CarryNoise = c.carryNoise;
            cane.StepVolume = c.stepVolume;
            movement.CharacterSpeed = c.speed;

            // Surdo: zera o volume de TODO o jogo. Fica em GameSettings.Muted para mexer
            // no volume pela pausa não "curar" a surdez.
            GameSettings.Muted = c.deaf;
            GameSettings.Apply();
        }
    }
}
