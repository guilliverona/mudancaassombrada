using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fica na cena da fase. Cria o boneco de cada jogador quando a fase termina de carregar
// em TODOS os computadores.
//
// Antes do menu, o Netcode criava o boneco sozinho assim que alguém conectava.
// Agora a conexão acontece no menu (onde não pode ter boneco andando), então
// desligamos isso e criamos os bonecos aqui, na hora certa.
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;

    // ORDEM IMPORTA: a inscrição no evento fica no Awake, e não no Start.
    // Jogando sozinho, o Netcode avisa "todos carregaram" na hora em que a cena termina de
    // carregar, ANTES do Start rodar (o Start só roda no frame seguinte). Inscrevendo no Start,
    // o aviso já tinha passado e ninguém nascia. O Awake roda assim que a cena é ativada.
    void Awake()
    {
        var net = NetworkManager.Singleton;
        if (net != null && net.IsServer) net.SceneManager.OnLoadEventCompleted += OnLoadCompleted;
    }

    void Start()
    {
        var net = NetworkManager.Singleton;

        // Apertou Play direto nesta cena, sem passar pelo menu? Não existe partida: vai para o menu.
        if (net == null || !net.IsListening) SceneManager.LoadScene(NetworkSession.MenuScene);
    }

    void OnDestroy()
    {
        var net = NetworkManager.Singleton;
        if (net != null && net.SceneManager != null) net.SceneManager.OnLoadEventCompleted -= OnLoadCompleted;
    }

    // Só no host: chamado quando todos terminaram de carregar a cena.
    void OnLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameObject.scene.name) return;

        foreach (ulong clientId in clientsCompleted)
        {
            var player = Instantiate(playerPrefab);
            // SpawnAsPlayerObject: "nasça na rede e pertença a este jogador" (ele vira o dono).
            // destroyWithScene: true = some junto com a fase, se a cena trocar.
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }
    }
}
