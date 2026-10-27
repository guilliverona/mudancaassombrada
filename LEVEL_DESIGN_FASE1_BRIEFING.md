# Briefing de level design — Fase 1: A Casa do Espelho

Documento para quem vai **projetar** a fase (level design e roteiro da fase), fora do código.
Tudo o que está aqui foi tirado do repositório em 26/10/2026: os números vêm do código e das
fichas do jogo, não de suposição. Onde o desenho original (roteiro, Art Bible) e o jogo atual
não batem, está marcado como **lacuna**.

Fontes: [roteiro.txt](roteiro.txt), Art Bible completo (pranchas ENV_01, GHO_01 e PRP),
[ART_BIBLE.md](ART_BIBLE.md), [PLANEJAMENTO.md](PLANEJAMENTO.md), [ARQUITETURA.md](ARQUITETURA.md),
[DESIGN.md](DESIGN.md) e os scripts em `MudancaAssombrada/Assets`.

---

## 0. O que se espera de volta

Um **documento de level design da Fase 1** que dê para implementar sem adivinhar. Ele deve terminar com:

1. **Ritmo da fase** (beat chart): os momentos em ordem, com tempo aproximado dentro dos 180 s
   da noite (ou a proposta de outra duração), e o que cada personagem está fazendo em cada um.
2. **Planta** no grid de 2 m, com medidas: cômodos, portas, passagens, escada (se houver 2º andar),
   onde fica cada móvel, cada espelho, o caminhão, o ponto de nascimento e as rotas até o caminhão.
3. **Tabela de cômodos**: nome, tamanho, função no jogo, qual sentido "lê" o cômodo (eco, vibração,
   visão), móveis, espelhos, luzes, sons e pistas de história.
4. **Lista do que é novo**: modelos (com medidas), sons (com uma frase descrevendo cada um),
   mecânicas que o código ainda não tem.
5. **Perguntas em aberto** que precisam do time.

O que **não** precisa: código, nomes de scripts, detalhes de rede. Isso é feito depois, no repositório.

---

## 1. O jogo em um minuto

- **Mudança Assombrada**: co-op online de **até 3 jogadores**, terror **com comédia**, em primeira pessoa.
  Uma transportadora falida (Transportes Descanso Eterno Ltda.) tira móveis de casas mal-assombradas
  **antes do amanhecer**. O chefe, Seu Valdemar, só fala por bilhetes; no fim, descobre-se que é um fantasma.
- **Contraste que move o humor:** burocracia chata (bilhetes, notas fiscais de "remoção de entidade",
  avaliação com estrelas) contra terror absurdo.
- **Cada personagem percebe o mundo de um jeito**, e um precisa traduzir o mundo para o outro:

| | Dona Cida | Tiago | Marcelo "Marreta" |
|---|---|---|---|
| Condição | cega | surdo | mudo |
| Como joga | tela escura; "vê" pelo **eco** dos sons (ondas que revelam paredes e objetos) e bate a **bengala** | **sem som nenhum**; vê **vibrações** (anéis no chão nos passos, batidas, apito) | forte e silencioso; carrega peso sozinho; se comunica por gestos e **apito** |
| Altura / olhos | 1,52 m / 1,09 m (passa sob uma mesa de 1,6 m) | 1,74 m / 1,52 m | 1,98 m / 1,74 m |
| Velocidade | normal | normal | 0,85 (um pouco mais lento) |
| Força | 1 | 1 | 3 (carrega qualquer móvel atual sem perder velocidade) |
| Passos | volume normal | volume normal | metade do volume |

**A Fase 1 é a fase da Cida.** O espaço precisa recompensar quem **ouve**: o Imitador copia a
aparência, mas não tem passos nem respiração, e é pelo eco que a Cida descobre o falso.

---

## 2. A fase no roteiro (o que existe hoje)

