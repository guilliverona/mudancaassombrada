using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // A visão de vibrações (Tiago): ele não ouve, mas VÊ o chão tremer.
    // Cada emissor (colega, fantasma, móvel) tem UMA marca de chão tremendo (VibrationMark), que
    // vai atrás dele e reacende a cada som. Quem decide força, alcance e quem sai quando enche é o
    // VibrationRules: colegas tremem fraco e amarelo, perigo treme forte e vermelho.
    // Desligar este componente (enabled = false) desliga a visão; o CharacterSwitcher faz isso.
    public class VibrationVision : MonoBehaviour
    {
        [SerializeField] Shader tremorShader;
        [Tooltip("Segundos para um tremor fraco (colega, casa) acalmar sem som novo.")]
        [SerializeField] float weakSeconds = 1.2f;
        [Tooltip("Segundos para um tremor forte (perigo) acalmar sem som novo.")]
        [SerializeField] float strongSeconds = 2.5f;
        [Tooltip("No máximo isto de marcas ao mesmo tempo (um colega nunca tira um perigo).")]
        [SerializeField] int maxMarks = 6;
        [SerializeField] Color weakColor = new Color(1f, 0.85f, 0.3f);  // vibração fraca: amarelo
        [SerializeField] Color strongColor = new Color(1f, 0.2f, 0.3f); // vibração forte: vermelho

        // A chave é quem fez o som (o GameObject). Som sem dono (o ambiente) ganha uma chave nova.
        readonly Dictionary<object, VibrationMark> marks = new();
        readonly List<object> keys = new();
        Material material;

        // Mesmo evento que o fantasma usa para ouvir. Um evento, vários "ouvintes" diferentes:
        // o fantasma persegue, o Tiago enxerga. Nenhum dos dois sabe que o outro existe.
        void OnEnable() => EchoSystem.SoundEmitted += OnSound;

        void OnDisable()
        {
            EchoSystem.SoundEmitted -= OnSound;
            foreach (var m in marks.Values) if (m != null) Destroy(m.gameObject);
            marks.Clear();
        }

        void OnSound(SoundEvent sound)
        {
            bool fromGhost = sound.Source != null && sound.Source.GetComponentInParent<Ghost>() != null;
            var kind = VibrationRules.Classify(sound.Type, fromGhost, sound.Source == gameObject);
            if (kind == VibrationKind.None) return;

            // Os sons nascem no ar (ex.: ponto de impacto). Projetamos no chão com um raio para baixo.
            Vector3 ground = sound.Position;
            if (Physics.Raycast(sound.Position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                ground = hit.point;

            Vector3 feet = transform.position + Vector3.down * 0.9f;
            Vector3 flat = ground - feet;
            float heightDiff = flat.y;
            flat.y = 0;
            if (!VibrationRules.InReach(kind, flat.magnitude, heightDiff)) return;

            Prune();
            object key = sound.Source != null ? sound.Source : new object();
            if (!marks.TryGetValue(key, out var mark))
            {
                if (!MakeRoom(kind)) return;
                if (material == null)
                {
                    if (tremorShader == null) tremorShader = Shader.Find("MudancaAssombrada/VibrationTremor");
                    material = new Material(tremorShader);
                }
                mark = VibrationMark.Create(material);
                marks[key] = mark;
            }

            bool strong = kind == VibrationKind.Strong;
            float size = strong ? Mathf.Lerp(1.2f, 2.2f, sound.Intensity) : Mathf.Lerp(0.5f, 0.9f, sound.Intensity);
            mark.Pulse(ground, kind, sound.Intensity, size, strong ? strongSeconds : weakSeconds,
                       strong ? strongColor : weakColor);
        }

        // Tira das contas as marcas que já acalmaram (elas se destroem sozinhas).
        void Prune()
        {
            keys.Clear();
            foreach (var pair in marks) if (pair.Value == null || !pair.Value.Alive) keys.Add(pair.Key);
            foreach (var k in keys) marks.Remove(k);
        }

        // Cabe mais uma marca? Se não couber, o VibrationRules diz qual sai (ou que a nova fica de fora).
        bool MakeRoom(VibrationKind incoming)
        {
            keys.Clear();
            keys.AddRange(marks.Keys);
            var power = new float[keys.Count];
            var kinds = new VibrationKind[keys.Count];
            for (int i = 0; i < keys.Count; i++) { power[i] = marks[keys[i]].Power; kinds[i] = marks[keys[i]].Kind; }

            int victim = VibrationRules.ChooseVictim(power, kinds, incoming, maxMarks);
            if (victim == VibrationRules.Room) return true;
            if (victim == VibrationRules.Skip) return false;
            Destroy(marks[keys[victim]].gameObject);
            marks.Remove(keys[victim]);
            return true;
        }
    }
}
