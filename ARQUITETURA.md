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
│   ├── MirrorHouseBuilder    capítulo 1: prefabs da Casa do Espelho; .Mansao.cs monta a planta de tools/level/mansao_v1_2.json
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
4. Rode **Preparar Tudo** (para o botão aparecer na sala de espera).
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
**Peso e dupla (vale em todas as fases):** cada um levanta sozinho até a **própria força**
(`CharacterDefinition.LiftLimit`): Cida 1, Tiago 1,5, Marcelo 3. Mais pesado, sozinho só arrasta
(no chão, devagar, `SoundType.Carry` forte = `arrastar_movel`). Quem pega primeiro é a primeira
ponta (dono, simula a física); um colega mirando vê "Segurar a outra ponta" (ajudante,
`Furniture.helper`). A dupla levanta até a soma das forças (Cida + Tiago = 2,5; o armário, 3,5, pede
o Marcelo), divide o peso por essa soma e anda no passo do mais lento; peso acima de 2 na escada anda
÷1,5. O Imitador pode ser a "ponta falsa" (`ServerSetFakeHelper`): aparece o nome, não vale nada.
Quem precisa de ajuda para cada móvel sai só do **peso** dele e da **força** das fichas.

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
Estado novo em `GhostState` vai sempre **no fim** do enum (o número fica gravado nos `GhostSounds`).
O `GhostVisual` lê o estado e mexe no modelo: inclinação por estado, o osso `hat` (cartola) e o
blendshape `Tragedia` (máscara), quando o modelo tem. Apontar (`Pointable`) vale enquanto
`Ghost.CanBeUnmasked`: disfarçado, ou na reverência do Imitador (`GhostState.Bowing`).
Fantasma que "some" (o Imitador dentro do espelho, `GhostState.InMirror`): o `GhostVisual` esconde o
corpo e desliga os colisores; para trocar de lugar sem deslizar na tela dos outros, `Ghost.ServerTeleport`.
Som por estado do fantasma: `GhostSounds` (uma troca de estado toca uma vez, com atraso, chance de
pular e "sem direção" opcionais; um estado repete um som enquanto dura, mais rápido no fim da noite).
Pista que só a Cida vê: um timbre do `SurfaceTag` que o `Echo.shader` desenha mesmo sem onda (o espelho
ocupado "respira"); quem enxerga usa o material normal e não vê nada.
Qual espelho o Imitador usa (entrar, puxar, fugir): `MirrorPaths.Nearest`/`Farthest`, pelo caminho no
NavMesh e não em linha reta (numa casa de dois níveis, o espelho "logo acima" pode estar a 30 m andando).
Na Mansão: o `SecretMirrorDoor` (o espelho-porta E9, que só a Cida revela) e o `PortalTrap` (o portal
da Câmara, que puxa quem fica sozinho; o Imitador sai por ele sem disfarce, `ServerAmbushFromPortal`)
não são `Mirror`: não entram no rodízio de esconderijos. `AppearanceLights` acende o lustre e o
refletor só enquanto ele está em cena.
Coisas que acontecem numa hora da noite (a aparição dos 45 s, o manequim que tomba) leem o relógio
do `GameManager` (`NightDuration − TimeLeft`), que já chega a todos: sem rede própria (`TimedTopple`).

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
5. Rode **Preparar Tudo** (atualiza a Build Settings). O capítulo aparece na sala de espera.

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
a partida só acaba quando todos caem). Puxado: o fantasma escreve a contagem (`ServerSetPullLeft`,
a HUD mostra) e lê `IsAnchored` (um colega segurando: "Segurar fulano", o mesmo segurar-E do resgate);
segurado até o fim, a condição volta a `Normal` e o fantasma percebe que a vítima escapou.

