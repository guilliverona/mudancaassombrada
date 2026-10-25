using UnityEngine;

namespace MudancaAssombrada
{
    // COMPORTAMENTO "imitar" (o Imitador, Seu Reflexo, da Casa do Espelho). Fica junto de um Ghost.
    //
    //   Escondido (no espelho) --tempo--> Disfarçado (copia quem está mais longe do grupo)
    //        ^                                  | chegou perto: "segura a outra ponta?"
    //        |                                  v
    //   Fugindo <--desmascarado-- Puxando (arrasta a vítima até o espelho; chegou = pegou)
    //
    // Ele NÃO faz passos nem respira (não tem Breathing): no eco da Cida, a cópia é a única
    // pessoa "muda". Qualquer jogador pode APONTAR para ele (tecla Interagir) e desmascará-lo.
    public class GhostImitator : GhostBehaviour
    {
        [Header("Tempos")]
        [SerializeField] float lurkTime = 12f;       // escondido no espelho antes de atacar
        [SerializeField] float giveUpTime = 40f;     // disfarçado sem conseguir chegar: volta para o espelho

        [Header("Velocidades")]
        [SerializeField] float approachSpeed = 3.5f; // anda como um jogador, para não chamar atenção
        [SerializeField] float pullSpeed = 1.2f;     // arrastando a vítima
        [SerializeField] float fleeSpeed = 5f;

        [Header("Ataque")]
        [SerializeField] float askDistance = 2f;     // perto assim, pede ajuda e começa a puxar
        [SerializeField] float voiceRange = 6f;      // "segura a outra ponta do armário?"

        float timer;
        NetworkPlayer victim;
        Mirror targetMirror;

        public override void ServerStart()
        {
            // Sem passos e sem pegar ninguém encostando: ele puxa para o espelho.
            Ghost.Footsteps = false;
            Ghost.CatchesOnTouch = false;
            Lurk();
        }

        public override void ServerReset()
        {
            Release();
            ServerStart();
        }

        public override void ServerUnmasked()
        {
            // "É falso!": larga a vítima, volta ao próprio corpo e foge para OUTRO espelho.
            Release();
            Ghost.ServerSetDisguise(-1);
            Ghost.ServerSetState(GhostState.Fleeing);
            Ghost.Agent.speed = fleeSpeed;
            targetMirror = Mirror.Farthest(transform.position);
            if (targetMirror != null) Ghost.Agent.SetDestination(targetMirror.ExitPoint);
        }

        public override void ServerTick()
        {
            switch (Ghost.State)
            {
                case GhostState.Patrolling: // escondido, esperando a hora
                    timer -= Time.deltaTime;
                    if (timer <= 0) TryDisguise();
                    break;

                case GhostState.Disguised:
                    timer -= Time.deltaTime;
                    if (victim == null || !victim.IsSpawned || timer <= 0) { Lurk(); break; }
                    Ghost.Agent.SetDestination(victim.transform.position);
                    if (Vector3.Distance(transform.position, victim.transform.position) < askDistance) StartPull();
                    break;

                case GhostState.Pulling:
                    if (victim == null || !victim.IsSpawned) { Lurk(); break; }
                    // Chegou no espelho com a vítima: ela foi para "dentro do reflexo".
                    if (Ghost.Arrived())
                    {
                        Release();
                        Ghost.ServerCatch();
                    }
                    break;

                case GhostState.Fleeing:
                    // Entrou no outro espelho: some e espera para tentar de novo.
                    if (Ghost.Arrived()) Lurk();
                    break;
            }
        }

        void Lurk()
        {
            Release();
            Ghost.ServerSetDisguise(-1);
            Ghost.ServerSetState(GhostState.Patrolling);
            Ghost.Agent.ResetPath(); // fica parado na frente do espelho
            timer = lurkTime;
        }

        void TryDisguise()
        {
            var copied = MostIsolatedPlayer();
            if (copied == null) { timer = 1f; return; } // ninguém conectado ainda

            // A vítima: o jogador mais perto, que NÃO seja quem ele copiou
            // (um "Tiago" chegando no próprio Tiago não engana ninguém). Sozinho, é o único que há.
            victim = NearestPlayer(except: NetworkPlayer.All.Count > 1 ? copied : null);
            if (victim == null) { timer = 1f; return; }

            // Sai do espelho mais perto da vítima, já disfarçado.
            var mirror = Mirror.Nearest(victim.transform.position);
            if (mirror != null) Ghost.Agent.Warp(mirror.ExitPoint);

            Ghost.ServerSetDisguise(copied.Character.CurrentIndex);
            Ghost.ServerSetState(GhostState.Disguised);
            Ghost.Agent.speed = approachSpeed;
            timer = giveUpTime;
        }

        void StartPull()
        {
            // Pede ajuda com a voz (a Cida "ouve" a voz no eco, mas sem passos nem respiração junto).
            EchoSystem.Emit(new SoundEvent(transform.position, voiceRange, 0.6f, SoundType.Voice, gameObject));

            targetMirror = Mirror.Nearest(transform.position);
            if (targetMirror == null) { Ghost.ServerCatch(); return; } // cena sem espelho: pega direto
            Ghost.ServerSetState(GhostState.Pulling);
            Ghost.Agent.speed = pullSpeed;
            Ghost.Agent.SetDestination(targetMirror.ExitPoint);
            victim.SetPulledRpc(Ghost.NetworkObject, true);
        }

        // Solta a vítima (se estava puxando alguém).
        void Release()
        {
            if (victim != null && victim.IsSpawned && Ghost.State == GhostState.Pulling)
                victim.SetPulledRpc(Ghost.NetworkObject, false);
            victim = null;
        }

        // Quem está mais longe dos outros (a maior distância média até o resto do grupo).
        static NetworkPlayer MostIsolatedPlayer()
        {
            NetworkPlayer best = null;
            float bestDistance = -1f;
            foreach (var p in NetworkPlayer.All)
            {
                float sum = 0;
                foreach (var other in NetworkPlayer.All)
                    if (other != p) sum += Vector3.Distance(p.transform.position, other.transform.position);
                if (sum > bestDistance) { bestDistance = sum; best = p; }
            }
            return best;
        }

        NetworkPlayer NearestPlayer(NetworkPlayer except)
        {
            NetworkPlayer best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in NetworkPlayer.All)
            {
                if (p == except) continue;
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < bestDistance) { bestDistance = d; best = p; }
            }
            return best;
        }
    }
}
