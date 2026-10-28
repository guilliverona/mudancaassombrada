using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // A CASA REAGE (LEVEL_DESIGN_FASE1_MANSAO.md §9): esbarrou, caiu. Os livros da estante, a pilha de
    // pratos, o retrato da Galeria Leste, a gaveta do aparador, o telefone que toca, a cadeira de
    // balanço... Generaliza o TimedTopple (que derruba o manequim por tempo).
    //
    //  - O QUE DISPARA (pode marcar mais de um): alguém correndo perto, um móvel carregado perto,
    //    passar perto (com uma chance, sorteada uma vez), um segundo da noite, ou ninguém olhando.
    //  - QUEM DECIDE é o host: o "correndo" vem do computador de quem corre (só ele sabe), o resto
    //    o host vê sozinho (posições pela rede). Todos tocam a mesma animação, sem física em rede (R19).
    //  - REGRAS DO SUSTO (HouseScareRules): um a cada 20 s na casa inteira, nenhum no pânico, cada
    //    um uma vez por noite. O que só revela uma pilha (sem susto) ignora o intervalo e o pânico.
    //  - O QUE EMITE: o som 3D e o SoundEvent de impacto (borra o eco, faz anel para o Tiago), o
    //    aviso por sentido (GameCue) e, às vezes, larga uma pilha escondida (FlashlightPickup).
    //
    // O que se mexe é o "target" (o modelo, nunca a raiz de um móvel: a física dele é do dono).
    public class HouseScare : NetworkBehaviour
    {
        [Flags] public enum Trigger { None = 0, Running = 1, Carried = 2, PassNear = 4, AtSecond = 8, NobodyLooking = 16 }
        public enum Motion { None, Topple, Drop, Slide, Swing }

        static readonly List<HouseScare> all = new();
        public static IReadOnlyList<HouseScare> All => all;

        // Hora (Time.time do host) do último susto da casa. Um só para todos: "um a cada 20 s".
        static float lastScare = HouseScareRules.Never;

        [Header("Quando")]
        [SerializeField] Trigger trigger = Trigger.Running;
        [Tooltip("Raio (metros) para correr, móvel carregado, passar perto e ninguém olhando.")]
        [SerializeField] float radius = HouseScareRules.RunRadius;
        [Tooltip("Passar perto: a chance (sorteada uma vez só).")]
        [SerializeField, Range(0f, 1f)] float chance = 0.5f;
        [Tooltip("Segundo da noite em que acontece (gatilho AtSecond).")]
        [SerializeField] float atSecond = 90f;
        [SerializeField, Min(1)] int maxFires = 1;
        [Tooltip("Desligado: não é susto (só larga uma pilha): ignora o intervalo de 20 s e o pânico.")]
        [SerializeField] bool isScare = true;

        [Header("O que se mexe")]
        [SerializeField] Transform target;
        [SerializeField] Motion motion = Motion.None;
        [Tooltip("Topple: para onde tomba. Drop: deslocamento lateral até o chão. Slide: quanto anda. (No espaço do target.)")]
        [SerializeField] Vector3 direction = Vector3.forward;
        [SerializeField] float duration = 0.8f;
        [Tooltip("Swing: quanto balança (graus). A cadeira de balanço, 12; o telefone tocando, 4.")]
        [SerializeField] float swingDegrees = 12f;

        [Header("Som e aviso")]
        [SerializeField] string sound = "";
        [SerializeField] float soundRange = 10f;
        [Tooltip("De onde sai o som (vazio = do target). A porta da frente bate longe do vão de carga.")]
        [SerializeField] Transform soundAt;
        [SerializeField] string cue = "";
        [Tooltip("A pilha escondida que este susto larga (opcional).")]
        [SerializeField] FlashlightPickup reveals;

        readonly NetworkVariable<int> fired = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        GameObject voice;                 // a "voz" do susto: o SoundEvent de impacto toca ESTE som
        Vector3 restPosition, restLocal;  // onde o target começa (mundo e local)
        Quaternion restRotation, restLocalRotation;
        Vector3 dropTo;                   // Drop: o ponto no chão
        float playing = -1f;              // -1 = parado; 0..1 = animando
        bool rolled;                      // passar perto: já sorteou (no host)
        float nextLocalCheck;

        public int Fired => fired.Value;
        Transform Target => target != null ? target : transform;
        Vector3 SoundPosition => (soundAt != null ? soundAt.position : Target.position) + Vector3.up * 0.3f;

        void Awake()
        {
            var t = Target;
            restPosition = t.position;
            restRotation = t.rotation;
            restLocal = t.localPosition;
            restLocalRotation = t.localRotation;
            dropTo = FindFloor(restPosition + t.rotation * direction);

            if (string.IsNullOrEmpty(sound)) return;
            voice = new GameObject("Som do susto");
            voice.transform.SetParent(transform, false);
            voice.AddComponent<SoundVoice>().choices = new[] { new SoundChoice { type = SoundType.Impact, sound = sound } };
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            fired.OnValueChanged += OnFiredChanged;
            if (fired.Value > 0) Pose(1f); // entrou depois: já encontra caído, sem barulho
        }

        public override void OnNetworkDespawn() => fired.OnValueChanged -= OnFiredChanged;

        // ---------- Quem decide (o host), com a ajuda de quem corre ----------

        void Update()
        {
            Animate();
            var game = GameManager.Instance;
            if (!IsSpawned || game == null || game.Outcome != MatchOutcome.Playing) return;

            // "Correndo" só o computador de quem corre sabe: ele pede ao host.
            if ((trigger & Trigger.Running) != 0 && Time.time >= nextLocalCheck && LocalRunningNear())
            {
                nextLocalCheck = Time.time + 0.5f;
                RunningRpc();
            }
            if (IsServer && HostTriggered(game)) ServerTryFire();
        }

        bool LocalRunningNear()
        {
            var me = NetworkPlayer.Local;
            if (me == null || me.Movement == null) return false;
            return HouseScareRules.RunningNear(Vector3.Distance(me.transform.position, Target.position), me.Movement.IsRunning, radius);
        }

        [Rpc(SendTo.Server)]
        void RunningRpc() => ServerTryFire();

        bool HostTriggered(GameManager game)
        {
            Vector3 at = Target.position;
            if ((trigger & Trigger.AtSecond) != 0 && game.HasClock && game.NightDuration - game.TimeLeft >= atSecond) return true;
            if ((trigger & Trigger.Carried) != 0)
                foreach (var f in Furniture.All)
                    if (f.IsHeld && Vector3.Distance(f.transform.position, at) < radius) return true;
            if ((trigger & Trigger.PassNear) != 0 && !rolled)
                foreach (var p in NetworkPlayer.All)
                    if (HouseScareRules.PassNear(Vector3.Distance(p.transform.position, at), radius, rolled, UnityEngine.Random.value, chance, out rolled))
                        return true;
            if ((trigger & Trigger.NobodyLooking) != 0 && SomeoneNear(at) && HouseScareRules.NobodyLooking(FacingDots(at), LookCos))
                return true;
            return false;
        }

        static readonly float LookCos = Mathf.Cos(60f * Mathf.Deg2Rad);
        readonly List<float> dots = new();

        bool SomeoneNear(Vector3 at)
        {
            foreach (var p in NetworkPlayer.All)
                if (Vector3.Distance(p.transform.position, at) < radius) return true;
            return false;
        }

        // O quanto cada jogador está virado para o objeto (1 = olhando direto, -1 = de costas).
        List<float> FacingDots(Vector3 at)
        {
            dots.Clear();
            foreach (var p in NetworkPlayer.All)
            {
                Vector3 to = at - p.transform.position;
                to.y = 0;
                dots.Add(to.sqrMagnitude < 0.01f ? 1f : Vector3.Dot(p.transform.forward, to.normalized));
            }
            return dots;
        }

        void ServerTryFire()
        {
            bool panic = SoundDirector.Current == SoundDirector.Moment.Panic;
            bool ok = isScare
                ? HouseScareRules.CanFire(Time.time, lastScare, panic, fired.Value, maxFires)
                : fired.Value < maxFires;
            if (!ok) return;
            if (isScare) lastScare = Time.time;
            fired.Value++;
            if (reveals != null) reveals.ServerReveal();
        }

        // Só no host (ao reiniciar a fase): de pé de novo, sem sorteio feito, e o relógio dos sustos zera.
        public void ServerReset()
        {
            fired.Value = 0;
            rolled = false;
            lastScare = HouseScareRules.Never;
        }

        // ---------- O que todos veem ----------

        void OnFiredChanged(int previous, int current)
        {
            if (current == 0) { playing = -1f; Pose(0f); return; } // reinício
            if (current <= previous) return;
            playing = 0f;
            Pose(0f);
            if (voice != null) EchoSystem.EmitLocal(new SoundEvent(SoundPosition, soundRange, 0.8f, SoundType.Impact, voice));
            if (!string.IsNullOrEmpty(cue))
                GameCues.EmitLocal(new GameCue(SoundPosition, soundRange, Senses.Hearing | Senses.Vibration, cue, gameObject));
        }

        void Animate()
        {
            if (playing < 0f) return;
            playing = Mathf.Min(1f, playing + Time.deltaTime / Mathf.Max(0.05f, duration));
            Pose(playing);
            if (playing >= 1f) playing = -1f;
        }

        // A pose do target em t (0 = como começou, 1 = no fim).
        void Pose(float t)
        {
            var tr = Target;
            switch (motion)
            {
                case Motion.Topple:
                {
                    // Gira 90° pela quina do lado da queda; ao quadrado = começa devagar e acelera.
                    Vector3 dir = restRotation * direction;
                    dir.y = 0;
                    dir.Normalize();
                    Vector3 pivot = restPosition + dir * 0.2f;
                    var turn = Quaternion.AngleAxis(90f * t * t, Vector3.Cross(Vector3.up, dir));
                    tr.SetPositionAndRotation(pivot + turn * (restPosition - pivot), turn * restRotation);
                    break;
                }
                case Motion.Drop:
                {
                    // Cai até o chão (aceleração, ao quadrado) e deita de lado.
                    float k = t * t;
                    Vector3 pos = Vector3.Lerp(restPosition, dropTo, k);
                    var tilt = Quaternion.AngleAxis(80f * k, restRotation * Vector3.right);
                    tr.SetPositionAndRotation(pos, tilt * restRotation);
                    break;
                }
                case Motion.Slide:
                    // Anda e freia (a gaveta abrindo).
                    tr.localPosition = restLocal + restLocalRotation * direction * (1f - (1f - t) * (1f - t));
                    break;
                case Motion.Swing:
                    // Balança e para (a cadeira de balanço): volta para onde estava.
                    float angle = Mathf.Sin(t * Mathf.PI * 6f) * swingDegrees * (1f - t);
                    tr.localRotation = restLocalRotation * Quaternion.Euler(angle, 0, 0);
                    break;
            }
        }

        // O chão debaixo do ponto (para onde as coisas caem), sem contar o próprio objeto.
        Vector3 FindFloor(Vector3 from)
        {
            var hits = Physics.RaycastAll(from + Vector3.up * 0.1f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 floor = new(from.x, from.y - 1f, from.z);
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform) || (target != null && h.transform.IsChildOf(target))) continue;
                if (h.distance < best) { best = h.distance; floor = h.point; }
            }
            return floor + Vector3.up * 0.05f;
        }
    }
}
