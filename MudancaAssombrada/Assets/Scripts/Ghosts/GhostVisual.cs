using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // O DESENHO do fantasma ("lógica não desenha"): só LÊ o estado do Ghost e mexe no modelo.
    // Não decide nada, então roda igual em todos os computadores, e todos veem a mesma coisa,
    // porque o estado e o disfarce vêm das NetworkVariables do Ghost.
    //
    //  - Inclina o corpo conforme o estado (curvado caçando, de cabeça baixa escutando, a reverência).
    //  - A cartola (osso "hat", se o modelo tiver) tomba para a frente na reverência.
    //  - A máscara (blendshape "Tragedia", se tiver) vira TRAGÉDIA enquanto foge desmascarado.
    //  - DISFARCE: esconde o próprio corpo e veste o modelo da ficha do personagem copiado,
    //    ESPELHADO (escala X negativa): o logo do colete fica ao contrário, como num espelho.
    //    É a pista para quem enxerga; a Cida descobre pelo eco (sem passos, sem respiração).
    //  - DENTRO DO ESPELHO (GhostState.InMirror): nenhum corpo e nenhum colisor. Ele está "no
    //    reflexo": só a Cida percebe, porque o espelho ocupado brilha diferente no eco (Echo.shader).
    // Quando houver animações (Animator), é aqui que elas entram: o resto do fantasma nem muda.
    [RequireComponent(typeof(Ghost))]
    public class GhostVisual : MonoBehaviour
    {
        [Tooltip("O desenho (o filho \"Modelo\" com o FBX). Vazio = procura o filho \"Modelo\".")]
        [SerializeField] Transform model;
        [SerializeField] float smoothing = 4f; // quão rápido muda de pose

        [Header("Inclinação para frente (graus) em cada estado")]
        [SerializeField] float patrolLean = 0f;
        [SerializeField] float investigateLean = 8f;  // "escutando" o chão
        [SerializeField] float huntLean = 18f;        // curvado, avançando
        [SerializeField] float fleeLean = -8f;        // jogado para trás, assustado
        [SerializeField] float performLean = -4f;     // peito estufado, em cena
        [SerializeField] float bowLean = 40f;         // a reverência
        [SerializeField] float bowHatTilt = 35f;      // a cartola tomba junto

        const string HatBone = "hat";
        const string TragedyShape = "Tragedia";

        Ghost ghost;
        Quaternion restRotation;
        float lean, hatTilt, tragedy;
        Renderer[] ownRenderers;  // o corpo do próprio fantasma (some quando disfarçado)
        Collider[] colliders;     // somem dentro do espelho (ninguém esbarra nem aponta para o nada)
        GameObject disguiseModel; // o corpo emprestado (null = sem disfarce)
        bool inMirror;
        Transform hat;
        Quaternion hatRest;
        SkinnedMeshRenderer mask;
        int tragedyIndex = -1;

        void Awake()
        {
            ghost = GetComponent<Ghost>();
            if (model == null) model = transform.Find("Modelo");
            if (model != null)
            {
                restRotation = model.localRotation;
                hat = FindDeep(model, HatBone);
                if (hat != null) hatRest = hat.localRotation;
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    int index = skin.sharedMesh != null ? skin.sharedMesh.GetBlendShapeIndex(TragedyShape) : -1;
                    if (index >= 0) { mask = skin; tragedyIndex = index; break; }
                }
            }
            ownRenderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponents<Collider>();
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        void OnEnable() => ghost.DisguiseChanged += ShowDisguise;
        void OnDisable() => ghost.DisguiseChanged -= ShowDisguise;

        // Quem aparece: o corpo próprio, o emprestado (disfarce) ou nenhum (dentro do espelho).
        void ApplyVisibility()
        {
            bool disguised = disguiseModel != null;
            foreach (var r in ownRenderers)
                if (r != null) r.enabled = !inMirror && !disguised;
            if (disguised) disguiseModel.SetActive(!inMirror);
            foreach (var c in colliders)
                if (c != null) c.enabled = !inMirror;
        }

        void ShowDisguise(int characterIndex)
        {
            if (disguiseModel != null) Destroy(disguiseModel);
            disguiseModel = null;

            var roster = CharacterRoster.Instance;
            var character = characterIndex >= 0 && roster != null && roster.IsValid(characterIndex) ? roster.Get(characterIndex) : null;
            bool disguised = character != null && character.model != null;
            if (!disguised) { ApplyVisibility(); return; }

            // Os pés do fantasma ficam "baseOffset" abaixo do centro (ver NavMeshAgent).
            float feet = TryGetComponent(out NavMeshAgent agent) ? agent.baseOffset : 1f;
            disguiseModel = Instantiate(character.model, transform, false);
            disguiseModel.name = $"{character.displayName} (falso)";
            disguiseModel.transform.localPosition = new Vector3(0, -feet, 0);
            disguiseModel.transform.localScale = new Vector3(-1f, 1f, 1f); // ESPELHADO
            if (!disguiseModel.TryGetComponent(out DollWalk _)) disguiseModel.AddComponent<DollWalk>();
            ApplyVisibility();

            // Se a tela está no modo eco (Cida), o corpo novo também precisa escurecer.
            if (EchoSystem.Instance != null) EchoSystem.Instance.RefreshNewRenderers();
        }

        // LateUpdate: depois de uma animação (se um dia houver), para a cartola e a máscara valerem.
        void LateUpdate()
        {
            var state = ghost.State;
            // Lido a cada quadro (e não por evento): quem entra no meio da partida já vê certo.
            if (ghost.IsInsideMirror != inMirror)
            {
                inMirror = ghost.IsInsideMirror;
                ApplyVisibility();
            }
            if (model == null) return;

            float target = state switch
            {
                GhostState.Investigating => investigateLean,
                GhostState.Hunting => huntLean,
                GhostState.Fleeing => fleeLean,
                GhostState.Performing => performLean,
                GhostState.Bowing => bowLean,
                _ => patrolLean,
            };
            // Lerp: a pose muda aos poucos, não "estala".
            lean = Mathf.Lerp(lean, target, smoothing * Time.deltaTime);
            model.localRotation = Quaternion.AngleAxis(lean, Vector3.right) * restRotation;

            if (hat != null)
            {
                hatTilt = Mathf.Lerp(hatTilt, state == GhostState.Bowing ? bowHatTilt : 0f, smoothing * Time.deltaTime);
                // Gira em volta do "lado" do fantasma (o mesmo eixo da inclinação): não depende de como o osso veio do FBX.
                hat.localRotation = hatRest;
                hat.rotation = Quaternion.AngleAxis(hatTilt, transform.right) * hat.rotation;
            }

            if (mask != null)
            {
                // A máscara gira para tragédia rápido (o susto) e volta à comédia devagar.
                bool tragic = state == GhostState.Fleeing;
                tragedy = Mathf.MoveTowards(tragedy, tragic ? 100f : 0f, (tragic ? 250f : 60f) * Time.deltaTime);
                mask.SetBlendShapeWeight(tragedyIndex, tragedy);
            }
        }
    }
}
