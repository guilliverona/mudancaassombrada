# Art Bible — Mudança Assombrada

> **Carisma pela forma, terror pela distorção, leitura pelos sentidos.**
> A equipe é redonda, quente e gasta; os fantasmas são altos, frios e "errados".
> Tudo precisa funcionar também sem textura: é isso que o eco da Cida mostra.

Este arquivo é o **resumo** do Art Bible para o dia a dia: o que um modelo precisa ter
para entrar no jogo e onde cada coisa fica. O Art Bible completo (fichas com vistas,
cabeça e mãos, fantasmas, cenários e props) é a referência principal:

**[Art Bible completo](https://claude.ai/artifact/QRYrVZE3fR6YV9xz1jgzkX)**
Cópia em PDF: [LINK](https://drive.google.com/drive/folders/1PjxH6G6GQJBmZjVnji4HFQb9Co_kUkIT?usp=sharing).

Como o código está organizado: [ARQUITETURA.md](ARQUITETURA.md).
Padrão visual da interface: [DESIGN.md](DESIGN.md).
Ordem de implementação dos designs, em fases: [PLANEJAMENTO.md](PLANEJAMENTO.md).

---

## 1. Pipeline Blender → Unity

Os modelos são gerados por **scripts** a partir das medidas do Art Bible, não modelados à mão.
Mudou uma medida? Ajuste o script e gere de novo.

```
tools/blender/                         ← scripts (Python do Blender)
├── blockout_common.py                 base: primitivas, rig Humanoid, sockets, export
├── build_cida_blockout.py             um script por personagem
├── build_tiago_blockout.py
├── build_marcelo_blockout.py
├── build_bibliotecario_blockout.py    fantasmas (um script cada)
├── build_imitador_blockout.py
├── build_maestrina_blockout.py
├── build_valdemar_blockout.py         o chefe (Bosses)
├── build_props_blockout.py            armário, cadeira, caixa e caminhão
├── build_story_props_blockout.py      rádio, bilhete, nota fiscal, relíquias, caixinha
├── build_furniture_blockout.py        mobília do Art Bible e props da biblioteca
├── build_env_kit_blockout.py          kit modular: paredes, porta, pisos, molduras
├── build_all.ps1                      gera todos
└── check_fbx.py                       reimporta um FBX e lista o que veio
Art/Source/<Tipo>/<Nome>/              ← arquivos-fonte (fora do Unity)
├── <PREFIXO>_<Nome>_Blockout.blend
└── previews/                          frente, perfil, costas e 3/4 (para comparar com o Art Bible)
MudancaAssombrada/Assets/Art/<Tipo>/<Nome>/
└── <PREFIXO>_<Nome>.fbx               ← o que o jogo usa
```

| Tipo (pasta) | Prefixo | Rig no Unity |
|---|---|---|
| `Characters` | `SK_CHR` | Humanoid |
| `Ghosts` | `SK_GHO` | Humanoid |
| `Bosses` | `SK_BOS` | Humanoid |
| `Props` | `SM_PRP` | sem rig |
| `Environment` | `SM_ENV` | sem rig |

Os scripts de props, mobília e cenário aceitam nomes depois de `--` para gerar só alguns:
`blender --background --factory-startup --python tools/blender/build_furniture_blockout.py -- Sofa`.

A importação é configurada sozinha pela pasta
([`ArtImportSettings`](MudancaAssombrada/Assets/Editor/ArtImportSettings.cs)): ninguém precisa
mexer no Rig do Inspector. Vale só na primeira importação; depois, o Inspector manda.

**Gerar de novo:** na raiz do repositório, com o Blender 5.2 instalado:

```
.\tools\blender\build_all.ps1
```

Depois, no Unity: **Mudança Assombrada › Criar Prefabs que Faltam**. Ele recria o Player,
liga os modelos nas fichas que estiverem vazias e **troca o bloco pelo modelo** nos prefabs do
mundo que já têm arte (móveis, caminhão, fantasma): o bloco vira só a colisão e o modelo entra
como filho `Modelo`. Peso, velocidades e tamanho ajustados no Inspector continuam. Depois disso,
gerar de novo no Blender atualiza os prefabs sozinho, porque o `Modelo` fica ligado ao FBX.

### Regras de modelagem

| Regra | Valor |
|---|---|
| Escala | Metric, Unit Scale 1.0 · **1 BU = 1 m** |
| Frente | o personagem olha para **−Y** no Blender e precisa olhar para **+Z** no Unity (a frente do Player) |
| Origem | pés no chão (0,0,0); props no ponto de pega |
| Export FBX | Apply Scalings: FBX All · Forward −Z · Up Y · sem leaf bones · **raízes giradas 180° em Z só durante o export** (sem isso, o −Y do Blender chega como −Z no Unity e o personagem anda de costas; o `export_fbx` do `blockout_common.py` já faz) |
| Import no Unity | Rig **Humanoid** (Create From This Model) · Bake Axis Conversion · sem câmeras, luzes e animação |
| Ossos | nomes do Unity Humanoid: `Hips`, `Spine`, `Chest`, `UpperChest`, `Neck`, `Head`, `LeftUpperArm`... (o Unity mapeia sozinho) |
| Altura | a altura escrita na ficha do Art Bible manda; o script escala o desenho até ela |

### Nomes

| Prefixo | Exemplo | O que é |
|---|---|---|
| `SK_` | `SK_CHR_Cida` | malha com esqueleto |
| `SM_` | `SM_CHR_Cida_Oculos` | malha estática |
| `PRP_` | `SM_PRP_Bengala` | prop (objeto da cena ou de mão) |
| `MAT_` | `MAT_CHR_Cida_Pele` | material |
| `T_` | `T_CHR_Cida_Roupa_BC` · `_N` · `_MSAO` · `_E` | textura (cor, normal, metal/AO/suavidade, emissão) |
| `SOC_` | `SOC_Hand_L` | socket: ponto de encaixe preso a um osso |
| `UCX_` | `UCX_SM_Sofa_01` | colisão convexa |

### Orçamento (triângulos)

| Tipo | Limite | Hoje |
|---|---|---|
| Personagem jogável (com props) | 18k | Cida 10,2k · Tiago 8,5k · Marcelo 7,4k |
| Fantasma de fase | 20k | Bibliotecário 6,0k · Imitador 6,3k · Maestrina 4,2k |
| Seu Valdemar | 15k | 4,8k |
| O Inquilino (forma final) | 45k | — |
| Móvel carregável grande | 3–6k | 0,4k a 2,6k (penteadeira) |
| Prop de história | 2–4k | 0,1k a 1,3k (partitura) |
| Módulo de cenário | 0,5–1,5k | 0,2k a 0,8k (piso xadrez) |

Os blockouts ficam abaixo do orçamento de propósito: são formas e cores, sem o detalhe da arte final.

## 2. Personagens jogáveis

O modelo entra no jogo pela **ficha** do personagem (`Assets/Data/Personagens`):
campo **Modelo** (o FBX) e **Altura dos olhos** (onde fica a câmera). O `CharacterSwitcher`
cria o modelo no Player; ficha sem modelo continua com o boneco de blocos colorido.
Personagem novo com modelo = ficha nova, sem mexer em código.

| | Dona Cida | Tiago | Marcelo "Marreta" |
|---|---|---|---|
| Altura | 1,52 m | 1,74 m | 1,98 m |
| Olhos (câmera) | 1,09 m | 1,52 m | 1,74 m |
| Forma | círculo | triângulo | quadrado |
| Cor (ficha) | roxo | amarelo | verde |
| Malhas separadas | cabelo, óculos, bengala | cabelo, boné, bloquinho, lápis | barba, gorro, luvas, quadro branco |
| Sockets | `SOC_Hand_L` (bengala), `SOC_Hand_R`, `SOC_Bun`, `SOC_CaneTip` | `SOC_Hand_L/R`, `SOC_Neck` (bloquinho), `SOC_Ear_L` (lápis), `SOC_Hip_L` (bolsa) | `SOC_Hand_L/R`, `SOC_Carry_A/B`, `SOC_Chest` (quadro), `SOC_Mouth` (apito) |
| Ossos extras | `bun_jiggle`, `skirt_F/B/L/R` | `hood_01`, `notebook_lanyard`, `bag` | `beard_01`, `board_hinge`, `whistle_cord`, `tape_roll`, `belly_jiggle` |

**Lados:** nos desenhos do Art Bible, a bengala da Cida, o lápis e a bolsa do Tiago
ficam do lado **esquerdo** do personagem; o texto de algumas fichas diz "direito".
Os modelos seguem os desenhos.

## 3. Situação: o que já existe e o que falta

Já têm modelo (blockout): os três jogáveis, os três fantasmas de fase, o Seu Valdemar, todos os
props de história, a mobília da tabela do Art Bible, o caminhão e o kit modular de cenário.
Falta modelo só para o **Inquilino** (3 formas + epílogo) e os instrumentos da Fase 2.

**Modelo existe ≠ está no jogo.** Só os móveis do protótipo, o caminhão e o Bibliotecário estão
ligados a prefabs (`LevelPrefabsBuilder`). Os demais esperam o prefab da fase em que entram.

| Design (prancha do Art Bible) | No jogo hoje | Falta |
|---|---|---|
| Cida, Tiago, Marcelo | blockout + rig + ficha | animações (bengala, Libras, carregar), roda de gestos e quadro do Marcelo, colisor por altura |
| Bibliotecário (GHO_03) | blockout 2,6 m + rig + `GhostHunter` (caça por som) | orelhas que abrem, distração por livros, captura, postura curvada (animação) |
| Imitador (GHO_01) | blockout 2,3 m + rig (`Ghosts/Imitador`) | prefab; **disfarce** (copia um jogador), máscara comédia → tragédia |
| Maestrina (GHO_02) | blockout 2,4 m + rig (`Ghosts/Maestrina`) | prefab; **hipnose** pela música, pescoço que estica |
| Seu Valdemar (BOS_00) | blockout 1,68 m + rig (`Bosses/Valdemar`) | aparições por estado (rádio → translúcido → sólido) |
| O Inquilino (BOS_01) | ficha do capítulo | **modelo** (3 etapas + epílogo), chefe em 3 etapas, final |
| Móveis do protótipo | armário, cadeira e caixa com modelo, no prefab | pontos de pega |
| Mobília do Art Bible | penteadeira, piano de armário, estante, mesa de leitura, sofá, caixas P/G (`Props/`) | prefabs com o peso da tabela |
| Props de história | rádio, bilhete, nota fiscal, cartaz, partitura, diário, caixinha (`Props/`) | relíquias que pesam mais longe da porta; texto real nos bilhetes (decal) |
| Biblioteca (ENV_03) | estante, pilha de livros, luminária verde, piso de tábuas | cena; piso que range |
| Kit modular (ENV_01–04) | parede lisa, parede com porta, porta, piso xadrez, molduras de espelho S/M/L (`Environment/`) | cenas das casas; instrumentos do Conservatório |
| Caminhão | modelo + zona de entrega | — |

Ordem sugerida: ~~Bibliotecário, móveis e caminhão~~ (feito) → ~~modelos dos fantasmas, chefe,
props e kit~~ (feito) → Casa do Espelho e Imitador → Conservatório e Maestrina → Biblioteca →
Inquilino e Sobrado.

## 4. Decisões em aberto

- **Acabamento do mundo 3D.** O [DESIGN.md](DESIGN.md#7-mundo-3d) pede low-poly com cores
  chapadas e pouca textura; o Art Bible propõe um visual estilizado com material PBR
  (rampa de luz em 3 tons, rim light) e texturas `_BC`/`_N`/`_MSAO`. Os blockouts atuais
  seguem o DESIGN.md. Decidir antes da arte final.
- **Colisor por personagem.** O CharacterController tem 2 m para todos; a Cida (1,52 m)
  não passa onde caberia.
