# Roteiro de testes (TDD)

Plano de testes automatizados do projeto: o que já existe, o que vem a seguir e em que ordem.
A regra de trabalho (feature nova começa por um teste que falha; bug corrigido ganha um teste)
está no [CLAUDE.md](CLAUDE.md), regra 6. Como rodar os testes também está lá ("Rodar os testes").

## Como os testes são feitos aqui

- **Edit Mode** (`Assets/Tests/EditMode`): rápido, sem cena nem rede. É onde fica quase tudo.
- **A regra mora numa classe C# pura** (`LobbyRules`, `MatchRules`...). O MonoBehaviour ou o RPC
  só junta os dados e pergunta a ela. Assim a regra é testada sem host, sem física, sem Play.
- **Teste de dados:** carrega os `.asset` de verdade (Elenco, Campanha, Sons) e confere que estão
  coerentes. Pega erro de quem edita no Inspector, não só de quem mexe no código.
- **Play Mode** (`Assets/Tests/PlayMode`, asmdef `MudancaAssombrada.Tests.PlayMode`): só testes de fumaça. Lento e frágil,
  vale para "a cena abre e o Player nasce", não para regra.
- **Achou diferença entre o código e os .md?** O teste não "escolhe" sozinho: avisar e decidir com o time.

## Onde estamos

| Parte | Classe da regra | Arquivo de teste | Testes | Situação |
|---|---|---|---|---|
| 1. Campanha e save | `Campaign`, `SaveData`, `SaveSystem.Parse` | `CampaignTests`, `SaveDataTests` | 16 | ✅ feito |
| 1. Sala de espera | `LobbyRules` | `LobbyRulesTests` | 27 | ✅ feito |
| 2. Partida e objetivos | `MatchRules`, `HouseClock.OffBeatDelay` | `MatchRulesTests` | 18 | ✅ feito |
| 3. Reforço dos testes já feitos | — | os mesmos | +22 | ✅ feito (falta 1 item, abaixo) |
| 4. Dados do projeto | — | `DataTests` | 10 | ✅ feito |
| 5. Peso e dupla | `CarryRules` | `CarryRulesTests` | 19 | ✅ feito |
| 6. Resgate de colega | `RescueRules` | `RescueRulesTests` | 12 | ✅ feito |
| 7. Imitador: quando age | `ImitatorRules` | `ImitatorRulesTests` | 11 | ✅ feito |
| 8. Testes de fumaça | — | `SmokeTests` (Play Mode) | 3 | ✅ feito |
| 9. Mansão do Espelho | `ImitatorRules` (espelho pelo caminho), `SecretDoorRules`, `PortalRules`, `MirrorReflection.Priority` | `MansionRulesTests` | 18 | ✅ feito |
| 10. Atmosfera da Mansão | `HouseScareRules`, `PianolaRules`, `PeriodFindRules`, `SaveData.Complete` (extra) | `AtmosphereRulesTests` | 16 | ✅ feito |

---

## 3. Reforço dos testes já feitos ✅

Casos que faltaram nas partes 1 e 2. Tudo feito, menos o "trocar de capítulo desmarca os prontos":
o `ClearReady` mexe direto na `NetworkList` dentro do RPC, e separar só ele não vale a pena agora.
O teste do `id` repetido mostra o comportamento atual (dois capítulos dividem o "concluído"); quem
impede isso é o teste de dados da parte 4.

**Campanha e save**
- Capítulo concluído com `id` vazio ou repetido: o que acontece? (Hoje dois capítulos com o mesmo
  `id` dividiriam o mesmo "concluído" no save.)
- `FindByScene` acha o capítulo certo e devolve `null` para cena desconhecida.
- `CompleteChapter(null)` não quebra nem paga.
- Save antigo (JSON sem `storyFlags`, ou com `version` menor) carrega sem erro.
- Save corrompido (texto qualquer) cai no save vazio, sem exceção. Precisa separar a leitura do
  arquivo (`SaveSystem.Parse(string)`) para testar sem disco.

**Sala de espera**
- Liderança volta ao host quando o líder sai (hoje está no `RemovePlayer`, dentro do RPC; separar).
- Trocar de capítulo desmarca todos os "pronto" (`ClearReady`).
- Começar exige capítulo jogável **e** todos prontos (`CanStart`), e a contagem cancela se alguém
  desmarcar no meio.
