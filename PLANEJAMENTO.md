# Planejamento — do Art Bible ao jogo

Como os designs do Art Bible viram jogo, em fases. Cada fase diz **o que entra**,
**o que o código precisa ganhar** e **quando está pronta**.

- Os designs: [ART_BIBLE.md](ART_BIBLE.md) (resumo) e o Art Bible completo (link lá).
- Onde mexer no código: [ARQUITETURA.md](ARQUITETURA.md).
- Padrão visual da interface: [DESIGN.md](DESIGN.md).

> Este é um plano **vivo**: ao terminar uma fase, marque aqui e atualize a tabela
> "Situação" do ART_BIBLE.md. Mudou de ideia? Mude o plano antes do código.

---

## Visão geral

| Fase | Entrega | Tipo de trabalho | Situação |
|---|---|---|---|
| 1 | Bibliotecário, móveis e caminhão com modelo | arte + builder | **feita** |
| 2 | Fundação de código para os fantasmas e personagens | código | **feita** (falta testar no Unity) |
| 3 | Capítulo 1 — Casa do Espelho + Imitador | cena + arte + mecânica | **em revisão**: [LEVEL_DESIGN_FASE1.md](LEVEL_DESIGN_FASE1.md), lotes 1 a 4 de 5 feitos (casa de dois andares, lanterna, as mecânicas da Cida, o Imitador completo e carregar em dupla no jogo todo: cada um levanta sozinho até a própria força (Cida 1, Tiago 1,5, Marcelo 3; armário 3,5), arrastar sozinho, a isca da outra ponta, 6 s para soltar, escada); lote 4 não testado com Play. **Nova planta** (Mansão do Espelho V1.2, 14 ambientes em dois níveis, decisões D1–D14 aprovadas em 27/10/2026): [LEVEL_DESIGN_FASE1_MANSAO.md](LEVEL_DESIGN_FASE1_MANSAO.md), **as 3 etapas implementadas em 27/10/2026** (a casa; os espelhos e o Imitador; atmosfera e época). Medida em batch, **não testada com Play** |
| 4 | Capítulo 2 — Conservatório + Maestrina | cena + arte + mecânica | a fazer |
| 5 | Capítulo 3 — Biblioteca do Silêncio | cena + arte + mecânica | depende de decisão |
| 6 | Seu Valdemar, relíquias e o final (Inquilino + Sobrado) | cena + arte + mecânica | a fazer |
| — | Animações e comunicação da equipe | arte + código | em paralelo |
| — | Sons: catálogo + geração (ElevenLabs) + `GameAudio` tocando os `SoundEvent` | ferramenta + código | **sistema pronto** (sons da IA a gerar; não testado com Play) |
| — | Hotbar de itens (3 espaços, teclas 1-2-3, rodinha/direcional): a lanterna mora nela; móvel nas mãos manda a lanterna para o colete (mais fraca, aponta para a frente do corpo). Troca de personagem de teste foi para F1-F3 (`HotbarRules`/`Hotbar`) | código + interface | **feita** em 28/10/2026 (não testada com Play). Ideias para depois: pilha guardada como item, bengala da Cida num espaço, Marcelo com 2 espaços, lanterna visível na mão (`SOC_Hand_R`) |
| — | Opções em abas (Geral, Tela, Gráficos): resolução, brilho, VSync, limite de FPS, qualidade, antialiasing, resolução 3D com STP (`VideoRules`/`VideoSettings`) | código + interface | **feita** em 27/10/2026 (não testada com Play) |

Ordem pensada para que cada fase use o que a anterior deixou pronto: a Fase 2 prepara
o terreno para os três fantasmas, e o Inquilino (Fase 6) junta os três.

## O que já existe e serve aos designs

Antes de escrever código novo, vale lembrar o que já está pronto:

