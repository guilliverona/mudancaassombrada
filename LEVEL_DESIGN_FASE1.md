# Level design — Fase 1: A Casa do Espelho (Casarão Taborda)

Resposta ao [LEVEL_DESIGN_FASE1_BRIEFING.md](LEVEL_DESIGN_FASE1_BRIEFING.md).
Documento de **direção de arte + direção de jogo + roteiro** da Fase 1, escrito para ser
implementado direto no repositório: toda medida está no grid de 2 m, todo som tem id de
catálogo e toda mecânica aponta para o arquivo onde ela nasce.

Fontes: [roteiro.txt](roteiro.txt), Art Bible completo (ENV_01, GHO_01, CHR_01–03, PRP),
[ART_BIBLE.md](ART_BIBLE.md), [ARQUITETURA.md](ARQUITETURA.md), [DESIGN.md](DESIGN.md),
[PLANEJAMENTO.md](PLANEJAMENTO.md) e os scripts em `MudancaAssombrada/Assets` (lidos em 26/10/2026).

> **Como ler este documento.** As seções 1 a 5 são a **intenção** (o que a fase quer ser).
> As seções 6 a 12 são a **especificação** (o que construir). A seção 13 é a **ordem de
> implementação**, em lotes que podem ser feitos por pessoas diferentes.

---

## 1. A tese da fase em uma frase

> **A casa é escura, barulhenta e cheia de espelhos — e a única pessoa que não precisa de
> nada disso é a cega.**

A Fase 1 é a fase da Dona Cida. Isso não pode ser só "ela tem uma dica a mais": o espaço
inteiro precisa ser montado para que **ser a Cida seja a melhor cadeira da mesa** por cinco
minutos, e para que os outros dois sintam que dependem dela — sem virarem figurantes.

Três contrastes guiam todas as decisões abaixo:

| Terror | Carisma |
|---|---|
| Um corredor de 16 m com espelhos, mais longo que o eco dela (12 m) | Um manequim de figurino usando o colete da empresa |
| A cópia que pede "segura a outra ponta" no meio da escada | Bilhetes do chefe colados nos móveis: "NÃO ARRANHAR" |
| O camarim do fundo, o único cômodo em silêncio absoluto | O Imitador fazendo reverência depois de imitar bem |
| Desmascarado, a máscara gira para tragédia | …e ele sai de cena **ofendido**, batendo a cortina |

---

## 2. O que muda em relação ao rascunho atual

| | Hoje (`MirrorHouseBuilder`) | Proposta |
|---|---|---|
| Planta | 12 × 12 m, térreo, 3 cômodos | 16 × 12 m térreo + 16 × 6 m 1º andar, 9 cômodos, **dois anéis** |
| Noite | 180 s | **300 s** (âmbar aos 225 s) |
| Objetivo | 1 etapa, "entregar todos" | **3 etapas**: térreo → camarins → o cartaz |
| Móveis | 5, todos de 1 pessoa | 8 entregas, **3 delas em dupla** |
| Espelhos | 5, 2 "herói" | 9, 2 "herói" na parede que a escada reflete |
| Vantagem da Cida | só a ausência de passos/respiração | **8 vantagens** concretas (§4) |
| Fraqueza da Cida | não existe no jogo | **3 fontes de ruído** que ela — e só ela — desliga |
| História na casa | 1 cartaz opcional | a família Taborda contada em 11 objetos; o cartaz é **objetivo** |
| Comédia | nenhuma no espaço | bilhetes, manequins, o piano, a reverência, o gingado |

**O que NÃO entra nesta fase** (para o escopo não estourar): mundo do espelho jogável,
relíquia que pesa mais longe da porta (é da Fase 6), chat de voz, sótão, 2º andar completo.

---

## 3. O roteiro expandido — a família Taborda

O roteiro original dá 6 linhas para a fase. Esta é a expansão; o que já existia está preservado.

### 3.1 Quem morava aqui

**Taborda & Companhia — Grande Elenco** foi uma companhia de teatro de revista dos anos 1920.
O casarão da rua era a casa e o ensaio deles: a Sala de Ensaio tem palco, a casa tem camarins
no andar de cima e espelho em toda parede, porque **ator ensaia olhando**.

- **Dona Herondina Taborda**, a matriarca, primeira-atriz. Os retratos do hall são todos dela.
- **Olegário Taborda**, o irmão mais novo. Nunca foi protagonista: era o **suplente**, o que
  decorava todos os papéis e só subia quando alguém faltava. O público nunca soube que era ele.
- **Valdemar** entrou como contrarregra, aprendeu a atuar e, em 1927, virou nome de cartaz.
  Olegário continuou suplente — inclusive dele.

Quando a companhia acabou, Olegário ficou na casa, sozinho, **ensaiando todos os papéis para
ninguém**, na frente dos espelhos. Morreu lá e virou exatamente o que sempre tinha sido: a cópia.

### 3.2 Por que isso faz o fantasma funcionar

Tudo o que o Imitador faz passa a ter um motivo, e o motivo é engraçado e triste ao mesmo tempo:

- **Copia** porque é a única coisa que sabe fazer.
- **Faz reverência** depois de imitar bem porque está **se apresentando**, não atacando.
- **Sai ofendido** quando desmascarado porque é a reação de um ator vaiado, não de um monstro.
- **Puxa para dentro do espelho** porque é onde ele mora: do outro lado é onde ele sempre esteve.
- **Não respira** porque nunca teve fala. E atores respiram. É isso que a Cida percebe.

### 3.3 A pista do chefe

No **camarim do suplente** (o menor, no fim do corredor), atrás do espelho rachado, está o
**cartaz de 1927**: "GRANDE ELENCO · **VALDEMAR** · …" em letra grande e, no rodapé, minúsculo,
"O. Taborda (suplente)".

**No verso, a lápis, em letra espelhada** — só legível no reflexo:

> *"Valdemar — guarde o meu lugar. Eu volto no segundo ato. — O."*

Ele nunca voltou. O Valdemar guardou o lugar e agora manda uma equipe buscar o cartaz.
É o mesmo padrão da Fase 2 ("Valdemar nunca veio me buscar"): **o chefe deixa as pessoas
esperando**, e isso vai se acumulando até a Fase Final.

### 3.4 Bilhete de abertura (substitui o atual)

Ficha `Assets/Data/Capítulos/1 - A Casa do Espelho.asset`, campo `openingNote`:

```
TRANSPORTES DESCANSO ETERNO LTDA.
ORDEM DE SERVIÇO Nº 014 — CASARÃO TABORDA

Casa de família de teatro. Cuidado com os espelhos: são muitos e são velhos.
Se alguém da equipe pedir ajuda com um móvel, confiram se a pessoa respira.

Tem um cartaz antigo no camarim do fundo. Esse vem junto.
Não está na lista. Vem junto.

Não arranhem o piano.
                                                              — V.
```

Três funções numa página: ensina a mecânica ("confiram se respira"), marca o objetivo
escondido ("o cartaz vem junto") e abre a rachadura do mistério — por que uma transportadora
quer um cartaz de 1927? A repetição "Não está na lista. Vem junto." é o chefe se traindo.

### 3.5 Ficha de avaliação (campo `ratingComments`)

| Personagem | Comentário do Seu Valdemar |
|---|---|
| Dona Cida | "A senhora ouviu o que ninguém ouviu. Já trabalhei com gente assim. — V." |
| Tiago | "O senhor leu uma coisa que não era para ler. Tudo bem. — V." |
| Marcelo | "Obrigado por não deixar ninguém segurar a outra ponta sozinho. — V." |

E, se o cartaz **não** for entregue (marcador de save ausente), o bilhete de fim de fase vira:

> "Faltou o cartaz. Volte quando puder. — V."

---

## 4. As oito vantagens da Dona Cida

Esta é a parte central do pedido. Cada vantagem tem a mecânica que a entrega (§8) e o custo
que a equilibra. A regra: **nenhuma delas é "ela vê melhor"; todas são "ela sabe e ela faz".**

| # | Vantagem | Como aparece no jogo | Custo / equilíbrio |
|---|---|---|---|
| V1 | **Não precisa de luz** | Há só **2 lanternas** no caminhão para 3 pessoas. A dica dela ao mirar uma lanterna é "Não preciso." | Os outros dois precisam decidir quem fica sem |
| V2 | **Enxerga a 12 m; os outros precisam de 2,5 m** | A onda da bengala cobre o hall inteiro (12 m = o comprimento do hall, de propósito). Apontar o falso exige 2,5 m | A onda é um som: o fantasma sabe onde ela está |
| V3 | **Sabe em qual espelho o fantasma está** | **Diapasão** (MEC-01): o espelho ocupado responde com zumbido rachado em vez de tom limpo | 8 s de recarga e 15 m de barulho |
| V4 | **Deixa a casa melhor para todo mundo** | **Afinar** as 3 fontes de ruído (MEC-05): o eco cresce de 4 para 12 m e a casa fica silenciosa — e numa casa silenciosa a ausência de respiração é óbvia **para os três** | Afinar a arandela do corredor **apaga a luz do corredor** |
| V5 | **É o GPS da equipe** | **Chamada** (MEC-06): segurar a bengala 2 s marca o chão por 6 s para os três (anel para o Tiago, pulso para o Marcelo) | 20 m de alcance sonoro |
| V6 | **Ouve pelo chão** | Tábuas que rangem (MEC-11) dão a posição dos colegas **através do piso** — ela sabe quem está na galeria sem subir | Também denuncia a própria Cida |
| V7 | **Não pode ser enganada por reflexo nenhum** | No modo cego todos os espelhos são vidro escuro. Toda a armadilha visual do Imitador (reflexo atrasado, logo espelhado, cópia perfeita) simplesmente não existe para ela | Em compensação, **manequins** (MEC-10) enganam o eco dela |
| V8 | **Nunca se perde** | Cada cômodo tem uma **âncora sonora** própria (§7.4): relógio, gramofone, torneira, lustre, cabides. Ela navega a casa inteira sem bater em nada | O camarim do suplente **não tem âncora**: o único lugar onde ela fica sem referência |

### 4.1 E as fraquezas, para a vantagem ter preço

1. **Ruído borra o eco.** `NoiseLevel` 0→1 encolhe o raio de 12 → 4 m e engrossa a borda
   (é o `_NoiseLevel` da prancha CHR_01 do Art Bible, agora de verdade).
2. **Correr e agachar.** O passo é a visão dela: agachada enxerga 1 m, correndo enxerga 7 m
   mas é ouvida a 10,5 m pelo fantasma. Já é assim no `CaneTapper`; está documentado aqui porque
   o level design depende disso (corredores onde correr é tentador e caro).
3. **Manequins.** Sem passos e sem respiração, igual ao Imitador. A diferença: não se movem, e
   o diapasão faz madeira soar diferente de vidro. Ela precisa **aprender** a diferença.
4. **O diapasão a entrega.** 15 m de som, 8 s de recarga.

### 4.2 O que o Tiago e o Marcelo fazem numa fase que é da Cida

A fase não funciona se eles virarem carregadores. Cada um tem **um sinal exclusivo** e **um
trabalho exclusivo**:

