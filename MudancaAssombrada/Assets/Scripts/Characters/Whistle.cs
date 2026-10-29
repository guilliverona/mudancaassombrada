using UnityEngine;

namespace MudancaAssombrada
{
    // O apito (tecla G). O Marcelo não fala, mas apita BEM alto.
    // Uso tático: atrair o fantasma para longe enquanto os outros carregam os móveis.
    // Desligado (enabled = false) para quem não tem apito na ficha.
    public class Whistle : MonoBehaviour
    {
        [SerializeField] float range = 15f;
        [SerializeField] float cooldown = 4f;

        float nextWhistleTime;

        void Update()
        {
            if (PauseMenu.IsOpen || !GameInput.Whistle.WasPressedThisFrame() || Time.time < nextWhistleTime) return;

            nextWhistleTime = Time.time + cooldown;
            // Um som como outro qualquer: o fantasma vem, a Cida vê, o Tiago vê o anel.
            // Nenhum desses scripts precisou mudar para o apito existir!
            EchoSystem.Emit(new SoundEvent(transform.position, range, 1f, SoundType.Whistle, gameObject));
        }
    }
}
