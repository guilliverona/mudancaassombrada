using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS das poses de mão por cima da animação (camadas do Animator dos jogadores), sem
    // cena: quando cada pose vale e como ela entra e sai. O DollWalk e o HeldItems perguntam aqui; os
    // testes (Assets/Tests/EditMode/HandPoseRulesTests.cs e FlashlightArmRulesTests.cs) conferem sem
    // abrir uma fase.
    public static class HandPoseRules
    {
        // Pega da bengala (camada "Bengala": braço e dedos esquerdos). Só quem tem bengala na ficha,
        // e solta quando cai ou levanta: a queda e o levantar usam o corpo inteiro.
        public static float CaneWeight(bool holdsCane, bool down, bool gettingUp) =>
            holdsCane && !down && !gettingUp ? 1f : 0f;

        // Mão no chão (caído e levantando): os clipes do Mixamo apoiam a palma pensando num corpo
        // médio, e a mão grande do Marcelo (com luva) entrava até 24 cm no chão. Quanto subir a mão
        // para o ponto mais baixo dela ("lowest": palma e pontas dos dedos) ficar acima do chão,
        // contando a espessura ("clearance": dedo, luva). 0 = já está fora do chão.
        public static float FloorLift(float lowest, float floor, float clearance) =>
            Mathf.Max(0f, floor + clearance - lowest);

        // A pose entra e sai aos poucos ("speed" = quanto do caminho por segundo), sem tranco.
        public static float Smooth(float current, float target, float speed, float dt) =>
            Mathf.MoveTowards(current, target, speed * dt);

        // ---------- Braço da lanterna (camada "Lanterna" + IK no HeldItems) ----------

        // Lanterna NA MÃO (não no cinto, não guardada) e de pé: o braço direito sobe. Cair e levantar
        // usam o corpo inteiro, como na bengala.
        public static float FlashlightWeight(bool inHand, bool down, bool gettingUp) =>
            inHand && !down && !gettingUp ? 1f : 0f;

        // Um ponto preso a um referencial ("offset" em metros: direita, cima, frente).
        public static Vector3 HoldPoint(Vector3 eye, Quaternion look, Vector3 offset) => eye + look * offset;

        // Onde a mão vai para CARREGAR a lanterna (o que os colegas veem): a partir do OMBRO, em
        // "braços" (fraction: direita, cima, frente, multiplicados pelo tamanho do braço), no referencial
        // "frame" (o corpo, inclinado com o olhar: ArmPitch). Em braços, cada personagem segura na
        // altura e no alcance dele. Mão abaixo e à frente do ombro, cotovelo pouco dobrado: na pega da
        // lanterna (tools/blender/build_pega_lanterna.py), com o braço quase caído, o furo do punho já
        // aponta para a frente, então o punho fica quase reto.
        public static Vector3 CarryPoint(Vector3 shoulder, Quaternion frame, float reach, Vector3 fraction) =>
            shoulder + frame * (fraction * reach);

        // Divide um giro em TWIST (em volta de "axis": o antebraço virando, como o pulso de verdade) e
        // SWING (o resto: o punho dobrando). rotation = swing * twist.
        public static void SplitTwist(Quaternion rotation, Vector3 axis, out Quaternion twist, out Quaternion swing)
        {
            axis = axis.normalized;
            Vector3 r = new Vector3(rotation.x, rotation.y, rotation.z);
            Vector3 p = Vector3.Project(r, axis);
            twist = new Quaternion(p.x, p.y, p.z, rotation.w);
            float len = Mathf.Sqrt(twist.x * twist.x + twist.y * twist.y + twist.z * twist.z + twist.w * twist.w);
            twist = len < 1e-6f ? Quaternion.identity
                                : new Quaternion(twist.x / len, twist.y / len, twist.z / len, twist.w / len);
            swing = rotation * Quaternion.Inverse(twist);
        }

        // Lanterna no cinto: até onde vai o corpo para um lado ("side": −1 esquerda, +1 direita), na
        // altura "height" (± "band"), com os pontos da malha no espaço do corpo (X para a direita, Y para
        // cima). Quem chama tira os braços (eles também passam na altura do quadril). 0 = nenhum ponto.
        // Assim a lanterna encosta na cintura do Tiago e na barriga do Marcelo, cada um do seu tamanho.
        public static float BodySide(Vector3[] points, float height, float band, float side)
        {
            float best = 0f;
            foreach (var p in points)
                if (Mathf.Abs(p.y - height) <= band && p.x * side > best) best = p.x * side;
            return best;
        }

        // O braço acompanha só PARTE do olhar para cima/baixo ("follow"); o punho faz o resto. Inteiro,
        // olhar para cima ergueria o braço até o rosto.
        public static float ArmPitch(float lookPitch, float follow) => lookPitch * follow;

        // Suaviza um ponto: anda metade do caminho a cada "halfLife" segundos. Correndo, o ombro sacode
        // com a animação; com o alvo suavizado, a mão fica firme e só segue o movimento de verdade.
        public static Vector3 Damp(Vector3 current, Vector3 target, float halfLife, float dt) =>
            halfLife <= 0f ? target : Vector3.Lerp(current, target, 1f - Mathf.Pow(0.5f, dt / halfLife));

        // Um alvo fora do alcance puxa a mão até o braço quase esticado (98%: esticado de todo, o
        // cotovelo "estala" de um lado para o outro).
        public static Vector3 ClampToReach(Vector3 shoulder, Vector3 target, float reach)
        {
            Vector3 toTarget = target - shoulder;
            float max = reach * 0.98f;
            return toTarget.magnitude > max ? shoulder + toTarget.normalized * max : target;
        }

        // IK de dois ossos: onde fica o cotovelo para a mão chegar ao alvo, com o braço (upper) e o
        // antebraço (lower) do mesmo tamanho. Pela lei dos cossenos, o cotovelo fica num círculo em
        // volta da reta ombro-alvo; a "hint" escolhe o ponto do círculo (para baixo e para fora).
        public static Vector3 Elbow(Vector3 shoulder, Vector3 target, float upper, float lower, Vector3 hint)
        {
            Vector3 toTarget = target - shoulder;
            float d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upper - lower) + 1e-4f, upper + lower - 1e-4f);
            Vector3 axis = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector3.forward;
            // distância do ombro até o pé da perpendicular do cotovelo, e a altura do cotovelo
            float along = (upper * upper - lower * lower + d * d) / (2f * d);
            float height = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            Vector3 side = Vector3.ProjectOnPlane(hint, axis);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.down, axis);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.right, axis);
            return shoulder + axis * along + side.normalized * height;
        }
    }
}