| | Sinal exclusivo do falso | Trabalho exclusivo |
|---|---|---|
| **Cida** | sem passos, sem respiração (12 m, atravessa parede) | afinar o ruído; auditar espelhos; guiar |
| **Tiago** | as **bordas tremem** como superfície de espelho (visão, linha de visada) | **ler o verso do cartaz no reflexo** (a Cida não vê reflexo; o Marcelo não lê espelhado rápido o suficiente — é o olho treinado dele); ser o corredor entre os andares |
| **Marcelo** | o **peso não alivia** quando a cópia "segura a outra ponta" (MEC-08) e ela não copia o **gingado** dele carregando | ser a âncora da dupla; único que carrega o armário sozinho |

Os três sinais são **o mesmo fato** (não é uma pessoa) lido por três sentidos. É o pilar
"leitura pelos sentidos" do Art Bible aplicado a um único acontecimento.

---

## 5. Ritmo da fase (beat chart) — 300 s

A noite passa de 180 s para **300 s**. Justificativa: a casa dobrou de tamanho, ganhou um andar
e três móveis de dupla; em 180 s a fase vira corrida e a Cida nunca tem tempo de usar nada do
que foi desenhado para ela. A luz continua virando âmbar a 75% da noite → **aos 225 s**.

> Ficha do capítulo: `nightDuration = 300`. Primeiro playtest deve conferir se sobram 30–60 s
> para três jogadores que já conhecem a casa; se sobrar muito mais, cortar para 270 s.

| Tempo | Beat | Dona Cida | Tiago | Marcelo |
|---|---|---|---|---|
| −0:15 | **O rádio lê o bilhete** no alpendre | aviso privado: *"a voz não respira"* | lê o bilhete de papel (o rádio não existe para ele) | ouve e dá de ombros |
| 0:00 | Começa a noite. O **relógio do hall** assume o compasso (1 tique/s) | a bengala cobre o hall ponta a ponta | — | pega a primeira caixa |
| 0:00–0:45 | **Térreo**: cadeiras e caixas. Gramofone travado na Sala de Ensaio | eco borrado a oeste | vê as ondas do gramofone | carrega sozinho |
| ~0:20 | **1ª afinação possível** (gramofone) | "Afinar" (2 s) → o eco volta a 8 m | vê o gramofone parar de tremer | ouve a casa aquietar |
| 0:45 | **A reverência.** O Imitador sai de E2, atravessa o hall devagar, faz uma reverência teatral e volta para o espelho. **Não ataca.** | uma figura **sem passos** no eco | bordas tremendo | uma máscara de porcelana |
| 0:45–2:00 | Imitador ativo: patrulha os espelhos, disfarça o jogador mais isolado, primeira isca | audita espelhos com o diapasão | confere as bordas | confere o gingado |
| ~1:10 | **O piano toca um acorde sozinho** quando o primeiro móvel cruza o hall (liga a 2ª fonte de ruído) | eco borrado no hall | — | susto cômico |
| 2:00 | **Etapa 0 cumprida** → "Esvaziar os camarins". A escada abre | — | sobe primeiro | — |
| 2:00–3:15 | **A escada.** Armário e penteadeira em dupla, descendo na frente dos dois espelhos-herói: a equipe se vê descendo | avisa de baixo quem está na galeria | lê o verso do cartaz no reflexo | segura a ponta pesada |
| ~2:30 | **A isca na escada**: a cópia pede a outra ponta no meio do lance | "ele não respira!" (a 12 m) | "a borda treme" | "o peso não aliviou" |
| 3:15 | **O camarim do suplente**: silêncio absoluto, espelho rachado | diapasão → zumbido rachado: *ele está aí* | vê o texto espelhado | tira o cartaz de trás do espelho |
| 3:45 | **A luz vira âmbar** (75%) | — | — | — |
| 3:45–4:40 | A corrida final. O Imitador disfarça com metade do tempo de espera | **chamada** (3 batidas) reagrupa a equipe | corre | carrega dois de uma vez |
| ~4:40 | **A última reverência.** Desmascarado, a máscara gira para tragédia e ele sai pela cortina do espelho | ouve o veludo | vê a máscara girar | — |
| 5:00 | **Amanhecer.** Galo, luz âmbar, avaliação do Seu Valdemar com um comentário por personagem | | | |

---

## 6. Planta no grid de 2 m

Grid de 2 m. Origem no **centro do térreo**. **+Z = fundo da casa (norte)**, **+X = leste**,
**−Z = rua**. Pé-direito 2,80 m; laje 0,30 m; **piso do 1º andar em Y = +3,10 m**.

Colunas **A–H** (X de −8 a +8) e linhas **1–6** (Z de −6 a +6):

```
col     A       B       C       D       E       F       G       H
X   -8..-6  -6..-4  -4..-2  -2..0   0..+2  +2..+4  +4..+6  +6..+8
```

```
lin  Z

 6  +4..+6   ┌──────────────── CORREDOR DOS ESPELHOS (16 × 2) ──────────────┐
             │  ◧E5a       ◧E5b     [porta dupla]      ◧E5c    arandela ✺   │
             ├────────┬───────────────┬──────────────┬──────────────────────┤
 5  +2..+4   │        │               │              │                      │
             │  COPA  │   HALL (norte, 2,80, sob a GALERIA)  ⏱ relógio      │
 4   0..+2   │ (6×4)  │   piano ▯     │              │   SALA DE JANTAR     │
             ├────────┴──┬────────────┴──────────────┤      (4 × 10)        │
 3  -2..0    │           │                           │                      │
             │  SALA DE  │ ◧E2  HALL (sul)           │   lustre ✦           │
 2  -4..-2   │  ENSAIO   │      pé-direito 5,90      │                      │
             │  (6 × 6)  │      (duplo)              │   mesa de 9 lugares  │
 1  -6..-4   │  palco ▭  │ ◧E1           ESCADA ↗ (X +2,4..+4,0, Z -5..0)  │
             └───────────┴──────[ porta dupla ]──────┴──────────────────────┘
                                  ALPENDRE (4 × 2, Z -8..-6)
                                  nascimento ×3
                                        ⇓ 4 m de calçada
                                  CAMINHÃO  (0, -12)  ·  zona 4 × 4
```

**1º andar** (Y = +3,10), só sobre a metade do fundo (Z 0..+6):

```
lin  Z

 6  +4..+6   ┌──────────────┬──────────────────────┬──────────────────────┐
             │  CAMARIM     │ CORREDOR DOS CAMARINS│  CAMARIM DO          │
             │  GRANDE      │      (6 × 2)         │  SUPLENTE            │
 5  +2..+4   │  (6 × 6)     ├──────────────────────┤  (4 × 6)             │
             │              │                      │                      │
             │  E6 ◧        │      GALERIA         │  E7 ◧ (rachado)      │
 4   0..+2   │  armário ▯   │      (6 × 4)         │  cartaz ▤            │
             │  penteadeira │                      │                      │
             └──────────────┴───[peitoril]──┬───────┴──────────────────────┘
                   (debruça sobre o HALL sul)│ ESCADA chega aqui (Z = 0)
```

### 6.1 Por que esta planta

- **Dois anéis no térreo.** Hall → oeste (Ensaio → Copa) → Corredor → leste (Jantar) → Hall.
  Dá para dar a volta pelos dois lados: é como o grupo se separa **sem ninguém ser mandado
  embora**, e é o que o Art Bible pede com "salas em anel para o 'dois Tiagos'".
- **O hall tem exatamente 12 m de profundidade** = o alcance da bengala. Uma batida no alpendre
  mostra a casa até o fundo do hall. É a constante que ensina o alcance dela sem texto.
- **O corredor tem 16 m** = maior que o eco. O fundo do corredor é sempre desconhecido.
- **A escada corre pela parede leste do hall; os dois espelhos-herói estão na parede oeste.**
  Quem sobe ou desce é refletido o tempo todo. É o quadro-chave da fase: a equipe carregando o
  armário escada abaixo e, em algum degrau, **tendo uma pessoa a mais no reflexo**.
- **A galeria debruça sobre o hall.** De cima, a Cida bate a bengala e cobre o andar de baixo:
  o alcance dela atravessa o vão, a visão dos outros não. Também é o ponto de reencontro visual
  que impede o "me perdi e não achei mais ninguém".
- **O camarim do suplente é o único beco sem saída.** É onde está a relíquia e é onde se é pego.
- **Bloco leste é cortável.** Se o orçamento apertar, remova a Sala de Jantar e feche o anel
  só pelo oeste: a fase continua jogável, perde variedade de rota.

### 6.2 Portas e passagens

Toda passagem entre cômodos tem **≥ 2 m** (navegação do fantasma) ou é porta.
Portas simples 0,90 × 2,10; portas duplas **1,60 × 2,10** (por onde passam os móveis de dupla).

| # | Onde | Tipo | Posição |
|---|---|---|---|
| P1 | Rua → Hall sul (entrada) | **dupla 1,60** | parede Z = −6, X −0,8..+0,8 |
| P2 | Hall sul → Sala de Ensaio | simples 0,90 | parede X = −2, Z −3,2..−2,3 (entre E1 e E2) |
| P3 | Sala de Ensaio → Copa | **dupla 1,60** | parede Z = 0, X −5,8..−4,2 |
| P4 | Copa → Corredor dos Espelhos | simples 0,90 | parede Z = +4, X −5,5..−4,6 |
| P5 | Hall norte → Corredor dos Espelhos | **dupla 1,60** | parede Z = +4, X +0,2..+1,8 |
| P6 | Corredor → Sala de Jantar | simples 0,90 | parede Z = +4, X +5,5..+6,4 |
| P7 | Sala de Jantar → Hall norte | **dupla 1,60** | parede X = +4, Z +1,2..+2,8 |
| P8 | Galeria → Camarim Grande | **dupla 1,60** | parede X = −2, Z +1,2..+2,8 |
| P9 | Galeria → Corredor dos Camarins | **dupla 1,60** | parede Z = +4, X +0,2..+1,8 |
| P10 | Corredor dos Camarins → Camarim do Suplente | simples 0,90 | parede X = +4, Z +4,8..+5,7 |
| P11 | Camarim Grande → Corredor dos Camarins | simples 0,90 | parede X = −2, Z +4,8..+5,7 |
| P12 | Camarim do Suplente → fundo (**cenário, não abre**) | simples | parede Z = +6, X +5,5..+6,4 |

### 6.3 A escada

| Medida | Valor |
|---|---|
| Posição | X +2,40 .. +4,00 (encostada na parede leste do hall) |
| Avanço | Z −5,00 → Z 0,00 (**5,00 m**) |
| Subida | Y 0 → **+3,10 m** (2,80 pé-direito + 0,30 laje) |
| Inclinação | **31,9°** (≤ 32°, como o Art Bible pede) |
| Degraus | 16 × (0,3125 avanço × 0,19375 altura) |
| Largura útil | **1,60 m** — passa o armário de figurino em dupla |
| Guarda-corpo | lado oeste (X = +2,40), altura 1,00 m |
| Chegada | Galeria, Z = 0, com abertura no peitoril em X +2,40..+4,00 |

