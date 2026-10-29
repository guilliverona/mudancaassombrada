using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MudancaAssombrada
{
    // O "filtro" da tela nas casas: um Volume global de pós-processamento, criado aqui por código
    // (nada salvo na cena, nada para sujar o Git). As regras de quanto de cada efeito ficam no
    // ScreenLookRules; aqui só se lê o jogo (amanhecer, fantasmas, personagem) e se escreve no URP.
    //
    // A NightLighting põe este componente ao lado dela: toda casa com noite ganha o mesmo visual.
    // O brilho das opções (VideoSettings) é outro Volume, por cima, e só mexe na exposição.
    [RequireComponent(typeof(NightLighting))]
    public class ScreenLook : MonoBehaviour
    {
        // O quanto o perigo sobe e desce por segundo: entra rápido (susto), sai devagar (alívio).
        const float DangerRise = 2.5f, DangerFall = 0.6f;

        NightLighting night;
        VolumeProfile profile;
        ColorAdjustments color;
        WhiteBalance white;
        SplitToning split;
        Vignette vignette;
        ChromaticAberration chromatic;
        FilmGrain grain;
        Bloom bloom;
        float danger;

        // O perigo agora, já suavizado (0..1). A trilha e outros efeitos podem ler daqui.
        public static float Danger { get; private set; }

        void Awake()
        {
            night = GetComponent<NightLighting>();
            var go = new GameObject("Filtro da Tela");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10; // abaixo do brilho das opções (1000)
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            // O que fica fixo a noite inteira.
            var tone = profile.Add<Tonemapping>();
            // Neutral, e não ACES: o ACES escurece muito os tons baixos, e a casa é quase toda tom
            // baixo (só a lua fraca). Com ele, só sobravam os pontos de luz, e o degradê sumia.
            tone.mode.Override(TonemappingMode.Neutral);

            var lgg = profile.Add<LiftGammaGain>();
            // Neutro: a luz ambiente da NightLighting já é azulada; tingir aqui de novo deixava a tela
            // toda azul e lavada. O Lift fica para ajuste fino (o 4º número é o brilho dos escuros).
            lgg.lift.Override(new Vector4(0.985f, 0.995f, 1.02f, 0f)); // escuros um tico azuis

            color = profile.Add<ColorAdjustments>();
            white = profile.Add<WhiteBalance>();
            split = profile.Add<SplitToning>();
            split.shadows.Override(new Color(0.38f, 0.47f, 0.68f)); // sombras frias (quanto mais perto do cinza, mais fraco)
            split.highlights.Override(new Color(1f, 0.78f, 0.5f));  // luz quente

            vignette = profile.Add<Vignette>();
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);

            chromatic = profile.Add<ChromaticAberration>();

            grain = profile.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.response.Override(0.8f); // o grão aparece mais no escuro, como num filme barato

            bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1f, 0.85f, 0.7f));
        }

        void OnDestroy()
        {
            if (profile != null) Destroy(profile);
            Danger = 0f;
        }

        void Update()
        {
            var me = NetworkPlayer.Local;
            float target = me != null ? NearestDanger(me.transform.position) : 0f;
            float rate = target > danger ? DangerRise : DangerFall;
            danger = Mathf.MoveTowards(danger, target, rate * Time.deltaTime);
            Danger = danger;

            var sight = ScreenLookRules.Sight.Normal;
            if (EchoSystem.Instance != null && EchoSystem.Instance.IsBlind) sight = ScreenLookRules.Sight.Echo;
            else if (me != null && me.Character != null && me.Character.Current != null && me.Character.Current.vibrationVision)
                sight = ScreenLookRules.Sight.Vibration;
            bool down = me != null && me.Condition != null && (me.Condition.IsDown || me.Condition.IsPulled);
            bool effects = VideoRules.Quality(GameSettings.Quality).effects;

            Write(ScreenLookRules.Evaluate(night.Dawn, danger, sight, down, effects));
        }

        // O fantasma que mais assusta quem está jogando neste computador.
        public static float NearestDanger(Vector3 position)
        {
            float worst = 0f;
            foreach (var g in Ghost.All)
            {
                if (g == null || !g.IsSpawned) continue;
                worst = Mathf.Max(worst, ScreenLookRules.Danger(g.State, Vector3.Distance(position, g.transform.position)));
            }
            return worst;
        }

        void Write(ScreenLookRules.Look look)
        {
            color.saturation.Override(look.saturation);
            color.contrast.Override(look.contrast);
            white.active = look.grading;
            white.temperature.Override(look.temperature);
            split.active = look.grading;
            split.balance.Override(look.splitBalance);
            vignette.intensity.Override(look.vignette);
            vignette.color.Override(look.vignetteColor);
            chromatic.intensity.Override(look.chromatic);
            grain.active = look.grain > 0f;
            grain.intensity.Override(look.grain);
            bloom.active = look.bloom > 0f;
            bloom.intensity.Override(look.bloom);
        }
    }
}
