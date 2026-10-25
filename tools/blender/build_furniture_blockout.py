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