**Regra de carga na escada** (MEC-13): carregando um móvel de `minCarriers` 2, a velocidade na
escada é dividida por 1,5 e o móvel **range** (`escada_degrau`, 3 variações, alcance 9 m).
É o "carregar o sofá pela escada" do roteiro, com um monstro embaixo.

### 6.4 Nascimento, caminhão e navegação

- **Spawn Points (3)**, no alpendre, virados para +Z: `(−1, 0, −7)`, `(0, 0, −7)`, `(+1, 0, −7)`.
- **Caminhão** em `(0, 0, −12)`, `TruckZone` 4 × 4 × 3 m centrada em `(0, 1.5, −12)`.
- **Lanternas (2)**, `Interactable` "Pegar lanterna", no estribo do caminhão em `(−0.9, 1.0, −11.2)`
  e `(+0.9, 1.0, −11.2)`. Para a Cida a dica é **"Não preciso."** e a interação não faz nada.
- **NavMesh**: bake com agentes `Player`, `Ghost`. O 1º andar precisa de NavMesh próprio; a
  escada precisa de `NavMeshLink` nas duas pontas, senão o Imitador não sobe.
- **Peitoril da galeria**: colisor cheio (1,00 m) — ninguém cai no hall.

---

## 7. Tabela de cômodos

Esta é a tabela pedida no item 3 do briefing. "Sentido que lê o cômodo" = por qual percepção o
cômodo é **principalmente** legível; os três sentidos continuam funcionando em todos.

### 7.1 Térreo (Y = 0)

| Cômodo | Células | Mundo (X, Z) | Tamanho | Função no jogo | Lê-se por | Móveis | Espelhos | Luz | Âncora sonora | Pista de história |
|---|---|---|---|---|---|---|---|---|---|---|
| **Alpendre** | D0–E0 | X −2..+2 · Z −8..−6 | 4 × 2 | nascimento, bilhete, rádio do chefe | audição | — | — | farol do caminhão (âmbar, fraco) | **rádio chiado** (12 m) | bilhete nº 014 pregado na porta; nota fiscal "remoção de entidade — R$ 0,00" |
| **Hall dos Retratos (sul)** | D1–F3 | X −2..+4 · Z −6..0 | 6 × 6 · **pé-direito 5,90** | hub, escada, o quadro-chave da fase | **eco** (12 m = o hall inteiro) | caixa M | **E1, E2 (herói, parede X=−2)**, E3 (sul) | 2 arandelas a gás no hall | **relógio de pêndulo** (tique/taque 1 s, 9 m) | galeria de 7 retratos da Herondina; um retrato **vazio** com a moldura virada para a parede |
| **Hall dos Retratos (norte)** | D4–F5 | X −2..+4 · Z 0..+4 | 6 × 4 · pé-direito 2,80 | passagem sob a galeria; onde ficam o piano e o relógio | eco | — (piano e relógio são **fixos**) | — | sombra da galeria: o ponto mais escuro do térreo | **relógio de pêndulo** (9 m, alcança o hall inteiro) + **piano desafinado** (a partir de ~1:10) | partituras de revista no banco do piano |
| **Sala de Ensaio** | A1–C3 | X −8..−2 · Z −6..0 | 6 × 6 | primeiro trabalho; 1ª fonte de ruído | eco borrado (gramofone) | 2 cadeiras de plateia | **E4** (parede X=−8, 2,0 m, parede inteira) | arandela apagada; só lanterna | **gramofone travado** (loop, 10 m) → depois de afinado, **cortina de veludo** (6 m) | palco de 0,30 m; 4 manequins com figurino; cartazes de outras peças, todos sem o nome Taborda |
| **Copa** | A4–C5 | X −8..−2 · Z 0..+4 | 6 × 4 | rota alternativa do anel oeste | eco | caixa G | — | nenhuma | **torneira pingando** (1 gota / 2 s, 5 m) | louça com o monograma "T"; uma xícara a mais sempre posta |
| **Sala de Jantar** | G1–H5 | X +4..+8 · Z −6..+4 | 4 × 10 | rota do anel leste; corredor longo e estreito | eco / vibração | — | — | luar pela janela leste (frio) | **lustre de cristal tilintando** (7 m) | mesa posta para 9 pessoas; 8 lugares com talher, 1 sem |
| **Corredor dos Espelhos** | A6–H6 | X −8..+8 · Z +4..+6 | **16 × 2** | o corredor do medo: mais longo que o eco, e **mudo** (tapete corrido de 16 m: sem vibração para o Tiago, passo encurtado para a Cida) | **visão** — é o único cômodo onde os dois sentidos especiais falham | — | **E5a (M), E5b (G), E5c (M)** na parede Z=+6 | **arandela a gás assobiando** — a única luz; apagá-la é escolha da equipe | **arandela** (14 m) → depois de afinada, **silêncio** | 3 manequins encostados, um deles com o **colete da empresa** |

### 7.2 1º andar (Y = +3,10)

| Cômodo | Células | Mundo (X, Z) | Tamanho | Função no jogo | Lê-se por | Móveis | Espelhos | Luz | Âncora sonora | Pista de história |
|---|---|---|---|---|---|---|---|---|---|---|
| **Galeria** | D4–F5 | X −2..+4 · Z 0..+4 | 6 × 4 | chegada da escada; ponto de reencontro; a Cida ecoa o hall de cima | eco atravessa o vão | — | — | o que sobe do hall | **vento pela clarabóia** (`vento_janelas`, 8 m) | mural de fotos da companhia; em todas falta a mesma pessoa |
| **Camarim Grande** | A4–C6 | X −8..−2 · Z 0..+6 | 6 × 6 | o trabalho pesado: armário e penteadeira em dupla | eco / visão | **armário de figurino (2)**, **penteadeira (2)**, arara de figurino (1) | **E6** (parede X=−8, 2,0 m) | arandela a gás | **cabides de metal** na arara (5 m) | nome de cada ator colado no espelho; o de Olegário está no **canto**, fora da luz |
| **Corredor dos Camarins** | D6–F6 | X −2..+4 · Z +4..+6 | 6 × 2 | passagem estreita; **piso que range** | **audição pelo chão** | — | — | nenhuma | **assoalho rangendo** (só quando pisado, 7 m) | papel de parede descolando; atrás, outro papel de parede, mais antigo |
| **Camarim do Suplente** | G4–H6 | X +4..+8 · Z 0..+6 | 4 × 6 | beco sem saída; a relíquia; o cômodo mais silencioso | **diapasão** (é a única leitura confiável aqui) | **cartaz do teatro (relíquia)** atrás do espelho | **E7 — espelho rachado** (parede X=+8) | nenhuma: escuro total | **nenhuma.** Só um zumbido baixo do vidro rachado (4 m) | penteadeira pequena com um único pote de pó; um livro de falas com **todos** os papéis sublinhados |

### 7.3 Espelhos

| # | Cômodo | Parede · posição | Tamanho | Herói (reflexo real) | O que aparece |
|---|---|---|---|---|---|
| E1 | Hall sul | X = −2 · Z = −4,5 · virado +X | G (2,0) | **sim** | a cena real: a escada e quem sobe |
| E2 | Hall sul | X = −2 · Z = −1,5 · virado +X | G (2,0) | **sim** | a cena real · **esconderijo inicial do Imitador** |
| E3 | Hall sul | Z = −6 · X = +2,5 · virado +Z | M (1,2) | não | vidro escuro |
| E4 | Sala de Ensaio | X = −8 · Z = −3 · virado +X | G (2,0) | não | espelho de ensaio |
| E5a | Corredor | Z = +6 · X = −6 · virado −Z | M (1,2) | não | vidro escuro |
| E5b | Corredor | Z = +6 · X = 0 · virado −Z | G (2,0) | não | vidro escuro |
| E5c | Corredor | Z = +6 · X = +6 · virado −Z | M (1,2) | não | vidro escuro |
| E6 | Camarim Grande | X = −8 · Z = +3 · virado +X | G (2,0) | não | vidro escuro |
| E7 | Camarim do Suplente | X = +8 · Z = +3 · virado −X | **G rachado** | não (rachado) | **mostra o verso do cartaz, em letra espelhada legível** (`Reflected Layers` = só a camada `MirrorClue`) |

**Regra do motor**: só os **2 espelhos mais perto e na tela** refletem de verdade. Por isso E1 e E2
são os heróis (ficam juntos, na mesma parede, no cômodo mais visitado) e todos os outros são
vidro escuro — exceto E7, que não reflete a cena: mostra **só** o objeto da camada `MirrorClue`.
Isso é barato e é a resposta à pergunta 8 do briefing (pista que só existe no reflexo).

### 7.4 Âncoras sonoras — o mapa sonoro da casa

Resposta à pergunta 9 do briefing. **Toda sala tem um som próprio e constante**, para a Cida se
localizar sem gastar bengala. É o esqueleto da acessibilidade dela e também o clima da casa.

| Cômodo | Âncora | Alcance | Caráter |
|---|---|---|---|
| Alpendre | rádio chiado | 12 m | burocracia |
| Hall | **relógio de pêndulo** (1 tique/s) | 9 m | o metrônomo da casa; respiração e sussurro entram no contratempo |
| Hall norte | piano desafinado (a partir de ~70 s) | 12 m | ruído — atrapalha |
| Sala de Ensaio | gramofone travado → cortina de veludo | 10 m → 6 m | ruído → calmo |
| Copa | torneira pingando | 5 m | pequeno e regular |
| Sala de Jantar | lustre de cristal | 7 m | agudo, fino |
| Corredor dos Espelhos | arandela a gás → **silêncio** | 14 m → 0 | ruído → o vazio mais assustador da casa |
| Galeria | vento pela clarabóia | 8 m | aberto |
| Camarim Grande | cabides de metal | 5 m | metálico |
| Corredor dos Camarins | assoalho rangendo (só pisado) | 7 m | delator |
| Camarim do Suplente | **nenhuma** (zumbido do vidro, 4 m) | 4 m | o cômodo sem chão sonoro |

---

## 8. Mecânicas novas

Ordem por tier: **A** = a fase não funciona sem; **B** = a fase fica muito melhor;
**C** = fica para depois (Fase 6 ou polimento).

