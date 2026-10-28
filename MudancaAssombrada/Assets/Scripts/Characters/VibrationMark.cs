using UnityEngine;

namespace MudancaAssombrada
{
    // Um pedaço de chão tremendo (VibrationTremor.shader) que o Tiago vê. É UMA marca por emissor:
    // o colega andando não deixa um rastro de marcas, a mesma marca vai atrás dele e "reacende" a cada
    // passo. Sem som novo, o tremor acalma sozinho e a marca some.
    public class VibrationMark : MonoBehaviour
    {
        static Mesh quad;                       // um quadrado deitado (XZ), igual para todas as marcas
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int StrengthId = Shader.PropertyToID("_Strength");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;            // cor e força de CADA marca, sem copiar o material
        Vector3 target;                         // onde foi o último som (a marca desliza até lá)
        float power, settle;                    // força atual e quanto tempo leva para acalmar
        float radius;

        public VibrationKind Kind { get; private set; }
        public float Power => power;
        public bool Alive => power > 0.01f;

        public static VibrationMark Create(Material material)
        {
            var go = new GameObject("Vibração");
            var mark = go.AddComponent<VibrationMark>();
            go.AddComponent<MeshFilter>().sharedMesh = Quad();
            mark.meshRenderer = go.AddComponent<MeshRenderer>();
            mark.meshRenderer.sharedMaterial = material;
            mark.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mark.meshRenderer.receiveShadows = false;
            mark.block = new MaterialPropertyBlock();
            mark.block.SetFloat(SeedId, Random.Range(0f, 100f));
            return mark;
        }

        // Um som novo deste emissor: a marca vai até lá e treme de novo.
        //   size = raio (m), seconds = quanto leva para acalmar sem som novo.
        public void Pulse(Vector3 ground, VibrationKind kind, float intensity, float size, float seconds, Color color)
        {
            bool fresh = !Alive;
            target = ground + Vector3.up * 0.02f;
            if (fresh) transform.position = target;
            // Uma marca que já foi perigo não "rebaixa" enquanto ainda treme.
            if (fresh || kind == VibrationKind.Strong) Kind = kind;
            power = Mathf.Max(power, Mathf.Lerp(0.45f, 1f, intensity));
            radius = Mathf.Max(fresh ? 0f : radius, size);
            settle = seconds;
            block.SetColor(ColorId, color);
            Apply();
        }

        void Update()
        {
            power -= Time.deltaTime / Mathf.Max(settle, 0.01f);
            if (!Alive)
            {
                Destroy(gameObject);
                return;
            }
            // Desliza até o último som (o colega andando leva a marca junto).
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            Apply();
        }

        void Apply()
        {
            transform.localScale = Vector3.one * radius * 2f;
            block.SetFloat(StrengthId, Mathf.Clamp01(power));
            meshRenderer.SetPropertyBlock(block);
        }

        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Vibração (chão)" };
            quad.vertices = new[]
            {
                new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f),
            };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 }; // virado para cima
            quad.RecalculateNormals();
            quad.RecalculateBounds();
            return quad;
        }
    }
}
