"""Blockout da mobília carregável do Art Bible (tabela "Mobília carregável" da prancha PRP),
dos props de cenário da Biblioteca (ENV_03) e do relógio de pêndulo da Casa do Espelho (ENV_01).

Rodar (fecha sozinho ao terminar; passe nomes depois de -- para gerar só alguns):
    blender --background --factory-startup --python tools/blender/build_furniture_blockout.py
    blender ... --python tools/blender/build_furniture_blockout.py -- Sofa MesaLeitura

Saídas, uma pasta por móvel: Art/Source/Props/<Nome>/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Props/<Nome>/SM_PRP_<Nome>.fbx

Medidas (largura × profundidade × altura) da tabela do Art Bible. Origem na base, frente -Y.
O armário de figurino, a cadeira e a caixa média já existem (build_props_blockout.py).
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

MATS = bc.Materials("MAT_PRP", {
    "Mogno": ("#5A2E22", 0.0, 0.7, 0.0),
    "MognoClaro": ("#6E3A2A", 0.0, 0.7, 0.0),
    "Madeira": ("#8A5A3A", 0.0, 0.75, 0.0),
    "MadeiraVelha": ("#9C4A2E", 0.0, 0.85, 0.0),   # ferrugem / madeira velha
    "Veludo": ("#7A1E2E", 0.0, 0.95, 0.0),
    "Tecido": ("#6B4A6E", 0.0, 0.95, 0.0),
    "TecidoGasto": ("#5C4F45", 0.0, 1.0, 0.0),
    "Dourado": ("#C9A24A", 1.0, 0.35, 0.0),
    "Latao": ("#D9B45A", 1.0, 0.3, 0.0),
    "Sombra": ("#1C1A22", 0.0, 0.8, 0.0),
    "Marfim": ("#EDEBE4", 0.0, 0.4, 0.0),
    "Preto": ("#131118", 0.0, 0.4, 0.0),
    "Espelho": ("#BFD0D6", 1.0, 0.05, 0.0),
    "Lampada": ("#FFC98A", 0.0, 0.3, 2.0),         # lanterna 3200K (luz quente = segurança)
    "VerdeLuminaria": ("#2F5A3E", 0.1, 0.4, 0.0),
    "Papelao": ("#B88B5A", 0.0, 0.95, 0.0),
    "Fita": ("#E9DFC8", 0.0, 0.5, 0.0),
    "Carimbo": ("#C8463C", 0.0, 0.8, 0.0),
    "Petroleo": ("#3F5A5E", 0.2, 0.55, 0.0),
    "Ferro": ("#6E6A72", 1.0, 0.45, 0.0),
    "Louca": ("#E6E2D6", 0.0, 0.3, 0.0),
    "MadeiraManequim": ("#B08A62", 0.0, 0.6, 0.0),  # madeira clara e lisa: lê "falso" mesmo sem textura
    "Lencol": ("#A9A396", 0.0, 0.95, 0.0),         # lençol encardido sobre os móveis abandonados
    "LencolSombra": ("#8C8679", 0.0, 0.95, 0.0),
    "Teia": ("#8E8E96", 0.0, 0.9, 0.0),
    "Reboco": ("#6A6460", 0.0, 0.95, 0.0),          # caliça do teto que caiu
    "PapelVinho": ("#5A1E2A", 0.0, 0.9, 0.0),
    "PapelVelho": ("#7A6A4E", 0.0, 0.9, 0.0),       # o papel de parede mais antigo, por baixo
    "VidroQuebrado": ("#1E2A3A", 0.6, 0.1, 0.0),
    "Papel": ("#D8D0BC", 0.0, 0.9, 0.0),
    "Cera": ("#E6DCC4", 0.0, 0.6, 0.0),
    "Plastico": ("#2A2A30", 0.0, 0.5, 0.0),
    "Lente": ("#FFE8B0", 0.0, 0.2, 1.5),
    "Borracha": ("#1A1A1E", 0.0, 0.9, 0.0),         # anéis de pega e a cordinha da lanterna
    "FitaIsolante": ("#2E4A7A", 0.0, 0.45, 0.0),    # o remendo no cabo (a empresa não compra lanterna nova)
    # lombadas de livro (variam por prateleira, todas dessaturadas: o escuro é o padrão)
    "Livro1": ("#6E3A2A", 0.0, 0.8, 0.0),
    "Livro2": ("#3F5A5E", 0.0, 0.8, 0.0),
    "Livro3": ("#4A3A2E", 0.0, 0.8, 0.0),
    "Livro4": ("#6B4A6E", 0.0, 0.8, 0.0),
    "Livro5": ("#8E8794", 0.0, 0.8, 0.0),
    "Livro6": ("#9C7A3E", 0.0, 0.8, 0.0),
})

BOOKS = ["Livro1", "Livro2", "Livro3", "Livro4", "Livro5", "Livro6"]


def feet(p, w, d, h, mat, inset=0.04, size=0.06):
    for x in (-w / 2 + inset, w / 2 - inset):
        for y in (-d / 2 + inset, d / 2 - inset):
            p.box((x, y, h / 2), (size, size, h), mat, "root")


def book_row(p, x0, x1, y, z_base, depth, seed, max_h):
    """Uma fileira de livros de alturas e larguras variadas, com algum livro inclinado."""
    x = x0
    i = seed
    while x < x1 - 0.03:
        w = 0.025 + (i * 7 % 5) * 0.008
        h = max_h * (0.7 + (i * 13 % 7) * 0.04)
        if x + w > x1:
            break
        tilt = 12 if i % 11 == 5 else 0
        p.box((x + w / 2, y, z_base + h / 2), (w, depth, h), BOOKS[i % len(BOOKS)], "root", rot_y=tilt)
        x += w + 0.003
        i += 1


def build_penteadeira(col):
    """Penteadeira de camarim (F1): 1,2 × 0,5 × 1,6 m. Mesa com gavetas e o espelho cercado de lâmpadas."""
    p = bc.Part("SM_PRP_Penteadeira", MATS)
    w, d = 1.2, 0.5
    p.box((0, 0, 0.74), (w, d, 0.05), "MognoClaro", "root")               # tampo
    for x in (-0.42, 0.42):
        p.box((x, 0, 0.36), (0.32, d - 0.04, 0.68), "Mogno", "root")       # gaveteiros
        for z in (0.16, 0.38, 0.6):
            p.box((x, -d / 2 + 0.005, z), (0.28, 0.01, 0.18), "MognoClaro", "root")
            p.ellipsoid((x, -d / 2 - 0.01, z), (0.016,) * 3, "Latao", "root", seg=8, rings=5)
    # espelho com moldura e lâmpadas de camarim
    p.box((0, d / 2 - 0.06, 1.2), (0.86, 0.05, 0.86), "Mogno", "root")
    p.box((0, d / 2 - 0.09, 1.2), (0.7, 0.01, 0.7), "Espelho", "root")
    for i in range(5):
        t = i / 4
        for z in (0.82, 1.58):
            p.ellipsoid((-0.36 + t * 0.72, d / 2 - 0.1, z), (0.028,) * 3, "Lampada", "root", seg=10, rings=6)
        for x in (-0.4, 0.4):
            p.ellipsoid((x, d / 2 - 0.1, 0.86 + t * 0.68), (0.028,) * 3, "Lampada", "root", seg=10, rings=6)
    # um pote e um pincel em cima
    p.tube((-0.25, -0.05, 0.765), (-0.25, -0.05, 0.83), 0.04, 0.035, "Marfim", "root", seg=12)
    p.tube((0.2, -0.08, 0.77), (0.32, -0.12, 0.78), 0.008, 0.008, "Preto", "root", seg=6)
    return p.to_static(col)


def build_piano_armario(col):
    """Piano de armário (F2): 1,5 × 0,6 × 1,3 m, 230 kg, três jogadores. Teclado com teclas pretas."""
    p = bc.Part("SM_PRP_PianoArmario", MATS)
    w, d, h = 1.5, 0.6, 1.3
    p.box((0, 0.1, h / 2), (w, 0.4, h), "Mogno", "root")                   # caixa
    p.box((0, 0.1, h + 0.015), (w + 0.04, 0.44, 0.03), "MognoClaro", "root")  # tampa de cima
    p.box((0, -0.12, 0.72), (w - 0.1, 0.32, 0.06), "Mogno", "root")        # base do teclado
    keys_w = w - 0.2
    p.box((0, -0.16, 0.765), (keys_w, 0.18, 0.03), "Marfim", "root")       # teclas brancas
    for i in range(36):
        if i % 7 in (2, 6):
            continue                                                       # vãos do padrão 2+3
        x = -keys_w / 2 + 0.03 + i * (keys_w - 0.06) / 36
        p.box((x, -0.12, 0.79), (0.014, 0.1, 0.022), "Preto", "root")
    p.box((0, -0.1, 0.84), (w - 0.12, 0.02, 0.06), "MognoClaro", "root")   # tampa do teclado aberta
    for x in (-w / 2 + 0.08, w / 2 - 0.08):
        p.box((x, -0.14, 0.38), (0.08, 0.08, 0.72), "MognoClaro", "root")   # pernas da frente
    for x in (-0.12, 0.0, 0.12):
        p.box((x, -0.12, 0.08), (0.05, 0.08, 0.02), "Latao", "root")       # pedais
    p.box((0, -0.105, 1.0), (0.36, 0.01, 0.2), "Marfim", "root")           # partitura no apoio
    return p.to_static(col)


def build_estante(col):
    """Estante de biblioteca (F3): 1,0 × 0,4 × 2,4 m, 90 kg (Marcelo solo ou dois).
    Também é o módulo do kit da biblioteca (estante A)."""
    p = bc.Part("SM_PRP_EstanteBiblioteca", MATS)
    w, d, h = 1.0, 0.4, 2.4
    for x in (-w / 2 + 0.025, w / 2 - 0.025):
        p.box((x, 0, h / 2), (0.05, d, h), "Mogno", "root")                 # laterais
    p.box((0, d / 2 - 0.01, h / 2), (w, 0.02, h), "Mogno", "root")          # fundo
    p.box((0, 0, h - 0.03), (w + 0.06, d + 0.04, 0.06), "MognoClaro", "root")  # cornija
    p.box((0, -0.01, 0.05), (w, d, 0.1), "MognoClaro", "root")             # rodapé
    shelves = [0.1, 0.55, 1.0, 1.45, 1.9]
    for i, z in enumerate(shelves):
        p.box((0, 0, z), (w - 0.1, d - 0.02, 0.03), "Mogno", "root")
        # cada prateleira cheia de livros, com espaços (alguns livros "fora do lugar")
        book_row(p, -w / 2 + 0.06, w / 2 - 0.06 - (0.18 if i == 2 else 0.0), 0.02, z + 0.015, 0.26, i * 5, 0.36)
    # um livro deitado na prateleira com vão: o Bibliotecário não resiste
    p.box((0.33, 0.0, 1.03), (0.2, 0.15, 0.04), "Livro4", "root")
    # plaquinha de latão
    p.box((0, -d / 2 - 0.005, h - 0.1), (0.16, 0.01, 0.05), "Latao", "root")
    return p.to_static(col)


def build_mesa_leitura(col):
    """Mesa de leitura (F3): 2,0 × 0,9 × 0,78 m, 60 kg, dois jogadores. Tampo grosso e pés torneados."""
    p = bc.Part("SM_PRP_MesaLeitura", MATS)
    w, d, h = 2.0, 0.9, 0.78
    p.box((0, 0, h - 0.03), (w, d, 0.06), "MognoClaro", "root")
    p.box((0, 0, h - 0.09), (w - 0.12, d - 0.12, 0.08), "Mogno", "root")   # saia do tampo
    for x in (-w / 2 + 0.08, w / 2 - 0.08):
        for y in (-d / 2 + 0.08, d / 2 - 0.08):
            p.tube((x, y, 0.0), (x, y, h - 0.13), 0.04, 0.032, "Mogno", "root", seg=10)
            p.ellipsoid((x, y, 0.35), (0.05, 0.05, 0.06), "Mogno", "root", seg=10, rings=6)
    p.box((0, 0, 0.15), (w - 0.2, 0.05, 0.04), "Mogno", "root")             # travessa
    # dois livros abertos em cima
    for x in (-0.45, 0.5):
        for s in (-1, 1):
            p.box((x + s * 0.085, -0.1, h + 0.008), (0.16, 0.22, 0.012), "Marfim", "root", rot_y=-6 * s)
    return p.to_static(col)


def build_sofa(col):
    """Sofá de 3 lugares (fase final): 2,1 × 0,9 × 0,85 m, 70 kg. Tecido gasto, almofadas e remendo."""
    p = bc.Part("SM_PRP_Sofa", MATS)
    w, d, h = 2.1, 0.9, 0.85
    p.box((0, 0.02, 0.27), (w - 0.1, d - 0.08, 0.3), "TecidoGasto", "root")  # base
    p.box((0, d / 2 - 0.12, 0.58), (w - 0.1, 0.22, 0.56), "TecidoGasto", "root")  # encosto
    for x in (-w / 2 + 0.11, w / 2 - 0.11):
        p.box((x, 0.0, 0.42), (0.22, d, 0.48), "TecidoGasto", "root")       # braços
        p.ellipsoid((x, 0.0, 0.66), (0.11, d / 2, 0.05), "TecidoGasto", "root", seg=12, rings=6)
    for i in range(3):
        x = -0.6 + i * 0.6
        p.box((x, -0.05, 0.47), (0.58, 0.66, 0.12), "Tecido", "root")        # assentos
        p.ellipsoid((x, d / 2 - 0.25, 0.68), (0.27, 0.08, 0.2), "Tecido", "root", seg=14, rings=8)  # encosto
    feet(p, w - 0.1, d - 0.1, 0.12, "Mogno", size=0.07)
    p.box((0.55, -d / 2 + 0.06, 0.32), (0.18, 0.01, 0.12), "Veludo", "root")  # remendo
    return p.to_static(col)


def build_caixa(name, size, col):
    """Caixa de mudança S/M/L (todas as fases): cubo com fita, FRÁGIL e o logo da empresa."""
    p = bc.Part(f"SM_PRP_{name}", MATS)
    s = size
    p.box((0, 0, s / 2), (s, s, s), "Papelao", "root")
    p.box((0, 0, s + 0.003), (s + 0.005, 0.09, 0.006), "Fita", "root")
    for y in (-s / 2 - 0.003, s / 2 + 0.003):
        p.box((0, y, s - 0.07), (0.09, 0.006, 0.14), "Fita", "root")
    p.box((0, 0, s + 0.004), (s, 0.004, 0.004), "Sombra", "root")
    p.box((s * 0.14, -s / 2 - 0.002, s * 0.44), (s * 0.44, 0.004, s * 0.16), "Carimbo", "root")
    p.box((-s / 2 - 0.002, 0, s / 2), (0.004, s * 0.32, s * 0.24), "Petroleo", "root")
    return p.to_static(col)


def build_pilha_livros(col):
    """Pilha instável de livros (F3, derrubável): distração para o Bibliotecário. ~0,35 × 0,25 × 0,6 m."""
    p = bc.Part("SM_PRP_PilhaLivros", MATS)
    z = 0.0
    for i in range(9):
        w = 0.26 + (i * 7 % 4) * 0.02
        dd = 0.18 + (i * 5 % 3) * 0.02
        t = 0.045 + (i * 3 % 4) * 0.012
        rot = (i * 37 % 21) - 10
        p.box(((i % 3 - 1) * 0.012, (i % 2) * 0.01, z + t / 2), (w, dd, t), BOOKS[i % len(BOOKS)], "root", rot_y=0)
        # a rotação no plano do chão dá o ar de pilha bagunçada
        z += t
    for i in range(3):
        p.box((0.1, -0.02, z + 0.08 + i * 0.0), (0.03, 0.17, 0.16), BOOKS[(i + 2) % 6], "root", rot_y=15)
    return p.to_static(col)


def build_luminaria(col):
    """Luminária verde de biblioteca (F3): ligar faz "clique" (ruído). Base de latão e cúpula verde."""
    p = bc.Part("SM_PRP_LuminariaVerde", MATS)
    p.tube((0, 0, 0.0), (0, 0, 0.025), 0.08, 0.075, "Latao", "root", seg=16)
    p.tube((0, 0.04, 0.025), (0, 0.04, 0.35), 0.012, 0.012, "Latao", "root", seg=8)
    # cúpula: meio cilindro deitado (só a metade de cima), como as luminárias de banco
    shade = p.tube((-0.17, 0.04, 0.36), (0.17, 0.04, 0.36), 0.075, 0.075, "VerdeLuminaria", "root", seg=16)
    p.cut_below(shade, 0.355)
    p.box((0, 0.04, 0.35), (0.3, 0.1, 0.01), "Lampada", "root")             # luz por baixo da cúpula
    p.tube((0.05, 0.0, 0.31), (0.05, 0.0, 0.24), 0.003, 0.003, "Latao", "root", seg=5)  # cordinha
    p.ellipsoid((0.05, 0.0, 0.235), (0.008,) * 3, "Latao", "root", seg=8, rings=5)
    return p.to_static(col)


def build_relogio(col):
    """Relógio de pêndulo (cenário da Casa do Espelho, F1): 0,55 × 0,32 × 2,1 m. Não está na tabela do
    Art Bible: é decoração do casarão dos anos 1920 (mogno e dourado, como as molduras) e a origem do
    tique-taque (AmbientSound), uma âncora de som para a Cida. Fica parado: não é carregável."""
    p = bc.Part("SM_PRP_RelogioPendulo", MATS)
    p.box((0, 0, 0.15), (0.55, 0.32, 0.3), "Mogno", "root")                  # base
    p.box((0, 0, 0.31), (0.5, 0.29, 0.03), "MognoClaro", "root")             # moldura da base
    p.box((0, 0, 0.92), (0.42, 0.26, 1.2), "Mogno", "root")                  # caixa (tronco)
    # janela do pêndulo: fundo escuro, moldura dourada, haste e disco de latão
    front = -0.13
    p.box((0, front - 0.004, 0.95), (0.24, 0.01, 0.8), "Sombra", "root")
    for x in (-0.13, 0.13):
        p.box((x, front - 0.01, 0.95), (0.025, 0.012, 0.84), "Dourado", "root")
    for z in (0.54, 1.36):
        p.box((0, front - 0.01, z), (0.285, 0.012, 0.025), "Dourado", "root")
    p.box((0, front - 0.012, 1.1), (0.012, 0.006, 0.5), "Latao", "root")
    p.tube((0, front - 0.008, 0.8), (0, front - 0.02, 0.8), 0.075, 0.075, "Latao", "root", seg=16)
    # capitel com o mostrador
    p.box((0, 0, 1.77), (0.5, 0.3, 0.46), "Mogno", "root")
    p.box((0, 0, 1.53), (0.52, 0.31, 0.03), "MognoClaro", "root")
    face_y, face_z = -0.15, 1.77
    p.tube((0, face_y, face_z), (0, face_y - 0.007, face_z), 0.19, 0.19, "Dourado", "root", seg=24)
    p.tube((0, face_y, face_z), (0, face_y - 0.012, face_z), 0.165, 0.165, "Marfim", "root", seg=24)
    for i in range(12):                                                      # marcas das horas
        a = math.radians(i * 30)
        size = (0.012, 0.004, 0.035) if i % 3 == 0 else (0.008, 0.004, 0.02)
        p.box((math.sin(a) * 0.14, face_y - 0.014, face_z + math.cos(a) * 0.14), size, "Preto", "root", rot_y=i * 30)
    # ponteiros parados em 10h10 (rot_y gira no plano do mostrador, no sentido horário)
    for angle, length, width in ((305, 0.09, 0.014), (60, 0.13, 0.009)):
        a = math.radians(angle)
        p.box((math.sin(a) * length / 2, face_y - 0.017, face_z + math.cos(a) * length / 2),
              (width, 0.004, length), "Preto", "root", rot_y=angle)
    p.ellipsoid((0, face_y - 0.02, face_z), (0.015,) * 3, "Latao", "root", seg=8, rings=5)
    # coroa com frontão e pináculo dourado
    p.box((0, 0, 2.03), (0.56, 0.33, 0.06), "MognoClaro", "root")
    p.box((0, 0, 2.08), (0.3, 0.25, 0.04), "Mogno", "root")
    p.ellipsoid((0, 0, 2.13), (0.035, 0.035, 0.05), "Dourado", "root", seg=10, rings=6)
    return p.to_static(col)


# ---------- Casa do Espelho (LEVEL_DESIGN_FASE1.md §9): cenário e a arara ----------

def build_manequim(name, variant, col):
    """Manequim de figurino (0,45 × 0,30 × 1,75 m), fixo. No eco lê igual ao Imitador: sem passos e
    sem respiração (a pegadinha da fase). Três roupas: A vestido de veludo, B casaca, C o COLETE
    DA EMPRESA (a piada: alguém vestiu o manequim com o uniforme)."""
    p = bc.Part(f"SM_PRP_{name}", MATS)
    p.tube((0, 0, 0.0), (0, 0, 0.04), 0.18, 0.17, "Preto", "root", seg=16)          # base
    p.tube((0, 0, 0.04), (0, 0, 0.82), 0.015, 0.015, "Latao", "root", seg=8)        # haste
    p.ellipsoid((0, 0, 0.9), (0.16, 0.1, 0.1), "MadeiraManequim", "root", seg=14, rings=8)  # quadril
    p.ellipsoid((0, 0, 1.22), (0.18, 0.11, 0.28), "MadeiraManequim", "root", seg=16, rings=10)  # tronco
    p.tube((0, 0, 1.48), (0, 0, 1.56), 0.04, 0.035, "MadeiraManequim", "root", seg=10)  # pescoço
    p.ellipsoid((0, 0, 1.65), (0.085, 0.095, 0.11), "MadeiraManequim", "root", seg=14, rings=8)  # cabeça
    if variant == "A":
        p.tube((0, 0, 1.02), (0, 0, 0.42), 0.17, 0.3, "Veludo", "root", seg=18)     # saia
        p.ellipsoid((0, 0, 1.22), (0.19, 0.12, 0.25), "Veludo", "root", seg=16, rings=10)
        p.ellipsoid((0, -0.11, 1.32), (0.05, 0.01, 0.05), "Dourado", "root", seg=8, rings=4)  # broche
    elif variant == "B":
        p.ellipsoid((0, 0, 1.2), (0.21, 0.13, 0.3), "Tecido", "root", seg=16, rings=10)  # casaca
        p.box((0, 0.02, 0.86), (0.36, 0.2, 0.3), "Tecido", "root")                    # aba
        p.tube((0, 0, 1.74), (0, 0, 1.9), 0.06, 0.06, "Preto", "root", seg=12)          # cartola
        p.tube((0, 0, 1.73), (0, 0, 1.75), 0.11, 0.11, "Preto", "root", seg=14)
    else:
        p.ellipsoid((0, 0, 1.18), (0.2, 0.125, 0.24), "Petroleo", "root", seg=16, rings=10)  # colete
        p.box((-0.07, -0.12, 1.26), (0.08, 0.01, 0.05), "Carimbo", "root")           # logo da empresa
        p.ellipsoid((0, 0, 1.72), (0.095, 0.1, 0.05), "Petroleo", "root", seg=12, rings=6)  # boné
        p.box((0, -0.1, 1.71), (0.12, 0.08, 0.015), "Petroleo", "root")             # aba do boné
    return p.to_static(col)


def build_gramofone(col):
    """Gramofone (0,5 × 0,5 × 0,8 m) na Sala de Ensaio: a 1ª fonte de ruído. A corneta aponta
    para a frente e para cima."""
    p = bc.Part("SM_PRP_Gramofone", MATS)
    p.box((0, 0.05, 0.08), (0.4, 0.36, 0.16), "Mogno", "root")                     # caixa
    p.box((0, 0.05, 0.165), (0.42, 0.38, 0.01), "Dourado", "root")
    p.tube((0, 0.05, 0.17), (0, 0.05, 0.185), 0.15, 0.15, "Preto", "root", seg=20)  # disco
    p.tube((0, 0.05, 0.185), (0, 0.05, 0.19), 0.04, 0.04, "Carimbo", "root", seg=12)  # selo
    p.tube((0.14, 0.18, 0.17), (0.14, 0.18, 0.3), 0.015, 0.015, "Latao", "root", seg=8)  # base do braço
    p.tube((0.14, 0.18, 0.3), (0.04, 0.0, 0.32), 0.012, 0.012, "Latao", "root", seg=8)   # braço
    p.tube((0.14, 0.18, 0.3), (0.0, -0.08, 0.5), 0.02, 0.035, "Latao", "root", seg=10)   # pescoço da corneta
    p.tube((0.0, -0.08, 0.5), (0.0, -0.2, 0.78), 0.035, 0.24, "Latao", "root", seg=18, caps=False)  # corneta
    p.ellipsoid((0.21, 0.05, 0.08), (0.01, 0.03, 0.03), "Latao", "root", seg=8, rings=4)  # manivela
    return p.to_static(col)


def build_retrato(name, h, col):
    """Retrato da Dona Herondina (galeria do hall): moldura dourada e o busto em cores escuras.
    Proporção 3:4. Origem no meio da base, encostado na parede (olha para -Y)."""
    p = bc.Part(f"SM_PRP_{name}", MATS)
    w = h * 0.75
    b = max(0.04, h * 0.08)
    p.box((0, 0.0, h / 2), (w - b, 0.02, h - b), "Sombra", "root")                 # tela
    for x in (-(w - b) / 2, (w - b) / 2):
        p.box((x, -0.005, h / 2), (b, 0.04, h), "Dourado", "root")
    for z in (b / 2, h - b / 2):
        p.box((0, -0.005, z), (w, 0.04, b), "Dourado", "root")
    p.ellipsoid((0, -0.012, h * 0.3), (w * 0.3, 0.005, h * 0.18), "Veludo", "root", seg=14, rings=6)  # busto
    p.ellipsoid((0, -0.014, h * 0.56), (w * 0.13, 0.005, h * 0.12), "Marfim", "root", seg=14, rings=6)  # rosto
    p.ellipsoid((0, -0.013, h * 0.66), (w * 0.17, 0.005, h * 0.08), "Preto", "root", seg=12, rings=6)  # coque
    return p.to_static(col)


def build_lustre(col):
    """Lustre de cristal (0,6 × 0,6 × 0,7 m) da Sala de Jantar: âncora sonora (tilinta).
    Origem na BASE (a gota de baixo); pendura com o topo da corrente no forro."""
    p = bc.Part("SM_PRP_Lustre", MATS)
    p.tube((0, 0, 0.45), (0, 0, 0.7), 0.008, 0.008, "Latao", "root", seg=6)      # corrente
    p.ellipsoid((0, 0, 0.36), (0.07, 0.07, 0.11), "Latao", "root", seg=12, rings=8)  # corpo
    for i in range(6):
        a = math.radians(i * 60)
        x, y = math.cos(a) * 0.27, math.sin(a) * 0.27
        p.tube((0, 0, 0.33), (x, y, 0.3), 0.009, 0.009, "Latao", "root", seg=6)  # braço
        p.tube((x, y, 0.3), (x, y, 0.36), 0.025, 0.02, "Latao", "root", seg=8)  # copinho
        p.ellipsoid((x, y, 0.39), (0.015, 0.015, 0.03), "Lampada", "root", seg=8, rings=5)  # vela
        p.ellipsoid((x, y, 0.24), (0.018, 0.018, 0.04), "Espelho", "root", seg=8, rings=5)  # cristal
    p.ellipsoid((0, 0, 0.12), (0.035, 0.035, 0.08), "Espelho", "root", seg=10, rings=6)  # gota
    p.ellipsoid((0, 0, 0.04), (0.018, 0.018, 0.04), "Espelho", "root", seg=8, rings=5)
    return p.to_static(col)


def build_torneira(col):
    """Pia da Copa com a torneira que pinga (0,6 × 0,45 × 1,05 m): âncora sonora pequena e regular.
    O Art Bible lista só a torneira; sem a pia ela flutuaria na parede."""
    p = bc.Part("SM_PRP_Torneira", MATS)
    p.box((0, 0, 0.4), (0.6, 0.45, 0.8), "Madeira", "root")                       # armário
    for x in (-0.15, 0.15):
        p.box((x, -0.226, 0.42), (0.26, 0.01, 0.6), "MadeiraVelha", "root")       # portinhas
    p.box((0, 0, 0.83), (0.62, 0.47, 0.06), "Louca", "root")                     # tampo
    p.box((0, -0.02, 0.85), (0.44, 0.3, 0.03), "Sombra", "root")                # cuba
    p.tube((0, 0.18, 0.86), (0, 0.18, 1.02), 0.015, 0.015, "Latao", "root", seg=8)
    p.tube((0, 0.18, 1.02), (0, 0.04, 0.98), 0.013, 0.011, "Latao", "root", seg=8)  # bico
    for x in (-0.08, 0.08):
        p.ellipsoid((x, 0.18, 0.95), (0.025, 0.025, 0.012), "Latao", "root", seg=8, rings=4)  # registros
    p.ellipsoid((0, 0.04, 0.95), (0.006, 0.006, 0.01), "Espelho", "root", seg=6, rings=4)  # a gota
    return p.to_static(col)


def build_arara(col):
    """Arara de figurino (1,4 × 0,5 × 1,7 m, 20 kg, 1 pessoa): carregável, e os cabides de metal
    são a âncora sonora do Camarim Grande."""
    p = bc.Part("SM_PRP_AraraFigurino", MATS)
    for x in (-0.66, 0.66):
        p.tube((x, 0, 0.04), (x, 0, 1.66), 0.018, 0.018, "Ferro", "root", seg=8)  # colunas
        p.box((x, 0, 0.025), (0.06, 0.5, 0.04), "Ferro", "root")                 # pés
        for y in (-0.22, 0.22):
            p.ellipsoid((x, y, 0.02), (0.025, 0.025, 0.02), "Preto", "root", seg=8, rings=4)  # rodinhas
    p.tube((-0.7, 0, 1.66), (0.7, 0, 1.66), 0.016, 0.016, "Ferro", "root", seg=8)  # barra
    clothes = ["Veludo", "Tecido", "TecidoGasto", "Marfim", "Veludo", "Tecido", "Petroleo"]
    for i, mat in enumerate(clothes):
        x = -0.5 + i * (1.0 / (len(clothes) - 1))
        p.tube((x, 0, 1.66), (x, 0, 1.58), 0.004, 0.004, "Ferro", "root", seg=4)  # gancho
        p.box((x, 0, 1.56), (0.02, 0.36, 0.03), "Ferro", "root")                  # cabide
        drop = 0.75 + (i * 7 % 4) * 0.08
        p.box((x, 0, 1.55 - drop / 2), (0.05, 0.42, drop), mat, "root")           # roupa
    return p.to_static(col)


# ---------- Lanterna e pilhas (a luz que a equipe precisa gerir) ----------

LANTERNA_EIXO = 0.055   # altura do eixo: a cabeça (raio 5,5 cm) encosta no chão, deitada


def build_lanterna(col):
    """Lanterna de mão barata da transportadora (0,11 × 0,11 × 0,25 m), 3200 K. Deitada (o corpo
    corre em -Y = a frente, a lente): é como ela fica no caminhão. Origem no meio da base.

    Tudo no MESMO eixo (a cabeça antiga era um cone inclinado e a borda dele cortava a lente).
    Cabo de borracha com anéis de pega, remendo de fita isolante, botão de borracha embutido,
    adesivo da empresa, cabeça de latão com aro em volta da lente, tampa das pilhas com cordinha.
    O jogo usa: o meio do cabo (HeldItems.flashlightGrip: Y 0,03 → Unity Z −0,03, altura 0,055)
    e a lente (Y −0,11 → Unity Z +0,11)."""
    a = LANTERNA_EIXO
    p = bc.Part("SM_PRP_Lanterna", MATS)
    # cabo e tampa das pilhas
    p.tube((0, 0.12, a), (0, -0.06, a), 0.032, 0.032, "Plastico", "root", seg=16)            # cabo
    p.tube((0, 0.12, a), (0, 0.133, a), 0.030, 0.028, "Latao", "root", seg=12)              # tampa das pilhas
    for y in (0.095, 0.075, 0.055):                                                          # anéis de pega
        p.tube((0, y, a), (0, y - 0.006, a), 0.034, 0.034, "Borracha", "root", seg=16, caps=False)
    p.tube((0, 0.005, a), (0, -0.03, a), 0.0335, 0.0335, "FitaIsolante", "root", seg=16, caps=False)  # remendo
    # botão de borracha embutido no alto do cabo (a mão fecha logo atrás dele)
    p.ellipsoid((0, 0.03, a + 0.03), (0.011, 0.016, 0.006), "Carimbo", "root", seg=8, rings=4)
    # adesivo da transportadora na lateral: papel claro com a faixa vermelha da empresa
    p.box((0.0322, 0.075, a), (0.001, 0.045, 0.02), "Papel", "root")
    p.box((0.0328, 0.075, a), (0.001, 0.035, 0.005), "Carimbo", "root")
    # cabeça: pescoço que abre, o aro (bezel) e a lente um pouco à frente do aro
    p.tube((0, -0.06, a), (0, -0.095, a), 0.034, 0.052, "Latao", "root", seg=16, caps=False)  # pescoço
    p.tube((0, -0.095, a), (0, -0.109, a), 0.055, 0.055, "Latao", "root", seg=16)             # aro
    p.tube((0, -0.109, a), (0, -0.1105, a), 0.045, 0.045, "Lente", "root", seg=16)            # lente
    # cordinha de pulso: uma volta presa na tampa
    loop = [(0, 0.133, a + 0.012), (0, 0.15, a + 0.018), (0, 0.17, a + 0.008),
            (0, 0.175, a - 0.012), (0, 0.16, a - 0.024), (0, 0.14, a - 0.018), (0, 0.133, a - 0.008)]
    for i in range(len(loop) - 1):
        p.tube(loop[i], loop[i + 1], 0.0025, 0.0025, "Borracha", "root", seg=5, caps=False)
    return p.to_static(col)


def build_pilhas(col):
    """Pilhas (um pacotinho com 4, 0,14 × 0,08 × 0,06 m): recarregam a lanterna."""
    p = bc.Part("SM_PRP_Pilhas", MATS)
    p.box((0, 0, 0.005), (0.14, 0.08, 0.01), "Papelao", "root")
    for i in range(4):
        x = -0.045 + i * 0.03
        p.tube((x, -0.03, 0.03), (x, 0.03, 0.03), 0.013, 0.013, "Petroleo" if i % 2 else "Carimbo", "root", seg=10)
        p.tube((x, 0.03, 0.03), (x, 0.036, 0.03), 0.005, 0.005, "Latao", "root", seg=6)
    return p.to_static(col)


# ---------- Abandono (a casa ficou fechada décadas: lençóis, teias, caliça, tábuas) ----------

def build_movel_coberto(name, kind, col):
    """Móvel coberto por um lençol encardido. A = poltrona (0,9 × 0,85 × 1,0); B = cômoda alta
    (1,1 × 0,55 × 1,5). Formas moles por baixo do pano e a barra caindo até o chão."""
    p = bc.Part(f"SM_PRP_{name}", MATS)
    if kind == "A":
        p.ellipsoid((0, 0.05, 0.42), (0.46, 0.42, 0.3), "Lencol", "root", seg=18, rings=10)     # assento
        p.ellipsoid((0, 0.3, 0.75), (0.44, 0.16, 0.32), "Lencol", "root", seg=16, rings=10)     # encosto
        for x in (-0.38, 0.38):
            p.ellipsoid((x, 0.05, 0.6), (0.11, 0.38, 0.14), "LencolSombra", "root", seg=12, rings=8)  # braços
        p.tube((0, 0.05, 0.36), (0, 0.05, 0.0), 0.46, 0.5, "Lencol", "root", seg=20, caps=False)  # barra
    else:
        p.box((0, 0, 0.72), (1.06, 0.52, 1.44), "Lencol", "root")
        p.ellipsoid((0, 0, 1.44), (0.56, 0.3, 0.06), "Lencol", "root", seg=16, rings=6)          # tampo mole
        for x in (-0.3, 0.12, 0.42):
            p.box((x, -0.27, 0.6), (0.06, 0.02, 1.15), "LencolSombra", "root", rot_y=4 if x > 0 else -3)  # dobras
        p.box((0.2, -0.3, 0.06), (0.5, 0.12, 0.04), "Lencol", "root", rot_y=0)                    # ponta no chão
    return p.to_static(col)


def build_teia(col):
    """Teia de aranha para canto de parede com teto (0,6 × 0,6 m): raios saindo do canto e anéis.
    Fica no plano XZ (encostada numa parede, olhando para -Y); o canto é a origem, a teia desce e
    vai para +X."""
    p = bc.Part("SM_PRP_Teia", MATS)
    for i in range(6):
        a = math.radians(i * 18)  # de 0° (no teto) a 90° (na parede)
        length = 0.6
        p.box((math.cos(a) * length / 2, 0, -math.sin(a) * length / 2), (length, 0.004, 0.004), "Teia", "root",
              rot_y=math.degrees(a))
    for r in (0.15, 0.28, 0.42, 0.55):
        for i in range(5):
            a0, a1 = math.radians(i * 18), math.radians((i + 1) * 18)
            x0, z0 = math.cos(a0) * r, -math.sin(a0) * r
            x1, z1 = math.cos(a1) * r * 0.92, -math.sin(a1) * r * 0.92  # o fio cede no meio
            seg = math.hypot(x1 - x0, z1 - z0)
            ang = math.degrees(math.atan2(-(z1 - z0), x1 - x0))
            p.box(((x0 + x1) / 2, 0, (z0 + z1) / 2), (seg, 0.003, 0.003), "Teia", "root", rot_y=ang)
    return p.to_static(col)


def build_entulho(col):
    """Monte de entulho (1,2 × 1,0 × 0,45 m): tábuas quebradas e caliça que caiu do forro."""
    p = bc.Part("SM_PRP_Entulho", MATS)
    for i, (x, y, rot, length) in enumerate([(-0.3, -0.1, 18, 1.0), (0.15, 0.1, -25, 0.9), (0.0, -0.25, 8, 0.7),
                                               (0.35, 0.25, 40, 0.6), (-0.2, 0.3, -12, 0.8)]):
        p.box((x, y, 0.05 + i * 0.045), (length, 0.12, 0.03), "MadeiraVelha" if i % 2 else "Madeira", "root", rot_y=rot * 0.3)
    for x, y, s in [(0.3, -0.2, 0.14), (-0.4, 0.15, 0.1), (0.05, 0.2, 0.18), (0.45, 0.35, 0.08), (-0.1, -0.35, 0.12),
                    (0.2, 0.0, 0.22), (-0.35, -0.3, 0.09)]:
        p.ellipsoid((x, y, s * 0.5), (s, s * 0.8, s * 0.55), "Reboco", "root", seg=8, rings=5)
    return p.to_static(col)


def build_janela_tabuada(col):
    """Janela fechada com tábuas (1,0 × 1,4 m), vista de dentro: moldura, vidro quebrado e três
    tábuas pregadas tortas. Origem no meio da base (no peitoril), encostada na parede (olha para -Y)."""
    p = bc.Part("SM_PRP_JanelaTabuada", MATS)
    w, h = 1.0, 1.4
    p.box((0, 0.01, h / 2), (w - 0.1, 0.01, h - 0.1), "VidroQuebrado", "root")
    for x in (-w / 2 + 0.04, w / 2 - 0.04):
        p.box((x, 0, h / 2), (0.08, 0.08, h), "MadeiraVelha", "root")
    for z in (0.04, h - 0.04, h / 2):
        p.box((0, 0, z), (w, 0.08, 0.06 if z == h / 2 else 0.08), "MadeiraVelha", "root")
    p.box((0, -0.06, -0.02), (w + 0.12, 0.14, 0.05), "Mogno", "root")                       # peitoril
    for z, rot in ((0.35, 8), (0.75, -5), (1.1, 12)):
        p.box((0, -0.06, z), (w + 0.2, 0.025, 0.16), "Madeira", "root", rot_y=rot)            # tábuas
        for x in (-0.5, 0.5):
            p.ellipsoid((x, -0.075, z + x * math.tan(math.radians(-rot)) * 0.0), (0.012,) * 3, "Ferro", "root", seg=6, rings=3)
    return p.to_static(col)


def build_papel_descascando(col):
    """Papel de parede descascando (1,0 × 1,2 m): tiras vinho soltas, caídas para a frente, e o papel
    velho aparecendo por baixo. Encosta na parede (olha para -Y), origem no meio da base."""
    p = bc.Part("SM_PRP_PapelDescascando", MATS)
    p.box((0, 0.0, 0.6), (0.9, 0.004, 1.1), "PapelVelho", "root")
    for i, (x, length, curl) in enumerate([(-0.35, 0.8, 14), (-0.12, 1.1, 22), (0.1, 0.6, 9), (0.32, 0.95, 28)]):
        p.box((x, -0.02 - curl * 0.003, 1.2 - length / 2), (0.2, 0.006, length), "PapelVinho", "root")
        # a ponta de baixo enrola para a frente
        p.box((x, -0.06 - curl * 0.004, 1.2 - length + 0.05), (0.2, 0.08, 0.006), "PapelVinho", "root")
    return p.to_static(col)


def build_bau(col):
    """Baú velho de viagem da companhia (0,9 × 0,5 × 0,55 m): tampa curva, cintas de ferro e uma
    etiqueta "TABORDA & CIA"."""
    p = bc.Part("SM_PRP_Bau", MATS)
    p.box((0, 0, 0.22), (0.9, 0.5, 0.44), "MadeiraVelha", "root")
    p.tube((-0.45, 0, 0.44), (0.45, 0, 0.44), 0.25, 0.25, "MadeiraVelha", "root", seg=16, squash_y=1.0)
    p.cut_below(p.tube((-0.452, 0, 0.44), (0.452, 0, 0.44), 0.252, 0.252, "Ferro", "root", seg=16), 0.44)
    for x in (-0.3, 0.3):
        p.box((x, 0, 0.22), (0.05, 0.51, 0.45), "Ferro", "root")
    p.box((0, -0.255, 0.42), (0.08, 0.02, 0.08), "Latao", "root")                           # fecho
    p.box((-0.16, -0.252, 0.18), (0.22, 0.004, 0.1), "Papel", "root")                      # etiqueta
    return p.to_static(col)


def build_papeis_chao(col):
    """Papéis espalhados no chão (1,0 × 1,0 m): falas de peça, cartas e uma partitura de revista."""
    p = bc.Part("SM_PRP_PapeisChao", MATS)
    for i in range(9):
        x = -0.4 + (i * 37 % 9) * 0.1
        y = -0.4 + (i * 53 % 9) * 0.1
        p.box((x, y, 0.002 + i * 0.0015), (0.21, 0.3, 0.002), "Papel" if i % 3 else "PapelVelho", "root", rot_y=0)
    return p.to_static(col)


def build_velas(col):
    """Velas derretidas no chão (0,4 × 0,4 × 0,25 m): alguém acendeu na frente do espelho.
    As chamas são de material que brilha (a luz de verdade vem do prefab)."""
    p = bc.Part("SM_PRP_Velas", MATS)
    p.ellipsoid((0, 0, 0.005), (0.2, 0.18, 0.01), "Cera", "root", seg=14, rings=4)         # cera escorrida
    for x, y, h in [(-0.08, 0.03, 0.22), (0.06, -0.05, 0.14), (0.11, 0.08, 0.09), (-0.03, -0.1, 0.05), (0.0, 0.12, 0.17)]:
        p.tube((x, y, 0.0), (x, y, h), 0.022, 0.02, "Cera", "root", seg=10)
        if h > 0.06:
            p.ellipsoid((x, y, h + 0.02), (0.01, 0.01, 0.022), "Lampada", "root", seg=8, rings=5)
    return p.to_static(col)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
PROPS = [
    ("Penteadeira", build_penteadeira, 1.6, 1.8, 0.0),
    ("PianoArmario", build_piano_armario, 1.3, 1.8, 0.0),
    ("EstanteBiblioteca", build_estante, 2.4, 2.6, 0.0),
    ("MesaLeitura", build_mesa_leitura, 0.8, 2.2, 0.0),
    ("Sofa", build_sofa, 0.85, 2.3, 0.0),
    ("CaixaP", lambda c: build_caixa("CaixaP", 0.4, c), 0.4, 0.6, 0.0),
    ("CaixaG", lambda c: build_caixa("CaixaG", 0.6, c), 0.6, 0.8, 0.0),
    ("PilhaLivros", build_pilha_livros, 0.75, 0.9, 0.0),
    ("LuminariaVerde", build_luminaria, 0.45, 0.6, 0.0),
    ("RelogioPendulo", build_relogio, 2.18, 2.4, 0.0),
    ("ManequimA", lambda c: build_manequim("ManequimA", "A", c), 1.75, 2.0, 0.0),
    ("ManequimB", lambda c: build_manequim("ManequimB", "B", c), 1.9, 2.1, 0.0),
    ("ManequimC", lambda c: build_manequim("ManequimC", "C", c), 1.77, 2.0, 0.0),
    ("Gramofone", build_gramofone, 0.8, 1.0, 0.0),
    ("RetratoP", lambda c: build_retrato("RetratoP", 0.4, c), 0.4, 0.55, 0.0),
    ("RetratoM", lambda c: build_retrato("RetratoM", 0.7, c), 0.7, 0.85, 0.0),
    ("RetratoG", lambda c: build_retrato("RetratoG", 1.1, c), 1.1, 1.25, 0.0),
    ("Lustre", build_lustre, 0.7, 0.85, 0.0),
    ("Torneira", build_torneira, 1.05, 1.2, 0.0),
    ("AraraFigurino", build_arara, 1.7, 1.9, 0.0),
    ("Lanterna", build_lanterna, 0.12, 0.35, 0.0),
    ("Pilhas", build_pilhas, 0.06, 0.2, 0.0),
    ("MovelCobertoA", lambda c: build_movel_coberto("MovelCobertoA", "A", c), 1.07, 1.2, 0.0),
    ("MovelCobertoB", lambda c: build_movel_coberto("MovelCobertoB", "B", c), 1.5, 1.7, 0.0),
    ("Teia", build_teia, 0.6, 0.8, 0.0),
    ("Entulho", build_entulho, 0.45, 1.3, 0.0),
    ("JanelaTabuada", build_janela_tabuada, 1.4, 1.6, 0.0),
    ("PapelDescascando", build_papel_descascando, 1.2, 1.4, 0.0),
    ("Bau", build_bau, 0.7, 1.0, 0.0),
    ("PapeisChao", build_papeis_chao, 0.05, 1.1, 0.0),
    ("Velas", build_velas, 0.25, 0.45, 0.0),
]


def main():
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else None
    for name, build, height, frame, center_y in PROPS:
        if only and name not in only:
            continue
        scene, cols = bc.new_scene(name, kind="Props")
        obj = build(cols["Geo"])
        bc.report_tris((obj,))
        bc.finish(scene, cols, name, height, kind="Props", frame=frame, center_y=center_y)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
