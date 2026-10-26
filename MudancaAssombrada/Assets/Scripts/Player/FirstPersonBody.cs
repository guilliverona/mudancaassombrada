using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering; // ShadowCastingMode

namespace MudancaAssombrada
{
    // O corpo que VOCÊ vê ao olhar para baixo (só no seu computador).
    // O boneco original continua invisível para a sua câmera (DollWalk.HideBody): ele faz a sombra
    // e aparece no espelho, inteiro. Esta é uma CÓPIA dele sem a cabeça (a câmera fica dentro dela),
    // que copia a pose do original todo frame e não faz sombra nem aparece no espelho.
    //
    // Fica no boneco ORIGINAL: se ele for desligado (troca de personagem), a cópia some junto.
    public class FirstPersonBody : MonoBehaviour
    {
        // Registro das partes de todas as cópias: o MirrorReflection as desliga enquanto desenha.
        public static readonly List<Renderer> Parts = new();

        GameObject copy;
        PlayerMovement movement; // do dono: diz se o corpo está travado (caído/levantando)
        Transform[] source, target; // os mesmos ossos/partes, na mesma ordem, no original e na cópia
        Renderer[] sourceRenderers, targetRenderers;
        MaterialPropertyBlock block;

        // Cria (uma vez só) a cópia para este boneco.
        public static void For(DollWalk walk)
        {
            if (walk.GetComponent<FirstPersonBody>() != null) return;
            // Copia ANTES de pôr este componente: senão a cópia viria com ele e criaria outra cópia.
            var copy = Instantiate(walk.gameObject, walk.transform.parent, false);
            walk.gameObject.AddComponent<FirstPersonBody>().Setup(copy);
        }

        // Feito aqui (e não no Awake) porque o boneco pode estar desligado: o Awake esperaria.
        void Setup(GameObject clone)
        {
            block = new MaterialPropertyBlock();
            movement = GetComponentInParent<PlayerMovement>();
            copy = clone;
            copy.SetActive(gameObject.activeInHierarchy);
            copy.name = $"{name} (primeira pessoa)";

            // A cópia é só visual: sem scripts, sem colisão, sem outra cópia dela mesma.
            foreach (var c in copy.GetComponentsInChildren<DollWalk>(true)) DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Animator>(true)) c.enabled = false;

            source = GetComponentsInChildren<Transform>(true);
            target = copy.GetComponentsInChildren<Transform>(true);
            sourceRenderers = GetComponentsInChildren<Renderer>(true);
            targetRenderers = copy.GetComponentsInChildren<Renderer>(true);

            foreach (var r in targetRenderers)
            {
                r.shadowCastingMode = ShadowCastingMode.Off; // a sombra já vem do original
                Parts.Add(r);
            }
            HideHead();
        }

        // Sem cabeça: no modelo da ficha, o osso do PESCOÇO (e tudo preso nele: cabeça, cabelo,
        // óculos, gorro) encolhe a zero, senão a câmera, no rosto, enxerga o pescoço por dentro;
        // no boneco de blocos, some a parte "Cabeça".
        void HideHead()
        {
            var head = FindHead();
            if (head == null)
            {
                Debug.LogWarning($"{name}: não achei a cabeça; o corpo em primeira pessoa pode tampar a câmera.", this);
                return;
            }
            head.localScale = Vector3.zero;
            foreach (var r in head.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        Transform FindHead()
        {
            string bone = "Cabeça";
            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                // Pescoço se o esqueleto tiver (é opcional no Humanoid); senão, a cabeça.
                foreach (var b in animator.avatar.humanDescription.human)
                    if (b.humanName == "Head") bone = b.boneName;
                foreach (var b in animator.avatar.humanDescription.human)
                    if (b.humanName == "Neck") bone = b.boneName;
            }
            foreach (var t in target)
                if (t.name == bone) return t;
            return null;
        }

        void OnEnable() { if (copy != null) copy.SetActive(true); }
        void OnDisable() { if (copy != null) copy.SetActive(false); }

        void OnDestroy()
        {
            if (targetRenderers != null) foreach (var r in targetRenderers) Parts.Remove(r);
            if (copy != null) Destroy(copy);
        }

        // Depois do DollWalk balançar os ossos (Update): a cópia fica na mesma pose.
        // A escala não é copiada, senão a cabeça escondida voltaria.
        void LateUpdate()
        {
            if (copy == null) return;

            // Caindo, caído ou levantando (corpo travado): a câmera fica rente ao chão, colada nos
            // ombros e braços da cópia. Some até ele estar de pé; o original segue com a sombra,
            // o espelho e o corpo que os outros jogadores veem.
            bool show = movement == null || !movement.BodyLocked;
            if (copy.activeSelf != show) copy.SetActive(show);
            if (!show) return;

            for (int i = 0; i < source.Length && i < target.Length; i++)
            {
                if (source[i] == null || target[i] == null) continue;
                target[i].SetLocalPositionAndRotation(source[i].localPosition, source[i].localRotation);
                if (target[i].gameObject.activeSelf != source[i].gameObject.activeSelf)
                    target[i].gameObject.SetActive(source[i].gameObject.activeSelf);
            }

            // A cor do personagem (CharacterSwitcher) e os materiais do modo cego vêm do original.
            for (int i = 0; i < sourceRenderers.Length && i < targetRenderers.Length; i++)
            {
                if (sourceRenderers[i] == null || targetRenderers[i] == null) continue;
                sourceRenderers[i].GetPropertyBlock(block);
                targetRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
