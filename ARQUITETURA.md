# Arquitetura — Mudança Assombrada

Como o código está organizado e **onde mexer para adicionar cada tipo de coisa**.
O visual (cores, fontes, tamanhos) está em [DESIGN.md](DESIGN.md).
Os modelos 3D (personagens, fantasmas, cenários, pipeline Blender → Unity) estão em [ART_BIBLE.md](ART_BIBLE.md).
O que vem pela frente, em fases: [PLANEJAMENTO.md](PLANEJAMENTO.md).

---

## Visão geral

```
Assets/
├── Scripts/                  ← o JOGO (assembly "MudancaAssombrada")
│   ├── Campaign/             capítulos da história, a Campanha e o save
│   ├── Characters/           personagens: fichas, elenco, habilidades (apito, vibração)
│   ├── Audio/                o que se OUVE: biblioteca de sons, quem toca (GameAudio), loops, botões
│   ├── Echo/                 sons (SoundEvent, com TIPO) e o eco (EchoSystem, bengala/passos)
│   ├── Objectives/           objetivos das fases (entregar, chegar, usar) e objetos usáveis
│   ├── Furniture/            móveis carregáveis
│   ├── Ghosts/               fantasmas (IA por som)
│   ├── Input/                GameInput: todos os controles, com nome
│   ├── Interaction/          a mira (PlayerInteractor) e o contrato dos alvos (IInteractionTarget)
│   ├── Senses/               acontecimentos (GameCue) e o que cada jogador percebe (CueListener)
│   ├── Level/                peças da fase (caminhão, câmera de espera)
│   ├── Menu/                 menu principal, pausa, opções, configurações, fontes
│   ├── Network/              conexão, sala de espera, jogadores na rede
│   ├── Player/               movimento, carregar móveis, animação do boneco
│   ├── UI/                   HUD da partida
│   ├── GameManager.cs        regras da partida (tempo, vitória, derrota)
│   └── Theme.cs              cores e tamanhos do padrão visual
├── Editor/                   ← FERRAMENTAS do editor (assembly "MudancaAssombrada.Editor")
│   ├── GameDataBuilder       cria as fichas de personagem (se faltarem)
│   ├── LevelPrefabsBuilder   cria os prefabs (se faltarem)
│   ├── SoundLibraryBuilder   monta a biblioteca de sons a partir do catálogo (tools/audio)
│   ├── ArtImportSettings / AudioImportSettings  importação de modelos e sons pela pasta
│   ├── HudBuilder / UiBuilder  monta interface com o padrão visual
│   ├── MirrorHouseBuilder    rascunho da Casa do Espelho (capítulo 1)
│   └── MenuSceneBuilder      cena do menu (100% gerada)
├── Data/Personagens/         fichas dos personagens (.asset, editáveis)
├── Data/Capítulos/           fichas dos capítulos (.asset, editáveis)
├── Audio/<Pasta>/            sons gerados por tools/audio/gerar_sons.py (Passos, Moveis, Fantasmas...)
├── Resources/                Elenco.asset, Campanha.asset, Sons.asset e fontes (carregados por nome)
└── Prefabs/                  Casa, Móveis, Fantasmas, Sistemas, Interface, Player
```

Todo o código fica no namespace `MudancaAssombrada` (e `MudancaAssombrada.Editor`).
As duas **Assembly Definitions** (`.asmdef`) separam jogo de editor: o jogo nunca
depende do editor, e mudar um script só recompila a parte dele.

## O jogo é uma HISTÓRIA

Mudança Assombrada é uma campanha com fim (ver [roteiro.txt](roteiro.txt)), não fases
infinitas. A estrutura segue isso:

- **Campanha** (`Resources/Campanha.asset`): a lista ORDENADA de capítulos.
  Um capítulo só libera quando o anterior foi concluído.
- **Capítulo** (`ChapterDefinition`): a casa, o fantasma, o personagem em destaque,
  a cena, o tempo até amanhecer (0 = sem relógio, como na fase final), o pagamento,
  o bilhete de abertura do Seu Valdemar e a ficha de avaliação **por personagem**.
- **Objetivos** ficam na cena de cada fase (componentes `Objective`), divididos em **etapas**.
  Fase normal: tudo na etapa 0. Fase final: etapas 0, 1, 2, como no roteiro.
- **Save** (`SaveSystem`, arquivo `save.json`): capítulos concluídos, dinheiro e
  **marcadores da história** (para pistas e o final alternativo). Cada jogador tem o seu;
  numa partida vale o save do host para liberar capítulos.
