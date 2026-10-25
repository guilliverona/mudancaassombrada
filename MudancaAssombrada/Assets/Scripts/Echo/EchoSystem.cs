using System;
using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // O "rádio" de todos os sons do jogo, e o desenho das ondas no "modo cego".
    // Qualquer script faz um som chamando:
    //   EchoSystem.Emit(new SoundEvent(posição, alcance, intensidade, SoundType.Footstep, gameObject));
    // e qualquer script ouve se inscrevendo em EchoSystem.SoundEmitted.
    public class EchoSystem : MonoBehaviour
    {
        const int MaxPulses = 8; // precisa ser igual ao MAX_PULSES do Echo.shader

        [SerializeField] Shader echoShader;
        [SerializeField] float waveSpeed = 7f;      // metros por segundo
        [SerializeField] float ringWidth = 0.35f;   // espessura do anel
        [SerializeField] Color echoColor = new Color(0.55f, 0.8f, 1f);
        [Tooltip("Quais tipos de som viram onda na tela de quem enxerga pelo eco.")]
        [SerializeField] SoundType visibleSounds = SoundType.Everything;

        // A instância da fase atual (null no menu, onde não há eco).
        // É um "serviço da cena": existe um só, e os outros scripts falam com ele por aqui.
        public static EchoSystem Instance { get; private set; }

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

        // A tela deste computador está no "modo cego" (só o eco)?
        public bool IsBlind => blind;

        // Shader.PropertyToID transforma o nome em número uma vez só (mais rápido que usar texto todo frame).
        static readonly int PulsesId = Shader.PropertyToID("_EchoPulses");
        static readonly int SettingsId = Shader.PropertyToID("_EchoSettings");
        static readonly int ColorId = Shader.PropertyToID("_EchoColor");
        static readonly int RingWidthId = Shader.PropertyToID("_EchoRingWidth");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            Instance = this;
            if (echoShader == null) echoShader = Shader.Find("MudancaAssombrada/Echo");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // EVENTO: quem quiser "ouvir" os sons do jogo se inscreve aqui (padrão Observer).
        // O EchoSystem não sabe quem está ouvindo, e nem precisa saber.
        public static event Action<SoundEvent> SoundEmitted;

        // Quem envia os sons para os outros computadores (o SoundRelay se cadastra aqui ao conectar).
        // Sem rede (null), o jogo funciona igual: só local.
        public static Action<SoundEvent> NetworkBroadcast;

        // Todo script do jogo chama Emit, sem saber que existe rede.
        public static void Emit(SoundEvent sound)
        {
            EmitLocal(sound);
            NetworkBroadcast?.Invoke(sound);
        }

        // Só neste computador. O SoundRelay usa ESTA versão ao receber um som de outro jogador,
        // senão o som seria reenviado de volta para a rede, para sempre (eco infinito!).
        public static void EmitLocal(SoundEvent sound)
        {
            // "?." = só chama se alguém estiver inscrito (senão seria null).
            // Fica antes do return: fantasmas ouvem mesmo sem o modo de eco na cena.
            SoundEmitted?.Invoke(sound);

            if (Instance == null || !sound.Is(Instance.visibleSounds)) return;
            Instance.pulses[Instance.nextSlot] = new Pulse { origin = sound.Position, maxRadius = sound.Range, intensity = sound.Intensity };
            Instance.nextSlot = (Instance.nextSlot + 1) % MaxPulses; // % = resto da divisão: 7 -> 0
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

        // Quem decide se a tela fica cega agora é o CharacterSwitcher (pela ficha do personagem).
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
}
