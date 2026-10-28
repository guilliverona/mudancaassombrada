# Instruções para o Claude (e para quem chega no projeto)

Este arquivo é lido **automaticamente** pelo Claude Code em toda conversa aberta nesta pasta.
Ele diz o que é o projeto, onde está cada informação e **como trabalhamos**, para nenhuma
conversa nova precisar ser "apresentada" ao projeto de novo.

**Regra deste arquivo:** ele é um MAPA, não um manual. Fatos que mudam com o projeto (o que já
foi feito, medidas, nomes de scripts) moram nos documentos abaixo; aqui fica só o que ajuda a
achá-los e as regras de trabalho. Ver "Como manter este arquivo" no fim.

---

## O projeto em poucas linhas

- **Mudança Assombrada**: co-op online (até 3 jogadores) de terror e comédia, em Unity 6 + URP,
  com Netcode for GameObjects. Uma transportadora falida tira móveis de casas mal-assombradas
  antes do amanhecer. O chefe, Seu Valdemar, só fala por bilhetes e é um fantasma.
- **Personagens jogáveis:** Dona Cida (cega, enxerga pelo eco), Tiago (surdo, vê vibrações),
  Marcelo (mudo, forte e silencioso). Cada fase favorece um deles.
- É uma **campanha com fim**: capítulos em ordem, com save, e um final alternativo.
- Várias pessoas mexem no projeto ao mesmo tempo: antes de reestruturar um sistema que já
  existe, combinar com o time.

## Onde está cada coisa