> Uma casa antiga cheia de espelhos, onde morava uma família de atores. O fantasma, Seu Reflexo,
> copia perfeitamente a aparência dos jogadores. De repente existem dois Tiagos, dois Marcelos…
> e um deles quer te levar para dentro do espelho.

- **Como ataca:** chega disfarçado de amigo, pede "segura a outra ponta do armário" e puxa para o reflexo.
- **Por que a Cida brilha:** no eco, o Imitador aparece sem passos e sem respiração. Ela aponta qual é o falso e guia a equipe.
- **Momento engraçado:** o Imitador copia tudo, menos o andar desengonçado do Marcelo carregando peso.
- **Pista do chefe:** num camarim, um cartaz de teatro antigo com "Valdemar" no elenco.
- **Ligação com a história maior:** o Imitador foi **colega de teatro** do Valdemar. O cartaz é a
  **relíquia 1 de 3**; na fase final ele precisa ser carregado para fora (pesa mais quanto mais longe da porta).
- **Bilhete de abertura (rascunho atual):** "Casa de uma família de atores. Muitos espelhos. Se alguém
  pedir ajuda com o armário, confiram se é mesmo alguém da equipe."

O roteiro da fase tem só isso. **Expandir é parte do trabalho:** a família de atores (quem eram,
o que aconteceu), o que a casa conta sobre eles, como o Valdemar aparece nas pistas, os textos
do bilhete, da avaliação final e dos comentários do chefe para cada personagem.

---

## 3. O Imitador (ficha GHO_01 do Art Bible)

- **2,30 m, flutua**, forma de "lâmina vertical" (4 cm de perfil). Máscara de porcelana, cortina de
  veludo vinho, luvas de dedos longos demais, olhos ciano (os únicos pontos ciano). Sem sombra, sem pegadas.
- **Tique cômico:** faz uma reverência teatral depois de cada imitação bem-sucedida. Desmascarado,
  a máscara gira de comédia para tragédia e ele sai de cena "ofendido", pela cortina de um espelho.
- **Estados de IA (Art Bible):** 01 patrulha (salta de espelho em espelho) → 02 disfarce (copia o
  jogador mais afastado do grupo) → 03 isca ("segura a outra ponta aqui") → 04 captura (puxa para o
  reflexo; **amigos têm 6 s para soltar**) → 05 desmascarado (máscara trágica, foge para outro espelho).
- **Sinais do disfarce (legíveis por design):**
  - **logo** do colete espelhado;
  - **eco**: sem passos e sem respiração;
  - **vibração**: bordas tremem como superfície de espelho;
  - **andar**: não copia o gingado do Marcelo carregando peso;
  - **sombra**: nenhuma, e o reflexo vem 0,3 s atrasado.

### Como o Imitador funciona no jogo hoje

| Momento | O que acontece | Números |
|---|---|---|
| Escondido | parado na frente de um espelho, sussurrando de vez em quando | espera **12 s** |
| Disfarce | sai do espelho **mais perto da vítima**, já com o corpo do jogador **mais isolado** (maior distância média até os outros) | anda a **3,5 m/s** (quase o andar normal, 4) |
| Isca | perto da vítima, pede ajuda **com voz** | a **2 m**; a voz alcança **6 m** |
| Puxar | arrasta a vítima até o espelho | **1,2 m/s**, a vítima vai a 1,2 m atrás |
| Captura | chegou no espelho: a vítima fica **caída** até um colega ajudar | — |
| Desmascarado | qualquer jogador **aponta** para ele (tecla Interagir, alcance 2,5 m): larga a vítima e foge para o espelho **mais longe** | foge a **5 m/s** |
| Desiste | disfarçado sem alcançar ninguém | depois de **40 s** volta a se esconder |

- Ele **não emite passos nem respiração** (a pista da Cida existe de verdade no código).
- O aviso de apontar ("é falso!") chega à equipe num raio de **12 m**.
- **Sons dele hoje:** sussurro (~a cada 9 s, ouvido a 8 m), zumbido do espelho enquanto puxa,
  som de captura e vidro trincando ao ser desmascarado. Os sons de ameaça abaixam o fundo da casa.

