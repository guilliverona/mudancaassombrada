using UnityEngine;

namespace MudancaAssombrada
{
    // Uma luminária que se ACENDE (a luminária verde da Biblioteca, LEVEL_DESIGN_FASE1_MANSAO.md §6.1).
    // Quem acende é o Interactable do mesmo objeto ("Acender"); este script só segue o estado dele,
    // em todos os computadores: luz acesa e um CLIQUE que se ouve a 6 m (um ruído de verdade:
    // o fantasma escuta, a Cida vê a onda). Reiniciar a fase apaga de novo (o Interactable volta).
    [RequireComponent(typeof(Interactable))]
    public class LampSwitch : MonoBehaviour
    {
        [SerializeField] float clickRange = 6f;

        Interactable button;
        Light[] lights;
        bool on;

        void Awake()
        {
            button = GetComponent<Interactable>();
            lights = GetComponentsInChildren<Light>(true);
            Apply(false);
        }

        void Update()
        {
            if (button.IsUsed == on) return;
            Apply(button.IsUsed);
            if (on) EchoSystem.EmitLocal(new SoundEvent(transform.position + Vector3.up * 0.4f, clickRange, 0.5f, SoundType.Impact, gameObject));
        }

        void Apply(bool value)
        {
            on = value;
            foreach (var l in lights) l.enabled = value;
        }
    }
}
