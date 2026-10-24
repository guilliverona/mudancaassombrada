using System.Collections.Generic;
using UnityEngine;

// A caçamba do caminhão. Um móvel conta como "entregue" quando está aqui dentro
// E ninguém está segurando ele (tem que soltar lá dentro, não vale só encostar).
[RequireComponent(typeof(BoxCollider))]
public class TruckZone : MonoBehaviour
{
    // HashSet = uma lista sem repetição, ótima para "está ou não está dentro?".
    readonly HashSet<Furniture> inside = new();

    public int DeliveredCount
    {
        get
        {
            int count = 0;
            foreach (var f in inside)
                if (f != null && !f.IsHeld) count++;
            return count;
        }
    }

    // Um collider marcado como "Is Trigger" não bloqueia nada: só avisa quando algo entra ou sai.
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Furniture f)) inside.Add(f);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Furniture f)) inside.Remove(f);
    }
}
