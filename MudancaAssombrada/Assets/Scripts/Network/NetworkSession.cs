using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fica no MESMO objeto do NetworkManager. O NetworkManager se marca como
// "DontDestroyOnLoad": ele (e este script) sobrevivem à troca de cena Menu -> Fase.
// Por isso é o lugar certo para regras que valem o jogo inteiro:
//  - quem pode entrar na partida (no máximo 3, e só antes de a fase começar)
//  - o que fazer quando a conexão cai (voltar para o menu com uma mensagem)
[RequireComponent(typeof(NetworkManager))]
public class NetworkSession : MonoBehaviour
{
    public const int MaxPlayers = 3; // Cida, Tiago e Marcelo
    public const ushort Port = 7777;
    public const string MenuScene = "Menu";
    public const string GameScene = "Prototipo1";

    // Mensagem para o menu mostrar depois de uma desconexão (null = nenhuma).
    public static string LastError { get; set; }

    // Avisa o menu que a conexão falhou ou caiu (para ele trocar de painel).
    public static event Action Disconnected;

    NetworkManager net;

    // Valores "static" sobrevivem entre um Play e outro no editor quando a Unity está configurada
    // para entrar no Play rápido (sem recarregar os scripts). Este atributo faz a Unity chamar
    // a função no começo de todo Play, e aqui zeramos tudo.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        LastError = null;
        Disconnected = null;
        LobbyManager.ResetSession();
    }

    void Awake()
    {
        net = GetComponent<NetworkManager>();
        // Quem decide se um jogador pode entrar é a função ApproveConnection, lá embaixo.
        net.ConnectionApprovalCallback = ApproveConnection;
        net.OnClientDisconnectCallback += OnClientDisconnect;
    }

    void OnDestroy()
    {
        if (net != null) net.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public void Host()
    {
        LastError = null;
        LobbyManager.ResetSession(); // partida nova: esquece a anterior
        // "0.0.0.0" = aceitar conexões vindas de QUALQUER placa de rede deste PC
        // (incluindo a placa virtual do Radmin). O padrão aceita só o próprio PC.
        net.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", Port, "0.0.0.0");
        net.StartHost();
    }

    public void Join(string address)
    {
        LastError = null;
        net.GetComponent<UnityTransport>().SetConnectionData(address.Trim(), Port);
        if (!net.StartClient()) Fail("Endereço inválido.");
    }

    public void Leave() => net.Shutdown();

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
            response.Reason = "A sala está cheia (máximo de 3 jogadores).";
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
        if (net.IsServer || clientId != net.LocalClientId) return;

        // DisconnectReason = o motivo que o host mandou (ex.: "sala cheia"). Vazio = sem resposta.
        string reason = string.IsNullOrEmpty(net.DisconnectReason)
            ? "Não foi possível falar com o host. Confira o IP e se o Radmin está ligado."
            : net.DisconnectReason;
        Fail(reason);
    }

    void Fail(string message)
    {
        LastError = message;
        if (net.IsListening) net.Shutdown();

        // Se eu estava na fase, volto para o menu. Se já estou no menu, ele mostra o erro.
        if (SceneManager.GetActiveScene().name != MenuScene)
            SceneManager.LoadScene(MenuScene);
        else
            Disconnected?.Invoke();
    }
}
