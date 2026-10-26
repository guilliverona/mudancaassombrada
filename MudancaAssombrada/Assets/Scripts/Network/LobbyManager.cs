using System;
using System.Collections.Generic;
using Unity.Collections; // FixedString32Bytes
using Unity.Netcode;
using UnityEngine;
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
        public bool Ready;    // marcou "Estou pronto"
        // A conta (anônima) do jogador nos serviços da Unity. Serve para expulsar da sala online
        // e para não deixar quem foi expulso voltar.
        public FixedString64Bytes PlayerId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Character);
            serializer.SerializeValue(ref Ready);
            serializer.SerializeValue(ref PlayerId);
        }

        public bool Equals(LobbyPlayer other) =>
            ClientId == other.ClientId && Name.Equals(other.Name) && Character == other.Character
            && Ready == other.Ready && PlayerId.Equals(other.PlayerId);
    }

    // A sala de espera: quem está conectado, com que nome e qual personagem, quem é o líder,
    // quem está pronto e o chat. Fica na cena Menu, num objeto com NetworkObject.
    //
    // HOST x LÍDER: o host é o computador que "segura" a partida (o servidor). Isso não dá para
    // trocar sem derrubar todo mundo. O LÍDER é quem manda na sala: escolhe o capítulo, começa,
    // expulsa e passa a liderança. Começa sendo o host, mas pode ser qualquer um.
    //
    // Regra de ouro: quem pede é o cliente (Rpc para o servidor); quem CONFERE e decide é o host.
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        // Guardados no host quando a fase começa. São "static" porque este objeto é destruído
        // na troca de cena, mas o CharacterSwitcher (na fase) ainda precisa saber quem escolheu o quê.
        // Chave: o jogador. Valor: a posição do personagem no Elenco.
        public static readonly Dictionary<ulong, int> ChosenCharacters = new();
        public static bool GameStarted { get; private set; }

        // Contas expulsas desta partida (só no host): não entram de novo.
        static readonly HashSet<string> Banned = new();

        // Esquece a partida anterior (chamado ao hospedar e no começo de cada Play).
        public static void ResetSession()
        {
            GameStarted = false;
            ChosenCharacters.Clear();
            Banned.Clear();
        }

        public const float CountdownSeconds = 3f;
        public const int ChatMaxLength = 120;   // letras por mensagem (o campo do menu também limita)
        const int ChatHistory = 40;             // mensagens guardadas
        const float ChatCooldown = 0.5f;        // anti-flood: intervalo mínimo entre mensagens

        // O menu escuta estes eventos para redesenhar a tela.
        public event Action Changed;
        public event Action ChatChanged;

        // NetworkList = uma lista que a rede mantém igual em todos os computadores.
        // Precisa ser criada no Awake (e não na declaração), regra do Netcode.
        NetworkList<LobbyPlayer> players;

        // O capítulo escolhido pelo líder (posição na Campanha). Todos veem, só o host muda.
        readonly NetworkVariable<int> selectedChapter = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        readonly NetworkVariable<ulong> leader = new(
            Unity.Netcode.NetworkManager.ServerClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Hora (relógio do servidor) em que a partida começa. 0 = sem contagem.
        readonly NetworkVariable<double> startAt = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Sala privada = some da lista de salas; só entra quem tem o código.
        readonly NetworkVariable<bool> isPrivate = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // O chat é local (cada um guarda o que recebeu). Quem entra depois não vê o que veio antes.
        readonly List<string> chat = new();
        readonly Dictionary<ulong, float> lastChatTime = new(); // só no host
        readonly HashSet<ulong> kicked = new();                 // só no host: para não avisar "saiu" depois de "expulso"

        public int SelectedChapter => selectedChapter.Value;
        public ulong Leader => leader.Value;
        public bool IsPrivate => isPrivate.Value;
        public IReadOnlyList<string> Chat => chat;
        public bool IsLeader(ulong clientId) => leader.Value == clientId;
        public bool AmLeader => IsSpawned && leader.Value == NetworkManager.LocalClientId;

        // Segundos que faltam para começar (null = sem contagem).
        public float? Countdown => startAt.Value > 0
            ? Mathf.Max(0f, (float)(startAt.Value - NetworkManager.ServerTime.Time))
            : null;

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
            selectedChapter.OnValueChanged += OnIntChanged;
            leader.OnValueChanged += OnUlongChanged;
            startAt.OnValueChanged += OnDoubleChanged;
            isPrivate.OnValueChanged += OnBoolChanged;
            chat.Clear();

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddPlayer;
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;
                leader.Value = NetworkManager.ServerClientId;
                startAt.Value = 0;
                isPrivate.Value = false;
                AddPlayer(NetworkManager.LocalClientId); // o próprio host
                selectedChapter.Value = DefaultChapter();
            }

            // Cada um conta ao host o nome que digitou no menu e a sua conta.
            SetNameRpc(GameSettings.PlayerName, NetworkSession.MyPlayerId);
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            players.OnListChanged -= OnPlayersChanged;
            selectedChapter.OnValueChanged -= OnIntChanged;
            leader.OnValueChanged -= OnUlongChanged;
            startAt.OnValueChanged -= OnDoubleChanged;
            isPrivate.OnValueChanged -= OnBoolChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddPlayer;
                NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
            }
            Changed?.Invoke();
        }

        void OnPlayersChanged(NetworkListEvent<LobbyPlayer> change) => Changed?.Invoke();
        void OnIntChanged(int previous, int current) => Changed?.Invoke();
        void OnUlongChanged(ulong previous, ulong current) => Changed?.Invoke();
        void OnDoubleChanged(double previous, double current) => Changed?.Invoke();
        void OnBoolChanged(bool previous, bool current) => Changed?.Invoke();

        void Update()
        {
            // A contagem é conferida no host a cada quadro: se alguém desmarcar "pronto"
            // ou sair no meio, ela é cancelada.
            if (!IsServer || startAt.Value <= 0) return;
            if (!CanStart)
            {
                startAt.Value = 0;
                SystemMessage("Contagem cancelada.");
            }
            else if (NetworkManager.ServerTime.Time >= startAt.Value)
            {
                startAt.Value = 0;
                StartGame();
            }
        }

        // ---------- Pedidos dos jogadores (o menu chama estes) ----------

        public void RequestCharacter(int wanted) => RequestCharacterRpc(wanted);
        public void SetReady(bool ready) => SetReadyRpc(ready);
        public void SelectChapter(int index) => SelectChapterRpc(index);
        public void RequestStart() => RequestStartRpc();
        public void CancelStart() => CancelStartRpc();
        public void Kick(ulong clientId) => KickRpc(clientId);
        public void MakeLeader(ulong clientId) => MakeLeaderRpc(clientId);
        public void SetPrivate(bool value) => SetPrivateRpc(value);

        public void SendChat(string text)
        {
            text = text.Trim();
            if (text.Length == 0) return;
            if (text.Length > ChatMaxLength) text = text.Substring(0, ChatMaxLength);
            SendChatRpc(text);
        }

        // ---------- Só no host ----------

        void AddPlayer(ulong clientId)
        {
            if (IndexOf(clientId) >= 0) return; // já está na lista
            players.Add(new LobbyPlayer { ClientId = clientId, Name = "...", Character = FirstFreeCharacter() });
        }

        void RemovePlayer(ulong clientId)
        {
            int i = IndexOf(clientId);
            if (i < 0) return;
            string name = players[i].Name.ToString();
            players.RemoveAt(i);
            lastChatTime.Remove(clientId);
            if (!kicked.Remove(clientId)) SystemMessage($"{name} saiu da sala.");

            // O líder saiu: a liderança volta para o host.
            if (leader.Value == clientId)
            {
                leader.Value = NetworkManager.ServerClientId;
                int h = IndexOf(NetworkManager.ServerClientId);
                if (h >= 0) SystemMessage($"{players[h].Name} agora é o líder.");
            }
        }

        // Só o líder pode pedir isto (e o host confere).
        bool FromLeader(RpcParams rpcParams) => rpcParams.Receive.SenderClientId == leader.Value;

        [Rpc(SendTo.Server)]
        void SetNameRpc(FixedString32Bytes playerName, FixedString64Bytes playerId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            int i = IndexOf(sender);
            if (i < 0) return;

            // Foi expulso antes e tentou voltar (pela lista ou pelo código)? Não entra.
            if (sender != NetworkManager.ServerClientId && Banned.Contains(playerId.ToString()))
            {
                NetworkManager.DisconnectClient(sender, "Você foi expulso desta sala.");
                NetworkSession.RemoveFromRoom(playerId.ToString());
                return;
            }

            var p = players[i];
            p.Name = playerName;
            p.PlayerId = playerId;
            players[i] = p; // struct: muda a cópia e devolve para a lista (isso dispara a sincronização)
            SystemMessage($"{playerName} entrou na sala.");
        }

        [Rpc(SendTo.Server)]
        void RequestCharacterRpc(int wanted, RpcParams rpcParams = default)
        {
            int i = IndexOf(rpcParams.Receive.SenderClientId);
            // Quem já está pronto não troca de personagem (tem que desmarcar antes).
            if (i < 0 || players[i].Ready || !CharacterRoster.Instance.IsValid(wanted) || IsTaken(wanted)) return;
            var p = players[i];
            p.Character = wanted;
            players[i] = p;
        }

        [Rpc(SendTo.Server)]
        void SetReadyRpc(bool ready, RpcParams rpcParams = default)
        {
            int i = IndexOf(rpcParams.Receive.SenderClientId);
            if (i < 0 || players[i].Ready == ready) return;
            var p = players[i];
            p.Ready = ready;
            players[i] = p;
        }

        // O líder navega pelos capítulos. Dá para VER qualquer um (até os bloqueados, para a equipe
        // saber o que vem pela frente), mas só começa os liberados no save do HOST e com cena pronta.
        [Rpc(SendTo.Server)]
        void SelectChapterRpc(int index, RpcParams rpcParams = default)
        {
            if (!FromLeader(rpcParams) || startAt.Value > 0 || Campaign.Instance.Get(index) == null) return;
            if (selectedChapter.Value == index) return;
            selectedChapter.Value = index;
            ClearReady(); // mudou o capítulo: todo mundo confirma de novo
        }

        [Rpc(SendTo.Server)]
        void RequestStartRpc(RpcParams rpcParams = default)
        {
            if (!FromLeader(rpcParams) || !CanStart || startAt.Value > 0) return;
            startAt.Value = NetworkManager.ServerTime.Time + CountdownSeconds;
            SystemMessage($"A partida começa em {CountdownSeconds:0} segundos!");
        }

        [Rpc(SendTo.Server)]
        void CancelStartRpc(RpcParams rpcParams = default)
        {
            if (!FromLeader(rpcParams) || startAt.Value <= 0) return;
            startAt.Value = 0;
            SystemMessage("O líder cancelou a contagem.");
        }

        [Rpc(SendTo.Server)]
        void KickRpc(ulong target, RpcParams rpcParams = default)
        {
            // Ninguém expulsa a si mesmo nem o host (sem o host, a partida acaba para todos).
            if (!FromLeader(rpcParams) || target == rpcParams.Receive.SenderClientId
                || target == NetworkManager.ServerClientId) return;
            int i = IndexOf(target);
            if (i < 0) return;

            var p = players[i];
            string id = p.PlayerId.ToString();
            if (id.Length > 0) Banned.Add(id);
            SystemMessage($"{p.Name} foi expulso da sala.");
            kicked.Add(target);
            NetworkManager.DisconnectClient(target, "Você foi expulso da sala.");
            NetworkSession.RemoveFromRoom(id); // tira também da sala online (libera a vaga na lista)
        }

        [Rpc(SendTo.Server)]
        void MakeLeaderRpc(ulong target, RpcParams rpcParams = default)
        {
            if (!FromLeader(rpcParams) || target == leader.Value || startAt.Value > 0) return;
            int i = IndexOf(target);
            if (i < 0) return;
            leader.Value = target;
            // O novo líder não precisa marcar "pronto"; desmarca para a lista não confundir.
            var p = players[i];
            p.Ready = false;
            players[i] = p;
            SystemMessage($"{p.Name} agora é o líder.");
        }

        [Rpc(SendTo.Server)]
        void SetPrivateRpc(bool value, RpcParams rpcParams = default)
        {
            if (!FromLeader(rpcParams) || isPrivate.Value == value) return;
            isPrivate.Value = value;
            NetworkSession.SetRoomPrivate(value);
            SystemMessage(value ? "A sala agora é privada (só entra com o código)." : "A sala agora é pública.");
        }

        [Rpc(SendTo.Server)]
        void SendChatRpc(FixedString512Bytes text, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            int i = IndexOf(sender);
            if (i < 0) return;
            if (lastChatTime.TryGetValue(sender, out float last) && Time.unscaledTime - last < ChatCooldown) return;
            lastChatTime[sender] = Time.unscaledTime;

            // O texto é do jogador: tiramos o "<" para ninguém conseguir mexer nas cores (rich text).
            string clean = text.ToString().Replace("<", "‹").Trim();
            if (clean.Length == 0) return;
            if (clean.Length > ChatMaxLength) clean = clean.Substring(0, ChatMaxLength);

            var character = CharacterRoster.Instance.Get(players[i].Character);
            string color = ColorUtility.ToHtmlStringRGB(Theme.Muted(character.color));
            string author = players[i].Name.ToString().Replace("<", "‹");
            ChatRpc(Fit($"<color=#{color}>{author}:</color> {clean}"));
        }

        // Mensagem do sistema no chat (entrou, saiu, expulso...). Só o host manda.
        void SystemMessage(string text)
        {
            string color = ColorUtility.ToHtmlStringRGB(Theme.MutedColor);
            ChatRpc(Fit($"<color=#{color}><i>{text.Replace("<", "‹")}</i></color>"));
        }

        // O FixedString512Bytes guarda no máximo ~500 BYTES (letra com acento ou emoji ocupa mais
        // de um). Converter texto maior direto daria erro; o CopyFromTruncated corta o excesso.
        static FixedString512Bytes Fit(string line)
        {
            var fixedLine = new FixedString512Bytes();
            fixedLine.CopyFromTruncated(line);
            return fixedLine;
        }

        // SendTo.Everyone: roda em todos os computadores, inclusive no do host.
        [Rpc(SendTo.Everyone)]
        void ChatRpc(FixedString512Bytes line)
        {
            chat.Add(line.ToString());
            if (chat.Count > ChatHistory) chat.RemoveAt(0);
            ChatChanged?.Invoke();
        }

        void ClearReady()
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].Ready) continue;
                var p = players[i];
                p.Ready = false;
                players[i] = p;
            }
        }

        // Todos prontos? (o líder não precisa marcar)
        public bool AllReady
        {
            get
            {
                foreach (var p in players)
                    if (p.ClientId != leader.Value && !p.Ready) return false;
                return true;
            }
        }

        public bool ChapterPlayable => Campaign.Instance.CanPlay(selectedChapter.Value);
        public bool CanStart => ChapterPlayable && AllReady;

        void StartGame()
        {
            if (!IsServer || !CanStart) return;

            // Anota as escolhas antes de sair desta cena (este objeto vai ser destruído).
            ChosenCharacters.Clear();
            foreach (var p in players) ChosenCharacters[p.ClientId] = p.Character;
            GameStarted = true; // a partir daqui ninguém novo entra
            NetworkSession.LockRoom(); // e a sala some da lista de salas online

            // Todos cobrem a tela ANTES de a cena trocar (a mensagem chega antes da troca).
            ShowLoadingRpc(selectedChapter.Value);

            // Troca de cena EM REDE: o host carrega a fase, e o Netcode manda todos os
            // clientes carregarem junto. (Usar o SceneManager normal só trocaria a do host.)
            string scene = Campaign.Instance.Get(selectedChapter.Value).sceneName;
            NetworkManager.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        [Rpc(SendTo.Everyone)]
        void ShowLoadingRpc(int chapter)
        {
            var c = Campaign.Instance.Get(chapter);
            LoadingScreen.Show(c != null ? c.title : "");
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