| Quero saber sobre... | Leia |
|---|---|
| A história, as fases, os fantasmas (fonte original) | [roteiro.txt](roteiro.txt) |
| Como o código está organizado e **como adicionar** personagem, fantasma, fase, objetivo, som... | [ARQUITETURA.md](ARQUITETURA.md) |
| Padrão visual da interface (cores do `Theme`, fontes, HUD) | [DESIGN.md](DESIGN.md) |
| Modelos 3D: pipeline Blender → Unity, regras de modelagem, nomes, orçamento, o que já tem modelo | [ART_BIBLE.md](ART_BIBLE.md) |
| O que já foi feito, o que vem a seguir, decisões em aberto | [PLANEJAMENTO.md](PLANEJAMENTO.md) |
| Testes automatizados: o que já tem, o que falta, em que ordem | [TESTES.md](TESTES.md) |
| Level design da Fase 1: briefing com os números do jogo, para projetar fora do código | [LEVEL_DESIGN_FASE1_BRIEFING.md](LEVEL_DESIGN_FASE1_BRIEFING.md) |
| Fase 1 V1.2: a nova planta (Mansão do Espelho, casa na encosta), plantas, riscos e decisões | [LEVEL_DESIGN_FASE1_MANSAO.md](LEVEL_DESIGN_FASE1_MANSAO.md) (texto e medidas: a fonte) e o [Art Bible da fase](https://claude.ai/artifact/TqLD6yDYTxboC8wwv89ivw) (pranchas desenhadas) |
| Os designs completos (fichas com vistas, cabeça e mãos, cenários) | Art Bible completo (link no ART_BIBLE.md) |

**Antes de começar qualquer tarefa:** ler a parte do documento que trata do assunto e a tabela
"Visão geral" do PLANEJAMENTO.md, e rodar `git status` / `git log --oneline -10` para ver o
estado real (o time trabalha em paralelo; o repositório muda entre conversas).

## Mapa do repositório

```
mudancaassombrada/                 ← raiz do Git (os .md ficam aqui)
├── MudancaAssombrada/             ← o projeto Unity (Assets, Packages, ProjectSettings)
│   └── Assets/
│       ├── Scripts/               jogo (namespace MudancaAssombrada)
│       ├── Editor/                construtores de dados, prefabs, cenas e importação
│       ├── Art/<Tipo>/<Nome>/     FBX gerados pelo Blender (o que o jogo usa)
│       ├── Data/                  fichas de personagens e capítulos (.asset)
│       ├── Resources/             Elenco, Campanha, fontes
│       └── Prefabs/, Scenes/
├── tools/blender/                 scripts que GERAM os modelos (build_all.ps1 roda todos)
├── tools/audio/                   catálogo de sons + gerar_sons.py (ElevenLabs, chave em ELEVENLABS_API_KEY)
├── Art/Source/<Tipo>/<Nome>/      .blend e prévias (fonte, fora do Unity)
└── roteiro.txt, *.md
```

## Ferramentas desta máquina

- **Unity** 6000.6.7f1: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`
- **Blender** 5.2: `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`
- Shell: PowerShell 5.1 (Windows). Caminhos com acento: use `git -c core.quotepath=false`.

## Como trabalhamos (regras)

1. **Idioma:** tudo em português do Brasil: conversa, comentários no código, documentos, commits.
2. **Git:**
   - Commit e push só quando a pessoa pedir.
   - Antes de editar, confirmar a branch atual e se ela está atualizada com a `main`.
     Branch criada de um commit antigo = avisar antes de seguir.
   - Nunca apagar stash, nem fazer `reset --hard`, `force push` ou rebase de algo já enviado
     sem pedido explícito.
   - Ao apresentar mudanças, separar o que é **da tarefa** do que o **Unity regravou sozinho**
     (cenas, `HUD.prefab`, `Pausa.prefab`, `Player.prefab`, `ProjectSettings/*`, `Packages/*`):
     esse "ruído" fica fora dos commits.
3. **Unity aberto x fechado:**
   - Com o Unity aberto não dá para rodar o modo batch nem fazer rebase/merge com segurança
     (ele reimporta e regrava arquivos no meio). Pedir para fechar **todas** as janelas
     (inclusive o Player 2 do Multiplayer Play Mode) e conferir: `MudancaAssombrada/Temp/UnityLockfile`
     não pode existir.
   - Mudança em script com o Unity aberto: avisar que ele recompila ao receber foco, e que nada
     reimporta enquanto o **Play** estiver ligado.
4. **Verificar antes de dizer que funciona.** Compilar em batch, conferir prévias, medir no Unity.
   O que não foi testado com Play é dito como "não testado com Play".
5. **Código:** seguir o estilo que já existe: comentários didáticos em português, dados em
   ScriptableObjects/prefabs, composição, registros (`All`) em vez de `Find`, controles pelo
   `GameInput`, interface só pelo `UiBuilder`/`Theme` (nada de `OnGUI`). Os princípios completos
   estão no ARQUITETURA.md.
6. **Testes (TDD):** feature nova com regra começa por um teste que falha; bug corrigido ganha
   um teste que reproduz o bug. A regra fica numa classe C# pura (ex.: `LobbyRules`) e o
   MonoBehaviour/RPC só chama ela. Testes em `Assets/Tests/EditMode` (NUnit). Se um teste mostrar
   que o código não faz o que os .md dizem, avisar antes de decidir quem está certo.
7. **Arte:** modelos são **gerados por script** a partir das medidas do Art Bible, nunca
   editados à mão no Blender. Mudou uma medida → ajustar o script e gerar de novo.

## Como verificar (receitas)

**Gerar os modelos** (Blender em segundo plano, na raiz do repositório):
```
.\tools\blender\build_all.ps1
```
Cada script imprime `TRIS` e `SAVED`; as prévias ficam em `Art/Source/<Tipo>/<Nome>/previews/`
e devem ser abertas e comparadas com o Art Bible.

**Compilar e rodar um construtor no Unity em batch** (Unity fechado):
```
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\david\gamedev\mudancaassombrada\MudancaAssombrada" -executeMethod MudancaAssombrada.Editor.LevelPrefabsBuilder.EnsureMenu -quit -logFile <scratchpad>\unity.log
```
- Erros de compilação aparecem como `error CS` no log; `Exiting batchmode successfully` = ok.
- `-executeMethod` aceita métodos estáticos privados (ex.: os de `[MenuItem]`).
- Para medir algo (orientação, tamanhos, se um prefab tem o `Modelo`), criar um script
  temporário `Assets/Editor/Temp*.cs`, rodar em batch e **apagar** depois (com o `.meta`).

**Rodar os testes:** com o Unity aberto, Window › General › Test Runner › EditMode › Run All.
Fechado, em batch (o `results.xml` lista cada teste; código de saída 0 = todos passaram):
```
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -projectPath <projeto> -runTests -testPlatform EditMode -testResults <scratchpad>\results.xml -logFile <scratchpad>\tests.log
```

**Testar em rede:** Multiplayer Play Mode (Window › Multiplayer › Multiplayer Play Mode, Player 2):
uma janela cria a sala, a outra entra pela lista de salas ou pelo código. Não existe mais LAN:
tudo passa pelos serviços da Unity (Lobby + Relay), então precisa de internet. O jogo começa pela cena **Menu**.

## Armadilhas conhecidas

- **Frente dos modelos:** o −Y do Blender chega como −Z no Unity. O `export_fbx` do
  `tools/blender/blockout_common.py` gira as raízes 180° só durante a exportação para os
  modelos olharem para +Z (a frente do Player). Mexer no export = medir de novo no Unity.
- **Importação pela pasta:** `Assets/Editor/ArtImportSettings.cs` define o rig pela pasta de
  `Assets/Art` (Humanoid ou sem rig) e só age na **primeira** importação. FBX já importado com
  configuração errada → corrigir o `.meta` (ou reimportar apagando o `.meta`).
- **Construtores:** o Player, o HUD e a Pausa são sempre recriados pelo código; os prefabs do
  mundo só são criados se faltarem, e depois são de quem edita. A troca bloco → modelo
  (`ApplyArt` no `LevelPrefabsBuilder`) só mexe no visual e não repete se já existe o filho `Modelo`.
- **Falsas mudanças no Git:** `Assets/Editor/StableAssets.cs` evita que recriar cena/prefab troque
  todos os `fileID`. Mesmo assim, conferir o diff antes de propor commit.
- **Clipe feito no Blender sobre um rig nosso** (ex.: `PegaBengala`): o FBX só tem o esqueleto já
  posado, então precisa do avatar do personagem (`OwnRigClips` no `ArtImportSettings`: Copy From Other
  + Bake Axis Conversion) e de um vazio ao lado do rig (sozinho na raiz, o Unity achata a hierarquia).
  Mudou a regra? Apagar o `.meta` do clipe para reimportar.
- **Medir animação em teste (batch):** sem câmera, o Animator não mexe os ossos (culling): usar
  `cullingMode = AlwaysAnimate`. IK depois da animação vai no `LateUpdate` com a pose real; o
  `GetIKPosition` do Animator pode não coincidir com a mão (ex.: meio do Levantar).
- **Normais da malha:** o Unity só desenha a frente da face. As prévias do Blender escondem o verso
  (`show_backface_culling`); peça montada vértice a vértice precisa de `recalc_face_normals`.
- **Lados nos desenhos:** em várias fichas do Art Bible, o texto diz "direito" e o desenho mostra o
  lado esquerdo (bengala da Cida, lápis e bolsa do Tiago). Os modelos seguem o desenho.
- **NavMesh do fantasma (raio 0,5 m, altura 2 m):** vão com menos de 1 m ele não atravessa; a colisão
  de cima da porta precisa de folga (10 cm acima do vão; no 1º andar o arredondamento fecha); pé de
  escada estreito isola (usar `NavMeshLink`). Conferir rotas com `NavMesh.CalculatePath` num script temporário.
  Porta simples de 1,20 m fecha na resolução padrão do bake: a Mansão usa voxel de 5 cm (`FineNavMesh`).
  `NavMeshObstacle` ligado no bake grava o buraco para sempre: o `LevelSceneTools` desliga os obstáculos
  só durante o bake. Num script temporário, eles recortam o NavMesh também no editor (desligar e
  `RemoveData`/`AddData` para medir o mapa salvo). Fora do Play, `GhostHideout.All` fica vazio.
- **Fotos de cena em batch:** sem `-nographics`, dá para renderizar a cena num RenderTexture. Desligar a
  névoa da noite, senão a câmera de longe pinta tudo de bege. A pasta `Temp` é apagada ao sair (gravar em `Logs`).
  A 1ª foto sai cinza, sem materiais: renderizar um quadro de aquecimento antes.
- **Luz ambiente:** trocar `RenderSettings.ambientLight` não basta no URP; sem `DynamicGI.UpdateEnvironment()`
  a cena fica com o azul padrão do céu (a `NightLighting` já faz).
- **Art Bible offline:** o PDF não abre aqui (falta o poppler). O `.html` salvo é um pacote: cada página
  vem comprimida (gzip + base64) no `__bundler/manifest`, e as tabelas ficam no `renderVals()` do template.
- **PowerShell:** dentro de aspas duplas, `$nome_texto` vira variável; usar aspas simples ou
  `${nome}` quando houver `$` no texto. `2>&1` em programas nativos gera falsos erros vermelhos.

## Como manter este arquivo

- Atualizar quando mudar **uma regra de trabalho**, **um caminho/ferramenta** ou quando uma
  armadilha nova custar tempo (registrar em "Armadilhas conhecidas", em uma ou duas linhas).
- O que é **estado do projeto** (fase feita, modelo novo, decisão tomada) vai para o
  PLANEJAMENTO.md / ART_BIBLE.md, não para cá.
- Manter curto: se uma seção passar de ~15 linhas, ela provavelmente pertence a outro documento.
- Mudanças neste arquivo vão no mesmo commit da tarefa que as motivou.
