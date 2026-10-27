using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // Do que uma coisa É FEITA, para o diapasão da Cida (LEVEL_DESIGN_FASE1.md, MEC-02).
    // O eco comum mostra a forma; o diapasão mostra o MATERIAL: madeira responde curto e quente,
    // vidro frio e longo, metal agudo, tecido quase nada. Uma pessoa estremece; a CÓPIA do
    // Imitador não responde nada (é assim que a Cida desmascara até um disfarce parado).
    //
    // O número de cada tipo é o que o Echo.shader lê (_Timbre): não mude a ordem.
    public enum SurfaceType
    {
        Default = 0,       // paredes, piso: um anel neutro
        Wood = 1,
        Glass = 2,
        CrackedGlass = 3,  // espelho com o Imitador dentro: zumbido "batido"
        Metal = 4,
        Fabric = 5,
        Person = 6,
        Copy = 7,          // o Imitador: silêncio
    }

    // Fica na raiz do objeto: todos os desenhos dele (filhos) respondem com este timbre.
    // Num espelho, vira "vidro rachado" sozinho enquanto o Imitador está escondido nele.
    public class SurfaceTag : MonoBehaviour
    {
        static readonly List<SurfaceTag> all = new();
        public static IReadOnlyList<SurfaceTag> All => all;

        [SerializeField] SurfaceType type = SurfaceType.Wood;

        Mirror mirror; // se for um espelho: o timbre muda com quem está dentro

        public SurfaceType Current => mirror != null && type == SurfaceType.Glass && mirror.IsOccupied
            ? SurfaceType.CrackedGlass : type;

        void Awake() => mirror = GetComponent<Mirror>();
        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);
    }
}