| Já existe | Atende |
|---|---|
| Tipos de som (`SoundType`: passo, respiração, voz, impacto, bengala, apito, música) | Imitador sem passos, Maestrina pela música, Bibliotecário pela voz |
| Ficha do personagem com modelo (`CharacterDefinition.model`) | personagens jogáveis e o disfarce do Imitador |
| `Interactable` com `onlyFor` | instrumentos que só o Tiago desliga |
| Objetivos por etapa (`Objective.Stage`) | as 3 etapas do chefe final |
| Marcadores no save (`SaveSystem.SetFlag`) | pistas do Valdemar e final alternativo |
| `NightLighting` | o relógio da noite (a luz conta o tempo) |
| Fichas de capítulo (bilhete e avaliação do Valdemar) | o texto do chefe em cada fase |
| Pipeline Blender → Unity (`tools/blender`, `ArtImportSettings`, troca de bloco por modelo) | todos os modelos novos |

---

## Fase 1 — Arte nos sistemas que já funcionam ✅

**Entrega:** a fase Protótipo com cara de jogo, sem mudar mecânica.

- Modelos gerados por script: Bibliotecário (2,6 m, Humanoid), armário, cadeira, caixa e caminhão.
- Importação configurada pela pasta (`Assets/Editor/ArtImportSettings.cs`).
- `LevelPrefabsBuilder` troca o bloco pelo modelo quando a arte existe, sem perder ajustes.

## Fase 2 — Fundação de código ✅

**Entrega:** nenhuma mudança visível para o jogador; tudo que as Fases 3 a 6 vão precisar.
Decisão 4 tomada: **base comum** (`Ghost`) + um comportamento por fantasma (`GhostBehaviour`).

Como ficou: `Ghost` (registro, reinício, captura, passos, estado `GhostState` numa NetworkVariable),
`GhostHunter` virou um `GhostBehaviour`, `GhostVisual` lê o estado e inclina o modelo,
`Breathing` emite `SoundType.Breath` (só o dono do objeto), e a ficha ganhou `height`/`radius`,
aplicados pelo `PlayerMovement.SetBodySize` com os pés fixos. Prefab antigo do fantasma ganha as
peças novas em **Criar Prefabs que Faltam** (sem perder os números).

1. **Fantasmas por composição** (como já é feito com os personagens).
   - Uma base comum: registro (`All`), reinício, captura, passos e rede.
   - Um comportamento por fantasma: caçar por som (o `GhostHunter` de hoje), imitar, reger.
   - O Inquilino usa os três comportamentos juntos.
2. **Estado do fantasma na rede.** Hoje ele pensa só no host e os clientes recebem só a
   posição. Com o estado numa `NetworkVariable`, todos veem a mesma coisa (orelhas abrindo,
   máscara virando, pescoço esticando).
3. **Visual separado da lógica** ("lógica não desenha"): um componente só lê o estado do
   fantasma e anima o modelo.
4. **Respiração:** um emissor de `SoundType.Breath` nos jogadores e nos fantasmas que respiram.
   Sem isso, a pista da Cida contra o Imitador ("não respira") não existe.
5. **Colisor por personagem:** altura e raio na ficha, aplicados no `CharacterController`
   (hoje é 2 m para todos e a Cida, com 1,52 m, não passa onde caberia).

**Pronta quando:** o Bibliotecário funciona igual a hoje, mas por cima da base nova; os clientes
veem o estado dele; o eco mostra respiração; a Cida passa por baixo de uma mesa de 1,6 m.

## Fase 3b — Fundação das mecânicas (em andamento)

**Entrega:** nada novo para o jogador além de legendas; peças para as próximas mecânicas
entrarem sem mexer no jogador.

1. ✅ **Interação por componentes:** `PlayerInteractor` mira; cada alvo (`IInteractionTarget`)
   diz o que fazer. `FurnitureCarrier` ficou só com peso e física.
2. ✅ **Estado do jogador na rede:** `PlayerCondition` (normal, puxado, hipnotizado, caído), uma porta
   de entrada só (`ServerSet`). **Decidido:** ser pego = caído até um colega "Ajudar" (E); a equipe só
   perde quando todos caem. O Imitador puxa e derruba por aqui; a hipnose (Fase 4) já tem o estado.
   Falta: pose de caído no boneco (animação).
