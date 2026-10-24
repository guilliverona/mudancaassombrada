using UnityEngine;

// Marca um objeto como "móvel carregável". Coloque em qualquer cubo/modelo com Collider.
[RequireComponent(typeof(Rigidbody))]
public class Furniture : MonoBehaviour
{
    // 1 = leve (cadeira), 3 = pesado (armário). Depois, o Marcelo vai ignorar boa parte disso.
    [SerializeField, Range(1f, 5f)] float weight = 1f;

    public float Weight => weight;
    public Rigidbody Body { get; private set; }
    public bool IsHeld { get; set; } // o FurnitureCarrier liga/desliga ao pegar/soltar

    void Awake() => Body = GetComponent<Rigidbody>();

    // Chamado pela Unity quando este objeto bate em algo.
    // Móvel caindo ou batendo na parede faz barulho, e a Cida "vê" o barulho.
    void OnCollisionEnter(Collision collision)
    {
        float impact = collision.relativeVelocity.magnitude;
        if (impact < 1.5f) return; // encostar devagar não faz barulho

        Vector3 point = collision.GetContact(0).point;
        EchoSystem.Emit(point, Mathf.Clamp(impact * 1.5f, 3f, 10f), Mathf.Clamp01(impact / 5f), gameObject);
    }
}
