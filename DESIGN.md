# Padrão visual — Mudança Assombrada

> **Terror de mentirinha.**
> É de noite, a casa é assombrada e o chefe é um fantasma, mas quem está ali são três
> carregadores atrapalhados com uma caixa pesada. O escuro dá o clima; o papelão, a fita
> e os bilhetes do Seu Valdemar dão a graça.
> Na partida, continua valendo: na dúvida, a opção mais escura, mais quieta e mais simples.

Como o código está organizado: [ARQUITETURA.md](ARQUITETURA.md).
Personagens, fantasmas, cenários e pipeline de modelos 3D: [ART_BIBLE.md](ART_BIBLE.md).
Planejamento em fases: [PLANEJAMENTO.md](PLANEJAMENTO.md).

Os valores (cores, tamanhos, durações) ficam no código em
[`Assets/Scripts/Theme.cs`](MudancaAssombrada/Assets/Scripts/Theme.cs).
Este documento explica **as regras**; o `Theme` guarda **os números**.
Se mudar um, atualize o outro. Os mesmos valores, com prévias das peças e das telas do menu,
estão no design system "Mudança Assombrada" (artefato no Claude; os nomes entre parênteses abaixo são os de lá).

---

## 1. Cores

Poucas cores, cada uma com um papel fixo.

| Papel | Nome no `Theme` | Uso |
|---|---|---|
| Fundo | `BackgroundCenter` / `BackgroundEdge` | vinheta azul-noite das telas (noite-800 → noite-900) |
| Painel | `PanelColor` | caixa atrás de listas e formulários (noite-700) |
| Campo | `FieldColor` | campos, cartões, trilho do slider (noite-600) |
| Elevado | `RaisedColor` | hover de superfícies escuras (noite-500) |
| Borda | `BorderColor` | contorno fino, linha de corte dos painéis |
| Sombra | `ShadowColor` | sombra dura de "brinquedo" debaixo de botões e bilhetes |
| **Papelão** | `CardboardColor` (+ `Light` / `Dark`) | botão principal da tela e título |
| Fita | `TapeColor` | decoração de papel; nunca atrás de texto |
| Bilhete | `NoteColor` / `NoteInkColor` | papel e caneta dos bilhetes do Seu Valdemar |
| Título | `TitleColor` | = papelão |
| Texto | `TextColor` | todo texto normal |
| Nota | `MutedColor` | dicas, status, textos secundários, placeholders |
| Tinta da caixa | `CardboardInkColor` | texto sobre papelão |
| **Destaque** | `HighlightColor` (+ `HighlightMistColor`) | verde-fantasma ("ectoplasma"): hover, foco, selecionado, texto digitado |
| **Perigo** | `DangerColor` | fantasma, erro, tempo acabando (sempre com palavra junto) |
| **Amanhecer** | `DawnColor` | progresso, vitória, caminhão |

**Personagens:** Cida (roxo), Tiago (amarelo), Marcelo (verde), no campo **Cor** da
ficha de cada um (`CharacterDefinition.color`, em `Assets/Data/Personagens`). Na interface, só em detalhes (nome, ícone,
contorno), sempre "domadas" com `Theme.Muted(cor)`. Nunca em áreas grandes, e nunca só a cor
(roxo e verde se confundem em daltonismo): cor + nome ou inicial.

**Regra de ouro:** cor viva só quando significa algo. O papelão é a única cor "de marca":
marca a ação principal, então **um** botão de papelão por tela.

Todo texto passa 4,5:1 de contraste sobre o fundo em que aparece (os pares estão no design system).

## 2. Tipografia

| Fonte | Onde | Arquivo |
|---|---|---|
| **Saira Stencil One** | só o título do jogo (letra de carimbo de caixa) | `GameFonts.Stamp` |
| **Bebas Neue** | cabeçalhos, botões, números grandes (cronômetro, código da sala) | `GameFonts.Title` |
| **Poppins** | frases, dicas, listas, HUD | `GameFonts.Body` |
| **Poppins Bold** | ênfase dentro de texto | `GameFonts.Bold` |
| **Permanent Marker** | bilhetes do Seu Valdemar e subtítulos de botão | `GameFonts.Hand` |

Bebas Neue só tem maiúsculas: "Jogar" aparece como "JOGAR" sozinho.
A fonte de caneta é só para a graça: nunca para algo que o jogador precisa ler para jogar.

Tamanhos (tela 1920×1080). **Nada fora desta lista:**

| Nome | Tamanho | Uso |
|---|---|---|
| `TitleSize` | 150 | título do menu |
| `HeaderSize` | 48 | cabeçalho de painel ("PAUSA"), carimbo, título pequeno |
| `ButtonSize` | 36 | botões |
| `HandSize` | 28 | bilhetes, subtítulo do menu |
| `BodySize` | 26 | textos normais |
| `NoteSize` | 20 | notas, status, dicas pequenas, subtítulo de botão |

## 3. Tom de voz

