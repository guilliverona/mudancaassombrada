using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // O DESENHO do fantasma ("lógica não desenha"): só LÊ o estado do Ghost e mexe no modelo.
    // Não decide nada, então roda igual em todos os computadores, e todos veem a mesma coisa,
    // porque o estado e o disfarce vêm das NetworkVariables do Ghost.
    //
    //  - Inclina o corpo conforme o estado (curvado caçando, de cabeça baixa escutando).
    //  - DISFARCE: esconde o próprio corpo e veste o modelo da ficha do personagem copiado,
    //    ESPELHADO (escala X negativa): o logo do colete fica ao contrário, como num espelho.
    //    É a pista para quem enxerga; a Cida descobre pelo eco (sem passos, sem respiração).
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

        Ghost ghost;
        Quaternion restRotation;
        float lean;
        Renderer[] ownRenderers;  // o corpo do próprio fantasma (some quando disfarçado)
        GameObject disguiseModel; // o corpo emprestado (null = sem disfarce)

        void Awake()
        {
            ghost = GetComponent<Ghost>();
            if (model == null) model = transform.Find("Modelo");
            if (model != null) restRotation = model.localRotation;
            ownRenderers = GetComponentsInChildren<Renderer>(true);
        }

        void OnEnable() => ghost.DisguiseChanged += ShowDisguise;
        void OnDisable() => ghost.DisguiseChanged -= ShowDisguise;

        void ShowDisguise(int characterIndex)
        {
            if (disguiseModel != null) Destroy(disguiseModel);
            disguiseModel = null;

            var roster = CharacterRoster.Instance;
            var character = characterIndex >= 0 && roster != null && roster.IsValid(characterIndex) ? roster.Get(characterIndex) : null;
            bool disguised = character != null && character.model != null;
            foreach (var r in ownRenderers)
                if (r != null) r.enabled = !disguised;
            if (!disguised) return;

            // Os pés do fantasma ficam "baseOffset" abaixo do centro (ver NavMeshAgent).
            float feet = TryGetComponent(out NavMeshAgent agent) ? agent.baseOffset : 1f;
            disguiseModel = Instantiate(character.model, transform, false);
            disguiseModel.name = $"{character.displayName} (falso)";
            disguiseModel.transform.localPosition = new Vector3(0, -feet, 0);
            disguiseModel.transform.localScale = new Vector3(-1f, 1f, 1f); // ESPELHADO
            if (!disguiseModel.TryGetComponent(out DollWalk _)) disguiseModel.AddComponent<DollWalk>();

            // Se a tela está no modo eco (Cida), o corpo novo também precisa escurecer.
            if (EchoSystem.Instance != null) EchoSystem.Instance.RefreshNewRenderers();
        }

        void Update()
        {
            if (model == null) return;

            float target = ghost.State switch
            {
                GhostState.Investigating => investigateLean,
                GhostState.Hunting => huntLean,
                GhostState.Fleeing => fleeLean,
                _ => patrolLean,
            };
            // Lerp: a pose muda aos poucos, não "estala".
            lean = Mathf.Lerp(lean, target, smoothing * Time.deltaTime);
            model.localRotation = Quaternion.AngleAxis(lean, Vector3.right) * restRotation;
        }
    }
}
