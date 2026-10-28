using UnityEngine;

namespace MudancaAssombrada
{
    // COMPORTAMENTO "imitar" (o Imitador, Seu Reflexo, da Casa do Espelho). Fica junto de um Ghost.
    //
    // Ele MORA no espelho: passa a noite DENTRO de um (invisível, sem corpo) e de tempos em tempos
    // troca de espelho. Só sai quando tem interesse: atacar alguém, ou "ajudar a carregar" quem
    // arrasta sozinho um móvel de dupla. Ninguém vê em qual espelho ele está, menos a Cida: no eco,
    // o espelho ocupado respira um brilho esverdeado (Echo.shader, timbre "vidro rachado").
    //
    //   No espelho --troca de espelho de vez em quando--> No espelho
    //        |  interesse: passa para o espelho mais perto da vítima e espera uns segundos
    //        v  (a deixa da Cida: "o espelho do lado do Tiago mudou")
    //   Disfarçado (copia quem está mais longe do grupo) --perto--> Puxando (6 s para salvar)
    //        |                                                         |
    //   desistiu: volta andando para um espelho          acabou o tempo: Reverência --> No espelho
    //        Fugindo <--desmascarado (apontado, ou seguraram a vítima até o fim)
    //
    // A APARIÇÃO (uma vez por noite, aos 45 s): antes de atacar pela primeira vez, ele sai do
    // espelho à mostra, atravessa o hall devagar (Em cena), faz a reverência e volta para dentro.
    // Não ataca: é a apresentação dele, e ensina a pista da Cida (uma figura SEM passos no eco).
    //
    // A ISCA: se a vítima arrasta sozinha um móvel de dupla, ele "pega a outra ponta" antes de puxar
    // (a ponta falsa do Furniture). PUXANDO, a equipe tem 6 s: apontar para ele, ou segurar a vítima
    // (PlayerCondition): segurada, ele para; segurada até o fim, ele larga e foge.
    //
    // Ele NÃO faz passos nem respira (não tem Breathing): no eco da Cida, a cópia é a única
    // pessoa "muda". Qualquer jogador pode APONTAR para ele (tecla Interagir) e desmascará-lo,
    // e também durante a reverência: apontado ali, ele fica ofendido e demora o dobro para voltar.
    public class GhostImitator : GhostBehaviour
    {
        [Header("Tempos")]
        [SerializeField] float lurkTime = 12f;       // dentro do espelho antes de atacar
        [SerializeField] float giveUpTime = 40f;     // disfarçado sem conseguir chegar: volta para um espelho
        [Tooltip("A partir desta parte da noite a espera no espelho cai pela metade (0.75 = quando a luz vira âmbar).")]
        [SerializeField, Range(0f, 1f)] float hurryFrom = 0.75f;

        [Header("Dentro do espelho")]
        [Tooltip("De quanto em quanto tempo ele troca de espelho, sem sair (mínimo, máximo).")]
        [SerializeField] Vector2 hopEvery = new(18f, 32f);
        [Tooltip("Antes de sair, ele passa para o espelho mais perto da vítima e espera tantos segundos " +
                 "(a deixa da Cida: o brilho mudou de espelho, perto de alguém).")]
        [SerializeField] float emergeDelay = 3f;
        [Tooltip("Alguém arrastando SOZINHO um móvel de dupla por tantos segundos: ele se interessa e sai para \"ajudar\".")]
        [SerializeField] float dragInterest = 2.5f;
        [Tooltip("Acabou de entrar num espelho: fica pelo menos isto antes de sair de novo (nem para ajudar).")]
        [SerializeField] float minInside = 5f;

