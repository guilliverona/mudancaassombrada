using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Monta o Animator Controller dos jogadores a partir dos clipes de Assets/Art/Animations
    // (baixados do Mixamo, importados como Humanoid pelo ArtImportSettings).
    //
    // Duas misturas 2D pela DIREÇÃO do movimento, medida em relação ao próprio boneco
    // (Lado = para a direita, Frente = para a frente, em metros por segundo):
    //
    //   Em pé:    Parado no meio; andar a 4 m/s e correr a 7,2 m/s para frente, trás, esquerda e direita
    //   Agachado: AgachadoParado no meio; andar agachado a 2 m/s nas quatro direções
    //             (não há clipe "agachado para trás": é o AgachadoAndar tocado ao contrário)
    //   Em pé <-> Agachado: pelo parâmetro Agachado (bool)
    //
    // As velocidades são as do PlayerMovement (andar 4, correr x1,8, agachado x0,5). Quem preenche
    // os parâmetros é o DollWalk, em todos os computadores (a posição e o agachar vêm pela rede).
    // Entre as direções (ex.: frente-esquerda), o Unity mistura os clipes vizinhos sozinho.
    //
    // Só cria se não existir, se for de uma versão antiga (sem os parâmetros de direção) ou se
    // chegou um clipe novo da lista ClipNames. Fora isso, o controller é de quem edita (no Animator
    // do Unity) — refazer apaga as edições à mão.
    public static class AnimationBuilder
    {
        public const string ControllerPath = "Assets/Art/Animations/Jogador.controller";
        const string ClipsFolder = "Assets/Art/Animations/";

        // Os nomes dos parâmetros: o DollWalk usa os mesmos.
        public const string ForwardParam = "Frente";
        public const string SideParam = "Lado";
        public const string CrouchParam = "Agachado";

        const float Walk = 4f, Run = 7.2f, Crouch = 2f;

        // Todos os clipes que o controller sabe usar (Agachar fica de fora: ainda não é usado).
        static readonly string[] ClipNames =
        {
            "Parado", "Andar", "AndarTras", "AndarEsquerda", "AndarDireita",
            "Correr", "CorrerTras", "CorrerEsquerda", "CorrerDireita",
            "AgachadoParado", "AgachadoAndar", "AgachadoTras", "AgachadoEsquerda", "AgachadoDireita",
        };

        [MenuItem("Mudança Assombrada/Criar Animações do Jogador", priority = 23)]
        static void Menu()
        {
            var controller = EnsureController();
            if (controller != null) Selection.activeObject = controller;
        }

        public static RuntimeAnimatorController EnsureController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                // Refaz do zero se for a versão antiga (só pela velocidade, sem direção) ou se chegou
                // um clipe que ele ainda não usa (ex.: AgachadoTras no lugar do "andar ao contrário").
                var used = existing.animationClips.Select(c => c.name).ToHashSet();
                var missing = ClipNames.Where(n => Clip(n) != null && !used.Contains(n)).ToList();
                if (existing.parameters.Any(p => p.name == SideParam) && missing.Count == 0) return existing;
                AssetDatabase.DeleteAsset(ControllerPath);
                Debug.Log(missing.Count > 0
                    ? $"Animator dos jogadores refeito com os clipes novos: {string.Join(", ", missing)}"
                    : "Animator dos jogadores: versão antiga (sem direção) substituída.");
            }

            var idle = Clip("Parado");
            var walk = Clip("Andar");
            var crouchIdle = Clip("AgachadoParado");
            var crouchWalk = Clip("AgachadoAndar");
            if (idle == null || walk == null || crouchIdle == null || crouchWalk == null)
            {
                Debug.LogWarning($"Animações do jogador: faltam clipes em {ClipsFolder} " +
                                 "(Parado, Andar, AgachadoParado, AgachadoAndar). O boneco segue com a animação por código.");
                return null;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(ForwardParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(SideParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(CrouchParam, AnimatorControllerParameterType.Bool);

            // Em pé. Sem um clipe de direção, usa o "Andar" (para trás: tocado ao contrário).
            var standing = controller.CreateBlendTreeInController("Em pé", out var stand, 0);
            Directional(stand);
            stand.AddChild(idle, Vector2.zero);
            Add(stand, Clip("Andar"), new Vector2(0, Walk));
            Add(stand, Clip("AndarTras"), new Vector2(0, -Walk), walk);
            Add(stand, Clip("AndarEsquerda"), new Vector2(-Walk, 0), walk);
            Add(stand, Clip("AndarDireita"), new Vector2(Walk, 0), walk);
            Add(stand, Clip("Correr"), new Vector2(0, Run), walk);
            Add(stand, Clip("CorrerTras"), new Vector2(0, -Run), Clip("AndarTras") ?? walk);
            Add(stand, Clip("CorrerEsquerda"), new Vector2(-Run, 0), Clip("AndarEsquerda") ?? walk);
            Add(stand, Clip("CorrerDireita"), new Vector2(Run, 0), Clip("AndarDireita") ?? walk);

            var crouched = controller.CreateBlendTreeInController("Agachado", out var crouch, 0);
            Directional(crouch);
            crouch.AddChild(crouchIdle, Vector2.zero);
            Add(crouch, crouchWalk, new Vector2(0, Crouch));
            Add(crouch, Clip("AgachadoTras"), new Vector2(0, -Crouch), crouchWalk);
            Add(crouch, Clip("AgachadoEsquerda"), new Vector2(-Crouch, 0), crouchWalk);
            Add(crouch, Clip("AgachadoDireita"), new Vector2(Crouch, 0), crouchWalk);

            var machine = controller.layers[0].stateMachine;
            machine.defaultState = standing;
            Transition(standing, crouched, true);
            Transition(crouched, standing, false);

            AssetDatabase.SaveAssets();
            Debug.Log($"Animator dos jogadores criado: {ControllerPath}");
            return controller;
        }

        // Mistura 2D "direcional": cada clipe num ponto (Lado, Frente); o Unity mistura os vizinhos.
        static void Directional(BlendTree tree)
        {
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = SideParam;
            tree.blendParameterY = ForwardParam;
        }

        // Põe o clipe no ponto. Faltou o clipe? Usa o substituto; se o substituto anda para a FRENTE
        // e o ponto é para TRÁS, toca ao contrário (velocidade -1): andar de ré "de mentira".
        static void Add(BlendTree tree, AnimationClip clip, Vector2 position, AnimationClip fallback = null)
        {
            bool reversed = false;
            if (clip == null)
            {
                clip = fallback;
                reversed = position.y < 0 && clip != null && !clip.name.EndsWith("Tras");
            }
            if (clip == null) return;
            tree.AddChild(clip, position);
            if (!reversed) return;
            var children = tree.children;
            children[children.Length - 1].timeScale = -1f;
            tree.children = children;
        }

        // Troca de estado sem esperar o clipe acabar, num "crossfade" curto (o mesmo tempo
        // em que a câmera desce ao agachar).
        static void Transition(AnimatorState from, AnimatorState to, bool crouch)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.2f;
            t.AddCondition(crouch ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, CrouchParam);
        }

        // O clipe de dentro do FBX (o Unity também guarda um "__preview__", que ignoramos).
        static AnimationClip Clip(string file) =>
            AssetDatabase.LoadAllAssetsAtPath($"{ClipsFolder}{file}.fbx")
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview"));
    }
}
