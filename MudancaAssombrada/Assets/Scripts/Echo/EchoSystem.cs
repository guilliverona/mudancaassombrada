using System;
using System.Collections.Generic;
using UnityEngine;

// Gerencia as ondas de som e o "modo cego" da Dona Cida.
// Qualquer script pode criar uma onda chamando: EchoSystem.Emit(posição, alcance, intensidade);
public class EchoSystem : MonoBehaviour
{
    const int MaxPulses = 8; // precisa ser igual ao MAX_PULSES do Echo.shader

    [SerializeField] Shader echoShader;
    [SerializeField] float waveSpeed = 7f;      // metros por segundo
    [SerializeField] float ringWidth = 0.35f;   // espessura do anel
    [SerializeField] Color echoColor = new Color(0.55f, 0.8f, 1f);

    // Singleton: guarda a única instância para os métodos static encontrarem.
    static EchoSystem instance;

    // struct = um "pacote" de dados simples. Cada onda é um destes.
    struct Pulse
    {
        public Vector3 origin;
        public float radius, maxRadius, intensity;
    }

    readonly Pulse[] pulses = new Pulse[MaxPulses];
    readonly Vector4[] pulseData = new Vector4[MaxPulses];
    readonly Vector4[] settingsData = new Vector4[MaxPulses];
    int nextSlot; // ondas novas substituem a mais antiga (fila circular)

    // Guarda os materiais originais para poder "devolver a visão" depois.
    readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    CameraClearFlags originalClearFlags;
    bool blind;

    // Shader.PropertyToID transforma o nome em número uma vez só (mais rápido que usar texto todo frame).
    static readonly int PulsesId = Shader.PropertyToID("_EchoPulses");
    static readonly int SettingsId = Shader.PropertyToID("_EchoSettings");
    static readonly int ColorId = Shader.PropertyToID("_EchoColor");
    static readonly int RingWidthId = Shader.PropertyToID("_EchoRingWidth");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        instance = this;
        if (echoShader == null) echoShader = Shader.Find("MudancaAssombrada/Echo");
    }

    // EVENTO: quem quiser "ouvir" os sons do jogo se inscreve aqui (padrão Observer).
    // O EchoSystem não sabe quem está ouvindo, e nem precisa saber.
    // Parâmetros: posição, alcance, intensidade, quem fez o som.
    public static event Action<Vector3, float, float, GameObject> SoundEmitted;

    public static void Emit(Vector3 origin, float maxRadius, float intensity = 1f, GameObject source = null)
    {
        // "?." = só chama se alguém estiver inscrito (senão seria null).
        // Fica antes do return: fantasmas ouvem mesmo sem o modo de eco na cena.
        SoundEmitted?.Invoke(origin, maxRadius, intensity, source);

        if (instance == null) return;
        instance.pulses[instance.nextSlot] = new Pulse { origin = origin, maxRadius = maxRadius, intensity = intensity };
        instance.nextSlot = (instance.nextSlot + 1) % MaxPulses; // % = resto da divisão: 7 -> 0
    }

    void Update()
    {

        for (int i = 0; i < MaxPulses; i++)
        {
            ref Pulse p = ref pulses[i]; // "ref" = mexer no original, não numa cópia
            if (p.intensity > 0)
            {
                p.radius += waveSpeed * Time.deltaTime;
                if (p.radius >= p.maxRadius) p.intensity = 0; // a onda morreu
            }

            // A onda enfraquece conforme se espalha.
            float fade = p.intensity > 0 ? p.intensity * (1f - p.radius / p.maxRadius) : 0f;
            pulseData[i] = new Vector4(p.origin.x, p.origin.y, p.origin.z, p.radius);
            settingsData[i] = new Vector4(fade, 0, 0, 0);
        }

        // Envia tudo para a placa de vídeo. Todos os materiais Echo leem isto.
        Shader.SetGlobalVectorArray(PulsesId, pulseData);
        Shader.SetGlobalVectorArray(SettingsId, settingsData);
        Shader.SetGlobalColor(ColorId, echoColor);
        Shader.SetGlobalFloat(RingWidthId, ringWidth);
    }

    // Quem decide se a tela fica cega agora é o CharacterSwitcher (Cida = cega).
    public void SetBlind(bool on)
    {
        // Proteção: ligar duas vezes salvaria os materiais de ECO como se fossem os
        // originais, e a visão normal nunca mais voltaria.
        if (on == blind) return;
        blind = on;
        var cam = Camera.main;

        if (on)
        {
            originalClearFlags = cam.clearFlags;
            cam.clearFlags = CameraClearFlags.SolidColor; // fundo preto em vez do céu
            cam.backgroundColor = Color.black;

            RefreshNewRenderers();
        }
        else
        {
            cam.clearFlags = originalClearFlags;
            foreach (var pair in originalMaterials)
                if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
            originalMaterials.Clear();
        }
    }

    // Objetos que aparecem DEPOIS de a tela escurecer (ex.: um jogador que acabou de conectar)
    // ainda estão com o material normal. Esta função "escurece" só os que faltam.
    public void RefreshNewRenderers()
    {
        if (!blind) return;

        foreach (var r in FindObjectsByType<Renderer>())
        {
            if (originalMaterials.ContainsKey(r)) continue; // já está escuro

            originalMaterials[r] = r.sharedMaterials;
            var echoMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < echoMats.Length; i++)
            {
                var original = r.sharedMaterials[i];
                echoMats[i] = new Material(echoShader);
                // Copia a cor original (se tiver) para o objeto manter um "tom".
                if (original != null && original.HasProperty(BaseColorId))
                    echoMats[i].SetColor(BaseColorId, original.GetColor(BaseColorId));
            }
            r.sharedMaterials = echoMats;
        }
    }
}