- **Sons têm tipo** (`SoundType`: passo, respiração, voz, impacto, bengala, apito, música).
  Cada fantasma escolhe o que ouve, o eco e a vibração escolhem o que mostram.
  É isso que permite o Imitador (sem passos nem respiração), a Maestrina (música) e o
  Bibliotecário (voz).

## Princípios

1. **Dados fora do código.** Números de equilíbrio e conteúdo moram em
   ScriptableObjects (fichas) e prefabs, editados no Inspector.
2. **Composição.** Um personagem é uma combinação de componentes pequenos
   (bengala, visão de vibração, apito...), ligados ou desligados pela ficha.
3. **Lógica não desenha; tela não decide.** `GameManager`, `PlayerMovement` e
   `FurnitureCarrier` só expõem estado. A `Hud` só lê esse estado e desenha.
4. **Eventos em vez de dependências.** Sons passam pelo `EchoSystem.SoundEmitted`:
   fantasma, eco e vibração ouvem sem se conhecer. `NetworkPlayer.LocalSpawned` avisa
   quando o seu boneco nasce.
5. **Registros em vez de busca.** `NetworkPlayer.All`, `Furniture.All`,
   `Ghost.All`, `TruckZone.TotalDelivered`: nada de `Find` / `FindObjectsByType`
   no meio do jogo.
6. **Controles com nome.** Ninguém lê o teclado direto: tudo passa pelo `GameInput`.
7. **Cenas de fase são de quem desenha a fase.** O código cria um rascunho; depois,
   a cena é editada no Unity. O menu é a exceção (só interface, sempre gerado).
8. **Rede: o host decide.** Regras (tempo, quem pega o móvel, IA do fantasma) rodam
   no host. Cada jogador é dono do próprio boneco e do móvel que carrega.

## Receitas: como adicionar...

### Um personagem
1. `Assets/Data/Personagens` → botão direito → **Create › Mudança Assombrada › Personagem**.
2. Preencha a ficha (nome, cor, atributos, habilidades).
3. Arraste a ficha para a lista do **Elenco** (`Assets/Resources/Elenco.asset`).
4. Rode **Criar Cena Menu** (para o botão aparecer na sala de espera).
5. Se quiser 4+ jogadores, aumente `NetworkSession.MaxPlayers`.

### Uma habilidade
1. Crie um componente em `Scripts/Characters` (ex.: `Flashlight`), lendo o
   controle do `GameInput`.
2. Adicione uma caixinha na `CharacterDefinition` (ex.: `public bool flashlight;`).
3. Em `CharacterSwitcher.Become`, ligue o componente pela ficha.
4. Em `LevelPrefabsBuilder.BuildPlayer`, adicione o componente ao Player
   (e na lista `ownerOnly` se ele lê controles ou mexe na tela).
5. Se ela tem tecla, crie a ação no `GameInput` e mostre a dica na `Hud.UpdateCharacter`.

### Um controle (tecla)
Uma linha no `GameInput` (ex.: `Flashlight = Button(map, "Lanterna", "<Keyboard>/q");`).
Use `GameInput.Flashlight.WasPressedThisFrame()` e `GameInput.KeyLabel(...)` para a dica.

### Um móvel
Duplique um prefab em `Assets/Prefabs/Móveis`, troque o modelo/tamanho e o **peso**
(componente Furniture). Arraste para a cena. O caminhão e o placar contam sozinhos.
O desenho fica no filho `Modelo` (o FBX); a colisão é o BoxCollider da raiz, que é o que a
mira e o caminhão enxergam. Modelo novo gerado pelo Blender: ver [ART_BIBLE.md](ART_BIBLE.md).

### Um fantasma (ou tipo de fantasma)
Duplique `Assets/Prefabs/Fantasmas/Fantasma Bibliotecário` e ajuste velocidades,
audição e alcance no Inspector. Todo fantasma tem a base **`Ghost`** (registro em `Ghost.All`,
reinício, captura, passos e o estado na rede), o **`GhostVisual`** (só lê o estado e mexe no
modelo) e, se respira, o **`Breathing`**. O "jeito" de assombrar é um **comportamento**: uma classe
que herda de `GhostBehaviour` (ex.: `GhostHunter`, caça por som) e muda `Ghost.ServerSetState`.
Vários comportamentos no mesmo objeto se somam (é o plano do Inquilino): a cada frame o `Ghost`
pergunta `ServerPriority()` a todos e dá o "volante" a quem quer mais (`ServerGainControl` /
`ServerLoseControl`); só ele recebe `ServerTick`, os outros `ServerIdleTick`. Os SENTIDOS são
componentes à parte: `GhostHearing` (ouve os tipos de som marcados e avisa pelo evento `Heard`).
A raiz fica no meio do corpo (cápsula de colisão e `NavMeshAgent` com `baseOffset` = metade da
altura); o desenho é o filho `Modelo`, com `DollWalk` para balançar braços e pernas.

