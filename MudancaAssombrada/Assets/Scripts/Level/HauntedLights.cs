using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // As lâmpadas da casa FALHAM quando um fantasma passa perto delas (ScreenLookRules.Flicker).
    // Só visual e só neste computador (os fantasmas já chegam a todos pela rede): nada passa na rede.
    //
    // A NightLighting põe este componente ao lado dela. Ele junta as luzes da cena uma vez, ao
    // começar, e deixa de fora as que já têm dono da intensidade (LightFlicker, AppearanceLights).
    // A lanterna de cada jogador falha pelo mesmo motivo, mas lá quem chama é o Flashlight.
    public class HauntedLights : MonoBehaviour
    {
        struct Lamp
        {
            public Light light;
            public float baseIntensity;
            public float seed;
        }
        readonly List<Lamp> lamps = new();

        void Start()
        {
            var owned = new HashSet<Light>();
            foreach (var s in FindObjectsByType<AppearanceLights>())
                foreach (var l in s.GetComponentsInChildren<Light>(true)) owned.Add(l);

            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type == LightType.Directional || owned.Contains(l)) continue;
                if (l.GetComponent<LightFlicker>() != null) continue;
                if (l.GetComponentInParent<Flashlight>() != null) continue;
                lamps.Add(new Lamp { light = l, baseIntensity = l.intensity, seed = Random.value * 100f });
            }
        }

        void Update()
        {
            foreach (var lamp in lamps)
            {
                if (lamp.light == null || !lamp.light.isActiveAndEnabled) continue;
                lamp.light.intensity = lamp.baseIntensity * Multiplier(lamp.light.transform.position, lamp.seed);
            }
        }

        // Quanto uma luz neste ponto brilha agora (1 = normal). Ruído rápido: as falhas parecem elétricas.
        public static float Multiplier(Vector3 position, float seed)
        {
            float danger = ScreenLook.NearestDanger(position);
            if (danger <= 0f) return 1f;
            return ScreenLookRules.Flicker(danger, Mathf.PerlinNoise(Time.time * 11f, seed));
        }
    }
}
