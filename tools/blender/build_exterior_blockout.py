"""Blockout do CENÁRIO EXTERNO da Mansão do Espelho (documentação "Mansão e cenário externo não
urbano"): a propriedade isolada na floresta, com muro de pedra, portão de ferro, caminho de pedra,
fonte, luminárias, lago com doca e barco, galpão, árvore retorcida, arbustos, pedras e ruínas.

Rodar (fecha sozinho ao terminar; passe nomes depois de -- para gerar só alguns):
    blender --background --factory-startup --python tools/blender/build_exterior_blockout.py

Saídas: Art/Source/Environment/<Nome>/ e MudancaAssombrada/Assets/Art/Environment/<Nome>/SM_ENV_<Nome>.fbx
Quem põe as peças no mapa é o MirrorHouseBuilder.Exterior.cs (Unity).

Contra a cara de "massinha" (ART_BIBLE.md, "Acabamento"): nada aqui é uma caixa lisa. O muro é
feito pedra por pedra (cada uma de um tamanho, um pouco torta, saltando do rejunte), o caminho é
de lajes irregulares, as copas e as pedras são amassadas por ruído (Part.wrinkle), as quinas têm
bisel e o UV é em metros: no Unity entram as texturas de detalhe de pedra, grama, folhas, madeira
e metal. As peças que encaixam em fila (muro, grade, laje do caminho) têm 4 m ou 2 m.
"""

import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

# Bisel um pouco mais largo que o dos móveis: pedra e madeira de fora são grossas e gastas.
bc.DEFAULT_BEVEL = 0.012

MATS = bc.Materials("MAT_ENV", {
    # pedra úmida: o muro, o pilar, a fonte e as lajes (o nome "Pedra" liga o detalhe de pedra)
    "PedraMuro": ("#4E4C52", 0.0, 0.85, 0.0),
    "PedraClara": ("#5E5B5E", 0.0, 0.85, 0.0),
    "PedraEscura": ("#36343A", 0.0, 0.9, 0.0),
    "PedraRejunte": ("#232226", 0.0, 0.95, 0.0),   # o fundo entre as pedras
    "PedraMusgo": ("#2F3A2C", 0.0, 0.95, 0.0),     # musgo nas pedras (detalhe de pedra também)
    # ferro oxidado: portão, grade e postes
    "Ferro": ("#1C1C20", 0.8, 0.55, 0.0),
    "Ferrugem": ("#4A2C20", 0.4, 0.8, 0.0),
    # madeira envelhecida: galpão, doca e barco (o nome tem "Madeira"/"Tabua": detalhe de madeira)
    "MadeiraVelha": ("#3E3229", 0.0, 0.85, 0.0),
    "MadeiraVelhaClara": ("#4E4034", 0.0, 0.85, 0.0),
    "MadeiraPintada": ("#2E3A36", 0.0, 0.8, 0.0),  # o barco e a porta do galpão: verde gasto
    "TabuaTelhado": ("#2A2422", 0.0, 0.9, 0.0),
    "MadeiraCasca": ("#2B211A", 0.0, 0.95, 0.0),   # tronco
    "MadeiraMorta": ("#3A342E", 0.0, 0.9, 0.0),    # a árvore retorcida: cinza de madeira seca
    # folhagem (o nome "Folhagem" liga o detalhe de folhas)
    "FolhagemPinheiro": ("#1A2820", 0.0, 0.9, 0.0),
    "FolhagemPinheiroEscura": ("#13201A", 0.0, 0.9, 0.0),
    "Folhagem": ("#22301E", 0.0, 0.9, 0.0),
    "FolhagemEscura": ("#18241A", 0.0, 0.9, 0.0),
    "FolhagemSeca": ("#4A3A24", 0.0, 0.9, 0.0),    # folhas caídas
    "FolhagemSecaEscura": ("#3A2C1C", 0.0, 0.9, 0.0),
    "Grama": ("#1E2A1C", 0.0, 0.95, 0.0),
    "Terra": ("#2C241C", 0.0, 0.95, 0.0),
    # água escura: quase preta, lisa (reflete o luar e as luminárias)
    "Agua": ("#0B141A", 0.3, 0.06, 0.0),
    # luz quente: a chama das luminárias (3200 K, como as arandelas da casa)
    "Chama": ("#FFC98A", 0.0, 0.3, 2.0),
    "VidroLampiao": ("#6A5A40", 0.2, 0.15, 0.4),   # vidro fosco, tingido pela chama
    "VidroJanela": ("#141C24", 0.5, 0.1, 0.0),
    # hera, musgo e mato (o nome "Folhagem"/"Grama" liga o detalhe de folhas/grama)
    "FolhagemHera": ("#1F2E1C", 0.0, 0.8, 0.0),
    "FolhagemHeraEscura": ("#152114", 0.0, 0.85, 0.0),
    "FolhagemMusgo": ("#2A3622", 0.0, 0.95, 0.0),
    "GramaTufo": ("#24331F", 0.0, 0.9, 0.0),
    "GramaSeca": ("#4A4630", 0.0, 0.9, 0.0),
    # a fachada da casa: reboco ocre desbotado e manchado de umidade (o nome "Reboco" liga o
    # detalhe de reboco), com cantaria de pedra na base, nos cunhais e nas janelas
    "RebocoFachada": ("#6B6152", 0.0, 0.9, 0.0),
    "RebocoManchado": ("#4C463C", 0.0, 0.95, 0.0),
    "RebocoFriso": ("#7A705F", 0.0, 0.85, 0.0),
})


def rng_for(name):
    """O mesmo sorteio em toda geração (o modelo não muda sozinho de uma vez para a outra)."""
    return random.Random(sum(ord(c) * (i + 1) for i, c in enumerate(name)))


# ---------------------------------------------------------------- ajudantes

def stone_face(p, rng, x0, x1, z0, z1, y, side, rows, mats=("PedraMuro", "PedraClara", "PedraEscura"),
               depth=(0.08, 0.14), moss=0.15):
    """Uma face de muro feita PEDRA POR PEDRA, na faixa x0..x1 / z0..z1, no plano y.
    side = -1 (as pedras saltam para -Y) ou +1. Cada fileira tem pedras de largura sorteada,
    um pouco tortas (rot_y gira no plano da face) e de profundidade diferente: a luz rasante
    da lanterna pega o relevo de cada uma. O rejunte é o núcleo escuro do muro, atrás delas."""
    h = (z1 - z0) / rows
    for r in range(rows):
        x = x0 + (rng.uniform(0, 0.25) if r % 2 else 0.0)  # fiadas desencontradas
        zc = z0 + (r + 0.5) * h
        while x < x1 - 0.05:
            w = min(rng.uniform(0.32, 0.75), x1 - x)
            d = rng.uniform(*depth)
            mat = "PedraMusgo" if rng.random() < moss * (1.0 - r / rows) else rng.choice(mats)
            p.box((x + w / 2, y + side * d / 2, zc + rng.uniform(-0.015, 0.015)),
                  (w - 0.035, d, h - 0.035 - rng.uniform(0, 0.03)), mat, "root",
                  rot_y=rng.uniform(-4, 4))
            x += w


def cut_faces(p, faces, keep):
    """Apaga as faces (de uma primitiva) cujo centro não passa no teste keep(centro)."""
    kill = [f for f in faces if f.is_valid and not keep(f.calc_center_median())]
    bmesh.ops.delete(p.bm, geom=kill, context="FACES")


def flip(p, faces):
    bmesh.ops.reverse_faces(p.bm, faces=[f for f in faces if f.is_valid])


def ring(p, center, radius, z, thick, mat, seg=24, squash=1.0):
    """Anel de tubos (beirada de bacia, borda de barco): um polígono fechado de tubos."""
    pts = [(center[0] + math.cos(2 * math.pi * i / seg) * radius,
            center[1] + math.sin(2 * math.pi * i / seg) * radius * squash, z) for i in range(seg + 1)]
    p.polyline(pts, thick, mat, "root", seg=8)


def spear(p, x, y, z, mat="Ferro", size=0.05):
    """A ponta de lança no topo de cada barra do portão e da grade."""
    p.tube((x, y, z), (x, y, z + size * 2.4), size, 0.002, mat, "root", seg=6, smooth=False)


def lantern(p, x, y, z, scale=1.0):
    """Lampião de ferro com quatro vidros e a chama no meio (topo dos postes e dos pilares).
    z é a base do lampião."""
    s = scale
    p.tube((x, y, z), (x, y, z + 0.06 * s), 0.09 * s, 0.13 * s, "Ferro", "root", seg=8, smooth=False)
    for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):                     # montantes
        p.box((x + dx * 0.11 * s, y + dy * 0.11 * s, z + 0.24 * s), (0.02 * s, 0.02 * s, 0.36 * s), "Ferro", "root")
    for dx, dy, sx, sy in ((0, -1, 0.2, 0.01), (0, 1, 0.2, 0.01), (-1, 0, 0.01, 0.2), (1, 0, 0.01, 0.2)):
        p.box((x + dx * 0.105 * s, y + dy * 0.105 * s, z + 0.24 * s), (sx * s, sy * s, 0.33 * s), "VidroLampiao", "root")
    p.ellipsoid((x, y, z + 0.2 * s), (0.04 * s, 0.04 * s, 0.07 * s), "Chama", "root", seg=8, rings=6)
    p.tube((x, y, z + 0.42 * s), (x, y, z + 0.6 * s), 0.17 * s, 0.02 * s, "Ferro", "root", seg=8, smooth=False)  # chapéu
    p.ellipsoid((x, y, z + 0.63 * s), (0.03 * s,) * 3, "Ferro", "root", seg=8, rings=5)


# ---------------------------------------------------------------- muro, pilares, portão e grade

MURO_W, MURO_H, MURO_T = 4.0, 1.6, 0.5