- Banido não volta: mesmo `PlayerId` recusado; `PlayerId` vazio (login falhou) não bane ninguém.
- Chat: texto com acentos e emoji acima do limite de bytes (o `Fit` do `FixedString512Bytes`).
- Personagem padrão de quem entra: o primeiro livre (já testado), e o que acontece com 4 jogadores.

**Partida**
- Estrelas nas bordas: exatamente 20%, 40%... da noite (o `CeilToInt` arredonda para cima).
- Etapa sem objetivo nenhum no meio (etapas 0 e 2, sem a 1): pula direto, sem travar.
- Texto dos objetivos acima de 150 letras é cortado (`SetObjectiveText`).

## 4. Dados do projeto ✅

Falhou um destes? Quase sempre é um dado para corrigir no Unity, não o teste. Os nomes de som
escolhidos no Inspector (em prefabs e cenas) ainda não são conferidos, só os escritos no código.

Testes que carregam os `.asset` reais e conferem que estão coerentes:
- **Elenco:** 3 personagens, sem ficha nula, nomes preenchidos, força e velocidade > 0, nenhum
  personagem repetido.
- **Campanha:** os 4 capítulos na ordem do roteiro, `id` únicos e não vazios, nenhum capítulo nulo;
  todo capítulo com `sceneName` tem a cena no Build Settings.
- **Capítulo:** `nightDuration` ≥ 0, `payment` ≥ 0; comentários da avaliação apontam para
  personagens do Elenco.
- **Sons:** todo nome de som usado no código (`"relogio_tique"`, `"relogio_taque"`...) existe na
  biblioteca `Sons.asset`, e nenhuma entrada está sem clipe.
- **Móveis:** todo prefab de `Assets/Prefabs/Móveis` tem `Furniture`, `Rigidbody`, `NetworkObject`
  e peso entre 1 e 5.
- **Menu:** depois do `MenuSceneBuilder`, nenhum campo `[SerializeField]` do `MainMenu` ficou nulo.

## 5. Peso e dupla ✅

A `CarryRules` (`Assets/Scripts/Furniture/`) tem as contas; o `Furniture` e o `FurnitureCarrier` só
juntam força, peso, velocidade e ids e perguntam a ela. Ponta vazia ou ponta FALSA entra como 0.
Números do ARQUITETURA.md: cada um levanta sozinho até a **própria força** (Cida 1, Tiago 1,5,
Marcelo 3; cadeira 1, caixa 1,5, caixa G 2, penteadeira 2,5, armário 3,5).
- Levantar sozinho: Cida levanta a cadeira e arrasta a caixa; Tiago levanta a caixa e arrasta a caixa G;
  Marcelo levanta a caixa G e arrasta o armário.
- Dupla: Cida + Tiago = 2,5 levanta a penteadeira (limite "maior ou igual"); o armário só com o Marcelo.
- A ponta falsa do Imitador vale zero: o móvel continua arrastando.
- "Segurar a outra ponta" só aparece se quem segura sente o peso, a ponta está livre e não há ponta falsa.
- Velocidade: passo do mais lento; peso sentido nunca abaixo de 1; arrastando = 0,35 ÷ peso sentido;
  móvel grande (`IsBulky`, peso > 2, só para a escada) anda ÷1,5 na escada.
- **Bug corrigido:** o host (`HelpRpc`) aceitava como ajudante quem já carregava ou ajudava em outro
  móvel; agora `CarryRules.CanAcceptHelper` recebe "mãos ocupadas em outro móvel" e recusa.
- O comentário do topo do `Furniture.cs` agora diz "a própria força", e o `IsBulky` foi reexplicado.
- **Bug corrigido (achado depois):** o `PickUpRpc` tinha o mesmo furo: o host entregava o móvel a quem
  já carregava ou ajudava em outro (só o `TryPickUp` do cliente impedia). Agora `CarryRules.CanAcceptPickUp`
  recebe o "mãos ocupadas" (`BusyElsewhere`) e recusa.

## 6. Resgate de colega ✅

A `RescueRules` (`Assets/Scripts/Player/`) tem as contas; o `PlayerCondition` (no host) e o
`GameManager` (`EveryoneDown`) só juntam os dados e perguntam a ela.
- Levantar um colega caído: 8,4 s segurando. Soltou: o progresso volta a zero em 1,5 s (fica entre 0 e 1).
- Segurar um colega puxado: 1,5 s e o falso larga.
- Só quem começou o resgate pode parar; um segundo jogador não rouba a vez. Vítima de pé: ninguém começa.
- Quem ajuda precisa estar na partida, de pé (livre) e a até 3,5 m (no limite ainda vale).
- A equipe inteira caída = derrota; um de pé = a partida segue; sem jogadores, ninguém perdeu.