| # | Mecânica | De quem | O que faz | Onde no código | Tier |
|---|---|---|---|---|---|
| **MEC-01** | **Diapasão** | Cida | Tecla nova. Emite `SoundType.Tuning` (novo, `1 << 7`), alcance 15 m, recarga 8 s. Por 4 s a onda de eco fica **sustentada** (raio 8 m, 2 m/s) em vez de um pulso. | novo `Scripts/Characters/TuningFork.cs`; `SoundType.Tuning` em `Echo/SoundEvent.cs`; `GameInput.Fork` (`<Keyboard>/q`); `bool tuningFork` na `CharacterDefinition`; linha no `CharacterSwitcher.Become`; componente em `LevelPrefabsBuilder.BuildPlayer` (lista `ownerOnly`) | **A** |
| **MEC-02** | **Timbre de superfície** | eco | Cada material responde ao diapasão com cor e espessura própria: madeira (quente, curto), vidro (frio, longo), tecido (quase nada), metal (agudo). | novo `Scripts/Echo/SurfaceTag.cs` (enum + componente no prefab); `EchoSystem` passa o tipo ao shader; `Echo.shader` ganha `_EchoTimbre` | **A** |
| **MEC-03** | **Espelho ocupado soa rachado** | Cida | Um `Mirror` que esteja servindo de esconderijo ao Imitador responde ao diapasão com zumbido **rachado** em vez de tom limpo. É como ela audita a casa. | `Mirror.IsOccupied` (bool, escrito pelo `GhostImitator` no host, replicado); `TuningFork` lê e pinta; som `diapasao_vidro_rachado` | **A** |
| **MEC-04** | **Fontes de ruído** | casa | Um `NoiseSource` ligado soma ruído num raio. `NoiseLevel` (0..1) da Cida = soma das fontes ativas que a alcançam. Raio da bengala = `Lerp(12, 4, noise)`; borda do anel × `(1 + noise × 2)`; vida da onda × `(1 − noise × 0,5)`. | novo `Scripts/Echo/NoiseSource.cs`; `EchoSystem` aplica em `_EchoRingWidth` e no raio dos pulsos | **A** |
| **MEC-05** | **Afinar** | **só a Cida** | `Interactable` com verbo "Afinar", `onlyFor = Cida` e **`holdSeconds = 2`** (campo novo). Desliga a `NoiseSource`, toca o som de alívio e emite `GameCue` para a equipe ("A casa ficou mais quieta."). | `Objectives/Interactable.cs` ganha `holdSeconds`; `PlayerInteractor` passa a suportar segurar; `On Used` → `NoiseSource.Silence()` | **A** |
| **MEC-06** | **Chamada da bengala** | Cida | Segurar a tecla da bengala por 2 s: 3 batidas seguidas, alcance 20 m, e um **marcador no chão por 6 s** visível para os três (anel de vibração para o Tiago, pulso de luz fria para o Marcelo). `GameCue` "A Cida está chamando" com `Senses.Hearing \| Senses.Vibration`. | `Echo/CaneTapper.cs`; `Characters/VibrationRing.cs` para o desenho; `GameCues.Emit` | **A** |
| **MEC-07** | **Carregar em dupla** | todos | `Furniture` ganha `minCarriers` (1–3) e pontos de pega `SOC_Carry_A/B`. **Marcelo conta como 2.** Com `minCarriers` 2, um jogador sozinho só consegue **arrastar** (lento e barulhento: `arrastar_movel`, 10 m). Velocidade da dupla = a do mais lento. | `Furniture/Furniture.cs`, `Player/FurnitureCarrier.cs`; o `Furniture.PromptFor` passa a dizer "Segurar a outra ponta" quando já há um carregador | **A** |
| **MEC-08** | **A isca é mentira** | Imitador | Quando a cópia "segura a outra ponta", ela **não** alivia o peso, **não** emite impacto de apoio e **não** faz o móvel inclinar. Três sinais, um por personagem: o Marcelo sente o peso, o Tiago vê a falta de inclinação, a Cida não ouve o apoio (`movel_dupla_apoio`). | `Ghosts/GhostImitator.cs` + `Furniture` (um carregador "fantasma" que não conta para `minCarriers`) | **A** |
| **MEC-09** | **6 s para soltar** | todos | Vítima sendo puxada pode ser salva de **duas** formas: apontar (desmascarar, já existe) **ou** um terceiro segurar o mesmo móvel (âncora). Passados 6 s sem salvamento, captura. | `Player/PlayerCondition.cs` + `GhostImitator`; cronômetro na HUD do grupo via `GameCue` | **A** |
| **MEC-10** | **Manequins** | cenário | Props estáticos com `EmitsFootstep = false` e sem respiração: no eco leem igual ao Imitador. Só o diapasão (madeira × vidro) e o fato de não se moverem os separam. Um deles **cai sozinho** no corredor aos ~2:40 (vento). | prefab `Assets/Prefabs/Casa/Manequim.prefab` + `SurfaceTag.Madeira`; o tombo é um `Animation Event` + `espelho_... ` → `manequim_tomba` | **A** |
| **MEC-11** | **Piso que range** | casa | Volume de gatilho sobre tábuas marcadas: o passo de quem pisa vira `passo_madeira_rangendo` com alcance 7 m (em vez de 2,5). Vale para o Corredor dos Camarins e 3 tábuas da Galeria. | novo `Scripts/Echo/CreakyFloor.cs` (BoxCollider trigger que multiplica o próximo passo) | **A** |
| **MEC-12** | **A reverência** | Imitador | Estado novo: depois de uma imitação bem-sucedida (ou da aparição dos 45 s), ele para **2 s** fazendo reverência — **vulnerável a ser apontado**. É a janela de contra-ataque e a piada. | `Ghosts/GhostImitator.cs` (novo estado) + `Audio/GhostSounds.cs` (`imitador_reverencia`) | **A** |
| **MEC-13** | **Carga na escada** | todos | Carregando `minCarriers ≥ 2`, a velocidade na escada ÷ 1,5 e cada degrau emite `escada_degrau` (9 m). | `Player/PlayerMovement.cs` (detecta inclinação > 15°) | **B** |
| **MEC-14** | **Reflexo atrasado 0,3 s** | visão | O reflexo da cópia atrasa 0,3 s em relação ao corpo. Tell dos jogadores que enxergam. | `Level/MirrorReflection.cs` + um transform-buffer no `GhostVisual` | **B** |
| **MEC-15** | **Sem sombra** | visão | A cópia não projeta sombra (`ShadowCastingMode.Off` enquanto disfarçada). | `Ghosts/GhostVisual.cs` | **B** |
| **MEC-16** | **Lanterna (2 no caminhão)** | todos menos a Cida | Luz quente 3200 K, cone 60°, 10 m. Só 2 unidades. Para a Cida a dica é "Não preciso." | novo `Scripts/Characters/Flashlight.cs` (receita "Uma habilidade" do ARQUITETURA.md) + `Interactable` no caminhão | **B** |
| **MEC-17** | **Bilhetes do chefe nos móveis** | comédia | `Interactable` com verbo **"Ler"** em 6 móveis/objetos; mostra 1 linha do Valdemar. O Tiago lê; a Cida recebe uma versão em áudio ("alguém colou um papel aqui"). | `Objectives/Interactable.cs` + `GameCue` com textos diferentes por sentido | **B** |
| **MEC-18** | **Gingado do Marcelo** | comédia / tell | Animação `Walk_Desengoncado` ao carregar peso; a cópia **nunca** a usa. | `Player/DollWalk.cs` + Animator (`AnimationBuilder`) | **B** |
| **MEC-19** | **Mundo do espelho** | vítima | Capturado vai para um cômodo-espelho de 4 × 4 m e pode **bater no vidro** (`SoundType.Impact`, 8 m) para ser achado. | nova cena aditiva + `PlayerCondition` | **C** |
| **MEC-20** | **Relíquia que resiste** | todos | Massa efetiva = base × (1 + d/10), d = metros até a porta. | `Furniture` + `resistCurve` | **C** (Fase 6) |

### 8.1 Como o diapasão fica legível (detalhe de arte)

| Superfície | Resposta ao diapasão | Cor no eco | Duração |
|---|---|---|---|
| Vidro (espelho limpo) | tom puro e longo | ciano claro, anel fino | 2,5 s |
| **Vidro rachado / ocupado** | **zumbido batido, bate-estaca** | **ciano sujo, anel tremido** | 1,2 s |
| Madeira (parede, manequim, móvel) | toque curto e quente | azul morno, anel grosso | 0,8 s |
| Metal (cabide, arandela, lustre) | agudo e cortante | branco, anel fino | 1,8 s |
| Tecido (cortina, figurino) | quase nada | mancha apagada | 0,3 s |
| **Pessoa de verdade** | **estremece** (micro-vibração) | contorno pulsa 1× | — |
| **A cópia** | **nada** | contorno parado | — |

---

### 8.2 O terror do Tiago — o medo que não passa pelo som

O Tiago **burla o terror sonoro de propósito**: é o superpoder dele, e tirar isso quebra o
personagem. O erro fácil seria compensar com sustos visuais — isso não assusta, e não é ele.
O medo dele precisa ter outra natureza.

> **O medo da Cida é não saber.
> O medo do Marcelo é não poder gritar.
> O medo do Tiago é saber primeiro e não conseguir contar.**

Dois eixos sustentam tudo o que vem abaixo:

1. **Ele vive levemente no passado.** A vibração chega atrasada, então tudo o que ele vê
   já aconteceu.
2. **O mundo dele tem um buraco com o formato do fantasma.** O Imitador **flutua** e não emite
   passos (prancha GHO_01): a casa inteira pulsa de vibração — os passos dos amigos, o relógio,
   o vento, o disco riscado — e a única coisa que **não** faz anel nenhum é a que quer pegá-lo.
   Ele não procura presença: procura **ausência**.

