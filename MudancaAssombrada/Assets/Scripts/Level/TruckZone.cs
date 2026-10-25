using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // A caçamba do caminhão. Um móvel conta como "entregue" quando está aqui dentro
    // E ninguém está segurando ele (tem que soltar lá dentro, não vale só encostar).
    //
    // Pode haver mais de um caminhão na fase: o GameManager soma todos (TotalDelivered).
    [RequireComponent(typeof(BoxCollider))]
    public class TruckZone : MonoBehaviour
    {
        static readonly List<TruckZone> all = new();

        // HashSet = uma lista sem repetição, ótima para "está ou não está dentro?".
        readonly HashSet<Furniture> inside = new();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

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

        // Soma de todos os caminhões da fase.
        public static int TotalDelivered
        {
            get
            {
                int total = 0;
                foreach (var truck in all) total += truck.DeliveredCount;
                return total;
            }
        }

        // Este móvel está entregue (solto dentro de algum caminhão)?
        public static bool IsDelivered(Furniture furniture)
        {
            if (furniture.IsHeld) return false;
            foreach (var truck in all)
                if (truck.inside.Contains(furniture)) return true;
            return false;
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
}