## 7. Imitador: quando ele age ✅

A `ImitatorRules` (`Assets/Scripts/Ghosts/`) tem as decisões de tempo; o `GhostImitator` junta o
relógio e os segundos no espelho e pergunta a ela (`ReadyToAct`, `NextLurkTime`, desistir, puxar).
- Antes da aparição programada (45 s de noite), não ataca: espera por ela. Sem relógio ou já
  aparecido: sem aparição. A aparição não espera o mínimo no espelho.
- Depois: sai do espelho quando o tempo de espera acaba **ou** quando alguém arrasta sozinho um
  móvel de dupla por 2,5 s; nunca antes de 5 s dentro do espelho.
- Espera normal 12 s. Fim da noite (a partir de 75%): metade; ofendido (não apontaram na reverência):
  o dobro; as duas juntas se somam.
- Disfarçado há 40 s sem chegar na vítima: desiste e volta para um espelho.
- Puxando a vítima: 6 s para os colegas salvarem.

A máquina de estados inteira (andar, NavMesh, espelhos) fica fora: depende de cena.

## 9. Mansão do Espelho ✅

As regras novas da Fase 1 V1.2 (LEVEL_DESIGN_FASE1_MANSAO.md):
- **Qual espelho (R03):** `ImitatorRules.NearestMirror`/`FarthestMirror` escolhem pelo caminho
  andando (o `MirrorPaths` mede no NavMesh); inalcançável não conta; sem caminho nenhum, vale a
  linha reta.
- **Espelho-porta da Câmara (D5, R01):** `SecretDoorRules`. Fechado só a Cida afina; revelado
  qualquer um empurra; fora de ordem nada muda; sem a Cida na partida, já começa revelado.
- **Portal (R13):** `PortalRules`. Só depois de mexer no cartaz; avisa aos 2 s sozinho (uma vez),
  puxa aos 4 s; um colega a menos de 6 m zera a conta.
- **Espelhos-herói (R04):** `MirrorReflection.Priority` põe os heróis à frente de um comum mais perto.

## 10. Atmosfera da Mansão ✅

As regras da Etapa 3 (LEVEL_DESIGN_FASE1_MANSAO.md §6 a §9):
- **Sustos (R20):** `HouseScareRules`.
  - O primeiro dispara na hora; o próximo espera 20 s; nada no pânico.
  - Cada susto acontece uma vez por noite (a estante, duas).
  - Correr a menos de 1,5 m dispara; andar, não.
  - "Passar perto" sorteia uma vez só.
  - A cadeira de balanço só balança com ninguém olhando (a menos de 60°).
- **Teias (R23):** só jogador rasga.
- **Pianola (R18):** `PianolaRules`.
  - Diferença pequena com a cama não pula.
  - Na volta da faixa (fim → começo) as duas não estão "270 s" separadas.
  - Perto da pianola a cama 2D abaixa; na Câmara, some.
- **Pagamento (D10, D13):** `PeriodFindRules`.
  - Achados somam ao pagamento; cada pancada num frágil desconta; o total nunca fica negativo.
  - Quicar logo depois (menos de 1 s) é a mesma pancada.
  - O save paga o extra junto com o capítulo, só na primeira vez, e o desconto nunca tira do caixa.

## 8. Testes de fumaça (Play Mode) ✅

Em `Assets/Tests/PlayMode/SmokeTests.cs`. Rodam na aba **PlayMode** do Test Runner (ou em batch com
`-testPlatform PlayMode`). São lentos: abrem as cenas de verdade. Qualquer erro no Console reprova.
O host é **local** (`StartHost` direto, sem Lobby nem Relay): não precisa de internet.
- A cena Menu abre sem erro e cria o `NetworkManager`.
- Host local sobe, a Casa do Espelho abre pela rede, o Player nasce e a HUD aparece.
- Na Casa do Espelho o `GameManager` acha o capítulo pela cena (e ele tem relógio).

## Ordem sugerida

1. **Parte 3** (reforço): pequena, aproveita o que já existe.
2. **Parte 4** (dados): barata e pega erro de Inspector, que hoje só aparece no Play.
3. ~~**Parte 5** (peso e dupla)~~ ✅
4. ~~**Partes 6 e 7** (resgate e Imitador)~~ ✅
5. ~~**Parte 8** (fumaça)~~ ✅. Próximo passo: rodar Edit Mode e Play Mode num CI.