| # | Mecânica | O que faz | Por que assusta | Onde no código | Tier |
|---|---|---|---|---|---|
| **MEC-T1** | **A vibração chega atrasada** | O anel nasce com `distância ÷ 7 m/s` de atraso — **a mesma velocidade da onda do eco**, porque é a mesma onda lida por dois sentidos. A 10 m ele vê 1,4 s no passado. **Abaixo de 4 m é instantâneo** (válvula de justiça) | Tudo que ele vê **já aconteceu**. Quando algo corre na direção dele, o anel mostra onde aquilo **estava**. Numa perseguição ele é obrigado a confiar nos olhos, não no chão | `Characters/VibrationVision.cs`: fila de eventos com carimbo de tempo em vez de desenhar na hora | **A** |
| **MEC-T2** | **O buraco no chão** | O Imitador flutua e não emite passos: **zero anéis**. A poeira do assoalho guarda o **rastro que não se mexeu**; a lanterna rente ao chão (ângulo < 25°) o revela | O único ponto morto do mundo dele é o que quer pegá-lo. E o verbo dele vira **olhar para baixo** — justo quando o fantasma está a 2,30 m de altura | `VibrationVision` + decal de poeira + `Characters/Flashlight.cs` | **A** |
| **MEC-T3** | **Ruído satura a tela** | O mesmo `NoiseLevel` da MEC-04 que encolhe o eco da Cida **enche** a tela do Tiago de anéis falsos (o gramofone sozinho gera ~6 anéis/s). Acima de 0,6 ele não separa passo de disco riscado | Ela fica **sem imagem**, ele fica com **imagem demais**: os dois são cegados pela mesma coisa, e os dois passam a torcer para a Cida afinar. Em silêncio total o jogo inverte: **cada anel passa a ser informação pura**, e um anel que ele não consegue explicar é desastre | `Echo/NoiseSource.cs` → `VibrationVision.falseRingRate` | **A** |
| **MEC-T4** | **Rotas mudas (tapetes)** | Tapete mata o anel de vibração **e** encurta o passo no eco. O **Corredor dos Espelhos ganha um tapete corrido de 16 m** | O cômodo mais assustador da casa é exatamente onde o sentido dele não funciona — e o da Cida também. O corredor é onde todo mundo fica menor | `SurfaceTag.Tecido` + `CaneTapper` e `VibrationVision` lendo a superfície pisada | **A** |
| **MEC-T5** | **Toque no ombro** | Qualquer pessoa pode tocar o ombro dele (Interagir a 1,5 m, por trás) para avisar: tranco de câmera + `GameCue` só para ele. **É o único alarme que o alcança.** A cópia disfarçada faz igual — mesma animação, mesmo tranco; só que **a mão não sai** | O gesto que significa "estou te protegendo" vira a coisa que ele mais teme. É o `Startle_Ombro` da prancha CHR_02 virando mecânica | novo `Scripts/Characters/ShoulderTap.cs` (`IInteractionTarget` no Player, só mira em quem tem `deaf`) + `Ghosts/GhostImitator.cs` | **A** |
| **MEC-T6** | **Uma pessoa a mais no reflexo** | Em momentos marcados, o reflexo de E1/E2 tem **4 pessoas** e a sala tem 3. Só quem está olhando vê — e ele é o que olha | Ele **sabe primeiro**. E depois precisa chegar até alguém, fazer a pessoa olhar para ele, e só então contar | `Level/MirrorReflection.cs` + `GhostImitator` entrando na camada refletida no estado de patrulha | **B** |
| **MEC-T7** | **O bloquinho escrito** | Uma vez por partida, ao abrir o bloquinho há um desenho que ele não fez: **uma máscara de teatro fazendo reverência** | É o susto-assinatura dele, como o rádio sem respiração é o da Cida. E é o Olegário **se apresentando** para o único que repara nas coisas | troca da RenderTexture do `PRP_Bloquinho` + `StoryFlag` para não repetir | **B** |
| **MEC-T8** | **Sub-grave e rumble** (acessibilidade) | A mixagem do Tiago deixa de ser −80 dB e passa a **passa-baixa em 80 Hz a −18 dB** + vibração de controle. O sub-grave **não tem direção nem detalhe**: só pressão | Devolve a atmosfera sem devolver informação nenhuma. E é mais fiel: surdez profunda não é ausência de percepção, é percepção por outro caminho — o corpo sente o grave | **feita** em 29/10/2026 sem AudioMixer: `AudioLowPassFilter` no `AudioListener` + `Audio/DeafMixRules.cs` + opção **"silêncio total"** em `Menu/GameSettings.cs`. Falta a vibração do controle | **B** |
| **MEC-T9** | **Ele não pode gritar** (regra de level design, não código) | O aviso dele custa ~1,5 s (Libras / bloquinho / apontar) **e exige que o outro esteja olhando**. O da Cida é instantâneo e cobre 12 m | O terror dele é a **latência da comunicação**. O level design tem que criar os momentos em que ele sabe e está longe: a galeria vendo o hall, o corredor reto, o camarim do fundo | planta (§6): garantir linha de visada galeria→hall e corredor→corredor | **A** |

**Regras de justiça** (toda mecânica que tira agência tem contrapartida):

| Tira | Devolve |
|---|---|
| Vibração atrasada (MEC-T1) | abaixo de 4 m é instantânea; agachado, os próprios anéis dele somem e a leitura limpa |
| Fantasma sem anel (MEC-T2) | a poeira é evidência negativa legível com a lanterna |
| Tela saturada (MEC-T3) | a Cida pode afinar; e ele é quem **vê** o gramofone tremendo para avisá-la onde está |
| Tapete mudo (MEC-T4) | é o cômodo com mais luz (a arandela) enquanto ela não for afinada |
| Toque falso (MEC-T5) | a mão da cópia **não sai do ombro**: 1 s de tell antes do ataque |

**O que isso NÃO muda:** ele continua imune a hipnose, a gritos e à voz da isca do Imitador.
A Fase 2 continua sendo a fase dele. O que muda é que, na Fase 1, ele para de ser
"o que não é afetado" e vira **o que enxerga cedo demais e não consegue falar a tempo**.

## 9. Modelos novos

Tudo gerado por script, como manda o [ART_BIBLE.md](ART_BIBLE.md) (nada modelado à mão).
Destino: `MudancaAssombrada/Assets/Art/<Tipo>/<Nome>/<PREFIXO>_<Nome>.fbx`.

### 9.1 Kit de cenário — `tools/blender/build_env_kit_blockout.py`

| Nome | Medidas (m) | Tris (alvo) | Observação |
|---|---|---|---|
| `SM_ENV_EscadaLance` | 2,00 avanço × 1,60 larg × 1,24 subida | 0,5k | módulo de 4 degraus; 4 módulos + 1 degrau de topo = o lance |
| `SM_ENV_GuardaCorpo` | 2,00 × 0,08 × 1,00 | 0,4k | escada e peitoril da galeria |
| `SM_ENV_ParedePortaDupla` | 2,00 × 2,80 × 0,15 | 0,6k | vão de 1,60 × 2,10 |
| `SM_ENV_PortaDupla` | 1,60 × 2,10 × 0,05 | 0,4k | duas folhas, origem na dobradiça de cada uma |
| `SM_ENV_PisoTabua` | 2,00 × 2,00 | 0,3k | corredores e camarins (o xadrez fica no térreo nobre) |
| `SM_ENV_Laje` | 2,00 × 2,00 × 0,30 | 0,1k | piso do 1º andar visto de baixo |
| `SM_ENV_MolduraG_Rachada` | 2,00 | 0,8k | variante do G com `_CrackMask`; é o E7 |
| `SM_ENV_ArandelaGas` | 0,25 × 0,20 × 0,45 | 0,5k | com `SOC_Flame` para a luz piscando |
| `SM_ENV_ClaraboiaGaleria` | 2,00 × 2,00 × 0,25 | 0,4k | luar na galeria |
| `SM_ENV_TabuaSolta` | 0,30 × 2,00 × 0,04 | 0,1k | marca visual do piso que range |
| `SM_ENV_TapeteCorrido` | 2,00 × 1,40 × 0,02 | 0,1k | módulo; 8 deles = o tapete de 16 m do corredor (`SurfaceTag.Tecido`) |
| `SM_ENV_PoeiraChao` | 2,00 × 2,00 (decal) | 0,1k | camada de poeira do assoalho; guarda o rastro que **não** se mexeu (MEC-T2) |

### 9.2 Props — `tools/blender/build_props_blockout.py`

| Nome | Medidas (m) | Tris | Observação |
|---|---|---|---|
| `SM_PRP_Manequim_A/B/C` | 0,45 × 0,30 × 1,75 | 0,9k cada | três poses; **sem rig**; `SurfaceTag.Madeira` |
| `SM_PRP_Gramofone` | 0,50 × 0,50 × 0,80 | 1,2k | corneta + disco; fonte de ruído |
| `SM_PRP_Retrato_P/M/G` | 0,40 / 0,70 / 1,10 de altura | 0,2k cada | galeria do hall; um deles virado para a parede |
| `SM_PRP_Lustre` | 0,60 × 0,60 × 0,70 | 1,4k | Sala de Jantar; âncora sonora |
| `SM_PRP_Lanterna` | 0,12 × 0,12 × 0,25 | 0,6k | 3200 K; `SOC_Hand_R` |
| `SM_PRP_Torneira` | 0,15 × 0,12 × 0,20 | 0,3k | Copa |
| `SM_PRP_PoteDePo` | 0,08 × 0,08 × 0,06 | 0,1k | camarim do suplente |
| `SM_PRP_LivroDeFalas` | 0,16 × 0,22 × 0,04 | 0,2k | camarim do suplente |

### 9.3 Mobília — `tools/blender/build_furniture_blockout.py`

| Nome | Medidas (m) | Massa | `minCarriers` | Tris | Já existe? |
|---|---|---|---|---|---|
| `SM_PRP_ArmarioFigurino` | 1,00 × 0,60 × 2,00 | 80 kg | **2** | 2,6k | sim |
| `SM_PRP_Penteadeira` | 1,20 × 0,50 × 1,60 | 45 kg | **2** | 2,2k | sim |
| `SM_PRP_PianoArmario` | 1,50 × 0,60 × 1,30 | 230 kg | — (**fixo** nesta fase) | 3,0k | sim |
| `SM_PRP_CadeiraPlateia` | 0,50 × 0,50 × 1,00 | 6 kg | 1 | 0,6k | sim (cadeira) |
| `SM_PRP_Caixa_M/G` | 0,50 / 0,60 cubo | 15 / 25 kg | 1 | 0,3k | sim |
| **`SM_PRP_AraraFigurino`** | 1,40 × 0,50 × 1,70 | 20 kg | 1 | 1,0k | **novo** — cabides de metal (âncora sonora) |
| `SM_PRP_RelogioPendulo` | 0,55 × 0,32 × 2,10 | — (fixo) | — | 1,8k | sim |

### 9.4 Props de história — `tools/blender/build_story_props_blockout.py`

| Nome | O que muda | Observação |
|---|---|---|
| `SM_PRP_Cartaz_Teatro` | ganha **verso** com decal de texto espelhado | camada `MirrorClue`; `relicId = CARTAZ`; massa 30 kg, `minCarriers` 1 |
| `SM_PRP_Bilhete` | 6 variações de texto (TextMeshPro em RenderTexture) | textos em §11 |
| `SM_PRP_NotaFiscal` | já existe | "REMOÇÃO DE ENTIDADE — R$ 0,00 (cortesia)" |
| `SM_PRP_Radio_Valdemar` | já existe | vai no alpendre, não no caminhão |

### 9.5 Imitador — `tools/blender/build_imitador_blockout.py`

| Falta | O quê |
|---|---|
| Blendshape `comedy → tragedy` | 100 frames, mesmos vértices (prancha GHO_01) |
| Osso `hat` na cartola | inclina na reverência |
| Cadeias de cortina (8, mola) | o "sai ofendido" |

---

## 10. Sons novos

Formato pronto para colar em `tools/audio/catalogo.json` (lista `sons`). Gerar com
`python tools/audio/gerar_sons.py --so <id>`. Depois: **Mudança Assombrada › Atualizar
Biblioteca de Sons**.

