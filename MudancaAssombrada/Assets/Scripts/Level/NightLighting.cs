using UnityEngine;
using UnityEngine.Rendering; // RenderPipelineManager, ScriptableRenderContext

namespace MudancaAssombrada
{
    // Deixa a fase de NOITE e faz o amanhecer chegar aos poucos (DESIGN.md: "a própria luz
    // é o relógio"). Fria e azulada no começo; no fim da noite, o céu clareia e a luz fica âmbar.
    //
    // Segue o relógio do GameManager, então todos os jogadores veem o MESMO céu.
    // Fase sem relógio (ex.: a final) fica de noite o tempo todo.
    //
    // Mexe em três coisas:
    //   - a luz direcional da cena (lua -> sol nascendo)
    //   - a luz ambiente e a neblina (o "ar" da cena)
    //   - o fundo das câmeras (o céu), da mesma cor da neblina para o horizonte sumir no escuro
    public class NightLighting : MonoBehaviour
    {
        [Header("Noite")]
        [SerializeField] Color moonColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField] float moonIntensity = 0.25f;
        [SerializeField] Color nightAmbient = new Color(0.07f, 0.09f, 0.15f);
        [SerializeField] Color nightSky = new Color(0.01f, 0.015f, 0.04f);
        [SerializeField] Vector3 moonAngles = new Vector3(55f, -30f, 0f); // alta no céu

        [Header("Amanhecer")]
        [SerializeField] Color sunColor = new Color(1f, 0.72f, 0.45f);    // âmbar
        [SerializeField] float sunIntensity = 0.9f;
        [SerializeField] Color dawnAmbient = new Color(0.35f, 0.3f, 0.3f);
        [SerializeField] Color dawnSky = new Color(0.85f, 0.6f, 0.45f);
        [SerializeField] Vector3 sunAngles = new Vector3(6f, 90f, 0f);    // baixo, no leste
        [Tooltip("Em que parte da noite o céu começa a clarear (0.75 = no último quarto).")]
        [SerializeField, Range(0f, 1f)] float dawnStart = 0.75f;

        [Header("Neblina (esconde o fundo e deixa o escuro mais denso)")]
        [SerializeField] float nightFogDensity = 0.06f;
        [SerializeField] float dawnFogDensity = 0.02f;

        Light sun;
        Color sky; // a cor do céu agora
        float appliedAmbient = -1f; // o brilho da luz ambiente da última vez que a sonda foi recalculada

        // O URP avisa "vou desenhar esta câmera" ANTES de cada desenho. Pintar o fundo nessa hora
        // pega até a câmera do jogador que acabou de nascer, já no primeiro quadro (sem isso,
        // ela mostrava o céu azul padrão da Unity por um instante).
        void OnEnable() => RenderPipelineManager.beginCameraRendering += PaintSky;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= PaintSky;

        void Awake()
        {
            // A luz principal da cena (Window > Rendering > Lighting > Sun Source).
            // Cena sem "Sun Source" configurado: usa a primeira luz direcional que achar (só uma vez).
            sun = RenderSettings.sun;
            if (sun == null)
                foreach (var light in FindObjectsByType<Light>())
                    if (light.type == LightType.Directional) { sun = light; break; }
            RenderSettings.ambientMode = AmbientMode.Flat; // uma cor só, simples de controlar
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            Apply(0f);
        }

        void Update()
        {
            var game = GameManager.Instance;
            // Progresso da noite: 0 = acabou de escurecer, 1 = amanheceu.
            float night = game != null && game.IsSpawned ? game.NightProgress : 0f;
            // Só clareia no fim da noite: antes do "dawnStart" fica 0, depois vai até 1.
            float dawn = Mathf.InverseLerp(dawnStart, 1f, night);
            Apply(dawn * dawn); // ao quadrado: começa devagarinho e acelera no final
        }

        void Apply(float t)
        {
            if (sun != null)
            {
                sun.color = Color.Lerp(moonColor, sunColor, t);
                sun.intensity = Mathf.Lerp(moonIntensity, sunIntensity, t);
                sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(moonAngles), Quaternion.Euler(sunAngles), t);
            }

            Color ambient = Color.Lerp(nightAmbient, dawnAmbient, t);
            RenderSettings.ambientLight = ambient;
            // Trocar a cor não basta: o URP ilumina pela "sonda de ambiente", calculada uma vez. Sem
            // recalcular, a cena ficava com a luz azulada padrão do céu (nada era escuro de verdade).
            // Recalcula só quando a cor muda o suficiente para se notar (é barato, mas não é de graça).
            if (Mathf.Abs(ambient.grayscale - appliedAmbient) > 0.002f || appliedAmbient < 0f)
            {
                appliedAmbient = ambient.grayscale;
                DynamicGI.UpdateEnvironment();
            }
            sky = Color.Lerp(nightSky, dawnSky, t);
            RenderSettings.fogColor = sky;
            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dawnFogDensity, t);
        }

        // Pinta o fundo da câmera que vai ser desenhada (a de espera ou a do jogador).
        void PaintSky(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType != CameraType.Game) return; // a janela Scene do editor fica como está
            // Quem enxerga pelo eco tem a tela PRETA de propósito: não mexemos na câmera dele.
            if (cam == Camera.main && EchoSystem.Instance != null && EchoSystem.Instance.IsBlind) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
        }
    }
}