        [Header("A aparição e a reverência")]
        [Tooltip("Segundos de noite até a aparição (sai à mostra, faz a reverência e volta, sem atacar). " +
                 "Antes disso ele não ataca. 0 = sem aparição.")]
        [SerializeField] float appearAt = 45f;
        [Tooltip("Para onde ele anda na aparição (o meio do hall). Vazio = 4 m à frente, na direção de quem está mais perto.")]
        [SerializeField] Transform stage;
        [Tooltip("Depois da reverência da aparição, ele volta por ESTE espelho (na Mansão: sai do E1, volta pelo E2). " +
                 "Vazio = o mesmo de onde saiu.")]
        [SerializeField] Mirror appearanceExit;
        [SerializeField] float performSpeed = 1.2f;  // devagar, teatral
        [SerializeField] float bowTime = 2f;         // a janela para apontar
        [Tooltip("Apontado na reverência, fica ofendido: a próxima espera no espelho é tantas vezes maior.")]
        [SerializeField] float offendedFactor = 2f;

        [Header("Velocidades")]
        [SerializeField] float approachSpeed = 3.5f; // anda como um jogador, para não chamar atenção
        [SerializeField] float pullSpeed = 2.5f;     // arrastando a vítima (mais que andar devagar: pouco tempo para salvar)
        [SerializeField] float fleeSpeed = 5f;

        [Header("Ataque")]
        [SerializeField] float askDistance = 2f;     // perto assim, pede ajuda e começa a puxar
        [SerializeField] float voiceRange = 6f;      // "segura a outra ponta do armário?"
        [Tooltip("Puxando: segundos até levar a vítima para dentro do espelho (o tempo para salvar). " +
                 "Chegou antes no espelho? Espera ali. Um colega segurando a vítima pausa a contagem.")]
        [SerializeField] float pullTime = 6f;
        [Tooltip("A ISCA: a vítima arrasta sozinha um móvel de dupla? Ele \"pega a outra ponta\" por tantos " +
                 "segundos antes de puxar. Ele não vale nada na carga: o móvel continua arrastando.")]
        [SerializeField] float baitTime = 4f;

        GhostState mode = GhostState.InMirror; // o MEU estado (no Ghost só quando estou no volante)
        float timer;
        NetworkPlayer victim;
        int copied = -1;      // o personagem que ele vai vestir ao sair (posição no Elenco)
        Mirror home;          // o espelho em que ele está (ou de onde saiu)
        Mirror targetMirror;  // para onde ele está indo (voltando, fugindo ou puxando a vítima)
        Furniture baited;     // o móvel em que ele finge segurar a outra ponta (null = nenhum)
        float pulled;         // segundos puxando (sem contar o tempo em que seguraram a vítima)
        float hopTimer;       // até a próxima troca de espelho
        float inside;         // segundos desde que entrou no espelho
        float dragSeen;       // segundos vendo alguém arrastar sozinho um móvel de dupla
        bool appeared;        // a aparição desta noite já aconteceu
        bool goingBack;       // em cena: voltando para o espelho (depois da reverência)
        bool bowThenBack;     // a reverência é a da aparição (depois volta andando) ou a da captura (some ali)
        bool offended;        // apontado na reverência: a próxima espera é maior
        Vector3? pullPoint;   // saiu do PORTAL (sem disfarce): puxa de volta para ele, não para um espelho

        public override void ServerStart()
        {
            appeared = false;
            offended = false;
            EnterMirror(MirrorPaths.Nearest(transform.position));
        }

        // No espelho, quase não quer o corpo; fora dele (disfarçado, puxando, em cena), não larga por nada.
        public override int ServerPriority() => mode switch
        {
            GhostState.Disguised or GhostState.Pulling or GhostState.Performing or GhostState.Bowing => 20,
            GhostState.Fleeing => 15,
            GhostState.Emerging => 20,
            _ => ReadyToAct ? 20 : 0, // no espelho: quando chega a hora, pede o volante
        };

        // --- O relógio da noite (GameManager): a aparição e a pressa do fim ---