```json
{ "id": "diapasao_bate",            "pasta": "Personagens", "duracao": 4.0,  "texto": "A metal tuning fork struck once, pure sustained musical note ringing and slowly fading out, close, clean, no reverb" },
{ "id": "diapasao_vidro",           "pasta": "Personagens", "duracao": 2.5,  "texto": "A large pane of glass resonating sympathetically with a pure tone, glassy shimmering ring, clean" },
{ "id": "diapasao_vidro_rachado",   "pasta": "Fantasmas",   "duracao": 2.5,  "texto": "A cracked pane of glass resonating dully with a buzzing rattle, wrong and unstable, ends abruptly" },
{ "id": "diapasao_madeira",         "pasta": "Personagens", "duracao": 1.0,  "texto": "A short warm wooden knock resonating briefly, dry, close" },
{ "id": "diapasao_metal",           "pasta": "Personagens", "duracao": 2.0,  "texto": "A thin metal object ringing with a high sharp tone, bright, slowly fading" },
{ "id": "cida_chamada",             "pasta": "Personagens", "duracao": 1.8,  "texto": "Three sharp deliberate taps of a white cane on a wooden floor in quick succession, like a signal" },
{ "id": "gramofone_travado",        "pasta": "Casa",        "duracao": 12.0, "loop": true, "normalizar": -30, "texto": "An old gramophone stuck on a scratched record, the same half second of a 1920s waltz repeating endlessly with crackle and hiss" },
{ "id": "gramofone_para",           "pasta": "Casa",        "duracao": 2.0,  "texto": "A gramophone needle lifted off a spinning record, the music stops with a scrape and the turntable slows down" },
{ "id": "piano_desafinado_acorde",  "pasta": "Casa",        "duracao": 3.0,  "texto": "A single badly out of tune upright piano chord played by itself in a large empty hall, long decay" },
{ "id": "piano_abafado",            "pasta": "Casa",        "duracao": 2.0,  "texto": "Piano strings being muted by a hand, a soft thud and the resonance dying away quickly" },
{ "id": "arandela_gas",             "pasta": "Casa",        "duracao": 10.0, "loop": true, "normalizar": -30, "texto": "A gas wall lamp hissing steadily with a faint flutter, old house, continuous" },
{ "id": "arandela_fecha",           "pasta": "Casa",        "duracao": 1.5,  "texto": "A small brass valve being turned shut, the hiss stops with a soft pop and a flame goes out" },
{ "id": "torneira_pingando",        "pasta": "Casa",        "duracao": 6.0,  "loop": true, "normalizar": -32, "texto": "A tap dripping slowly into an empty metal sink, one drop every two seconds, small room" },
{ "id": "lustre_tilintando",        "pasta": "Casa",        "duracao": 8.0,  "loop": true, "normalizar": -32, "texto": "A crystal chandelier tinkling faintly in a draft, small glass pieces touching, continuous" },
{ "id": "cabides_metal",            "pasta": "Casa",        "duracao": 6.0,  "loop": true, "normalizar": -32, "texto": "Metal coat hangers knocking softly against each other on a clothing rail, faint, continuous" },
{ "id": "cortina_veludo",           "pasta": "Ambiente",    "duracao": 3.0,  "texto": "A heavy velvet stage curtain moving slowly in a draft, soft fabric rustle" },
{ "id": "escada_degrau",            "pasta": "Passos",      "duracao": 0.8,  "variacoes": 3, "texto": "A heavy footstep on a creaking wooden staircase step, loud single creak" },
{ "id": "madeira_rangendo_longo",   "pasta": "Passos",      "duracao": 2.0,  "texto": "An old wooden floorboard creaking slowly under shifting weight, long and loud, haunted house" },
{ "id": "movel_dupla_apoio",        "pasta": "Moveis",      "duracao": 1.2,  "texto": "Two people setting a heavy wardrobe down together on a wooden floor, a controlled double thud and a wooden knock" },
{ "id": "manequim_tomba",           "pasta": "Moveis",      "duracao": 1.5,  "texto": "A wooden shop mannequin toppling over and clattering hollow on a wooden floor" },
{ "id": "imitador_reverencia",      "pasta": "Fantasmas",   "duracao": 2.5,  "texto": "A heavy velvet curtain swishing and an old stage board creaking softly, like a theatrical bow, no voice" },
{ "id": "imitador_mascara_gira",    "pasta": "Fantasmas",   "duracao": 1.5,  "texto": "A porcelain mask rotating with a dry ceramic grind and a final sharp click" },
{ "id": "imitador_sai_ofendido",    "pasta": "Fantasmas",   "duracao": 2.0,  "texto": "A heavy velvet curtain yanked shut sharply in anger, with a glassy shimmer underneath" },
{ "id": "espelho_batida_interna",   "pasta": "Fantasmas",   "duracao": 1.5,  "texto": "Fists banging on thick glass from the other side, muffled and desperate, three hits" },
{ "id": "radio_bilhete",            "pasta": "Ambiente",    "duracao": 10.0, "texto": "An old valve radio reading a short note in a crackly tired male voice, words unintelligible, flat delivery with no breaths between phrases" },
{ "id": "casa_fica_quieta",         "pasta": "Ambiente",    "duracao": 2.5,  "texto": "A room suddenly becoming quiet, a soft downward swell as background noise fades out, relief" }
```

### 10.1 Mixagem (lista `mixagem` do mesmo arquivo)

| id | camada | volume | alcance | intervaloMin | noTempo |
|---|---|---|---|---|---|
| `diapasao_bate` | pista | 0,60 | 18 | 8 | não |
| `diapasao_vidro` / `_madeira` / `_metal` | pista | 0,40 | 12 | 0,2 | não |
| `diapasao_vidro_rachado` | **ameaca** | 0,55 | 12 | 0,5 | não |
| `cida_chamada` | pista | 0,75 | 22 | 6 | não |
| `gramofone_travado` | cama | 0,55 | 12 | — | — |
| `arandela_gas` | cama | 0,45 | 16 | — | — |
| `piano_desafinado_acorde` | **ameaca** | 0,60 | 14 | 20 | não |
| `torneira_pingando` / `lustre_tilintando` / `cabides_metal` | cama | 0,30 | 7 | — | — |
| `cortina_veludo` | cama | 0,25 | 8 | — | — |
| `escada_degrau` | pista | 0,55 | 10 | 0,3 | não |
| `madeira_rangendo_longo` | pista | 0,65 | 9 | 1,5 | não |
| `movel_dupla_apoio` | pista | 0,55 | 8 | 0,5 | não |
| `manequim_tomba` | **ameaca** | 0,70 | 12 | 10 | não |
| `imitador_reverencia` | **ameaca** | 0,50 | 10 | 3 | não |
| `imitador_mascara_gira` / `_sai_ofendido` | **ameaca** | 0,65 | 12 | 2 | não |
| `espelho_batida_interna` | pista | 0,70 | 10 | 1 | não |
| `radio_bilhete` | musica | 0,70 | 14 | — | — |
| `casa_fica_quieta` | interface | 0,50 | 0 | 5 | não |

> **`radio_bilhete` é o som mais importante desta lista.** A frase do texto para a IA —
> *"no breaths between phrases"* — é literalmente a primeira pista do roteiro inteiro.

### 10.2 Direção sonora: o ritmo do medo (27/10/2026)

**O problema (depois do primeiro Play):** a trilha era confusa, irritante e repetitiva; tudo alto,
um som atravessando o outro, nenhum silêncio. A causa principal era um erro: o "pulso" que cada fonte
de ruído manda para o eco tocava a **caixinha de música** (o som padrão de música) a cada 1,5 s, várias
por cima umas das outras. Somado a sussurros a cada 9 s e ao acorde do piano a cada 7 s, os dois na
camada de ameaça, o fundo abaixava e subia sem parar, e nada assustava porque tudo assustava.

**Os princípios** (do cinema e dos jogos de terror):

| Princípio | De onde vem | Como fica no jogo |
|---|---|---|
| A música fica por baixo, e o susto é a mudança nela | *Silent Hill 2*, *Alien: Isolation* | A trilha é uma **cama baixa e contínua** (a faixa inteira em loop, 0,55 do volume dela), abaixo da casa. O medo vem de quando ela **some** (vácuo) ou **muda** (pânico, aparição) |
| O silêncio antes do susto assusta mais que o susto | Hitchcock (a bomba debaixo da mesa), *Um Lugar Silencioso*, *P.T.* | O falso a menos de 9 m: a casa **se cala** em 2,5 s (o vácuo), só o relógio continua. 7 s depois, um crescendo baixinho |
| O silêncio não pode virar detector | *Alien: Isolation* (o rastreador também engana) | **Vácuo falso**: a cada 60–100 s, a cama afunda e a casa se cala por 8–12 s sem motivo nenhum |
| Tensão e alívio em ciclos | O "AI Director" de *Left 4 Dead* (construir, pico, relaxar) | Calma → vácuo → pânico → **corte seco** → 8 s sem música → a cama volta de onde parou (6 s de fade) |
| Um susto de cada vez | *Resident Evil 7*, *Amnesia* | **Uma ameaça por vez**: o clima (sussurro, acorde) só toca com a casa livre e 6 s depois da última. O acontecimento (captura, desmascarar) toca sempre e corta o clima |
| O golpe precisa de vazio antes | toda trilha de terror | O trecho da aparição começa com **3,5 s de nada**, o golpe cai na reverência do Imitador, e o grave arrastado o leva de volta |

**A trilha** (*Dark Piano Migraine*, 4:30), medida a cada 0,5 s e cortada nos vales de volume
(`Assets/Data/Trilhas/Casa do Espelho.asset`):

| Trecho | Tempo | Como soa | Quando toca |
|---|---|---|---|
| *(a faixa inteira)* | 0:00–4:30 | — | a **cama**, em loop, retomando de onde parou |
| Crescendo | 2:23–2:49 | cresce | 7 s depois do vácuo de verdade |
| Pressão | 1:26–1:59 | grave e pesado | alguém sendo puxado |
| Clímax | 2:56–3:19 | o mais alto da faixa | alguém sendo puxado |
| Aparição | 4:06–4:19 | 3,5 s de nada, golpe de +22 dB, grave arrastado | os 45 s |

Os trechos "calma" e "final" (0:56–1:25, 2:02–2:19, 0:02–0:53, 3:20–3:59) continuam no asset, mas
desde a cama contínua o diretor não os usa.

**A noite, no ouvido** (com o beat chart do §5):

| Tempo | O que se ouve |
|---|---|
| 0:03 | a cama entra baixinho (6 s de fade) por baixo da casa |
| 0:45 | a aparição: silêncio, golpe na reverência, o grave. Depois, 8 s sem música e a cama volta |
| 0:45–3:45 | a cama sempre por baixo; vácuos falsos a cada 60–100 s, o vácuo de verdade com o falso perto, e pânico quando alguém é puxado |
| 3:45 | último quarto: a cama sobe para 0,75 e não tem mais vácuo falso |
| 5:00 | a música sai em fade com o amanhecer |

**Passos (2º Play):** gerados de novo, mais baixos e sombrios (texto "quiet, low, dark", 2 semitons
abaixo; o do fantasma, 3), com volume igualado (−32 dB RMS) e mixados a 0,36–0,45 (antes 0,5–0,6).

**A mixagem nova** (`tools/audio/catalogo.json`): âncoras entre 0,22 e 0,28 de volume e 6 a 9 m de
alcance (antes 0,35–0,40 e até 30 m; o vento cobria a casa inteira); o relógio a 0,16; o gramofone e a
arandela mais baixos e mais perto; o sussurro com 14 s de intervalo mínimo e o acorde do piano com 15 s,
a cada 16 s; a captura e o vidro trincando a 0,8. São **acontecimentos** (`importante`): captura,
vidro trincando, reverência, máscara, sai ofendido, manequim, vidro rachado do diapasão.

**Para ajustar de ouvido:** os números do ritmo ficam no `SoundDirector` (objeto "Sons" da cena), os
cortes da faixa no asset da trilha e os volumes no catálogo (rodar Atualizar Biblioteca de Sons).
**Ficou para depois:** a estamina no arrastar e uma segunda faixa para o Tiago sentir (vibração no
controle) no lugar da trilha, que ele não ouve.

---

## 11. Objetivos, móveis e textos

