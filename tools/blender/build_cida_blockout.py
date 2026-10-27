"""Blockout da Dona Cida (CHR_01) gerado a partir da ficha do Art Bible.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_cida_blockout.py

Saídas (caminhos relativos à raiz do repositório):
    Art/Source/Characters/Cida/SK_CHR_Cida_Blockout.blend
    MudancaAssombrada/Assets/Art/Characters/Cida/SK_CHR_Cida.fbx
    Art/Source/Characters/Cida/previews/*.png
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import blockout_common as bc  # noqa: E402

# desenho da ficha: chão em y = 466, centro em x = 120
d = bc.mapper(120, 466)
HEIGHT = 1.52

MATS = bc.Materials("MAT_CHR_Cida", {
    "Pele": ("#E6B892", 0.0, 0.55, 0.0),
    "Cabelo": ("#CFCAD3", 0.0, 0.7, 0.0),
    "Cardiga": ("#C99A3E", 0.0, 0.9, 0.0),
    "Colete": ("#3F5A5E", 0.0, 0.8, 0.0),
    "Faixa": ("#F08A3C", 0.0, 0.5, 0.3),
    "Saia": ("#6B4A6E", 0.0, 0.85, 0.0),
    "Bengala": ("#EDEBE4", 0.0, 0.4, 0.0),
    "BengalaFaixa": ("#C8463C", 0.0, 0.4, 0.0),
    "Metal": ("#D9B45A", 1.0, 0.3, 0.0),
    "Lente": ("#1C1A22", 0.0, 0.1, 0.0),
    "Sapato": ("#5A3A2A", 0.0, 0.6, 0.0),
    "Pochete": ("#2D2A33", 0.0, 0.6, 0.0),
    "Etiqueta": ("#E9DFC8", 0.0, 0.8, 0.0),
    "Perola": ("#F4F0E8", 0.0, 0.25, 0.0),
    "Boca": ("#7A3E3A", 0.0, 0.6, 0.0),
    "Sobrancelha": ("#8E8794", 0.0, 0.8, 0.0),
    "Borracha": ("#1C1A22", 0.0, 0.8, 0.0),
    "Unha": ("#F0CDB0", 0.0, 0.4, 0.0),
})

# Ficha "Dona Cida — mão direita": mão de 15 cm (punho até a ponta do médio), palma de 8,5 cm,
# nós dos dedos marcados (mão de 63 anos que trabalhou com piano). Aliança e relógio dourado
# são parte da malha (props de skin), não malhas extras.
HAND = {
    "length": 0.15, "middle": 0.50, "knuckles": 0.085, "wrist": 0.074, "thick": 0.024,
    "radius": {"Thumb": 0.0098, "Index": 0.0080, "Middle": 0.0084, "Ring": 0.0080, "Little": 0.0070},
    "ratio": {"Index": 0.92, "Middle": 1.0, "Ring": 0.94, "Little": 0.76},
    "drop": {"Ring": 0.002, "Little": 0.007},
    "splay": {"Index": 2, "Middle": 0, "Ring": 2, "Little": 5},
    "thumb_len": 0.056, "knuckle_bulge": 0.10,
}

ARMS = {
    "Left": ((0.03, 0, 0.83), (0.153, 0, 0.813), (0.22, -0.01, 0.70), (0.207, -0.03, 0.593), (0.21, -0.035, 0.53)),
    "Right": ((-0.03, 0, 0.83), (-0.153, 0, 0.813), (-0.207, 0, 0.68), (-0.2, 0, 0.553), (-0.2, 0, 0.49)),
}
LEGS = {side: ((x, 0, 0.53), (x, 0, 0.29), (x, 0, 0.06)) for side, x in (("Left", 0.067), ("Right", -0.067))}
GRIP = (0.21, -0.035, 0.575)


def build_rig(col):
    bones = [
        ("Hips", (0, 0, 0.53), (0, 0, 0.62), None, False),
        ("Spine", (0, 0, 0.62), (0, 0, 0.70), "Hips", True),
        ("Chest", (0, 0, 0.70), (0, 0, 0.78), "Spine", True),
        ("UpperChest", (0, 0, 0.78), (0, 0, 0.86), "Chest", True),
        ("Neck", (0, -0.01, 0.86), (0, -0.015, 0.95), "UpperChest", False),
        ("Head", (0, -0.015, 0.95), (0, -0.02, 1.30), "Neck", True),
        ("bun_jiggle", (0, 0.06, 1.34), (0, 0.07, 1.47), "Head", False),
    ]
    bones += bc.humanoid_limbs(ARMS, LEGS)
    # saia: 4 ossos de mola (sem peso no blockout; prontos para cloth/spring no Unity)
    for name, (x, y) in {"skirt_F": (0, -0.12), "skirt_B": (0, 0.12),
                         "skirt_L": (0.15, 0), "skirt_R": (-0.15, 0)}.items():
        bones.append((name, (x * 0.8, y * 0.8, 0.53), (x * 1.25, y * 1.25, 0.12), "Hips", False))
    arm = bc.build_armature("RIG_CHR_Cida", col, bones)
    bc.add_finger_bones(arm, ARMS, {side: HAND for side in ARMS})
    return arm


def build_body(arm, col):
    p = bc.Part("SK_CHR_Cida", MATS)

    # pernas (meia-calça) e sapatos
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.067 * s
        p.tube((x, 0, 0.53), (x, 0, 0.29), 0.035, 0.03, "Pele", f"{side}UpperLeg", seg=10)
        p.tube((x, 0, 0.29), (x, 0, 0.045), 0.026, 0.021, "Pele", f"{side}LowerLeg", seg=10)
        p.ellipsoid((x, -0.025, 0.03), (0.06, 0.095, 0.035), "Sapato", f"{side}Foot", seg=16, rings=8)
        p.tube((x - 0.055, -0.02, 0.05), (x + 0.055, -0.02, 0.05), 0.008, 0.008, "Sapato", f"{side}Foot", seg=6)

    # saia (pregas ficam na textura)
    p.tube((0, 0, d(0, 438)[1]), (0, 0, d(0, 300)[1]), 0.19, 0.14, "Saia", "Hips", seg=24, squash_y=0.78)

    # tronco = cardigã; colete por cima com a frente aberta
    z_t0, z_t1 = d(0, 310)[1], d(0, 212)[1]
    p.tube((0, 0, z_t0), (0, 0, z_t1), 0.187, 0.167, "Cardiga", "Chest", seg=24, squash_y=0.64)
    p.ellipsoid((0, 0, z_t1 - 0.02), (0.17, 0.11, 0.06), "Cardiga", "UpperChest", seg=24, rings=8)
    vest = p.tube((0, 0, z_t0 + 0.01), (0, 0, z_t1 + 0.002), 0.194, 0.174, "Colete", "Chest",
                  seg=24, caps=False, squash_y=0.67)
    p.cut_front_opening(vest, 0.04)
    z_stripe = d(0, 272)[1]
    stripe = p.tube((0, 0, z_stripe - 0.015), (0, 0, z_stripe + 0.015), 0.199, 0.196, "Faixa", "Chest",
                    seg=24, caps=False, squash_y=0.68)
    p.cut_front_opening(stripe, 0.04)
    for z in (0.765, 0.70, 0.635):
        p.ellipsoid((0, -0.112, z), (0.009, 0.006, 0.009), "Metal", "Chest", seg=8, rings=4)
    p.box((-0.083, -0.112, 0.77), (0.07, 0.008, 0.033), "Etiqueta", "Chest")
    p.box((0, 0.118, 0.711), (0.2, 0.008, 0.107), "Faixa", "Chest")

    # pochete
    p.box((0, -0.118, 0.517), (0.24, 0.06, 0.073), "Pochete", "Hips")
    p.box((0, -0.15, 0.517), (0.053, 0.008, 0.04), "Metal", "Hips")

    # braços (mangas do cardigã), punhos, mãos
    for side, (_, sh, el, wr, _) in ARMS.items():
        p.limb([sh, el, wr], (0.045, 0.042, 0.038), "Cardiga", [f"{side}UpperArm", f"{side}LowerArm"])
        wr, el = Vector(wr), Vector(el)
        cuff_dir = (wr - el).normalized()
        p.tube(wr - cuff_dir * 0.03, wr, 0.046, 0.046, "Cardiga", f"{side}LowerArm", seg=12)
        p.hand(side, ARMS[side], HAND, "Pele", nail="Unha")
    # aliança no anelar esquerdo, no meio da falange proximal (presa a esse osso)
    ring = bc.hand_chains("Left", ARMS["Left"], HAND)["Ring"]
    mid = ring[0].lerp(ring[1], 0.45)
    along = (ring[1] - ring[0]).normalized() * 0.0018
    r = HAND["radius"]["Ring"] * 1.12
    p.tube(mid - along, mid + along, r, r, "Metal", "LeftRingProximal", seg=10)
    watch = Vector(ARMS["Right"][3]) + Vector((0, 0, -0.006))
    p.tube(watch + Vector((0, 0, 0.007)), watch - Vector((0, 0, 0.007)), 0.04, 0.04, "Metal", "RightHand", seg=12)

    # pescoço, cabeça, orelhas, brincos, nariz
    p.tube((0, -0.01, 0.84), (0, -0.015, 0.96), 0.042, 0.04, "Pele", "Neck", seg=12)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        ex, ez = d(120 + 62 * s, 138)
        p.ellipsoid((ex, 0.0, ez), (0.037, 0.022, 0.037), "Pele", "Head", seg=12, rings=8)
        p.ellipsoid((ex, -0.005, ez - 0.052), (0.013, 0.013, 0.013), "Perola", "Head", seg=8, rings=6)
    nx, nz = d(120, 155)
    p.ellipsoid((nx, -0.212, nz), (0.022, 0.03, 0.028), "Pele", "Head", seg=12, rings=8)

    # sobrancelhas e sorriso torto (geometria leve só para leitura do blockout)
    for s in (1, -1):
        p.polyline([(0.04 * s, -0.188, 1.175), (0.085 * s, -0.19, 1.19), (0.127 * s, -0.172, 1.175)],
                   0.008, "Sobrancelha", "Head", seg=6)
    p.polyline([(-0.053, -0.197, 0.997), (-0.025, -0.205, 0.975), (0.0, -0.207, 0.968),
                (0.03, -0.205, 0.978), (0.053, -0.197, 1.0)], 0.0065, "Boca", "Head", seg=6)
    return p.to_object(col, arm)


HEAD_C = Vector((0.0, -0.02, d(120, 132)[1]))
HEAD_R = Vector((0.207, 0.193, 0.213))


def build_hair(arm, col):
    """Cabelo em nuvem de cachos: esferas sobre a calota da cabeça (fora do rosto), coque e diapasão."""
    p = bc.Part("SK_CHR_Cida_Hair", MATS)

    # cachos da silhueta, direto dos círculos da vista de frente da ficha
    front = [(72, 106, 26, -0.03), (92, 82, 28, -0.06), (120, 74, 30, -0.05), (148, 82, 28, -0.06),
             (168, 106, 26, -0.03), (62, 130, 14, 0.01), (178, 130, 14, 0.01)]
    for x, y, r, depth in front:
        ex, ez = d(x, y)
        p.ellipsoid((ex, depth, ez), (r / 300,) * 3, "Cabelo", "Head", seg=12, rings=8)

    # cobertura: pontos de Fibonacci na calota, pulando a região do rosto
    n = 90
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rad = math.sqrt(1 - z * z)
        x, y = math.cos(golden * i) * rad, math.sin(golden * i) * rad
        face = y < -0.25 and z < 0.62
        nape = y > 0.35 and z > -0.6
        if (z < -0.25 and not nape) or face or (y < 0.1 and z < 0.05):
            continue
        pos = HEAD_C + Vector((x * HEAD_R.x, y * HEAD_R.y, z * HEAD_R.z)) * 0.93
        p.ellipsoid(pos, (0.062,) * 3, "Cabelo", "Head", seg=9, rings=5)

    bx, bz = d(120, 46)
    p.ellipsoid((bx, 0.05, bz), (0.078, 0.072, 0.076), "Cabelo", "bun_jiggle", seg=20, rings=10)
    p.ellipsoid((-0.022, 0.035, bz + 0.012), (0.032, 0.032, 0.032), "Cabelo", "bun_jiggle", seg=10, rings=6)
    fork = [((134, 52), (148, 20)), ((144, 56), (158, 24)), ((134, 52), (140, 58)),
            ((140, 58), (144, 56)), ((139, 58), (133, 72))]
    for (x0, y0), (x1, y1) in fork:
        a, b = d(x0, y0), d(x1, y1)
        p.tube((a[0], 0.055, a[1]), (b[0], 0.055, b[1]), 0.0055, 0.0055, "Metal", "bun_jiggle", seg=6)
    return p.to_object(col, arm)


def build_glasses(arm, col):
    p = bc.Part("SM_CHR_Cida_Oculos", MATS)
    for s in (1, -1):
        cx = 0.073 * s
        p.tube((cx, -0.209, 1.09), (cx, -0.217, 1.09), 0.057, 0.057, "Lente", "Head", seg=20, smooth=False)
        p.tube((cx, -0.205, 1.09), (cx, -0.211, 1.09), 0.063, 0.063, "Metal", "Head", seg=20, smooth=False)
        p.tube((0.13 * s, -0.205, 1.097), (0.2 * s, -0.03, 1.105), 0.005, 0.005, "Metal", "Head", seg=6)
    p.tube((-0.017, -0.214, 1.095), (0.017, -0.214, 1.095), 0.005, 0.005, "Metal", "Head", seg=6)
    return p.to_object(col, arm)


# ---------------------------------------------------------------- pega da bengala
# Pose-chave "pega da bengala" da ficha de mãos: a mão esquerda fecha em volta da bengala em pé,
# com o antebraço à frente e o polegar para cima (assim o "furo" do punho fica na vertical; com o
# braço caído ele fica deitado e a bengala não passa). A pose vira o clipe
# Animations/PegaBengala.fbx (camada "Bengala" do Animator: só braço e dedos esquerdos) e a
# bengala é modelada NESSA pose, então cai certinha dentro do punho.
HOLD_UPPER = Vector((0.25, -0.35, -1.0))   # braço: para baixo, um pouco à frente e para fora
HOLD_FORE = Vector((0.08, -1.0, -0.45))    # antebraço (e mão): à frente, um pouco para baixo
HOLD_CURL = {"Proximal": 80, "Intermediate": 90, "Distal": 45}   # graus; +X fecha na palma
THUMB_CURL = {"Proximal": 25, "Intermediate": 35, "Distal": 25}
GRIP_FINGERS = ("Index", "Middle", "Ring", "Little")


def pose_hold(arm):
    """Põe o braço esquerdo na pega (pose, não muda o rig)."""
    pb = arm.pose.bones
    inward = Vector((-1, 0, 0))   # a palma esquerda olha para o corpo, em repouso e na pega

    def orient(name, direction):
        # Aponta o osso em "direction" mantendo a palma para dentro (sem torcer o braço).
        bpy.context.view_layer.update()
        b = pb[name]
        palm_local = b.bone.matrix_local.to_3x3().normalized().inverted() @ inward
        palm_local.y = 0
        palm_local.normalize()
        y = direction.normalized()
        z = (inward - y * inward.dot(y)).normalized()
        world = Matrix((y.cross(z), y, z)).transposed()
        local = Matrix((Vector((0, 1, 0)).cross(palm_local), Vector((0, 1, 0)), palm_local)).transposed()
        m = (world @ local.inverted()).to_4x4()
        m.translation = b.head
        b.matrix = m

    orient("LeftUpperArm", HOLD_UPPER)
    orient("LeftLowerArm", HOLD_FORE)
    orient("LeftHand", HOLD_FORE)
    for finger in GRIP_FINGERS + ("Thumb",):
        curl = THUMB_CURL if finger == "Thumb" else HOLD_CURL
        for part, deg in curl.items():
            b = pb[f"Left{finger}{part}"]
            b.rotation_mode = "XYZ"
            b.rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()


def clear_pose(arm):
    for b in arm.pose.bones:
        b.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def build_cane(col, socket, arm):
    """Bengala como prop separado, origem no ponto de pega (convenção de props).

    Calculada com o braço na pega: em pé, passando pelo meio do punho fechado, até o chão.
    """
    pose_hold(arm)
    pb = arm.pose.bones
    # meio do punho: entre o nó de cada dedo e a ponta da falange média (que volta para a palma)
    center = sum(((pb[f"Left{f}Proximal"].head + pb[f"Left{f}Intermediate"].tail) / 2
                  for f in GRIP_FINGERS), Vector()) / len(GRIP_FINGERS)
    top = center + Vector((0, 0, 0.035))
    tip = Vector((center.x, center.y, 0.0))
    p0, c, p2 = top, top + Vector((0, 0, 0.075)), top + Vector((0, -0.048, 0.048))   # gancho para a frente
    world = [(1 - t) ** 2 * p0 + 2 * (1 - t) * t * c + t ** 2 * p2 for t in [i / 6 for i in range(7)]]
    band_z = d(0, 440)[1]
    band = top + (tip - top) * ((top.z - band_z) / (top.z - tip.z))
    ferrule = tip + (top - tip).normalized() * 0.02

    # A bengala anda com o socket da mão: guarda os pontos no espaço do socket NA PEGA
    # (to_prop desconta o GRIP, então soma de volta).
    to_socket = socket.matrix_world.inverted()
    local = lambda v: to_socket @ v + Vector(GRIP)
    p = bc.Part("SM_PRP_Bengala", MATS)
    p.polyline([local(v) for v in world], 0.01, "Bengala", "cane", seg=10)
    p.tube(local(top), local(band), 0.01, 0.01, "Bengala", "cane", seg=10)
    p.tube(local(band), local(ferrule), 0.0105, 0.0105, "BengalaFaixa", "cane", seg=10)
    p.tube(local(ferrule), local(tip), 0.011, 0.011, "Borracha", "cane", seg=10)
    tip_local = to_socket @ tip
    clear_pose(arm)
    print("PEGA centro", tuple(round(x, 3) for x in center), "ponta", tuple(round(x, 3) for x in tip))
    return p.to_prop(col, socket, GRIP), tip_local


def export_hold_clip(arm):
    """A pega como clipe (dois quadros iguais), só o esqueleto: Assets/Art/Animations/PegaBengala.fbx."""
    pose_hold(arm)
    arm.animation_data_create()
    action = bpy.data.actions.new("PegaBengala")
    arm.animation_data.action = action
    for frame in (1, 30):
        for b in arm.pose.bones:
            b.keyframe_insert("location", frame=frame)
            b.keyframe_insert("rotation_quaternion" if b.rotation_mode == "QUATERNION" else "rotation_euler", frame=frame)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 30
    # Um vazio ao lado do esqueleto: sozinho na raiz, o Unity "achata" o RIG_CHR_Cida e a
    # hierarquia deixa de bater com a do modelo (o clipe usa o avatar da Cida, ver ArtImportSettings).
    keep = bpy.data.objects.new("Clip", None)
    bpy.context.scene.collection.objects.link(keep)
    bc.export_fbx(bc.UNITY_ROOT / "Animations" / "PegaBengala.fbx", animation=True)
    bpy.data.objects.remove(keep)
    print("SAVED", bc.UNITY_ROOT / "Animations" / "PegaBengala.fbx")


def main():
    scene, cols = bc.new_scene("Cida")
    arm = build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    hair = build_hair(arm, cols["Geo"])
    glasses = build_glasses(arm, cols["Geo"])

    # os desenhos da ficha mostram a bengala na mão esquerda da Cida
    soc_l = bc.add_socket("SOC_Hand_L", cols["Sockets"], arm, "LeftHand", GRIP)
    bc.add_socket("SOC_Hand_R", cols["Sockets"], arm, "RightHand", (-0.2, -0.01, 0.515))
    bc.add_socket("SOC_Bun", cols["Sockets"], arm, "bun_jiggle", (0.0, 0.05, d(0, 46)[1]))
    cane, tip = build_cane(cols["Props"], soc_l, arm)
    bc.add_child_empty("SOC_CaneTip", cols["Sockets"], cane, tip)

    bc.report_tris((body, hair, glasses, cane))
    # o FBX sai em repouso; as prévias (e o .blend) já mostram a Cida segurando a bengala
    bc.finish(scene, cols, "Cida", HEIGHT, before_previews=lambda: pose_hold(arm))
    clear_pose(arm)
    export_hold_clip(arm)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
