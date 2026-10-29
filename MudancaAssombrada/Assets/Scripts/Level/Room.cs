using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // MARCADOR: um CÔMODO da casa (Sala, Camarim...). Uma caixa invisível com nome, desenhada
    // no editor. Serve para o jogo FALAR do lugar: "Tiago caiu no Camarim!". Mais tarde, para
    // regras por cômodo (luz apagada, música tocando, fantasma que não sai do porão...).
    //
    // De propósito NÃO tem Collider: um collider de gatilho atrapalharia a mira dos jogadores.
    public class Room : MonoBehaviour
    {
        static readonly List<Room> all = new();
        public static IReadOnlyList<Room> All => all;

        [Tooltip("Nome que aparece nos avisos. Vazio = o nome do objeto.")]
        [SerializeField] string displayName = "";
        [Tooltip("Tamanho da caixa (largura, altura, profundidade), centrada no objeto.")]
        [SerializeField] Vector3 size = new(4f, 3f, 4f);

        public Vector3 Size => size;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        // O ponto está dentro deste cômodo? (A caixa gira junto com o objeto.)
        public bool Contains(Vector3 point)
        {
            Vector3 local = transform.InverseTransformPoint(point);
            Vector3 half = size / 2f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        // Em qual cômodo fica este ponto? null = fora de todos (ex.: na rua).
        public static Room At(Vector3 point)
        {
            foreach (var room in all)
                if (room.Contains(point)) return room;
            return null;
        }

        // " (Camarim)", ou "" se não estiver em cômodo nenhum. Pronto para colar no fim de uma frase.
        public static string InRoomText(Vector3 point)
        {
            var room = At(point);
            return room != null ? $" ({room.DisplayName})" : "";
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
