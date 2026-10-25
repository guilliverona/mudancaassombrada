"""Blockout do Seu Reflexo, o Imitador (GHO_01, Fase 1 · A Casa do Espelho).

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_imitador_blockout.py

Saídas: Art/Source/Ghosts/Imitador/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Ghosts/Imitador/SK_GHO_Imitador.fbx

Ficha: 2,30 m (flutua) · forma "lâmina vertical" (perfil fino) · máscara de porcelana com
cartola sobre um vazio · o tronco é um espelho oval de moldura dourada · braços longos demais
e luvas com 5 falanges por dedo (médio = 62% da mão) · cortina de veludo no lugar das pernas.
Olhos ciano (#7FE3C4, emissão 4,0): os únicos pontos ciano dele.

Rig Humanoid como os outros fantasmas (as pernas existem só no esqueleto, escondidas pela
cortina): o Unity precisa delas para o avatar. As cadeias de mola da cortina ficam para a arte final.
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

HEIGHT = 2.3

MATS = bc.Materials("MAT_GHO_Imitador", {
    "Porcelana": ("#F4F0E8", 0.0, 0.4, 0.0),
    "Bochecha": ("#E5A0A8", 0.0, 0.6, 0.0),
    "Boca": ("#1C1A22", 0.0, 0.8, 0.0),
    "Olho": ("#7FE3C4", 0.0, 0.3, 4.0),
    "Cartola": ("#1C1A22", 0.0, 0.7, 0.0),
    "FitaCartola": ("#7A1E2E", 0.0, 0.8, 0.0),
    "Veludo": ("#7A1E2E", 0.0, 0.95, 0.0),
    "VeludoEscuro": ("#4A1220", 0.0, 0.95, 0.0),
    "Moldura": ("#C9A24A", 1.0, 0.35, 0.0),
    "Espelho": ("#BFD0D6", 1.0, 0.05, 0.0),
    "Rachadura": ("#2B2733", 0.0, 0.8, 0.0),
    "Luva": ("#EDE6F0", 0.0, 0.6, 0.0),
    "Botao": ("#C9A24A", 1.0, 0.35, 0.0),
})

THIN = 0.32   # achata o corpo em Y: "lâmina vertical" (o perfil é fino de propósito)

ARMS = {
    "Left": ((0.06, 0, 1.74), (0.3, 0, 1.74), (0.42, 0, 1.18), (0.47, 0, 0.62), (0.48, 0, 0.36)),
    "Right": ((-0.06, 0, 1.74), (-0.3, 0, 1.74), (-0.42, 0, 1.18), (-0.47, 0, 0.62), (-0.48, 0, 0.36)),
}
LEGS = {side: ((0.1 * s, 0, 0.95), (0.11 * s, 0, 0.52), (0.12 * s, 0, 0.1)) for side, s in (("Left", 1), ("Right", -1))}
MASK_C = Vector((0, -0.01, 2.0))


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.95), (0, 0, 1.15), None, False),
        ("Spine", (0, 0, 1.15), (0, 0, 1.35), "Hips", True),
        ("Chest", (0, 0, 1.35), (0, 0, 1.55), "Spine", True),
        ("UpperChest", (0, 0, 1.55), (0, 0, 1.76), "Chest", True),
        ("Neck", (0, 0, 1.78), (0, 0, 1.86), "UpperChest", False),
        ("Head", (0, 0, 1.86), (0, 0, 2.3), "Neck", True),
        ("hat", (0, 0, 2.2), (0, 0, 2.32), "Head", False),  # inclina na reverência
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS, foot_fwd=0.08, toe_fwd=0.13)
    return bc.build_armature("RIG_GHO_Imitador", col, bones)


def build_body(arm, col):
    p = bc.Part("SK_GHO_Imitador", MATS)

    # cortina de veludo no lugar das pernas: abre em sino até o chão, com pregas
    p.tube((0, 0, 0.0), (0, 0, 1.1), 0.46, 0.2, "Veludo", "Hips", seg=24, squash_y=THIN)
    p.tube((0, 0, 0.0), (0, 0, 0.12), 0.47, 0.44, "VeludoEscuro", "Hips", seg=24, caps=False, squash_y=THIN + 0.01)
    for i in range(-3, 4):
        x0 = i * 0.12
        p.tube((x0, -0.155 * (1 - abs(i) / 5), 0.05), (x0 * 0.45, -0.07, 1.05), 0.018, 0.01,
               "VeludoEscuro", "Hips", seg=6)

    # tronco: o espelho oval com moldura dourada, rachado
    zc = 1.42
    p.ellipsoid((0, 0, zc), (0.33, 0.05, 0.42), "Moldura", "Chest", seg=28, rings=12)
    p.ellipsoid((0, -0.03, zc), (0.27, 0.03, 0.35), "Espelho", "Chest", seg=28, rings=12)
    p.polyline([(0.02, -0.062, 1.72), (-0.04, -0.064, 1.5), (0.06, -0.064, 1.32), (0.01, -0.062, 1.1)],
               0.004, "Rachadura", "Chest", seg=5)
    p.polyline([(-0.04, -0.064, 1.5), (-0.17, -0.06, 1.42)], 0.003, "Rachadura", "Chest", seg=5)
    for z in (1.82, 1.02):
        p.ellipsoid((0, -0.04, z), (0.035, 0.02, 0.035), "Moldura", "Chest", seg=10, rings=6)  # enfeites
    # ombros (barra fina que segura os braços) e pescoço fininho
    p.tube((-0.3, 0, 1.74), (0.3, 0, 1.74), 0.03, 0.03, "Luva", "UpperChest", seg=10)
    p.tube((0, 0, 1.74), (0, 0, 1.88), 0.022, 0.02, "Luva", "Neck", seg=8)

    # braços longos demais, brancos, e luvas de 5 falanges com punho de renda
    for side, (_, sh, el, wr, _) in ARMS.items():
        s = 1 if side == "Left" else -1
        p.limb([sh, el, wr], (0.032, 0.03, 0.028), "Luva", [f"{side}UpperArm", f"{side}LowerArm"], seg=10)
        p.tube(Vector(wr) + Vector((0, 0, 0.06)), Vector(wr) + Vector((0, 0, -0.01)), 0.05, 0.035,
               "Luva", f"{side}LowerArm", seg=12)                                    # punho de renda
        for z in (0.0, -0.025):
            p.ellipsoid(Vector(wr) + Vector((0.0, -0.03, z - 0.02)), (0.008,) * 3, "Botao", f"{side}Hand", seg=6, rings=4)
        palm = Vector(wr) + Vector((0, 0, -0.06))
        p.ellipsoid(palm, (0.045, 0.02, 0.055), "Luva", f"{side}Hand", seg=12, rings=8)
        for i, dx in enumerate((-0.03, -0.01, 0.01, 0.03)):
            length = (0.17, 0.21, 0.2, 0.15)[i]           # médio = 62% da mão
            top = palm + Vector((dx * s, 0, -0.045))
            pts = [top + Vector((dx * s * 0.3 * k / 5, -0.006 * math.sin(k), -length * k / 5)) for k in range(6)]
            p.polyline(pts, 0.008, "Luva", f"{side}Hand", seg=6)   # 5 falanges: dobra "em ondas"
        thumb = palm + Vector((-0.04 * s, -0.012, -0.01))
        p.polyline([thumb, thumb + Vector((-0.03 * s, -0.01, -0.05)), thumb + Vector((-0.035 * s, -0.012, -0.1))],
                   0.009, "Luva", f"{side}Hand", seg=6)
    return p.to_object(col, arm)


def build_mask(arm, col):
    """Máscara de porcelana (comédia) sobre um vazio, e a cartola com fita vinho."""
    p = bc.Part("SK_GHO_Imitador_Mascara", MATS)
    p.ellipsoid(MASK_C, (0.15, 0.06, 0.2), "Porcelana", "Head", seg=24, rings=14)
    for s in (1, -1):
        p.ellipsoid(MASK_C + Vector((0.055 * s, -0.052, 0.04)), (0.025, 0.012, 0.03), "Olho", "Head", seg=12, rings=6)
        p.ellipsoid(MASK_C + Vector((0.095 * s, -0.04, -0.03)), (0.03, 0.01, 0.02), "Bochecha", "Head", seg=10, rings=5)
        # sobrancelhas pintadas (arco)
        p.polyline([MASK_C + Vector((0.025 * s, -0.056, 0.09)), MASK_C + Vector((0.06 * s, -0.056, 0.105)),
                    MASK_C + Vector((0.095 * s, -0.05, 0.09))], 0.005, "Boca", "Head", seg=5)
    # sorriso largo (cantos para cima)
    smile = [MASK_C + Vector((math.cos(a) * 0.085, -0.056, -0.07 + math.sin(a) * 0.045))
             for a in [math.radians(190 + i * 20) for i in range(9)]]
    p.polyline(smile, 0.01, "Boca", "Head", seg=6)
    # cartola pequena, inclinada para trás
    top = MASK_C + Vector((0.0, 0.01, 0.2))
    p.tube(top, top + Vector((0, 0, 0.012)), 0.1, 0.1, "Cartola", "hat", seg=20, smooth=False)       # aba
    p.tube(top + Vector((0, 0, 0.012)), top + Vector((0, 0.01, 0.12)), 0.065, 0.068, "Cartola", "hat", seg=20)
    p.tube(top + Vector((0, 0, 0.015)), top + Vector((0, 0.002, 0.04)), 0.067, 0.067, "FitaCartola", "hat",
           seg=20, caps=False)
    return p.to_object(col, arm)


def main():
    scene, cols = bc.new_scene("Imitador", kind="Ghosts")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    mask = build_mask(arm, cols["Geo"])

    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.48, -0.02, 0.3))
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.48, -0.02, 0.3))
    bc.add_socket("SOC_Mirror", soc, arm, "Chest", (0, -0.05, 1.42))   # portal/captura

    bc.report_tris((body, mask))
    bc.finish(scene, cols, "Imitador", HEIGHT, kind="Ghosts")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
