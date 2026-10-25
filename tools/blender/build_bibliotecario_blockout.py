"""Blockout do Bibliotecário Sombra (GHO_03) gerado a partir da ficha do Art Bible.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_bibliotecario_blockout.py

Saídas: Art/Source/Ghosts/Bibliotecario/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Ghosts/Bibliotecario/SK_GHO_Bibliotecario.fbx

Modelado em pé (A-pose) para o rig Humanoid; a postura curvada da ficha vem da animação.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

# desenho da ficha (vista de frente): 220 px por metro, chão em y = 572, centro em x = 150;
# ponta das orelhas em y = 40
d = bc.mapper(150, 572, px_per_m=220.0)
HEIGHT = 2.6
bc.set_scale(HEIGHT / d(0, 40)[1])

MATS = bc.Materials("MAT_GHO_Bibliotecario", {
    "Pele": ("#9A9488", 0.0, 0.7, 0.0),
    "Dobras": ("#7E786E", 0.0, 0.8, 0.0),
    "Orelha": ("#B87A74", 0.0, 0.5, 0.0),
    "Casaco": ("#4A3A2E", 0.0, 0.95, 0.0),
    "Paginas": ("#D8CBB0", 0.0, 0.9, 0.0),
    "Calca": ("#2A2420", 0.0, 0.9, 0.0),
    "Chinelo": ("#7A6A8A", 0.0, 1.0, 0.0),
    "Unha": ("#C9BF9A", 0.0, 0.5, 0.0),
    "Metal": ("#D9B45A", 1.0, 0.35, 0.0),
    "Boca": ("#0B0A0F", 0.0, 0.8, 0.0),
})

ARMS = {
    "Left": ((0.05, 0, 1.78), (0.236, 0, 1.78), (0.355, 0, 1.236), (0.409, 0, 0.691), (0.41, 0, 0.36)),
    "Right": ((-0.05, 0, 1.78), (-0.236, 0, 1.78), (-0.355, 0, 1.236), (-0.409, 0, 0.691), (-0.41, 0, 0.36)),
}
LEGS = {side: ((0.082 * s, 0, 0.78), (0.095 * s, 0, 0.42), (0.109 * s, 0, 0.06))
        for side, s in (("Left", 1), ("Right", -1))}
HEAD_C = Vector((0.0, -0.02, d(150, 96)[1]))
HEAD_R = Vector((0.2, 0.18, 0.255))


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.78), (0, 0, 1.0), None, False),
        ("Spine", (0, 0, 1.0), (0, 0, 1.25), "Hips", True),
        ("Chest", (0, 0, 1.25), (0, 0, 1.5), "Spine", True),
        ("UpperChest", (0, 0, 1.5), (0, 0, 1.8), "Chest", True),
        ("Neck", (0, -0.01, 1.83), (0, -0.02, 1.96), "UpperChest", False),
        ("Head", (0, -0.02, 1.96), (0, -0.02, 2.42), "Neck", True),
        # orelhas: abrem 60° no alerta (ver ficha). Pivô na base, junto à cabeça.
        ("ear_L", (0.2, 0, 2.22), (0.34, 0, 2.42), "Head", False),
        ("ear_R", (-0.2, 0, 2.22), (-0.34, 0, 2.42), "Head", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.1, toe_fwd=0.17)
    return bc.build_armature("RIG_GHO_Bibliotecario", col, bones)


def build_body(arm, col):
    p = bc.Part("SK_GHO_Bibliotecario", MATS)

    # chinelos felpudos e pernas finas
    for side, s in (("Left", 1), ("Right", -1)):
        x = 0.118 * s
        p.ellipsoid((x, -0.04, 0.03), (0.1, 0.15, 0.045), "Chinelo", f"{side}Foot", seg=16, rings=8)
        for dx in (-0.04, 0.0, 0.04):
            p.ellipsoid((x + dx, -0.15, 0.06), (0.025,) * 3, "Chinelo", f"{side}Foot", seg=8, rings=5)
        p.limb([(0.082 * s, 0, 0.78), (0.095 * s, 0, 0.42), (0.109 * s, 0, 0.06)], (0.034, 0.032, 0.03),
               "Calca", [f"{side}UpperLeg", f"{side}LowerLeg"], seg=10)

    # casaco comprido: abre em sino até a canela; a barra vira páginas
    z_hem, z_top = d(0, 520)[1], d(0, 170)[1]
    p.tube((0, 0, z_hem), (0, 0, z_top), 0.33, 0.245, "Casaco", "Spine", seg=24, squash_y=0.55)
    p.tube((0, 0, z_hem - 0.03), (0, 0, z_hem + 0.02), 0.338, 0.335, "Paginas", "Spine", seg=24,
           caps=False, squash_y=0.56)
    p.ellipsoid((0, 0, z_top - 0.03), (0.25, 0.15, 0.07), "Casaco", "UpperChest", seg=24, rings=8)
    for z, y in ((1.509, -0.146), (1.236, -0.157), (0.964, -0.168)):
        p.ellipsoid((0, y, z), (0.018, 0.01, 0.018), "Paginas", "Chest", seg=8, rings=5)
    # bolso com livros (lado direito do peito)
    p.box((-0.13, -0.158, 1.15), (0.14, 0.02, 0.16), "Calca", "Chest")
    for i, h in enumerate((0.06, 0.08, 0.05)):
        p.box((-0.165 + i * 0.03, -0.156, 1.23 + h / 2), (0.022, 0.018, h), "Paginas", "Chest")

    # braços em A-pose com dedos longos demais (4 juntas)
    for side, (_, sh, el, wr, _) in ARMS.items():
        s = 1 if side == "Left" else -1
        p.limb([sh, el, wr], (0.05, 0.047, 0.042), "Casaco", [f"{side}UpperArm", f"{side}LowerArm"])
        palm = Vector(wr) + Vector((0, 0, -0.06))
        p.ellipsoid(palm, (0.045, 0.022, 0.06), "Pele", f"{side}Hand", seg=12, rings=8)
        for i, dx in enumerate((-0.03, -0.01, 0.01, 0.03)):
            length = (0.22, 0.26, 0.25, 0.19)[i]
            top = palm + Vector((dx * s, 0, -0.04))
            tip = top + Vector((dx * s * 0.5, 0, -length))
            mid = top.lerp(tip, 0.5)
            p.polyline([top, mid, tip], 0.009, "Pele", f"{side}Hand", seg=6)
            p.ellipsoid(tip + Vector((0, 0, -0.012)), (0.008, 0.006, 0.016), "Unha", f"{side}Hand", seg=6, rings=4)
        thumb = palm + Vector((-0.035 * s, -0.015, -0.02))
        p.polyline([thumb, thumb + Vector((-0.02 * s, -0.01, -0.12))], 0.01, "Pele", f"{side}Hand", seg=6)

    # pescoço e cabeça sem olhos: pele lisa e dobras onde deveriam estar os olhos
    p.tube((0, -0.01, 1.8), (0, -0.02, 1.98), 0.046, 0.044, "Pele", "Neck", seg=12)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        p.polyline([(0.16 * s, -0.128, 2.18), (0.1 * s, -0.172, 2.168), (0.035 * s, -0.18, 2.18)],
                   0.011, "Dobras", "Head", seg=6)
        p.polyline([(0.14 * s, -0.12, 2.25), (0.05 * s, -0.16, 2.26)], 0.006, "Dobras", "Head", seg=6)
    p.ellipsoid((0, -0.188, 2.07), (0.02, 0.02, 0.025), "Pele", "Head", seg=10, rings=6)
    p.ellipsoid((0, -0.145, 1.977), (0.02, 0.015, 0.014), "Boca", "Head", seg=10, rings=6)

    # óculos que não servem para nada, pendurados no peito pela corrente
    for s in (1, -1):
        ring = Vector((0.06 * s, -0.165, 1.75))
        p.tube(ring + Vector((0, -0.004, 0)), ring + Vector((0, 0.004, 0)), 0.045, 0.045, "Metal",
               "UpperChest", seg=16, smooth=False, caps=False)
        p.polyline([(0.2 * s, -0.05, 2.14), (0.15 * s, -0.15, 1.86), (0.1 * s, -0.165, 1.77)], 0.004,
                   "Metal", "UpperChest", seg=5)
    return p.to_object(col, arm)


def build_ears(arm, col):
    """Orelhas enormes: malha da cabeça, mas em ossos próprios para abrir no alerta."""
    p = bc.Part("SK_GHO_Bibliotecario_Orelhas", MATS)
    for side, s in (("Left", 1), ("Right", -1)):
        bone = "ear_L" if s > 0 else "ear_R"
        c = (0.29 * s, 0.0, 2.32)
        p.ellipsoid(c, (0.075, 0.022, 0.15), "Pele", bone, seg=16, rings=10, rot=(0, 35 * s, 0))
        p.ellipsoid((c[0] - 0.005 * s, -0.016, c[2] - 0.01), (0.048, 0.01, 0.105), "Orelha", bone,
                    seg=12, rings=8, rot=(0, 35 * s, 0))
    return p.to_object(col, arm)


def main():
    scene, cols = bc.new_scene("Bibliotecario", kind="Ghosts")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    ears = build_ears(arm, cols["Geo"])

    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.41, -0.02, 0.6))
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.41, -0.02, 0.6))
    bc.add_socket("SOC_Mouth", soc, arm, "Head", (0, -0.16, 1.977))  # respiração/"shhh"

    bc.report_tris((body, ears))
    bc.finish(scene, cols, "Bibliotecario", HEIGHT, kind="Ghosts")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
