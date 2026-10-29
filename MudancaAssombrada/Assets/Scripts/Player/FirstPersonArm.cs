using UnityEngine;

namespace MudancaAssombrada
{
    // A lanterna na SUA tela (fica no Player; só faz algo no dono).
    //
    // Os colegas veem o braço erguido segurando a lanterna (HeldItems). Na primeira pessoa, não: as
    // mãos não aparecem; você só vê a PONTA da lanterna no canto de baixo, apontando para a frente.
    // Por isso, com a lanterna na mão:
    //   - o braço direito da cópia de primeira pessoa (FirstPersonBody, o corpo que você vê) encolhe
    //     a zero, como a cabeça dela;
    //   - a lanterna vai para um ponto preso à câmera ("viewOffset"), igual para todos os personagens
    //     (preso à câmera, ela fica no mesmo lugar da tela e não balança correndo), e a luz sai da lente.
    // Os bonecos têm cabeça grande: os olhos ficam 30-40 cm acima do ombro. Por isso a lanterna da mão
    // de verdade (à frente do ombro) ficaria fora da tela, e a da tela fica presa à câmera.
    // A CARTELA DE PILHAS faz o mesmo, num lugar mais à vista ("batteryViewOffset"): pequena, ela
    // aparece inteira no canto de baixo, quase em pé e virada para você ver as duas pilhas.
    //
    // Roda DEPOIS do FirstPersonBody (200), que copia a pose do modelo para a cópia.
    [DefaultExecutionOrder(300)]
    public class FirstPersonArm : MonoBehaviour
    {
        [Tooltip("Onde fica o cabo da lanterna, em metros a partir da câmera (direita, cima, frente). " +
                 "Medido em batch: olhando reto, só a ponta (a lente) aparece no canto de baixo, à direita.")]
        [SerializeField] Vector3 viewOffset = new(0.17f, -0.2f, 0.25f);
        [Tooltip("Onde fica a aba da cartela de pilhas, em metros a partir da câmera (direita, cima, frente).")]
        [SerializeField] Vector3 batteryViewOffset = new(0.11f, -0.15f, 0.32f);
        [Tooltip("Graus: negativo = a frente da cartela sobe e as pilhas viram para você; e para dentro.")]
        [SerializeField] float batteryViewTilt = -55f, batteryViewTurn = -15f;

        // Os do dono (um por computador): o espelho pede para a lanterna voltar ao corpo no reflexo.
        static readonly System.Collections.Generic.List<FirstPersonArm> owners = new();

        HeldItems held;
        Flashlight flashlight;

        GameObject copy;      // a cópia atual (muda com o personagem)
        Transform copyArm;    // o braço direito da cópia (encolhe com a lanterna na mão)
        bool hidden;

        void Awake()
        {
            held = GetComponent<HeldItems>();
            flashlight = GetComponent<Flashlight>();
        }

        void OnDisable() => owners.Remove(this);

        // O espelho (MirrorReflection) mostra o seu corpo, não a primeira pessoa: enquanto a câmera do
        // reflexo desenha (reflecting), a lanterna fica na mão do corpo, e não no lugar da sua tela.
        public static void ShowInMirror(bool reflecting)
        {
            foreach (var arm in owners)
                if (arm != null && arm.held != null) arm.held.ShowHeldOnBody(reflecting);
        }

        void LateUpdate()
        {
            if (held == null || flashlight == null || !flashlight.IsOwner) return;
            if (!owners.Contains(this)) owners.Add(this);
            FindCopyArm();

            bool holding = held.ArmRaised;
            ShowArm(!holding);
            if (!holding) return;

            // Presa à câmera. Entrando (a pose ainda subindo), vem da mão de verdade até o lugar da tela.
            Transform eye = flashlight.Beam.parent; // a câmera do jogador
            float weight = held.Walk != null ? held.Walk.FlashlightWeight : 1f;
            if (held.BatteryInHand)
            {
                // A cartela, quase em pé e virada para você (não aponta: é só para ver o pacote).
                Vector3 at = HandPoseRules.HoldPoint(eye.position, eye.rotation, batteryViewOffset);
                Quaternion facing = eye.rotation * Quaternion.Euler(batteryViewTilt, batteryViewTurn, 0f);
                held.PlaceBatteries(Vector3.Lerp(held.BatteryGrip, at, weight), facing);
                return;
            }
            // A lanterna, apontando para onde você olha.
            Vector3 point = HandPoseRules.HoldPoint(eye.position, eye.rotation, viewOffset);
            Vector3 grip = Vector3.Lerp(held.LanternGrip, point, weight);
            held.PlaceLantern(grip, eye.rotation, moveBeam: weight > 0.99f);
        }

        // O braço direito da cópia: achado pelo nome do osso do modelo (a cópia é um clone dele).
        void FindCopyArm()
        {
            var walk = held.Walk;
            var body = walk != null ? walk.GetComponent<FirstPersonBody>() : null;
            var current = body != null ? body.Copy : null;
            if (current == copy) return;
            copy = current;
            copyArm = null;
            hidden = false;
            var animator = walk != null ? walk.GetComponent<Animator>() : null;
            var bone = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
            if (copy == null || bone == null) return;
            foreach (var t in copy.GetComponentsInChildren<Transform>(true))
                if (t.name == bone.name) { copyArm = t; break; }
        }

        // A escala não é copiada pelo FirstPersonBody (por isso a cabeça escondida não volta): basta
        // mudar quando troca.
        void ShowArm(bool show)
        {
            if (copyArm == null || hidden == !show) return;
            copyArm.localScale = show ? Vector3.one : Vector3.zero;
            hidden = !show;
        }
    }
}
