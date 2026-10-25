"""Blockout da Maestrina Odete (GHO_02, Fase 2 · O Conservatório).

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_maestrina_blockout.py

Saídas: Art/Source/Ghosts/Maestrina/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Ghosts/Maestrina/SK_GHO_Maestrina.fbx

Ficha: 2,40 m (flutua) · forma "sino + espiral" · vestido de concerto em sino com fitas de
partitura enroladas · pescoço longo (estica +40 cm no crescendo: ossos de pescoço com escala
em Y na arte final) · olhos fechados · coque atravessado por duas batutas · broche ciano.
Modelada "ensaiando": batuta na mão direita, braços em A-pose para o rig Humanoid.
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

HEIGHT = 2.4

MATS = bc.Materials("MAT_GHO_Maestrina", {
    "Vestido": ("#2E2A4A", 0.0, 0.9, 0.0),
    "VestidoEscuro": ("#1E1B33", 0.0, 0.9, 0.0),
    "Fita": ("#E9DFC8", 0.0, 0.8, 0.0),
    "Pauta": ("#2B2733", 0.0, 0.8, 0.0),
    "Pele": ("#C9C6D6", 0.0, 0.6, 0.0),
    "Coque": ("#8E8794", 0.0, 0.8, 0.0),
    "Batuta": ("#F4F0E8", 0.0, 0.5, 0.0),
    "Cortica": ("#9C4A2E", 0.0, 0.8, 0.0),
    "Luva": ("#1C1A22", 0.0, 0.3, 0.0),
    "Broche": ("#7FE3C4", 0.0, 0.3, 3.0),
    "Ouro": ("#C9A24A", 1.0, 0.35, 0.0),
    "Linha": ("#4A4552", 0.0, 0.8, 0.0),
})

ARMS = {
    "Left": ((0.05, 0, 1.92), (0.2, 0, 1.92), (0.36, 0, 1.5), (0.46, 0, 1.12), (0.5, 0, 0.94)),
    "Right": ((-0.05, 0, 1.92), (-0.2, 0, 1.92), (-0.36, 0, 1.5), (-0.46, 0, 1.12), (-0.5, 0, 0.94)),
}
LEGS = {side: ((0.1 * s, 0, 1.0), (0.11 * s, 0, 0.55), (0.12 * s, 0, 0.1)) for side, s in (("Left", 1), ("Right", -1))}
HEAD_C = Vector((0, -0.01, 2.2))


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 1.0), (0, 0, 1.25), None, False),
        ("Spine", (0, 0, 1.25), (0, 0, 1.5), "Hips", True),
        ("Chest", (0, 0, 1.5), (0, 0, 1.72), "Spine", True),
        ("UpperChest", (0, 0, 1.72), (0, 0, 1.93), "Chest", True),
        ("Neck", (0, 0, 1.95), (0, 0, 2.1), "UpperChest", False),   # estica no crescendo
        ("Head", (0, 0, 2.1), (0, 0, 2.4), "Neck", True),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.08, toe_fwd=0.13)
    return bc.build_armature("RIG_GHO_Maestrina", col, bones)


DRESS_H, DRESS_R0, DRESS_R1, DRESS_SQ = 1.55, 0.58, 0.16, 0.85


def dress_front(z):
    """Onde fica a frente do vestido (em -Y) na altura z: para as fitas encostarem nele."""
    r = DRESS_R0 + (DRESS_R1 - DRESS_R0) * min(max(z / DRESS_H, 0.0), 1.0)
    return -r * DRESS_SQ


def ribbon(p, center, length, angle, bone):
    """Fita de partitura: uma tira de papel inclinada, com as 5 linhas da pauta."""
    c = Vector(center)
    c.y = dress_front(c.z) - 0.012   # colada na frente do vestido
    p.box(c, (length, 0.012, 0.07), "Fita", bone, rot_y=angle)
    for i in range(5):
        dz = -0.024 + i * 0.012
        off = Vector((-math.sin(math.radians(angle)) * dz, -0.007, math.cos(math.radians(angle)) * dz))
        p.box(c + off, (length * 0.94, 0.002, 0.003), "Pauta", bone, rot_y=angle)


def build_body(arm, col):
    p = bc.Part("SK_GHO_Maestrina", MATS)

    # vestido em sino até o chão (a barra se desfaz no jogo: _DissolveAmount)
    p.tube((0, 0, 0.0), (0, 0, DRESS_H), DRESS_R0, DRESS_R1, "Vestido", "Hips", seg=28, squash_y=DRESS_SQ)
    p.tube((0, 0, 0.0), (0, 0, 0.14), 0.59, 0.53, "VestidoEscuro", "Hips", seg=28, caps=False, squash_y=0.86)
    # corpete e ombros
    p.tube((0, 0, 1.5), (0, 0, 1.94), 0.15, 0.17, "Vestido", "Chest", seg=20, squash_y=0.8)
    p.ellipsoid((0, 0, 1.93), (0.21, 0.13, 0.05), "Vestido", "UpperChest", seg=20, rings=8)
    p.ellipsoid((0, -0.13, 1.82), (0.028, 0.012, 0.028), "Broche", "UpperChest", seg=12, rings=6)
    p.tube((0, 0, 1.94), (0, -0.005, 2.0), 0.06, 0.045, "Fita", "Neck", seg=16, caps=False)  # gola de renda

    # fitas de partitura enroladas no vestido (a espiral da forma), mais curtas que a largura
    # do sino em cada altura, para acompanharem o vestido em vez de flutuar
    for (x, z, length, ang) in ((-0.04, 0.55, 0.7, 18), (0.1, 0.85, 0.55, -12),
                                 (-0.05, 1.15, 0.38, 25), (0.12, 0.3, 0.75, -15)):
        ribbon(p, (x, 0, z), length, ang, "Hips")

    # braços em mangas escuras e luvas de ópera; batuta na mão direita
    for side, (_, sh, el, wr, _) in ARMS.items():
        s = 1 if side == "Left" else -1
        p.limb([sh, el], (0.045, 0.04), "Vestido", [f"{side}UpperArm"], seg=12)
        p.limb([el, wr], (0.038, 0.032), "Luva", [f"{side}LowerArm"], seg=12)      # luva até o cotovelo
        palm = Vector(wr) + Vector((0, 0, -0.06))
        p.ellipsoid(palm, (0.035, 0.018, 0.05), "Luva", f"{side}Hand", seg=12, rings=8)
        for i, dx in enumerate((-0.022, -0.007, 0.008, 0.022)):
            top = palm + Vector((dx * s, 0, -0.04))
            tip = top + Vector((dx * s * 0.3, -0.01, -(0.09, 0.11, 0.1, 0.08)[i]))
            p.polyline([top, top.lerp(tip, 0.5) + Vector((0, -0.01, 0)), tip], 0.007, "Luva", f"{side}Hand", seg=6)
        if s < 0:
            grip = palm + Vector((0, -0.03, -0.02))
            p.tube(grip, grip + Vector((0, -0.05, 0.02)), 0.012, 0.012, "Cortica", "RightHand", seg=8)
            p.tube(grip + Vector((0, -0.05, 0.02)), grip + Vector((-0.06, -0.38, 0.22)), 0.006, 0.002,
                   "Batuta", "RightHand", seg=6)
            p.ellipsoid(palm + Vector((0.03, -0.02, 0.0)), (0.012,) * 3, "Ouro", "RightHand", seg=8, rings=5)  # anel

    # pescoço longo e a cabeça de olhos fechados
    p.tube((0, 0, 1.92), (0, -0.01, 2.1), 0.035, 0.032, "Pele", "Neck", seg=12)
    p.ellipsoid(HEAD_C, (0.1, 0.1, 0.13), "Pele", "Head", seg=24, rings=14)
    for s in (1, -1):
        eye = [HEAD_C + Vector((dx * s, -0.094, 0.02 - abs(dx - 0.04) * 0.25)) for dx in (0.015, 0.04, 0.065)]
        p.polyline(eye, 0.004, "Linha", "Head", seg=5)                       # olho fechado (arco)
        p.polyline([HEAD_C + Vector((0.02 * s, -0.092, 0.06)), HEAD_C + Vector((0.07 * s, -0.085, 0.055))],
                   0.004, "Linha", "Head", seg=5)                            # sobrancelha
        # pince-nez de ouro
        p.tube(HEAD_C + Vector((0.04 * s, -0.1, 0.015)), HEAD_C + Vector((0.04 * s, -0.104, 0.015)),
               0.022, 0.022, "Ouro", "Head", seg=14, smooth=False, caps=False)
    p.ellipsoid(HEAD_C + Vector((0, -0.1, -0.05)), (0.014, 0.008, 0.016), "Luva", "Head", seg=10, rings=6)  # boca "o"
    p.ellipsoid(HEAD_C + Vector((0, -0.1, -0.01)), (0.012, 0.012, 0.018), "Pele", "Head", seg=8, rings=5)   # nariz

    # coque alto com as duas batutas cruzadas
    bun = HEAD_C + Vector((0, 0.03, 0.15))
    p.ellipsoid(HEAD_C + Vector((0, 0.01, 0.06)), (0.105, 0.1, 0.09), "Coque", "Head", seg=20, rings=10)
    p.ellipsoid(bun, (0.075, 0.07, 0.07), "Coque", "Head", seg=18, rings=10)
    for s in (1, -1):
        p.tube(bun + Vector((-0.13 * s, 0, -0.06)), bun + Vector((0.13 * s, 0, 0.1)), 0.006, 0.006,
               "Batuta", "Head", seg=6)
    return p.to_object(col, arm)


def main():
    scene, cols = bc.new_scene("Maestrina", kind="Ghosts")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])

    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.5, -0.02, 0.88))
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.5, -0.05, 0.9))   # batuta
    bc.add_socket("SOC_Bun", soc, arm, "Head", (0, 0.03, 2.35))
    bc.add_socket("SOC_Brooch", soc, arm, "UpperChest", (0, -0.14, 1.82))     # pulsa no tempo da música

    bc.report_tris((body,))
    bc.finish(scene, cols, "Maestrina", HEIGHT, kind="Ghosts")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