3. ✅ **Acontecimentos pelos sentidos:** `GameCue` + `CueListener` + legendas na HUD.
   O Imitador já avisa (pedido de ajuda, vítima sendo puxada, desmascarado) e apontar avisa a equipe.
4. ✅ **Sentidos do fantasma separados + árbitro:** `GhostHearing` só ouve e avisa (`Heard`); o `Ghost`
   entrega o "volante" ao comportamento com maior `ServerPriority` (os outros recebem `ServerIdleTick`
   e continuam percebendo). `GhostHunter` e `GhostImitator` guardam o próprio estado e só aplicam no
   corpo com o volante. O Inquilino = os três comportamentos no mesmo fantasma.
5. ✅ **Kit de autoria do mapa:** prefab único **Sistemas da Fase** (GameManager + SoundRelay na raiz,
   eco/noite/nascimento/HUD/pausa como filhos ligados), marcadores `SpawnPoint`, `GhostHideout`
   (o `Mirror` virou um tipo) e `Room`. Menus **Nova Fase (cena vazia)** e **Usar Sistemas da Fase
   na cena aberta**. O rascunho da Casa do Espelho agora é montado por cima da fase vazia.
6. ⬜ **Ferramentas de teste** no editor (estado do fantasma, atalhos de depuração).
7. ⬜ **Arrastar gasta estamina.** **Decidido:** quem pega um móvel mais pesado que a própria força
   só arrasta (já feito: bem devagar, mais devagar quanto mais pesado) e, depois, arrastar vai
   cansar (a estamina do `PlayerMovement`).

**Pronta quando:** pegar móvel, usar objeto e apontar funcionam como antes; o Tiago não recebe as
falas do Imitador, a Cida não recebe o que só se vê, e os dois veem as legendas certas em rede.

## Fase 3 — Capítulo 1: A Casa do Espelho + o Imitador ✅

**Entrega:** o primeiro capítulo da história jogável.

Como ficou: menu **Criar Cena Casa do Espelho** (`MirrorHouseBuilder`) cria os prefabs do kit,
espelhos (`Mirror`), penteadeira, cartaz e o Imitador, e um rascunho da cena `CasaDoEspelho`
(já ligada na ficha do capítulo). `GhostImitator`: escondido → disfarçado (copia o jogador mais
isolado, modelo espelhado, sem passos nem respiração) → pede ajuda (voz) e puxa a vítima até o
espelho → desmascarado (qualquer jogador **aponta** com Interagir) foge para o espelho mais longe.
O cartaz grava `achou-cartaz-teatro` no save (`StoryFlag`); é pista opcional, não objetivo.
Máscara comédia → tragédia fica para as animações.

- **Arte:** kit modular (parede 2 × 2,8 m, porta 0,9 × 2,1 m, molduras de espelho 0,6 / 1,2 / 2,0 m),
  piso xadrez, armário de figurino, penteadeira de camarim, máscara e luvas do Imitador.
- **Cena:** receita "Uma fase" do ARQUITETURA.md; preencher `sceneName` na ficha do capítulo.
- **Comportamento "imitar":**
  - escolhe o jogador mais afastado do grupo e copia o modelo da ficha dele;
  - a cópia tem o logo do colete espelhado e não emite passos nem respiração;
  - pede ajuda ("segura a outra ponta") e puxa a vítima para o espelho;
  - desmascarado, foge para outro espelho.
- **Relíquia:** cartaz do teatro (ver Fase 6).

**Pronta quando:** dá para jogar do bilhete de abertura até a avaliação, e a Cida consegue
apontar qual cópia é falsa só pelo eco.

## Fase 4 — Capítulo 2: O Conservatório + a Maestrina

**Entrega:** o segundo capítulo jogável.

- **Arte:** auditório, piano de cauda, violino, caixinha de música, a Maestrina
  (pescoço que estica, fitas de partitura).
- **`MusicSource`:** instrumento que emite `SoundType.Music` enquanto está ligado e desliga
  por um `Interactable` com `onlyFor` = Tiago.
