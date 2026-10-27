using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MudancaAssombrada
{
    // Fica no MESMO objeto do NetworkManager. O NetworkManager se marca como
    // "DontDestroyOnLoad": ele (e este script) sobrevivem à troca de cena Menu -> Fase.
    // Por isso é o lugar certo para regras que valem o jogo inteiro:
    //  - criar, listar e entrar nas salas online
    //  - quem pode entrar na partida (no máximo MaxPlayers, e só antes de a fase começar)
    //  - o que fazer quando a conexão cai (voltar para o menu com uma mensagem)
    //
    // Como funciona a partida pela internet (serviços da Unity, pacote Multiplayer Services):
    //  - Lobby: a "vitrine" de salas. O host publica a sala lá; os outros veem a lista
    //    (ou digitam o código da sala) e entram.
    //  - Relay: um servidor da Unity no meio do caminho que repassa os pacotes do jogo.
    //    Assim ninguém precisa abrir porta no roteador nem saber o IP de ninguém.
    // O pacote junta os dois numa "Session" e ainda liga o NetworkManager sozinho
    // (StartHost no host, StartClient em quem entra).
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkSession : MonoBehaviour
    {
        public const int MaxPlayers = 3;
        public const string MenuScene = "Menu"; // as cenas das fases vêm da Campanha

        // Mensagem para o menu mostrar depois de uma desconexão (null = nenhuma).
        public static string LastError { get; set; }

        // Avisa o menu que a conexão falhou ou caiu (para ele trocar de painel).
        public static event Action Disconnected;

        // A sala em que estou agora (null = nenhuma). "static" para o menu achar fácil.
        static ISession current;

        // Código curto da sala (ex.: "K7Q2XM"), para passar aos amigos. null = sem sala.
        public static string RoomCode => current?.Code;

        NetworkManager net;
        bool leaving;      // eu mesmo pedi para sair (não é erro)
        bool wasConnected; // cheguei a entrar na partida (para escolher a mensagem certa se cair)

        // Valores "static" sobrevivem entre um Play e outro no editor quando a Unity está configurada
        // para entrar no Play rápido (sem recarregar os scripts). Este atributo faz a Unity chamar
        // a função no começo de todo Play, e aqui zeramos tudo.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            LastError = null;
            Disconnected = null;
            current = null;
            pending = published = null;
            CreatedPrivate = false;
            LobbyManager.ResetSession();
        }

        void Awake()
        {
            net = GetComponent<NetworkManager>();
            // Quem decide se um jogador pode entrar é a função ApproveConnection, lá embaixo.
            net.ConnectionApprovalCallback = ApproveConnection;
            net.OnClientDisconnectCallback += OnClientDisconnect;
            net.OnClientConnectedCallback += OnClientConnected;
        }

        void OnDestroy()
        {
            if (net == null) return;
            net.OnClientDisconnectCallback -= OnClientDisconnect;
            net.OnClientConnectedCallback -= OnClientConnected;
        }

        // ---------- Login ----------

        // Liga os serviços da Unity e faz o login anônimo (só na primeira vez).
        // Anônimo = o jogador não digita nada; a Unity cria uma conta invisível para este PC.
        static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // A minha conta nos serviços da Unity ("" se ainda não fez login).
        public static string MyPlayerId =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId : "";

        // ---------- Salas ----------
        // "async" = a função espera a resposta dos servidores da Unity sem travar o jogo.

        // As salas que ainda não começaram, para a tela "Entrar numa sala" (as cheias também:
        // aparecem apagadas, com "Cheia"). O que o host publicou (líder, capítulo, personagens)
        // vem nas propriedades da sala; a ordem e a busca ficam no RoomBrowserRules.
        public static async Task<List<RoomListing>> ListRooms()
        {
            await SignInAsync();
            var result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions { Count = 40 });
            var rooms = new List<RoomListing>();
            foreach (var room in result.Sessions)
            {
                if (room.IsLocked) continue;
                rooms.Add(new RoomListing
                {
                    Id = room.Id,
                    Name = room.Name,
                    Leader = Property(room, RoomBrowserRules.LeaderKey),
                    Chapter = RoomBrowserRules.ParseChapter(Property(room, RoomBrowserRules.ChapterKey)),
                    Characters = RoomBrowserRules.DecodeCharacters(Property(room, RoomBrowserRules.CharactersKey)),
                    Players = room.MaxPlayers - room.AvailableSlots,
                    MaxPlayers = room.MaxPlayers,
                });
            }
            return rooms;
        }

        static string Property(ISessionInfo room, string key) =>
            room.Properties != null && room.Properties.TryGetValue(key, out var property) ? property.Value : "";

        // A sala que estou criando é privada? O LobbyManager lê isto quando a sala de espera abre.
        public static bool CreatedPrivate { get; private set; }

        public async Task CreateRoom(string roomName, bool isPrivate)
        {
            BeginSession();
            LobbyManager.ResetSession(); // partida nova: esquece a anterior
            CreatedPrivate = isPrivate;
            published = null;
            try
            {
                await SignInAsync();
                var options = new SessionOptions { Name = roomName, MaxPlayers = MaxPlayers, IsPrivate = isPrivate }.WithRelayNetwork();
                // O líder já aparece na lista desde o começo; capítulo e personagens chegam pelo PublishRoomInfo.
                options.SessionProperties[RoomBrowserRules.LeaderKey] = new SessionProperty(GameSettings.PlayerName);
                current = await MultiplayerService.Instance.CreateSessionAsync(options);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Fail("Não foi possível criar a sala. Confira a internet.");
            }
        }

        public Task JoinRoom(string roomId) =>
            Join(() => MultiplayerService.Instance.JoinSessionByIdAsync(roomId), "A sala não existe mais ou está cheia.");

        public Task JoinByCode(string code) =>
            Join(() => MultiplayerService.Instance.JoinSessionByCodeAsync(RoomBrowserRules.CleanCode(code)),
                 "Código inválido, ou a sala está cheia ou já começou.");

        async Task Join(Func<Task<ISession>> join, string errorMessage)
        {
            BeginSession();
            try
            {
                await SignInAsync();
                current = await join();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Fail(errorMessage);
            }
        }

        // Chamado pelo host quando a fase começa: a sala some da lista e ninguém mais entra.
        public static async void LockRoom()
        {
            if (current == null || !current.IsHost) return;
            try
            {
                var host = current.AsHost();
                host.IsLocked = true;
                await host.SavePropertiesAsync();
            }
            catch (Exception e) { Debug.LogException(e); } // não é grave: o ApproveConnection também barra
        }

        // Só no host: tira um jogador da sala online (expulsão). Sem isso a vaga continuaria
        // ocupada na lista de salas.
        public static async void RemoveFromRoom(string playerId)
        {
            if (current == null || !current.IsHost || string.IsNullOrEmpty(playerId)) return;
            try { await current.AsHost().RemovePlayerAsync(playerId); }
            catch (Exception e) { Debug.LogWarning("Erro ao tirar jogador da sala: " + e.Message); }
        }

        // Só no host: sala privada some da lista (só entra quem tem o código).
        public static async void SetRoomPrivate(bool value)
        {
            if (current == null || !current.IsHost) return;
            try
            {
                var host = current.AsHost();
                host.IsPrivate = value;
                await host.SavePropertiesAsync();
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ---------- O que a lista de salas mostra sobre a minha sala (só no host) ----------
        // O LobbyManager chama sempre que algo muda; aqui juntamos as mudanças e mandamos no
        // máximo uma vez a cada PublishInterval segundos (o Lobby da Unity limita os pedidos).

        const float PublishInterval = 2f;
        static Dictionary<string, string> pending;   // o que falta mandar (null = nada)
        static Dictionary<string, string> published; // o último que foi mandado
        float nextPublish;
        bool publishing;

        public static void PublishRoomInfo(string leaderName, int chapter, string characters)
        {
            if (current == null || !current.IsHost) return;
            var info = new Dictionary<string, string>
            {
                [RoomBrowserRules.LeaderKey] = leaderName,
                [RoomBrowserRules.ChapterKey] = chapter.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [RoomBrowserRules.CharactersKey] = characters,
            };
            if (published != null && SameInfo(info, published)) { pending = null; return; }
            pending = info;
        }

        static bool SameInfo(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            foreach (var pair in a)
                if (!b.TryGetValue(pair.Key, out var value) || value != pair.Value) return false;
            return true;
        }

        void Update()
        {
            if (pending == null || publishing || Time.unscaledTime < nextPublish) return;
            SendRoomInfo();
        }

        async void SendRoomInfo()
        {
            var info = pending;
            pending = null;
            if (current == null || !current.IsHost) return;
            publishing = true;
            nextPublish = Time.unscaledTime + PublishInterval;
            try
            {
                var host = current.AsHost();
                var properties = new Dictionary<string, SessionProperty>();
                foreach (var pair in info) properties[pair.Key] = new SessionProperty(pair.Value);
                host.SetProperties(properties);
                await host.SavePropertiesAsync();
                published = info;
            }
            catch (Exception e) { Debug.LogWarning("Erro ao atualizar a sala na lista: " + e.Message); }
            finally { publishing = false; }
        }

        void BeginSession()
        {
            LastError = null;
            leaving = false;
            wasConnected = false;
        }

        public void Leave()
        {
            leaving = true; // saída de propósito: o OnClientDisconnect não deve tratar como erro
            net.Shutdown();
            LeaveRoom();
        }

        // Sai da sala no Lobby. Se eu for o host, apago a sala (para não ficar "fantasma" na lista).
        static async void LeaveRoom()
        {
            var room = current;
            current = null;
            if (room == null) return;
            try
            {
                if (room.IsHost) await room.AsHost().DeleteAsync();
                else await room.LeaveAsync();
            }
            catch (Exception e) { Debug.LogWarning("Erro ao sair da sala: " + e.Message); }
        }

        // Pela pausa: sai da partida e volta para o menu. Se eu for o host, a partida acaba
        // para todos (os clientes caem e voltam ao menu com o aviso de que o host saiu).
        public void ReturnToMenu()
        {
            LastError = null;
            Leave();
            SceneManager.LoadScene(MenuScene); // troca só a MINHA cena (não é troca em rede)
        }

        // Roda NO HOST toda vez que alguém tenta entrar (inclusive o próprio host).
        void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // Os jogadores só nascem quando a fase começar (o PlayerSpawner cuida disso).
            response.CreatePlayerObject = false;

            // O próprio host sempre entra (o Netcode nem deixa recusar).
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                response.Approved = true;
                return;
            }

            if (LobbyManager.GameStarted)
            {
                response.Approved = false;
                response.Reason = "A partida já começou.";
            }
            else if (net.ConnectedClientsIds.Count >= MaxPlayers)
            {
                response.Approved = false;
                response.Reason = $"A sala está cheia (máximo de {MaxPlayers} jogadores).";
            }
            else
            {
                response.Approved = true;
            }
        }

        void OnClientDisconnect(ulong clientId)
        {
            // No host, isto dispara quando OUTRO jogador sai: o lobby cuida disso.
            // Aqui só interessa quando EU (cliente) caí ou fui recusado.
            if (net.IsServer || clientId != net.LocalClientId || leaving) return;

            // DisconnectReason = o motivo que o host mandou (ex.: "sala cheia").
            // Vazio = o host não disse nada: ou nunca respondeu, ou a conexão caiu no meio.
            string reason;
            if (!string.IsNullOrEmpty(net.DisconnectReason)) reason = net.DisconnectReason;
            else if (wasConnected) reason = "A conexão com o host caiu (ou ele saiu da partida).";
            else reason = "Não foi possível falar com o host.";
            Fail(reason);
        }

        void OnClientConnected(ulong clientId)
        {
            if (clientId == net.LocalClientId) wasConnected = true;
        }

        void Fail(string message)
        {
            LastError = message;
            leaving = true; // daqui em diante a queda já foi tratada (evita avisar duas vezes)
            if (net.IsListening) net.Shutdown();
            LeaveRoom();

            // Se eu estava na fase, volto para o menu. Se já estou no menu, ele mostra o erro.
            if (SceneManager.GetActiveScene().name != MenuScene)
                SceneManager.LoadScene(MenuScene);
            else
                Disconnected?.Invoke();
        }
    }
}