def build_muro_pedra(col):
    """Muro de pedra de 4 m × 1,6 m × 0,5 m: o limite da propriedade (laterais). Pedras nas duas
    faces, capa de pedras maiores por cima, com musgo. Origem no meio da base; corre em X."""
    rng = rng_for("MuroPedra")
    p = bc.Part("SM_ENV_MuroPedra", MATS)
    p.box((0, 0, MURO_H / 2), (MURO_W, MURO_T - 0.12, MURO_H), "PedraRejunte", "root")
    for side in (-1, 1):
        stone_face(p, rng, -MURO_W / 2, MURO_W / 2, 0.0, MURO_H - 0.14, side * (MURO_T / 2 - 0.08), side, rows=4)
    x = -MURO_W / 2
    while x < MURO_W / 2 - 0.05:                                             # capa
        w = min(rng.uniform(0.45, 0.8), MURO_W / 2 - x)
        p.box((x + w / 2, rng.uniform(-0.02, 0.02), MURO_H - 0.07), (w - 0.03, MURO_T + 0.04, 0.14),
              rng.choice(("PedraClara", "PedraMuro", "PedraMusgo")), "root", rot_y=rng.uniform(-2, 2))
        x += w
    return p.to_static(col, bevel_segments=1)


def build_muro_arrimo(col):
    """Muro de ARRIMO de 4 m × 3,6 m: a frente do morro onde ficam os fundos da casa (nível de
    cima, Y 3,6). Uma face só (a do pátio, -Y = +Z no Unity: girar 180° para olhar para a frente
    da casa), com contrafortes e musgo escorrendo. Origem no meio da base."""
    rng = rng_for("MuroArrimo")
    p = bc.Part("SM_ENV_MuroArrimo", MATS)
    h = 3.6
    p.box((0, 0.15, h / 2), (MURO_W, 0.3, h), "PedraRejunte", "root")
    stone_face(p, rng, -MURO_W / 2, MURO_W / 2, 0.0, h - 0.16, 0.0, -1, rows=7, moss=0.3)
    for x in (-MURO_W / 2 + 0.3, MURO_W / 2 - 0.3):                          # contrafortes
        p.box((x, -0.25, 0.9), (0.55, 0.5, 1.8), "PedraEscura", "root")
        p.box((x, -0.12, 2.1), (0.5, 0.25, 0.6), "PedraEscura", "root", rot_y=0)
    p.box((0, -0.06, h - 0.08), (MURO_W, 0.42, 0.16), "PedraMusgo", "root")  # beirada com musgo
    for _ in range(5):                                                       # musgo escorrendo
        x = rng.uniform(-1.7, 1.7)
        length = rng.uniform(0.4, 1.4)
        p.box((x, -0.16, h - 0.1 - length / 2), (rng.uniform(0.15, 0.4), 0.03, length), "PedraMusgo", "root")
    return p.to_static(col, bevel_segments=1)


def build_muro_ruina(col):
    """Trecho de muro DESMORONADO (4 m): metade ainda de pé, a outra virou pedras no chão.
    Dá idade ao lugar e fica perto dos limites (Documentação §2, "vegetação e ruínas")."""
    rng = rng_for("MuroRuina")
    p = bc.Part("SM_ENV_MuroRuina", MATS)
    for i, top in enumerate((1.5, 1.2, 0.8, 0.45)):                          # quatro blocos cada vez mais baixos
        x0 = -MURO_W / 2 + i * 0.75
        p.box((x0 + 0.375, 0, top / 2), (0.75, MURO_T - 0.12, top), "PedraRejunte", "root")
        for side in (-1, 1):
            stone_face(p, rng, x0, x0 + 0.75, 0.0, top, side * (MURO_T / 2 - 0.08), side, rows=max(1, round(top / 0.4)), moss=0.35)
    for _ in range(9):                                                       # pedras caídas
        x, y = rng.uniform(0.4, 2.0), rng.uniform(-0.9, 0.9)
        s = rng.uniform(0.18, 0.4)
        p.box((x, y, s * 0.35), (s * 1.4, s, s * 0.7), rng.choice(("PedraMuro", "PedraEscura", "PedraMusgo")), "root",
              rot_y=rng.uniform(-25, 25), rot_z=rng.uniform(0, 180))
    return p.to_static(col, bevel_segments=1)


def pillar(p, rng, h, w=0.7):
    """Pilar de pedra de seção quadrada: pedras nas quatro faces, base e capa com beiral."""
    p.box((0, 0, h / 2), (w - 0.1, w - 0.1, h), "PedraRejunte", "root")
    p.box((0, 0, 0.1), (w + 0.12, w + 0.12, 0.2), "PedraEscura", "root")
    rows = max(3, round(h / 0.42))
    for side in (-1, 1):
        stone_face(p, rng, -w / 2, w / 2, 0.2, h - 0.2, side * (w / 2 - 0.08), side, rows=rows, moss=0.2)
    # as faces em X: mesmo muro, girado (pedras em Y, saltando para os lados)
    sub = bc.Part("tmp", MATS)
    for side in (-1, 1):
        stone_face(sub, rng, -w / 2, w / 2, 0.2, h - 0.2, side * (w / 2 - 0.08), side, rows=rows, moss=0.2)
    sub.bm.transform(Matrix.Rotation(math.pi / 2, 4, "Z"))
    _merge(p, sub)
    p.box((0, 0, h - 0.1), (w + 0.14, w + 0.14, 0.2), "PedraClara", "root")  # capa
    p.box((0, 0, h + 0.04), (w - 0.06, w - 0.06, 0.08), "PedraMuro", "root")


def _merge(p, sub):
    """Junta o bmesh de outra Part (com os materiais dela) nesta."""
    mesh = bpy.data.meshes.new("tmp")
    sub.bm.to_mesh(mesh)
    remap = [p._index(p.mats, m) for m in sub.mats]
    before = set(p.bm.faces)
    p.bm.from_mesh(mesh)
    for f in p.bm.faces:
        if f not in before:
            f.material_index = remap[f.material_index] if f.material_index < len(remap) else 0
    sub.bm.free()
    bpy.data.meshes.remove(mesh)


def build_pilar_portao(col):
    """Pilar do portão (0,7 × 0,7 × 2,4 m) com o lampião em cima: o marco da chegada.
    Origem no meio da base. No Unity ganha a luz do lampião (Y 2,75)."""
    rng = rng_for("PilarPortao")
    p = bc.Part("SM_ENV_PilarPortao", MATS)
    pillar(p, rng, 2.4)
    lantern(p, 0, 0, 2.48, scale=1.1)
    return p.to_static(col, bevel_segments=1)


def build_pilar_muro(col):
    """Pilar do muro e da grade (0,7 × 0,7 × 2,0 m), com uma bola de pedra no topo."""
    rng = rng_for("PilarMuro")
    p = bc.Part("SM_ENV_PilarMuro", MATS)
    pillar(p, rng, 2.0)
    p.tube((0, 0, 2.08), (0, 0, 2.16), 0.14, 0.1, "PedraClara", "root", seg=12)
    p.ellipsoid((0, 0, 2.32), (0.18, 0.18, 0.18), "PedraMusgo", "root", seg=12, rings=8)
    return p.to_static(col, bevel_segments=1)


GATE_W, GATE_H = 4.4, 2.6


def build_portao_ferro(col):
    """Portão de ferro de duas folhas (4,4 × 2,6 m no meio do arco): barras com ponta de lança,
    duas travessas, um arco de volutas por cima e a corrente com cadeado. Fechado (é o fim do
    mapa para quem chega: o caminhão entrou e o portão ficou trancado). Origem no meio da base."""
    rng = rng_for("PortaoFerro")
    p = bc.Part("SM_ENV_PortaoFerro", MATS)
    half = GATE_W / 2
    for side in (-1, 1):
        x_out, x_in = side * half, side * 0.02
        lo, hi = min(x_out, x_in), max(x_out, x_in)
        p.box(((lo + hi) / 2, 0, 0.18), (hi - lo, 0.05, 0.05), "Ferro", "root")         # travessa de baixo
        p.box(((lo + hi) / 2, 0, 1.9), (hi - lo, 0.05, 0.05), "Ferro", "root")          # travessa de cima
        p.box((x_out - side * 0.03, 0, 1.3), (0.06, 0.06, 2.6), "Ferro", "root")        # montante da dobradiça
        p.box((x_in, 0, 1.3), (0.05, 0.06, 2.6), "Ferro", "root")
        n = 15
        for i in range(n):
            x = x_out - side * (0.12 + i * (half - 0.16) / (n - 1))
            t = abs(x) / half
            top = 2.15 + 0.45 * math.cos(t * math.pi / 2)                  # o arco: mais alto no meio
            mat = "Ferrugem" if rng.random() < 0.25 else "Ferro"
            p.tube((x, 0, 0.04), (x, 0, top), 0.013, 0.013, mat, "root", seg=6, smooth=False)
            spear(p, x, 0, top)
        for k in range(3):                                                 # volutas entre as travessas
            cx = side * (0.45 + k * 0.7)
            pts = [(cx + math.cos(a) * 0.17, 0, 1.0 + math.sin(a) * 0.17) for a in
                   [i * 2 * math.pi / 14 for i in range(15)]]
            p.polyline(pts, 0.012, "Ferro", "root", seg=6)
    # o arco de cima, de pilar a pilar
    arc = [(x, 0, 2.15 + 0.45 * math.cos(abs(x) / half * math.pi / 2) - 0.12)
           for x in [-half + i * GATE_W / 20 for i in range(21)]]
    p.polyline(arc, 0.022, "Ferro", "root", seg=6)
    # corrente com cadeado no meio
    for i in range(6):
        p.ellipsoid((0.0, -0.04, 1.2 - i * 0.07), (0.03, 0.012, 0.04), "Ferrugem", "root", seg=8, rings=4,
                    rot=(0, 90 * (i % 2), 0))
    p.box((0, -0.06, 0.74), (0.09, 0.04, 0.11), "Ferrugem", "root")
    return p.to_static(col, bevel=0)  # barra fina não precisa de bisel (e ele triplicava o portão)