        static float NightElapsed(out bool hasClock)
        {
            var game = GameManager.Instance;
            hasClock = game != null && game.IsSpawned && game.HasClock && game.Outcome == MatchOutcome.Playing;
            return hasClock ? game.NightDuration - game.TimeLeft : 0f;
        }

        // A aparição ainda vai acontecer nesta noite? (Sem relógio, numa cena de teste: não.)
        bool AppearancePending
        {
            get
            {
                NightElapsed(out bool hasClock);
                return ImitatorRules.AppearancePending(appeared, appearAt, hasClock);
            }
        }

        bool AppearanceDue => ImitatorRules.AppearanceDue(AppearancePending, NightElapsed(out _), appearAt);

        // No espelho: é hora de agir? (Antes da aparição ele não ataca: espera por ela.)
        // Interesse: o tempo de espera acabou, ou alguém arrasta sozinho um móvel de dupla.
        bool ReadyToAct => ImitatorRules.ReadyToAct(AppearanceDue, AppearancePending, inside, minInside, timer, dragSeen, dragInterest);

        // Quanto tempo fica no espelho desta vez: metade no fim da noite, mais se estiver ofendido.
        float NextLurkTime()
        {
            var game = GameManager.Instance;
            float progress = game != null && game.IsSpawned ? game.NightProgress : 0f;
            float t = ImitatorRules.LurkTime(lurkTime, progress, hurryFrom, offended, offendedFactor);
            offended = false;
            return t;
        }

        // Sem o volante, só conta o tempo no espelho.
        public override void ServerIdleTick()
        {
            if (mode is GhostState.InMirror or GhostState.Emerging) CountInside();
        }

        // Ganhou o volante: sem passos e sem pegar ninguém encostando (ele puxa para o espelho).
        public override void ServerGainControl()
        {
            Ghost.Footsteps = false;
            Ghost.CatchesOnTouch = false;
            // Outro comportamento andou com o corpo (no Inquilino): volta para dentro do espelho mais perto.
            if (mode is GhostState.InMirror or GhostState.Emerging) EnterMirror(MirrorPaths.Nearest(transform.position));
            else Ghost.ServerSetState(mode);
        }

        // Perdeu o volante (outro comportamento do mesmo fantasma, no Inquilino): tira o disfarce.
        public override void ServerLoseControl()
        {
            if (mode == GhostState.Emerging) mode = GhostState.InMirror; // desiste de sair
            if (mode == GhostState.InMirror) return;
            Release();
            Ghost.ServerSetDisguise(-1);
            mode = GhostState.InMirror;
            timer = NextLurkTime();
        }

        // Muda o MEU estado; o Ghost (que todos veem) só muda se eu estiver no volante.
        void SetMode(GhostState newMode)
        {
            mode = newMode;
            if (HasControl) Ghost.ServerSetState(newMode);
            if (Ghost.Agent.isOnNavMesh) Ghost.Agent.isStopped = false; // a âncora para o corpo; o resto anda
        }

        public override void ServerReset()
        {
            Release();
            ServerStart();
        }

        public override void ServerUnmasked()
        {
            if (Ghost.IsDisguised)
            {
                // "É falso!": larga a vítima, volta ao próprio corpo e foge para OUTRO espelho.
                Cue(Senses.Sight, $"O \"{CopiedName()}\" era falso! Fugiu para um espelho.");
                Cue(Senses.Hearing, "Uma voz rouca resmunga, ofendida... o falso fugiu!");
            }
            else
            {
                // Apontado na reverência: a máscara vira tragédia e ele sai OFENDIDO (demora a voltar).
                offended = true;
                Cue(Senses.Sight, "A máscara virou tragédia: ofendido, ele sumiu num espelho.");
                Cue(Senses.Hearing, "Uma voz rouca, furiosa: \"Plateia ingrata...\" Ele foi embora ofendido.");
            }
            Flee();
        }

