using Unity.Netcode;
using UnityEngine;

// Tela inicial: escolher entre HOSPEDAR a partida ou ENTRAR na de outra pessoa.
// Por enquanto só funciona na mesma máquina/rede local (endereço 127.0.0.1).
// A conexão pela internet (Relay + código de sala) vem numa etapa futura.
public class ConnectionMenu : MonoBehaviour
{
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

        var area = new Rect(Screen.width / 2f - 150, Screen.height / 2f - 70, 300, 140);
        GUILayout.BeginArea(area);
        GUILayout.Label("Mudança Assombrada — Multiplayer");
        // StartHost = servidor + jogador ao mesmo tempo (o computador dele é "a verdade").
        if (GUILayout.Button("Hospedar partida (Host)", GUILayout.Height(40))) net.StartHost();
        // StartClient = conecta no host. O host precisa ter clicado antes!
        if (GUILayout.Button("Entrar na partida (Cliente)", GUILayout.Height(40))) net.StartClient();
        GUILayout.EndArea();
    }
}