### 11.1 Etapas (componentes `Objective` na cena)

| Etapa | Componente | Conteúdo | Abre quando |
|---|---|---|---|
| **0** | `DeliverFurnitureObjective` · "Tirar os móveis do térreo" | 2 cadeiras, caixa M, caixa G (**4**) | início |
| **1** | `DeliverFurnitureObjective` · "Esvaziar os camarins" | arara, penteadeira, armário (**3**) | etapa 0 completa (~2:00) |
| **2** | `DeliverFurnitureObjective` · "O cartaz do camarim do fundo" | cartaz do teatro (**1**) | etapa 1 completa |

> Entregar o cartaz grava `SaveSystem.Data.SetFlag("relíquia-cartaz-entregue")` além do
> `achou-cartaz-teatro` que já existe ao pegá-lo. A Fase Final lê os dois.

### 11.2 Móveis na cena

| Móvel | Posição (X, Z) · andar | Peso (jogo) | `minCarriers` | Etapa |
|---|---|---|---|---|
| Cadeira de plateia A | (−6,0 · −4,5) térreo | 1 | 1 | 0 |
| Cadeira de plateia B | (−4,5 · −4,5) térreo | 1 | 1 | 0 |
| Caixa de mudança M | (+1,5 · −4,0) térreo | 1,5 | 1 | 0 |
| Caixa de mudança G | (−6,0 · +2,0) térreo | 2 | 1 | 0 |
| Arara de figurino | (−6,5 · +1,0) 1º andar | 1,5 | 1 | 1 |
| **Penteadeira de camarim** | (−4,0 · +4,5) 1º andar | 2,5 | **2** | 1 |
| **Armário de figurino** | (−6,5 · +4,8) 1º andar | 3,5 (era 3) | **2** (o Marcelo e mais um) | 1 |
| **Cartaz do teatro** (relíquia) | (+7,6 · +3,0) 1º andar, **atrás do E7** | 2 | 1 | 2 |

Fixos (não saem): relógio de pêndulo `(+3,6 · +3,2)` (parede leste do hall norte, pé-direito 2,80 comporta os 2,10 m), piano de armário `(−1,2 · +2,0)`,
gramofone `(−6,0 · −4,0)`, arandelas, manequins, lustre, torneira.

### 11.3 Fontes de ruído

| # | Fonte | Posição | Raio | Liga | "Afinar" faz |
|---|---|---|---|---|---|
| R1 | **Gramofone travado** | Sala de Ensaio `(−6,0 · −4,0)` | 10 m | desde 0:00 | `gramofone_para`; a cortina de veludo vira a âncora |
| R2 | **Piano de armário** | Hall norte `(−1,2 · +2,0)` | 12 m | sozinho, quando o 1º móvel cruza o hall (~1:10) | `piano_abafado`; o piano dá um último acorde azedo |
| R3 | **Arandela a gás** | Corredor `(0 · +5,5)` | 14 m | desde 0:00 | `arandela_fecha`; **o corredor apaga** (trade-off real) |

### 11.4 Bilhetes do chefe espalhados (MEC-17)

| Onde | Texto |
|---|---|
| Porta de entrada | "Não quebrem nada que já esteja quebrado. Isso conta como quebrar. — V." |
| Armário de figurino | "NÃO ARRANHAR. Desconto no salário. — V." |
| Piano de armário | "O piano fica. O piano sempre fica. — V." |
| Penteadeira | "Se tiver carta na gaveta, deixem onde está." |
| Nota fiscal no corredor | "REMOÇÃO DE ENTIDADE — R$ 0,00 (cortesia)" |
| Cartaz do teatro | *(verso, letra espelhada)* "Valdemar — guarde o meu lugar. Eu volto no segundo ato. — O." |

---

## 12. Respostas às 11 perguntas do briefing

1. **Sequência de momentos** → §5. A noite passa a **300 s**, com âmbar aos 225 s.
2. **Ensinar a pista da Cida sem texto** → três lições em 60 s: o **rádio sem respiração** no
   alpendre (−0:15), a **reverência sem passos** no hall (0:45), e os **manequins** como falsos
   positivos que a obrigam a refinar a leitura.
3. **Como o espaço separa o grupo sem frustrar** → dois anéis no térreo (ninguém é mandado
   embora, você só perde o outro de vista ao contornar), corredor de 16 m com 2 m de largura,
   escada que separa por altura, e **um só beco sem saída** (camarim do suplente). Religação:
   a galeria debruça sobre o hall, o relógio é audível quase sempre, e a **chamada** (MEC-06)
   reagrupa.
4. **Espelhos** → 9 (§7.3). Heróis: **E1 e E2**, na parede oeste do hall, porque a escada corre
   na parede oposta e reflete quem sobe e desce. E7 não reflete a cena: mostra só a pista.
5. **2º andar vale o custo?** → **Sim.** É o único jeito de ter o "carregar o sofá pela escada",
   é o que cria isolamento sem corredor artificial, e é onde o roteiro põe o camarim e a
   relíquia. Custo: ~96 m² de cenário, 4 modelos de kit novos, NavMesh em dois níveis.
6. **Carregar em dupla entra?** → **Sim, é a mecânica mais importante da fase** (MEC-07).
   Sem ela, o ataque do Imitador é uma mentira de roteiro. E ela dá ao Marcelo o papel dele.
7. **Papel do Tiago e do Marcelo** → §4.2: cada um tem **um sinal exclusivo** do falso e **um
   trabalho exclusivo**. O Tiago lê o verso do cartaz no reflexo (a Cida não vê reflexo nenhum).
8. **Pistas de história, e o que só aparece no reflexo** → §3 e §7. Só no reflexo: o verso do
   cartaz, via `Reflected Layers = MirrorClue` em E7.
9. **Âncoras sonoras** → §7.4, uma por cômodo, e **nenhuma** no camarim do suplente.
10. **Comédia no espaço** → bilhetes (§11.4), manequim de colete, manequim que tomba sozinho,
    o piano que toca e a frase "Não arranhem o piano", a reverência, o "sai ofendido", o
    gingado que a cópia não copia, a xícara a mais na Copa, o lugar sem talher na mesa de 9.
11. **Perguntas que sobram para o time** → §14.

---

## 13. Ordem de implementação

Cada lote é fechado: dá para parar depois de qualquer um e o jogo continua jogável.

### Lote 1 · Planta e cenário (sem mecânica nova)
- [ ] `build_env_kit_blockout.py`: escada, guarda-corpo, porta dupla, parede com porta dupla,
      piso de tábua, laje, moldura G rachada, arandela, clarabóia, tábua solta.
- [ ] `build_props_blockout.py`: manequins A/B/C, gramofone, retratos, lustre, torneira.
- [ ] `build_furniture_blockout.py`: arara de figurino.
- [ ] `.\tools\blender\build_all.ps1` e conferir as prévias contra o Art Bible.
- [ ] `MirrorHouseBuilder.cs`: trocar a planta pela de §6 (dois andares, 9 cômodos, 12 portas,
      a escada, 9 espelhos, 8 móveis, 3 marcadores `Room` por andar).
- [ ] `Room` em cada cômodo; `SpawnPoint` ×3 no alpendre; `TruckZone` em (0, −12).
- [ ] NavMesh em dois níveis + `NavMeshLink` nas pontas da escada.
- [ ] Ficha do capítulo: `nightDuration = 300`, `openingNote` de §3.4, `ratingComments` de §3.5.
- **Pronto quando:** dá para andar a casa inteira, subir, carregar e entregar os 8 móveis.

### Lote 2 · A Dona Cida — ✅ feito em 26/10/2026 (testado com Play)
- [x] `SoundType.Tuning = 1 << 7`.
- [x] `Scripts/Echo/SurfaceTag.cs` + `Echo.shader` com `_Timbre` por material (MEC-02): madeira,
      vidro, vidro rachado, metal, tecido, pessoa e **cópia (não responde)**; as cores e durações da §8.1.
- [x] `Scripts/Echo/NoiseSource.cs` + `EchoSystem.NoiseLevel` (MEC-04): raio × Lerp(1, ⅓), borda × (1 + 2·ruído),
      vida × (1 − ruído/2). As fontes também pulsam música no eco e na vibração (o Tiago vê o gramofone).
- [x] `Scripts/Characters/TuningFork.cs` (tecla **Q**, 15 m, 8 s) + `bool tuningFork` na ficha (Cida)
      + `CharacterSwitcher.Become` + `LevelPrefabsBuilder.BuildPlayer` (MEC-01). A onda anda a 2 m/s até 8 m.
- [x] `Mirror.IsOccupied` (MEC-03): **sem rede nova** — calculado em cada computador pelo estado e a
      posição do Imitador (que já chegam a todos). O espelho responde ao diapasão com `diapasao_vidro`
      ou `diapasao_vidro_rachado`, e no eco vira "vidro rachado" (treme).
- [x] `Interactable.holdSeconds` + `hideFromOthers`; os 3 "Afinar" (MEC-05), só a Cida, segurando 2 s:
      **Gramofone** (R1, a cortina vira a âncora), **Piano** (R2, acorda quando um móvel carregado passa a
      6 m), **Arandela do Corredor** (R3, afinada **apaga**). Afinar avisa "A casa ficou mais quieta".
- [x] Chamada (MEC-06): segurar a bengala 2 s → `cida_chamada` (20 m), aviso para os três e um marcador
      no chão por 6 s (anel + luz fria), via `CaneCall` (rede). A batida comum ficou com intensidade 0,9.
- [x] `Scripts/Echo/CreakyFloor.cs` (MEC-11) no Corredor dos Camarins e em 3 tábuas da Galeria: passo vira
      rangido de 7 m, para todos (até agachado).
- [x] Manequins com `SurfaceTag` madeira (MEC-10); espelhos vidro; Imitador cópia; arara, lustre e
      arandelas metal; móveis cobertos tecido; móveis de madeira.
- [x] Âncoras (§7.4): rádio chiando no alpendre, relógio, piano, gramofone→cortina, torneira, lustre,
      arandela, vento na clarabóia, cabides; **nenhuma** no Camarim do Suplente.
- **Pronto quando:** uma pessoa com a Cida consegue atravessar a casa inteira de olhos
  fechados usando só as âncoras e a bengala, e as 3 fontes de ruído mudam o alcance dela.

**Ficou para depois:** a resposta em SOM de madeira e metal ao diapasão (só o espelho soa; o resto é
visual).

### Lote 3 · O Imitador completo — ✅ feito em 27/10/2026 (não testado com Play)
- [x] Estados novos `Performing` (em cena) e `Bowing` (reverência), no fim do `GhostState`.
- [x] **A aparição dos 45 s** (MEC-12): até ela, ele não ataca. Sai do E2 à mostra, atravessa o hall
      devagar (1,2 m/s) até o "Palco do Imitador" (1,4 · −3,0), se vira para quem está mais perto, faz a
      reverência e volta para o espelho. Lê o relógio do `GameManager` (`appearAt` no Inspector; 0 = desliga).