def build_grade_ferro(col):
    """Grade de ferro de 4 m sobre uma mureta de pedra (0,5 m): a frente da propriedade, dos dois
    lados do portão. Barras de 1,7 m com ponta de lança. Origem no meio da base; corre em X."""
    rng = rng_for("GradeFerro")
    p = bc.Part("SM_ENV_GradeFerro", MATS)
    base = 0.5
    p.box((0, 0, base / 2), (MURO_W, 0.32, base), "PedraRejunte", "root")
    for side in (-1, 1):
        stone_face(p, rng, -MURO_W / 2, MURO_W / 2, 0.0, base - 0.1, side * 0.1, side, rows=1, depth=(0.06, 0.1))
    x = -MURO_W / 2
    while x < MURO_W / 2 - 0.05:
        w = min(rng.uniform(0.5, 0.8), MURO_W / 2 - x)
        p.box((x + w / 2, 0, base - 0.05), (w - 0.03, 0.44, 0.1), rng.choice(("PedraClara", "PedraMuro")), "root")
        x += w
    for z in (base + 0.12, base + 1.45):
        p.box((0, 0, z), (MURO_W, 0.04, 0.04), "Ferro", "root")
    n = 28
    for i in range(n):
        x = -MURO_W / 2 + (i + 0.5) * MURO_W / n
        top = base + 1.7 + (0.06 if i % 2 else 0.0)
        p.tube((x, 0, base), (x, 0, top), 0.011, 0.011, "Ferrugem" if rng.random() < 0.2 else "Ferro", "root", seg=6, smooth=False)
        spear(p, x, 0, top, size=0.04)
    return p.to_static(col, bevel_segments=1)


# ---------------------------------------------------------------- chão: lajes, canteiro, folhas

def build_laje_caminho(col):
    """Laje do caminho de 2 × 2 m: pedras irregulares (umas mais altas, tortas, umas gastas), com
    terra e um pouco de grama entre elas. O topo fica rente ao chão (Z 0 a 2 cm): ninguém tropeça.
    Origem no meio. Gira de 90 em 90° no Unity para não repetir."""
    rng = rng_for("LajeCaminho")
    p = bc.Part("SM_ENV_LajeCaminho", MATS)
    p.box((0, 0, -0.01), (2.0, 2.0, 0.02), "Terra", "root")
    n, s = 4, 0.5
    for i in range(n):
        for j in range(n):
            if rng.random() < 0.06:
                continue                                                   # uma pedra que falta
            cx = -1 + s / 2 + i * s + rng.uniform(-0.04, 0.04)
            cy = -1 + s / 2 + j * s + rng.uniform(-0.04, 0.04)
            w, d = s - rng.uniform(0.04, 0.09), s - rng.uniform(0.04, 0.09)
            top = rng.uniform(0.006, 0.02)
            mat = rng.choice(("PedraMuro", "PedraClara", "PedraEscura", "PedraMuro"))
            p.box((cx, cy, top - 0.03), (w, d, 0.06), mat, "root", rot_z=rng.uniform(-7, 7))
    for _ in range(6):                                                     # tufos de grama nas juntas
        x, y = rng.uniform(-0.95, 0.95), rng.uniform(-0.95, 0.95)
        p.ellipsoid((x, y, 0.01), (0.06, 0.05, 0.03), "Grama", "root", seg=6, rings=4)
    return p.to_static(col, bevel=0.008, bevel_segments=1)


def build_canteiro(col):
    """Canteiro abandonado (3,0 × 1,2 m): borda de pedra baixa, terra, plantas secas e ervas
    daninhas. Origem no meio da base."""
    rng = rng_for("Canteiro")
    p = bc.Part("SM_ENV_Canteiro", MATS)
    w, d = 3.0, 1.2
    p.box((0, 0, 0.06), (w - 0.2, d - 0.2, 0.06), "Terra", "root")
    for sx, sy, lx, ly in ((0, -1, w, 0.18), (0, 1, w, 0.18), (-1, 0, 0.18, d), (1, 0, 0.18, d)):
        n = max(2, round(max(lx, ly) / 0.4))
        for k in range(n):
            t = (k + 0.5) / n - 0.5
            cx = sx * (w / 2 - 0.09) if sx else t * w
            cy = sy * (d / 2 - 0.09) if sy else t * d
            p.box((cx, cy, 0.09), (lx / n - 0.03 if not sx else 0.18, ly / n - 0.03 if not sy else 0.18, 0.18),
                  rng.choice(("PedraMuro", "PedraEscura", "PedraMusgo")), "root", rot_z=rng.uniform(-6, 6))
    for _ in range(9):                                                     # plantas secas
        x, y = rng.uniform(-1.2, 1.2), rng.uniform(-0.35, 0.35)
        h = rng.uniform(0.25, 0.6)
        for k in range(3):
            a = rng.uniform(0, 2 * math.pi)
            p.tube((x, y, 0.08), (x + math.cos(a) * 0.12, y + math.sin(a) * 0.12, 0.08 + h), 0.008, 0.003,
                   "FolhagemSeca", "root", seg=4, smooth=False)
    for _ in range(5):                                                     # erva daninha
        x, y = rng.uniform(-1.2, 1.2), rng.uniform(-0.35, 0.35)
        f = p.ellipsoid((x, y, 0.14), (0.16, 0.14, 0.1), "Folhagem", "root", seg=8, rings=6)
        p.wrinkle(f, 0.06, scale=0.08, seed=x)
    return p.to_static(col, bevel_segments=1)


def build_folhas_chao(col):
    """Folhas caídas e galhinhos (um tapete de ~2,5 m): espalhar perto das árvores e do caminho.
    Só visual (sem colisão). Origem no meio, no chão."""
    rng = rng_for("FolhasChao")
    p = bc.Part("SM_ENV_FolhasChao", MATS)
    for _ in range(70):
        r = 1.25 * math.sqrt(rng.random())
        a = rng.uniform(0, 2 * math.pi)
        x, y = math.cos(a) * r, math.sin(a) * r
        p.box((x, y, 0.004 + rng.uniform(0, 0.006)), (rng.uniform(0.05, 0.09), rng.uniform(0.03, 0.05), 0.004),
              rng.choice(("FolhagemSeca", "FolhagemSecaEscura", "FolhagemSeca", "Folhagem")), "root",
              rot_z=rng.uniform(0, 180), rot_y=rng.uniform(-10, 10))
    for _ in range(6):
        x, y = rng.uniform(-1, 1), rng.uniform(-1, 1)
        a = rng.uniform(0, math.pi)
        length = rng.uniform(0.25, 0.6)
        p.tube((x, y, 0.012), (x + math.cos(a) * length, y + math.sin(a) * length, 0.012), 0.01, 0.005,
               "MadeiraMorta", "root", seg=5, smooth=False)
    return p.to_static(col, bevel=0)


# ---------------------------------------------------------------- luminária e fonte

def build_poste(col):
    """Luminária antiga (3,1 m): base de degraus, coluna canelada de ferro, braço e o lampião.
    O braço sai para -Y (= +Z no Unity): no Unity, a luz fica em (0; 2,95; 0,55).
    Origem no meio da base."""
    p = bc.Part("SM_ENV_Poste", MATS)
    p.tube((0, 0, 0), (0, 0, 0.12), 0.22, 0.2, "PedraEscura", "root", seg=10, smooth=False)
    p.tube((0, 0, 0.12), (0, 0, 0.32), 0.14, 0.1, "Ferro", "root", seg=10, smooth=False)
    p.tube((0, 0, 0.32), (0, 0, 2.7), 0.06, 0.045, "Ferro", "root", seg=8, smooth=False)  # coluna
    for k in range(8):                                                     # caneluras (frisos)
        a = 2 * math.pi * k / 8
        p.tube((math.cos(a) * 0.058, math.sin(a) * 0.058, 0.4), (math.cos(a) * 0.045, math.sin(a) * 0.045, 2.6),
               0.009, 0.008, "Ferro", "root", seg=4, smooth=False)
    p.tube((0, 0, 2.62), (0, 0, 2.78), 0.08, 0.06, "Ferrugem", "root", seg=10, smooth=False)
    # braço em curva e o lampião pendurado
    arm = [(0, -math.sin(t) * 0.5, 2.7 + (1 - math.cos(t)) * 0.0 + math.sin(t * 2) * 0.12)
           for t in [i * (math.pi / 2) / 8 for i in range(9)]]
    p.polyline(arm, 0.018, "Ferro", "root", seg=6)
    p.tube((0, -0.5, 2.78), (0, -0.5, 2.66), 0.008, 0.008, "Ferro", "root", seg=4)
    lantern(p, 0, -0.55, 2.38, scale=0.9)
    p.box((0.0, 0.0, 1.2), (0.04, 0.13, 0.04), "Ferro", "root")            # o gancho da escada do acendedor
    return p.to_static(col, bevel=0)


