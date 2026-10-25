# Padrão visual — Mudança Assombrada

> **Silêncio, escuridão e pressa.**
> Na dúvida, escolha a opção mais escura, mais quieta e mais simples.
> Toda decoração precisa de um motivo para existir.

Como o código está organizado: [ARQUITETURA.md](ARQUITETURA.md).
Personagens, fantasmas, cenários e pipeline de modelos 3D: [ART_BIBLE.md](ART_BIBLE.md).

Os valores (cores, tamanhos, durações) ficam no código em
[`Assets/Scripts/Theme.cs`](MudancaAssombrada/Assets/Scripts/Theme.cs).
Este documento explica **as regras**; o `Theme` guarda **os números**.
Se mudar um, atualize o outro.

---

## 1. Cores

Poucas cores, cada uma com um papel fixo.

| Papel | Nome no `Theme` | Uso |
|---|---|---|
| Fundo | `BackgroundCenter` / `BackgroundEdge` | fundo das telas (vinheta: mais claro no meio) |
| Painel | `PanelColor` | caixa translúcida atrás de textos e campos |
| Campo | `FieldColor` | campos de texto, trilho do slider, caixa do checkbox |
| Borda | `BorderColor` | contorno fino de botões e campos |
| Título | `TitleColor` | título grande do menu |
| Texto | `TextColor` | todo texto normal |
| Nota | `MutedColor` | dicas, status, textos secundários |
| Destaque | `HighlightColor` | hover, item selecionado, texto digitado |
| **Perigo** | `DangerColor` | fantasma, erro, tempo acabando |
| **Amanhecer** | `DawnColor` | progresso, vitória, caminhão |

**Personagens:** Cida (roxo), Tiago (amarelo), Marcelo (verde), no campo **Cor** da
ficha de cada um (`CharacterDefinition.color`, em `Assets/Data/Personagens`). Na interface, só em detalhes (nome, ícone,
contorno), sempre "domadas" com `Theme.Muted(cor)`. Nunca em áreas grandes.

**Regra de ouro:** cor viva só quando significa algo. Se tudo é cinza,
o vermelho do perigo chama atenção na hora.

## 2. Tipografia

| Fonte | Onde | Arquivo |
|---|---|---|
| **Bebas Neue** | títulos, botões, números grandes (cronômetro) | `GameFonts.Title` |
| **Poppins** | frases, dicas, listas, HUD | `GameFonts.Body` |
| **Poppins Bold** | ênfase dentro de texto | `GameFonts.Bold` |

Bebas Neue só tem maiúsculas: "Jogar" aparece como "JOGAR" sozinho.

Tamanhos (tela 1920×1080). **Nada fora desta lista:**

| Nome | Tamanho | Uso |
|---|---|---|
| `TitleSize` | 150 | título do menu |
| `HeaderSize` | 48 | cabeçalho de painel ("PAUSA") |
| `ButtonSize` | 36 | botões |
| `BodySize` | 26 | textos normais |
| `NoteSize` | 20 | notas, status, dicas pequenas |

## 3. Componentes

Toda interface sai do [`UiBuilder`](MudancaAssombrada/Assets/Editor/UiBuilder.cs)
(a HUD da partida é montada pelo `HudBuilder` com as mesmas peças).
**Nunca crie UI "na mão"** fora dele: se faltar uma peça, adicione ela lá.
Nada de `OnGUI`: ele não segue o `Theme`.

- **Botão:** retângulo quase transparente, borda fina, texto em maiúsculas.
  No hover só **clareia** (fundo e texto). Nada cresce, brilha ou pisca.
- **Painel:** caixa escura translúcida, sem borda e sem sombra.
  No menu inicial, os botões ficam soltos (sem painel).
- **Campo de texto / slider / checkbox:** fundo `FieldColor`, borda fina, detalhes em `HighlightColor`.
- **Dica de tecla:** `[E] Pegar`, com a tecla numa caixinha com borda. Sempre o mesmo formato.
- **Aviso na tela:** texto centralizado que aparece e some com fade
  ("O fantasma ouviu algo...").

## 4. Movimento

- Só **fade** (aparecer/sumir) e **deslizes curtos**.
- Durações: `FadeFast` (0,15 s) para hover; `FadeSlow` (0,3 s) para trocar de tela e avisos.
- **Única exceção: o perigo.** Fantasma perto ou tempo acabando pode tremer
  ou piscar. Como nada mais se mexe, isso assusta de verdade.

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

- [ ] Usa só cores do `Theme`? Cor viva só com significado?
- [ ] Usa só os 5 tamanhos de texto?
- [ ] Foi feita pelo `UiBuilder`?
- [ ] Animações só com fade/deslize curto (exceto perigo)?
- [ ] Dá para tirar algum elemento sem perder nada? Então tire.
