using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS das poses de mão por cima da animação (camadas do Animator dos jogadores), sem
    // cena: quando cada pose vale e como ela entra e sai. O DollWalk pergunta aqui; os testes
    // (Assets/Tests/EditMode/HandPoseRulesTests.cs) conferem sem abrir uma fase.
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
    }
}