- **Hipnose:** quem ouve a música fica com a tela embaçada e lenta; quem tem `deaf` na ficha
  é imune. Jogador hipnotizado fica "travado" até alguém arrastá-lo.
- **Relíquia:** partitura da Maestrina.

**Pronta quando:** o Tiago consegue desligar os três instrumentos enquanto os outros se escondem,
e a música atrapalha quem não é surdo.

## Fase 5 — Capítulo 3: A Biblioteca do Silêncio

**Entrega:** o terceiro capítulo jogável, onde o Marcelo brilha.

- **Arte:** estantes modulares (1,0 × 2,4 × 0,4 m), mezanino, tapetes, luminárias verdes.
- **Mecânica:** piso que range (gera som), luminária que faz "clique", livros derrubáveis
  como distração (o Bibliotecário para para guardá-los).
- **Voz:** o design depende de "falar alto atrai o fantasma" → **ver decisão 2**.
- **Relíquia:** o diário do Seu Valdemar.

**Pronta quando:** a fase é vencível em silêncio e perdível por barulho.

## Fase 6 — Seu Valdemar, relíquias e o final

**Entrega:** a campanha completa, com os dois finais.

- **Relíquias:** um móvel especial que pesa mais quanto mais longe da porta e grava um marcador
  no save ao ser entregue (cartaz, partitura, diário).
- **Seu Valdemar:** modelo e presença por estado (só rádio → translúcido → sólido), com a borda
  de luz indo de ciano a âmbar conforme as relíquias.
- **O Inquilino + o Sobrado:**
  - etapa 1: os três comportamentos de fantasma ao mesmo tempo;
  - etapa 2: carregar as relíquias pela escada estreita;
  - etapa 3: a conversa final e o encolhimento até o velho na caixa.
- **Final alternativo:** relíquia esquecida → bilhete "Volte para buscar o resto".

**Pronta quando:** a campanha vai do capítulo 1 ao final, e o final muda conforme as relíquias.

## Em paralelo — animações e comunicação da equipe

Não bloqueia nenhuma fase; entra quando houver gente livre.

- **Animações** (Animator com andar e ações): batida de bengala da Cida, sinais de Libras do Tiago,
  postura carregando peso, postura curvada do Bibliotecário. O `DollWalk` continua como reserva.
- **Marcelo:** roda de gestos (8 sinais) e quadro branco (desenho do jogador num RenderTexture).
- **Tiago:** bloquinho com desenho.

---

## Decisões em aberto

Precisam de resposta do time antes da fase indicada.

| # | Decisão | Opções | Bloqueia |
|---|---|---|---|
| 1 | Acabamento do mundo 3D | low-poly com cores chapadas (DESIGN.md) **ou** PBR estilizado com rampa de luz (Art Bible) | arte final a partir da Fase 3 |
| 2 | Chat de voz | voz por proximidade (Vivox ou similar, tem custo e conta da Unity) **ou** uma tecla "chamar" no lugar | Fase 5 |
| 3 | Quem troca o visual dos prefabs do mundo | o builder (como na Fase 1) **ou** a pessoa que edita o prefab | Fase 3 |
| 4 | ~~Fantasmas por composição~~ | **decidido:** base comum `Ghost` + comportamentos | — |

## Riscos

- **Rede:** cada mecânica nova (disfarce, hipnose, relíquia) precisa funcionar no host e no
  cliente. Testar sempre com dois jogadores (Multiplayer Play Mode).
- **Conflitos no Git:** builders e prefabs são mexidos por várias pessoas. Combinar quem está em
  qual arquivo e fazer pull antes de começar.
- **Escopo do final:** a Fase 6 é a maior. Se apertar, o Inquilino pode estrear com uma etapa e
  ganhar as outras depois.

## Como trabalhar numa fase

1. Escolha a fase e avise o time (quem pega o quê).
2. Crie uma branch a partir da `main` atualizada.
3. Arte primeiro (scripts em `tools/blender`), depois mecânica, depois a cena.
4. Teste com dois jogadores.
5. Atualize este arquivo e a tabela "Situação" do ART_BIBLE.md no mesmo PR.