def build_fonte(col):
    """A FONTE do pátio (Ø 4,6 m): bacia de pedra com água escura, coluna, taça de cima e uma
    estátua gasta (uma figura de capa, sem rosto). É o ponto de orientação do pátio.
    Origem no meio da base. No Unity: colisão de cilindro (raio 2,3, 0,7 m) e uma luz fraca."""
    rng = rng_for("Fonte")
    p = bc.Part("SM_ENV_Fonte", MATS)
    r = 2.3
    n = 22
    for k in range(n):                                                     # mureta da bacia, bloco por bloco
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
        am = (a0 + a1) / 2
        length = 2 * r * math.sin(math.pi / n) + 0.02
        p.box((math.cos(am) * (r - 0.16), math.sin(am) * (r - 0.16), 0.3), (0.32, length, 0.6),
              rng.choice(("PedraMuro", "PedraClara", "PedraMuro")), "root", rot_z=math.degrees(am))
        p.box((math.cos(am) * (r - 0.14), math.sin(am) * (r - 0.14), 0.64), (0.42, length + 0.03, 0.08),
              rng.choice(("PedraClara", "PedraMusgo")), "root", rot_z=math.degrees(am))
    p.tube((0, 0, 0.0), (0, 0, 0.1), r - 0.3, r - 0.3, "PedraEscura", "root", seg=24, smooth=False)
    p.tube((0, 0, 0.1), (0, 0, 0.48), r - 0.31, r - 0.31, "Agua", "root", seg=24, smooth=False)  # a água
    # coluna e taça
    p.tube((0, 0, 0.48), (0, 0, 0.62), 0.45, 0.38, "PedraEscura", "root", seg=12)
    p.tube((0, 0, 0.62), (0, 0, 1.35), 0.2, 0.16, "PedraMuro", "root", seg=12)
    for z in (0.75, 1.2):
        p.tube((0, 0, z), (0, 0, z + 0.06), 0.24, 0.24, "PedraClara", "root", seg=12)
    bowl = p.ellipsoid((0, 0, 1.55), (0.85, 0.85, 0.3), "PedraMuro", "root", seg=18, rings=10)
    cut_faces(p, bowl, lambda c: c.z < 1.55 * bc.K)
    inner = p.ellipsoid((0, 0, 1.56), (0.78, 0.78, 0.24), "PedraEscura", "root", seg=18, rings=10)
    cut_faces(p, inner, lambda c: c.z < 1.56 * bc.K)
    flip(p, inner)
    ring(p, (0, 0), 0.82, 1.56, 0.04, "PedraClara", seg=18)
    p.tube((0, 0, 1.4), (0, 0, 1.47), 0.7, 0.7, "Agua", "root", seg=18, smooth=False)
    # a estátua: base, capa (pano que cai), cabeça baixa, braços segurando uma bilha que verte
    p.tube((0, 0, 1.47), (0, 0, 1.65), 0.22, 0.2, "PedraClara", "root", seg=10)
    cloak = p.ellipsoid((0, 0, 2.2), (0.24, 0.2, 0.6), "PedraMuro", "root", seg=14, rings=10)
    p.wrinkle(cloak, 0.05, scale=0.1, seed=3.0, stretch_z=3.0)
    p.ellipsoid((0, -0.03, 2.84), (0.11, 0.12, 0.13), "PedraMuro", "root", seg=12, rings=8)  # cabeça baixa
    veil = p.ellipsoid((0, 0.03, 2.82), (0.15, 0.13, 0.2), "PedraMusgo", "root", seg=12, rings=8)  # véu, por trás
    p.wrinkle(veil, 0.025, scale=0.06, seed=5.0, stretch_z=3.0)
    p.limb([(0.15, -0.05, 2.6), (0.18, -0.22, 2.4), (0.05, -0.28, 2.35)], (0.05, 0.045, 0.04), "PedraMuro", ["root"] * 3, seg=8)
    p.limb([(-0.15, -0.05, 2.6), (-0.16, -0.22, 2.42), (-0.03, -0.28, 2.37)], (0.05, 0.045, 0.04), "PedraMuro", ["root"] * 3, seg=8)
    p.ellipsoid((0.01, -0.33, 2.36), (0.1, 0.1, 0.12), "PedraClara", "root", seg=10, rings=6)  # bilha
    p.tube((0.01, -0.42, 2.38), (0.01, -0.5, 2.32), 0.03, 0.025, "PedraClara", "root", seg=8)
    p.tube((0.01, -0.52, 2.28), (0.01, -0.7, 1.5), 0.012, 0.02, "Agua", "root", seg=6)  # o fio d'água
    for _ in range(7):                                                     # musgo no pé da mureta
        a = rng.uniform(0, 2 * math.pi)
        f = p.ellipsoid((math.cos(a) * (r + 0.02), math.sin(a) * (r + 0.02), 0.05), (0.25, 0.25, 0.08),
                        "PedraMusgo", "root", seg=8, rings=5)
        p.wrinkle(f, 0.04, scale=0.07, seed=a)
    return p.to_static(col, bevel_segments=1)


# ---------------------------------------------------------------- vegetação e pedras

def skirt(p, rng, z_top, z_tip, r_top, r_tip, n, mat, under="FolhagemPinheiroEscura"):
    """Um ANDAR de pinheiro: uma "saia" contínua de agulhas que sai do tronco, abre e cai nas
    pontas. A borda é serrilhada (2n pontas, uma longa e uma curta, cada uma caindo de um tanto):
    é esse recorte, andar sobre andar, que faz ler "pinheiro" de longe contra a névoa. Por baixo,
    um fundo escuro fecha a saia (quem olha de baixo não vê o oco)."""
    bm = p.bm
    k = bc.K
    turn = rng.uniform(0, 2 * math.pi)
    rings = []
    for i, (rf, zf) in enumerate(((0.0, 0.0), (0.55, 0.45), (1.0, 1.0))):
        ring = []
        for j in range(2 * n):
            a = turn + math.pi * j / n + (rng.uniform(-0.08, 0.08) if i else 0.0)
            long_tip = j % 2 == 0
            spread = (1.0 if long_tip else 0.74) * rng.uniform(0.9, 1.1) if i else 0.0
            r = r_top + (r_tip - r_top) * rf * spread
            droop = (0.22 if long_tip else 0.08) * (r_tip - r_top) * rf * rf
            z = z_top + (z_tip - z_top) * zf - droop + (rng.uniform(-0.04, 0.04) * (r_tip - r_top) if i else 0.0)
            bulge = 1.08 if i == 1 else 1.0                                 # a saia estufa: tem volume
            ring.append(bm.verts.new((math.cos(a) * r * bulge * k, math.sin(a) * r * bulge * k, z * k)))
        rings.append(ring)
    center = bm.verts.new((0, 0, (z_tip + (z_top - z_tip) * 0.35) * k))
    m_out, m_under = p._index(p.mats, mat), p._index(p.mats, under)
    gi = p._index(p.groups, "root")
    for v in [v for r in rings for v in r] + [center]:
        v[p.deform][gi] = 1.0
    for i in range(2):                                                       # a saia, por cima
        a, b = rings[i], rings[i + 1]
        for j in range(2 * n):
            j2 = (j + 1) % (2 * n)
            bm.faces.new((a[j], b[j], b[j2], a[j2])).material_index = m_out
    for j in range(2 * n):                                                   # o fundo escuro
        bm.faces.new((rings[2][(j + 1) % (2 * n)], rings[2][j], center)).material_index = m_under


def pine(p, rng, height, trunk_r):
    """Pinheiro: tronco que afina e ANDARES de agulhas em saia (skirt), cada um girado e de um
    verde, ficando menores e mais juntos para o alto, até a ponteira."""
    p.tube((0, 0, 0), (0, 0, height * 0.9), trunk_r, trunk_r * 0.2, "MadeiraCasca", "root", seg=8)
    for k in range(4):                                                     # raízes aparentes
        a = 2 * math.pi * k / 4 + rng.uniform(-0.3, 0.3)
        p.tube((0, 0, 0.25), (math.cos(a) * trunk_r * 3, math.sin(a) * trunk_r * 3, -0.05), trunk_r * 0.5, trunk_r * 0.15,
               "MadeiraCasca", "root", seg=5, smooth=False)
    layers = 10
    base = height * 0.16
    for k in range(layers):
        t = k / (layers - 1)
        # para o alto: andares menores e mais juntos, e cada saia cobre o pé da de cima
        z_tip = base + (height * 0.8 - base) * t ** 0.85
        r_tip = (1 - t * 0.88) * height * 0.24 * rng.uniform(0.92, 1.08)
        z_top = z_tip + r_tip * 0.95 + height * 0.02
        mat = rng.choice(("FolhagemPinheiro", "FolhagemPinheiro", "FolhagemPinheiroEscura"))
        skirt(p, rng, z_top, z_tip, 0.05, r_tip, max(6, round(11 - t * 5)), mat)
    skirt(p, rng, height, height * 0.86, 0.01, height * 0.045, 5, "FolhagemPinheiro")  # ponteira


