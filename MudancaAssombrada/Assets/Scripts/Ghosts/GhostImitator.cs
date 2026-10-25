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

        GhostState mode = GhostState.Patrolling; // o MEU estado (no Ghost só quando estou no volante)
        float timer;
        NetworkPlayer victim;
        Mirror targetMirror;

        public override void ServerStart() => Lurk();

        // Escondido, quase não quer o corpo; disfarçado ou puxando, não larga por nada.
        public override int ServerPriority() => mode switch
        {
            GhostState.Disguised or GhostState.Pulling => 20,
            GhostState.Fleeing => 15,
            _ => timer <= 0 ? 20 : 0, // escondido: quando chega a hora, pede o volante para atacar
        };

        // Sem o volante, só conta o tempo escondido.
        public override void ServerIdleTick()
        {
            if (mode == GhostState.Patrolling) timer -= Time.deltaTime;
        }

        // Ganhou o volante: sem passos e sem pegar ninguém encostando (ele puxa para o espelho).
        public override void ServerGainControl()
        {
            Ghost.Footsteps = false;
            Ghost.CatchesOnTouch = false;
            Ghost.ServerSetState(mode);
        }

        // Perdeu o volante (outro comportamento do mesmo fantasma, no Inquilino): tira o disfarce.
        public override void ServerLoseControl()
        {
            if (mode == GhostState.Patrolling) return;
            Release();
            Ghost.ServerSetDisguise(-1);
            mode = GhostState.Patrolling;
            timer = lurkTime;
        }

        // Muda o MEU estado; o Ghost (que todos veem) só muda se eu estiver no volante.
        void SetMode(GhostState newMode)
        {
            mode = newMode;
            if (HasControl) Ghost.ServerSetState(newMode);
        }
        public override void ServerReset()
        {
            Release();
            ServerStart();
        }

        public override void ServerUnmasked()
        {
            // "É falso!": larga a vítima, volta ao próprio corpo e foge para OUTRO espelho.
            Cue(Senses.Sight, $"O \"{CopiedName()}\" era falso! Fugiu para um espelho.");
            Cue(Senses.Hearing, "Um barulho de vidro trincando... o falso fugiu!");
            Release();
            Ghost.ServerSetDisguise(-1);
            SetMode(GhostState.Fleeing);
            Ghost.Agent.speed = fleeSpeed;
            targetMirror = GhostHideout.Farthest<Mirror>(transform.position);
            if (targetMirror != null) Ghost.Agent.SetDestination(targetMirror.ExitPoint);
        }

        public override void ServerTick()
        {
            switch (mode)
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
                    if (victim == null || !victim.IsSpawned || victim.Condition.IsDown) { Lurk(); break; }
                    // Chegou no espelho com a vítima: ela foi para "dentro do reflexo" e fica caída
                    // até alguém ajudar. Ele volta a se esconder.
                    if (Ghost.Arrived())
                    {
                        var caught = victim;
                        Release();
                        Ghost.ServerCatch(caught);
                        Lurk();
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
            SetMode(GhostState.Patrolling);
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
            var mirror = GhostHideout.Nearest<Mirror>(victim.transform.position);
            if (mirror != null) Ghost.Agent.Warp(mirror.ExitPoint);

            Ghost.ServerSetDisguise(copied.Character.CurrentIndex);
            SetMode(GhostState.Disguised);
            Ghost.Agent.speed = approachSpeed;
            timer = giveUpTime;
        }

        void StartPull()
        {
            // Pede ajuda com a voz (a Cida "ouve" a voz no eco, mas sem passos nem respiração junto).
            EchoSystem.Emit(new SoundEvent(transform.position, voiceRange, 0.6f, SoundType.Voice, gameObject));
            // O pedido chega como FALA para quem ouve (o Tiago, surdo, não fica sabendo).
            Cue(Senses.Hearing, $"{CopiedName()}: \"Me ajuda a segurar a outra ponta?\"", voiceRange);

            targetMirror = GhostHideout.Nearest<Mirror>(transform.position);
            if (targetMirror == null) { Ghost.ServerCatch(victim); Lurk(); return; } // cena sem espelho: derruba direto
            SetMode(GhostState.Pulling);
            Ghost.Agent.speed = pullSpeed;
            Ghost.Agent.SetDestination(targetMirror.ExitPoint);
            victim.Condition.ServerSet(PlayerState.Pulled, Ghost.NetworkObject); // a condição cuida do arrasto

            string name = victim.Character.Current.displayName;
            Cue(Senses.Sight, $"{name} está sendo arrastado para o espelho!");
            Cue(Senses.Hearing, $"{name} grita por socorro!");
            Cue(Senses.Vibration, "Algo pesado está sendo arrastado pelo chão.");
        }

        // Um aviso para a equipe (GameCue), saindo do fantasma. O host é o dono dele.
        void Cue(Senses senses, string text, float range = 15f) =>
            GameCues.Emit(new GameCue(transform.position, range, senses, text, gameObject));

        // O nome de quem ele está copiando agora ("Tiago").
        string CopiedName()
        {
            var roster = CharacterRoster.Instance;
            return roster != null && roster.IsValid(Ghost.Disguise) ? roster.Get(Ghost.Disguise).displayName : "alguém";
        }

        // Solta a vítima (se estava puxando alguém).
        void Release()
        {
            if (victim != null && victim.IsSpawned) victim.Condition.ServerClear(PlayerState.Pulled);
            victim = null;
        }

        // Quem está mais longe dos outros (a maior distância média até o resto do grupo).
        static NetworkPlayer MostIsolatedPlayer()
        {
            NetworkPlayer best = null;
            float bestDistance = -1f;
            foreach (var p in NetworkPlayer.All)
            {
                if (p.Condition.IsDown) continue; // caído não é copiado
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
                if (p == except || p.Condition.IsDown) continue; // caído não é atacado
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < bestDistance) { bestDistance = d; best = p; }
            }
            return best;
        }
    }
}
