using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MudancaAssombrada
{
    // Reflexo de verdade num espelho ("reflexão planar").
    // Ideia: uma segunda câmera fica do OUTRO lado do vidro, como se o espelho fosse uma janela
    // para um mundo invertido, e grava o que vê numa textura. O shader Espelho cola essa textura
    // no vidro. Assim o reflexo mostra jogadores, fantasmas e móveis se mexendo.
    //
    // Custo: cada espelho ligado desenha a cena de novo. Por isso só os MaxActive espelhos
    // mais perto (e na tela) refletem; os outros mostram o vidro escuro. Tudo é só visual,
    // em cada computador: nada passa pela rede.
    //
    // Fica separado do Mirror (o esconderijo do Imitador): um cuida do jogo, este da imagem.
    public class MirrorReflection : MonoBehaviour
    {
        // Registro de todos os espelhos com reflexo (em vez de Find).
        public static readonly List<MirrorReflection> All = new();

        const int MaxActive = 2;

        [Tooltip("O renderer do modelo que tem o vidro (preenchido pelo construtor).")]
        [SerializeField] Renderer glass;
        [Tooltip("Qual material do renderer é o vidro.")]
        [SerializeField] int glassMaterialIndex;
        [Tooltip("Mais longe que isto, o espelho fica escuro (economiza).")]
        [SerializeField] float maxDistance = 12f;
        [Tooltip("Resolução do reflexo em relação à tela (0,5 = metade).")]
        [SerializeField, Range(0.25f, 1f)] float resolution = 0.5f;
        [Tooltip("Camadas que aparecem no reflexo. Dá para esconder (ou só mostrar) coisas no espelho.")]
        [SerializeField] LayerMask reflectedLayers = ~0;
        [Tooltip("Folga do corte atrás do vidro (evita que a moldura apareça no reflexo).")]
        [SerializeField] float clipOffset = 0.02f;
        [Tooltip("Espelho HERÓI: na tela, reflete antes dos outros, mesmo mais longe (a escadaria da " +
                 "Mansão: quem sobe se vê nos dois do topo, e o reflexo não fica pulando de espelho).")]
        [SerializeField] bool hero;

        public bool Hero { get => hero; set => hero = value; }

        // A ordem de quem reflete (menor primeiro): os heróis à frente de qualquer comum, e entre
        // iguais o mais perto. Os testes ficam em MansionRulesTests (R04).
        public static float Priority(float distance, bool hero) => hero ? distance - 1000f : distance;

        static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
        static readonly int StrengthId = Shader.PropertyToID("_Strength");

        Camera reflectionCamera;
        RenderTexture texture;
        MaterialPropertyBlock block;
        Vector3 localPoint, localNormal; // o plano do vidro, no espaço deste objeto
        bool active;

        // O construtor de prefabs chama isto ao montar o espelho.
        public void Configure(Renderer glassRenderer, int materialIndex)
        {
            glass = glassRenderer;
            glassMaterialIndex = materialIndex;
        }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            FindGlassPlane();
        }

        void OnEnable()
        {
            if (All.Count == 0)
            {
                RenderPipelineManager.beginContextRendering += UpdateAll;
                RenderPipelineManager.beginCameraRendering += ShowOwnBody;
                RenderPipelineManager.endCameraRendering += HideOwnBody;
            }
            All.Add(this);
            SetActive(false);
        }

        void OnDisable()
        {
            All.Remove(this);
            if (All.Count == 0)
            {
                RenderPipelineManager.beginContextRendering -= UpdateAll;
                RenderPipelineManager.beginCameraRendering -= ShowOwnBody;
                RenderPipelineManager.endCameraRendering -= HideOwnBody;
            }
            SetActive(false);
            if (reflectionCamera != null) ReflectionCameras.Remove(reflectionCamera);
            if (reflectionCamera != null) Destroy(reflectionCamera.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
            reflectionCamera = null;
            texture = null;
        }

        // O vidro olha para +Z do espelho (o construtor encosta as costas na parede).
        // A frente do vidro é o ponto mais à frente da parte da malha que usa o material do vidro.
        void FindGlassPlane()
        {
            localNormal = Vector3.forward;
            localPoint = Vector3.zero;
            if (glass == null) return;
            var filter = glass.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || glassMaterialIndex >= filter.sharedMesh.subMeshCount) return;

            var b = filter.sharedMesh.GetSubMesh(glassMaterialIndex).bounds;
            float front = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one);
                var local = transform.InverseTransformPoint(glass.transform.TransformPoint(corner));
                front = Mathf.Max(front, local.z);
            }
            localPoint = transform.InverseTransformPoint(glass.bounds.center);
            localPoint.z = front;
        }

        // Uma vez por frame, antes de qualquer câmera desenhar: escolhe quais espelhos refletem
        // e posiciona as câmeras deles. (Antes das câmeras = o reflexo nunca fica um frame atrasado.)
        static void UpdateAll(ScriptableRenderContext context, List<Camera> cameras)
        {
            var eye = Camera.main;
            // Quem enxerga pelo eco não vê reflexo nenhum: nem gasta desenhando.
            bool blind = EchoSystem.Instance != null && EchoSystem.Instance.IsBlind;
            if (eye == null || blind)
            {
                foreach (var m in All) m.SetActive(false);
                return;
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(eye);
            var chosen = new List<(float distance, MirrorReflection mirror)>();
            foreach (var m in All)
            {
                float d = m.Score(eye, planes);
                if (d >= 0) chosen.Add((Priority(d, m.hero), m));
            }
            chosen.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < chosen.Count && i < MaxActive; i++) chosen[i].mirror.Render(eye);
            for (int i = MaxActive; i < chosen.Count; i++) chosen[i].mirror.SetActive(false);
            foreach (var m in All)
                if (!chosen.Exists(c => c.mirror == m)) m.SetActive(false);
        }

        // O seu próprio corpo é invisível para a sua câmera (DollWalk.HideBody), mas no espelho
        // você se vê: o corpo aparece só enquanto uma câmera de reflexo desenha, e a cópia sem
        // cabeça (FirstPersonBody, o que você vê ao olhar para baixo) some nesse meio-tempo.
        // A Dona Cida não se vê porque, no modo cego, nenhum espelho reflete (UpdateAll).
        static readonly HashSet<Camera> ReflectionCameras = new();

        static void ShowOwnBody(ScriptableRenderContext context, Camera cam) => SetOwnBody(cam, ShadowCastingMode.On);
        static void HideOwnBody(ScriptableRenderContext context, Camera cam) => SetOwnBody(cam, ShadowCastingMode.ShadowsOnly);

        static void SetOwnBody(Camera cam, ShadowCastingMode mode)
        {
            if (!ReflectionCameras.Contains(cam)) return;
            foreach (var part in DollWalk.HiddenBodies)
                if (part != null) part.shadowCastingMode = mode;
            // A cópia sem cabeça (corpo em primeira pessoa) não entra no reflexo.
            bool reflecting = mode == ShadowCastingMode.On;
            foreach (var part in FirstPersonBody.Parts)
                if (part != null) part.forceRenderingOff = reflecting;
            // Nem a lanterna presa à câmera: no reflexo ela volta para a mão do corpo.
            FirstPersonArm.ShowInMirror(reflecting);
        }

        // Distância até a câmera, ou -1 se este espelho não precisa refletir agora.
        float Score(Camera eye, Plane[] planes)
        {
            if (glass == null) return -1;
            Vector3 point = transform.TransformPoint(localPoint);
            Vector3 normal = transform.TransformDirection(localNormal);
            Vector3 toEye = eye.transform.position - point;
            if (Vector3.Dot(toEye, normal) <= 0.05f) return -1;      // olhando por trás do espelho
            float distance = toEye.magnitude;
            if (distance > maxDistance) return -1;
            if (!GeometryUtility.TestPlanesAABB(planes, glass.bounds)) return -1; // fora da tela
            return distance;
        }

        void Render(Camera eye)
        {
            EnsureResources(eye);
            Vector3 point = transform.TransformPoint(localPoint);
            Vector3 normal = transform.TransformDirection(localNormal).normalized;

            // A câmera "espelhada": a mesma câmera do jogador, refletida pelo plano do vidro.
            float d = -Vector3.Dot(normal, point) - clipOffset;
            Matrix4x4 reflection = ReflectionMatrix(new Vector4(normal.x, normal.y, normal.z, d));
            reflectionCamera.CopyFrom(eye);
            reflectionCamera.targetTexture = texture;
            reflectionCamera.cullingMask = reflectedLayers & eye.cullingMask;
            reflectionCamera.depth = eye.depth - 1; // desenha antes da câmera do jogador
            reflectionCamera.transform.position = reflection.MultiplyPoint(eye.transform.position);
            reflectionCamera.worldToCameraMatrix = eye.worldToCameraMatrix * reflection;

            // Corta tudo que está ATRÁS do vidro (senão a parede apareceria no reflexo).
            Vector4 clip = CameraSpacePlane(reflectionCamera.worldToCameraMatrix, point, normal);
            Matrix4x4 projection = eye.CalculateObliqueMatrix(clip);
            // Refletir inverte a "mão" do mundo e as faces ficariam do avesso. Inverter também
            // o X da imagem desfaz isso; o shader Espelho desvira a imagem na hora de ler.
            reflectionCamera.projectionMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1)) * projection;
            reflectionCamera.cullingMatrix = eye.projectionMatrix * reflectionCamera.worldToCameraMatrix;
            reflectionCamera.enabled = true;

            if (!active)
            {
                active = true;
                ApplyBlock(1f);
            }
        }

        void EnsureResources(Camera eye)
        {
            int w = Mathf.Max(64, (int)(eye.pixelWidth * resolution));
            int h = Mathf.Max(64, (int)(eye.pixelHeight * resolution));
            if (texture == null || texture.width != w || texture.height != h)
            {
                if (texture != null) { texture.Release(); Destroy(texture); }
                texture = new RenderTexture(w, h, 24) { name = $"Reflexo {name}" };
                active = false; // força reenviar a textura nova ao material
                if (reflectionCamera != null) reflectionCamera.targetTexture = texture;
            }

            if (reflectionCamera != null) return;
            var go = new GameObject("Câmera do Reflexo") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            reflectionCamera = go.AddComponent<Camera>();
            reflectionCamera.enabled = false;
            ReflectionCameras.Add(reflectionCamera);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = false;          // reflexo sem sombras: bem mais barato
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        void SetActive(bool on)
        {
            if (!on && reflectionCamera != null) reflectionCamera.enabled = false;
            if (on == active) return;
            active = on;
            ApplyBlock(on ? 1f : 0f);
        }

        void ApplyBlock(float strength)
        {
            if (glass == null) return;
            glass.GetPropertyBlock(block, glassMaterialIndex);
            if (texture != null) block.SetTexture(ReflectionTexId, texture);
            block.SetFloat(StrengthId, strength);
            glass.SetPropertyBlock(block, glassMaterialIndex);
        }

        // Matriz que espelha pontos pelo plano (n.x, n.y, n.z, d): a "fórmula do espelho".
        static Matrix4x4 ReflectionMatrix(Vector4 p)
        {
            var m = new Matrix4x4();
            m.m00 = 1 - 2 * p.x * p.x; m.m01 = -2 * p.x * p.y;    m.m02 = -2 * p.x * p.z;    m.m03 = -2 * p.w * p.x;
            m.m10 = -2 * p.y * p.x;    m.m11 = 1 - 2 * p.y * p.y; m.m12 = -2 * p.y * p.z;    m.m13 = -2 * p.w * p.y;
            m.m20 = -2 * p.z * p.x;    m.m21 = -2 * p.z * p.y;    m.m22 = 1 - 2 * p.z * p.z; m.m23 = -2 * p.w * p.z;
            m.m33 = 1;
            return m;
        }

        // O plano do vidro visto pela câmera (é assim que a matriz "oblíqua" quer receber).
        Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 point, Vector3 normal)
        {
            Vector3 p = worldToCamera.MultiplyPoint(point + normal * clipOffset);
            Vector3 n = worldToCamera.MultiplyVector(normal).normalized;
            return new Vector4(n.x, n.y, n.z, -Vector3.Dot(p, n));
        }
    }
}
