using System;
using System.Collections.Generic;
using Unity.Collections; // FixedString32Bytes
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace MudancaAssombrada
{
    // Um jogador na sala de espera. "struct" pequena porque vai pela rede o tempo todo.
    // INetworkSerializable: ensina o Netcode a transformar isto em bytes (e de volta).
    // IEquatable: o NetworkList precisa saber comparar dois itens para detectar mudanças.
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        // string normal não pode ir dentro de uma NetworkList (tamanho variável).
        // FixedString32Bytes = texto de tamanho fixo, até ~30 letras.
        public FixedString32Bytes Name;
        public int Character; // posição do personagem no Elenco (CharacterRoster)

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Character);
        }

        public bool Equals(LobbyPlayer other) =>
            ClientId == other.ClientId && Name.Equals(other.Name) && Character == other.Character;
    }

    // A sala de espera: quem está conectado, com que nome e qual personagem.
    // Fica na cena Menu, num objeto com NetworkObject.
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        // Guardados no host quando a fase começa. São "static" porque este objeto é destruído
        // na troca de cena, mas o CharacterSwitcher (na fase) ainda precisa saber quem escolheu o quê.
        // Chave: o jogador. Valor: a posição do personagem no Elenco.
        public static readonly Dictionary<ulong, int> ChosenCharacters = new();
        public static bool GameStarted { get; private set; }

        // Esquece a partida anterior (chamado ao hospedar e no começo de cada Play).
        public static void ResetSession()
        {
            GameStarted = false;
            ChosenCharacters.Clear();
        }

        // O menu escuta este evento para redesenhar a lista quando algo muda.
        public event Action Changed;

        // NetworkList = uma lista que a rede mantém igual em todos os computadores.
        // Precisa ser criada no Awake (e não na declaração), regra do Netcode.
        NetworkList<LobbyPlayer> players;

        // O capítulo escolhido pelo host (posição na Campanha). Todos veem, só o host muda.
        readonly NetworkVariable<int> selectedChapter = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int SelectedChapter => selectedChapter.Value;

        public IReadOnlyList<LobbyPlayer> Players
        {
            get
            {
                var list = new List<LobbyPlayer>();
                foreach (var p in players) list.Add(p);
                return list;
            }
        }

        void Awake()
        {
            Instance = this;
            players = new NetworkList<LobbyPlayer>();
        }

        public override void OnNetworkSpawn()
        {
            players.OnListChanged += OnPlayersChanged;
            selectedChapter.OnValueChanged += OnChapterChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddPlayer;
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;
                AddPlayer(NetworkManager.LocalClientId); // o próprio host
                selectedChapter.Value = DefaultChapter();
            }

            // Cada um conta ao host o nome que digitou no menu.
            SetNameRpc(GameSettings.PlayerName);
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            players.OnListChanged -= OnPlayersChanged;
            selectedChapter.OnValueChanged -= OnChapterChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddPlayer;
                NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
            }
            Changed?.Invoke();
        }

        void OnPlayersChanged(NetworkListEvent<LobbyPlayer> change) => Changed?.Invoke();
        void OnChapterChanged(int previous, int current) => Changed?.Invoke();

        // ---------- Só no host ----------

        void AddPlayer(ulong clientId)
        {
            if (IndexOf(clientId) >= 0) return; // já está na lista
            players.Add(new LobbyPlayer { ClientId = clientId, Name = "...", Character = FirstFreeCharacter() });
        }

        void RemovePlayer(ulong clientId)
        {
            int i = IndexOf(clientId);
            if (i >= 0) players.RemoveAt(i);
        }

        [Rpc(SendTo.Server)]
        void SetNameRpc(FixedString32Bytes playerName, RpcParams rpcParams = default)
        {
            int i = IndexOf(rpcParams.Receive.SenderClientId);
            if (i < 0) return;
            var p = players[i];
            p.Name = playerName;
            players[i] = p; // struct: muda a cópia e devolve para a lista (isso dispara a sincronização)
        }

        public void RequestCharacter(int wanted) => RequestCharacterRpc(wanted);

        [Rpc(SendTo.Server)]
        void RequestCharacterRpc(int wanted, RpcParams rpcParams = default)
        {
            int i = IndexOf(rpcParams.Receive.SenderClientId);
            if (i < 0 || !CharacterRoster.Instance.IsValid(wanted) || IsTaken(wanted)) return; // o host recusa
            var p = players[i];
            p.Character = wanted;
            players[i] = p;
        }

        // O host navega pelos capítulos. Dá para VER qualquer um (até os bloqueados, para a equipe
        // saber o que vem pela frente), mas só começa os liberados no save DELE e com cena pronta.
        public void SelectChapter(int index)
        {
            if (!IsServer || Campaign.Instance.Get(index) == null) return;
            selectedChapter.Value = index;
        }

        public bool CanStart => Campaign.Instance.CanPlay(selectedChapter.Value);

        public void StartGame()
        {
            if (!IsServer || !CanStart) return;

            // Anota as escolhas antes de sair desta cena (este objeto vai ser destruído).
            ChosenCharacters.Clear();
            foreach (var p in players) ChosenCharacters[p.ClientId] = p.Character;
            GameStarted = true; // a partir daqui ninguém novo entra

            // Troca de cena EM REDE: o host carrega a fase, e o Netcode manda todos os
            // clientes carregarem junto. (Usar o SceneManager normal só trocaria a do host.)
            string scene = Campaign.Instance.Get(selectedChapter.Value).sceneName;
            NetworkManager.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        // Começa no capítulo mais adiantado que já dá para jogar (onde a equipe parou).
        static int DefaultChapter()
        {
            var campaign = Campaign.Instance;
            int best = 0;
            for (int i = 0; i < campaign.Count; i++)
                if (campaign.CanPlay(i)) best = i;
            return best;
        }

        // ---------- Ajudantes ----------

        int IndexOf(ulong clientId)
        {
            for (int i = 0; i < players.Count; i++)
                if (players[i].ClientId == clientId) return i;
            return -1;
        }

        public bool IsTaken(int character)
        {
            foreach (var p in players)
                if (p.Character == character) return true;
            return false;
        }

        int FirstFreeCharacter()
        {
            for (int c = 0; c < CharacterRoster.Instance.Count; c++)
                if (!IsTaken(c)) return c;
            return 0; // mais jogadores que personagens: repete o primeiro
        }
    }
}
