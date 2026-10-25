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
├── build_all.ps1                      gera todos
└── check_fbx.py                       reimporta um FBX e lista o que veio
Art/Source/Characters/<Nome>/          ← arquivos-fonte (fora do Unity)
├── SK_CHR_<Nome>_Blockout.blend
└── previews/                          frente, perfil, costas e 3/4 (para comparar com o Art Bible)
MudancaAssombrada/Assets/Art/Characters/<Nome>/
└── SK_CHR_<Nome>.fbx                  ← o que o jogo usa
```

**Gerar de novo:** na raiz do repositório, com o Blender 5.2 instalado:

```
.\tools\blender\build_all.ps1
```

Depois, no Unity: **Mudança Assombrada › Criar Prefabs que Faltam** (recria o Player e
liga os modelos nas fichas que estiverem vazias).

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
| Fantasma de fase | 20k | — |
| O Inquilino (forma final) | 45k | — |
| Móvel carregável grande | 3–6k | — |
| Prop de história | 2–4k | — |

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

Hoje só os três jogáveis têm modelo. Cenário, móveis, caminhão e fantasma ainda são blocos.

| Design (prancha do Art Bible) | No jogo hoje | Falta |
|---|---|---|
| Cida, Tiago, Marcelo | blockout + rig + ficha | animações (bengala, Libras, carregar), roda de gestos e quadro do Marcelo, colisor por altura |
| Bibliotecário (GHO_03) | cápsula + `GhostHunter` (caça por som) | modelo; orelhas que abrem, distração por livros, captura |
| Móveis do protótipo | cubos com peso | modelos e pontos de pega |
| Caminhão | cubos + zona de entrega | modelo |
| Props de história (rádio, bilhetes, relíquias) | bilhetes como texto nos capítulos | modelos; relíquias que pesam mais longe da porta |
| Casa do Espelho + Imitador (cap. 1) | ficha do capítulo | cena, kit modular, modelo; **disfarce** (copia um jogador) |
| Conservatório + Maestrina (cap. 2) | ficha do capítulo | cena, instrumentos, modelo; **hipnose** pela música |
| Biblioteca do Silêncio (cap. 3) | ficha do capítulo | cena, estantes, piso que range |
| Seu Valdemar | bilhetes e avaliação nos capítulos | modelo; aparições por estado |
| O Inquilino + Sobrado (cap. 4) | ficha do capítulo | cena, chefe em 3 etapas, final |

Ordem sugerida: Bibliotecário e móveis → caminhão e caixas → Casa do Espelho e Imitador →
Conservatório e Maestrina → Biblioteca → Valdemar, Inquilino e Sobrado.

## 4. Decisões em aberto

- **Acabamento do mundo 3D.** O [DESIGN.md](DESIGN.md#7-mundo-3d) pede low-poly com cores
  chapadas e pouca textura; o Art Bible propõe um visual estilizado com material PBR
  (rampa de luz em 3 tons, rim light) e texturas `_BC`/`_N`/`_MSAO`. Os blockouts atuais
  seguem o DESIGN.md. Decidir antes da arte final.
- **Colisor por personagem.** O CharacterController tem 2 m para todos; a Cida (1,52 m)
  não passa onde caberia.