### O que a Cida "ouve" da casa (diapasão, ruído, timbre)
- **Timbre** (`SurfaceTag` na raiz de um objeto): do que ele é feito (madeira, vidro, metal, tecido,
  pessoa, cópia). O `EchoSystem` passa para o material de eco (`_Timbre`), e a onda do **diapasão**
  (`SoundType.Tuning`, `TuningFork`, tecla Q) faz cada coisa responder do seu jeito. A cópia do
  Imitador não responde. Objeto novo que importa no eco = um `SurfaceTag` no prefab.
- **Ruído** (`NoiseSource`): uma fonte ligada encolhe e borra o eco por perto (`EchoSystem.NoiseLevel`).
  Quem desliga é um `Interactable` no mesmo objeto (o "Afinar" da Cida, com `holdSeconds` e `onlyFor`).
- **Piso que range** (`CreakyFloor`): área (sem colisor) onde o passo vira rangido alto.
- **Chamada** (`CaneCall`): segurar a bengala; marcador no chão para todos.
- **Que desenho cada som faz** (`EchoRules`, testado em `EchoRulesTests`): bengala e diapasão = onda
  cheia (no máximo 3 ao mesmo tempo); os outros sons = só um **ponto** onde nasceram; os próprios passos
  e a própria respiração = nada (menos o passo forte da tábua que range). O mesmo emissor dentro de 0,4 s
  reaproveita a onda. Só muda o desenho: os fantasmas ouvem todo `SoundEvent` igual.
- **Tela quase escura**: um vulto cinza de perto (`nearRadius`/`nearStrength`) e um contorno fraco no que
  a bengala já revelou (`memoryStrength`, só peças sem `NetworkObject`), no Inspector do `EchoSystem`.
  Nenhum dos dois usa cor nem timbre, para não entregar pista. Zerar os três volta à tela 100% escura.

### O que o Tiago vê (chão tremendo)
- O `VibrationVision` mostra cada som como um pedaço de **chão tremendo** (`VibrationMark`, shader
  `VibrationTremor`: entorta a imagem da tela com `_CameraOpaqueTexture`, ligada no `PC_RPAsset`, e põe
  um brilho fraco para não sumir no escuro). É **uma marca por emissor**, que segue quem fez o som.
- As regras ficam no `VibrationRules` (testado em `VibrationRulesTests`): fantasma, impacto e apito =
  forte (vermelho, até 20 m); colegas, bengala, móvel arrastado e música da casa = fraco (amarelo, até
  12 m); os próprios sons, voz e respiração = nada; outro andar (mais de 2,5 m de altura) = nada. No
  máximo 6 marcas, e uma fraca nunca tira uma forte. O `VibrationRing` continua só para a chamada da bengala.