- Informal, prático, com cara de empresa pequena e falida. A instrução vem primeiro; a piada seca, depois.
- Botões do menu têm um subtítulo à mão: **JOGAR** · *turno da madrugada*, **SAIR** · *pedir as contas*.
- O Seu Valdemar escreve bilhetes curtos, assina "— V.", é mão de vaca e se preocupa mais com os
  móveis que com os funcionários. As frases ficam em `ValdemarNotes.cs`.
- Sem emoji, sem "Ops!". Erro diz o que aconteceu antes de brincar.

## 4. Componentes

Toda interface sai do [`UiBuilder`](MudancaAssombrada/Assets/Editor/UiBuilder.cs)
(a HUD da partida é montada pelo `HudBuilder` com as mesmas peças).
**Nunca crie UI "na mão"** fora dele: se faltar uma peça, adicione ela lá.
Nada de `OnGUI`: ele não segue o `Theme`.

- **Botão** (`CreateButton`, três estilos):
  - `Cardboard`: etiqueta de papelão com fita no canto e sombra dura. A ação principal (só uma por tela).
  - `Dark`: o padrão, escuro com contorno fino.
  - `Ghost`: voltar/cancelar/atualizar, transparente até o mouse passar.
  - Hover/foco: contorno e texto em ectoplasma. Apertado: a sombra some (o botão "desce o degrau").
  - Subtítulo opcional à mão (`subtitle`).
- **Painel:** caixa escura, sem sombra, com uma linha tracejada de "corte aqui" no topo.
  Cabeçalho = **carimbo** (`CreateStamp`): Bebas em papelão, com moldura, torto 1°.
  No menu inicial, os botões ficam soltos (sem painel).
- **Bilhete** (`CreateNote`): papel amarelo torto, preso com fita, assinado "— V.". Um por tela no máximo.
- **Telas de rede:** a inicial já separa os dois caminhos. **Criar sala** (nome, pública ou só com código,
  botão de papelão) e **Entrar numa sala** (lista de salas com busca e páginas, uma linha por sala com o
  próprio ENTRAR, e o código embaixo com COLAR). As regras da lista ficam no `RoomBrowserRules`.
- **Campo de texto / slider / checkbox:** fundo `FieldColor`, contorno fino, detalhes em `HighlightColor`.
- **Dica de tecla:** `[E] Pegar`, com a tecla numa caixinha com borda. Sempre o mesmo formato.
- **Aviso na tela:** texto centralizado que aparece e some com fade
  ("O fantasma ouviu algo...").

## 4.1 Movimento

- Fade (aparecer/sumir) e deslizes curtos. `FadeFast` (0,15 s) para hover; `FadeSlow` (0,3 s) para trocar de tela e avisos.
- **Só no menu:** o botão balança um pouco no hover (`HoverTilt`, ligado pelo `wobble` do `ButtonHover`),
  e um fantasma de lençol bem apagado atravessa o fundo de vez em quando (`MenuGhost`). Nada mais se mexe sozinho.
- **Na partida (HUD e pausa) nada balança.** A única exceção continua sendo o perigo: fantasma perto
  ou tempo acabando pode tremer ou piscar. Como nada mais se mexe, isso assusta de verdade.

## 5. HUD da partida

- Mínima: cronômetro e móveis entregues num canto, pequenos, em `TextColor`.
- A informação importante fica **no mundo, não na tela**: ondas de eco,
  vibrações no chão, a sombra do fantasma.
- Cada personagem percebe o mundo de um jeito. A HUD também:
  - **Cida (cega):** tela escura; a "HUD" é o som e o pulso do eco.
  - **Tiago (surdo):** legendas visuais dos sons ("passos à esquerda").
  - **Marcelo (mudo):** sinais/gestos para os outros em vez de fala.

## 6. Som da interface

Clique abafado (como madeira), hover quase inaudível, erro grave e curto.
Nada de "plim" alegre.

## 7. Mundo 3D

Personagens, fantasmas, cenários e as regras de modelagem estão no [ART_BIBLE.md](ART_BIBLE.md).

- Low-poly, cores chapadas, pouca textura (como o boneco de blocos e os blockouts atuais).
  **Em aberto:** o Art Bible propõe material estilizado com PBR e rampa de luz; o time
  decide qual dos dois vale para a arte final (ver [ART_BIBLE.md](ART_BIBLE.md#4-decisões-em-aberto)).
- Iluminação fria e azulada à noite, ficando âmbar (`DawnColor`) conforme o
  amanhecer chega: a própria luz é o relógio.
- Neblina escura para esconder o fundo (e economizar modelagem).

---

## Checklist para qualquer tela nova

- [ ] Usa só cores do `Theme`? Cor viva só com significado? Um botão de papelão só?
- [ ] Usa só os tamanhos de texto da lista?
- [ ] Foi feita pelo `UiBuilder`?
- [ ] Animações só com fade/deslize curto (exceto o balanço do menu e o perigo)?
- [ ] A piada vem depois da informação, nunca no lugar dela?
- [ ] Dá para tirar algum elemento sem perder nada? Então tire.