- [x] **A reverência** (2 s, `bowTime`): na aparição e **depois de cada captura** (ele tira o disfarce e
      agradece a plateia). Nela dá para **apontar** ("Apontar: é ele!", `Ghost.CanBeUnmasked`).
      Apontado na reverência, sai **ofendido**: foge para o espelho mais longe e a próxima espera dobra.
- [x] `GhostSounds`: captura → reverência, `imitador_reverencia`, `imitador_mascara_gira` (toda fuga),
      `imitador_sai_ofendido` (fuga saindo da reverência); o vidro trincando só quando estava disfarçado.
- [x] Blendshape `Tragedia` na máscara (mesma malha, gerado no `build_imitador_blockout.py`) e o osso
      `hat`. O `GhostVisual` curva 40° na reverência, tomba a cartola e vira a máscara enquanto ele foge.
- [x] Espera no espelho cai pela metade a partir de 75% da noite (`hurryFrom`, junto da luz âmbar).
- [x] **O manequim que tomba** aos 2:40 (`TimedTopple`) no Corredor dos Espelhos, ao comprido do
      corredor: som `manequim_tomba`, batida no eco e na vibração, aviso "Algo de madeira caiu no corredor."
- **Pronto quando:** a aparição dos 45 s acontece sozinha e a reverência dá uma janela real
  de contra-ataque.

**Revisto depois do 2º Play (27/10/2026): o Imitador MORA no espelho.** Ele passa a noite **dentro** de
um espelho (`GhostState.InMirror`: invisível, sem colisor) e troca de espelho a cada 18–32 s. Só sai com
**interesse**: a espera acabou (`lurkTime`) ou alguém arrasta **sozinho** um móvel de dupla por 2,5 s
(ele sai para "ajudar a carregar"). Antes de sair, passa para o espelho mais perto da vítima e espera
3 s (`emergeDelay`). **Só a Cida sabe onde ele está:** no eco dela o espelho ocupado respira um brilho
esverdeado fraco, mesmo sem onda (o mesmo timbre "vidro rachado" do diapasão). Os outros veem um
espelho comum. Desistiu do ataque: volta **andando** disfarçado até um espelho (sumir na frente de todos
o entregaria). A aparição dos 45 s continua igual.

**A voz** (voz Bill do ElevenLabs, modelo `eleven_multilingual_v2`, escolhida de ouvido; `GhostSounds` no
prefab). Ele é o ator vaidoso que morreu suplente, e cada momento tem o seu repertório. As falas são
escritas como um brasileiro fala (no máximo uma reticência, no fim; "pra"), sem engrossar o áudio: as
pausas no meio da frase e o áudio mais lento soavam robóticos, e a 1ª voz (Callum) tinha sotaque
americano. Vozes brasileiras nativas da biblioteca do ElevenLabs (ex.: "Adriano – Deep, Gravelly")
exigem plano pago na API. **O tratamento de fantasma** (`"assombrar"` no catálogo, feito pelo
`gerar_sons.py`): o tom 2 a 4 semitons abaixo **sem** arrastar a fala, uma cópia uma oitava abaixo bem
baixa por baixo (o peso), reverb de teatro vazio e, antes da fala, o **eco ao contrário** (um sopro que
cresce até a primeira palavra). No espelho é o mais etéreo (mais eco, sopro de 1,2 s); com raiva
(desmascarado, ofendido) é seco e imediato, sem sopro. **A voz de "rádio"** que tocava quando o falso
pedia ajuda era o `imitador_voz` (padrão de voz da primeira leva de sons): no Imitador ela ficou muda
(`SoundVoice` com som vazio); o pedido continua no eco, para os fantasmas e no texto. Nenhuma variação repete antes de todas tocarem (o baralho do
`SoundLibrary`), e ele não fala durante o pânico (só o zumbido do espelho).

| Momento | Falas | Como toca |
|---|---|---|
| Dentro do espelho | "Minha casa...", "Minha apresentação...", "Minha fama...", "Minha casa, minha apresentação, minha fama...", "Eu sei todas as falas.", "Era pra ser eu.", "Silêncio na plateia...", "De novo. Do começo.", "Ensaiei a vida inteira pra isso.", "Ninguém sai antes do aplauso.", "Quem vai ser o próximo a subir no palco?" | sem direção (a casa sussurra); a 1ª depois de 30 s no espelho, depois a cada 25–47 s; no último quarto, 16–30 s. É clima: espera a vez (uma ameaça por vez) |
| Prestes a sair (estado `Emerging`, 3 s) | "É a minha deixa.", "Atenção, vou entrar em cena.", "Luzes..." | sem direção, em **metade** das vezes |
| A aparição | "Senhoras e senhores...", "Sejam bem-vindos à minha casa.", "Hoje o espetáculo sou eu." | ao sair e 6 s depois; na reverência, "Obrigado... obrigado." |
| Depois de uma captura | "Mais um pro elenco.", "Agora você é o meu reflexo.", "Que bela estreia." | 1 s depois do som da captura |
| Desmascarado (ou largou a vítima segurada) | "Errei a fala.", "Vocês não sabem reconhecer um artista!" | com raiva, no lugar do vidro trincando e da máscara girando |
| Apontado na reverência | "Plateia ingrata!", "Isso não vai ficar assim." | no lugar do "sai ofendido" (a cortina) |

**Ficou para depois:** as cadeias de mola da cortina (o "sai ofendido" é só som e máscara por enquanto)
e a animação de verdade da reverência (hoje é o corpo inteiro inclinando, sem Animator).

### Lote 4 · Carregar em dupla — ✅ feito em 27/10/2026 (não testado com Play)
- [x] Peso e dupla (MEC-07), **para o jogo todo**: no lugar do `minCarriers`, cada um levanta sozinho
      até a **própria força** (Cida 1, **Tiago 1,5**, Marcelo 3) e a dupla até a soma. O **armário passou
      a 3,5**. Na casa: cadeiras (1) qualquer um; arara e caixa M (1,5) o Tiago ou o Marcelo; caixa G,
      cartaz (2) e penteadeira (2,5) o Marcelo, ou Cida + Tiago; o armário (3,5) **só com o Marcelo e mais
      alguém**.
- [x] `FurnitureCarrier`: "Segurar a outra ponta" (o ajudante), o móvel vai para o meio das duas mãos,
      a força da dupla se soma, a dupla anda no passo do mais lento. Sem gente: **arrasta** (no chão,
      35% da velocidade ÷ o excesso de peso: a Cida arrastando o armário anda a 10%, o Tiago com a caixa G
      a 26%; `arrastar_movel` a 10 m). Colega pegando a ponta: o "toc" `movel_dupla_apoio`.
- [x] A isca (MEC-08): a cópia chega em quem arrasta sozinho um móvel de dupla, diz "Deixa que eu pego a
      outra ponta!" e vira a **ponta falsa** por 4 s (o nome aparece em "Soltar · Tiago na outra ponta").
      As três pistas: o móvel **continua arrastando** (peso), **não sai do chão** (visão) e **não faz o
      "toc"** do apoio (som). Depois puxa.
- [x] 6 s para soltar (MEC-09): o puxão leva a vítima aos 6 s (chegou antes no espelho, espera). A HUD
      da vítima conta os segundos. Salva por **apontar** ou por um colega **segurando a vítima**
      ("Segurar fulano", segure E): segurada, o puxão e o tempo param; 1,5 s segurando, o falso larga e foge.
- [x] Escada (MEC-13): móvel com peso acima de 2 em chão com mais de 15° anda ÷ 1,5, e cada 0,7 m é um passo
      rangendo de 9 m.
- **Pronto quando:** dá para descer o armário em dupla pela escada, e a cópia consegue
  enganar alguém a segurar a outra ponta no meio do lance.

**Diferente do documento:** a âncora é **segurar a vítima**, não "um terceiro segurar o mesmo móvel"
(quem é puxado larga o móvel na hora). O degrau usa o `passo_madeira_rangendo` que já existe, não um
`escada_degrau` novo. **Ficou para depois:** **arrastar gastar estamina** (decidido: quem arrasta
cansa; hoje só fica lento), os pontos de pega `SOC_Carry_A/B` nos modelos (hoje o
ajudante segura "do lado dele"), a inclinação do móvel na dupla, e o Bibliotecário ouvir o arrastar
(o prefab dele tem a lista de sons marcada à mão, sem o tipo novo `Carry`).

### Lote 5 · Som e acabamento
- [ ] 26 sons de §10 no `catalogo.json` + `gerar_sons.py` + Atualizar Biblioteca de Sons.
- [ ] `AmbientSound` com as âncoras de §7.4 em cada cômodo.
- [ ] Reflexo atrasado (MEC-14) e sem sombra (MEC-15).
- [ ] Lanternas no caminhão, 2 unidades (MEC-16).
- [ ] Bilhetes com verbo "Ler" (MEC-17); gingado do Marcelo (MEC-18).
- [ ] Cartaz: verso com decal espelhado na camada `MirrorClue`; E7 com `Reflected Layers`.
- **Pronto quando:** dá para jogar do bilhete de abertura até a avaliação, e um jogador que
  nunca viu o jogo entende sozinho por que a Cida é importante.

---

## 14. Riscos e perguntas em aberto

| # | Questão | Por que importa | Quem decide |
|---|---|---|---|
| 1 | **300 s é o número certo?** | Se sobrar muito tempo, a tensão some; se faltar, a Cida nunca afina nada | playtest com 3 pessoas |
| 2 | **Dois reflexos "herói" bastam num hall de pé-direito duplo?** | O quadro-chave depende de E1 e E2 refletirem ao mesmo tempo | perfil de GPU |
| 3 | **"Afinar" é interação de segurar — e segurar com o fantasma por perto?** | Pode virar punição injusta | design; alternativa: toque único com 1 s de animação |
| 4 | **Apagar a arandela do corredor é divertido ou é só ruim?** | É o trade-off mais arriscado do documento | playtest; alternativa: a arandela vira luz fraca em vez de apagar |
| 5 | **O Imitador sobe a escada?** | Exige `NavMeshLink`; sem isso o 1º andar é zona segura | código |
| 6 | **Arrastar sozinho um móvel de 2 pessoas entra?** | Evita o softlock de jogar com 2 pessoas em vez de 3 | design |
| 7 | **A fase funciona com 2 jogadores?** | A sala de espera permite | testar: com 2, cortar 1 móvel de dupla |
| 8 | **Acabamento 3D** (decisão 1 do PLANEJAMENTO.md) | O `SurfaceTag` do diapasão assume cor chapada legível sem textura | time |

---

## 15. O que atualizar nos outros documentos

| Arquivo | O quê |
|---|---|
| `PLANEJAMENTO.md` | Fase 3 volta para "em revisão"; lotes 1–5 deste documento entram como subitens; `SoundType.Tuning` e carregar em dupla viram pré-requisitos da Fase 6 |
| `ART_BIBLE.md` | tabela "Situação": 10 modelos de kit/props novos; arara de figurino; blendshape do Imitador |
| `CLAUDE.md` | "Armadilhas conhecidas": NavMesh em dois níveis precisa de `NavMeshLink` na escada |
| `tools/audio/catalogo.json` | 26 sons + a mixagem de §10.1 |
| Ficha do capítulo 1 | `nightDuration`, `openingNote`, `ratingComments` |