### Hotbar
O `Hotbar` no Player guarda até 3 itens pequenos (`HotbarItem`: lanterna, pilhas, bengala),
empacotados num `uint` de rede que o host escreve; qual espaço está na mão é do dono (teclas 1-2-3,
`GameInput.HotbarSlot`; rodinha e direcional para os lados passam de espaço; a mesma tecla de novo
guarda). Quantos espaços vem da ficha (`hotbarSlots`: o Marcelo tem 2) e o **kit** (`ApplyKit`: a
bengala da Cida no espaço 1, já na mão) é aplicado ao nascer, ao trocar de personagem e ao reiniciar.
As regras ficam em `HotbarRules` (testes em `HotbarRulesTests`). A tecla própria de um item (L, Espaço)
faz o mesmo que o número + usar (`OnItemKey`). "Usar item" (botão esquerdo / gatilho direito) usa o
que está na mão (hoje: trocar as pilhas, **segurando** por 3 s: `HoldAction`, testes em `HoldActionTests`;
soltar antes perde o que foi, e completar só vale de novo depois de soltar). O `HeldItems` mostra no boneco o que está na mão: com a
lanterna **na mão**, o braço direito sobe: a camada "Lanterna" do Animator fecha os dedos no cabo
(clipe `PegaLanterna`, gerado por `tools/blender/build_pega_lanterna.py` no rig do Tiago) e, por cima,
um IK de dois ossos leva a mão para baixo e um pouco à frente do **ombro suavizado** (`carry`, em
"braços": cada personagem na altura dele; correndo, a mão não sacode), o braço acompanha metade do
olhar e o punho aponta a lanterna para onde o dono olha (`Flashlight.AimRotation`; o olhar vai pela
rede). O giro em volta do antebraço vai para o antebraço (`SplitTwist`), e só o resto dobra o punho.
Medido: com o punho reto, a pega só aponta para a frente com o antebraço quase em pé; erguida até o
ombro, o punho dobrava 60-80° (o braço "torto"); ao lado do quadril, com o cotovelo rente ao corpo,
~30°. Junto ao corpo, o antebraço entrava no Tiago e na barriga do Marcelo: o `carry` deixa o braço
a 5-18 cm do corpo (pose aprovada pelas fotos em 28/10/2026). A luz sai da lente. Na SUA
tela, as mãos não aparecem: o `FirstPersonArm` (ordem 300, depois do `FirstPersonBody`) encolhe o
braço direito da cópia de primeira pessoa e leva a lanterna a um ponto preso à câmera (`viewOffset`),
onde só a ponta aparece no canto de baixo (os olhos ficam 30-40 cm acima do ombro: pela mão de
verdade, ela sairia da tela). No espelho você se vê pelo corpo (`MirrorReflection.SetOwnBody`), então
enquanto a câmera do reflexo desenha a lanterna volta para a mão do corpo (`FirstPersonArm.ShowInMirror`
→ `HeldItems.ShowLanternOnBody`). Algo novo preso à câmera precisa do mesmo cuidado.
As contas ficam em `HandPoseRules` (testes em `FlashlightArmRulesTests`). No **cinto** (carregando um
móvel), o modelo vai bem do lado DIREITO da cintura (`beltSide`; a bolsa do Tiago fica no esquerdo),
encostado no corpo: o `HeldItems.MeasureBelt` mede uma vez, por personagem, até onde o corpo vai para
esse lado na altura do quadril (sem os braços; `HandPoseRules.BodySide`), então ele encosta na cintura
do Tiago e do Marcelo, cada um do seu tamanho. A luz sai dele; colegas e o seu reflexo veem o mesmo lugar. A bengala da Cida some e solta a pega quando ela escolhe outro espaço. **Decisão:** carregar móvel não guarda a bengala (é a visão da Cida).
Item novo: um nome no `HotbarItem`, dar com `ServerAdd` e o componente do item pergunta
`InHand`/`HandsBusy`. A troca de personagem de teste foi para F1-F3. Na HUD, cada espaço é um
`HotbarSlotView` (caixinha com fita, ícone, bateria, tecla) e as contas da barra (centralizar,
bateria fraca, entrada suave, brilho de item novo) ficam em `HotbarHudRules` (testes em
`HotbarHudRulesTests`). Os ícones (`Assets/Art/Icons/T_ICO_*.png`) são fotografados dos modelos pelo
`ItemIconBuilder` (precisa de placa de vídeo: rodar o Unity em batch SEM `-nographics`, ou o menu
"Recriar Ícones da Hotbar"). Item novo na hotbar = um ícone novo na lista do `ItemIconBuilder`. Trocar de espaço toca `item_troca`
(som procedural, `tools/audio`).