        // Larga tudo, volta ao próprio corpo e foge para o espelho MAIS LONGE.
        void Flee()
        {
            Release();
            Ghost.ServerSetDisguise(-1);
            SetMode(GhostState.Fleeing);
            Ghost.Agent.speed = fleeSpeed;
            targetMirror = MirrorPaths.Farthest(transform.position);
            if (targetMirror != null) Ghost.Agent.SetDestination(targetMirror.ExitPoint);
        }

        public override void ServerTick()
        {
            switch (mode)
            {
                case GhostState.InMirror:
                    CountInside();
                    if (AppearanceDue) StartAppearance();
                    else if (ReadyToAct) PrepareAttack();
                    else if (hopTimer <= 0) Hop(OtherMirror());
                    break;

                case GhostState.Emerging: // no espelho perto da vítima, esperando a deixa
                    CountInside();
                    if (timer <= 0) Emerge();
                    break;

                case GhostState.Performing: // em cena: indo para o palco, ou voltando para o espelho
                    timer -= Time.deltaTime;
                    if (Ghost.Arrived() || timer <= 0)
                    {
                        if (goingBack) EnterMirror(targetMirror);
                        else Bow(thenBack: true);
                    }
                    break;

                case GhostState.Bowing:
                    timer -= Time.deltaTime;
                    if (timer > 0) break;
                    if (bowThenBack) GoBack();
                    else EnterMirror(MirrorPaths.Nearest(transform.position)); // some ali e reaparece no reflexo
                    break;

                case GhostState.Disguised:
                    timer -= Time.deltaTime;
                    if (victim == null)
                    {
                        // Desistiu: volta ANDANDO para um espelho, ainda disfarçado (sumir na frente de todos o entregaria).
                        if (Ghost.Arrived() || timer <= -15f) EnterMirror(targetMirror);
                        break;
                    }
                    if (!victim.IsSpawned || victim.Condition.IsDown || ImitatorRules.GiveUp(timer)) { GiveUp(); break; }
                    if (baited != null) { TickBait(); break; }
                    Ghost.Agent.SetDestination(victim.transform.position);
                    if (Vector3.Distance(transform.position, victim.transform.position) < askDistance)
                    {
                        // Arrastando sozinho um móvel de dupla? "Deixa que eu pego a outra ponta!"
                        var load = Furniture.HeldBy(victim.OwnerClientId);
                        if (load != null && IsSoloDrag(load)) StartBait(load);
                        else StartPull(ask: Ghost.IsDisguised); // sem disfarce (o portal) ele não pede nada
                    }
                    break;

                case GhostState.Pulling:
                    if (victim == null || !victim.IsSpawned || victim.Condition.IsDown) { GiveUp(); break; }
                    // Um colega segurou a vítima até o fim: ela está livre e ele larga, desmascarado.
                    if (!victim.Condition.IsPulled) { Saved(); break; }

                    // Segurada (âncora): ele para e o tempo para. Solta: volta a puxar e a contar.
                    bool anchored = victim.Condition.IsAnchored;
                    Ghost.Agent.isStopped = anchored;
                    if (!anchored) pulled += Time.deltaTime;
                    victim.Condition.ServerSetPullLeft(pullTime - pulled);

                    // Acabou o tempo: a vítima foi para "dentro do reflexo" e fica caída até alguém
                    // ajudar. Ele tira o disfarce, agradece a plateia e volta para o espelho. (Chegou
                    // antes no espelho? Fica ali segurando até o tempo acabar: a equipe sempre tem os 6 s.)
                    if (ImitatorRules.PullDone(pulled, pullTime))
                    {
                        var caught = victim;
                        Release();
                        Ghost.ServerCatch(caught);
                        Ghost.ServerSetDisguise(-1);
                        Bow(thenBack: false);
                    }
                    break;

                case GhostState.Fleeing:
                    // Chegou no outro espelho: entra e espera para tentar de novo.
                    if (Ghost.Arrived()) EnterMirror(targetMirror);
                    break;
            }
        }

