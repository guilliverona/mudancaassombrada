using Unity.Netcode;
using UnityEngine;

namespace MudancaAssombrada
{
    // A CHAMADA da bengala (LEVEL_DESIGN_FASE1.md MEC-06): segurar a bengala por 2 s faz três
    // batidas fortes (20 m) e deixa um MARCADOR no chão por 6 s, visível para os três: um anel e
    // uma luz fria (o Marcelo vê, o Tiago vê o anel, a Cida ouve). É o "GPS" da equipe: reagrupa
    // quem se perdeu no anel da casa.
    //
    // Quem decide a hora é o CaneTapper (no dono); aqui só se espalha a chamada pela rede.
    public class CaneCall : NetworkBehaviour
    {
        [SerializeField] float range = 20f;
        [SerializeField] float markerSeconds = 6f;
        [SerializeField] Color markerColor = new Color(0.55f, 0.75f, 1f);

        // Chamado pelo CaneTapper do dono, com o ponto onde a bengala bateu.
        public void Call(Vector3 point)
        {
            // Intensidade 1 = o som de três batidas (cida_chamada), não a batida comum.
            EchoSystem.Emit(new SoundEvent(point, range, 1f, SoundType.Cane, gameObject));
            GameCues.Emit(new GameCue(point, 0f, Senses.Hearing | Senses.Vibration | Senses.Sight,
                "A Cida está chamando! (siga o marcador)", gameObject));
            if (IsSpawned) MarkerRpc(point);
            else SpawnMarker(point); // testando a cena sozinho, sem rede
        }

        [Rpc(SendTo.Everyone)]
        void MarkerRpc(Vector3 point) => SpawnMarker(point);

        void SpawnMarker(Vector3 point)
        {
            Vector3 ground = point + Vector3.up * 0.03f;
            VibrationRing.Spawn(ground, 1.2f, markerColor, markerSeconds);
            var go = new GameObject("Marcador da Chamada");
            go.transform.position = ground + Vector3.up * 0.6f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = markerColor;
            light.range = 4f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>(); // pulsa, para chamar o olho no escuro
            Destroy(go, markerSeconds);
        }
    }
}