### Lanterna, pilhas e coisas tocáveis
A lanterna é o componente `Flashlight` no Player (tecla L, `GameInput.Flashlight`), mas só funciona
depois de pegar uma: o host a põe na hotbar (`ServerGive`), o dono liga e gasta a bateria, e todos
veem a luz falhando quando ela está fraca. Ela fica na **mão** (espaço escolhido), no **cinto**
(estava na mão e pegou um móvel: mais fraca, aponta para a frente do corpo) ou **guardada** (apaga);
com ela guardada, a tecla L tira e acende (carregando um móvel, direto no cinto). No código, o lugar
"cinto" ainda se chama `FlashlightPlace.Chest` (era o colete). Os itens do
chão são `FlashlightPickup` (lanterna ou pilhas, some ao pegar e volta no reinício). O **facho** tem
desenho (miolo forte, faixa mais escura e o aro do refletor): o cookie `T_PRP_Lanterna_Facho.png`,
gerado pelo `FlashlightCookieBuilder` a partir do `FlashlightBeamRules` (testes em
`FlashlightBeamRulesTests`; mudou o desenho, recrie o Player). A **lente** da lanterna na mão acende
e pisca junto com a luz (`Flashlight.LightLevel` → `HeldItems`, numa cópia do material só dela). As pilhas vão para
a hotbar (ocupam um espaço) e recarregam segurando "Usar item" (`reloadHoldSeconds` = 3 s e `batteryCharge` = 40% no `Flashlight`). Um objeto que dá para **tocar** (o piano) é um
`PlayableInstrument`: o host emite um `SoundEvent` de música e o `SoundVoice` do objeto diz o arquivo.
Luz que treme (arandela, vela): `LightFlicker` na `Light`.
Pilha **escondida**: `startsHidden` no `FlashlightPickup`; aparece quando algo chama `ServerReveal`
(um `HouseScare` com o campo "Reveals"). Luz que se **carrega** (o lampião da Mansão): um móvel
(`Furniture`) com uma `Light` filha; ocupa as mãos e não entra em objetivo nenhum. Luz que se
**acende** (a luminária verde): `Interactable` ("Acender") + `LampSwitch`, que segue o estado dele.

### Algo que a casa faz sozinha (cai, toca, balança)
O `HouseScare` (um objeto de rede; na Mansão, o prefab "Susto da Casa" com o modelo como filho).
No Inspector:
- **Trigger** (pode marcar vários): correndo perto, móvel carregado perto, passar perto (chance,
  sorteada uma vez), um segundo da noite, ninguém olhando.
- **Motion:** cai no chão, gaveta que abre, balanço ou tombo.
- **Som** (vira um `SoundEvent` de impacto: eco e anel), **Cue** (o aviso) e **Reveals** (a pilha).
O host decide e todos animam igual; as regras (um a cada 20 s, nada no pânico, uma vez por noite)
ficam na `HouseScareRules`. **Is Scare** desligado = não é susto (só revela uma pilha).
Teia que fecha um vão: `DoorWeb` (só jogador rasga; o fantasma passa e ela fica: a pista).

### Um achado de época (bônus) ou um frágil
Um móvel com `PeriodFind`: nome, valor (R$), o bilhete do chefe e, se quiser, um som em loop
enquanto é carregado. Não entra em objetivo; entregue no caminhão, soma ao pagamento da noite e
aparece no bilhete da avaliação. **Frágil:** `Fragile` no móvel (e um `SoundVoice` de impacto com o
som de vidro): cada pancada desconta R$ 5. As contas: `PeriodFindRules`; o `GameManager` fecha o
pagamento no fim (`Pay`) e o save recebe o extra (`SaveData.Complete(capítulo, extra)`).

### Um tipo de som
Um valor novo no `SoundType` (o próximo bit livre, ex.: `1 << 7`). Quem faz o som usa
`EchoSystem.Emit(new SoundEvent(..., SoundType.Novo, gameObject))`; fantasmas, eco e
vibração decidem no Inspector se reagem a ele. Para ele também se OUVIR, dê um som padrão ao
tipo em `Sons.asset` (lista "Defaults").

