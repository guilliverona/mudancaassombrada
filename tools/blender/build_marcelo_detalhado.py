"""Marcelo "Marreta" DETALHADO: o modelo que vai para o jogo (SK_CHR_Marcelo.fbx).

Usa o mesmo rig, as mesmas medidas e as mesmas mãos do blockout (build_marcelo_blockout.py),
mas troca as formas "de bloco" por peças com volume: macacão com dobras, bolsos e zíper,
joelheiras acolchoadas com tira, botas de bico de aço com sola e cadarço, cinto com passantes
e fivela vazada, alças do quadro de fita chata com regulador, barba em tufos, gorro canelado,
remendo da transportadora nas costas, tatuagem de âncora (ex-estivador) e o quadro branco com
moldura, bandeja, marcador, desenho de fantasma e um bilhete do Seu Valdemar preso com fita.

Rodar DEPOIS do build_marcelo_blockout.py (o build_all.ps1 já faz nessa ordem): o blockout
também grava o SK_CHR_Marcelo.fbx, e este script grava por cima. O .blend do blockout continua
servindo de base (rig e medidas) e para a comparação lado a lado.
    blender --background --factory-startup --python tools/blender/build_marcelo_detalhado.py

Saídas: MudancaAssombrada/Assets/Art/Characters/Marcelo/SK_CHR_Marcelo.fbx,
Art/Source/Characters/Marcelo/SK_CHR_Marcelo_Detalhado.blend e
Art/Source/Characters/Marcelo/previews/detalhado/ (corpo inteiro, closes e lado a lado).
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import blockout_common as bc  # noqa: E402
import build_marcelo_blockout as mb  # noqa: E402  (importar já acerta a escala da ficha)

ARMS, HAND, GLOVE = mb.ARMS, mb.HAND, mb.GLOVE

# Mesmas cores do blockout + as peças novas.
MATS = bc.Materials("MAT_CHR_Marcelo", {
    **mb.MATS.table,
    "Sola": ("#3A2F28", 0.0, 0.9, 0.0),
    "Aco": ("#8E9399", 1.0, 0.35, 0.0),
    "Cadarco": ("#B98E52", 0.0, 0.9, 0.0),
    "Couro": ("#6B4A30", 0.0, 0.7, 0.0),
    "Papel": ("#EDE3C8", 0.0, 0.9, 0.0),
    "Tinta": ("#2C3E66", 0.0, 0.6, 0.0),
    "Ziper": ("#6E6A64", 0.8, 0.4, 0.0),
    "Trena": ("#C23B2E", 0.0, 0.5, 0.0),
    "Flanela": ("#B0352E", 0.0, 0.95, 0.0),
    "Feltro": ("#8B8B86", 0.0, 1.0, 0.0),
    "Tatuagem": ("#3B4E63", 0.0, 0.6, 0.0),
    "MarcadorCorpo": ("#ECE8DE", 0.0, 0.5, 0.0),
    "Caixa": ("#B58A55", 0.0, 0.85, 0.0),
})

OUT = bc.SOURCE_ROOT / "Characters" / "Marcelo"


# ---------------------------------------------------------------- ajudantes de forma

def lin(a, b, n):
    return [a + (b - a) * i / (n - 1) for i in range(n)]


def chain(p, pts, radii, mat, bones, seg=16, steps=4, squash_y=1.0):
    """Tubo contínuo com vários anéis por trecho (pano). Os anéis coincidem nas emendas e o
    wrinkle solda tudo; assim a dobra corre pela manga inteira, sem degrau."""
    faces = set()
    for i in range(len(pts) - 1):
        a, b = Vector(pts[i]), Vector(pts[i + 1])
        for k in range(steps):
            t0, t1 = k / steps, (k + 1) / steps
            r0 = radii[i] + (radii[i + 1] - radii[i]) * t0
            r1 = radii[i] + (radii[i + 1] - radii[i]) * t1
            faces |= p.tube(a.lerp(b, t0), a.lerp(b, t1), r0, r1, mat, bones[i],
                            seg=seg, caps=False, squash_y=squash_y)
    return faces


def line(p, pts, radius, mat, bone, seg=4):
    """Traço fino (desenho, costura, tatuagem): só tubos, sem as esferas das juntas do
    polyline, que custavam mais triângulos que o traço. Cada tubo passa um pouco da ponta
    para a quina não abrir."""
    for a, b in zip(pts, pts[1:]):
        a, b = Vector(a), Vector(b)
        d = (b - a).normalized() * radius
        p.tube(a - d, b + d, radius, radius, mat, bone, seg=seg, caps=False)


def weld(p, faces):
    """Solda os anéis repetidos de um chain sem enrugar (pele)."""
    p.wrinkle(faces, 0.0)


def tris(bm):
    return sum(len(f.verts) - 2 for f in bm.faces)


def _basis(axes, sizes, k):
    cols = [Vector(a).normalized() * s * k for a, s in zip(axes, sizes)]
    return Matrix(cols).transposed().to_4x4()


def obox(p, center, axes, size, mat, bone, real=False):
    """Caixa orientada: axes = direções dos três lados de `size`. real=True: coordenadas já
    em metros finais (vindas do hand_frame), sem a escala da ficha."""
    k = 1.0 if real else bc.K
    m = _basis(axes, size, k)
    m.translation = Vector(center) * k
    before = set(p.bm.faces)
    vs = bmesh.ops.create_cube(p.bm, size=1.0, matrix=m)["verts"]
    faces = p._finish(vs, mat, bone, False)
    bmesh.ops.recalc_face_normals(p.bm, faces=[f for f in p.bm.faces if f not in before])
    return faces


def oellipsoid(p, center, axes, radii, mat, bone, seg=12, rings=6, real=False):
    k = 1.0 if real else bc.K
    m = _basis(axes, radii, k)
    m.translation = Vector(center) * k
    before = set(p.bm.faces)
    vs = bmesh.ops.create_uvsphere(p.bm, u_segments=seg, v_segments=rings, radius=1.0, matrix=m)["verts"]
    faces = p._finish(vs, mat, bone, True)
    bmesh.ops.recalc_face_normals(p.bm, faces=[f for f in p.bm.faces if f not in before])
    return faces


def slab(p, surf, us, vs, offset, thick, mat, bone):
    """Placa com espessura que acompanha uma superfície curva (bolso, remendo, joelheira).
    surf(u, v) -> (ponto, normal) antes da escala. Fecha frente, verso e as quatro bordas,
    para não ficar um "plano" colado no corpo."""
    before = set(p.bm.faces)
    outer, inner = [], []
    for v in vs:
        ro, ri = [], []
        for u in us:
            pt, n = surf(u, v)
            n = Vector(n).normalized()
            ro.append(p.bm.verts.new((Vector(pt) + n * (offset + thick)) * bc.K))
            ri.append(p.bm.verts.new((Vector(pt) + n * offset) * bc.K))
        outer.append(ro)
        inner.append(ri)
    nu, nv = len(us), len(vs)
    new = p.bm.faces.new
    for j in range(nv - 1):
        for i in range(nu - 1):
            new((outer[j][i], outer[j][i + 1], outer[j + 1][i + 1], outer[j + 1][i]))
            new((inner[j][i], inner[j + 1][i], inner[j + 1][i + 1], inner[j][i + 1]))
    edge = ([(0, i) for i in range(nu)] + [(j, nu - 1) for j in range(1, nv)]
            + [(nv - 1, i) for i in range(nu - 2, -1, -1)] + [(j, 0) for j in range(nv - 2, 0, -1)])
    for (j0, i0), (j1, i1) in zip(edge, edge[1:] + edge[:1]):
        new((outer[j0][i0], inner[j0][i0], inner[j1][i1], outer[j1][i1]))
    verts = [v for row in outer + inner for v in row]
    p._finish(verts, mat, bone, True)
    bmesh.ops.recalc_face_normals(p.bm, faces=[f for f in p.bm.faces if f not in before])


# superfícies do corpo (as mesmas medidas do blockout)

Z0, Z1 = mb.d(0, 394)[1], mb.d(0, 172)[1]


def torso_r(z):
    return 0.26 + (0.313 - 0.26) * (z - Z0) / (Z1 - Z0)


def torso_back(x, z):
    """Costas do macacão em (x, z): ponto e normal (elipse achatada em Y, 0.65)."""
    r = torso_r(z)
    a = math.asin(max(-0.98, min(0.98, x / r)))
    return (r * math.sin(a), 0.65 * r * math.cos(a), z), (math.sin(a) / r, math.cos(a) / (0.65 * r), 0)


def torso_front(x, z):
    (px, py, pz), (nx, ny, nz) = torso_back(x, z)
    return (px, -py, pz), (nx, -ny, nz)


def shoulder_top(x, y):
    """Altura do alto do ombro (o elipsoide que fecha o tronco) em (x, y)."""
    q = 1 - (x / 0.31) ** 2 - (y / 0.2) ** 2
    return Z1 - 0.03 + 0.08 * math.sqrt(max(q, 0.0))


def puck(p, center, radii, zmin, zmax, mat, bone, seg=20, rings=10):
    """Elipsoide achatado em cima e embaixo: sola que segue o contorno do pé, com a borda
    redonda (e não uma tábua)."""
    faces = p.ellipsoid(center, radii, mat, bone, seg=seg, rings=rings)
    for v in {v for f in faces for v in f.verts}:
        v.co.z = min(max(v.co.z, zmin * bc.K), zmax * bc.K)
    return faces


def leg_r(z):
    return 0.08 + (0.088 - 0.08) * (z - 0.4) / (0.713 - 0.4) if z > 0.4 else 0.076 + 0.004 * (z - 0.09) / 0.31


def leg_surf(x0):
    """Perna como cilindro: u = ângulo a partir da frente (-Y) girando para +X."""
    def f(a, z):
        r = leg_r(z)
        return (x0 + r * math.sin(a), -r * math.cos(a), z), (math.sin(a), -math.cos(a), 0)
    return f


def belly_front(z):
    """Frente da barriga/macacão em z (o que estiver mais para fora)."""
    t = (z - 0.92) / 0.24
    belly = -0.06 - 0.18 * math.sqrt(max(0.0, 1 - t * t))
    return min(belly, -0.65 * torso_r(z))


# ---------------------------------------------------------------- corpo

def build_boots(p):
    for s, side in ((1, "Left"), (-1, "Right")):
        x, foot = 0.103 * s, f"{side}Foot"
        # sola grossa de borracha no contorno do pé, com o salto mais alto e o vão do meio
        puck(p, (x, -0.045, 0.018), (0.092, 0.155, 0.03), 0.0, 0.034, "Sola", foot, seg=16, rings=6)
        puck(p, (x, 0.05, 0.02), (0.085, 0.06, 0.03), -0.004, 0.04, "Sola", foot, seg=12, rings=5)
        # vira de couro marrom costurada em volta, logo acima da sola
        puck(p, (x, -0.045, 0.036), (0.095, 0.159, 0.02), 0.03, 0.042, "Couro", foot, seg=16, rings=4)
        # cano e gáspea de couro
        p.ellipsoid((x, -0.035, 0.065), (0.084, 0.138, 0.05), "Bota", foot, seg=16, rings=8)
        p.tube((x, 0.0, 0.05), (x, 0.0, 0.12), 0.084, 0.082, "Bota", foot, seg=16, caps=False)
        # bico de aço: capa de couro por cima e o aço aparecendo onde o couro gastou
        p.ellipsoid((x, -0.135, 0.052), (0.08, 0.062, 0.041), "Bota", foot, seg=12, rings=6)
        p.ellipsoid((x + 0.015 * s, -0.168, 0.083), (0.026, 0.02, 0.006), "Aco", foot, seg=10, rings=4, rot=(-30, 0, 0))
        # costura entre a capa do bico e a gáspea
        line(p, [(x - 0.07, -0.11, 0.058), (x - 0.04, -0.094, 0.088), (x, -0.089, 0.096),
                 (x + 0.04, -0.094, 0.088), (x + 0.07, -0.11, 0.058)], 0.0028, "Cadarco", foot)
        # cadarço cruzado e laço
        holes = [(-0.025, -0.12, 0.104), (0.025, -0.12, 0.104), (-0.025, -0.095, 0.109), (0.025, -0.095, 0.109)]
        lace = lambda a, b: line(p, [(x + a[0], a[1], a[2] + 0.003), (x + b[0], b[1], b[2] + 0.003)],
                                 0.003, "Cadarco", foot)
        lace(holes[0], holes[1])
        lace(holes[0], holes[3])
        lace(holes[1], holes[2])
        for k in (-1, 1):
            p.ellipsoid((x + 0.014 * k, -0.09, 0.116), (0.014, 0.006, 0.008), "Cadarco", foot,
                        seg=6, rings=3, rot=(0, 25 * k, 0))
            line(p, [(x + 0.003 * k, -0.093, 0.114), (x + 0.012 * k, -0.102, 0.092),
                     (x + 0.016 * k, -0.107, 0.072)], 0.0025, "Cadarco", foot)


def build_legs(p):
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.103 * s
        up, low = f"{side}UpperLeg", f"{side}LowerLeg"
        legs = chain(p, [(x, 0, 0.713), (x, 0, 0.4), (x, 0, 0.12)], (0.088, 0.08, 0.077),
                     "Macacao", [up, low], seg=16, steps=4)
        p.wrinkle(legs, 0.007, scale=0.07, seed=3.1 * s)
        # barra virada, folgada por cima da bota
        roll = chain(p, [(x, 0, 0.082), (x, 0, 0.115), (x, 0, 0.15)], (0.089, 0.094, 0.088),
                     "MacacaoClaro", [low, low], seg=16, steps=2)
        p.wrinkle(roll, 0.004, scale=0.05, seed=1.3 * s)
        # faixa refletiva da canela
        p.tube((x, 0, 0.383), (x, 0, 0.417), 0.0855, 0.0855, "Faixa", low, seg=16, caps=False)

        surf = leg_surf(x)
        # joelheira acolchoada, com as tiras por trás e o gomo do meio
        slab(p, surf, lin(-0.85, 0.85, 7), lin(0.435, 0.57, 4), 0.006, 0.02, "MacacaoClaro", up)
        slab(p, surf, lin(-0.5, 0.5, 4), lin(0.47, 0.535, 3), 0.026, 0.006, "Macacao", up)
        for z in (0.448, 0.556):
            p.tube((x, 0, z - 0.009), (x, 0, z + 0.009), leg_r(z) + 0.006, leg_r(z) + 0.006,
                   "Cinto", up, seg=16, caps=False)
        # bolso de carga do lado de fora da coxa: bolso, aba e botão
        out = math.pi / 2 * s
        slab(p, surf, lin(out - 0.55, out + 0.55, 5), lin(0.585, 0.665, 3), 0.007, 0.014, "MacacaoClaro", up)
        slab(p, surf, lin(out - 0.6, out + 0.6, 5), lin(0.655, 0.69, 2), 0.02, 0.008, "Macacao", up)
        bpt, bn = surf(out, 0.668)
        p.ellipsoid(Vector(bpt) + Vector(bn) * 0.03, (0.008, 0.008, 0.008), "Aco", up, seg=8, rings=4)


def build_torso(p):
    # tronco em anéis (para dobrar), fundo do macacão, barriga e ombros
    body = chain(p, [(0, 0, Z0), (0, 0, Z1)], (0.26, 0.313), "Macacao", ["Chest"], seg=28, steps=8, squash_y=0.65)
    p.wrinkle(body, 0.006, scale=0.09, seed=7.0, stretch_z=1.6)
    p.ellipsoid((0, 0, 0.69), (0.255, 0.165, 0.07), "Macacao", "Hips", seg=16, rings=5)
    p.ellipsoid((0, 0, Z1 - 0.03), (0.31, 0.2, 0.08), "Macacao", "UpperChest", seg=20, rings=5)
    belly = p.ellipsoid((0, -0.06, 0.92), (0.25, 0.18, 0.24), "Macacao", "belly_jiggle", seg=18, rings=8)
    p.wrinkle(belly, 0.005, scale=0.08, seed=2.0)

    # gola do macacão com as duas pontas e a camiseta aparecendo
    p.tube((0, -0.005, 1.405), (0, -0.008, 1.465), 0.122, 0.11, "Macacao", "UpperChest", seg=16, caps=False)
    for s in (1, -1):
        p.ellipsoid((0.062 * s, -0.128, 1.425), (0.05, 0.012, 0.034), "MacacaoClaro", "UpperChest",
                    seg=10, rings=4, rot=(-30, -25 * s, 18 * s))
    p.ellipsoid((0, -0.125, 1.4), (0.05, 0.02, 0.045), "Camiseta", "UpperChest", seg=10, rings=4)

    # zíper descendo pela barriga até o cinto
    zip_pts = [(0, belly_front(z) - 0.003, z) for z in lin(0.755, 1.0, 6)]
    line(p, zip_pts, 0.0055, "Ziper", "belly_jiggle", seg=6)
    for s in (1, -1):
        line(p, [(0.012 * s, y + 0.001, z) for _, y, z in zip_pts], 0.004, "MacacaoClaro", "belly_jiggle")
    p.box((0, belly_front(0.975) - 0.009, 0.968), (0.012, 0.006, 0.026), "Ziper", "belly_jiggle")

    # faixa refletiva da cintura
    p.tube((0, 0, 0.955), (0, 0, 0.985), 0.292, 0.292, "Faixa", "Spine", seg=24, caps=False, squash_y=0.86)

    # cinto: passantes e fivela vazada com pino
    p.tube((0, 0, 0.687), (0, 0, 0.747), 0.28, 0.28, "Cinto", "Hips", seg=24, caps=False, squash_y=0.8)
    for deg in (-150, -100, -40, 40, 100, 150, 180):
        a = math.radians(deg)
        pos = (0.288 * math.sin(a), -0.8 * 0.288 * math.cos(a), 0.717)
        p.box(pos, (0.02, 0.012, 0.078), "Macacao", "Hips", rot_z=deg)
    fy = -0.229
    for bx, bz, sx, sz in ((0, 0.746, 0.08, 0.011), (0, 0.688, 0.08, 0.011),
                           (0.0345, 0.717, 0.011, 0.07), (-0.0345, 0.717, 0.011, 0.07)):
        p.box((bx, fy, bz), (sx, 0.012, sz), "Metal", "Hips")
    p.box((-0.004, fy - 0.004, 0.717), (0.05, 0.006, 0.007), "Metal", "Hips")
    p.box((0.03, fy + 0.002, 0.717), (0.05, 0.014, 0.05), "Cinto", "Hips")   # ponta do cinto saindo

    # remendo da transportadora nas costas: placa curva com debrum e um caminhãozinho em relevo
    slab(p, torso_back, lin(-0.18, 0.18, 10), lin(1.125, 1.315, 4), 0.006, 0.009, "Faixa", "Chest")
    edge = ([(x, 1.13) for x in lin(-0.175, 0.175, 8)] + [(0.175, z) for z in lin(1.13, 1.31, 4)[1:]]
            + [(x, 1.31) for x in lin(0.175, -0.175, 8)[1:]] + [(-0.175, z) for z in lin(1.31, 1.13, 4)[1:]])
    border = []
    for x, z in edge:
        pt, n = torso_back(x, z)
        border.append(Vector(pt) + Vector(n).normalized() * 0.015)
    line(p, border, 0.0045, "Cinto", "Chest", seg=4)
    slab(p, torso_back, lin(-0.1, 0.03, 4), lin(1.19, 1.27, 2), 0.015, 0.006, "Camiseta", "Chest")   # baú
    slab(p, torso_back, lin(0.035, 0.1, 3), lin(1.19, 1.245, 2), 0.015, 0.006, "Camiseta", "Chest")  # cabine
    for wx in (-0.06, 0.065):
        pt, n = torso_back(wx, 1.185)
        n = Vector(n).normalized()
        oellipsoid(p, Vector(pt) + n * 0.023, (n.cross(Vector((0, 0, 1))), n, (0, 0, 1)),
                   (0.019, 0.006, 0.019), "Cinto", "Chest", seg=12, rings=4)

    # rolo de fita no quadril esquerdo, preso num mosquetão, com a ponta solta
    p.tube((0.22, -0.13, 0.64), (0.22, -0.08, 0.64), 0.057, 0.057, "Fita", "tape_roll", seg=16)
    p.tube((0.22, -0.1325, 0.64), (0.22, -0.0775, 0.64), 0.024, 0.024, "Furo", "tape_roll", seg=12)
    line(p, [(0.22, -0.105, 0.664), (0.215, -0.108, 0.7), (0.222, -0.115, 0.735),
                (0.23, -0.108, 0.7), (0.226, -0.105, 0.664)], 0.004, "Aco", "tape_roll", seg=6)
    p.box((0.245, -0.105, 0.565), (0.04, 0.003, 0.05), "Fita", "tape_roll", rot_y=-15)

    # cinta de catraca enrolada no lado direito do quadril, com a catraca pendurada
    cx_, cy_ = -0.3, -0.03
    p.tube((cx_ + 0.03, cy_, 0.63), (cx_ - 0.03, cy_, 0.63), 0.048, 0.048, "Cinta", "Hips", seg=16)
    p.tube((cx_ + 0.0315, cy_, 0.63), (cx_ - 0.0315, cy_, 0.63), 0.016, 0.016, "Furo", "Hips", seg=10)
    p.box((cx_ - 0.024, cy_, 0.565), (0.004, 0.036, 0.06), "Cinta", "Hips")
    p.box((cx_ - 0.026, cy_, 0.525), (0.026, 0.05, 0.032), "Aco", "Hips")
    line(p, [(cx_, cy_, 0.676), (cx_ + 0.004, cy_ - 0.006, 0.71), (-0.272, cy_ - 0.01, 0.725)], 0.004, "Aco", "Hips", seg=6)

    # trena presa no cinto, na frente
    p.cloth_box((-0.125, -0.232, 0.704), (0.07, 0.036, 0.07), "Trena", "Hips", cuts=2, radius=0.014)
    p.box((-0.125, -0.252, 0.704), (0.04, 0.006, 0.04), "Aco", "Hips")
    p.box((-0.125, -0.248, 0.666), (0.02, 0.012, 0.008), "Aco", "Hips")

    # flanela enfiada no cinto, atrás
    rag = p.cloth_box((-0.15, 0.222, 0.63), (0.11, 0.012, 0.17), "Flanela", "Hips", cuts=3, radius=0.005)
    p.wrinkle(rag, 0.012, scale=0.04, seed=5.5, stretch_z=2.0)

    # alças do quadro: fita chata passando pelo ombro e costurada nas costas, com regulador
    # Os pontos saem das superfícies do corpo (+ meia espessura), para a fita deitar no pano.
    for s in (1, -1):
        on = lambda surf, x, z, off=0.007: tuple(Vector(surf(x, z)[0]) + Vector(surf(x, z)[1]).normalized() * off)
        pts = [(0.17 * s, -0.215, 1.267), on(torso_front, 0.2 * s, 1.37), on(torso_front, 0.215 * s, 1.405),
               (0.22 * s, -0.07, shoulder_top(0.22, -0.07) + 0.006),
               (0.22 * s, 0.07, shoulder_top(0.22, 0.07) + 0.006),
               on(torso_back, 0.215 * s, 1.405), on(torso_back, 0.21 * s, 1.36), on(torso_back, 0.2 * s, 1.24)]
        norms = [(0, -1, 0.35), (0, -1, 0.6), (0, -0.6, 1), (0, 0, 1), (0, 0.6, 1), (0, 1, 0.6), (0, 1, 0.1)]
        for i in range(len(pts) - 1):
            a, b = Vector(pts[i]), Vector(pts[i + 1])
            dirv = b - a
            side_v = dirv.cross(Vector(norms[i])).normalized()
            up = side_v.cross(dirv).normalized()
            obox(p, (a + b) / 2, (dirv, side_v, up), (dirv.length + 0.012, 0.032, 0.006), "Cinto", "UpperChest")
        a, b = Vector(pts[0]), Vector(pts[1])
        dirv = b - a
        side_v = dirv.cross(Vector(norms[0])).normalized()
        obox(p, a.lerp(b, 0.55), (dirv, side_v, side_v.cross(dirv)), (0.026, 0.042, 0.014), "Aco", "UpperChest")

    # apito: bocal, câmara redonda e argola, no cordão
    dirv = Vector((0.866, 0, -0.5))
    w = Vector(mb.WHISTLE)
    mouth = w - dirv * 0.035
    line(p, [(0.04, -0.13, 1.45), (0.09, -0.2, 1.38), (0.11, -0.235, 1.315), tuple(mouth)],
         0.003, "Faixa", "whistle_cord", seg=5)
    p.tube(mouth, w + dirv * 0.002, 0.009, 0.011, "Faixa", "whistle_cord", seg=10, squash_y=0.7)
    p.ellipsoid(w + dirv * 0.016, (0.019, 0.016, 0.019), "Faixa", "whistle_cord", seg=10, rings=6)
    p.box(w - dirv * 0.012 + Vector((0, -0.008, 0.006)), (0.01, 0.004, 0.006), "Furo", "whistle_cord")


def build_arms(p):
    for side, (_, sh, el, wr, _) in ARMS.items():
        sh, el, wr = Vector(sh), Vector(el), Vector(wr)
        upper, lower = f"{side}UpperArm", f"{side}LowerArm"
        p.ellipsoid(sh, (0.077,) * 3, "Macacao", upper, seg=12, rings=6)
        sleeve = chain(p, [sh, el], (0.077, 0.072), "Macacao", [upper], seg=16, steps=4)
        p.wrinkle(sleeve, 0.006, scale=0.06, seed=4.2 if side == "Left" else 8.4)
        # manga dobrada: rolo gordo acima e abaixo do cotovelo
        cuff = el + (wr - el) * 0.25
        mid = el.lerp(cuff, 0.5)
        # (os três pontos na mesma reta, senão os anéis não coincidem e a dobra abre uma fresta)
        fdir = (wr - el).normalized()
        roll = chain(p, [el - fdir * 0.02, mid, cuff], (0.08, 0.088, 0.081),
                     "MacacaoClaro", [lower, lower], seg=16, steps=2)
        p.wrinkle(roll, 0.004, scale=0.04, seed=6.6 if side == "Left" else 9.9)
        # antebraço grosso de estivador (soldado: sem a emenda do meio)
        fore = chain(p, [cuff - fdir * 0.01, cuff.lerp(wr, 0.45), wr], (0.066, 0.07, 0.06), "Pele",
                     [lower, lower], seg=16, steps=2)
        weld(p, fore)
        p.ellipsoid(wr, (0.038, 0.058, 0.045), "Pele", lower, seg=10, rings=4)
        # mão nua (fica dentro da luva; aparece sozinha nas cutscenes, sem a malha da luva)
        p.hand(side, ARMS[side], HAND, "Pele", nail="Unha")

        # remendo redondo da transportadora no ombro esquerdo
        if side == "Left":
            axis = (el - sh).normalized()
            out = (Vector((1, 0, 0)) - axis * axis.x).normalized()
            fwd = axis.cross(out)
            c = sh + (el - sh) * 0.3
            r = 0.0755 - 0.005 * 0.3
            oellipsoid(p, c + out * (r + 0.003), (fwd, out, axis), (0.042, 0.005, 0.042), "Faixa", upper, seg=16, rings=4)
            oellipsoid(p, c + out * (r + 0.008), (fwd, out, axis), (0.03, 0.003, 0.03), "Camiseta", upper, seg=16, rings=4)
            obox(p, c + out * (r + 0.014), (fwd, out, axis), (0.026, 0.008, 0.02), "Caixa", upper)
            obox(p, c + out * (r + 0.0185), (fwd, out, axis), (0.006, 0.002, 0.021), "Fita", upper)
            build_tattoo(p, cuff, wr, lower)


def build_tattoo(p, cuff, wr, bone):
    """Âncora de estivador no lado de fora do antebraço esquerdo, em traço levemente elevado
    (no acabamento final vira textura; aqui é para o desenho aparecer na prévia)."""
    axis = (wr - cuff).normalized()
    out = (Vector((1, 0, 0)) - axis * axis.x).normalized()
    fwd = (Vector((0, -1, 0)) - axis * axis.dot(Vector((0, -1, 0)))).normalized()
    length = (wr - cuff).length

    def on_arm(s, h):
        t = 0.3 + h / length
        c = cuff + axis * (t * length)
        r = 0.067 + (0.07 - 0.067) * min(t / 0.45, 1) if t < 0.45 else 0.07 + (0.06 - 0.07) * (t - 0.45) / 0.55
        return c + fwd * s + out * (math.sqrt(max(r * r - s * s, 0)) + 0.0006)

    ink = lambda pts: line(p, [on_arm(s, h) for s, h in pts], 0.0022, "Tatuagem", bone)
    ring = [(0.008 * math.sin(a), 0.008 - 0.008 * math.cos(a)) for a in lin(0, 2 * math.pi, 9)]
    ink(ring)
    ink([(0, 0.016), (0, 0.09)])
    ink([(-0.022, 0.03), (0.022, 0.03)])
    ink([(-0.032, 0.066), (-0.024, 0.08), (-0.012, 0.088), (0, 0.091), (0.012, 0.088), (0.024, 0.08), (0.032, 0.066)])
    for k in (-1, 1):
        ink([(0.032 * k, 0.066), (0.026 * k, 0.074)])
        ink([(0.032 * k, 0.066), (0.038 * k, 0.073)])


def build_head(p):
    """Cabeça da versão aprovada: pescoço, cabeça, orelhas com concha e lóbulo, olhos de bolinha
    com pálpebra pesada, bochechas, sobrancelhas grossas e o nariz de batata."""
    p.tube((0, -0.005, 1.38), (0, -0.01, 1.54), 0.102, 0.096, "PeleSombra", "Neck", seg=14)
    p.ellipsoid(mb.HEAD_C, mb.HEAD_R, "Pele", "Head", seg=24, rings=14)
    for s in (1, -1):
        ex, ez = mb.d(150 + 50 * s, 92)
        # orelha com a concha marcada e o lóbulo
        p.ellipsoid((ex, 0.0, ez), (0.037, 0.022, 0.037), "Pele", "Head", seg=10, rings=6)
        p.ellipsoid((ex + 0.012 * s, -0.004, ez + 0.004), (0.022, 0.012, 0.024), "PeleSombra", "Head", seg=8, rings=4)
        p.ellipsoid((ex + 0.004 * s, -0.004, ez - 0.032), (0.016, 0.012, 0.014), "Pele", "Head", seg=8, rings=4)
        # olhos com pálpebra pesada e sobrancelha grossa
        p.ellipsoid((0.06 * s, -0.152, 1.673), (0.018, 0.01, 0.018), "Olho", "Head", seg=10, rings=5)
        p.ellipsoid((0.06 * s, -0.149, 1.684), (0.025, 0.012, 0.013), "PeleSombra", "Head", seg=10, rings=4)
        p.ellipsoid((0.068 * s, -0.124, 1.632), (0.03, 0.02, 0.022), "Pele", "Head", seg=10, rings=5)   # bochecha
        p.polyline([(0.104 * s, -0.146, 1.714), (0.082 * s, -0.159, 1.724), (0.058 * s, -0.165, 1.725),
                    (0.028 * s, -0.167, 1.712)], 0.0145, "Barba", "Head", seg=6)
        p.ellipsoid((0.022 * s, -0.159, 1.626), (0.015, 0.016, 0.013), "PeleSombra", "Head", seg=8, rings=4)  # asa do nariz
    # nariz de batata com ponte
    p.ellipsoid((0, -0.158, 1.66), (0.017, 0.02, 0.03), "Pele", "Head", seg=8, rings=4)
    p.ellipsoid((0, -0.17, 1.634), (0.031, 0.032, 0.028), "PeleSombra", "Head", seg=12, rings=6)


def build_beard(arm, col):
    """Barba da versão aprovada: massa cheia que desce em pá na frente, com fios caindo
    (wrinkle esticado na altura), bigode de morsa e costeletas subindo até o gorro."""
    p = bc.Part("SK_CHR_Marcelo_Barba", MATS)
    c, r = Vector((0, -0.06, 1.56)), Vector((0.165, 0.132, 0.103))
    mass = p.ellipsoid(c, r, "Barba", "beard_01", seg=24, rings=12)
    for v in {v for f in mass for v in f.verts}:
        q = v.co / bc.K - c
        front = max(0.0, min(1.0, -q.y / r.y))
        if q.z < 0:
            low = -q.z / r.z
            q.z *= 1.0 + 0.55 * front ** 1.5
            q.y -= 0.02 * front * low
            # barra em pontas: sete tufos caindo em volta do queixo
            ang = math.atan2(q.x, -q.y)
            q.z -= 0.022 * low ** 2 * (0.5 + 0.5 * math.cos(7 * ang))
        v.co = (c + q) * bc.K
    p.wrinkle(mass, 0.016, scale=0.022, seed=1.7, stretch_z=3.0)
    for s in (1, -1):
        p.ellipsoid((0.036 * s, -0.188, 1.598), (0.044, 0.02, 0.017), "Barba", "beard_01",
                    seg=12, rings=6, rot=(0, -16 * s, 0))
        p.ellipsoid((0.15 * s, -0.062, 1.63), (0.02, 0.03, 0.066), "Barba", "Head", seg=10, rings=6)
    p.polyline([(-0.03, -0.196, 1.556), (0.0, -0.201, 1.55), (0.03, -0.196, 1.556)], 0.007, "PeleSombra", "beard_01", seg=6)
    return p.to_object(col, arm)


def build_body(arm, col):
    p = bc.Part("SK_CHR_Marcelo", MATS)
    # imprime o custo de cada parte, para saber onde cortar se passar do orçamento
    for fn in (build_boots, build_legs, build_torso, build_arms, build_head):
        before = tris(p.bm)
        fn(p)
        print("TRIS_PARTE", fn.__name__, tris(p.bm) - before)
    return p.to_object(col, arm)


def build_beanie(arm, col):
    """Gorro de tricô: domo com relevo, dobra canelada e etiqueta de couro."""
    p = bc.Part("SK_CHR_Marcelo_Gorro", MATS)
    dome = p.ellipsoid((0, 0, mb.BEANIE_BASE), mb.BEANIE_R, "Gorro", "Head", seg=28, rings=12)
    p.cut_below(dome, mb.BEANIE_BASE - 0.005)
    p.wrinkle([f for f in dome if f.is_valid], 0.004, scale=0.025, seed=3.3, stretch_z=2.5)
    z0, z1 = mb.d(0, 80)[1], mb.d(0, 60)[1]
    # dobra canelada: um tubo de 56 lados com um vértice sim, outro não, puxado para dentro (o fundo do gomo ainda fica por fora do domo, senão o domo aparece entre os gomos)
    # (antes de soldar: depois da solda parte das faces do chain deixa de valer)
    fold = chain(p, [(0, 0, z0), ((0, 0, z1))], (0.188, 0.185), "GorroDobra", ["Head"], seg=56, steps=2, squash_y=0.97)
    for v in {v for f in fold for v in f.verts}:
        k = round(math.atan2(v.co.y, v.co.x) / (2 * math.pi / 56))
        if k % 2:
            v.co.x *= 0.96
            v.co.y *= 0.96
    for f in fold:
        f.smooth = False   # facetado: cada gomo pega luz de um lado e sombra do outro
    weld(p, fold)
    a = math.radians(38)
    obox(p, (0.192 * math.sin(a), -0.97 * 0.192 * math.cos(a), (z0 + z1) / 2),
         ((math.cos(a), math.sin(a), 0), (math.sin(a), -math.cos(a), 0), (0, 0, 1)),
         (0.032, 0.006, 0.024), "Couro", "Head")
    return p.to_object(col, arm)


def build_gloves(arm, col):
    """Luvas de carga: casca da mão + punho com tira de velcro e reforço na palma."""
    p = bc.Part("SK_CHR_Marcelo_Luvas", MATS)
    for side, (_, _, el, wr, _) in ARMS.items():
        wrv, elv = Vector(wr), Vector(el)
        dirv = (wrv - elv).normalized()
        p.tube(wrv - dirv * 0.018, wrv + dirv * 0.022, 0.071, 0.07, "Velcro", f"{side}Hand", seg=16)
        p.hand(side, ARMS[side], HAND, "Luva", grow=GLOVE, knuckle_pad="Reforco")
        w, a, t, n = bc.hand_frame(side, ARMS[side])
        palm = HAND["length"] * (1 - HAND["middle"])
        thick = HAND["thick"] + 2 * GLOVE
        oellipsoid(p, w + a * (palm * 0.45) + n * (thick / 2 + 0.001), (t, a, n),
                   (HAND["knuckles"] * 0.42, palm * 0.34, 0.006), "Reforco", f"{side}Hand", seg=12, rings=4, real=True)
        obox(p, w + a * 0.004 - n * (0.072 * bc.K * 0.75), (t, a, n), (0.045, 0.03, 0.012), "Luva", f"{side}Hand", real=True)
    return p.to_object(col, arm)


def build_board(col, socket):
    """Quadro branco: moldura de alumínio com cantoneiras, bandeja com marcador e apagador,
    desenho de fantasma com seta e um bilhete do Seu Valdemar preso com fita."""
    p = bc.Part("SM_PRP_Quadro", MATS)
    cx, cz, hw, hh = 0.0, 1.147, 0.19, 0.137
    p.box((cx, -0.208, cz), (0.37, 0.012, 0.264), "Moldura", "board")
    p.box((cx, -0.217, cz), (0.37, 0.006, 0.264), "Quadro", "board")
    corners = [(-hw, cz - hh), (hw, cz - hh), (hw, cz + hh), (-hw, cz + hh)]
    for (x0, z0), (x1, z1) in zip(corners, corners[1:] + corners[:1]):
        p.tube((x0, -0.217, z0), (x1, -0.217, z1), 0.0105, 0.0105, "Moldura", "board", seg=8, caps=False)
    for x, z in corners:
        p.ellipsoid((x, -0.217, z), (0.016, 0.016, 0.016), "Cinto", "board", seg=8, rings=4)
    # bandeja embaixo, com marcador (corpo branco, tampa rosa) e apagador
    p.box((cx, -0.236, cz - hh - 0.006), (0.3, 0.028, 0.008), "Moldura", "board")
    p.box((cx, -0.249, cz - hh + 0.002), (0.3, 0.004, 0.014), "Moldura", "board")
    mz, my = cz - hh + 0.009, -0.236
    p.tube((0.02, my, mz), (0.115, my, mz), 0.008, 0.008, "MarcadorCorpo", "board", seg=10)
    p.tube((0.115, my, mz), (0.145, my, mz), 0.0088, 0.0075, "Marcador", "board", seg=10)
    p.box((-0.1, my, mz + 0.006), (0.075, 0.022, 0.018), "Cinto", "board")
    p.box((-0.1, my, mz - 0.004), (0.075, 0.022, 0.006), "Feltro", "board")

    # desenho no quadro (rosa, do marcador): fantasma, seta e exclamação
    sy = -0.2205
    gx, gz = -0.08, 1.16
    ghost = [(gx + 0.045 * math.cos(a), sy, gz + 0.045 * math.sin(a)) for a in lin(0, math.pi, 9)]
    ghost += [(gx - 0.045, sy, gz - 0.05), (gx - 0.03, sy, gz - 0.035), (gx - 0.015, sy, gz - 0.05),
              (gx, sy, gz - 0.035), (gx + 0.015, sy, gz - 0.05), (gx + 0.03, sy, gz - 0.035),
              (gx + 0.045, sy, gz - 0.05), (gx + 0.045, sy, gz)]
    line(p, ghost, 0.0028, "Marcador", "board", seg=5)
    for ex in (-0.015, 0.015):
        p.ellipsoid((gx + ex, sy, gz + 0.008), (0.006, 0.003, 0.009), "Marcador", "board", seg=8, rings=4)
    line(p, [(gx + 0.065, sy, gz - 0.005), (gx + 0.14, sy, gz - 0.005)], 0.0028, "Marcador", "board", seg=5)
    line(p, [(gx + 0.12, sy, gz + 0.015), (gx + 0.142, sy, gz - 0.005), (gx + 0.12, sy, gz - 0.025)],
               0.0028, "Marcador", "board", seg=5)
    line(p, [(0.1, sy, 1.105), (0.1, sy, 1.065)], 0.0034, "Marcador", "board", seg=5)
    p.ellipsoid((0.1, sy, 1.05), (0.005, 0.003, 0.005), "Marcador", "board", seg=8, rings=4)

    # bilhete do Seu Valdemar preso com dois pedaços de fita, com as linhas escritas
    nx, nz, rot = 0.115, 1.235, -7
    p.box((nx, -0.2208, nz), (0.1, 0.002, 0.075), "Papel", "board", rot_y=rot)
    ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    local = lambda u, v: (nx + u * ca + v * sa, nz - u * sa + v * ca)
    for u in (-0.04, 0.04):
        tx, tz = local(u, 0.036)
        p.box((tx, -0.2222, tz), (0.03, 0.0014, 0.014), "Fita", "board", rot_y=rot + (12 if u > 0 else -12))
    for v, w in ((0.016, 0.07), (0.0, 0.075), (-0.016, 0.05)):
        pts = []
        for i in range(7):
            u = -0.038 + w * i / 6
            x, z = local(u, v + 0.002 * math.sin(i * 2.1))
            pts.append((x, -0.2222, z))
        line(p, pts, 0.0013, "Tinta", "board", seg=4)
    return p.to_prop(col, socket, mb.BOARD_GRIP)


# ---------------------------------------------------------------- fotos

def shot(scene, col, name, target, offset, lens=85, size=(900, 900), ortho=None):
    cam_data = bpy.data.cameras.new(f"CAM_{name}")
    if ortho:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho
    else:
        cam_data.lens = lens
    cam = bpy.data.objects.new(f"CAM_{name}", cam_data)
    col.objects.link(cam)
    t = Vector(target) * bc.K
    cam.location = t + Vector(offset)
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = str(OUT / "previews" / "detalhado" / f"{name}.png")
    bpy.ops.render.render(write_still=True)


def closeups(scene, col):
    shot(scene, col, "close_rosto", (0, -0.05, 1.66), (0.35, -0.95, 0.08))
    shot(scene, col, "close_barba", (0.04, -0.09, 1.57), (0.75, -0.55, -0.05))
    shot(scene, col, "close_quadro", (0, -0.2, 1.17), (0.12, -1.25, 0.1))
    shot(scene, col, "close_cintura", (0, -0.1, 0.68), (0.55, -1.25, 0.2))
    shot(scene, col, "close_botas", (0, -0.05, 0.15), (0.6, -1.2, 0.45))
    shot(scene, col, "close_costas", (0, 0.15, 1.05), (-0.75, 1.9, 0.3), lens=70)
    shot(scene, col, "close_braco", (0.4, -0.02, 0.93), (1.05, -0.5, 0.05))


def side_by_side(scene, col):
    """Blockout (à esquerda) e detalhado (à direita), de frente e de 3/4."""
    src = OUT / "SK_CHR_Marcelo_Blockout.blend"
    with bpy.data.libraries.load(str(src)) as (data_from, data_to):
        data_to.objects = [n for n in data_from.objects if not n.startswith("CAM_")]
    tmp = bpy.data.collections.new("_Comparacao")
    scene.collection.children.link(tmp)
    for o in data_to.objects:
        if o:
            tmp.objects.link(o)
            if o.parent is None:
                o.location.x -= 0.6
    for o in bpy.data.objects:
        if o.parent is None and o.type == "ARMATURE" and o.name not in tmp.objects:
            o.location.x += 0.6
    bpy.context.view_layer.update()
    h = mb.HEIGHT
    shot(scene, col, "lado_a_lado_frente", (0, 0, h / 2 / bc.K), (0, -8, 0), size=(1300, 900), ortho=h * 1.15)
    shot(scene, col, "lado_a_lado_tres_quartos", (0, 0, h / 2 / bc.K), (3.0, -6.2, 0.7), lens=60, size=(1300, 900))


def main():
    scene, cols = bc.new_scene("Marcelo")
    arm = mb.build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    beard = build_beard(arm, cols["Geo"])
    beanie = build_beanie(arm, cols["Geo"])
    gloves = build_gloves(arm, cols["Geo"])

    soc = cols["Sockets"]
    for side, (_, _, el, wr, _) in ARMS.items():
        palm = Vector(wr) + (Vector(wr) - Vector(el)).normalized() * 0.075
        bc.add_socket(f"SOC_Hand_{side[0]}", soc, arm, f"{side}Hand", palm)
        bc.add_socket(f"SOC_Carry_{'A' if side == 'Left' else 'B'}", soc, arm, f"{side}Hand", palm + Vector((0, -0.07, 0)))
    chest = bc.add_socket("SOC_Chest", soc, arm, "board_hinge", mb.BOARD_GRIP)
    bc.add_socket("SOC_Mouth", soc, arm, "Head", (0, -0.2, 1.553))
    board = build_board(cols["Props"], chest)

    bc.report_tris((body, beard, beanie, gloves, board))
    # O modelo do jogo: mesmo caminho, nomes de malhas, ossos e encaixes do blockout, então o
    # avatar Humanoid e a ficha do Marcelo continuam valendo (antes das fotos: o lado a lado
    # carrega o blockout na cena).
    fbx = bc.UNITY_ROOT / "Characters" / "Marcelo" / "SK_CHR_Marcelo.fbx"
    bc.export_fbx(fbx)
    out = OUT / "previews" / "detalhado"
    bc.render_previews(scene, cols["Preview"], out, "marcelo_detalhado", mb.HEIGHT)
    closeups(scene, cols["Preview"])
    bc.save_blend(OUT / "SK_CHR_Marcelo_Detalhado.blend")
    side_by_side(scene, cols["Preview"])
    print("SAVED", OUT / "SK_CHR_Marcelo_Detalhado.blend", fbx)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
