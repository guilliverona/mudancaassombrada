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
        // O diapasão (SoundType.Tuning) não é um estalo: a onda anda devagar e fica "cantando" uns
        // 4 s, e cada superfície responde com o seu timbre (SurfaceTag) enquanto ela passa.
        const float TuningSpeed = 2f;
        const float TuningMaxRadius = 8f;
        // Ponto (EchoWave.Spot): os sons que não são da Cida só brilham onde nasceram, sem varrer a sala.
        const float SpotMaxRadius = 1.2f;
        const float SpotSpeed = 4f;
        // No máximo isto de ondas cheias (bengala/diapasão) ao mesmo tempo: a 4ª tira a mais fraca.
        const int MaxFullWaves = 3;

        [SerializeField] Shader echoShader;
        [SerializeField] float waveSpeed = 7f;      // metros por segundo
        [SerializeField] float ringWidth = 0.35f;   // espessura do anel
        [SerializeField] Color echoColor = new Color(0.55f, 0.8f, 1f);
        [Tooltip("Quais tipos de som viram onda na tela de quem enxerga pelo eco.")]
        [SerializeField] SoundType visibleSounds = SoundType.Everything;
        [Header("Visão de perto e memória")]
        [Tooltip("Raio (m) do vulto sem cor em volta da Cida: o 'tato', só o que está quase encostado nela.")]
        [SerializeField] float nearRadius = 1.5f;
        [Tooltip("Quanto clareia o vulto de perto (0 = tela 100% escura, como antes).")]
        [Range(0f, 0.5f)] [SerializeField] float nearStrength = 0.06f;
        [Tooltip("Força do contorno que fica no que a bengala acabou de revelar (0 = sem memória).")]
        [Range(0f, 0.5f)] [SerializeField] float memoryStrength = 0.1f;
        [Tooltip("Segundos até o contorno da memória sumir: ela precisa bater a bengala de novo.")]
        [SerializeField] float memorySeconds = 4f;

        // A instância da fase atual (null no menu, onde não há eco).
        // É um "serviço da cena": existe um só, e os outros scripts falam com ele por aqui.
        public static EchoSystem Instance { get; private set; }

        // struct = um "pacote" de dados simples. Cada onda é um destes.
        struct Pulse
        {
            public Vector3 origin;
            public float radius, maxRadius, intensity, speed;
            public bool tuning;
            public EchoWave wave;
            public GameObject source; // para juntar sons repetidos do mesmo emissor
            public float born;        // Time.time de quando nasceu
        }

        // Ruído em volta de quem está jogando neste computador (0 = silêncio, 1 = colado numa fonte
        // ligada). Encolhe as ondas (bengala: 12 m → 4 m), engrossa a borda e encurta a vida delas.
        public static float NoiseLevel { get; private set; }

        // Objetos com timbre: o material de eco de cada um e o timbre aplicado por último
        // (o espelho muda de "vidro" para "vidro rachado" quando o Imitador entra nele).
        struct Tagged
        {
            public SurfaceTag tag;
            public Material[] materials;
            public SurfaceType applied;
        }
        readonly List<Tagged> tagged = new();
        static readonly int TimbreId = Shader.PropertyToID("_Timbre");

        // Memória: as peças fixas da casa (sem NetworkObject: nem jogador, nem fantasma, nem móvel)
        // ficam com um contorno fraco depois que a bengala passa por elas, e ele APAGA em memorySeconds.
        struct Remembered
        {
            public Renderer renderer;
            public Material[] materials;
            public float lastWave; // quando nasceu a última onda que passou aqui (para marcar uma vez por onda)
        }
        readonly List<Remembered> remembered = new();
        static readonly int RevealedId = Shader.PropertyToID("_Revealed");

        readonly Pulse[] pulses = new Pulse[MaxPulses];
        readonly Vector4[] pulseData = new Vector4[MaxPulses];
        readonly Vector4[] settingsData = new Vector4[MaxPulses];
        readonly float[] slotStrength = new float[MaxPulses]; // força atual de cada vaga (0 = livre)
        readonly EchoWave[] slotWave = new EchoWave[MaxPulses];

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
        static readonly int ListenerId = Shader.PropertyToID("_EchoListener");
        static readonly int NearId = Shader.PropertyToID("_EchoNear");

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
            Instance.Draw(sound);
        }

        // Escolhe o desenho (EchoRules), junta com uma onda recente do mesmo emissor ou acha uma vaga.
        void Draw(SoundEvent sound)
        {
            var wave = EchoRules.Classify(sound.Type, sound.Intensity, IsMine(sound.Source));
            if (wave == EchoWave.None) return;

            // Num lugar barulhento a onda vai menos longe (só no desenho: a regra do fantasma não muda).
            float range = sound.Range * Mathf.Lerp(1f, 1f / 3f, NoiseLevel);
            bool tuning = sound.Is(SoundType.Tuning);
            var pulse = new Pulse
            {
                origin = sound.Position,
                maxRadius = wave == EchoWave.Spot ? Mathf.Min(range, SpotMaxRadius)
                          : tuning ? Mathf.Min(range, TuningMaxRadius) : range,
                intensity = sound.Intensity,
                speed = wave == EchoWave.Spot ? SpotSpeed : tuning ? TuningSpeed : waveSpeed,
                tuning = tuning,
                wave = wave,
                source = sound.Source,
                born = Time.time,
            };

            // Mesmo emissor logo depois: reaproveita a vaga em vez de abrir outra onda.
            for (int i = 0; i < MaxPulses; i++)
            {
                ref Pulse p = ref pulses[i];
                if (p.intensity <= 0 || p.source == null) continue;
                if (!EchoRules.ShouldMergeWave(wave, p.source == sound.Source, p.wave == wave, Time.time - p.born)) continue;
                pulse.intensity = Mathf.Max(pulse.intensity, p.intensity);
                Put(i, pulse);
                return;
            }

            int slot = EchoRules.ChooseSlot(slotStrength, slotWave, wave, MaxFullWaves);
            if (slot >= 0) Put(slot, pulse);
        }

        void Put(int slot, Pulse pulse)
        {
            pulses[slot] = pulse;
            slotStrength[slot] = pulse.intensity;
            slotWave[slot] = pulse.wave;
        }

        // O boneco DESTE computador? (Sem rede, testando sozinho, o único jogador conta como meu.)
        static bool IsMine(GameObject source)
        {
            if (source == null) return false;
            if (NetworkPlayer.Local != null) return source == NetworkPlayer.Local.gameObject;
            return source.TryGetComponent(out NetworkPlayer _);
        }

        void Update()
        {
            // O ruído é medido onde está a câmera de quem joga aqui (a Cida, quando é ela).
            var cam = Camera.main;
            NoiseLevel = cam != null ? NoiseSource.LevelAt(cam.transform.position) : 0f;

            for (int i = 0; i < MaxPulses; i++)
            {
                ref Pulse p = ref pulses[i]; // "ref" = mexer no original, não numa cópia
                if (p.intensity > 0)
                {
                    p.radius += p.speed * Time.deltaTime;
                    if (p.radius >= p.maxRadius) p.intensity = 0; // a onda morreu
                    // Onda cheia de forma (a bengala) passando por uma peça da casa: ela fica na memória.
                    else if (p.wave == EchoWave.Full && !p.tuning) Reveal(p.origin, p.radius, p.born);
                }

                // A onda enfraquece conforme se espalha (e mais rápido no meio do barulho).
                float fade = p.intensity > 0 ? p.intensity * (1f - p.radius / p.maxRadius) * (1f - NoiseLevel * 0.5f) : 0f;
                pulseData[i] = new Vector4(p.origin.x, p.origin.y, p.origin.z, p.radius);
                // y = é do diapasão? z = velocidade (o shader calcula há quanto tempo a onda passou).
                // y: 0 = onda de forma, 1 = diapasão, 2 = ponto.
                float kind = p.wave == EchoWave.Spot ? 2f : p.tuning ? 1f : 0f;
                settingsData[i] = new Vector4(fade, kind, p.speed, p.maxRadius);
                slotStrength[i] = fade;
                slotWave[i] = fade > 0 ? p.wave : EchoWave.None;
            }

            // Envia tudo para a placa de vídeo. Todos os materiais Echo leem isto.
            Shader.SetGlobalVectorArray(PulsesId, pulseData);
            Shader.SetGlobalVectorArray(SettingsId, settingsData);
            Shader.SetGlobalColor(ColorId, echoColor);
            Shader.SetGlobalFloat(RingWidthId, ringWidth * (1f + NoiseLevel * 2f));
            if (cam != null) Shader.SetGlobalVector(ListenerId, cam.transform.position);
            Shader.SetGlobalVector(NearId, new Vector4(nearRadius, nearStrength, memoryStrength, memorySeconds));

            // Timbres que mudam sozinhos (o espelho com o Imitador dentro).
            for (int i = 0; i < tagged.Count; i++)
            {
                var t = tagged[i];
                if (t.tag == null) continue;
                var now = t.tag.Current;
                if (now == t.applied) continue;
                foreach (var m in t.materials) m.SetFloat(TimbreId, (float)now);
                t.applied = now;
                tagged[i] = t;
            }
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
                tagged.Clear();
                remembered.Clear();
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
                var tag = r.GetComponentInParent<SurfaceTag>();
                var timbre = tag != null ? tag.Current : SurfaceType.Default;
                var echoMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < echoMats.Length; i++)
                {
                    var original = r.sharedMaterials[i];
                    echoMats[i] = new Material(echoShader);
                    // Copia a cor original (se tiver) para o objeto manter um "tom".
                    if (original != null && original.HasProperty(BaseColorId))
                        echoMats[i].SetColor(BaseColorId, original.GetColor(BaseColorId));
                    echoMats[i].SetFloat(TimbreId, (float)timbre); // do que é feito (para o diapasão)
                }
                r.sharedMaterials = echoMats;
                if (tag != null) tagged.Add(new Tagged { tag = tag, materials = echoMats, applied = timbre });
                if (r.GetComponentInParent<Unity.Netcode.NetworkObject>() == null)
                {
                    foreach (var mat in echoMats) mat.SetFloat(RevealedId, -1000f); // nunca revelado
                    remembered.Add(new Remembered { renderer = r, materials = echoMats, lastWave = -1f });
                }
            }
        }

        // Marca na memória as peças da casa que a frente da onda acabou de alcançar. O material guarda
        // o HORÁRIO (no relógio do shader, _Time.y) e o shader apaga o contorno com o tempo.
        void Reveal(Vector3 origin, float radius, float waveBorn)
        {
            float sqr = radius * radius;
            for (int i = 0; i < remembered.Count; i++)
            {
                var m = remembered[i];
                if (m.lastWave == waveBorn || m.renderer == null) continue;
                if ((m.renderer.bounds.ClosestPoint(origin) - origin).sqrMagnitude > sqr) continue;
                foreach (var mat in m.materials) mat.SetFloat(RevealedId, Time.timeSinceLevelLoad);
                m.lastWave = waveBorn;
                remembered[i] = m;
            }
        }
    }
}
