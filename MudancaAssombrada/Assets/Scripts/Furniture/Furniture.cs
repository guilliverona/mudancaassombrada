using Unity.Netcode;
using UnityEngine;

// Marca um objeto como "móvel carregável". Coloque em qualquer cubo/modelo com Collider.
//
// EM REDE: quem simula a física de um móvel é o DONO dele.
//  - Parado no chão: o dono é o host.
//  - Alguém pega: o host passa a posse para essa pessoa, e o computador DELA simula
//    o móvel enquanto carrega (sem atraso, como se estivesse jogando sozinho).
//  - Soltou: a posse volta para o host.
// Nos outros computadores o Rigidbody fica "kinematic" (sem física própria) e só copia
// a posição que chega pela rede. Quem faz essa troca é o componente NetworkRigidbody.
[RequireComponent(typeof(Rigidbody))]
public class Furniture : NetworkBehaviour
{
    // 1 = leve (cadeira), 3 = pesado (armário). Depois, o Marcelo vai ignorar boa parte disso.
    [SerializeField, Range(1f, 5f)] float weight = 1f;

    // Todos precisam saber se o móvel está sendo carregado: o caminhão só conta móvel SOLTO,
    // e ninguém pode pegar um móvel que já está na mão de outro.
    readonly NetworkVariable<bool> isHeld = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float Weight => weight;
    public Rigidbody Body { get; private set; }
    public bool IsHeld => isHeld.Value;
    public float NoiseMultiplier { get; set; } = 1f; // < 1 = batidas mais silenciosas

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        startPosition = transform.position; // guarda onde o móvel começa, para o reinício
        startRotation = transform.rotation;
    }

    // Só no host (chamado pelo GameManager ao reiniciar): devolve o móvel ao lugar inicial.
    public void ServerReset()
    {
        if (isHeld.Value)
        {
            isHeld.Value = false;
            NetworkObject.RemoveOwnership(); // tira da mão de quem estava carregando
        }
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.position = startPosition;
        // Teleport: "pule para lá sem deslizar". Sem ele, os outros computadores veriam o
        // móvel atravessar a casa voando até o lugar inicial (a interpolação "suaviza" o salto).
        GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(startPosition, startRotation, transform.localScale);
    }

    public void RequestPickUp() => PickUpRpc();
    public void RequestDrop() => DropRpc();

    // RpcParams traz informações sobre a chamada, como QUEM pediu (SenderClientId).
    [Rpc(SendTo.Server)]
    void PickUpRpc(RpcParams rpcParams = default)
    {
        if (isHeld.Value) return; // dois jogadores pegaram ao mesmo tempo: o primeiro leva
        isHeld.Value = true;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender != OwnerClientId) NetworkObject.ChangeOwnership(sender); // o host já é dono
    }

    [Rpc(SendTo.Server)]
    void DropRpc(RpcParams rpcParams = default)
    {
        // Só quem está segurando pode soltar (evita um cliente "arrancar" o móvel de outro).
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;
        isHeld.Value = false;
        NetworkObject.RemoveOwnership(); // devolve a posse para o host
    }

    // Chamado pela Unity quando este objeto bate em algo.
    // Móvel caindo ou batendo na parede faz barulho, e a Cida "vê" o barulho.
    void OnCollisionEnter(Collision collision)
    {
        // Só quem simula a física (o dono) faz o som. O SoundRelay manda para os outros.
        // Sem isso, cada computador faria o mesmo barulho e ele chegaria repetido.
        if (IsSpawned && !IsOwner) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < 1.5f) return; // encostar devagar não faz barulho

        Vector3 point = collision.GetContact(0).point;
        float range = Mathf.Clamp(impact * 1.5f, 3f, 10f) * NoiseMultiplier;
        EchoSystem.Emit(point, range, Mathf.Clamp01(impact / 5f), gameObject);
    }
}