def build_pinheiro(name, height, seed_name):
    def build(col):
        rng = rng_for(seed_name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        pine(p, rng, height, 0.12 + height * 0.012)
        return p.to_static(col, bevel=0)
    return build


def broadleaf(p, rng, height):
    """Árvore de copa larga: tronco com curva e pé alargado que se divide em galhos (e cada galho
    em dois), e uma copa de MUITOS chumaços: os de cima claros, os de baixo escuros (a sombra da
    própria copa), achatados embaixo, com vãos onde aparecem os galhos. Amassados pelo ruído."""
    trunk_top = height * 0.42
    lean = (rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25))
    trunk = [(0, 0, 0), (lean[0] * 0.3, lean[1] * 0.3, trunk_top * 0.45), (lean[0], lean[1], trunk_top)]
    p.tube(trunk[0], trunk[1], 0.34, 0.24, "MadeiraCasca", "root", seg=9, smooth=False, caps=False)
    p.tube(trunk[1], trunk[2], 0.245, 0.19, "MadeiraCasca", "root", seg=9, smooth=False)
    for k in range(5):                                                     # raízes
        a = 2 * math.pi * k / 5 + rng.uniform(-0.3, 0.3)
        p.tube((0, 0, 0.45), (math.cos(a) * 0.85, math.sin(a) * 0.85, -0.05), 0.2, 0.05, "MadeiraCasca", "root", seg=6, smooth=False)
    tips = []
    for k in range(5):
        a = 2 * math.pi * k / 5 + rng.uniform(-0.35, 0.35)
        reach = height * rng.uniform(0.16, 0.24)
        up = height * rng.uniform(0.14, 0.24)
        start = (lean[0], lean[1], trunk_top - 0.25)
        mid = (lean[0] + math.cos(a) * reach * 0.5, lean[1] + math.sin(a) * reach * 0.5, trunk_top + up * 0.55)
        p.limb([start, mid], (0.16, 0.1), "MadeiraCasca", ["root"] * 2, seg=6)
        for side in (-0.45, 0.45):                                         # o galho se divide em dois
            b = a + side + rng.uniform(-0.15, 0.15)
            tip = (mid[0] + math.cos(b) * reach * 0.6, mid[1] + math.sin(b) * reach * 0.6, trunk_top + up + rng.uniform(-0.3, 0.4))
            p.limb([mid, tip], (0.1, 0.035), "MadeiraCasca", ["root"] * 2, seg=5)
            tips.append(tip)
    # A copa: um volume (elipsoide de 0,3 × 0,3 × 0,2 da altura) com um MIOLO escuro que fecha os
    # buracos e chumaços espalhados pela casca dele, mais densos em cima. Um chumaço em cada ponta
    # de galho amarra a copa aos galhos.
    cc = (lean[0], lean[1], trunk_top + height * 0.3)
    cr = (height * 0.29, height * 0.27, height * 0.2)
    core = p.ellipsoid(cc, (cr[0] * 0.8, cr[1] * 0.8, cr[2] * 0.75), "FolhagemEscura", "root", seg=10, rings=7)
    p.wrinkle(core, height * 0.03, scale=height * 0.05, seed=9.0)
    spots = [t for t in tips]
    for _ in range(14):
        th = rng.uniform(0, 2 * math.pi)
        ph = math.acos(rng.uniform(-0.35, 1.0))                              # mais em cima que embaixo
        spots.append((cc[0] + math.sin(ph) * math.cos(th) * cr[0], cc[1] + math.sin(ph) * math.sin(th) * cr[1],
                      cc[2] + math.cos(ph) * cr[2]))
    for i, c in enumerate(spots):
        s = height * rng.uniform(0.08, 0.12)
        low = c[2] < cc[2] - cr[2] * 0.2
        mat = "FolhagemEscura" if low else rng.choice(("Folhagem", "Folhagem", "FolhagemEscura"))
        f = p.ellipsoid(c, (s, s * rng.uniform(0.85, 1.1), s * 0.7), mat, "root", seg=8, rings=6)
        p.wrinkle(f, s * 0.3, scale=s * 0.4, seed=i * 3.1)