**Lacunas:** os 6 s para soltar a vítima (hoje só desmascarando), sem sombra, reflexo atrasado,
bordas tremendo na vibração, reverência e máscara (animações), o gingado do Marcelo.

---

## 4. Regras da partida

- **Objetivo atual:** "Entregar todos os móveis" (todos os móveis da cena na zona do caminhão).
  O jogo aceita **etapas** de objetivos (uma etapa libera a próxima) e outros tipos: chegar a uma
  área (a equipe toda ou um só), usar objetos (com restrição de quem pode usar, ex.: só o Tiago).
- **Tempo:** a noite dura **180 s** (configurável por capítulo). A luz é o relógio: fria e azulada,
  vira âmbar a partir de **75% da noite** (aos 135 s).
- **Fim:** vence ao cumprir os objetivos; perde no **amanhecer** ou se **todos caírem**.
  Quem cai fica caído até um colega chegar perto e **ajudar** (alcance 2,5 m).
- **Avaliação:** de 1 a 5 estrelas pelo tempo que sobrou (mais tempo = mais estrelas), e um comentário
  do chefe para cada personagem. Cada capítulo também tem um **pagamento** (padrão 100).
- **Pista de história:** pegar o cartaz grava um marcador no save. Hoje é **opcional**, não um objetivo.
- **Avisos na tela:** cada jogador recebe só o que o **seu** personagem percebe (o Tiago não recebe
  falas; a Cida não recebe o que só se vê). Até 4 linhas, 6 s cada.

---

## 5. Verbos do jogador e números

| Verbo | Números |
|---|---|
| Andar / correr / agachar | 4 m/s / 7,2 m/s / 2 m/s. Correr gasta estamina: **4 s** de corrida, recupera em ~7 s, e esgotada só volta a correr com 30%. Agachado: 1,2 m de altura. |
| Carregar móvel | **um jogador por móvel** (lacuna, ver abaixo). Pega a até 2,5 m, segura 1,8 m à frente. Velocidade carregando = velocidade ÷ (peso ÷ força), nunca mais que a normal. |
| Bengala (Cida) | onda de eco de **12 m**, a cada 0,6 s no máximo. A onda anda a 7 m/s (chega a 12 m em ~1,7 s). Até 8 ondas ao mesmo tempo na tela. |
| Apito (Marcelo) | som de **15 m**, a cada 4 s. Atrai o fantasma (serve de distração). |
| Apontar | para um disfarçado a até 2,5 m: desmascara. |
| Ajudar | colega caído a até 2,5 m. |

**Velocidade carregando cada móvel atual** (andando, m/s):

| Móvel (peso no jogo) | Cida / Tiago | Marcelo |
|---|---|---|
| Cadeira (1) | 4,0 | 3,4 |
| Caixa (1,5) | 2,7 | 3,4 |
| Penteadeira (2,5) | 1,6 | 3,4 |
| Armário de figurino (3) | 1,3 | 3,4 |

**Lacuna importante:** o Art Bible define os móveis por **massa e por quantas pessoas carregam**
(armário de figurino 80 kg para 2 jogadores; penteadeira 45 kg para 2), e o próprio ataque do
Imitador depende de "segurar a outra ponta". **No jogo, só um jogador segura cada móvel** (se dois
pegam ao mesmo tempo, o primeiro leva). Decidir se a fase pede carregar em dupla é decisão de
design com impacto no código.

---

## 6. Sentidos e som: o que cada um percebe, e a que distância

O jogo tem **eventos de som** (regra: quem ouviu e até onde) e o **áudio** (o que sai da caixa de
som). São coisas diferentes, e as duas importam para o level design.

**Eventos (regra do jogo; o eco da Cida e os ouvidos dos fantasmas usam isto):**