### Uma fase (capítulo)
1. **Mudança Assombrada › Nova Fase (cena vazia)**. A cena nasce com o prefab **Sistemas da Fase**
   (partida, rede, eco, noite, nascimento, HUD, pausa: não mexa nele na cena), luz, câmera de
   espera, chão, 3 marcadores de nascer e o objetivo padrão.
2. Monte a casa à mão com os prefabs (`Casa`, `Móveis`, `Espelhos`...) e os **marcadores**
   (componentes num objeto vazio):
   - **Spawn Point**: onde cada jogador nasce (1º jogador no 1º da Hierarchy);
   - **Ghost Hideout** (ou um tipo, como **Mirror**): esconderijo de onde o fantasma sai e para onde foge;
   - **Room**: cômodo com nome e caixa (os avisos dizem "caiu (Camarim)").
   Depois selecione o objeto **NavMesh** → **Bake**.
3. Coloque os **objetivos** (veja abaixo).
   Cena antiga, com sistemas soltos? Abra e rode **Usar Sistemas da Fase na cena aberta**.
4. Na ficha do capítulo (`Data/Capítulos`), preencha **Scene Name** com o nome da cena.
   As 4 fases do roteiro já têm ficha, só falta a cena ("Em produção" no menu).
5. Rode **Criar Cena Menu** (atualiza a Build Settings). O capítulo aparece na sala de espera.

### Um objetivo
Crie um objeto vazio na cena e adicione um destes componentes:
- **Deliver Furniture Objective**: levar móveis ao caminhão (lista vazia = todos).
- **Reach Area Objective**: chegar a um lugar (ajuste a caixa do BoxCollider).
- **Interact Objective**: usar objetos (arraste os `Interactable` para a lista).

O campo **Stage** divide em etapas: a etapa 1 só começa quando todos os da etapa 0 terminam.
Tipo novo de objetivo = uma classe que herda de `Objective` (2 propriedades: `IsComplete` e `Progress`).

### Algo usável (instrumento, interruptor, porta...)
Adicione **Interactable** + **NetworkObject** a um objeto com Collider. Escreva o verbo
(ex.: "Desligar"), limite a certos personagens se quiser (ex.: só o Tiago) e ligue efeitos
em **On Used** sem código.

### Algo para fazer com a tecla Interagir (E)
Crie um componente que assina `IInteractionTarget` (dois métodos: `PromptFor`, que devolve a dica,
e `Interact`) e coloque num objeto com Collider. O `PlayerInteractor` mira, mostra a dica e chama.
Exemplos: `Furniture` (pegar), `Interactable` (usar), `Pointable` (apontar o falso).
O jogador não muda; um objeto pode ter vários alvos (vale o primeiro com dica). O alvo pode ficar no
pai do Collider atingido. Para **segurar** a tecla, assine `IHoldInteractionTarget`: `Interact` é o
começo e `HoldStop` chega quando o jogador solta, tira a mira ou sai de alcance; quem conta o tempo é o
alvo (ex.: `PlayerCondition`, levantar um colega: 8,4 s segurando, contados no host).

### Um acontecimento que os jogadores percebem (aviso)
`GameCues.Emit(new GameCue(posição, alcance, Senses.Sight | Senses.Hearing, "texto", gameObject))`.
O `CueListener` do jogador local filtra pelos sentidos da ficha (cego não vê, surdo não ouve,
o Tiago sente vibração) e a HUD mostra "[viu] / [ouviu] / [sentiu]". Textos diferentes por
sentido = um `Emit` para cada. Na rede, vai pelo `SoundRelay` (só o dono da fonte envia).

### Um efeito de fantasma NO jogador (puxar, hipnotizar, derrubar)
O fantasma (no host) só chama `player.Condition.ServerSet(PlayerState.X, NetworkObject)`; para
desfazer, `ServerClear(PlayerState.X)`. O `PlayerCondition` aplica as consequências no boneco do dono
(travar, ser arrastado, largar o móvel) e avisa a equipe. Efeito novo = um valor no `PlayerState`
e o efeito dele em `PlayerCondition.ApplyEffects`. Pego = `Ghost.ServerCatch(player)` (fica caído;
a partida só acaba quando todos caem).

