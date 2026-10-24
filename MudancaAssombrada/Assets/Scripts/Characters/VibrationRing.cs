using UnityEngine;

// Um anel no chão que cresce um pouco e desaparece. Desenhado com LineRenderer:
// uma "linha" que passa por vários pontos; ligando 32 pontos em círculo, vira um anel.
[RequireComponent(typeof(LineRenderer))]
public class VibrationRing : MonoBehaviour
{
    const int Segments = 32;
    static Material sharedMaterial; // um material só para todos os anéis (economiza memória)

    LineRenderer line;
    float maxRadius, lifetime, age;
    Color color;

    // "Fábrica": quem quer um anel chama Spawn, sem precisar saber como ele é montado.
    public static void Spawn(Vector3 position, float maxRadius, Color color, float lifetime)
    {
        if (sharedMaterial == null) sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        var go = new GameObject("Vibração");
        go.transform.position = position;
        var ring = go.AddComponent<VibrationRing>(); // o LineRenderer vem junto (RequireComponent)
        ring.maxRadius = maxRadius;
        ring.color = color;
        ring.lifetime = lifetime;

        ring.line = go.GetComponent<LineRenderer>();
        ring.line.sharedMaterial = sharedMaterial;
        ring.line.loop = true;            // liga o último ponto ao primeiro
        ring.line.useWorldSpace = false;  // pontos relativos ao objeto
        ring.line.positionCount = Segments;
        ring.line.widthMultiplier = 0.06f;
        ring.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.UpdateShape();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        UpdateShape();
    }

    void UpdateShape()
    {
        float t = age / lifetime; // 0 = nasceu, 1 = vai sumir

        // Cresce rápido no começo e desacelera: 1 - (1-t)² é uma "curva de easing".
        float radius = maxRadius * (1f - (1f - t) * (1f - t));
        radius = Mathf.Max(radius, 0.05f);

        for (int i = 0; i < Segments; i++)
        {
            // Trigonometria: o ponto i de um círculo fica em (cos, sen) do ângulo i/Segments de uma volta.
            float angle = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }

        Color c = color;
        c.a = 1f - t; // vai ficando transparente
        line.startColor = c;
        line.endColor = c;
    }
}
