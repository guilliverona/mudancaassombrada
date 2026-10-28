using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    public struct PortalTick
    {
        public float AloneTime; // há quanto tempo a pessoa está sozinha na Câmara
        public bool Warn;       // passou do aviso AGORA (o vidro escurece, a voz sussurra)
        public bool Pull;       // passou do limite: o portal puxa
    }

    // As REGRAS do portal da Câmara Secreta (R13, LEVEL_DESIGN_FASE1_MANSAO.md), sem cena nem rede.
    // O portal é a casa do Imitador e tem gatilho próprio: quem fica SOZINHO na Câmara é puxado sem
    // disfarce. Para não punir quem só entrou para pegar o cartaz: só vale depois de mexer no cartaz
    // ("armado"), avisa antes, e um colega por perto zera a conta. Os testes ficam em MansionRulesTests.
    public static class PortalRules
    {
        public static PortalTick Tick(bool armed, bool alone, float aloneTime, float dt, float warnAt, float pullAt)
        {
            if (!armed || !alone) return new PortalTick();
            float t = aloneTime + dt;
            return new PortalTick { AloneTime = t, Warn = aloneTime < warnAt && t >= warnAt, Pull = t >= pullAt };
        }

        // Sozinho = nenhum OUTRO jogador a menos de "radius" metros (a própria posição não conta).
        public static bool IsAlone(Vector3 me, IEnumerable<Vector3> players, float radius)
        {
            foreach (var p in players)
            {
                float d = Vector3.Distance(me, p);
                if (d > 0.01f && d < radius) return false;
            }
            return true;
        }
    }
}
