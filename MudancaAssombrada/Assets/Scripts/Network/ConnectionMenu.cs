using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Tela inicial: escolher entre HOSPEDAR a partida ou ENTRAR na de outra pessoa.
//
// Funciona no mesmo PC (127.0.0.1), na mesma rede local, ou por uma VPN de "rede local virtual"
// como o Radmin VPN: o cliente digita o IP do host (no Radmin, o IP 26.x.x.x que aparece nele).
// A conexão pela internet sem VPN (Relay + código de sala) vem numa etapa futura.
public class ConnectionMenu : MonoBehaviour
{
    const ushort Port = 7777; // a "porta" é como o número do apartamento dentro do endereço (IP)

    string hostAddress = "127.0.0.1";

    void OnGUI()
    {
        var net = NetworkManager.Singleton;
        if (net == null) return;

        // Já conectado? Só mostra quem somos, no canto.
        if (net.IsClient || net.IsServer)
        {
            string role = net.IsHost ? "Host" : "Cliente";
            GUI.Label(new Rect(20, 90, 400, 30), $"{role} — jogadores: {(net.IsServer ? net.ConnectedClients.Count.ToString() : "?")}");
            return;
        }

        Cursor.lockState = CursorLockMode.None; // solta o mouse para clicar nos botões
        var transport = net.GetComponent<UnityTransport>();

        var area = new Rect(Screen.width / 2f - 160, Screen.height / 2f - 110, 320, 220);
        GUILayout.BeginArea(area);
        GUILayout.Label("Mudança Assombrada — Multiplayer");

        if (GUILayout.Button("Hospedar partida (Host)", GUILayout.Height(40)))
        {
            // "0.0.0.0" = aceitar conexões vindas de QUALQUER placa de rede deste PC
            // (incluindo a placa virtual do Radmin). O padrão aceita só o próprio PC.
            transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            net.StartHost();
        }

        GUILayout.Space(10);
        GUILayout.Label("IP do host (no Radmin: o IP 26.x.x.x dele):");
        hostAddress = GUILayout.TextField(hostAddress);
        if (GUILayout.Button("Entrar na partida (Cliente)", GUILayout.Height(40)))
        {
            transport.SetConnectionData(hostAddress.Trim(), Port);
            net.StartClient();
        }
        GUILayout.EndArea();
    }
}