        // ---------- Dentro do espelho ----------

        // Entra no espelho: some (GhostVisual), fica parado na saída dele e recomeça a espera.
        void EnterMirror(Mirror mirror)
        {
            Release();
            Ghost.ServerSetDisguise(-1);
            copied = -1;
            pullPoint = null;
            SetMode(GhostState.InMirror);
            if (Ghost.Agent.isOnNavMesh) Ghost.Agent.ResetPath();
            home = mirror;
            if (mirror != null) Ghost.ServerTeleport(mirror.ExitPoint);
            timer = NextLurkTime();
            inside = 0f;
            dragSeen = 0f;
            hopTimer = Random.Range(hopEvery.x, hopEvery.y);
        }

        // Troca de espelho sem sair: ninguém vê, mas no eco da Cida o brilho muda de lugar.
        void Hop(Mirror mirror)
        {
            hopTimer = Random.Range(hopEvery.x, hopEvery.y);
            if (mirror == null || mirror == home) return;
            home = mirror;
            Ghost.ServerTeleport(mirror.ExitPoint);
        }

        Mirror OtherMirror()
        {
            Mirror pick = null;
            int seen = 0;
            foreach (var h in GhostHideout.All)
            {
                if (h is not Mirror m || m == home) continue;
                seen++;
                if (Random.Range(0, seen) == 0) pick = m; // sorteio justo sem montar lista
            }
            return pick;
        }

        void CountInside()
        {
            float dt = Time.deltaTime;
            timer -= dt;
            hopTimer -= dt;
            inside += dt;
            dragSeen = SoloDragger() != null ? dragSeen + dt : 0f;
        }

        // O PORTAL da Câmara Secreta (PortalTrap, R13): alguém ficou sozinho lá dentro. Ele sai pelo
        // portal SEM disfarce (a forma de verdade) e puxa a vítima para dentro dele. Só se está quieto
        // num espelho e a aparição já aconteceu. Devolve se aceitou.
        public bool ServerAmbushFromPortal(NetworkPlayer target, Vector3 portalExit)
        {
            if (target == null || target.Condition.IsDown) return false;
            if (mode is not (GhostState.InMirror or GhostState.Emerging) || AppearancePending) return false;
            victim = target;
            copied = -1;
            pullPoint = portalExit;
            Ghost.ServerTeleport(portalExit);
            Ghost.ServerSetDisguise(-1);
            SetMode(GhostState.Disguised);
            Ghost.Agent.speed = approachSpeed;
            timer = giveUpTime;
            return true;
        }

        // Escolhe a vítima e quem vai copiar, passa para o espelho mais perto dela e espera para sair.
        void PrepareAttack()
        {
            // Quem arrasta sozinho um móvel de dupla atrai ele primeiro ("deixa que eu ajudo!").
            var dragger = dragSeen >= dragInterest ? SoloDragger() : null;
            NetworkPlayer model;
            if (dragger != null)
            {
                victim = dragger;
                model = MostIsolatedPlayer(except: NetworkPlayer.All.Count > 1 ? dragger : null);
            }
            else
            {
                model = MostIsolatedPlayer(except: null);
                // A vítima: o jogador mais perto, que NÃO seja quem ele copiou
                // (um "Tiago" chegando no próprio Tiago não engana ninguém). Sozinho, é o único que há.
                victim = model != null ? NearestPlayer(except: NetworkPlayer.All.Count > 1 ? model : null) : null;
            }
            if (model == null || victim == null) { victim = null; timer = 1f; return; } // ninguém conectado ainda

            copied = model.Character.CurrentIndex;
            Hop(MirrorPaths.Nearest(victim.transform.position));
            SetMode(GhostState.Emerging); // todos os computadores sabem: a fala "minha deixa..."
            timer = emergeDelay;
        }