### Um som (arquivo de áudio)
O `SoundEvent` é a REGRA (quem ouviu, até onde); o arquivo é só o que sai na caixa de som.
1. Descreva o som em `tools/audio/catalogo.json` (id, pasta, texto em inglês para a IA, duração).
   Som que a IA entrega baixo demais (os ambientes vêm a ~−60 dB): `"normalizar": -30` gera em
   PCM, ajusta o volume do arquivo até esse nível e salva em `.wav`. `"tom": -2` deixa o som mais
   grave (passos mais sombrios). Uma VOZ falando frases: `"fala": [...]` e `"voz": "<id da voz>"`
   (cada frase vira uma variação). Escreva a fala como se fala (reticência só no fim) e escolha a voz
   de ouvido: voz estrangeira falando português tem sotaque. `"modelo"` troca o modelo de voz.
   `"assombrar"` dá o tratamento de voz de fantasma (grave sem arrastar, oitava abaixo, reverb e eco
   ao contrário); os campos estão explicados no `gerar_sons.py`.
2. `python tools/audio/gerar_sons.py --so <id>` (chave do ElevenLabs na variável
   `ELEVENLABS_API_KEY`; o script pula o que já existe). Sai em `Assets/Audio/<pasta>/`, e a
   importação é configurada pela pasta (`AudioImportSettings`).
3. **Mudança Assombrada › Avançado › Criar Prefabs que Faltam** liga os arquivos ao `Sons.asset`.
4. Para tocar: nada a fazer se é um `SoundEvent` (o `GameAudio` toca todos sozinho, pelo tipo).
   Um objeto com som próprio (passos arrastados do fantasma) ganha um `SoundVoice`. Loop de
   cena (clima, relógio, música): componente `AmbientSound`. Som avulso: `GameAudio.Play(id, posição)`.
   Som pelo estado de um fantasma (vidro trincando ao ser desmascarado, zumbido enquanto puxa):
   `GhostSounds`, com trocas de estado e loops preenchidos no Inspector (o Imitador já tem).

**Mixagem (para os sons não se atropelarem).** Cada som tem uma **camada** (cama, ritmo, pista,
ameaça, música, interface), volume, alcance, intervalo mínimo e se entra "no tempo". Os números
ficam na lista `mixagem` do `catalogo.json` (calculados pelo volume medido de cada arquivo) e o
menu Criar Prefabs que Faltam os copia para o `Sons.asset`. O `GameAudio` aplica três regras:
o mesmo som não repete antes do intervalo mínimo; sons de **ameaça** abaixam a cama e o ritmo
(`BedGain`); e sons marcados "no tempo" (respiração, sussurro) esperam o **contratempo** do relógio
da casa (`HouseClock`, um tique ou taque por segundo), para não cair em cima da batida. Passos,
batidas, voz e ataques nunca esperam: são informação e precisam ser instantâneos.
E **uma ameaça por vez**: ameaça de clima (sussurro, acorde) só toca com nenhuma outra soando e 6 s
depois da última; ameaça marcada `"importante": true` (captura, desmascarar) é acontecimento, toca
sempre e corta as de clima. Um evento só para o eco/vibração, sem som: `SoundVoice` com som vazio.

**Trilha e silêncio (o diretor de som).** A música de uma fase é um asset `Soundtrack`
(`Assets/Data/Trilhas/`): a faixa cortada em trechos, cada um com um humor (calma, crescendo, pânico,
aparição). O `SoundDirector` (no objeto "Sons" da cena) toca a faixa inteira como uma **cama** baixa
e contínua, e troca para o trecho do momento nos sustos, visto do MEU boneco; com o perigo perto, a
cama e a casa se calam (`AmbienceGain`, que todo `AmbientSound` obedece). Faixa trazida pela equipe (não gerada): `"externo": true` no catálogo,
o `gerar_sons.py` nunca mexe. Fase nova = um `Soundtrack` com a faixa dela e o diretor na cena.
Os porquês e os tempos da Casa do Espelho: LEVEL_DESIGN_FASE1.md §10.2.
**A pianola** (Mansão): o componente `Pianola` toca a MESMA faixa da cama em 3D, no mesmo ponto
(`PianolaRules`), e a cama 2D abaixa perto dela (`Pianola.BedDuck`); numa zona de silêncio (a
Câmara) as duas param. As **frases de piano** por momento (aparição, etapa nova, âmbar, captura,
amanhecer) ficam no `PianoPhrases`, que lê o que já chega pela rede (`GameManager.StageIndex`).

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
pós-processamento); os outros, e todos no modo cego, mostram o vidro escuro. Espelho marcado como
**Hero** reflete antes de qualquer comum que esteja na tela (na Mansão, o E1 e o E2 da escadaria). O campo **Reflected
Layers** escolhe o que aparece no espelho: é o gancho para pistas que só existem no reflexo.

