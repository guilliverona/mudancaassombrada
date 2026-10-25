# Arquitetura — Mudança Assombrada

Como o código está organizado e **onde mexer para adicionar cada tipo de coisa**.
O visual (cores, fontes, tamanhos) está em [DESIGN.md](DESIGN.md).

---

## Visão geral

```
Assets/
├── Scripts/                  ← o JOGO (assembly "MudancaAssombrada")
│   ├── Characters/           personagens: fichas, elenco, habilidades (apito, vibração)
│   ├── Echo/                 sons e o eco (EchoSystem, bengala/passos)
│   ├── Furniture/            móveis carregáveis
│   ├── Ghosts/               fantasmas (IA por som)
│   ├── Input/                GameInput: todos os controles, com nome
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
│   ├── HudBuilder / UiBuilder  monta interface com o padrão visual
│   ├── PrototypeSceneBuilder rascunho inicial da fase
│   └── MenuSceneBuilder      cena do menu (100% gerada)
├── Data/Personagens/         fichas dos personagens (.asset, editáveis)
├── Resources/                Elenco.asset e fontes (carregados por nome)
└── Prefabs/                  Casa, Móveis, Fantasmas, Sistemas, Interface, Player
```

Todo o código fica no namespace `MudancaAssombrada` (e `MudancaAssombrada.Editor`).
As duas **Assembly Definitions** (`.asmdef`) separam jogo de editor: o jogo nunca
depende do editor, e mudar um script só recompila a parte dele.

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
   `GhostHunter.All`, `TruckZone.TotalDelivered`: nada de `Find` / `FindObjectsByType`
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

### Um fantasma (ou tipo de fantasma)
Duplique `Assets/Prefabs/Fantasmas/Fantasma Bibliotecário` e ajuste velocidades,
audição e alcance no Inspector. Comportamento diferente = um script novo baseado no
`GhostHunter` (ele se registra em `GhostHunter.All` para o reinício funcionar).

### Uma fase
1. Duplique `Assets/Scenes/Prototipo1` (ou crie uma cena nova com os prefabs de
   `Sistemas` e `Interface`, mais um `PlayerSpawner`).
2. Monte a casa com prefabs, selecione o objeto **NavMesh** → **Bake**.
3. Adicione a cena na Build Settings e troque `NetworkSession.GameScene`
   (seleção de fase ainda não existe: é o próximo passo natural).

### Algo na HUD
Exponha o estado no script que é dono dele (propriedade pública, só leitura) e
leia na `Hud`. Crie a peça visual no `HudBuilder` (ou direto no prefab HUD).

## Menus do editor (Mudança Assombrada)

| Menu | O que faz | Apaga trabalho manual? |
|---|---|---|
| Criar Todas as Cenas | dados + prefabs que faltam + fase (pergunta) + menu | só se você confirmar |
| Criar Cena Menu | recria o menu | não (o menu é só código) |
| Criar Cena Protótipo 1 | recria a fase a partir dos prefabs | **sim** (pergunta antes) |
| Criar Dados que Faltam | cria fichas/Elenco se não existirem | não |
| Criar Prefabs que Faltam | cria prefabs que não existem; recria o Player | não |
| Recriar Todos os Prefabs | volta todos os prefabs ao original | **sim** (pergunta antes) |