        // Sai do espelho já disfarçado e vai até a vítima.
        void Emerge()
        {
            if (victim == null || !victim.IsSpawned || victim.Condition.IsDown || copied < 0)
            {
                victim = null;
                SetMode(GhostState.InMirror);
                timer = 1f; // a vítima sumiu enquanto ele esperava: escolhe de novo
                return;
            }
            Ghost.ServerSetDisguise(copied);
            SetMode(GhostState.Disguised);
            Ghost.Agent.speed = approachSpeed;
            timer = giveUpTime;
        }

        // Não conseguiu (a vítima caiu, sumiu, ou demorou demais): volta disfarçado para o espelho mais perto.
        void GiveUp()
        {
            Release();
            SetMode(GhostState.Disguised);
            targetMirror = MirrorPaths.Nearest(transform.position);
            if (targetMirror == null) { EnterMirror(null); return; }
            Ghost.Agent.speed = approachSpeed;
            Ghost.Agent.SetDestination(targetMirror.ExitPoint);
            timer = 0f; // conta para baixo: passou de -15 s sem chegar, entra assim mesmo
        }

        // Alguém arrastando SOZINHO um móvel de dupla (sem colega e sem a ponta falsa)?
        static NetworkPlayer SoloDragger()
        {
            foreach (var p in NetworkPlayer.All)
            {
                if (p.Condition.IsDown) continue;
                var load = Furniture.HeldBy(p.OwnerClientId);
                if (load != null && IsSoloDrag(load)) return p;
            }
            return null;
        }

        static bool IsSoloDrag(Furniture load) =>
            !load.CanLift && load.HelperClientId == Furniture.NoHelper && !load.HasFakeHelper;

        // ---------- A aparição ----------

        // A aparição: sai do espelho À MOSTRA e atravessa o hall devagar até o palco.
        void StartAppearance()
        {
            appeared = true;
            goingBack = false;
            // Para onde volta depois: o espelho da saída marcado (o E2), ou o mesmo de onde saiu.
            targetMirror = appearanceExit != null ? appearanceExit : home != null ? home : MirrorPaths.Nearest(transform.position);
            Vector3 spot = stage != null ? stage.position : FrontSpot();
            SetMode(GhostState.Performing);
            Ghost.Agent.speed = performSpeed;
            Ghost.Agent.SetDestination(spot);
            timer = 15f; // não chegou (caminho bloqueado): faz a reverência onde estiver
        }

        // Sem palco marcado: 4 m à frente, na direção de quem está mais perto.
        Vector3 FrontSpot()
        {
            var p = Ghost.NearestPlayer();
            Vector3 dir = p != null ? p.transform.position - transform.position : transform.forward;
            dir.y = 0;
            return transform.position + (dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward) * 4f;
        }

        // A reverência: para, vira para quem está mais perto e se curva. Dá para apontar.
        void Bow(bool thenBack)
        {
            bowThenBack = thenBack;
            SetMode(GhostState.Bowing);
            Ghost.Agent.ResetPath();
            timer = bowTime;
            var p = Ghost.NearestPlayer();
            if (p == null) return;
            Vector3 look = p.transform.position - transform.position;
            look.y = 0;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
        }

        // Fim da aparição: volta andando para o espelho de onde saiu.
        void GoBack()
        {
            goingBack = true;
            SetMode(GhostState.Performing);
            Ghost.Agent.speed = performSpeed;
            if (targetMirror != null) Ghost.Agent.SetDestination(targetMirror.ExitPoint);
            timer = 15f;
        }

        // ---------- O ataque ----------

