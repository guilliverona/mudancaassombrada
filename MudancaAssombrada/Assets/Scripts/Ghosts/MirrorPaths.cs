using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MudancaAssombrada
{
    // Qual espelho o Imitador usa, contando o CAMINHO e não a linha reta (R03): numa casa de dois
    // níveis, o espelho logo acima da cabeça pode estar a 30 m andando. Mede cada espelho pelo
    // NavMesh e pergunta à regra (ImitatorRules.NearestMirror/FarthestMirror, com testes).
    // Só no host; chamado poucas vezes (quando ele entra, sai, puxa ou foge), não a cada quadro.
    public static class MirrorPaths
    {
        static NavMeshPath path;
        static readonly List<Mirror> mirrors = new();

        public static Mirror Nearest(Vector3 from) => Pick(from, nearest: true);
        public static Mirror Farthest(Vector3 from) => Pick(from, nearest: false);

        static Mirror Pick(Vector3 from, bool nearest)
        {
            mirrors.Clear();
            foreach (var h in GhostHideout.All)
                if (h is Mirror m && m.isActiveAndEnabled) mirrors.Add(m);
            var walk = new float[mirrors.Count];
            var straight = new float[mirrors.Count];
            for (int i = 0; i < mirrors.Count; i++)
            {
                Vector3 exit = mirrors[i].ExitPoint;
                straight[i] = Vector3.Distance(from, exit);
                walk[i] = Walk(from, exit);
            }
            int pick = nearest ? ImitatorRules.NearestMirror(walk, straight) : ImitatorRules.FarthestMirror(walk, straight);
            return pick >= 0 ? mirrors[pick] : null;
        }

        // Metros andando pelo NavMesh (-1 = não dá para chegar, ou não há NavMesh ali).
        static float Walk(Vector3 a, Vector3 b)
        {
            path ??= new NavMeshPath();
            if (!NavMesh.SamplePosition(a, out var ha, 2.5f, NavMesh.AllAreas)) return -1f;
            if (!NavMesh.SamplePosition(b, out var hb, 2.5f, NavMesh.AllAreas)) return -1f;
            if (!NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                return -1f;
            float length = 0f;
            var corners = path.corners;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }
    }
}
