using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MudancaAssombrada
{
    // Uma "foto de reflexo" (Reflection Probe) por cômodo, tirada uma vez quando a casa carrega.
    //
    // Por que: metal e verniz só têm a cor do que refletem. Sem uma sonda no cômodo, o latão,
    // o dourado e o espelho de mão refletiam o céu vazio e ficavam escuros, de plástico.
    // Com ela, cada peça brilhante reflete a SALA onde está (as lâmpadas acesas, as paredes).
    //
    // Criadas por código a partir dos Room (nada salvo na cena). A NightLighting põe este
    // componente ao lado dela. As sondas não seguem a noite: uma foto só, no começo, já basta.
    public class RoomProbes : MonoBehaviour
    {
        [Tooltip("Tamanho da foto de cada sonda (px por face). 128 sobra para reflexo borrado de madeira e latão.")]
        [SerializeField] int resolution = 128;

        IEnumerator Start()
        {
            var probes = new System.Collections.Generic.List<ReflectionProbe>();
            foreach (var room in Room.All)
            {
                var go = new GameObject($"Reflexo ({room.DisplayName})");
                go.transform.SetParent(transform, false);
                go.transform.position = room.transform.position;
                var probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.resolution = resolution;
                // A caixa do cômodo (os Room não giram na Mansão; girado, a sonda fica alinhada ao mundo).
                probe.size = room.Size + Vector3.one * 0.2f;
                probe.boxProjection = true; // o reflexo "cola" nas paredes certas, e não no infinito
                probe.hdr = true;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = Color.black;
                probe.shadowDistance = 0f; // sem sombras na foto: barato e ninguém repara
                probes.Add(probe);
            }

            // Espera as lâmpadas da cena acordarem (LampSwitch, LightFlicker) antes da foto.
            yield return new WaitForSeconds(0.5f);
            foreach (var probe in probes)
                if (probe != null) probe.RenderProbe();
        }
    }
}
