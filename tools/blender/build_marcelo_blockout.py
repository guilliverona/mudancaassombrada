"""Blockout do Marcelo "Marreta" (CHR_03) gerado a partir da ficha do Art Bible.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_marcelo_blockout.py

Saídas: Art/Source/Characters/Marcelo/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Characters/Marcelo/SK_CHR_Marcelo.fbx
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

# desenho da ficha: chão em y = 594, centro em x = 150. No desenho o topo da cabeça passa
# do gorro; aqui o gorro cobre a cabeça e é o topo dele que bate com a altura da ficha.
d = bc.mapper(150, 594)
HEIGHT = 1.98
BEANIE_BASE = d(0, 72)[1]
BEANIE_R = (0.178, 0.172, 0.165)
bc.set_scale(HEIGHT / (BEANIE_BASE + BEANIE_R[2]))

MATS = bc.Materials("MAT_CHR_Marcelo", {
    "Pele": ("#C68E6A", 0.0, 0.55, 0.0),
    "PeleSombra": ("#B47C5A", 0.0, 0.6, 0.0),
    "Barba": ("#3A2A22", 0.0, 0.85, 0.0),
    "Gorro": ("#9C4A2E", 0.0, 0.95, 0.0),
    "GorroDobra": ("#7E3A22", 0.0, 0.95, 0.0),
    "Macacao": ("#3F5A5E", 0.0, 0.85, 0.0),
    "MacacaoClaro": ("#4F6C70", 0.0, 0.85, 0.0),
    "Faixa": ("#F08A3C", 0.0, 0.5, 0.4),
    "Camiseta": ("#E9DFC8", 0.0, 0.9, 0.0),
    "Cinto": ("#2A2420", 0.0, 0.7, 0.0),
    "Metal": ("#D9B45A", 1.0, 0.3, 0.0),
    "Fita": ("#C9B89A", 0.0, 0.6, 0.0),
    "Furo": ("#131118", 0.0, 0.7, 0.0),
    "Cinta": ("#C86A28", 0.0, 0.7, 0.0),
    "Luva": ("#E0B23A", 0.0, 0.75, 0.0),
    "Velcro": ("#3A3442", 0.0, 0.8, 0.0),
    "Bota": ("#2A2420", 0.3, 0.6, 0.0),
    "Quadro": ("#F4F4F0", 0.0, 0.3, 0.0),
    "Moldura": ("#B9B4BE", 0.6, 0.4, 0.0),
    "Marcador": ("#E5687A", 0.0, 0.5, 0.0),
    "Olho": ("#2A1E18", 0.0, 0.2, 0.0),
})

ARMS = {
    "Left": ((0.06, 0, 1.36), (0.287, 0, 1.36), (0.38, -0.01, 1.087), (0.413, -0.02, 0.74), (0.413, -0.02, 0.6)),
    "Right": ((-0.06, 0, 1.36), (-0.287, 0, 1.36), (-0.38, -0.01, 1.087), (-0.413, -0.02, 0.74), (-0.413, -0.02, 0.6)),
}
LEGS = {side: ((x, 0, 0.713), (x, 0, 0.4), (x, 0, 0.093)) for side, x in (("Left", 0.103), ("Right", -0.103))}
HEAD_C = Vector((0.0, 0.0, d(0, 88)[1]))
HEAD_R = Vector((0.167, 0.16, 0.193))
BOARD_GRIP = (0.0, -0.222, 1.28)
WHISTLE = (0.137, -0.24, 1.29)


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.713), (0, 0, 0.85), None, False),
        ("Spine", (0, 0, 0.85), (0, 0, 1.0), "Hips", True),
        ("Chest", (0, 0, 1.0), (0, 0, 1.18), "Spine", True),
        ("UpperChest", (0, 0, 1.18), (0, 0, 1.36), "Chest", True),
        ("Neck", (0, -0.005, 1.40), (0, -0.01, 1.50), "UpperChest", False),
        ("Head", (0, -0.01, 1.50), (0, 0, 1.85), "Neck", True),
        ("beard_01", (0, -0.06, 1.63), (0, -0.13, 1.48), "Head", False),
        ("board_hinge", BOARD_GRIP, (0, -0.225, 1.01), "UpperChest", False),
        ("whistle_cord", (0.04, -0.12, 1.45), WHISTLE, "UpperChest", False),
        ("tape_roll", (0.22, -0.1, 0.72), (0.22, -0.12, 0.6), "Hips", False),
        ("belly_jiggle", (0, -0.06, 0.92), (0, -0.24, 0.92), "Spine", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.1, toe_fwd=0.17)
    return bc.build_armature("RIG_CHR_Marcelo", col, bones)


def build_body(arm, col):
    p = bc.Part("SK_CHR_Marcelo", MATS)

    # botas com bico de aço
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.103 * s
        p.ellipsoid((x, -0.03, 0.05), (0.085, 0.13, 0.05), "Bota", f"{side}Foot", seg=16, rings=8)
        p.box((x, -0.03, 0.01), (0.17, 0.26, 0.02), "Cinto", f"{side}Foot")
        p.polyline([(x - 0.035, -0.1, 0.085), (x + 0.035, -0.08, 0.09)], 0.006, "Velcro", f"{side}Foot", seg=6)

    # pernas do macacão: joelheira, faixa refletiva, barra virada
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.103 * s
        p.limb([(x, 0, 0.713), (x, 0, 0.4), (x, 0, 0.09)], (0.088, 0.08, 0.076), "Macacao",
               [f"{side}UpperLeg", f"{side}LowerLeg"])
        p.box((x, -0.077, 0.51), (0.13, 0.03, 0.105), "MacacaoClaro", f"{side}UpperLeg")
        p.tube((x, 0, 0.385), (x, 0, 0.415), 0.083, 0.083, "Faixa", f"{side}LowerLeg", seg=16, caps=False)
        p.tube((x, 0, 0.085), (x, 0, 0.147), 0.085, 0.084, "MacacaoClaro", f"{side}LowerLeg", seg=16)

    # tronco em bloco + barriga + ombros
    z0, z1 = d(0, 394)[1], d(0, 172)[1]
    p.tube((0, 0, z0), (0, 0, z1), 0.26, 0.313, "Macacao", "Chest", seg=28, squash_y=0.65)
    p.ellipsoid((0, 0, z1 - 0.03), (0.31, 0.2, 0.08), "Macacao", "UpperChest", seg=28, rings=8)
    p.ellipsoid((0, -0.06, 0.92), (0.25, 0.18, 0.24), "Macacao", "belly_jiggle", seg=24, rings=12)
    p.ellipsoid((0, -0.13, 1.39), (0.06, 0.02, 0.045), "Camiseta", "UpperChest", seg=12, rings=6)

    # faixa do peito, cinto com fivela, logo nas costas
    p.tube((0, 0, 0.955), (0, 0, 0.985), 0.292, 0.292, "Faixa", "Spine", seg=28, caps=False, squash_y=0.86)
    p.tube((0, 0, 0.687), (0, 0, 0.747), 0.28, 0.28, "Cinto", "Hips", seg=28, caps=False, squash_y=0.8)
    p.box((0, -0.226, 0.717), (0.067, 0.012, 0.06), "Metal", "Hips")
    p.box((0, 0.2, 1.22), (0.36, 0.01, 0.187), "Faixa", "Chest")

    # rolo de fita no quadril esquerdo e cinta de mudança no direito
    p.tube((0.22, -0.13, 0.66), (0.22, -0.08, 0.66), 0.057, 0.057, "Fita", "tape_roll", seg=16)
    p.tube((0.22, -0.134, 0.66), (0.22, -0.076, 0.66), 0.023, 0.023, "Furo", "tape_roll", seg=10)
    p.polyline([(-0.2, -0.2, 0.69), (-0.215, -0.21, 0.64), (-0.195, -0.2, 0.6),
                (-0.215, -0.205, 0.56), (-0.2, -0.195, 0.52)], 0.012, "Cinta", "Hips", seg=8)

    # alças do quadro presas aos ombros
    for s in (1, -1):
        p.polyline([(0.17 * s, -0.215, 1.267), (0.24 * s, -0.15, 1.38), (0.22 * s, 0.05, 1.41)],
                   0.012, "Cinto", "UpperChest", seg=8)

    # apito no cordão
    p.polyline([(0.04, -0.13, 1.45), (0.09, -0.2, 1.38), WHISTLE], 0.004, "Faixa", "whistle_cord", seg=6)
    p.box(WHISTLE, (0.07, 0.035, 0.04), "Faixa", "whistle_cord", rot_y=30)

    # braços: manga dobrada até o cotovelo, antebraço de fora
    for side, (_, sh, el, wr, _) in ARMS.items():
        sh, el, wr = Vector(sh), Vector(el), Vector(wr)
        p.limb([sh, el], (0.077, 0.072), "Macacao", [f"{side}UpperArm"])
        cuff = el + (wr - el) * 0.25
        p.tube(el, cuff, 0.076, 0.074, "MacacaoClaro", f"{side}LowerArm", seg=14)
        p.limb([cuff, wr], (0.067, 0.06), "Pele", [f"{side}LowerArm"])
        p.ellipsoid(wr, (0.06,) * 3, "Pele", f"{side}LowerArm", seg=12, rings=6)

    # pescoço, cabeça, orelhas, rosto
    p.tube((0, -0.005, 1.38), (0, -0.01, 1.54), 0.1, 0.095, "PeleSombra", "Neck", seg=16)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        ex, ez = d(150 + 50 * s, 92)
        p.ellipsoid((ex, 0.0, ez), (0.037, 0.022, 0.037), "PeleSombra", "Head", seg=12, rings=8)
        p.ellipsoid((0.06 * s, -0.152, 1.673), (0.018, 0.01, 0.018), "Olho", "Head", seg=10, rings=6)
        p.polyline([(0.1 * s, -0.148, 1.717), (0.06 * s, -0.162, 1.722), (0.027 * s, -0.165, 1.71)],
                   0.012, "Barba", "Head", seg=6)
    p.ellipsoid((0, -0.165, 1.633), (0.033, 0.035, 0.03), "PeleSombra", "Head", seg=12, rings=8)
    return p.to_object(col, arm)


def build_beard(arm, col):
    """Barba como casca separada (a boca continua existindo por baixo)."""
    p = bc.Part("SK_CHR_Marcelo_Barba", MATS)
    p.ellipsoid((0, -0.06, 1.56), (0.168, 0.135, 0.105), "Barba", "beard_01", seg=24, rings=12)
    p.polyline([(-0.04, -0.192, 1.553), (0.0, -0.199, 1.54), (0.04, -0.192, 1.553)], 0.008, "Pele", "beard_01", seg=6)
    return p.to_object(col, arm)


def build_beanie(arm, col):
    p = bc.Part("SK_CHR_Marcelo_Gorro", MATS)
    dome = p.ellipsoid((0, 0, BEANIE_BASE), BEANIE_R, "Gorro", "Head", seg=24, rings=12)
    p.cut_below(dome, BEANIE_BASE - 0.005)
    p.tube((0, 0, d(0, 80)[1]), (0, 0, d(0, 60)[1]), 0.183, 0.18, "GorroDobra", "Head", seg=24, caps=False, squash_y=0.97)
    return p.to_object(col, arm)


def build_gloves(arm, col):
    """Luvas de carga: casca separada para trocar por mão nua nas cutscenes."""
    p = bc.Part("SK_CHR_Marcelo_Luvas", MATS)
    for side, (_, _, el, wr, _) in ARMS.items():
        wr, el = Vector(wr), Vector(el)
        dirv = (wr - el).normalized()
        p.tube(wr - dirv * 0.012, wr + dirv * 0.022, 0.07, 0.07, "Velcro", f"{side}Hand", seg=14)
        p.ellipsoid(wr + dirv * 0.075, (0.088, 0.07, 0.095), "Luva", f"{side}Hand", seg=16, rings=10)
    return p.to_object(col, arm)


def build_board(col, socket):
    p = bc.Part("SM_PRP_Quadro", MATS)
    p.box((0, -0.212, 1.147), (0.393, 0.02, 0.287), "Moldura", "board")
    p.box((0, -0.223, 1.147), (0.373, 0.006, 0.267), "Quadro", "board")
    p.box((0.2, -0.225, 1.18), (0.02, 0.02, 0.073), "Marcador", "board")
    return p.to_prop(col, socket, BOARD_GRIP)


def main():
    scene, cols = bc.new_scene("Marcelo")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    beard = build_beard(arm, cols["Geo"])
    beanie = build_beanie(arm, cols["Geo"])
    gloves = build_gloves(arm, cols["Geo"])

    soc = cols["Sockets"]
    for side, (_, _, el, wr, _) in ARMS.items():
        palm = Vector(wr) + (Vector(wr) - Vector(el)).normalized() * 0.075
        bc.add_socket(f"SOC_Hand_{side[0]}", soc, arm, f"{side}Hand", palm)
        bc.add_socket(f"SOC_Carry_{'A' if side == 'Left' else 'B'}", soc, arm, f"{side}Hand", palm + Vector((0, -0.07, 0)))
    chest = bc.add_socket("SOC_Chest", soc, arm, "board_hinge", BOARD_GRIP)
    bc.add_socket("SOC_Mouth", soc, arm, "Head", (0, -0.2, 1.553))
    board = build_board(cols["Props"], chest)

    bc.report_tris((body, beard, beanie, gloves, board))
    bc.finish(scene, cols, "Marcelo", HEIGHT)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
