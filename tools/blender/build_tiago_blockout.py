"""Blockout do Tiago (CHR_02) gerado a partir da ficha do Art Bible.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_tiago_blockout.py

Saídas: Art/Source/Characters/Tiago/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Characters/Tiago/SK_CHR_Tiago.fbx
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

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
    "Bone": ("#F08A3C", 0.0, 0.7, 0.0),
    "BoneAba": ("#C86A28", 0.0, 0.7, 0.0),
    "Moletom": ("#6E7F72", 0.0, 0.85, 0.0),
    "MoletomEscuro": ("#5C6B60", 0.0, 0.85, 0.0),
    "Colete": ("#3F5A5E", 0.0, 0.8, 0.0),
    "Faixa": ("#F08A3C", 0.0, 0.5, 0.3),
    "Calca": ("#2B3348", 0.0, 0.85, 0.0),
    "CalcaBarra": ("#3B4560", 0.0, 0.85, 0.0),
    "Bolsa": ("#9C4A2E", 0.0, 0.7, 0.0),
    "BolsaTampa": ("#7E3A22", 0.0, 0.7, 0.0),
    "Tenis": ("#EDEBE4", 0.0, 0.6, 0.0),
    "Joelheira": ("#1F1D24", 0.0, 0.6, 0.0),
    "Papel": ("#E9DFC8", 0.0, 0.9, 0.0),
    "Olho": ("#F4F0E8", 0.0, 0.3, 0.0),
    "Pupila": ("#2A1E18", 0.0, 0.2, 0.0),
    "Boca": ("#5A2A2A", 0.0, 0.6, 0.0),
    "Lapis": ("#E8C547", 0.0, 0.6, 0.0),
    "Grafite": ("#3A2A20", 0.0, 0.6, 0.0),
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
NOTEBOOK_GRIP = (0.0, -0.118, 0.99)
PENCIL_GRIP = (0.175, 0.02, 1.577)
BAG = (0.205, 0.02, 0.66)


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.64), (0, 0, 0.76), None, False),
        ("Spine", (0, 0, 0.76), (0, 0, 0.90), "Hips", True),
        ("Chest", (0, 0, 0.90), (0, 0, 1.02), "Spine", True),
        ("UpperChest", (0, 0, 1.02), (0, 0, 1.15), "Chest", True),
        ("Neck", (0, -0.005, 1.15), (0, -0.01, 1.27), "UpperChest", False),
        ("Head", (0, -0.01, 1.27), (0, -0.01, 1.66), "Neck", True),
        ("hood_01", (0, 0.08, 1.18), (0, 0.11, 1.05), "UpperChest", False),
        ("notebook_lanyard", (0, -0.07, 1.2), NOTEBOOK_GRIP, "UpperChest", False),
        ("bag", (0.19, 0.02, 0.76), (0.205, 0.02, 0.6), "Hips", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.08, toe_fwd=0.14)
    arm = bc.build_armature("RIG_CHR_Tiago", col, bones)
    bc.add_finger_bones(arm, ARMS, {side: HAND for side in ARMS})
    return arm


def build_body(arm, col):
    p = bc.Part("SK_CHR_Tiago", MATS)

    # tênis de sola grossa com faixa laranja
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.08 * s
        p.ellipsoid((x, -0.03, 0.033), (0.073, 0.11, 0.035), "Tenis", f"{side}Foot", seg=16, rings=8)
        p.box((x + 0.07 * s, -0.03, 0.035), (0.008, 0.12, 0.02), "Faixa", f"{side}Foot")
        p.polyline([(x - 0.03, -0.09, 0.06), (x + 0.03, -0.07, 0.065)], 0.005, "Preto", f"{side}Foot", seg=6)

    # perna direita com calça inteira; perna esquerda arregaçada (assimetria da ficha)
    p.limb([(-0.072, 0, 0.64), (-0.078, 0, 0.357), (-0.08, 0, 0.07)], (0.047, 0.045, 0.048),
           "Calca", ["RightUpperLeg", "RightLowerLeg"])
    p.limb([(0.072, 0, 0.64), (0.078, 0, 0.357), (0.081, 0, 0.235)], (0.047, 0.045, 0.046),
           "Calca", ["LeftUpperLeg", "LeftLowerLeg"])
    p.tube((0.081, 0, 0.212), (0.081, 0, 0.252), 0.056, 0.054, "CalcaBarra", "LeftLowerLeg", seg=12)
    p.tube((0.081, 0, 0.215), (0.081, 0, 0.11), 0.036, 0.032, "Pele", "LeftLowerLeg", seg=10)
    p.tube((0.081, 0, 0.115), (0.08, 0, 0.06), 0.04, 0.04, "Tenis", "LeftLowerLeg", seg=10)

    # joelheiras
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.077 * s
        p.box((x, -0.04, 0.377), (0.107, 0.05, 0.11), "Joelheira", f"{side}LowerLeg")
        p.ellipsoid((x, -0.067, 0.377), (0.012, 0.006, 0.012), "Faixa", f"{side}LowerLeg", seg=8, rings=4)

    # moletom (silhueta em V), barra, ombros
    z_hem, z_top = d(0, 338)[1], d(0, 176)[1]
    p.tube((0, 0, z_hem), (0, 0, z_top), 0.135, 0.18, "Moletom", "Chest", seg=24, squash_y=0.6)
    p.tube((0, 0, z_hem - 0.005), (0, 0, z_hem + 0.045), 0.142, 0.143, "MoletomEscuro", "Hips", seg=24, squash_y=0.62)
    p.ellipsoid((0, 0, z_top - 0.02), (0.18, 0.105, 0.05), "Moletom", "UpperChest", seg=24, rings=8)

    # colete aberto na frente + faixa refletiva + logo nas costas
    vest = p.tube((0, 0, 0.64), (0, 0, z_top + 0.002), 0.141, 0.187, "Colete", "Chest",
                  seg=24, caps=False, squash_y=0.63)
    p.cut_front_opening(vest, 0.035)
    zs = d(0, 266)[1]
    stripe = p.tube((0, 0, zs - 0.015), (0, 0, zs + 0.015), 0.168, 0.169, "Faixa", "Chest",
                    seg=24, caps=False, squash_y=0.645)
    p.cut_front_opening(stripe, 0.035)
    p.box((0, 0.11, d(0, 215)[1]), (0.187, 0.008, 0.1), "Faixa", "Chest")

    # capuz, gola, cordões, cordão do bloquinho
    # gola alta do moletom cobre o pescoço, como na ficha; capuz apoiado nas costas
    p.tube((0, 0, z_top - 0.01), (0, -0.005, z_top + 0.1), 0.1, 0.075, "MoletomEscuro", "UpperChest", seg=16)
    p.ellipsoid((0, 0.075, z_top + 0.03), (0.12, 0.075, 0.075), "MoletomEscuro", "hood_01", seg=16, rings=8)
    for s in (1, -1):
        p.polyline([(0.027 * s, -0.1, 1.15), (0.033 * s, -0.112, 1.04)], 0.004, "Olho", "Chest", seg=6)
        p.ellipsoid((0.033 * s, -0.113, 1.03), (0.006, 0.006, 0.012), "Faixa", "Chest", seg=6, rings=4)
        p.polyline([(0.04 * s, -0.07, 1.2), (0.015 * s, -0.11, 1.08), NOTEBOOK_GRIP], 0.004, "Faixa",
                   "notebook_lanyard", seg=6)

    # alça transversal da bolsa (frente e costas) e a bolsa no quadril esquerdo
    strap_front = [(-0.14, 0.0, 1.17), (-0.12, -0.112, 1.1), (0.0, -0.122, 0.92), (0.14, -0.1, 0.7), (0.19, 0.0, 0.69)]
    strap_back = [(-0.14, 0.0, 1.17), (-0.12, 0.105, 1.1), (0.0, 0.11, 0.92), (0.14, 0.1, 0.72), (0.19, 0.04, 0.69)]
    p.polyline(strap_front, 0.014, "Bolsa", "Chest", seg=8)
    p.polyline(strap_back, 0.014, "Bolsa", "Chest", seg=8)
    p.box(BAG, (0.07, 0.16, 0.14), "Bolsa", "bag")
    p.box((BAG[0] + 0.004, BAG[1], BAG[2] + 0.045), (0.075, 0.166, 0.05), "BolsaTampa", "bag")
    p.ellipsoid((BAG[0] + 0.038, -0.02, 0.63), (0.004, 0.017, 0.017), "Adesivo", "bag", seg=10, rings=6)
    p.box((BAG[0] + 0.038, 0.05, 0.625), (0.004, 0.047, 0.03), "Faixa", "bag")
    p.box((BAG[0] + 0.038, 0.02, 0.68), (0.006, 0.033, 0.027), "Metal", "bag")

    # braços em A-pose, munhequeiras, mãos
    for side, (_, sh, el, wr, _) in ARMS.items():
        p.limb([sh, el, wr], (0.038, 0.036, 0.034), "Moletom", [f"{side}UpperArm", f"{side}LowerArm"])
        wr, el = Vector(wr), Vector(el)
        dirv = (wr - el).normalized()
        band = "Faixa" if side == "Right" else "Colete"
        p.tube(wr - dirv * 0.03, wr - dirv * 0.002, 0.039, 0.039, band, f"{side}LowerArm", seg=12)
        p.hand(side, ARMS[side], HAND, "Pele", nail="Unha")

    # pescoço, cabeça, orelhas
    p.tube((0, -0.005, 1.14), (0, -0.01, 1.28), 0.04, 0.038, "PeleSombra", "Neck", seg=12)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        ex, ez = d(120 + 47 * s, 82)
        p.ellipsoid((ex, 0.0, ez), (0.033, 0.02, 0.033), "PeleSombra", "Head", seg=12, rings=8)

    # rosto: olhos grandes, sobrancelhas grossas, nariz com curativo, sorriso aberto
    for s in (1, -1):
        p.ellipsoid((0.063 * s, -0.147, 1.467), (0.037, 0.016, 0.043), "Olho", "Head", seg=14, rings=8)
        p.ellipsoid((0.057 * s, -0.16, 1.46), (0.022, 0.01, 0.022), "Pupila", "Head", seg=12, rings=6)
        p.ellipsoid((0.05 * s, -0.169, 1.472), (0.007, 0.004, 0.007), "Olho", "Head", seg=6, rings=4)
        p.polyline([(0.107 * s, -0.142, 1.527), (0.07 * s, -0.155, 1.53), (0.033 * s, -0.16, 1.52)],
                   0.011, "Cabelo", "Head", seg=6)
    p.ellipsoid((0, -0.166, 1.413), (0.02, 0.02, 0.02), "Pele", "Head", seg=10, rings=6)
    p.box((0.003, -0.176, 1.412), (0.073, 0.006, 0.023), "Papel", "Head", rot_y=12)
    p.ellipsoid((0.004, -0.122, 1.355), (0.06, 0.02, 0.02), "Boca", "Head", seg=14, rings=6)
    return p.to_object(col, arm)


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


def build_cap(arm, col):
    """Boné da empresa virado para trás (malha separada, como pede a ficha)."""
    p = bc.Part("SK_CHR_Tiago_Bone", MATS)
    z_base = CAP_BASE
    dome = p.ellipsoid((0, 0.0, z_base), CAP_R, "Bone", "Head", seg=24, rings=12)
    p.cut_below(dome, z_base - 0.003)
    p.tube((0, 0, z_base - 0.008), (0, 0, z_base + 0.012), 0.172, 0.171, "BoneAba", "Head", seg=24, caps=False)
    p.ellipsoid((0, 0.2, z_base - 0.004), (0.085, 0.09, 0.012), "BoneAba", "Head", seg=16, rings=6)
    p.box((0, -0.158, z_base + 0.035), (0.067, 0.01, 0.025), "Preto", "Head")
    return p.to_object(col, arm)


def build_notebook(col, socket):
    p = bc.Part("SM_PRP_Bloquinho", MATS)
    p.box((0, -0.122, 0.93), (0.093, 0.014, 0.113), "Papel", "notebook")
    for i in range(4):
        x = -0.03 + i * 0.02
        p.tube((x, -0.13, 0.99), (x, -0.114, 0.99), 0.004, 0.004, "Preto", "notebook", seg=6)
    return p.to_prop(col, socket, NOTEBOOK_GRIP)


def build_pencil(col, socket):
    p = bc.Part("SM_PRP_Lapis", MATS)
    a, b = Vector((0.127, 0.02, 1.627)), Vector((0.22, 0.02, 1.527))
    tip = b + (b - a).normalized() * 0.018
    p.tube(a, b, 0.01, 0.01, "Lapis", "pencil", seg=8)
    p.tube(b, tip, 0.01, 0.002, "Grafite", "pencil", seg=8)
    return p.to_prop(col, socket, PENCIL_GRIP)


def main():
    scene, cols = bc.new_scene("Tiago")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
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
