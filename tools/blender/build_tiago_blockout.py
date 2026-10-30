"""Blockout do Tiago (CHR_02) gerado a partir da ficha do Art Bible.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_tiago_blockout.py

Saídas: Art/Source/Characters/Tiago/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Characters/Tiago/SK_CHR_Tiago.fbx
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bmesh  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

# desenho da ficha: chão em y = 522, centro em x = 120; o topo do boné define a altura
d = bc.mapper(120, 522)
HEIGHT = 1.74
CAP_BASE = d(0, 52)[1]
CAP_R = (0.17, 0.168, 0.118)
bc.set_scale(HEIGHT / (CAP_BASE + CAP_R[2]))

MATS = bc.Materials("MAT_CHR_Tiago", {
    "Pele": ("#A8704A", 0.0, 0.55, 0.0),
    "PeleSombra": ("#9A6440", 0.0, 0.6, 0.0),
    "Cabelo": ("#1E1A1A", 0.0, 0.8, 0.0),
    "Bone": ("#3F5A5E", 0.0, 0.7, 0.0),             # a cor do colete (era laranja)
    "BoneAba": ("#33484C", 0.0, 0.7, 0.0),          # o escuro do colete
    "BoneAbaBaixo": ("#3F5A44", 0.0, 0.8, 0.0),     # verde por baixo da aba (detalhe clássico de boné)
    "BoneCostura": ("#2A3B3E", 0.0, 0.7, 0.0),      # pesponto tom sobre tom
    "BoneGasto": ("#6F8A8D", 0.0, 0.8, 0.0),        # beirada da aba desbotada de uso
    "BoneLogo": ("#F08A3C", 0.0, 0.6, 0.0),         # bordado do caminhãozinho, no laranja da empresa
    "Moletom": ("#6E7F72", 0.0, 0.85, 0.0),
    "MoletomEscuro": ("#5C6B60", 0.0, 0.85, 0.0),
    "Colete": ("#3F5A5E", 0.0, 0.8, 0.0),
    "ColeteEscuro": ("#33484C", 0.0, 0.8, 0.0),
    "Ziper": ("#5E6166", 0.8, 0.4, 0.0),
    "CapuzDentro": ("#3E4A42", 0.0, 0.9, 0.0),
    "Sola": ("#F2F0EA", 0.0, 0.6, 0.0),
    "SolaSulco": ("#B8B2A6", 0.0, 0.7, 0.0),
    "Costura": ("#D2CDC2", 0.0, 0.7, 0.0),
    "Faixa": ("#F08A3C", 0.0, 0.5, 0.3),
    "Calca": ("#1C1C20", 0.0, 0.85, 0.0),           # preta (era azul-marinho)
    "CalcaBarra": ("#2A2A30", 0.0, 0.85, 0.0),      # o avesso da barra, um tom acima
    "Bolsa": ("#2E2F35", 0.0, 0.55, 0.0),          # náilon escuro (bolsa transversal da referência)
    "BolsaDetalhe": ("#3A3C43", 0.0, 0.55, 0.0),
    "Alca": ("#232328", 0.0, 0.7, 0.0),            # fita da alça
    "Tenis": ("#EDEBE4", 0.0, 0.6, 0.0),
    "CalcaCostura": ("#E6E4DE", 0.0, 0.7, 0.0),     # linha branca de pesponto (era dourada)
    "Remendo": ("#45454C", 0.0, 0.9, 0.0),          # preto desbotado (cinza) do remendo no joelho
    "Papel": ("#E9DFC8", 0.0, 0.9, 0.0),
    "Olho": ("#F4F0E8", 0.0, 0.3, 0.0),
    "Pupila": ("#2A1E18", 0.0, 0.2, 0.0),
    "Boca": ("#5A2A2A", 0.0, 0.6, 0.0),
    "Lapis": ("#E8C547", 0.0, 0.6, 0.0),
    "Grafite": ("#3A2A20", 0.0, 0.6, 0.0),
    "Madeira": ("#E2B98A", 0.0, 0.8, 0.0),
    "Adesivo": ("#7FE3C4", 0.0, 0.5, 0.0),
    "Metal": ("#D9B45A", 1.0, 0.3, 0.0),
    "Preto": ("#131118", 0.0, 0.7, 0.0),
    "Unha": ("#D9A88A", 0.0, 0.4, 0.0),
})

# Ficha "Tiago — mão direita": mão de 20 cm (punho até a ponta do médio), médio = 54%,
# dedos longos para sinais legíveis de longe, palma quase quadrada. Curativo no indicador e
# mancha de caneta são decal na textura (_BC), não malha.
HAND = {
    "length": 0.20, "middle": 0.54, "knuckles": 0.092, "wrist": 0.078, "thick": 0.030,
    "radius": {"Thumb": 0.0118, "Index": 0.0093, "Middle": 0.0097, "Ring": 0.0093, "Little": 0.0084},
    "ratio": {"Index": 0.92, "Middle": 1.0, "Ring": 0.94, "Little": 0.74},
    "drop": {"Ring": 0.002, "Little": 0.009},
    "splay": {"Index": 3, "Middle": 0, "Ring": 3, "Little": 7},
    "thumb_len": 0.075, "knuckle_bulge": 0.04,
}

ARMS = {
    "Left": ((0.03, 0, 1.13), (0.167, 0, 1.12), (0.24, 0, 0.907), (0.267, 0, 0.693), (0.27, 0, 0.62)),
    "Right": ((-0.03, 0, 1.13), (-0.167, 0, 1.12), (-0.24, 0, 0.907), (-0.267, 0, 0.693), (-0.27, 0, 0.62)),
}
LEGS = {side: ((x, 0, 0.64), (x * 1.08, 0, 0.357), (x * 1.12, 0, 0.073))
        for side, x in (("Left", 0.072), ("Right", -0.072))}
HEAD_C = Vector((0.0, -0.01, d(0, 76)[1]))
HEAD_R = Vector((0.16, 0.153, 0.187))
Z_HEM, Z_TOP = d(0, 338)[1], d(0, 176)[1]


ZS = d(0, 266)[1]   # altura da faixa refletiva

# Colete moldado no moletom: cada altura é a elipse do moletom afastada VEST_GAP para fora, com a
# frente aberta (VEST_OPEN graus de cada lado do meio), cava em volta do braço e alça por cima do
# ombro. As linhas da grade caem nas bordas da faixa refletiva (ela é pintada na própria malha).
VEST_GAP = 0.012
VEST_OPEN = 15
VEST_ROWS = [0.64, 0.70, 0.76, ZS - 0.015, ZS + 0.015, 0.92, 0.98, 1.04, 1.09, Z_TOP - 0.01]


def body_r(z):
    """Raio do tubo do moletom em z (o achatado em Y é 0,6)."""
    return 0.135 + 0.045 * (z - Z_HEM) / (Z_TOP - Z_HEM)


def vest_a(z):
    return body_r(z) + VEST_GAP


def vest_b(z):
    return 0.6 * body_r(z) + VEST_GAP


def vest_y(x, z):
    """Y da frente do colete em (x, z)."""
    return -vest_b(z) * math.sqrt(max(1.0 - (x / vest_a(z)) ** 2, 0.0))


def shoulder_z(x, y):
    """Altura do alto do moletom em (x, y): a tampa do tubo do tronco ou o domo dos ombros."""
    top = Z_TOP if (x / 0.18) ** 2 + (y / 0.108) ** 2 < 1 else 0.0
    u = 1 - (x / 0.18) ** 2 - (y / 0.105) ** 2
    dome = Z_TOP - 0.02 + 0.05 * math.sqrt(u) if u > 0 else 0.0
    return max(top, dome)


def near_arm(pt, clearance=0.058):
    """Perto do braço (raio da manga 0,038 + folga)? Define a cava do colete."""
    pt = Vector(pt)
    for side in ARMS.values():
        a, b = Vector(side[1]), Vector(side[2])
        t = max(0.0, min(1.0, (pt - a).dot(b - a) / (b - a).length_squared))
        if (pt - (a + (b - a) * t)).length < clearance:
            return True
    return False


# Bolsa transversal pequena, em pé, no quadril esquerdo, entre a frente e o lado: encosta
# na barra do colete a 50° do meio da frente (mais de lado, a mão esquerda encosta nela).
# Medidas: 16 x 6 x 20 cm (largura x fundo x altura).
# BAG_T = largura (tangente ao corpo); BAG_N = frente da bolsa (para fora do corpo).
BAG_W, BAG_D, BAG_H = 0.16, 0.06, 0.20
_t, _zb = math.radians(-40), 0.66   # ângulo na elipse (0 = lado); -40 deixa ~1,5 cm até a mão esquerda
_a, _b = vest_a(_zb), vest_b(_zb)
BAG_N = Vector((math.cos(_t) / _a, math.sin(_t) / _b, 0)).normalized()
BAG_T = Vector((-BAG_N.y, BAG_N.x, 0))
BAG = tuple(Vector((_a * math.cos(_t), _b * math.sin(_t), 0.62)) + BAG_N * (BAG_D / 2 + 0.006))
BAG_PHI = math.atan2(BAG_T.y, BAG_T.x)


def bag_pt(lx, ly, lz):
    """Ponto da bolsa: lx na largura (+ para o lado de fora), ly no fundo (- = frente), lz na altura."""
    return Vector(BAG) + BAG_T * lx - BAG_N * ly + Vector((0, 0, lz))


def patch_frame(x0, x1, z):
    """Plano de uma peça chapada no colete (bolso, aba): a corda da curva entre x0 e x1.
    Devolve a origem (meio da corda) e o ângulo em Z; no plano, -Y local aponta para fora."""
    y0, y1 = vest_y(x0, z), vest_y(x1, z)
    return Vector(((x0 + x1) / 2, (y0 + y1) / 2, z)), math.atan2(y1 - y0, x1 - x0)


def on_patch(frame, lx, ly, lz):
    o, ang = frame
    c, s = math.cos(ang), math.sin(ang)
    return (o.x + lx * c - ly * s, o.y + lx * s + ly * c, o.z + lz)


# bolso do peito esquerdo, onde o bloquinho fica guardado (a ponta de cima aparece)
CHEST_POCKET = patch_frame(0.05, 0.13, 1.0)
NOTEBOOK_GRIP = on_patch(CHEST_POCKET, 0.0, -0.01, 0.035)
PENCIL_GRIP = (0.175, 0.02, 1.577)


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.64), (0, 0, 0.76), None, False),
        ("Spine", (0, 0, 0.76), (0, 0, 0.90), "Hips", True),
        ("Chest", (0, 0, 0.90), (0, 0, 1.02), "Spine", True),
        ("UpperChest", (0, 0, 1.02), (0, 0, 1.15), "Chest", True),
        ("Neck", (0, -0.005, 1.15), (0, -0.01, 1.27), "UpperChest", False),
        ("Head", (0, -0.01, 1.27), (0, -0.01, 1.66), "Neck", True),
        ("hood_01", (0, 0.08, 1.18), (0, 0.11, 1.05), "UpperChest", False),
        # o bloquinho saiu do cordão e foi para o bolso, mas o osso e o SOC_Neck ficam (mesmo nome e
        # pai): o clipe PegaLanterna precisa da mesma hierarquia do Tiago para importar
        ("notebook_lanyard", (0, -0.07, 1.2), NOTEBOOK_GRIP, "UpperChest", False),
        ("bag", (BAG[0], BAG[1], BAG[2] + 0.12), (BAG[0], BAG[1], BAG[2] - 0.08), "Hips", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.08, toe_fwd=0.14)
    arm = bc.build_armature("RIG_CHR_Tiago", col, bones)
    bc.add_finger_bones(arm, ARMS, {side: HAND for side in ARMS})
    return arm


def build_body(arm, col):
    p = bc.Part("SK_CHR_Tiago", MATS)

    # tênis de quadra, cano baixo, todo branco
    for s, side in ((1, "Left"), (-1, "Right")):
        build_sneaker(p, 0.08 * s, f"{side}Foot")

    # calça (perna direita inteira, esquerda arregaçada, como na ficha): ver build_pants
    build_pants(p)
    p.tube((0.081, 0, 0.212), (0.081, 0, 0.252), 0.056, 0.054, "CalcaBarra", "LeftLowerLeg", seg=12)
    p.tube((0.081, 0, 0.215), (0.081, 0, 0.11), 0.036, 0.032, "Pele", "LeftLowerLeg", seg=10)
    # meia: só um pouco mais larga que a canela (0,032 a 0,036), com uma faixa no punho
    p.tube((0.081, 0, 0.125), (0.08, 0, 0.06), 0.0345, 0.033, "Tenis", "LeftLowerLeg", seg=12)
    p.tube((0.081, 0, 0.113), (0.081, 0, 0.127), 0.0362, 0.0362, "Costura", "LeftLowerLeg", seg=12, caps=False)
    # barra dobrada na perna direita
    p.tube((-0.08, 0, 0.068), (-0.08, 0, 0.105), 0.055, 0.054, "CalcaBarra", "RightLowerLeg", seg=12)

    # moletom (silhueta em V), barra canelada, ombros
    z_hem, z_top = Z_HEM, Z_TOP
    p.tube((0, 0, z_hem), (0, 0, z_top), 0.135, 0.18, "Moletom", "Chest", seg=24, squash_y=0.6)
    p.tube((0, 0, z_hem - 0.005), (0, 0, z_hem + 0.045), 0.142, 0.143, "MoletomEscuro", "Hips", seg=24, squash_y=0.62)
    for i in range(24):
        t = math.tau * i / 24
        rx, ry = 0.1445 * math.cos(t), 0.62 * 0.1445 * math.sin(t)
        p.tube((rx, ry, z_hem - 0.004), (rx, ry, z_hem + 0.04), 0.0032, 0.0032, "MoletomEscuro", "Hips",
               seg=4, caps=False)
    p.ellipsoid((0, 0, z_top - 0.02), (0.18, 0.105, 0.05), "Moletom", "UpperChest", seg=24, rings=8)

    # colete moldado no corpo (faixa refletiva e logo das costas pintados nele) e o vivo das bordas
    build_vest(p)
    zs = ZS

    # costura pespontada nas bordas da abertura do colete (pula a faixa refletiva)
    for s in (1, -1):
        z = 0.665
        while z < z_top - 0.02:
            if abs(z - zs) > 0.024:
                x = s * (vest_a(z) * math.sin(math.radians(VEST_OPEN)) + 0.012)
                p.tube((x, vest_y(x, z) - 0.0015, z), (x, vest_y(x, z + 0.012) - 0.0015, z + 0.012),
                       0.0016, 0.0016, "Papel", "Chest", seg=4, caps=False)
            z += 0.024

    # bolso do peito (esquerdo) com o bloquinho; o pesponto escuro marca a boca do bolso
    deg = math.degrees(CHEST_POCKET[1])
    p.box(on_patch(CHEST_POCKET, 0, -0.01, -0.0025), (0.08, 0.02, 0.085), "Colete", "UpperChest", rot_z=deg)
    p.box(on_patch(CHEST_POCKET, 0, -0.011, 0.0375), (0.082, 0.022, 0.008), "ColeteEscuro", "UpperChest",
          rot_z=deg)
    # bolso de baixo (direito) com aba e botão; a alça da bolsa passa pelo outro lado
    low = patch_frame(-0.13, -0.05, 0.72)
    deg = math.degrees(low[1])
    p.box(on_patch(low, 0, -0.004, 0), (0.08, 0.008, 0.075), "Colete", "Spine", rot_z=deg)
    p.box(on_patch(low, 0, -0.009, 0.03), (0.086, 0.006, 0.03), "ColeteEscuro", "Spine", rot_z=deg)
    p.ellipsoid(on_patch(low, 0, -0.0125, 0.024), (0.007, 0.003, 0.007), "Metal", "Spine", seg=8, rings=4,
                rot=(0, 0, deg))

    # gola média do moletom com a borda dobrada (cobre metade do pescoço; mais baixa, ele parecia
    # comprido de costas). A gola fecha no pescoço (raio 0,045) e a borda é um rolinho que encosta
    # nele: sem folga em volta.
    p.tube((0, 0, z_top - 0.015), (0, -0.005, z_top + 0.085), 0.09, 0.056, "MoletomEscuro", "UpperChest", seg=16)
    p.tube((0, -0.005, z_top + 0.068), (0, -0.006, z_top + 0.088), 0.062, 0.059, "Moletom", "UpperChest", seg=16)
    rim = [(0.053 * math.cos(a), -0.006 + 0.053 * math.sin(a), z_top + 0.09)
           for a in (i * math.tau / 12 for i in range(13))]
    p.polyline(rim, 0.009, "Moletom", "UpperChest", seg=6)
    # capuz abaixado, caído por cima do colete nas costas; os cordões saem das pontas da borda dele
    build_hood(p)
    for s in (1, -1):
        p.polyline([(HOOD_CORD[0] * s, HOOD_CORD[1], HOOD_CORD[2]), (0.033 * s, -0.112, 1.04)], 0.004, "Olho",
                   "Chest", seg=6)
        p.ellipsoid((0.033 * s, -0.113, 1.03), (0.006, 0.006, 0.012), "Faixa", "Chest", seg=6, rings=4)

    # alça transversal (fita larga e reta) do ombro direito até a bolsa no quadril esquerdo: na frente
    # ela cruza o peito até a argola de dentro; nas costas, passa por cima do capuz e dá a volta pelo
    # lado esquerdo até a argola de fora. Por cima do colete o caminho todo.
    inner, outer = bag_pt(-0.065, 0, 0.137), bag_pt(0.065, 0, 0.137)
    strap_front = [(-0.14, 0.0, 1.2), (-0.137, -0.065, 1.182), (-0.12, -0.112, 1.1), (-0.06, -0.125, 0.98),
                   (0.0, -0.13, 0.86), (0.07, -0.14, 0.78), inner]
    strap_back = [(-0.14, 0.0, 1.2), (-0.137, 0.075, 1.18), (-0.12, 0.14, 1.1), (-0.06, 0.178, 1.01),
                  (0.01, 0.155, 0.9), (0.12, 0.11, 0.8), (0.175, 0.04, 0.77), (0.19, -0.03, 0.76), outer]
    ribbon(p, strap_front, 0.034, 0.004, "Alca", "Chest")
    ribbon(p, strap_back, 0.034, 0.004, "Alca", "Chest")
    # fivela de regular a alça, deitada na direção da alça (56° no plano da frente)
    p.box((-0.084, -0.122, 1.046), (0.026, 0.016, 0.04), "Metal", "Chest", rot_y=56.3)
    p.box((-0.084, -0.1305, 1.046), (0.012, 0.004, 0.03), "Alca", "Chest", rot_y=56.3)
    build_bag(p)

    # braços em A-pose, munhequeiras, mãos
    for side, (_, sh, el, wr, _) in ARMS.items():
        p.limb([sh, el, wr], (0.038, 0.036, 0.034), "Moletom", [f"{side}UpperArm", f"{side}LowerArm"])
        wr, el = Vector(wr), Vector(el)
        dirv = (wr - el).normalized()
        band = "Faixa" if side == "Right" else "Colete"
        p.tube(wr - dirv * 0.03, wr - dirv * 0.002, 0.039, 0.039, band, f"{side}LowerArm", seg=12)
        p.hand(side, ARMS[side], HAND, "Pele", nail="Unha")

    # pescoço, cabeça, orelhas
    # o pescoço entra na cabeça (a base dela fica em ~1,31 m a 4,6 cm do eixo): sem fresta entre os dois
    p.tube((0, -0.005, 1.14), (0, -0.011, 1.34), 0.046, 0.044, "PeleSombra", "Neck", seg=12)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        ex, ez = d(120 + 47 * s, 82)
        p.ellipsoid((ex, 0.0, ez), (0.033, 0.02, 0.033), "PeleSombra", "Head", seg=12, rings=8)

    # aparelho auditivo retroauricular na orelha direita (o lápis fica na esquerda): corpo atrás da
    # orelha, gancho por cima dela e a ponta (molde) entrando no ouvido. Verde-água, o mesmo do
    # adesivo da bolsa: é parte do estilo dele, não algo para esconder.
    p.ellipsoid((-0.174, 0.027, 1.464), (0.007, 0.009, 0.022), "Adesivo", "Head", seg=10, rings=6, rot=(-12, 0, 0))
    p.polyline([(-0.176, 0.026, 1.486), (-0.196, 0.012, 1.497), (-0.199, -0.004, 1.49), (-0.193, -0.008, 1.474)],
               0.0035, "Adesivo", "Head", seg=6)
    p.ellipsoid((-0.192, -0.007, 1.468), (0.006, 0.006, 0.007), "Olho", "Head", seg=8, rings=4)

    # rosto: olhos grandes, sobrancelhas grossas, nariz com curativo, sorriso aberto
    for s in (1, -1):
        p.ellipsoid((0.063 * s, -0.147, 1.467), (0.037, 0.016, 0.043), "Olho", "Head", seg=14, rings=8)
        p.ellipsoid((0.057 * s, -0.16, 1.46), (0.022, 0.01, 0.022), "Pupila", "Head", seg=12, rings=6)
        p.ellipsoid((0.05 * s, -0.169, 1.472), (0.007, 0.004, 0.007), "Olho", "Head", seg=6, rings=4)
    # sobrancelhas assimétricas: a esquerda arqueada lá em cima (cara de "hein?"), a direita reta.
    # Como ele não ouve, a expressão do rosto faz parte da conversa (Libras usa a sobrancelha).
    brows = {1: ((0.112, 1.523), (0.09, 1.546), (0.06, 1.554), (0.031, 1.538)),
             -1: ((-0.11, 1.52), (-0.085, 1.533), (-0.056, 1.535), (-0.03, 1.525))}
    for s, pts in brows.items():
        p.polyline([(x, face_y(x, z, 0.006), z) for x, z in pts], 0.013, "Cabelo", "Head", seg=6)
    # nariz com curativo em X (dois esparadrapos cruzados)
    p.ellipsoid((0, -0.166, 1.413), (0.02, 0.02, 0.02), "Pele", "Head", seg=10, rings=6)
    p.box((0.003, -0.177, 1.413), (0.085, 0.006, 0.024), "Papel", "Head", rot_y=18)
    p.box((0.0, -0.181, 1.413), (0.07, 0.006, 0.022), "Papel", "Head", rot_y=-22)
    # sorriso de canto (puxado para a esquerda dele)
    p.ellipsoid((0.01, -0.122, 1.357), (0.06, 0.02, 0.02), "Boca", "Head", seg=14, rings=6, rot=(0, -9, 0))
    return p.to_object(col, arm)


def armhole_angle(z):
    """Ângulo (graus, <= 0) da borda da frente da cava em z, no lado +X: anda da lateral para a
    frente até sair de perto do braço. 0 = abaixo da axila (o colete fecha na lateral)."""
    a, b = vest_a(z), vest_b(z)
    for deg in range(0, -76, -1):
        t = math.radians(deg)
        if not near_arm((a * math.cos(t), b * math.sin(t), z)):
            return float(deg)
    return -75.0


def rounded_rect(hw, hh, r, n=3):
    """Contorno (x, z) de um retângulo de cantos redondos, anti-horário, começando no lado +x."""
    pts = []
    for cx, cz, a0 in ((hw - r, hh - r, 0), (r - hw, hh - r, 90), (r - hw, r - hh, 180), (hw - r, r - hh, 270)):
        pts += [(cx + r * math.cos(math.radians(a0 + 90 * k / n)), cz + r * math.sin(math.radians(a0 + 90 * k / n)))
                for k in range(n + 1)]
    return pts


# Calça: cada perna é uma malha de anéis ao longo do eixo da perna (quadril, joelho, tornozelo), com
# raio que muda (coxa larga, afinando até o tornozelo, joelho marcado) e dobras: atrás do joelho e
# empilhadas em cima do tênis. Ângulo em volta da perna: 0 = lado de fora, 90 = frente, 270 = trás.
LEG_R = [(0.07, 0.05), (0.15, 0.047), (0.30, 0.046), (0.36, 0.049), (0.45, 0.052), (0.55, 0.057), (0.64, 0.06)]
LEG_KNEE = 0.357
PELVIS = ((0.0, 0.008, 0.6), (0.07, 0.055, 0.05))  # só fecha o gancho, onde as duas pernas se encontram


def leg_point(s, z, ang, lift=0.0):
    """Ponto na calça da perna do lado s (+1 esquerda, -1 direita) na altura z e no ângulo ang (graus)."""
    if z >= LEG_KNEE:
        cx = 0.061 + (0.078 - 0.061) * (0.64 - z) / (0.64 - LEG_KNEE)
    else:
        cx = 0.078 + (0.081 - 0.078) * (LEG_KNEE - z) / (LEG_KNEE - 0.07)
    a = math.radians(ang)
    r = lerp_table(LEG_R, z)
    if 0.31 < z < 0.40:  # dobras atrás do joelho (só na parte de trás)
        r += 0.004 * max(0.0, -math.sin(a)) * math.sin(math.tau * (z - 0.31) / 0.03) * math.sin(math.pi * (z - 0.31) / 0.09)
    if z < 0.14:         # pano empilhado em cima do tênis
        r += 0.0035 * abs(math.sin(math.tau * (z - 0.07) / 0.035))
    return Vector((s * (cx + math.cos(a) * (r + lift)), -math.sin(a) * (r * 0.95 + lift), z))


def surface_grid(p, rows, mat, bone, flip=False, closed=False):
    """Malha de quads a partir de linhas de pontos (rows[i][j]); flip inverte a frente das faces.
    closed: cada linha dá a volta (o último ponto liga no primeiro). Devolve os vértices."""
    bm = p.bm
    verts = [[bm.verts.new(bc.scaled(pt)) for pt in row] for row in rows]
    n = len(rows[0])
    for i in range(len(rows) - 1):
        for j in range(n if closed else n - 1):
            k = (j + 1) % n
            quad = (verts[i][j], verts[i][k], verts[i + 1][k], verts[i + 1][j])
            bm.faces.new(tuple(reversed(quad)) if flip else quad)
    vs = [v for row in verts for v in row]
    p._finish(vs, mat, bone, True)
    return vs


def leg_patch(p, s, a0, a1, z0, z1, mat, bone, puff=0.004, edge=0.0009, bottom=None, nu=6, nv=4,
              stitch="CalcaCostura", double=True, wobble=0.0, crease=False, tilt=0.0):
    """Peça costurada na perna (bolso, aba, remendo). Em vez de uma placa com moldura: a beirada
    encosta na perna (edge) e o meio estufa (puff), como pano com coisa dentro. O pesponto anda POR
    CIMA do pano seguindo a curva da perna (antes ligava os cantos em linha reta e afundava no pano,
    sobrando só os lados, como um colchete); double = pesponto duplo; wobble = pontos tortinhos de
    costura caseira. bottom(u) dá a altura da beirada de baixo (ponta em V, aba inclinada); crease
    marca a prega do meio (bolso sanfonado); tilt afasta a beirada de baixo (aba: presa em cima, solta
    embaixo, por cima do bolso). u e v vão de 0 a 1 (de a0 a a1, de baixo a z1)."""
    zb = bottom or (lambda u: z0)

    def pt(u, v, extra=0.0):
        z = zb(u) + (z1 - zb(u)) * v
        bump = puff * math.sin(math.pi * u) ** 0.7 * math.sin(math.pi * v) ** 0.7 + tilt * (1 - v)
        if crease:
            bump -= 0.6 * puff * math.exp(-((u - 0.5) / 0.05) ** 2) * math.sin(math.pi * v) ** 0.7
        return leg_point(s, z, a0 + (a1 - a0) * u, edge + bump + extra)

    surface_grid(p, [[pt(i / nu, j / nv) for i in range(nu + 1)] for j in range(nv + 1)], mat, bone, flip=s > 0)
    if not stitch:
        return
    for k, inset in enumerate((0.1, 0.17) if double else (0.12,)):
        corners = [(inset, inset), (1 - inset, inset), (1 - inset, 1 - inset), (inset, 1 - inset), (inset, inset)]
        path = []
        for (u0, v0), (u1, v1) in zip(corners, corners[1:]):
            for t in range(6):
                u, v = u0 + (u1 - u0) * t / 6, v0 + (v1 - v0) * t / 6
                jit = wobble * math.sin(37.0 * (u + 2 * v) + k)
                path.append(pt(u + jit, v + jit, 0.0012))
        sweep(p, path + [path[0]], 0.0011 if double else 0.0016, stitch, bone, seg=4)
    if crease:
        sweep(p, [pt(0.5, v / 4, 0.0004) for v in range(5)], 0.0012, "CalcaBarra", bone, seg=4)


def build_pants(p):
    """A calça: as pernas com forma e dobras (no alto elas se encontram no gancho, fechado por um
    pedacinho do quadril), a costura do lado de fora, bolsos cargo com aba e botão na coxa, bolsos de
    trás e um remendo no joelho esquerdo (onde ficava a joelheira: ele já levou tombo de bicicleta)."""
    (cx, cy, cz), (rx, ry, rz) = PELVIS
    p.ellipsoid((cx, cy, cz), (rx, ry, rz), "Calca", "Hips", seg=16, rings=8)
    angs = [i * 22.5 for i in range(16)]
    for s, side, bottom in ((1, "Left", 0.235), (-1, "Right", 0.07)):
        zs_ = [bottom + (0.14 - bottom) * i / 4 for i in range(4)] if bottom < 0.14 else [bottom]
        zs_ += [z for z in (0.17, 0.22, 0.27, 0.31, 0.325, 0.34, 0.355, 0.37, 0.385, 0.40, 0.45, 0.50, 0.55,
                            0.60, 0.64) if z > zs_[-1] + 0.005]
        lower = [z for z in zs_ if z <= LEG_KNEE + 0.02]
        upper = [z for z in zs_ if z >= lower[-1]]
        surface_grid(p, [[leg_point(s, z, a) for a in angs] for z in lower], "Calca", f"{side}LowerLeg",
                     flip=s > 0, closed=True)
        surface_grid(p, [[leg_point(s, z, a) for a in angs] for z in upper], "Calca", f"{side}UpperLeg",
                     flip=s > 0, closed=True)
        # costura do lado de fora, de cima a baixo (some embaixo do bolso cargo, que é costurado por cima).
        # Dividida no joelho, onde a calça também se divide: o trecho da canela vai com a canela. Inteira
        # na coxa, ao agachar ou correr ela ficava reta para a frente, fora da calça.
        knee = lower[-1]
        for part, bone in (([z for z in zs_ if z <= knee], f"{side}LowerLeg"),
                           ([z for z in zs_ if knee <= z < 0.385] + [0.385], f"{side}UpperLeg"),
                           ([0.497] + [z for z in zs_ if z > 0.497], f"{side}UpperLeg")):
            sweep(p, [leg_point(s, z, 0, 0.0012) for z in part], 0.0016, "CalcaCostura", bone, seg=4)
        # bolso cargo na coxa, puxado um pouco para a frente (aparece de frente), com prega no meio,
        # aba de ponta inclinada por cima e botão de pressão
        a0, a1 = -25, 70
        leg_patch(p, s, a0, a1, 0.385, 0.475, "Calca", f"{side}UpperLeg", puff=0.007, crease=True)
        leg_patch(p, s, a0 - 3, a1 + 3, 0.455, 0.495, "Calca", f"{side}UpperLeg", puff=0.0, tilt=0.0095, nv=2,
                  bottom=lambda u: 0.455 - 0.012 * u, double=False)
        p.ellipsoid(leg_point(s, 0.462, (a0 + a1) / 2, 0.0115), (0.005, 0.005, 0.005), "Metal", f"{side}UpperLeg",
                    seg=8, rings=4)
    # pesponto da barra da perna direita (logo acima da barra dobrada)
    sweep(p, [leg_point(-1, 0.112, a, 0.0012) for a in angs + [0]], 0.0013, "CalcaCostura", "RightLowerLeg", seg=4)
    # remendo no joelho esquerdo: costura caseira em volta, pontos grossos e tortinhos
    # (todo abaixo da divisão do joelho, em 0,37: se passasse dela, a ponta de cima sairia da calça ao dobrar)
    leg_patch(p, 1, 60, 120, 0.298, 0.366, "Remendo", "LeftLowerLeg", puff=0.0015, double=False, wobble=0.012)
    # bolsos de trás no assento: ponta em V embaixo, pesponto duplo e rebite nos cantos de cima
    for s, side in ((1, "Left"), (-1, "Right")):
        a0, a1 = 232, 302
        leg_patch(p, s, a0, a1, 0.515, 0.6, "Calca", f"{side}UpperLeg", puff=0.004,
                  bottom=lambda u: 0.515 - 0.02 * (1 - abs(2 * u - 1)))
        for a in (a0 + 4, a1 - 4):
            p.ellipsoid(leg_point(s, 0.594, a, 0.003), (0.003, 0.003, 0.003), "Metal", f"{side}UpperLeg",
                        seg=6, rings=4)

def build_bag(p):
    """Bolsa transversal pequena (náilon escuro, em pé): corpo macio de cantos redondos, bolso na
    metade de cima da frente com zíper, zíper principal por cima descendo pelos lados, vivo nas
    costuras da frente e de trás, passadores com argola nos cantos de cima (onde a alça prende).
    Detalhes do Tiago: puxadores com fitinha laranja, adesivo verde-água, um adesivo de fantasminha
    (o mesmo dos rabiscos dele), etiqueta laranja na costura do lado e um chaveiro de mosquetão."""
    bm, deg = p.bm, math.degrees(BAG_PHI)
    hw, hd, hh, r = BAG_W / 2, BAG_D / 2, BAG_H / 2, 0.022

    def soft_box(center, size, radius, mat, cuts):
        c = bag_pt(*center)
        faces = p.cloth_box(tuple(c), size, mat, "bag", cuts=cuts, radius=radius)
        bmesh.ops.rotate(bm, cent=c * bc.K, matrix=Matrix.Rotation(BAG_PHI, 3, "Z"),
                         verts=list({v for f in faces for v in f.verts}))

    soft_box((0, 0, 0), (BAG_W, BAG_D, BAG_H), r, "Bolsa", 4)
    soft_box((0, -hd - 0.005, 0.035), (0.13, 0.012, 0.08), 0.006, "BolsaDetalhe", 2)

    # vivo nas costuras da frente e de trás: no meio da quina arredondada
    inset = r * (1 - math.sqrt(0.5))
    for ly in (inset - hd, hd - inset):
        loop = [bag_pt(x, ly, z) for x, z in rounded_rect(hw - inset, hh - inset, r * math.sqrt(0.5))]
        sweep(p, loop + [loop[0]], 0.003, "Alca", "bag", seg=4)
    # zíper principal: por cima, descendo um pouco pelos lados; e o do bolso, na boca dele
    top = [(x, z) for x, z in rounded_rect(hw + 0.0015, hh + 0.0015, r + 0.0015, 4) if z > 0.04]
    sweep(p, [bag_pt(hw + 0.0015, 0, 0.04)] + [bag_pt(x, 0, z) for x, z in top] + [bag_pt(-hw - 0.0015, 0, 0.04)],
          0.0035, "Ziper", "bag", seg=4)
    sweep(p, [bag_pt(x, -hd - 0.0115, 0.071) for x in (-0.058, 0.0, 0.058)], 0.0025, "Ziper", "bag", seg=4)
    # puxadores (metal + fitinha laranja): o do zíper principal na ponta dele, no lado de fora;
    # o do bolso, pendurado na frente
    p.box(bag_pt(hw + 0.002, 0, 0.032), (0.003, 0.006, 0.012), "Metal", "bag", rot_z=deg)
    p.box(bag_pt(hw + 0.0035, 0, 0.019), (0.003, 0.008, 0.014), "Faixa", "bag", rot_z=deg)
    p.box(bag_pt(0.05, -hd - 0.013, 0.062), (0.006, 0.003, 0.012), "Metal", "bag", rot_z=deg)
    p.box(bag_pt(0.05, -hd - 0.0145, 0.049), (0.008, 0.003, 0.014), "Faixa", "bag", rot_z=deg)
    # passadores e argolas nos cantos de cima: a fita da alça termina no alto da argola
    for lx in (-0.065, 0.065):
        p.box(bag_pt(lx, 0, hh + 0.004), (0.022, 0.016, 0.014), "Alca", "bag", rot_z=deg)
        ring = [bag_pt(lx, 0.012 * math.cos(a), hh + 0.024 + 0.012 * math.sin(a))
                for a in (i * math.tau / 8 for i in range(9))]
        sweep(p, ring, 0.0022, "Metal", "bag", seg=4)
    # adesivos na metade de baixo da frente: o verde-água e o fantasminha (branco, olhos pretos)
    p.ellipsoid(bag_pt(0.035, -hd - 0.0005, -0.05), (0.014, 0.0015, 0.014), "Adesivo", "bag", seg=10, rings=4,
                rot=(0, 0, deg))
    p.ellipsoid(bag_pt(-0.03, -hd - 0.0005, -0.045), (0.011, 0.0015, 0.014), "Olho", "bag", seg=10, rings=4,
                rot=(0, 0, deg))
    for ex in (-0.034, -0.026):
        p.ellipsoid(bag_pt(ex, -hd - 0.0018, -0.042), (0.0018, 0.001, 0.0024), "Preto", "bag", seg=6, rings=3,
                    rot=(0, 0, deg))
    # etiqueta laranja na costura do lado de fora e o chaveiro (mosquetão + plaquinha) no canto
    p.box(bag_pt(hw + 0.001, -0.01, -0.03), (0.004, 0.012, 0.022), "Faixa", "bag", rot_z=deg)
    clip = [bag_pt(hw, 0.005 * math.cos(a), 0.088 + 0.012 * math.sin(a)) for a in (i * math.tau / 8 for i in range(9))]
    sweep(p, clip, 0.0018, "Metal", "bag", seg=4)
    p.ellipsoid(bag_pt(hw + 0.0015, 0, 0.068), (0.002, 0.009, 0.009), "Faixa", "bag", seg=8, rings=4,
                rot=(0, 0, deg))


def smooth_path(pts, sub=3):
    """Caminho suavizado (Catmull-Rom): sub pontos por trecho, passando pelos pontos dados."""
    pts = [Vector(q) for q in pts]
    out = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, len(pts) - 1)]
        for k in range(sub):
            t = k / sub
            out.append(0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (3 * p1 - p0 - 3 * p2 + p3) * t ** 3))
    return out + [pts[-1]]


def ribbon(p, pts, width, thick, mat, bone, sub=3):
    """Fita chata (alça de náilon): seção retangular deitada no corpo. A grossura aponta para fora,
    a partir do eixo do tronco (no ombro, para cima e para fora); a largura fica deitada no corpo."""
    pts = smooth_path(pts, sub)
    bm, rings, n = p.bm, [], len(pts)
    a, b = Vector((0, 0, 0.6)), Vector((0, 0, 1.0))
    for i, pt in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        s = max(0.0, min(1.0, (pt - a).dot(b - a) / (b - a).length_squared))
        out = pt - (a + (b - a) * s)
        out = (out - t * out.dot(t)).normalized()
        w = t.cross(out).normalized() * (width / 2)
        o = out * (thick / 2)
        rings.append([bm.verts.new(bc.scaled(q)) for q in (pt + w + o, pt + w - o, pt - w - o, pt - w + o)])
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(4):
            bm.faces.new((r0[k], r0[(k + 1) % 4], r1[(k + 1) % 4], r1[k]))
    p._finish([v for ring in rings for v in ring], mat, bone, True)


# Capuz abaixado. A borda (a abertura do rosto) contorna a gola por trás, apoiada nos ombros, de uma
# ponta na frente até a outra; o pano cai pelas costas, largo em cima e afinando até uma ponta
# redonda. hood_rim: ângulo (graus, 0 = lado +X, 90 = costas) -> ponto da borda.
HOOD_ENDS = (-72.0, 252.0)


def hood_rim(deg):
    a = math.radians(deg)
    ry = 0.13 if math.sin(a) > 0 else 0.10
    return (0.105 * math.cos(a), 0.01 + ry * math.sin(a), 1.17 + 0.035 * math.cos(a) ** 2)


HOOD_CORD = tuple(c + d for c, d in zip(hood_rim(HOOD_ENDS[0]), (0.0, -0.005, -0.008)))  # o cordão sai da ponta


def hood_point(s, t):
    """Ponto do pano do capuz: s de -1 a 1 (de um lado ao outro), t de 0 (embaixo da borda) a 1 (ponta).
    Sobre as costas do colete, com: um estufado que cresce para baixo (o pano embola perto da ponta),
    duas dobras suaves na diagonal, e uma folga extra em cima para o vivo do decote do colete não furar.
    Pano grosso: até nas beiradas ele fica 1,2 cm fora do colete, e o vivo de lá é largo (a dobra do
    tecido); o estufado chega a ~6 cm e fecha arredondado na ponta (some com t^6)."""
    z = 1.15 + (0.93 - 1.15) * t
    x = 0.105 * math.sqrt(max(1 - t ** 2.5, 0.0)) * s
    bulge = (0.03 + 0.035 * min(t, 0.85)) * (1 - s * s) ** 0.6 * (1 - t ** 6)
    fold = 0.014 * (math.exp(-((s - (0.55 - 0.9 * t)) / 0.22) ** 2)
                    + 0.7 * math.exp(-((s + 0.15 + 0.5 * t) / 0.2) ** 2)) * (1 - s * s) ** 0.3
    lift = 0.01 * max(0.0, 1 - t / 0.45)
    return Vector((x, -vest_y(x, z) + 0.012 + bulge + fold + lift, z))


def build_hood(p, cols=10):
    """O pano: a borda, a faixa do forro e as linhas até a ponta; colunas de um lado ao outro.
    O forro é uma meia-lua escura logo abaixo da borda, funda no meio e sumindo nas pontas, e um pouco
    afundada (é o lado de dentro aparecendo na abertura). Vivo nas beiradas e na ponta, costura no meio
    e a borda: um rolo mais gordo atrás, onde o pano acumula, afinando até as pontas da frente."""
    bm = p.bm
    ss = [-1 + 2 * j / cols for j in range(cols + 1)]
    ts = (0.12, 0.28, 0.44, 0.6, 0.75, 0.88, 1.0)
    rim = [Vector(hood_rim(90 - 60 * s)) for s in ss]
    lining_row = []
    for s, r in zip(ss, rim):
        pt = r + (hood_point(s, ts[0]) - r) * 0.75 * (1 - s * s)
        lining_row.append(pt - Vector((0, 0.012 * (1 - s * s), 0)))
    rows = [rim, lining_row] + [[hood_point(s, t) for s in ss] for t in ts]
    verts = [[bm.verts.new(bc.scaled(pt)) for pt in row] for row in rows]
    faces = []
    for i in range(len(rows) - 1):
        for j in range(cols):
            faces.append(bm.faces.new((verts[i][j], verts[i][j + 1], verts[i + 1][j + 1], verts[i + 1][j])))
    lining = faces[:cols]  # a faixa entre a borda e a linha do forro
    # pontas do forro (iguais à borda) e a ponta do capuz (a última linha é um ponto só)
    bmesh.ops.remove_doubles(bm, verts=verts[0] + verts[1] + verts[-1], dist=0.001 * bc.K)
    fs = [f for f in faces if f.is_valid]
    p._finish(list({v for f in fs for v in f.verts}), "MoletomEscuro", "hood_01", True)
    dark = p._index(p.mats, "CapuzDentro")
    for f in lining:
        if f.is_valid:
            f.material_index = dark

    on_rim = lambda v: min((v.co / bc.K - r).length for r in rim) < 1e-4  # noqa: E731
    edges = [e for f in fs for e in f.edges if len(e.link_faces) == 1 and not all(on_rim(v) for v in e.verts)]
    for chain, _ in boundary_chains(edges):
        sweep(p, chain, 0.009, "MoletomEscuro", "hood_01", seg=4)
    sweep(p, [hood_point(0, t) + Vector((0, 0.0015, 0)) for t in ts], 0.0022, "CapuzDentro", "hood_01", seg=4)

    # a borda: rolo em volta da gola, de uma ponta à outra, num tubo só
    n = 16
    sweep(p, [Vector(hood_rim(HOOD_ENDS[0] + (HOOD_ENDS[1] - HOOD_ENDS[0]) * i / n)) for i in range(n + 1)],
          [0.01 + 0.009 * math.sin(math.pi * i / n) for i in range(n + 1)], "MoletomEscuro", "UpperChest", seg=7)


def boundary_chains(edges, label=None):
    """Junta arestas de borda em cadeias (listas de pontos, sem o K): fechadas (cava) ou abertas.
    label(p0, p1): divide a cadeia onde o rótulo muda (ex.: zíper x vivo). Devolve (pontos, rótulo)."""
    nbr = {}
    for e in edges:
        a, b = e.verts
        nbr.setdefault(a, []).append(b)
        nbr.setdefault(b, []).append(a)
    seen, chains = set(), []
    for start in sorted(nbr, key=lambda v: len(nbr[v])):  # pontas (1 vizinho) primeiro
        if start in seen:
            continue
        chain, prev, cur = [start], None, start
        seen.add(start)
        while True:
            nxt = [v for v in nbr[cur] if v is not prev and (v not in seen or (v is start and len(chain) > 2))]
            if not nxt:
                break
            prev, cur = cur, nxt[0]
            chain.append(cur)
            if cur is start:
                break
            seen.add(cur)
        pts = [v.co / bc.K for v in chain]
        if label is None:
            chains.append((pts, None))
            continue
        run, run_label = [pts[0]], label(pts[0], pts[1])
        for a, b in zip(pts, pts[1:]):
            lab = label(a, b)
            if lab != run_label:
                chains.append((run, run_label))
                run, run_label = [a], lab
            run.append(b)
        chains.append((run, run_label))
    return chains


def sweep(p, pts, r, mat, bone, seg=6):
    """Tubo contínuo ao longo dos pontos (sem gomos nas emendas, ao contrário de tubos soltos): um anel
    em cada ponto, orientado pela direção do caminho e levado de um anel ao outro sem torcer.
    r: um raio só, ou uma lista com o raio de cada ponto (borda que afina nas pontas)."""
    if len(pts) < 2:
        return
    radii = list(r) if isinstance(r, (list, tuple)) else [r] * len(pts)
    closed = (pts[0] - pts[-1]).length < 1e-6
    if closed:
        pts, radii = pts[:-1], radii[:-1]
    bm, rings, normal = p.bm, [], None
    n = len(pts)
    for i, pt in enumerate(pts):
        r = radii[i]
        a = pts[(i - 1) % n] if (closed or i > 0) else pt
        b = pts[(i + 1) % n] if (closed or i < n - 1) else pt
        t = (b - a).normalized()
        normal = t.orthogonal().normalized() if normal is None else (normal - t * normal.dot(t)).normalized()
        binormal = t.cross(normal)
        rings.append([bm.verts.new(bc.scaled(pt + (normal * math.cos(k * math.tau / seg)
                                                   + binormal * math.sin(k * math.tau / seg)) * r))
                      for k in range(seg)])
    pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
    for r0, r1 in pairs:
        for k in range(seg):
            bm.faces.new((r0[k], r0[(k + 1) % seg], r1[(k + 1) % seg], r1[k]))
    p._finish([v for ring in rings for v in ring], mat, bone, True)


def build_vest(p, n=5):
    """Colete moldado no moletom: uma grade que segue a elipse do tronco (alturas VEST_ROWS), aberta
    na frente, e mais três linhas que sobem pelo ombro e se fecham em Y = 0 (a alça).

    As colunas de cada linha são distribuídas DENTRO de cada painel: frente (da abertura até a cava),
    costas (da cava até o meio), o meio das costas, e o espelho do outro lado. Assim a cava e o
    decote seguem a curva, sem escadinha. Abaixo da axila a cava tem largura zero e as duas pontas
    se emendam; sobre o ombro, os painéis afinam até a alça, que vai de x = 0,088 a 0,14 (decote em
    V na frente e nas costas). Faixa refletiva e logo das costas: faces da grade em laranja. Nas
    bordas, um vivo escuro dá espessura ao tecido (o colete é uma casca só)."""
    bm = p.bm
    z0 = VEST_ROWS[-1]
    a0, b0 = vest_a(z0), vest_b(z0)

    def front(m, z):
        """Coluna m (0 = abertura, n = cava) do painel da frente +X numa linha do corpo."""
        t = math.radians(-90 + VEST_OPEN + (armhole_angle(z) + 90 - VEST_OPEN) * m / n)
        return vest_a(z) * math.cos(t), vest_b(z) * math.sin(t)

    def strap(m, k):
        """Coluna m na linha k (1..3) sobre o ombro: o painel afinou até a alça."""
        x = 0.088 + (0.14 - 0.088) * m / n
        y = -b0 * math.sqrt(max(1 - (x / a0) ** 2, 0.0)) * (1 - k / 3)
        return x, y

    rows = []
    for k, z in enumerate(VEST_ROWS + [None] * 3):
        over = k - len(VEST_ROWS) + 1 if z is None else 0
        pts = []
        for m in range(n + 1):
            pts.append(strap(m, over) if over else front(m, z))
        if over:
            zs_ = [max(shoulder_z(x, y) + VEST_GAP, z0 + 0.01 * over) for x, y in pts]
        else:
            zs_ = [z] * len(pts)
        f_ = [(x, y, zz) for (x, y), zz in zip(pts, zs_)]                 # frente +X (abertura -> cava)
        b_ = [(x, -y, zz) for x, y, zz in reversed(f_)]                   # costas +X (cava -> meio)
        mid = (0.0, vest_b(z0 if over else z), z0 if over else z)         # meio das costas
        row = f_ + b_ + [mid] + [(-x, y, zz) for x, y, zz in reversed(b_)] + [(-x, y, zz) for x, y, zz in reversed(f_)]
        rows.append(row)
    # índices dos vãos a pular: a cava (entre frente e costas, dos dois lados) e, sobre o ombro, o meio
    gaps = {n, 3 * n + 3}
    middle = {2 * n + 1, 2 * n + 2}

    verts = [[bm.verts.new(bc.scaled(pt)) for pt in row] for row in rows]
    faces = []
    for i in range(len(rows) - 1):
        for j in range(len(rows[i]) - 1):
            if j in gaps or (i >= len(VEST_ROWS) - 1 and j in middle):
                continue
            quad = (rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j])
            c = tuple(sum(q[c_] for q in quad) / 4 for c_ in range(3))
            faces.append((bm.faces.new((verts[i][j], verts[i][j + 1], verts[i + 1][j + 1], verts[i + 1][j])), c))
    used = {v for f, _ in faces for v in f.verts}
    bmesh.ops.delete(bm, geom=[v for row in verts for v in row if v not in used], context="VERTS")
    # emenda: a cava fechada abaixo da axila e a alça no alto do ombro (frente com costas)
    bmesh.ops.remove_doubles(bm, verts=list(used), dist=0.002 * bc.K)

    fs = [f for f, _ in faces if f.is_valid]
    p._finish(list({v for f in fs for v in f.verts}), "Colete", "Chest", True)
    faixa = p._index(p.mats, "Faixa")
    for f, (cx, cy, cz) in faces:
        # logo das costas abaixo do capuz (mais alto, o capuz cobria metade)
        if f.is_valid and (ZS - 0.015 < cz < ZS + 0.015 or (cy > 0 and abs(cx) < 0.08 and 0.92 < cz < 1.04)):
            f.material_index = faixa
    # vivo nas bordas; na abertura da frente ele é o zíper (aberto: cada lado com sua fileira)
    def zipper(p0, p1):
        mx, my, mz = (p0 + p1) / 2
        return my < 0 and mz < z0 - 0.001 and abs(mx) < vest_a(mz) * math.sin(math.radians(VEST_OPEN)) + 0.004

    edges = [e for f in fs for e in f.edges if len(e.link_faces) == 1]
    for chain, is_zip in boundary_chains(edges, zipper):
        sweep(p, chain, 0.0045, "Ziper" if is_zip else "ColeteEscuro", "Chest", seg=4)
    # puxador do zíper, pendurado embaixo do lado esquerdo
    x = vest_a(0.69) * math.sin(math.radians(VEST_OPEN))
    p.box((x, vest_y(x, 0.69) - 0.006, 0.675), (0.009, 0.004, 0.026), "Metal", "Chest")


def lerp_table(table, t):
    """Interpola linearmente uma tabela [(t, valor), ...] em t."""
    for (t0, v0), (t1, v1) in zip(table, table[1:]):
        if t <= t1:
            return v0 + (v1 - v0) * (t - t0) / (t1 - t0)
    return table[-1][1]


# Tênis de quadra (cano baixo, solado de copo reto e grosso). ey vai de -1 (bico) a +1 (calcanhar).
# meio (no eixo da perna), comprimento para a frente e para trás (o calcanhar fica rente à perna)
SHOE_Y, SHOE_TOE, SHOE_HEEL, SHOE_N = -0.03, 0.13, 0.082, 2.6
SHOE_SOLE = 0.03                                      # altura do solado
SHOE_W = [(0, 0.05), (0.3, 0.052), (0.6, 0.045), (1, 0.046)]   # meia-largura da pegada
SHOE_H = [(0, 0.026), (0.25, 0.032), (0.45, 0.048), (0.65, 0.062), (0.85, 0.058), (1, 0.054)]  # altura do cabedal


def shoe_round(ey):
    """Quanto da largura sobra perto das pontas (superelipse): 1 no meio, 0 no bico e no calcanhar."""
    return (1 - abs(ey) ** SHOE_N) ** (1 / SHOE_N)


def shoe_y(ey, grow=0.0):
    """Y da seção ey: o bico vai 13 cm para a frente, o calcanhar só 8 cm para trás (rente à perna)."""
    return SHOE_Y + ey * ((SHOE_TOE if ey < 0 else SHOE_HEEL) + grow)


def shoe_point(xc, ey, phi, grow=0.0):
    """Ponto do cabedal: seção em ey, ângulo phi de 0 (lado +X) a pi (lado -X) passando por cima."""
    t, f = (ey + 1) / 2, shoe_round(ey)
    hw = max(lerp_table(SHOE_W, t) * f - 0.004, 0.004) + grow
    h = lerp_table(SHOE_H, t) * f ** (0.5 if ey < 0 else 0.25) + grow
    c, sn = math.cos(phi), math.sin(phi)
    ax = math.copysign(abs(c) ** (2 / 2.4), c)
    return (xc + hw * ax, shoe_y(ey), SHOE_SOLE - 0.002 + h * abs(sn) ** (2 / 2.4))


def shoe_outline(xc, grow, n=28):
    pts = []
    for i in range(n):
        a = math.tau * i / n
        c, sn = math.cos(a), math.sin(a)
        ex = math.copysign(abs(c) ** (2 / SHOE_N), c)
        ey = math.copysign(abs(sn) ** (2 / SHOE_N), sn)
        pts.append((xc + ex * (lerp_table(SHOE_W, (ey + 1) / 2) + grow), shoe_y(ey, grow)))
    return pts


def slab(p, outline, z0, z1, mat, bone):
    """Laje vertical com o contorno dado (anti-horário visto de cima): camadas do solado."""
    bm = p.bm
    lo = [bm.verts.new(bc.scaled((x, y, z0))) for x, y in outline]
    hi = [bm.verts.new(bc.scaled((x, y, z1))) for x, y in outline]
    caps = [bm.faces.new(list(reversed(lo))), bm.faces.new(hi)]
    for i in range(len(outline)):
        j = (i + 1) % len(outline)
        bm.faces.new((lo[i], lo[j], hi[j], hi[i]))
    p._finish(lo + hi, mat, bone, True)
    for f in caps:
        f.smooth = False


def seam(p, pts, mat, bone, r=0.0015):
    """Costura/linha fina: só tubos, sem as bolinhas das emendas do polyline (economiza triângulos)."""
    for a, b in zip(pts, pts[1:]):
        p.tube(a, b, r, r, mat, bone, seg=4, caps=False)


def build_sneaker(p, xc, foot):
    """Tênis branco de quadra: solado reto e grosso com friso, cabedal baixo com biqueira furadinha,
    costuras da biqueira e do calcanhar, cadarço branco e lingueta aparecendo na frente da canela."""
    slab(p, shoe_outline(xc, 0.002), 0.0, 0.012, "Sola", foot)
    slab(p, shoe_outline(xc, -0.001), 0.012, 0.016, "SolaSulco", foot)
    slab(p, shoe_outline(xc, 0.002), 0.016, SHOE_SOLE, "Sola", foot)

    # cabedal: seções em arco do bico ao calcanhar, fechadas nas pontas
    bm, rings = p.bm, []
    for k in range(13):
        ey = -0.95 * math.cos(math.pi * k / 12)
        rings.append([bm.verts.new(bc.scaled(shoe_point(xc, ey, math.pi * j / 10))) for j in range(11)])
    for a, b in zip(rings, rings[1:]):
        for j in range(10):
            bm.faces.new((a[j], b[j], b[j + 1], a[j + 1]))
    bm.faces.new(rings[0])
    bm.faces.new(list(reversed(rings[-1])))
    p._finish([v for r in rings for v in r], "Tenis", foot, True)

    def arc(ey, phi0, phi1, n=7, grow=0.0015):
        return [shoe_point(xc, ey, phi0 + (phi1 - phi0) * i / (n - 1), grow) for i in range(n)]

    half = math.pi / 2
    seam(p, arc(-0.6, 0.0, math.pi, n=9), "Costura", foot)                        # borda da biqueira
    seam(p, arc(0.55, 0.0, 0.9, n=4), "Costura", foot)                            # reforço do calcanhar
    seam(p, arc(0.55, math.pi - 0.9, math.pi, n=4), "Costura", foot)
    for d in (-0.62, 0.62):                                                        # beiradas dos ilhoses
        seam(p, [shoe_point(xc, ey, half + d, 0.0015) for ey in (-0.6, -0.4, -0.2, -0.05)], "Costura", foot)
    for ey in (-0.55, -0.45, -0.35, -0.25, -0.16):                                 # cadarço
        seam(p, arc(ey, half - 0.55, half + 0.55, n=3, grow=0.003), "Costura", foot, r=0.0028)
    for ey in (-0.82, -0.72):                                                      # furinhos da biqueira
        for d in (-0.5, -0.25, 0.0, 0.25, 0.5):
            p.box(shoe_point(xc, ey + (0.05 if abs(d) == 0.25 else 0), half + d, -0.0012),
                  (0.0035, 0.0035, 0.0035), "SolaSulco", foot)
    p.ellipsoid((xc, -0.046, 0.08), (0.024, 0.006, 0.019), "Tenis", foot, seg=10, rings=6, rot=(-15, 0, 0))


def face_y(x, z, lift):
    """Y da superfície da frente da cabeça (elipsoide) em (x, z), afastado `lift` para fora."""
    u = 1.0 - (x / HEAD_R.x) ** 2 - ((z - HEAD_C.z) / HEAD_R.z) ** 2
    return HEAD_C.y - HEAD_R.y * math.sqrt(max(u, 0.0)) - lift


def build_hair(arm, col):
    """Cabelo crespo só onde o boné não cobre: laterais e nuca."""
    p = bc.Part("SK_CHR_Tiago_Hair", MATS)
    p.ellipsoid((0, 0.045, 1.5), (0.166, 0.135, 0.16), "Cabelo", "Head", seg=20, rings=12)
    for x, y, r in ((78, 56, 14), (162, 56, 14), (86, 44, 12), (154, 44, 12), (74, 70, 11), (166, 70, 11)):
        ex, ez = d(x, y)
        p.ellipsoid((ex, 0.0, ez), (r / 300,) * 3, "Cabelo", "Head", seg=10, rings=6)
    for x, z, y in ((-0.09, 1.42, 0.13), (0.0, 1.4, 0.15), (0.09, 1.42, 0.13), (-0.06, 1.5, 0.15), (0.06, 1.5, 0.15)):
        p.ellipsoid((x, y, z), (0.045,) * 3, "Cabelo", "Head", seg=10, rings=6)
    return p.to_object(col, arm)


# Boné virado para trás: a FRENTE do boné (painel alto, logo, aba) fica em +Y, nas costas da cabeça,
# e a abertura de regulagem (snapback) na testa, em -Y. phi: ângulo em volta (90 = frente do boné),
# e: elevação (0 = beirada de baixo, 90 = topo).
CAP_SEAMS = [30 + 60 * k for k in range(6)]  # 6 gomos; costuras na frente, em 90, e na abertura, em 270


def cap_arch(phi):
    """Altura (graus de elevação) da abertura da regulagem em phi: um arco de 52° na testa."""
    dphi = (phi - 270 + 180) % 360 - 180
    return 30.0 * math.cos(math.pi / 2 * dphi / 26) ** 0.6 if abs(dphi) < 26 else 0.0


def cap_point(phi, e, lift=0.0):
    """Ponto da copa. A frente (+Y) sobe mais reta e firme (painel com entretela); o resto é redondo.
    Entre os gomos, um vinco suave na costura e o pano levemente estufado no meio do gomo."""
    f, er = math.radians(phi), math.radians(e)
    front = max(0.0, math.sin(f)) ** 2
    h = math.cos(er) ** (1 - 0.45 * front)              # parede mais em pé na frente
    d = min(abs((phi - sp + 180) % 360 - 180) for sp in CAP_SEAMS)
    gore = -0.004 * math.exp(-(d / 5) ** 2) + 0.0015 * math.sin(math.radians(d * 3)) ** 2
    gore *= math.sin(min(er * 2, math.pi / 2))          # some na beirada e no topo
    k = 1 + (gore + lift) / CAP_R[0]
    return Vector((CAP_R[0] * h * math.cos(f) * k, CAP_R[1] * h * math.sin(f) * k,
                   CAP_BASE - 0.003 + CAP_R[2] * (1 + 0.05 * front) * math.sin(er) * k))


def tilted_box(p, center, size, mat, bone, rx, rz):
    """Caixa girada em X (deita no plano do pano) e em Z (vira com a curva): bordado, regulagem."""
    faces = p.box(tuple(center), size, mat, bone)
    m = Matrix.Rotation(rz, 3, "Z") @ Matrix.Rotation(rx, 3, "X")
    bmesh.ops.rotate(p.bm, cent=Vector(center) * bc.K, matrix=m, verts=list({v for f in faces for v in f.verts}))


def build_cap(arm, col):
    """Boné da empresa virado para trás (malha separada, como pede a ficha): copa com a frente alta e
    os 6 gomos marcados, abertura em arco na testa com a tira de snapback, aba curvada (o "U") com
    espessura, pesponto em linhas e o verde por baixo, beirada desbotada, costuras contínuas, botão
    no topo, ilhoses e o caminhãozinho da transportadora bordado na frente."""
    p = bc.Part("SK_CHR_Tiago_Bone", MATS)
    bm = p.bm

    # copa: colunas em volta; a beirada de baixo segue o arco da abertura (sem recorte em escada)
    phis = [i * 7.5 for i in range(48)]
    ts = (0.0, 0.07, 0.15, 0.25, 0.37, 0.5, 0.63, 0.76, 0.87, 0.95)
    rows = [[cap_point(ph, cap_arch(ph) + (88 - cap_arch(ph)) * t) for ph in phis] for t in ts]
    verts = surface_grid(p, rows, "Bone", "Head", closed=True)
    vset = set(verts)
    edges = [e for v in verts for e in v.link_edges if len(e.link_faces) == 1 and all(x in vset for x in e.verts)]
    bottom = [e for e in set(edges) if (sum(v.co.z for v in e.verts) / 2) / bc.K < CAP_BASE + 0.06]
    for chain, _ in boundary_chains(bottom):
        sweep(p, chain, 0.0045, "BoneAba", "Head", seg=5)       # vivo da beirada (e do arco)
    p.ellipsoid(tuple(cap_point(0, 90)), (0.016, 0.016, 0.008), "BoneAba", "Head", seg=12, rings=4)

    # costuras dos gomos (contínuas), sem descer na abertura
    for sp in CAP_SEAMS:
        e0 = cap_arch(sp) + 4
        sweep(p, [cap_point(sp, e0 + (86 - e0) * i / 7, 0.0012) for i in range(8)], 0.0017, "BoneCostura",
              "Head", seg=4)
    for ph in (0, 120, 180, 240, 300):        # um ilhós no meio de cada gomo (no 60 fica o bordado)
        p.ellipsoid(tuple(cap_point(ph, 52, 0.001)), (0.0055,) * 3, "Metal", "Head", seg=8, rings=4)

    # tira do snapback na testa: plástico preto com os pininhos, fechando o pé da abertura
    for i in range(5):
        ph = 250 + 10 * i
        c = cap_point(ph, 3, 0.004)
        tilted_box(p, c, (0.03, 0.004, 0.014), "Preto", "Head", 0.0, math.radians(ph + 90))
        if 0 < i < 4:
            p.ellipsoid(tuple(cap_point(ph, 3, 0.007)), (0.003,) * 3, "Preto", "Head", seg=6, rings=3)

    # aba: da beirada da copa (phi 20..160) até a ponta; curvada nos lados, um pouco para baixo
    def brim(u, v, under=False):
        ph = 20 + 140 * u
        fp = math.radians(180 * u)
        base = cap_point(ph, 0, 0.002)
        tip = Vector((0.165 * math.cos(fp), 0.06 + 0.23 * math.sin(fp), base.z))
        q = base.lerp(tip, v)
        q.z -= (0.03 * (1 - math.sin(fp)) + 0.008) * v ** 1.2 + (0.005 if under else 0.0)
        return q

    nu, nv = 16, 5
    surface_grid(p, [[brim(i / nu, j / nv) for i in range(nu + 1)] for j in range(nv + 1)], "BoneAba", "Head",
                 flip=True)
    surface_grid(p, [[brim(i / nu, j / nv, True) for i in range(nu + 1)] for j in range(nv + 1)], "BoneAbaBaixo",
                 "Head")
    tip = [(brim(i / nu, 1) + brim(i / nu, 1, True)) / 2 for i in range(nu + 1)]
    sweep(p, tip, 0.0029, "BoneGasto", "Head", seg=5)            # beirada grossa e desbotada
    for v in (0.32, 0.52, 0.72, 0.88):                           # pesponto em linhas na aba
        sweep(p, [brim(0.04 + 0.92 * i / 20, v) + Vector((0, 0, 0.0011)) for i in range(21)], 0.0011,
              "BoneCostura", "Head", seg=4)

    # bordado na frente do boné (que fica para trás): o caminhãozinho da transportadora, no meio do
    # gomo da esquerda de quem olha (phi 60), sem pegar a costura do meio (phi 90)
    e, ph = 26, 60
    rz = math.radians(ph - 90)
    c0 = cap_point(ph, e, 0.0015)
    up = (cap_point(ph, e + 2, 0.0015) - cap_point(ph, e - 2, 0.0015)).normalized()
    up90 = Matrix.Rotation(-rz, 3, "Z") @ up                     # a inclinação, vista de frente do gomo
    rx = math.atan2(-up90.y, up90.z)
    side = Matrix.Rotation(rz, 3, "Z") @ Vector((-1, 0, 0))      # de quem olha as costas dele

    def logo(lx, lz):
        return c0 + side * lx + up * lz

    tilted_box(p, logo(0.006, 0.004), (0.034, 0.003, 0.02), "BoneLogo", "Head", rx, rz)     # baú
    tilted_box(p, logo(-0.018, 0.0), (0.013, 0.003, 0.013), "BoneLogo", "Head", rx, rz)     # cabine
    for lx in (-0.016, 0.014):                                                               # rodas
        p.ellipsoid(tuple(logo(lx, -0.009)), (0.004, 0.002, 0.004), "BoneLogo", "Head", seg=8, rings=4,
                    rot=(math.degrees(rx), 0, math.degrees(rz)))
    return p.to_object(col, arm)


def nb(lx, ly, lz):
    """Ponto do bloquinho no plano do bolso do peito: lx para o lado, ly para fora (negativo), lz a
    partir do meio do bloquinho (que sobra 4,5 cm para fora do bolso)."""
    return on_patch(CHEST_POCKET, lx, ly, 0.035 + lz)


def doodle(p, pts, r=0.0016):
    """Traço de lápis na capa do bloquinho: malha fina (cor chapada), não textura."""
    p.polyline([nb(x, -0.0166, z) for x, z in pts], r, "Grafite", "notebook", seg=4)


def build_notebook(col, socket):
    """Bloquinho guardado em pé no bolso do peito. Só a metade de cima aparece: a espiral e o
    rabisco da capa, um fantasma espiando por cima do bolso e um bonequinho fugindo dele."""
    p = bc.Part("SM_PRP_Bloquinho", MATS)
    deg = math.degrees(CHEST_POCKET[1])
    p.box(nb(0, -0.01, 0), (0.07, 0.012, 0.1), "Papel", "notebook", rot_z=deg)
    for x in (-0.02, 0.0, 0.02):
        p.tube(nb(x, -0.018, 0.051), nb(x, -0.002, 0.051), 0.004, 0.004, "Preto", "notebook", seg=6)
    # elástico laranja segurando as folhas (lado de fora da capa)
    p.box(nb(0.027, -0.01, 0), (0.006, 0.0135, 0.1005), "Faixa", "notebook", rot_z=deg)

    # fantasma: cabeça redonda, olhos e boca de susto (o corpo some dentro do bolso)
    head = [(-0.014 + 0.012 * math.cos(a), 0.022 + 0.016 * math.sin(a))
            for a in (math.pi * i / 10 for i in range(11))]
    doodle(p, [(-0.002, -0.01)] + head + [(-0.026, -0.01)])
    for x in (-0.019, -0.009):
        p.ellipsoid(nb(x, -0.0166, 0.024), (0.0028, 0.002, 0.0036), "Grafite", "notebook", seg=6, rings=4,
                    rot=(0, 0, deg))
    p.ellipsoid(nb(-0.014, -0.0166, 0.011), (0.0024, 0.002, 0.004), "Grafite", "notebook", seg=6, rings=4,
                rot=(0, 0, deg))
    # bonequinho correndo para fora, com risquinhos de velocidade atrás
    ring = [(0.014 + 0.0045 * math.cos(a), 0.031 + 0.0045 * math.sin(a))
            for a in (i * math.tau / 8 for i in range(9))]
    doodle(p, ring)
    doodle(p, [(0.013, 0.026), (0.011, 0.008)])
    doodle(p, [(0.011, 0.008), (0.018, -0.003)])
    doodle(p, [(0.011, 0.008), (0.005, -0.004)])
    doodle(p, [(0.004, 0.018), (0.012, 0.02), (0.021, 0.025)])
    for z in (0.026, 0.019):
        doodle(p, [(0.0, z), (0.004, z)], r=0.0012)
    return p.to_prop(col, socket, NOTEBOOK_GRIP)


def build_pencil(col, socket):
    """Lápis de seis faces, mais comprido e com a ponta apontada (madeira + grafite) para fora do
    boné: é a parte que aparece. A outra ponta fica escondida debaixo do boné."""
    p = bc.Part("SM_PRP_Lapis", MATS)
    a, b = Vector((0.127, 0.02, 1.627)), Vector((0.235, 0.02, 1.51))
    axis = (b - a).normalized()
    tip = b + axis * 0.024
    p.tube(a, b, 0.01, 0.01, "Lapis", "pencil", seg=6, smooth=False)
    p.tube(b, tip, 0.01, 0.0025, "Madeira", "pencil", seg=6, smooth=False)
    p.tube(tip - axis * 0.008, tip, 0.0034, 0.0012, "Grafite", "pencil", seg=6)
    return p.to_prop(col, socket, PENCIL_GRIP)


def main():
    scene, cols = bc.new_scene("Tiago")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    # UV em metros: o Unity põe o grão de tecido por cima da cor (ArtImportSettings, CharacterDetail)
    bc.metric_uvs(body, long_side_u=False)
    hair = build_hair(arm, cols["Geo"])
    cap = build_cap(arm, cols["Geo"])

    # os desenhos mostram lápis na orelha esquerda e bolsa no quadril esquerdo
    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.272, -0.01, 0.65))
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.272, -0.01, 0.65))
    neck = bc.add_socket("SOC_Neck", soc, arm, "notebook_lanyard", NOTEBOOK_GRIP)
    ear = bc.add_socket("SOC_Ear_L", soc, arm, "Head", PENCIL_GRIP)
    bc.add_socket("SOC_Hip_L", soc, arm, "bag", BAG)
    notebook = build_notebook(cols["Props"], neck)
    pencil = build_pencil(cols["Props"], ear)

    bc.report_tris((body, hair, cap, notebook, pencil))
    bc.finish(scene, cols, "Tiago", HEIGHT)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
