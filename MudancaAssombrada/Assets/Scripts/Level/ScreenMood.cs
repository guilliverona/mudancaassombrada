using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MudancaAssombrada
{
    // O "clima" da tela durante a fase, em pós-processamento:
    //   - PERIGO: com o fantasma perto, a vinheta fecha e a cor vai sumindo. Volta devagar
    //     quando ele se afasta (o susto chega na hora; o alívio demora).
    //   - FILTRO DO CAPÍTULO: a LUT da ficha (ChapterDefinition.colorLut) dá a cor da casa.
    //
    // A NightLighting coloca este componente sozinho (toda fase já tem a Iluminação Noturna).
    // Cada jogador calcula o próprio perigo com a SUA câmera: nada passa pela rede.
    // As contas ficam no ScreenMoodRules.
    public class ScreenMood : MonoBehaviour
    {
        [Header("Perigo (distância até o fantasma mais perto)")]
        [Tooltip("Daqui para perto, perigo máximo (m).")]
        [SerializeField] float dangerNear = 3f;
        [Tooltip("Daqui para longe, nenhum perigo (m).")]
        [SerializeField] float dangerFar = 12f;
        [SerializeField] float riseSpeed = 3f;   // por segundo
        [SerializeField] float fallSpeed = 0.4f; // por segundo

        [Header("Vinheta")]
        [SerializeField] Color vignetteColor = Color.black;
        [SerializeField, Range(0f, 1f)] float calmVignette = 0.25f;
        [SerializeField, Range(0f, 1f)] float dangerVignette = 0.55f;

        [Header("Cor")]
        [Tooltip("Saturação com o fantasma em cima (-100 = preto e branco).")]
        [SerializeField, Range(-100f, 0f)] float dangerSaturation = -80f;

        Vignette vignette;
        ColorAdjustments color;
        float danger;

        void Awake()
        {
            // Volume global com prioridade abaixo do brilho das opções (1000) e acima dos das casas.
            // O ColorAdjustments daqui só marca a saturação; a exposição continua sendo do brilho.
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 500;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            vignette = volume.profile.Add<Vignette>();
            vignette.color.Override(vignetteColor);
            vignette.smoothness.Override(0.6f);
            vignette.intensity.Override(calmVignette);

            color = volume.profile.Add<ColorAdjustments>();
            color.saturation.Override(0f);

            var game = GameManager.Instance;
            var chapter = game != null ? game.Chapter : null;
            if (chapter != null && chapter.colorLut != null)
            {
                var lut = volume.profile.Add<ColorLookup>();
                lut.texture.Override(chapter.colorLut);
                lut.contribution.Override(chapter.colorLutContribution);
            }
        }

        void Update()
        {
            float target = ScreenMoodRules.Danger(NearestGhost(), dangerNear, dangerFar);
            danger = ScreenMoodRules.Smooth(danger, target, Time.deltaTime, riseSpeed, fallSpeed);
            vignette.intensity.value = Mathf.Lerp(calmVignette, dangerVignette, danger);
            color.saturation.value = dangerSaturation * danger;
        }

        // Distância da câmera do jogador até o fantasma mais perto (infinito = nenhum).
        static float NearestGhost()
        {
            var cam = Camera.main;
            if (cam == null) return float.PositiveInfinity;
            float best = float.PositiveInfinity;
            foreach (var ghost in Ghost.All)
                best = Mathf.Min(best, Vector3.Distance(cam.transform.position, ghost.transform.position));
            return best;
        }
    }
}