| Som | Alcance | Observação |
|---|---|---|
| Passo andando / agachado / correndo | 2,5 / 1 / 7 m | um passo a cada 1,2 m. Marcelo: metade |
| Respiração | 1,5 m (dobra correndo) | a cada 3 s. O Imitador **não** respira |
| Bengala | 12 m | — |
| Apito | 15 m | — |
| Móvel batendo | 3 a 10 m conforme a força | o Marcelo carregando faz 40% do barulho |
| Voz do Imitador | 6 m | — |
| Passos de fantasma | 3,5 m | o Imitador não tem |

Fantasmas que caçam pelo som (não é o caso do Imitador) ouvem 1,5× o alcance do evento.

**Vibração (Tiago):** anéis no chão para passos, batidas, bengala, apito e música; somem em 2,5 s.

**Áudio (o que se ouve, até onde):** passos 9 a 14 m, bengala 18 m, respiração 4 a 7 m, sussurro do
Imitador 8 m, relógio 9 m. O **relógio de pêndulo** da sala bate um tique ou taque por segundo e é o
**metrônomo** da casa: respiração e sussurros entram entre as batidas. Ele já é uma **âncora sonora**
para a Cida se localizar, e o level design pode usar mais âncoras assim (cada cômodo com um som próprio).

**Espelhos com reflexo:** os **2 espelhos mais perto e na tela** refletem a cena de verdade (até 12 m).
Os outros, e todos no modo cego da Cida, ficam escuros. Dá para escolher **o que aparece no reflexo**:
é o gancho para **pistas que só existem no espelho** (algo que só o Tiago ou o Marcelo veem e precisam
contar para os outros, por exemplo).

---

## 7. Regras de construção (o que cabe no kit)

- **Kit modular:** parede 2 × 2,8 m (espessura 0,15 m), parede com porta, porta 0,90 × 2,10 m,
  piso xadrez 2 × 2 m, molduras de espelho P/M/G (0,6 / 1,2 / 2,0 m). Tudo encaixa num **grid de 2 m**.
- **Art Bible pede também:** porta dupla 1,60 m, corredor de pelo menos **1,60 m** (carregar em dupla),
  escada reta de no máximo **32°**.
- **Navegação do fantasma:** passagens de **2 m** entre cômodos. O mapa de navegação encolhe perto das
  paredes; vão estreito o fantasma não atravessa (isso também é ferramenta de design).
- **Móveis precisam passar** pelas portas e corredores até o caminhão. Medidas (Art Bible):
  armário de figurino 1,0 × 0,6 × 2,0 m; penteadeira 1,2 × 0,5 × 1,6 m; relógio de pêndulo
  0,55 × 0,32 × 2,1 m (fixo, não sai); caixas 0,4 / 0,5 / 0,6 m.
- **Orçamento visual:** módulo de cenário até 1,5 mil triângulos; móvel grande 3 a 6 mil.
  Os modelos são gerados por script, com formas simples e cores chapadas (o acabamento final ainda
  está em decisão).

---

## 8. Estilo e ambientação (Art Bible, ENV_01)

- **Casarão dos anos 1920** de uma família de atores. Papel de parede **vinho**, molduras **douradas**,
  piso **xadrez**. "Todo espelho é uma porta para o Imitador."
- **Luz:** ambiente quase zero; a luz principal são **lanternas quentes (3200 K)** e **arandelas a gás
  piscando**. No jogo, a noite é fria e azulada e vira âmbar no amanhecer. Luz quente = segurança.
- **Reflexos:** 2 espelhos "herói" que refletem de verdade; os demais, só sugeridos.
- **Props-chave:** armário de figurino (2 pessoas), penteadeira de camarim, **manequins**, o cartaz.
- **Layout:** salas **em anel** (para o "dois Tiagos": dá para dar a volta e se perder do grupo);
  **camarim no 2º andar** guarda a relíquia.
