using Unity.Netcode;
using UnityEngine;

// Fica no prefab do Player. Cada jogador conectado ganha uma CÓPIA deste prefab,
// e TODAS as cópias existem em TODOS os computadores.
//
// Exemplo com 2 jogadores, no computador do jogador A:
//   - Player do A  -> IsOwner = true  (é o MEU boneco: teclado, câmera e visão ligados)
//   - Player do B  -> IsOwner = false (é um "fantoche": só mostra onde o B está)
//
// NetworkBehaviour = MonoBehaviour com superpoderes de rede (IsOwner, IsServer, RPCs...).
public class NetworkPlayer : NetworkBehaviour
{
    // Scripts que só fazem sentido no MEU boneco: leem o MEU teclado ou mexem na MINHA tela.
    [SerializeField] Behaviour[] ownerOnly;

    // Chamado quando o objeto "nasce" na rede (como um Start, mas já sabendo quem é o dono).
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Cada jogador nasce um pouco ao lado do anterior, para não nascerem um dentro do outro.
            // O CharacterController "segura" a posição, então desligamos ele um instante.
            var controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position += Vector3.right * (OwnerClientId * 1.5f);
            controller.enabled = true;
            spawnPosition = transform.position; // para o reinício da fase

            // A câmera da cena de espera não é mais necessária: agora vemos pelos nossos olhos.
            var lobbyCamera = GameObject.Find("Câmera de Espera");
            if (lobbyCamera != null) lobbyCamera.SetActive(false);

            // Meu próprio boneco some da minha tela (a câmera fica dentro da cabeça).
            // Os outros jogadores continuam vendo ele normalmente.
            GetComponentInChildren<DollWalk>().HideBody();
            return;
        }

        // Não é meu: desliga o "cérebro" (scripts de input/visão) e os "olhos e ouvidos" (câmera).
        // Como o OnNetworkSpawn roda antes do Start, os Start desses scripts nunca chegam a rodar.
        foreach (var behaviour in ownerOnly)
            behaviour.enabled = false;

        var cam = GetComponentInChildren<Camera>();
        cam.enabled = false;
        // Só pode existir 1 AudioListener ligado por cena (são os "ouvidos" do jogador local).
        cam.GetComponent<AudioListener>().enabled = false;
        // A câmera do fantoche não pode ser "a câmera principal" do meu computador.
        cam.tag = "Untagged";
    }

    Vector3 spawnPosition;

    // O host pede, mas quem move é o DONO do boneco (a posição dos jogadores é
    // autoridade de cada um). SendTo.Owner: roda só no computador do dono deste boneco.
    [Rpc(SendTo.Owner)]
    public void ResetPositionRpc()
    {
        var controller = GetComponent<CharacterController>();
        controller.enabled = false;
        // Teleport em vez de mudar a posição: os outros veem o boneco "pular", não deslizar.
        GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(spawnPosition, transform.rotation, transform.localScale);
        controller.enabled = true;
    }
}
