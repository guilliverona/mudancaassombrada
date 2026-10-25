using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MudancaAssombrada
{
    // Fica no prefab do Player. Cada jogador conectado ganha uma CÓPIA deste prefab,
    // e TODAS as cópias existem em TODOS os computadores.
    //
    // Exemplo com 2 jogadores, no computador do jogador A:
    //   - Player do A  -> IsOwner = true  (é o MEU boneco: teclado, câmera e visão ligados)
    //   - Player do B  -> IsOwner = false (é um "fantoche": só mostra onde o B está)
    //
    // Também é o REGISTRO dos jogadores: quem precisa de "todos os jogadores" (o fantasma)
    // ou de "o meu jogador" (a HUD) pergunta aqui, em vez de procurar na cena.
    public class NetworkPlayer : NetworkBehaviour
    {
        static readonly List<NetworkPlayer> all = new();

        // Todos os jogadores conectados (o meu e os dos outros).
        public static IReadOnlyList<NetworkPlayer> All => all;
        // O MEU jogador neste computador (null antes de a partida começar).
        public static NetworkPlayer Local { get; private set; }
        // Avisa quando o meu jogador nasce (ex.: a câmera de espera se desliga).
        public static event Action<NetworkPlayer> LocalSpawned;

        // Valores "static" sobrevivem entre um Play e outro no editor (com o Play rápido):
        // este atributo faz a Unity chamar a função no começo de todo Play, e zeramos tudo.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            Local = null;
            LocalSpawned = null;
        }

        // Scripts que só fazem sentido no MEU boneco: leem o MEU teclado ou mexem na MINHA tela.
        [SerializeField] Behaviour[] ownerOnly;

        // Atalhos para as peças do boneco (a HUD lê a estamina e a dica de interação daqui).
        public PlayerMovement Movement { get; private set; }
        public FurnitureCarrier Carrier { get; private set; }
        public CharacterSwitcher Character { get; private set; }
        public PlayerInteractor Interactor { get; private set; }
        public CueListener Cues { get; private set; }
        public PlayerCondition Condition { get; private set; }

        Vector3 spawnPosition;

        void Awake()
        {
            Movement = GetComponent<PlayerMovement>();
            Carrier = GetComponent<FurnitureCarrier>();
            Character = GetComponent<CharacterSwitcher>();
            Interactor = GetComponent<PlayerInteractor>();
            Cues = GetComponent<CueListener>();
            Condition = GetComponent<PlayerCondition>();
        }

        // Chamado quando o objeto "nasce" na rede (como um Start, mas já sabendo quem é o dono).
        public override void OnNetworkSpawn()
        {
            all.Add(this);

            if (IsOwner)
            {
                // Onde nasci (o PlayerSpawner escolhe um SpawnPoint para cada um): o reinício volta aqui.
                spawnPosition = transform.position;

                // Meu próprio boneco some da minha tela (a câmera fica dentro da cabeça).
                // Os outros jogadores continuam vendo ele normalmente.
                // Pode haver mais de um (boneco de blocos + modelo da ficha), e algum desligado.
                foreach (var walk in GetComponentsInChildren<DollWalk>(true))
                    walk.HideBody();

                Local = this;
                LocalSpawned?.Invoke(this);
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

        public override void OnNetworkDespawn()
        {
            all.Remove(this);
            if (Local == this) Local = null;
        }

        // O host pede, mas quem move é o DONO do boneco (a posição dos jogadores é
        // autoridade de cada um). SendTo.Owner: roda só no computador do dono deste boneco.
        [Rpc(SendTo.Owner)]
        public void ResetPositionRpc()
        {
            var controller = GetComponent<CharacterController>();
            controller.enabled = false;
            // Teleport em vez de mudar a posição: os outros veem o boneco "pular", não deslizar.
            GetComponent<NetworkTransform>().Teleport(spawnPosition, transform.rotation, transform.localScale);
            controller.enabled = true;
        }
    }
}