### Um tipo de som
Um valor novo no `SoundType` (o próximo bit livre, ex.: `1 << 7`). Quem faz o som usa
`EchoSystem.Emit(new SoundEvent(..., SoundType.Novo, gameObject))`; fantasmas, eco e
vibração decidem no Inspector se reagem a ele. Para ele também se OUVIR, dê um som padrão ao
tipo em `Sons.asset` (lista "Defaults").

### Um som (arquivo de áudio)
O `SoundEvent` é a REGRA (quem ouviu, até onde); o arquivo é só o que sai na caixa de som.
1. Descreva o som em `tools/audio/catalogo.json` (id, pasta, texto em inglês para a IA, duração).
   Som que a IA entrega baixo demais (os ambientes vêm a ~−60 dB): `"normalizar": -30` gera em
   PCM, ajusta o volume do arquivo até esse nível e salva em `.wav`.
2. `python tools/audio/gerar_sons.py --so <id>` (chave do ElevenLabs na variável
   `ELEVENLABS_API_KEY`; o script pula o que já existe). Sai em `Assets/Audio/<pasta>/`, e a
   importação é configurada pela pasta (`AudioImportSettings`).
3. **Mudança Assombrada › Atualizar Biblioteca de Sons** liga os arquivos ao `Sons.asset`.
4. Para tocar: nada a fazer se é um `SoundEvent` (o `GameAudio` toca todos sozinho, pelo tipo).
   Um objeto com som próprio (passos arrastados do fantasma) ganha um `SoundVoice`. Loop de
   cena (clima, relógio, música): componente `AmbientSound`. Som avulso: `GameAudio.Play(id, posição)`.
   Som pelo estado de um fantasma (vidro trincando ao ser desmascarado, zumbido enquanto puxa):
   `GhostSounds`, com trocas de estado e loops preenchidos no Inspector (o Imitador já tem).

**Mixagem (para os sons não se atropelarem).** Cada som tem uma **camada** (cama, ritmo, pista,
ameaça, música, interface), volume, alcance, intervalo mínimo e se entra "no tempo". Os números
ficam na lista `mixagem` do `catalogo.json` (calculados pelo volume medido de cada arquivo) e o
menu Atualizar Biblioteca de Sons os copia para o `Sons.asset`. O `GameAudio` aplica três regras:
o mesmo som não repete antes do intervalo mínimo; sons de **ameaça** abaixam a cama e o ritmo
(`BedGain`); e sons marcados "no tempo" (respiração, sussurro) esperam o **contratempo** do relógio
da casa (`HouseClock`, um tique ou taque por segundo), para não cair em cima da batida. Passos,
batidas, voz e ataques nunca esperam: são informação e precisam ser instantâneos.

### Um marcador da história (pista, escolha)
`SaveSystem.Data.SetFlag("achou-cartaz-teatro"); SaveSystem.Save();` e depois
`SaveSystem.Data.HasFlag(...)` onde for preciso (ex.: o final alternativo).

### Algo na HUD
Exponha o estado no script que é dono dele (propriedade pública, só leitura) e
leia na `Hud`. Crie a peça visual no `HudBuilder` (ou direto no prefab HUD).