- **Pistas espalhadas** (em toda fase): fotos, cartas e o sobrenome da família repetidos.
- **Organização da cena proposta pelo Art Bible:** estático, iluminação, móveis, interativos, relíquias,
  âncoras do fantasma (espelhos, rotas), áudio (zonas de eco, ambientes), percepção (pisos que rangem,
  fontes de vibração), navegação, caminhão, pistas.
- **Princípio do Art Bible:** "carisma pela forma, terror pela distorção, leitura pelos sentidos".
  A equipe é redonda, quente e gasta; os fantasmas são altos, frios e "errados". Tudo precisa
  funcionar também **sem textura**, porque é isso que o eco da Cida mostra.

---

## 9. O rascunho atual da casa

Térreo só, **12 × 12 m**, peças de 2 m. O caminhão fica na rua, a 11 m do centro (5 m além da parede da frente).

```
        z=+6  +-------------------+-------------------+
              |  Camarim          |  Corredor dos     |
              |  (penteadeira,    |  Espelhos         |
              |   cartaz, armário) (passagem 2 m)     |
        z= 0  +-- passagem 2 m ---+-- passagem 2 m ---+
              |  Sala (cadeira, caixas, relógio)      |
        z=-6  +------- saída 4 m (frente) ------------+
                          caminhão (z = -11)
```

- **Anel:** Sala → Camarim → Corredor → Sala (já existe).
- **Espelhos (5):** grande no camarim (parede oeste), grande no fundo do corredor, médio na lateral
  do corredor, médio na sala, pequeno na entrada. O Imitador começa no do camarim.
- **Móveis (5):** penteadeira e armário de figurino no camarim; cadeira e uma caixa na sala; outra
  caixa no corredor. **Relógio** fixo no canto oeste da sala.
- **Nascimento:** 3 pontos lado a lado na sala, perto da saída.
- **Porta fechada** no fundo do camarim (só cenário).
- **Sons:** clima da casa (fundo) e o relógio.

---

## 10. Lacunas conhecidas (desenho original × jogo atual)

| Tema | Desenho (roteiro / Art Bible) | Jogo hoje |
|---|---|---|
| Andares | camarim no 2º andar, escada ≤ 32° | só térreo |
| Carregar | móveis de 2 pessoas, "segura a outra ponta" | 1 pessoa por móvel |
| Captura | amigos têm 6 s para soltar | só desmascarando |
| Imitador | sem sombra, reflexo atrasado, bordas tremem, reverência, máscara | não tem |
| Momento engraçado | não copia o gingado do Marcelo carregando peso | não tem |
| Luz | lanternas 3200 K, arandelas a gás piscando | só lua e a luz da noite |
| Props | manequins | não tem modelo |
| Pistas | fotos, cartas, sobrenome repetido | só o cartaz |
| Som por cômodo | zonas de eco (o hall com eco longo) | não tem |
| Relíquia | cartaz de 30 kg, achado no camarim | pista opcional que se "pega" |

---

## 11. Perguntas que o documento deve responder

1. Qual é a **sequência de momentos** da fase em 180 s (ou quanto tempo ela deveria ter)?
2. Como ensinar a **pista da Cida** ("sem passos e sem respiração") sem texto, logo no começo?
3. Como o espaço **separa o grupo** (para o Imitador ter quem copiar) sem frustrar?
4. Quantos **espelhos**, onde, e o que se vê de cada um? Quais são os 2 "heróis"?
5. **2º andar:** vale o custo? O que ele muda no ritmo (carregar pela escada é o "momento clássico")?
6. **Carregar em dupla:** entra nesta fase? É a base do ataque do Imitador.
7. Qual é o papel do **Tiago** e do **Marcelo** numa fase que é da Cida?
8. Que **pistas de história** a casa conta, e quais aparecem **só no reflexo**?
9. Que **âncoras sonoras** cada cômodo tem para a Cida se orientar?
10. Como a **comédia** aparece no espaço (bilhetes, objetos, o gingado do Marcelo)?
