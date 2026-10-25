"""Blockout do Seu Valdemar, o chefe (BOS_00): fala por bilhetes e por um rádio chiado.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_valdemar_blockout.py

Saídas: Art/Source/Bosses/Valdemar/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Bosses/Valdemar/SK_BOS_Valdemar.fbx

Ficha: 1,68 m (curvado) · forma "curva caída" · avô rabugento: chapéu velho com fita laranja,
bigodão que esconde a boca, óculos redondos, cachecol de papel cobrindo a garganta (perdeu a voz),
terno largo sépia, gravata laranja (o único ponto saturado nele), bolsos cheios de bilhetes e o
rádio no quadril. Mesma malha para todos os estados (_Solidity no shader).
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

HEIGHT = 1.68

MATS = bc.Materials("MAT_BOS_Valdemar", {
    "Terno": ("#5A4E44", 0.0, 0.9, 0.0),
    "TernoEscuro": ("#463C34", 0.0, 0.9, 0.0),
    "Gravata": ("#F08A3C", 0.0, 0.6, 0.0),
    "Papel": ("#E9DFC8", 0.0, 0.9, 0.0),
    "Pele": ("#D8C6B4", 0.0, 0.6, 0.0),
    "Bigode": ("#EDE6D6", 0.0, 0.9, 0.0),
    "Chapeu": ("#3A3029", 0.0, 0.85, 0.0),
    "Sapato": ("#2A2420", 0.0, 0.5, 0.0),
    "Aro": ("#8E8794", 0.8, 0.4, 0.0),
    "Lente": ("#E9DFC8", 0.0, 0.2, 0.0),
    "Olho": ("#2B2733", 0.0, 0.8, 0.0),
    "Madeira": ("#6E3A2A", 0.0, 0.6, 0.0),
    "Mostrador": ("#FFC98A", 0.0, 0.3, 1.5),
    "Lapis": ("#E0B23A", 0.0, 0.6, 0.0),
})

ARMS = {
    "Left": ((0.05, 0, 1.3), (0.19, 0, 1.29), (0.25, 0, 1.02), (0.28, 0, 0.78), (0.29, 0, 0.66)),
    "Right": ((-0.05, 0, 1.3), (-0.19, 0, 1.29), (-0.25, 0, 1.02), (-0.28, 0, 0.78), (-0.29, 0, 0.66)),
}
LEGS = {side: ((0.09 * s, 0, 0.72), (0.095 * s, 0, 0.38), (0.1 * s, 0, 0.07)) for side, s in (("Left", 1), ("Right", -1))}
HEAD_C = Vector((0, -0.02, 1.43))


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.72), (0, 0, 0.88), None, False),
        ("Spine", (0, 0, 0.88), (0, 0, 1.04), "Hips", True),
        ("Chest", (0, 0, 1.04), (0, 0, 1.18), "Spine", True),
        ("UpperChest", (0, 0, 1.18), (0, 0, 1.31), "Chest", True),
        ("Neck", (0, -0.01, 1.32), (0, -0.02, 1.37), "UpperChest", False),
        ("Head", (0, -0.02, 1.37), (0, -0.02, 1.62), "Neck", True),
        ("hat", (0, -0.02, 1.56), (0, -0.02, 1.68), "Head", False),       # "tirar o chapéu"
        ("mustache_L", (0.01, -0.12, 1.38), (0.07, -0.12, 1.36), "Head", False),
        ("mustache_R", (-0.01, -0.12, 1.38), (-0.07, -0.12, 1.36), "Head", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.09, toe_fwd=0.14)
    return bc.build_armature("RIG_BOS_Valdemar", col, bones)


def build_body(arm, col):
    p = bc.Part("SK_BOS_Valdemar", MATS)

    # sapatos e pernas de calça larga (no jogo, as pernas somem em papel picado)
    for side, s in (("Left", 1), ("Right", -1)):
        x = 0.1 * s
        p.ellipsoid((x, -0.05, 0.04), (0.055, 0.12, 0.045), "Sapato", f"{side}Foot", seg=14, rings=8)
        p.limb([(0.09 * s, 0, 0.72), (0.095 * s, 0, 0.38), (0.1 * s, 0, 0.08)], (0.075, 0.065, 0.06),
               "Terno", [f"{side}UpperLeg", f"{side}LowerLeg"], seg=12)

    # paletó largo e caído (a "curva caída"): mais largo embaixo, ombros que despencam
    p.tube((0, 0.0, 0.62), (0, 0.0, 1.27), 0.22, 0.18, "Terno", "Spine", seg=22, squash_y=0.75)
    p.ellipsoid((0, 0.0, 1.27), (0.2, 0.13, 0.06), "Terno", "UpperChest", seg=20, rings=8)
    # camisa, gravata laranja e lapelas
    p.box((0, -0.135, 1.13), (0.1, 0.01, 0.24), "Papel", "Chest")
    p.box((0, -0.143, 1.06), (0.045, 0.01, 0.22), "Gravata", "Chest")
    p.ellipsoid((0, -0.145, 1.19), (0.025, 0.012, 0.02), "Gravata", "Chest", seg=8, rings=5)  # nó
    for s in (1, -1):
        p.box((0.075 * s, -0.142, 1.1), (0.05, 0.012, 0.3), "TernoEscuro", "Chest", rot_y=-14 * s)
    for z in (0.92, 0.8):
        p.ellipsoid((0.0, -0.162, z), (0.012, 0.006, 0.012), "Chapeu", "Spine", seg=8, rings=5)  # botões
    # bolsos cheios de bilhetes
    for (x, z, rot) in ((0.12, 0.82, 8), (-0.13, 0.8, -6), (0.1, 1.12, -10)):
        p.box((x, -0.16, z), (0.1, 0.012, 0.06), "TernoEscuro", "Spine")
        p.box((x + 0.01, -0.158, z + 0.05), (0.07, 0.006, 0.07), "Papel", "Spine", rot_y=rot)

    # rádio no quadril esquerdo
    p.box((0.25, -0.02, 0.78), (0.06, 0.12, 0.09), "Madeira", "Hips")
    p.ellipsoid((0.282, -0.04, 0.79), (0.006, 0.02, 0.02), "Mostrador", "Hips", seg=8, rings=5)

    # braços em mangas largas, mãos de idoso; lápis na mão direita
    for side, (_, sh, el, wr, _) in ARMS.items():
        s = 1 if side == "Left" else -1
        p.limb([sh, el, wr], (0.06, 0.055, 0.05), "Terno", [f"{side}UpperArm", f"{side}LowerArm"], seg=12)
        p.tube(Vector(wr) + Vector((0, 0, 0.02)), Vector(wr) + Vector((0, 0, -0.01)), 0.045, 0.045,
               "Papel", f"{side}LowerArm", seg=12, caps=False)                       # punho da camisa
        palm = Vector(wr) + Vector((0, 0, -0.05))
        p.ellipsoid(palm, (0.035, 0.02, 0.045), "Pele", f"{side}Hand", seg=12, rings=8)
        for i, dx in enumerate((-0.022, -0.007, 0.008, 0.022)):
            top = palm + Vector((dx * s, 0, -0.035))
            p.polyline([top, top + Vector((0, -0.01, -(0.06, 0.07, 0.065, 0.05)[i]))], 0.008, "Pele",
                       f"{side}Hand", seg=6)
        if s < 0:
            p.tube(palm + Vector((0.0, -0.03, 0.02)), palm + Vector((0.02, -0.06, -0.1)), 0.005, 0.004,
                   "Lapis", "RightHand", seg=6)

    # cachecol de papel cobrindo a garganta (ele perdeu a voz)
    p.tube((0, -0.01, 1.28), (0, -0.015, 1.36), 0.075, 0.07, "Papel", "Neck", seg=16)
    p.box((0.04, -0.09, 1.2), (0.05, 0.012, 0.14), "Papel", "UpperChest", rot_y=10)       # ponta caída

    # cabeça: nariz, olhos cansados atrás dos óculos, sobrancelhas e o bigodão
    p.ellipsoid(HEAD_C, (0.1, 0.1, 0.125), "Pele", "Head", seg=24, rings=14)
    p.ellipsoid(HEAD_C + Vector((0, -0.1, 0.0)), (0.02, 0.022, 0.03), "Pele", "Head", seg=10, rings=6)
    for s in (1, -1):
        p.ellipsoid(HEAD_C + Vector((0.098 * s, 0.0, 0.0)), (0.015, 0.025, 0.03), "Pele", "Head", seg=10, rings=6)
        eye = HEAD_C + Vector((0.04 * s, -0.093, 0.025))
        p.ellipsoid(eye, (0.01, 0.006, 0.006), "Olho", "Head", seg=8, rings=5)
        p.tube(eye + Vector((0, -0.006, 0)), eye + Vector((0, -0.012, 0)), 0.028, 0.028, "Aro", "Head",
               seg=14, smooth=False, caps=False)                                      # óculos redondos
        p.polyline([HEAD_C + Vector((0.015 * s, -0.098, 0.062)), HEAD_C + Vector((0.07 * s, -0.09, 0.07))],
                   0.008, "Bigode", "Head", seg=5)                                    # sobrancelha
    for side, s in (("L", 1), ("R", -1)):
        bone = f"mustache_{side}"
        p.ellipsoid(HEAD_C + Vector((0.035 * s, -0.11, -0.045)), (0.045, 0.02, 0.025), "Bigode", bone,
                    seg=12, rings=6, rot=(0, -15 * s, 0))
        p.ellipsoid(HEAD_C + Vector((0.075 * s, -0.1, -0.06)), (0.022, 0.015, 0.018), "Bigode", bone,
                    seg=10, rings=5)

    # chapéu velho de feltro com fita laranja
    top = HEAD_C + Vector((0, 0.0, 0.09))
    p.tube(top, top + Vector((0, 0, 0.012)), 0.16, 0.16, "Chapeu", "hat", seg=22, smooth=False)
    p.tube(top + Vector((0, 0, 0.012)), top + Vector((0, 0, 0.13)), 0.1, 0.09, "Chapeu", "hat", seg=22)
    p.tube(top + Vector((0, 0, 0.014)), top + Vector((0, 0, 0.04)), 0.101, 0.099, "Gravata", "hat",
           seg=22, caps=False)
    p.ellipsoid(top + Vector((0, 0, 0.13)), (0.09, 0.08, 0.02), "Chapeu", "hat", seg=16, rings=6)
    return p.to_object(col, arm)


def main():
    scene, cols = bc.new_scene("Valdemar", kind="Bosses")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])

    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.29, -0.03, 0.6))   # lápis
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.29, -0.03, 0.6))     # bloco
    bc.add_socket("SOC_Hip", soc, arm, "Hips", (0.25, -0.02, 0.78))           # rádio

    bc.report_tris((body,))
    bc.finish(scene, cols, "Valdemar", HEIGHT, kind="Bosses")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