        // A ISCA (MEC-08): a vítima arrasta sozinha um móvel de dupla e o "colega" pega a outra ponta.
        // É mentira, e cada um percebe de um jeito: o peso não alivia (continua arrastando, devagar),
        // o móvel não sai do chão, e não tem o "toc" do apoio que um colega de verdade faz.
        void StartBait(Furniture load)
        {
            baited = load;
            baited.ServerSetFakeHelper(Ghost.Disguise);
            pulled = 0f; // aqui conta o tempo da isca
            EchoSystem.Emit(new SoundEvent(transform.position, voiceRange, 0.6f, SoundType.Voice, gameObject));
            Cue(Senses.Hearing, $"{CopiedName()}: \"Deixa que eu pego a outra ponta!\"", voiceRange);
        }

        void TickBait()
        {
            // Fica do outro lado do móvel, como quem segura a outra ponta.
            Vector3 away = baited.transform.position - victim.transform.position;
            away.y = 0;
            Ghost.Agent.SetDestination(baited.transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 0.9f);
            pulled += Time.deltaTime;
            // A vítima largou o móvel (desconfiou?) ou deu o tempo: agora puxa.
            if (!baited.IsHeld || pulled >= baitTime) StartPull(ask: false);
        }

        void StartPull(bool ask)
        {
            if (ask)
            {
                // Pede ajuda com a voz (a Cida "ouve" a voz no eco, mas sem passos nem respiração junto).
                EchoSystem.Emit(new SoundEvent(transform.position, voiceRange, 0.6f, SoundType.Voice, gameObject));
                // O pedido chega como FALA para quem ouve (o Tiago, surdo, não fica sabendo).
                Cue(Senses.Hearing, $"{CopiedName()}: \"Me ajuda a segurar a outra ponta?\"", voiceRange);
            }
            ClearBait();

            // Saiu do portal: leva a vítima para o portal. Senão, para o espelho mais perto ANDANDO.
            targetMirror = pullPoint.HasValue ? null : MirrorPaths.Nearest(transform.position);
            if (!pullPoint.HasValue && targetMirror == null) { Ghost.ServerCatch(victim); EnterMirror(null); return; } // cena sem espelho: derruba direto
            SetMode(GhostState.Pulling);
            pulled = 0f;
            Ghost.Agent.speed = pullSpeed;
            Ghost.Agent.SetDestination(pullPoint ?? targetMirror.ExitPoint);
            victim.Condition.ServerSet(PlayerState.Pulled, Ghost.NetworkObject); // a condição cuida do arrasto
            victim.Condition.ServerSetPullLeft(pullTime);

            string name = victim.Character.Current.displayName;
            string seconds = $"{Mathf.CeilToInt(pullTime)} s";
            Cue(Senses.Sight, $"{name} está sendo arrastado para o espelho! Aponte para o falso ou segure {name} ({seconds}).");
            Cue(Senses.Hearing, $"{name} grita por socorro! Segure {name} ({seconds}).");
            Cue(Senses.Vibration, "Algo pesado está sendo arrastado pelo chão.");
        }

        // Seguraram a vítima até o fim: ele larga, perde o disfarce e foge para longe.
        void Saved()
        {
            Cue(Senses.Sight, $"O \"{CopiedName()}\" largou e fugiu para um espelho!");
            Cue(Senses.Hearing, "Uma voz rouca resmunga... o falso largou e fugiu!");
            victim = null; // ela já está livre: nada a soltar
            Flee();
        }

        void ClearBait()
        {
            if (baited != null && baited.IsSpawned) baited.ServerSetFakeHelper(-1);
            baited = null;
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

        // Solta a vítima (se estava puxando alguém) e a ponta falsa (se estava na isca).
        void Release()
        {
            ClearBait();
            if (victim != null && victim.IsSpawned) victim.Condition.ServerClear(PlayerState.Pulled);
            victim = null;
        }

        // Quem está mais longe dos outros (a maior distância média até o resto do grupo).
        static NetworkPlayer MostIsolatedPlayer(NetworkPlayer except)
        {
            NetworkPlayer best = null;
            float bestDistance = -1f;
            foreach (var p in NetworkPlayer.All)
            {
                if (p == except || p.Condition.IsDown) continue; // caído não é copiado
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