### Uma regra na sala de espera (líder, pronto, chat...)
A partida é **só online**: o `NetworkSession` cria/lista/entra em salas pelo pacote Multiplayer
Services (Lobby = lista de salas e código; Relay = repassa os pacotes, sem abrir porta). A sala de
espera em si é o `LobbyManager`:
- **Host x líder:** o host é o servidor (não troca sem derrubar todos). O **líder** manda na sala
  (capítulo, Começar, expulsar, passar a liderança, sala privada) e pode ser qualquer jogador.
- Todo pedido é um `Rpc(SendTo.Server)` e o host confere (`FromLeader`) antes de mudar algo.
- Estado da sala = `NetworkList`/`NetworkVariable`; avisos do sistema e mensagens vão pelo `ChatRpc`.
- Interface: `MenuSceneBuilder` (peças) + `MainMenu` (o que cada botão faz). Mudou a interface → **Preparar Tudo**.

## Menus do editor (Mudança Assombrada)

No dia a dia, só os quatro primeiros. Os dois do **Avançado** são para quando só os prefabs mudaram.

| Menu | O que faz | Apaga trabalho manual? |
|---|---|---|
| Preparar Tudo | tudo do "Criar Prefabs que Faltam" + Casa do Espelho (pergunta) + Menu; o Console diz qual cena foi mantida | só se você confirmar |
| Nova Fase (cena vazia) | cena nova com Sistemas da Fase, chão e marcadores | não (cena nova) |
| Usar Sistemas da Fase na cena aberta | troca eco/partida/HUD/pausa soltos pelo prefab e cria marcadores de nascer | só os sistemas (pergunta antes) |
| Apagar Progresso (save) | zera capítulos concluídos, dinheiro e marcadores | **sim** (pergunta antes) |
| Avançado › Criar Prefabs que Faltam | dados e fichas que faltam, modelos 3D nas fichas, biblioteca de sons (e voz dos fantasmas), animações do Player, prefabs do mundo que não existem; recria Player, HUD e Pausa. Não mexe nas cenas | não (volume e alcance dos sons ficam) |
| Avançado › Recriar Todos os Prefabs | volta todos os prefabs ao original | **sim** (pergunta antes) |

Cenas feitas com **Nova Fase** são de quem edita: nenhum menu as recria.

**Rodar os menus não "suja" o Git.** Recriar o Menu, a HUD, a pausa ou o Player do zero faz a
Unity sortear números internos novos (fileID), e o arquivo inteiro pareceria mudado. O
`StableAssets` compara o arquivo novo com o antigo ignorando esses números e, se nada mudou de
verdade, devolve o arquivo antigo. Prefab novo de construtor: salve com `EditorTools.SavePrefab`.

## Testes

- Trocar de personagem com 1/2/3 durante a partida só funciona no editor e em builds de
  desenvolvimento (`CharacterSwitcher.DebugSwitchingEnabled`). No jogo final, cada jogador
  é o personagem que escolheu na sala de espera, como no roteiro.
- Não há mais capítulo de teste: a sala de espera já começa na Casa do Espelho (o primeiro capítulo).
