using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // Respiração: de tempos em tempos, um som baixinho (SoundType.Breath) na altura da boca.
    // Fica nos jogadores e nos fantasmas que respiram. Quem NÃO tem este componente
    // (o Imitador) não aparece respirando no eco: é assim que a Cida descobre a cópia falsa.
    //
    // REDE: só quem é DONO do objeto emite (o jogador no próprio computador, o fantasma no host).
    // O SoundRelay leva o som para os outros. Se todos emitissem, o som chegaria repetido.
    public class Breathing : MonoBehaviour
    {
        [SerializeField] float interval = 3f;      // segundos entre uma respiração e outra
        [SerializeField] float range = 1.5f;       // baixinho: só ouve quem está perto
        [SerializeField] float intensity = 0.2f;
        [Tooltip("Altura da boca acima do centro do objeto.")]
        [SerializeField] float mouthHeight = 0.6f;
        [Tooltip("Correndo (jogador): respira mais rápido e mais alto.")]
        [SerializeField] float runMultiplier = 2f;

        PlayerMovement movement; // só existe nos jogadores
        NetworkObject netObj;
        float timer;

        // Desligar a respiração por código (ex.: segurando o fôlego, num futuro).
        public bool Holding { get; set; }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            netObj = GetComponent<NetworkObject>();
            // Cada um começa num ponto diferente, para não respirarem todos juntos.
            timer = Random.Range(0f, interval);
        }

        void Update()
        {
            // Sem rede (testando a cena sozinho) todo mundo respira; com rede, só o dono.
            if (netObj != null && netObj.IsSpawned && !netObj.IsOwner) return;
            if (Holding) return;

            bool running = movement != null && movement.IsRunning;
            timer += Time.deltaTime * (running ? runMultiplier : 1f);
            if (timer < interval) return;
            timer = 0;

            float loud = running ? runMultiplier : 1f;
            Vector3 mouth = transform.position + Vector3.up * mouthHeight;
            EchoSystem.Emit(new SoundEvent(mouth, range * loud, intensity * loud, SoundType.Breath, gameObject));
        }
    }
}
