using System.Collections.Generic;
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
    //   Em pé:    Parado no meio; andar a 2,2 m/s e correr a 4,4 m/s para frente, trás, esquerda e direita
    //   Agachado: AgachadoParado no meio; andar agachado a 1,1 m/s nas quatro direções
    //             (para a esquerda: o AgachadoDireita espelhado; clipe "para trás" que faltar é
    //             o da frente tocado ao contrário)
    //   Em pé <-> Agachado: pelo parâmetro Agachado (bool)
    //   Caído (bool Caido): de QUALQUER estado vai para "Caindo" (o clipe Cair, que para deitado
    //   no último quadro); ao ser ajudado, "Levantando" (Levantar) e volta para Em pé.
    //
    // As velocidades são as do PlayerMovement (andar 2,2, correr x2, agachado x0,5). Quem preenche
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
        public const string DownParam = "Caido";
        public const string RescueParam = "Ajuda";

        // As velocidades do PlayerMovement (andar 2,2, correr x2, agachado x0,5), em m/s.
        public const float Walk = 2.2f, Run = 4.4f, Crouch = 1.1f;

        // A velocidade NATURAL de cada clipe (são "in place": o boneco não sai do lugar), medida
        // pelo pé de apoio deslizando para trás, num corpo de tamanho 1 (Animator.humanScale = 1).
        // Cada clipe toca a (velocidade do ponto ÷ esta), e o DollWalk ainda divide pelo tamanho de
        // cada personagem: assim o pé fica parado no chão. Os "Esquerda" usam o mesmo do "Direita".
        // Clipe novo ou trocado = medir de novo (média da Cida, do Tiago e do Marcelo).
        static readonly Dictionary<string, float> NaturalSpeed = new()
        {
            { "Andar", 1.33f }, { "AndarTras", 1.0f }, { "AndarDireita", 1.4f }, { "AndarEsquerda", 1.4f },
            { "Correr", 3.55f }, { "CorrerTras", 2.42f }, { "CorrerDireita", 3.43f }, { "CorrerEsquerda", 3.43f },
            { "AgachadoAndar", 1.1f }, { "AgachadoTras", 1.32f }, { "AgachadoDireita", 0.85f },
        };

        // Todos os clipes que o controller sabe usar (Agachar fica de fora: ainda não é usado).
        static readonly string[] ClipNames =
        {
            "Parado", "Andar", "AndarTras", "AndarEsquerda", "AndarDireita",
            "Correr", "CorrerTras", "CorrerEsquerda", "CorrerDireita",
            "AgachadoParado", "AgachadoAndar", "AgachadoTras", "AgachadoDireita",
            "Cair", "Levantar",
            "PegaBengala",
            // AgachadoEsquerda fica de fora: veio girado do Mixamo, usamos o AgachadoDireita espelhado.
        };

        // Camada "Bengala": a pega da bengala (pose-chave da ficha de mãos da Cida, gerada pelo
        // tools/blender/build_cida_blockout.py) por cima de tudo, só no braço e nos dedos esquerdos.
        // Começa com peso 0; o DollWalk liga para quem tem bengala na ficha (HandPoseRules).
        public const string CaneLayer = "Bengala";
        const string CaneMaskPath = ClipsFolder + "MascaraBengala.mask";

        public static RuntimeAnimatorController EnsureController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                // Refaz do zero se for a versão antiga (só pela velocidade, sem direção) ou se chegou
                // um clipe que ele ainda não usa (ex.: AgachadoTras no lugar do "andar ao contrário").
                var used = existing.animationClips.Select(c => c.name).ToHashSet();
                var missing = ClipNames.Where(n => Clip(n) != null && !used.Contains(n)).ToList();
                // ...ou se ainda usa um clipe que saiu da lista (ex.: o AgachadoEsquerda girado).
                var removed = used.Where(n => !ClipNames.Contains(n)).ToList();
                // ...ou se falta um parâmetro de uma versão mais nova (ex.: Ajuda, do resgate segurando a tecla).
                bool upToDate = new[] { SideParam, RescueParam }.All(n => existing.parameters.Any(p => p.name == n))
                    // ...ou se foi feito para outra velocidade (ex.: o antigo andar a 4 m/s).
                    && MaxForward(existing) == Run;
                if (upToDate && missing.Count == 0 && removed.Count == 0)
                    return existing;
                AssetDatabase.DeleteAsset(ControllerPath);
                Debug.Log(missing.Count + removed.Count > 0
                    ? $"Animator dos jogadores refeito (novos: {string.Join(", ", missing)}; retirados: {string.Join(", ", removed)})"
                    : "Animator dos jogadores: versão antiga substituída (faltava um parâmetro).");
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
            controller.AddParameter(DownParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(RescueParam, AnimatorControllerParameterType.Float);

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
            // Para a esquerda: o AgachadoDireita ESPELHADO. O "Crouched Sneaking Left" do Mixamo vem
            // girado ~160° (anda para a esquerda, mas de costas) e o boneco virava ao contrário.
            var crouchRight = Clip("AgachadoDireita");
            if (crouchRight != null) AddMirrored(crouch, crouchRight, new Vector2(-Crouch, 0));
            else Add(crouch, null, new Vector2(-Crouch, 0), crouchWalk);
            Add(crouch, crouchRight, new Vector2(Crouch, 0), crouchWalk);

            var machine = controller.layers[0].stateMachine;
            machine.defaultState = standing;
            Transition(standing, crouched, true);
            Transition(crouched, standing, false);
            AddFalling(machine, standing, crouched);
            AddCaneLayer(controller);

            AssetDatabase.SaveAssets();
            Debug.Log($"Animator dos jogadores criado: {ControllerPath}");
            return controller;
        }

        // Caído: de Em pé ou Agachado para "Caindo" assim que Caido liga. O clipe Cair não é loop:
        // termina deitado e FICA no último quadro.
        // Levantar é SEGURAR a tecla (PlayerCondition): o estado "Levantando" não toca sozinho, o
        // tempo dele É o parâmetro Ajuda (0 a 1, o progresso do resgate). Soltou a tecla, a Ajuda
        // volta e a animação volta junto, até ele ficar deitado de novo. Completo (Caido desliga),
        // volta para Em pé. Sem os clipes, nada muda (o boneco só trava).
        // (Sem Any State de propósito: ele dispararia de novo a queda quando a Ajuda voltasse a zero.)
        static void AddFalling(AnimatorStateMachine machine, AnimatorState standing, AnimatorState crouched)
        {
            var fall = Clip("Cair");
            var getUp = Clip("Levantar");
            if (fall == null || getUp == null) return;

            var falling = machine.AddState("Caindo");
            falling.motion = fall;
            var rising = machine.AddState("Levantando");
            rising.motion = getUp;
            rising.timeParameterActive = true;
            rising.timeParameter = RescueParam;

            foreach (var from in new[] { standing, crouched })
            {
                var down = from.AddTransition(falling);
                down.hasExitTime = false;
                down.hasFixedDuration = true;
                down.duration = 0.15f;
                down.AddCondition(AnimatorConditionMode.If, 0, DownParam);
            }

            // Alguém começou a ajudar. A queda termina DE LADO e o levantar começa DE BRUÇOS:
            // uma troca longa vira uma rolada suave em vez de um tranco.
            var up = falling.AddTransition(rising);
            up.hasExitTime = false;
            up.hasFixedDuration = true;
            up.duration = 0.6f;
            up.AddCondition(AnimatorConditionMode.Greater, 0.01f, RescueParam);

            // Levantou de vez (o host tirou o Caido): volta a andar.
            var back = rising.AddTransition(standing);
            back.hasExitTime = false;
            back.hasFixedDuration = true;
            back.duration = 0.5f;
            back.AddCondition(AnimatorConditionMode.IfNot, 0, DownParam);
        }

        // A pega da bengala numa camada própria, com máscara: só o braço e os dedos esquerdos
        // obedecem a ela; o resto do corpo continua andando, correndo e agachando pela camada de baixo.
        static void AddCaneLayer(AnimatorController controller)
        {
            var hold = Clip("PegaBengala");
            if (hold == null) return;

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(CaneMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, CaneMaskPath);
            }
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.LeftFingers);
            EditorUtility.SetDirty(mask);

            controller.AddLayer(CaneLayer);
            var layers = controller.layers;
            var layer = layers[layers.Length - 1];
            layer.avatarMask = mask;
            layer.defaultWeight = 0f;
            var state = layer.stateMachine.AddState("Pega da bengala");
            state.motion = hold;
            layer.stateMachine.defaultState = state;
            controller.layers = layers;
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
            var children = tree.children;
            children[children.Length - 1].timeScale = Pace(clip, position) * (reversed ? -1f : 1f);
            tree.children = children;
        }

        // O mesmo clipe, espelhado (direita vira esquerda). Só funciona em clipes Humanoid.
        static void AddMirrored(BlendTree tree, AnimationClip clip, Vector2 position)
        {
            tree.AddChild(clip, position);
            var children = tree.children;
            children[children.Length - 1].mirror = true;
            children[children.Length - 1].timeScale = Pace(clip, position);
            tree.children = children;
        }

        // Quantas vezes mais rápido o clipe toca para o pé acompanhar a velocidade do ponto
        // (num corpo de tamanho 1). Clipe sem medida na tabela: toca no ritmo original.
        static float Pace(AnimationClip clip, Vector2 position) =>
            NaturalSpeed.TryGetValue(clip.name, out var natural) ? position.magnitude / natural : 1f;

        // A maior velocidade "para a frente" da mistura Em pé: diz para qual velocidade o controller foi feito.
        static float MaxForward(AnimatorController controller)
        {
            var tree = controller.layers[0].stateMachine.states
                .Where(s => s.state.name == "Em pé").Select(s => s.state.motion).OfType<BlendTree>().FirstOrDefault();
            return tree == null ? 0f : tree.children.Max(c => c.position.y);
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