def build_folhosa(name, height, seed_name):
    def build(col):
        rng = rng_for(seed_name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        broadleaf(p, rng, height)
        return p.to_static(col, bevel=0)
    return build


def build_arvore_retorcida(col):
    """A ÁRVORE RETORCIDA (9 m): silhueta seca e torta contra a lua, perto de um canto do terreno.
    Tronco que gira em espiral, galhos que se dividem três vezes e terminam em pontas finas,
    raízes grossas para fora da terra. Sem folha nenhuma."""
    rng = rng_for("ArvoreRetorcida")
    p = bc.Part("SM_ENV_ArvoreRetorcida", MATS)
    pts, radii = [], []
    for i in range(8):
        t = i / 7
        a = t * 2.6
        pts.append((math.cos(a) * 0.6 * t, math.sin(a) * 0.45 * t, t * 5.0))
        radii.append(0.55 - t * 0.33)
    p.limb(pts, radii, "MadeiraMorta", ["root"] * len(pts), seg=10)

    def branch(start, direction, length, radius, depth):
        d = Vector(direction).normalized()
        segs = 3
        cur = Vector(start)
        chain, rs = [tuple(cur)], [radius]
        for s in range(segs):
            d = (d + Vector((rng.uniform(-0.45, 0.45), rng.uniform(-0.45, 0.45), rng.uniform(-0.2, 0.25)))).normalized()
            cur = cur + d * (length / segs)
            chain.append(tuple(cur))
            rs.append(radius * (1 - (s + 1) / (segs + 0.6)))
        p.limb(chain, rs, "MadeiraMorta", ["root"] * len(chain), seg=6 if depth else 5)
        if depth > 0:
            for k in range(2 if depth < 2 else 3):
                a = rng.uniform(0, 2 * math.pi)
                nd = d + Vector((math.cos(a), math.sin(a), 0.3)) * 0.9
                branch(cur, nd, length * 0.62, rs[-1] * 1.1, depth - 1)

    top = Vector(pts[-1])
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.4
        branch(top, (math.cos(a), math.sin(a), 0.7), 3.2, 0.2, 2)
    for k in range(2):                                                     # galhos baixos, quase horizontais
        a = rng.uniform(0, 2 * math.pi)
        branch(Vector(pts[3]), (math.cos(a), math.sin(a), 0.15), 2.8, 0.16, 1)
    for k in range(6):                                                     # raízes
        a = 2 * math.pi * k / 6 + rng.uniform(-0.25, 0.25)
        reach = rng.uniform(1.4, 2.4)
        p.limb([(0, 0, 0.6), (math.cos(a) * reach * 0.5, math.sin(a) * reach * 0.5, 0.15),
                (math.cos(a) * reach, math.sin(a) * reach, -0.1)], (0.3, 0.17, 0.05), "MadeiraMorta", ["root"] * 3, seg=6)
    return p.to_static(col, bevel=0)


def build_arbusto(name, size, seed_name, mats=("Folhagem", "FolhagemEscura")):
    """Arbusto: três a cinco chumaços amassados, com galhos secos saindo (negligenciado)."""
    def build(col):
        rng = rng_for(seed_name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        for k in range(rng.randint(3, 5)):
            c = (rng.uniform(-0.35, 0.35) * size, rng.uniform(-0.35, 0.35) * size, size * rng.uniform(0.3, 0.5))
            s = size * rng.uniform(0.35, 0.5)
            f = p.ellipsoid(c, (s, s, s * 0.8), mats[k % 2], "root", seg=12, rings=8)
            p.wrinkle(f, s * 0.4, scale=s * 0.4, seed=k * 1.7)
        for k in range(5):
            a = rng.uniform(0, 2 * math.pi)
            p.tube((0, 0, size * 0.3), (math.cos(a) * size * 0.75, math.sin(a) * size * 0.75, size * rng.uniform(0.7, 1.1)),
                   0.015, 0.004, "MadeiraMorta", "root", seg=4, smooth=False)
        return p.to_static(col, bevel=0)
    return build


def build_pedra(name, size, seed_name):
    """Rocha: elipsoide achatado e quebrado pelo ruído, metade enterrada, com musgo por cima."""
    def build(col):
        rng = rng_for(seed_name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        for k in range(rng.randint(1, 3)):
            c = (rng.uniform(-0.3, 0.3) * size if k else 0, rng.uniform(-0.3, 0.3) * size if k else 0, size * 0.12)
            s = size * (0.55 if k == 0 else rng.uniform(0.25, 0.4))
            f = p.ellipsoid(c, (s, s * rng.uniform(0.7, 0.95), s * rng.uniform(0.5, 0.75)), "PedraMuro" if k else "PedraEscura",
                            "root", seg=12, rings=8, smooth=True)
            p.wrinkle(f, s * 0.35, scale=s * 0.6, seed=k * 2.3 + size)
            moss = p.ellipsoid((c[0], c[1], c[2] + s * 0.45), (s * 0.7, s * 0.6, s * 0.25), "PedraMusgo", "root", seg=10, rings=6)
            p.wrinkle(moss, s * 0.15, scale=s * 0.3, seed=k + 9)
        return p.to_static(col, bevel=0)
    return build


# ---------------------------------------------------------------- construções de apoio

def build_galpao(col):
    """GALPÃO de serviço (4,0 × 3,0 m; 2,5 m na beirada, 3,5 m na cumeeira): tábuas na vertical
    (de dois tons, umas faltando), porta com travessa em Z, janelinha com vidro sujo, telhado de
    tábuas com beiral e um caixote na porta. A porta olha para -Y (= +Z no Unity).
    Origem no meio da base. No Unity: colisão de caixa."""
    rng = rng_for("Galpao")
    p = bc.Part("SM_ENV_Galpao", MATS)
    w, d, eave, ridge = 4.0, 3.0, 2.5, 3.5
    p.box((0, 0, 0.1), (w + 0.1, d + 0.1, 0.2), "PedraEscura", "root")   # sapata
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            p.box((x, y, eave / 2 + 0.1), (0.14, 0.14, eave), "MadeiraVelha", "root")

    def boards(part, x0, x1, y, top_of):
        """Uma parede de tábuas em pé, de x0 a x1, no plano y (a altura de cada uma: top_of)."""
        x = x0
        i = 0
        while x < x1 - 0.02:
            bw = 0.2
            if rng.random() < 0.05:
                x += bw
                continue                                                   # tábua faltando
            top = top_of(x + bw / 2)
            part.box((x + bw / 2, y, 0.2 + (top - 0.2) / 2), (bw - 0.015, 0.04, top - 0.2),
                     "MadeiraVelha" if i % 2 else "MadeiraVelhaClara", "root", rot_y=rng.uniform(-0.6, 0.6))
            x += bw
            i += 1

    slope = (ridge - eave) / (w / 2)
    gable = lambda x: eave + (w / 2 - abs(x)) * slope                      # noqa: E731 (oitão)
    # frente com a porta (vão de 1,1 m no meio)
    boards(p, -w / 2, -0.55, -d / 2, gable)
    boards(p, 0.55, w / 2, -d / 2, gable)
    p.box((0, -d / 2, 2.35), (1.1, 0.04, 0.3), "MadeiraVelha", "root")      # verga
    boards(p, -w / 2, w / 2, d / 2, gable)                                 # fundos
    sides = bc.Part("tmp", MATS)                                          # laterais: montadas em X e giradas
    boards(sides, -d / 2, d / 2, -w / 2, lambda x: eave)
    boards(sides, -d / 2, d / 2, w / 2, lambda x: eave)
    sides.bm.transform(Matrix.Rotation(math.pi / 2, 4, "Z"))
    _merge(p, sides)
    # porta (verde gasto, entreaberta 10°) com a travessa em Z
    p.box((0, -d / 2 - 0.06, 1.2), (1.06, 0.05, 2.1), "MadeiraPintada", "root", rot_z=10)
    for z in (0.45, 1.95):
        p.box((0, -d / 2 - 0.1, z), (1.0, 0.04, 0.12), "MadeiraVelhaClara", "root", rot_z=10)
    p.box((0, -d / 2 - 0.1, 1.2), (1.6, 0.04, 0.12), "MadeiraVelhaClara", "root", rot_y=-55, rot_z=10)
    # janela na lateral +X
    p.box((w / 2 + 0.03, 0.4, 1.5), (0.04, 0.7, 0.55), "VidroJanela", "root")
    for z in (1.2, 1.8):
        p.box((w / 2 + 0.05, 0.4, z), (0.06, 0.82, 0.06), "MadeiraVelhaClara", "root")
    p.box((w / 2 + 0.06, 0.4, 1.5), (0.04, 0.05, 0.6), "MadeiraVelhaClara", "root")
    # telhado: duas águas de tábuas, com beiral
    pitch = math.degrees(math.atan(slope))
    run = (w / 2 + 0.35) / math.cos(math.radians(pitch))
    for side in (-1, 1):
        n = 9
        for k in range(n):
            y = -d / 2 - 0.3 + (k + 0.5) * (d + 0.6) / n
            cx = side * (w / 4 + 0.17)
            cz = eave + (ridge - eave) / 2 + 0.05 + rng.uniform(-0.01, 0.01)
            p.box((cx, y, cz), (run, (d + 0.6) / n - 0.02, 0.05), "TabuaTelhado", "root",
                  rot_y=side * pitch + rng.uniform(-1, 1))
    p.box((0, 0, ridge + 0.06), (0.2, d + 0.65, 0.1), "MadeiraVelha", "root")  # cumeeira
    # caixote e um balde na porta (escala e história)
    p.box((1.3, -d / 2 - 0.5, 0.25), (0.5, 0.5, 0.5), "MadeiraVelhaClara", "root", rot_z=12)
    p.tube((-1.3, -d / 2 - 0.4, 0.0), (-1.3, -d / 2 - 0.4, 0.3), 0.15, 0.18, "Ferrugem", "root", seg=10)
    return p.to_static(col, bevel_segments=1)


def build_doca(col):
    """Pequena DOCA de madeira (1,8 × 7 m): tábuas com frestas e umas faltando, sobre estacas que
    entram na água. Começa na terra (origem, Y 0) e avança para -Y (= +Z no Unity). O piso fica
    20 cm acima do chão."""
    rng = rng_for("Doca")
    p = bc.Part("SM_ENV_Doca", MATS)
    w, length, top = 1.8, 7.0, 0.22
    for x in (-w / 2 + 0.1, w / 2 - 0.1):                                  # vigas
        p.box((x, -length / 2, top - 0.1), (0.12, length, 0.12), "MadeiraVelha", "root")
    k, y = 0, 0.0
    while y > -length + 0.1:
        if rng.random() > 0.06:
            p.box((rng.uniform(-0.02, 0.02), y - 0.09, top), (w + rng.uniform(-0.05, 0.1), 0.17, 0.04),
                  "MadeiraVelha" if k % 2 else "MadeiraVelhaClara", "root", rot_z=rng.uniform(-1.5, 1.5))
        y -= 0.2
        k += 1
    for i in range(4):                                                     # estacas
        yy = -0.4 - i * (length - 0.6) / 3
        for x in (-w / 2, w / 2):
            p.tube((x, yy, -0.8), (x, yy, top + 0.35 + rng.uniform(-0.05, 0.1)), 0.08, 0.075, "MadeiraVelha", "root", seg=8)
    # cabo amarrado na estaca da ponta (vai até o barco)
    p.polyline([(w / 2, -length + 0.2, top + 0.25), (w / 2 + 0.4, -length + 0.1, top - 0.05),
                (w / 2 + 0.9, -length - 0.05, top - 0.1)], 0.012, "MadeiraMorta", "root", seg=5)
    return p.to_static(col, bevel_segments=1)


def build_barco(col):
    """Barco a remo (3,0 × 1,2 m), de madeira pintada e descascada: casco de verdade (por dentro
    também), banco, borda e os dois remos. Flutua: a linha d'água fica 15 cm acima da origem.
    A proa aponta para -Y (= +Z no Unity)."""
    p = bc.Part("SM_ENV_Barco", MATS)
    zc = 0.5
    outer = p.ellipsoid((0, 0, zc), (0.62, 1.5, 0.45), "MadeiraPintada", "root", seg=20, rings=12)
    cut_faces(p, outer, lambda c: c.z < zc * bc.K - 0.001)
    inner = p.ellipsoid((0, 0, zc + 0.005), (0.57, 1.44, 0.4), "MadeiraVelha", "root", seg=20, rings=12)
    cut_faces(p, inner, lambda c: c.z < zc * bc.K)
    flip(p, inner)
    # proa afinada: puxa os vértices da frente para o meio
    for v in p.bm.verts:
        if v.co.y < -0.6 * bc.K:
            t = (-v.co.y / bc.K - 0.6) / 0.9
            v.co.x *= max(0.15, 1 - t * 0.8)
    ring(p, (0, 0), 0.6, zc, 0.03, "MadeiraVelhaClara", seg=28, squash=2.45)
    for y in (-0.35, 0.45):
        p.box((0, y, zc - 0.1), (1.05, 0.22, 0.04), "MadeiraVelhaClara", "root")  # bancos
    for side in (-1, 1):                                                   # remos dentro do barco
        p.tube((side * 0.25, 0.9, zc - 0.05), (side * 0.12, -0.9, zc - 0.12), 0.022, 0.022, "MadeiraVelha", "root", seg=6)
        p.box((side * 0.12, -1.0, zc - 0.13), (0.12, 0.35, 0.02), "MadeiraVelha", "root")
    return p.to_static(col, bevel=0.006, bevel_segments=1)


def build_lago(col):
    """LAGO escuro (~18 × 13 m) com margem de lama, pedras na borda, juncos e vitórias-régias.
    É um limite natural do terreno. A água fica 3 cm acima do chão (o chão do Unity é Y 0);
    a margem é uma faixa de lama. No Unity: colisão de caixa baixa na água (ninguém anda nela).
    Origem no meio."""
    rng = rng_for("Lago")
    p = bc.Part("SM_ENV_Lago", MATS)
    seg = 40
    rx, ry = 9.0, 6.5
    radius = [1 + 0.12 * math.sin(3 * 2 * math.pi * i / seg + 1) + 0.07 * math.sin(7 * 2 * math.pi * i / seg) for i in range(seg)]

    def outline(scale, z):
        return [p.bm.verts.new((math.cos(2 * math.pi * i / seg) * rx * radius[i] * scale * bc.K,
                                math.sin(2 * math.pi * i / seg) * ry * radius[i] * scale * bc.K, z * bc.K)) for i in range(seg)]

    def disc(scale, z, mat):
        vs = outline(scale, z)
        f = p.bm.faces.new(vs)
        f.material_index = p._index(p.mats, mat)
        gi = p._index(p.groups, "root")
        for v in vs:
            v[p.deform][gi] = 1.0
        return f

    def band(s0, s1, z0, z1, mat):
        a, b = outline(s0, z0), outline(s1, z1)
        mi = p._index(p.mats, mat)
        gi = p._index(p.groups, "root")
        for i in range(seg):
            f = p.bm.faces.new((a[i], a[(i + 1) % seg], b[(i + 1) % seg], b[i]))
            f.material_index = mi
        for v in a + b:
            v[p.deform][gi] = 1.0

    band(1.18, 1.0, 0.005, 0.02, "Terra")                                  # margem de lama
    disc(1.0, 0.03, "Agua")
    for i in range(seg):                                                   # pedras da borda
        if rng.random() < 0.45:
            a = 2 * math.pi * (i + rng.uniform(0, 1)) / seg
            rr = radius[i] * rng.uniform(1.0, 1.1)
            s = rng.uniform(0.18, 0.45)
            f = p.ellipsoid((math.cos(a) * rx * rr, math.sin(a) * ry * rr, s * 0.15), (s, s * 0.8, s * 0.5),
                            rng.choice(("PedraMuro", "PedraEscura", "PedraMusgo")), "root", seg=8, rings=5)
            p.wrinkle(f, s * 0.3, scale=s * 0.6, seed=i)
    for _ in range(14):                                                    # juncos
        a = rng.uniform(0, 2 * math.pi)
        rr = rng.uniform(0.9, 1.05)
        cx, cy = math.cos(a) * rx * rr, math.sin(a) * ry * rr
        for k in range(6):
            dx, dy = rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25)
            h = rng.uniform(0.6, 1.3)
            p.tube((cx + dx, cy + dy, 0.0), (cx + dx * 1.4, cy + dy * 1.4, h), 0.012, 0.004,
                   "FolhagemSeca" if k % 3 == 0 else "Folhagem", "root", seg=4, smooth=False)
    for _ in range(16):                                                    # vitórias-régias
        a = rng.uniform(0, 2 * math.pi)
        rr = rng.uniform(0.3, 0.85)
        cx, cy = math.cos(a) * rx * rr, math.sin(a) * ry * rr
        s = rng.uniform(0.18, 0.35)
        p.tube((cx, cy, 0.03), (cx, cy, 0.045), s, s, "Folhagem", "root", seg=10, smooth=False)
    return p.to_static(col, bevel=0)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
# ---------------------------------------------------------------- a fachada da casa

# O kit da casa (build_env_kit_blockout.py) só tem papel de parede: de fora, a mansão era um bloco
# vinho. A fachada é uma "casca" de 6 cm colada no lado de fora de cada módulo de parede que dá para
# a rua ou para o morro (quem decide é o MirrorHouseBuilder.Exterior.cs, pela planta). Os módulos
# têm o tamanho do kit (2 × 3,2 m) e esticam igual a ele. A face olha para -Y (= +Z no Unity, para
# fora); a origem fica no meio da base, na face de TRÁS (a que encosta na parede).
FAC_W, FAC_H, FAC_T = 2.0, 3.2, 0.06
FAC_DOOR = {"Porta": 1.2, "Dupla": 1.6}
DOOR_TOP = 2.4


def plaster(p, rng, x0, x1, z0, z1):
    """Um pano de reboco de x0..x1 / z0..z1, com manchas de umidade escorrendo e lascas caídas
    (onde aparece o tijolo). É o que tira a cara de caixa lisa da parede."""
    if x1 - x0 < 0.02 or z1 - z0 < 0.02:
        return
    p.box(((x0 + x1) / 2, -FAC_T / 2, (z0 + z1) / 2), (x1 - x0, FAC_T, z1 - z0), "RebocoFachada", "root")
    for _ in range(int((x1 - x0) * 1.5) + 1):                               # umidade escorrendo de cima
        w = min(rng.uniform(0.15, 0.5), x1 - x0)
        x = rng.uniform(x0 + w / 2, x1 - w / 2)
        length = rng.uniform(0.4, (z1 - z0) * 0.7)
        p.box((x, -FAC_T - 0.002, z1 - length / 2), (w, 0.004, length), "RebocoManchado", "root")
    if rng.random() < 0.6 and x1 - x0 > 0.7 and z1 - z0 > 1.0:              # reboco caído: o tijolo aparece
        w, h = rng.uniform(0.25, 0.5), rng.uniform(0.15, 0.35)
        x = rng.uniform(x0 + w / 2 + 0.05, x1 - w / 2 - 0.05)
        z = rng.uniform(z0 + h / 2 + 0.3, z1 - h / 2 - 0.3)
        for k in range(3):
            p.box((x + rng.uniform(-0.05, 0.05), -FAC_T - 0.004, z - h / 2 + (k + 0.5) * h / 3),
                  (w * rng.uniform(0.6, 1.0), 0.012, h / 3 - 0.02), "PedraEscura", "root")


def build_fachada(kind, plinth):
    """Módulo de fachada: "Lisa", "Janela", "Porta" ou "Dupla". plinth = com o soco de pedra
    (0,55 m) embaixo: a fileira que nasce no chão. A fileira de cima dos cômodos de pé-direito
    duplo não tem soco."""
    name = f"Fachada{kind}{'' if plinth else 'Alta'}"

    def build(col):
        rng = rng_for(name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        base = 0.55 if plinth else 0.0
        half = FAC_DOOR.get(kind, 0) / 2
        if plinth:                                                           # soco de cantaria (fora do vão)
            x_cuts = [(-FAC_W / 2, -half - 0.12), (half + 0.12, FAC_W / 2)] if half else [(-FAC_W / 2, FAC_W / 2)]
            for a, b in x_cuts:
                p.box(((a + b) / 2, -0.02, base / 2), (b - a, 0.04, base), "PedraRejunte", "root")
                stone_face(p, rng, a, b, 0.0, base, -0.04, -1, rows=2, depth=(0.04, 0.07), moss=0.25)
                p.box(((a + b) / 2, -0.1, base + 0.03), (b - a, 0.08, 0.06), "PedraClara", "root")  # pingadeira
        if half:
            plaster(p, rng, -FAC_W / 2, -half - 0.12, base + 0.06, FAC_H)
            plaster(p, rng, half + 0.12, FAC_W / 2, base + 0.06, FAC_H)
            plaster(p, rng, -half - 0.12, half + 0.12, DOOR_TOP + 0.3, FAC_H)
            # moldura de cantaria da porta: ombreiras de blocos alternados e a verga com a pedra-chave
            for side in (-1, 1):
                z = 0.0
                k = 0
                while z < DOOR_TOP:
                    h = min(0.4, DOOR_TOP - z)
                    w = 0.24 if k % 2 else 0.18
                    p.box((side * (half + w / 2 - 0.02), -0.06, z + h / 2), (w, 0.12, h - 0.02),
                          rng.choice(("PedraClara", "PedraMuro")), "root")
                    z += h
                    k += 1
            p.box((0, -0.07, DOOR_TOP + 0.15), (2 * half + 0.4, 0.14, 0.3), "PedraClara", "root")
            p.box((0, -0.11, DOOR_TOP + 0.17), (0.22, 0.1, 0.36), "PedraMuro", "root")  # pedra-chave
        else:
            plaster(p, rng, -FAC_W / 2, FAC_W / 2, base + 0.06, FAC_H)
            # friso horizontal na altura do peitoril (dá ritmo na parede comprida)
            p.box((0, -FAC_T - 0.02, 1.0), (FAC_W, 0.05, 0.08), "RebocoFriso", "root")
        if kind == "Janela":
            # janela de guilhotina (0,9 × 1,5 m) escura, com caixilho, peitoril e verga de pedra;
            # tábuas pregadas por cima (a casa está fechada há anos)
            wx, z0, z1 = 0.45, 1.05, 2.55
            p.box((0, -FAC_T - 0.005, (z0 + z1) / 2), (2 * wx, 0.01, z1 - z0), "VidroJanela", "root")
            for x in (-wx, 0.0, wx):
                p.box((x, -FAC_T - 0.03, (z0 + z1) / 2), (0.05, 0.05, z1 - z0), "MadeiraPintada", "root")
            for z in (z0, (z0 + z1) / 2, z1):
                p.box((0, -FAC_T - 0.03, z), (2 * wx + 0.05, 0.05, 0.05), "MadeiraPintada", "root")
            for side in (-1, 1):                                               # ombreiras de pedra
                p.box((side * (wx + 0.08), -FAC_T - 0.03, (z0 + z1) / 2), (0.14, 0.06, z1 - z0 + 0.1), "PedraClara", "root")
            p.box((0, -FAC_T - 0.07, z0 - 0.05), (2 * wx + 0.4, 0.14, 0.08), "PedraClara", "root")  # peitoril
            p.box((0, -FAC_T - 0.04, z1 + 0.1), (2 * wx + 0.36, 0.08, 0.2), "PedraClara", "root")   # verga
            for k in range(rng.randint(2, 3)):                                  # tábuas pregadas
                z = rng.uniform(z0 + 0.2, z1 - 0.2)
                p.box((rng.uniform(-0.05, 0.05), -FAC_T - 0.08, z), (2 * wx + 0.3, 0.025, 0.16),
                      rng.choice(("MadeiraVelha", "MadeiraVelhaClara")), "root", rot_y=rng.uniform(-14, 14))
        p.wrinkle([f for f in p.bm.faces], 0.004, scale=0.3, seed=len(name))
        return p.to_static(col, bevel_segments=1)

    return build


def build_cunhal(col):
    """CUNHAL: a quina de pedra da casa (0,42 × 0,42 × 3,2 m), blocos alternados longos e curtos
    nas duas faces. Cobre a fresta entre as fachadas de duas paredes que se encontram na quina.
    Origem no meio da base (no eixo da quina)."""
    rng = rng_for("Cunhal")
    p = bc.Part("SM_ENV_Cunhal", MATS)
    w = 0.42
    p.box((0, 0, FAC_H / 2), (w - 0.06, w - 0.06, FAC_H), "PedraRejunte", "root")
    z, k = 0.0, 0
    while z < FAC_H - 0.01:
        h = min(0.32, FAC_H - z)
        long_x = k % 2 == 0
        sx, sy = (w + 0.14, w) if long_x else (w, w + 0.14)
        dx, dy = (0.07, 0) if long_x else (0, 0.07)
        p.box((dx * rng.choice((-1, 1)), dy * rng.choice((-1, 1)), z + h / 2), (sx, sy, h - 0.025),
              rng.choice(("PedraClara", "PedraMuro", "PedraMuro", "PedraMusgo")), "root", rot_y=rng.uniform(-1.5, 1.5))
        z += h
        k += 1
    return p.to_static(col, bevel_segments=1)


def build_cornija(col):
    """CORNIJA de 2 m: o beiral de pedra no alto da fachada (três degraus que avançam 0,3 m, com
    dentículos embaixo e uma platibanda baixa em cima). Esconde a quina da laje do forro.
    Origem no meio da base, encostada na parede; avança para -Y."""
    rng = rng_for("Cornija")
    p = bc.Part("SM_ENV_Cornija", MATS)
    p.box((0, -0.05, 0.08), (FAC_W, 0.1, 0.16), "PedraMuro", "root")
    p.box((0, -0.12, 0.24), (FAC_W, 0.24, 0.16), "PedraClara", "root")
    p.box((0, -0.18, 0.38), (FAC_W, 0.36, 0.12), "PedraMuro", "root")
    x = -FAC_W / 2 + 0.06
    while x < FAC_W / 2:                                                     # dentículos
        p.box((x, -0.16, 0.12), (0.07, 0.08, 0.08), "PedraClara", "root")
        x += 0.16
    p.box((0, -0.08, 0.6), (FAC_W, 0.16, 0.32), "RebocoFachada", "root")   # platibanda
    p.box((0, -0.09, 0.78), (FAC_W, 0.2, 0.06), "PedraMusgo" if rng.random() < 0.5 else "PedraClara", "root")
    return p.to_static(col, bevel_segments=1)


# ---------------------------------------------------------------- hera, musgo e mato

def build_hera(name, w, h):
    """HERA subindo a parede (w × h m): ramos que nascem no pé e sobem em leque, ondulando, e
    folhas em ponta de pirâmide (as facetas pegam a luz da lanterna). Mais folhas embaixo, onde é
    mais velha. Plana (6 cm), olhando para -Y (= +Z no Unity); origem no meio da base, encostada
    na parede. Vai por cima da fachada e do muro de pedra."""
    def build(col):
        rng = rng_for(name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        for s in range(rng.randint(4, 6)):
            x = rng.uniform(-w * 0.35, w * 0.35)
            top = h * rng.uniform(0.55, 1.0)
            lean = rng.uniform(-0.35, 0.35) * w
            pts = []
            steps = 8
            for i in range(steps + 1):
                t = i / steps
                pts.append((x + lean * t + math.sin(t * 6 + s) * 0.08, -0.015, top * t))
            for a, b in zip(pts, pts[1:]):                                     # o ramo (sem juntas: é fino)
                p.tube(a, b, 0.012, 0.01, "MadeiraCasca", "root", seg=3, smooth=False, caps=False)
            n = int(top * 26)
            for i in range(n):
                t = (i / n) ** 0.8                                             # mais folhas embaixo
                z = top * t
                bx = x + lean * t + math.sin(t * 6 + s) * 0.08
                spread = 0.1 + 0.2 * (1 - t)
                lx, lz = bx + rng.uniform(-spread, spread), z + rng.uniform(-0.05, 0.05)
                size = rng.uniform(0.05, 0.085)
                mat = "FolhagemHera" if rng.random() < 0.6 else "FolhagemHeraEscura"
                p.tube((lx, -0.01, lz), (lx + rng.uniform(-0.01, 0.01), -0.04 - rng.uniform(0, 0.02), lz + rng.uniform(-0.01, 0.01)),
                       size, 0.004, mat, "root", seg=4, smooth=False)
        return p.to_static(col, bevel=0)
    return build


def build_musgo(col):
    """MUSGO em placa (≈1,2 × 0,8 m): almofadas baixas amassadas, coladas na pedra. Para o pé do
    muro de pedra, do arrimo e do soco da casa. Olha para -Y; origem no meio da base."""
    rng = rng_for("MusgoParede")
    p = bc.Part("SM_ENV_MusgoParede", MATS)
    for i in range(7):
        x, z = rng.uniform(-0.45, 0.45), rng.uniform(0.05, 0.6) * (1 - abs(rng.uniform(-0.45, 0.45)))
        s = rng.uniform(0.15, 0.3)
        f = p.ellipsoid((x, -0.005, z + s * 0.4), (s, 0.035, s * 0.65), "FolhagemMusgo" if i % 3 else "PedraMusgo",
                        "root", seg=10, rings=6)
        p.wrinkle(f, 0.02, scale=0.05, seed=i * 1.7)
    return p.to_static(col, bevel=0)


def build_tufo(name, blades, height, mats, seeds=False):
    """TUFO de grama ou de MATO: lâminas finas que saem de um pé (Ø ~0,35 m), cada uma curvada
    para fora; o mato alto tem hastes com o pendão de sementes. Quebra o chão liso perto do muro,
    da casa e do lago. Origem no meio da base."""
    def build(col):
        rng = rng_for(name)
        p = bc.Part(f"SM_ENV_{name}", MATS)
        for b in range(blades):
            a = rng.uniform(0, 2 * math.pi)
            r0 = rng.uniform(0, 0.12)
            hh = height * rng.uniform(0.5, 1.0)
            out = hh * rng.uniform(0.15, 0.5)
            base = (math.cos(a) * r0, math.sin(a) * r0, -0.02)
            mid = (math.cos(a) * (r0 + out * 0.35), math.sin(a) * (r0 + out * 0.35), hh * 0.6)
            tip = (math.cos(a) * (r0 + out), math.sin(a) * (r0 + out), hh * rng.uniform(0.8, 1.0))
            mat = rng.choice(mats)
            p.tube(base, mid, 0.016, 0.01, mat, "root", seg=3, smooth=False, caps=False)
            p.tube(mid, tip, 0.01, 0.001, mat, "root", seg=3, smooth=False, caps=False)
            if seeds and b % 3 == 0:                                            # o pendão
                p.ellipsoid(tip, (0.025, 0.025, 0.07), "GramaSeca", "root", seg=5, rings=3)
        return p.to_static(col, bevel=0)
    return build


GREENERY = [
    ("HeraA", build_hera("HeraA", 1.6, 2.8), 2.8, 3.0, 0.0),
    ("HeraB", build_hera("HeraB", 2.2, 1.6), 1.6, 2.4, 0.0),
    ("MusgoParede", build_musgo, 0.7, 1.4, 0.0),
    ("TufoGrama", build_tufo("TufoGrama", 34, 0.45, ("GramaTufo", "GramaTufo", "Grama", "GramaSeca")), 0.5, 0.9, 0.0),
    ("MatoAlto", build_tufo("MatoAlto", 22, 1.1, ("GramaSeca", "GramaTufo", "FolhagemSeca"), seeds=True), 1.1, 1.4, 0.0),
]


FACADE = [(f"Fachada{k}{'' if plinth else 'Alta'}", build_fachada(k, plinth), 3.3, 3.4, 0.0)
          for k, plinth in (("Lisa", True), ("Janela", True), ("Porta", True), ("Dupla", True),
                            ("Lisa", False), ("Janela", False))]


# ---------------------------------------------------------------- LOD das árvores

def with_lod(build, ratio):
    """Junta à peça uma versão SIMPLIFICADA (LOD1, Decimate) no mesmo FBX. Os nomes _LOD0 / _LOD1
    fazem o Unity criar o LODGroup sozinho; o MirrorHouseBuilder.Exterior.cs acerta as distâncias.
    A floresta tem ~600 árvores: de longe, ninguém vê os galhos (e a névoa come o resto)."""
    def wrapped(col):
        obj = build(col)
        low = obj.copy()
        low.data = obj.data.copy()
        col.objects.link(low)
        mod = low.modifiers.new("LOD", "DECIMATE")
        mod.ratio = ratio
        bpy.context.view_layer.objects.active = low
        bpy.ops.object.modifier_apply(modifier=mod.name)
        name = obj.name
        obj.name = f"{name}_LOD0"
        low.name = f"{name}_LOD1"
        low.hide_render = True                                               # as prévias mostram o LOD0
        bc.report_tris((low,))
        return obj
    return wrapped


PIECES = [
    ("MuroPedra", build_muro_pedra, 1.7, 4.4, 0.0),
    ("MuroArrimo", build_muro_arrimo, 3.6, 4.4, 0.0),
    ("MuroRuina", build_muro_ruina, 1.6, 4.6, 0.0),
    ("PilarPortao", build_pilar_portao, 3.1, 3.2, 0.0),
    ("PilarMuro", build_pilar_muro, 2.5, 2.6, 0.0),
    ("PortaoFerro", build_portao_ferro, 2.8, 4.8, 0.0),
    ("GradeFerro", build_grade_ferro, 2.3, 4.4, 0.0),
    ("LajeCaminho", build_laje_caminho, 0.1, 2.4, 0.0),
    ("Canteiro", build_canteiro, 0.6, 3.2, 0.0),
    ("FolhasChao", build_folhas_chao, 0.05, 2.8, 0.0),
    ("Poste", build_poste, 3.1, 3.2, -0.25),
    ("Fonte", build_fonte, 3.1, 4.8, 0.0),
    ("PinheiroA", with_lod(build_pinheiro("PinheiroA", 12.0, "PinheiroA"), 0.3), 12.0, 12.4, 0.0),
    ("PinheiroB", with_lod(build_pinheiro("PinheiroB", 9.0, "PinheiroB"), 0.3), 9.0, 9.4, 0.0),
    ("PinheiroC", with_lod(build_pinheiro("PinheiroC", 6.0, "PinheiroC"), 0.3), 6.0, 6.4, 0.0),
    ("FolhosaA", with_lod(build_folhosa("FolhosaA", 9.0, "FolhosaA"), 0.2), 9.5, 9.8, 0.0),
    ("FolhosaB", with_lod(build_folhosa("FolhosaB", 7.0, "FolhosaB"), 0.2), 7.5, 7.8, 0.0),
    ("ArvoreRetorcida", build_arvore_retorcida, 9.0, 10.0, 0.0),
    ("ArbustoA", build_arbusto("ArbustoA", 1.2, "ArbustoA"), 1.3, 2.0, 0.0),
    ("ArbustoB", build_arbusto("ArbustoB", 0.8, "ArbustoB", ("FolhagemEscura", "FolhagemSeca")), 0.9, 1.4, 0.0),
    ("PedraA", build_pedra("PedraA", 0.7, "PedraA"), 0.5, 1.0, 0.0),
    ("PedraB", build_pedra("PedraB", 1.8, "PedraB"), 1.2, 2.4, 0.0),
    ("Galpao", build_galpao, 3.6, 5.2, 0.0),
    ("Doca", build_doca, 0.6, 7.6, -3.5),
    ("Barco", build_barco, 0.6, 3.2, 0.0),
    ("Lago", build_lago, 1.3, 21.0, 0.0),
    *FACADE,
    *GREENERY,
    ("Cunhal", build_cunhal, 3.2, 3.4, 0.0),
    ("Cornija", build_cornija, 0.9, 2.4, 0.0),
]


def main():
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else None
    for name, build, height, frame, center_y in PIECES:
        if only and name not in only:
            continue
        scene, cols = bc.new_scene(name, kind="Environment")
        obj = build(cols["Geo"])
        bc.report_tris((obj,))
        bc.finish(scene, cols, name, height, kind="Environment", frame=frame, center_y=center_y)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