### Uma animação do jogador
Os clipes ficam em `Assets/Art/Animations/` (Mixamo: *FBX for Unity*, *Without Skin*, 30 fps, *In Place*
nos de andar). O `ArtImportSettings` importa essa pasta como **Humanoid**, com o nome do arquivo como
nome do clipe e em loop (menos `Agachar`), e o `AnimationBuilder` monta o `Jogador.controller`: duas
misturas 2D pela direção (`Lado` e `Frente`, em m/s, em relação ao boneco), trocadas pelo bool
`Agachado`. Em pé: andar a 4 e correr a 7,2 m/s para frente, trás, esquerda e direita
(`Andar`, `AndarTras`, `AndarEsquerda`, `AndarDireita`, e o mesmo com `Correr`); agachado: 2 m/s
(`AgachadoAndar`, `AgachadoTras`, `AgachadoDireita`; a esquerda é o `AgachadoDireita` **espelhado**,
porque o "Crouched Sneaking Left" do Mixamo vem girado ~160°). Clipe "para trás" que falta é o da
frente tocado ao contrário. O controller só é criado se faltar (ou se for da
versão antiga, sem direção); depois é editado no Animator do Unity. Quem
preenche os parâmetros é o `DollWalk` (em todos os computadores: a posição vem pelo `NetworkTransform`
e o agachar pela `NetworkVariable` do `NetworkPlayer`). Modelo sem Humanoid e o boneco de blocos
continuam com a animação por código do `DollWalk`. Caído (`PlayerState.Down`): bool `Caido`, de
Em pé ou Agachado para `Cair` (Falling Forward Death, sem loop: fica deitado no último quadro).
`Levantar` (Standing Up From Lying On Stomach, que termina relaxado; o começo, parado no chão, foi
cortado: `firstFrame` 85) não toca sozinho: o tempo do estado é o float `Ajuda`, o progresso do
resgate (`PlayerCondition.RescueProgress`), que sobe enquanto um colega segura a tecla e volta se
ele soltar; completo, `Caido` desliga e volta para Em pé. Sem *Any State*, de propósito: ele
dispararia a queda de novo quando a `Ajuda` voltasse a zero. Os dois precisam combinar: o fim da queda e o começo do levantar
com a cabeça para o mesmo lado. O Mixamo gira o corpo dentro dos clipes: o *Offset* de rotação
(Cair −45°, Levantar 0°, no `.meta`) foi medido pela curva `RootQ` do clipe. A queda termina de lado
e o levantar começa de bruços: a troca dura 0,6 s, para virar uma rolada. Deitado e levantando, o
mouse só vira a cabeça (`PlayerMovement.BodyLocked`).

### Um espelho com reflexo
Use um prefab de `Prefabs/Espelhos` (o `MirrorHouseBuilder` já põe o `MirrorReflection` e troca o
vidro da moldura, material `MAT_ENV_Espelho`, pelo `Espelho Vidro.mat`, do shader `Espelho`). O
reflexo é planar: uma câmera espelhada grava a cena numa textura, só neste computador. Para pesar
pouco, só os **2 espelhos mais perto e na tela** refletem (meia resolução, sem sombras nem
pós-processamento); os outros, e todos no modo cego, mostram o vidro escuro. O campo **Reflected
Layers** escolhe o que aparece no espelho: é o gancho para pistas que só existem no reflexo.

## Menus do editor (Mudança Assombrada)

| Menu | O que faz | Apaga trabalho manual? |
|---|---|---|
| Criar Todas as Cenas | dados + prefabs que faltam + fase (pergunta) + menu | só se você confirmar |
| Criar Cena Menu | recria o menu | não (o menu é só código) |
| Criar Cena Casa do Espelho | prefabs do capítulo 1 que faltam + rascunho da cena + liga a ficha | **sim** (pergunta antes) |
| Nova Fase (cena vazia) | cena nova com Sistemas da Fase, chão e marcadores | não (cena nova) |
| Usar Sistemas da Fase na cena aberta | troca eco/partida/HUD/pausa soltos pelo prefab e cria marcadores de nascer | só os sistemas (pergunta antes) |
| Criar Dados que Faltam | cria fichas/Elenco se não existirem | não |
| Criar Prefabs que Faltam | cria prefabs do mundo que não existem; recria Player, HUD e Pausa | não |
| Recriar Todos os Prefabs | volta todos os prefabs ao original | **sim** (pergunta antes) |
| Atualizar Biblioteca de Sons | liga os arquivos de `Assets/Audio` ao `Sons.asset` e dá voz aos fantasmas que não têm (também roda em "Criar Prefabs que Faltam") | não (volume e alcance ajustados ficam) |
| Apagar Progresso (save) | zera capítulos concluídos, dinheiro e marcadores | **sim** (pergunta antes) |

**Rodar os menus não "suja" o Git.** Recriar o Menu, a HUD, a pausa ou o Player do zero faz a
Unity sortear números internos novos (fileID), e o arquivo inteiro pareceria mudado. O
`StableAssets` compara o arquivo novo com o antigo ignorando esses números e, se nada mudou de
verdade, devolve o arquivo antigo. Prefab novo de construtor: salve com `EditorTools.SavePrefab`.

## Testes

- Trocar de personagem com 1/2/3 durante a partida só funciona no editor e em builds de
  desenvolvimento (`CharacterSwitcher.DebugSwitchingEnabled`). No jogo final, cada jogador
  é o personagem que escolheu na sala de espera, como no roteiro.
- Não há mais capítulo de teste: a sala de espera já começa na Casa do Espelho (o primeiro capítulo).
