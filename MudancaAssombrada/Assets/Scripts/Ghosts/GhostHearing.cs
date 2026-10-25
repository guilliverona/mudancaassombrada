using System;
using UnityEngine;

namespace MudancaAssombrada
{
    // SENTIDO do fantasma: a AUDIÇÃO. Só percebe e avisa ("ouvi isto, a tal distância");
    // quem decide o que fazer é o comportamento (o GhostHunter corre atrás, a Maestrina pode
    // ignorar a própria música, o Inquilino junta tudo). Assim vários comportamentos usam
    // o mesmo ouvido, sem copiar código.
    //
    // Só funciona no host (é lá que o fantasma pensa).
    // Sentido novo (visão, faro...) = outro componente igual a este, com o seu próprio evento.
    [RequireComponent(typeof(Ghost))]
    public class GhostHearing : MonoBehaviour
    {
        [Tooltip("Que tipos de som chamam a atenção dele. A música, por exemplo, o Bibliotecário ignora.")]
        [SerializeField] SoundType hears = SoundType.Footstep | SoundType.Voice | SoundType.Impact | SoundType.Cane | SoundType.Whistle;
        [Tooltip("Ouve sons um pouco além do alcance deles (1,5 = 50% mais longe).")]
        [SerializeField] float hearingMultiplier = 1.5f;

        // Avisa (só no host) cada som que ele ouviu: (o som, a distância até ele).
        public event Action<SoundEvent, float> Heard;

        // Um comportamento pode trocar o que ele ouve por um tempo (ex.: surdo de raiva).
        public SoundType Hears { get => hears; set => hears = value; }

        Ghost ghost;

        void Awake() => ghost = GetComponent<Ghost>();

        // OnEnable/OnDisable: o jeito certo de se inscrever num evento e se desinscrever dele.
        void OnEnable() => EchoSystem.SoundEmitted += OnSound;
        void OnDisable() => EchoSystem.SoundEmitted -= OnSound;

        void OnSound(SoundEvent sound)
        {
            if (!ghost.IsServer) return;            // só o host decide o que o fantasma ouve
            if (sound.Source == gameObject) return; // ignora os próprios passos
            if (!sound.Is(hears)) return;           // tipo de som que não interessa a ele

            float distance = Vector3.Distance(transform.position, sound.Position);
            if (distance > sound.Range * hearingMultiplier) return; // longe demais, não ouviu

            Heard?.Invoke(sound, distance);
        }
    }
}
