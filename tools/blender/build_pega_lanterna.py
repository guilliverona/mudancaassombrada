"""Pega da lanterna: a mão direita fechada em volta do cabo da lanterna (clipe Humanoid).

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_pega_lanterna.py

Saídas: MudancaAssombrada/Assets/Art/Animations/PegaLanterna.fbx (só o esqueleto do Tiago, na pose)
e Art/Source/Animations/PegaLanterna/previews/ (a mão fechada em volta do cabo, para conferir).

Só os DEDOS importam: no jogo, a camada "Lanterna" do Animator usa só os dedos direitos, e o braço
e o punho são postos por código (IK do HeldItems). Os dedos fecham em volta de um cabo de 3,2 cm de
raio (o SM_PRP_Lanterna: tubo de 0,032), mais folgado que a pega da bengala (1 cm de raio), e o
polegar passa por cima do cabo. Feito no rig do Tiago; como é Humanoid, vale para todos os
personagens (o Unity lê com o avatar do Tiago: ArtImportSettings.OwnRigClips).
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402
import build_tiago_blockout as tiago  # noqa: E402  (só o rig e o corpo; o main dele não roda)

HANDLE_R = 0.032   # raio do cabo da lanterna (build_furniture_blockout.build_lanterna)
# Graus em volta do X de cada falange (+X fecha na palma, ver bc.add_finger_bones). Um cabo grosso
# pede menos dobra que a bengala (80/90/45): os dedos abraçam em vez de fechar o punho.
CURL = {"Proximal": 55, "Intermediate": 65, "Distal": 40}
# O polegar por cima do cabo, encostando no indicador.
THUMB_CURL = {"Proximal": 30, "Intermediate": 35, "Distal": 25}
FINGERS = ("Index", "Middle", "Ring", "Little")


def pose_grip(arm):
    pb = arm.pose.bones
    for finger in FINGERS + ("Thumb",):
        curl = THUMB_CURL if finger == "Thumb" else CURL
        for part, deg in curl.items():
            b = pb[f"Right{finger}{part}"]
            b.rotation_mode = "XYZ"
            b.rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()


def fist(arm):
    """Meio do punho e o "furo" (do mindinho para o indicador), em mundo, na pose."""
    pb = arm.pose.bones
    w = arm.matrix_world
    center = sum(((w @ pb[f"Right{f}Proximal"].head + w @ pb[f"Right{f}Intermediate"].tail) / 2
                  for f in FINGERS), Vector()) / len(FINGERS)
    hole = (w @ pb["RightIndexProximal"].head - w @ pb["RightLittleProximal"].head).normalized()
    return center, hole


def export_clip(arm):
    """Dois quadros iguais, só o esqueleto: Assets/Art/Animations/PegaLanterna.fbx."""
    arm.animation_data_create()
    action = bpy.data.actions.new("PegaLanterna")
    arm.animation_data.action = action
    for frame in (1, 30):
        for b in arm.pose.bones:
            b.keyframe_insert("location", frame=frame)
            b.keyframe_insert("rotation_quaternion" if b.rotation_mode == "QUATERNION" else "rotation_euler", frame=frame)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 30
    # Um vazio ao lado do esqueleto: sozinho na raiz, o Unity "achata" o rig (como na PegaBengala).
    keep = bpy.data.objects.new("Clip", None)
    bpy.context.scene.collection.objects.link(keep)
    out = bc.UNITY_ROOT / "Animations" / "PegaLanterna.fbx"
    bc.export_fbx(out, animation=True)
    bpy.data.objects.remove(keep)
    print("SAVED", out)


def previews(scene, cols, arm, center, hole):
    """O corpo do Tiago na pose, com um cabo de prova no punho: frente e lado da mão, bem de perto."""
    tiago.build_body(arm, cols["Geo"])
    bpy.ops.mesh.primitive_cylinder_add(radius=HANDLE_R, depth=0.2, location=center)
    handle = bpy.context.active_object
    handle.rotation_euler = hole.to_track_quat("Z", "Y").to_euler()
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_backface_culling = True
    scene.render.resolution_x = scene.render.resolution_y = 600
    out = bc.SOURCE_ROOT / "Animations" / "PegaLanterna" / "previews"
    out.mkdir(parents=True, exist_ok=True)
    for name, offset in {"frente": Vector((0, -0.6, 0)), "lado": Vector((-0.6, 0, 0)),
                         "palma": Vector((0.6, 0, 0)), "cima": Vector((0, -0.05, 0.6))}.items():
        cam_data = bpy.data.cameras.new(name)
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = 0.28
        cam = bpy.data.objects.new(name, cam_data)
        cols["Preview"].objects.link(cam)
        cam.location = center + offset
        cam.rotation_euler = (-offset).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        scene.render.filepath = str(out / f"pega_lanterna_{name}.png")
        bpy.ops.render.render(write_still=True)
    print("PREVIEWS", out)


def add_sockets(cols, arm):
    """Os mesmos encaixes do SK_CHR_Tiago: o avatar dele usa a MESMA hierarquia (o Unity mapeou o
    SOC_Ear_L como "olho"); sem eles, o clipe não importa ("Transform hierarchy does not match")."""
    soc = cols["Sockets"]
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.272, -0.01, 0.65))
    bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", (0.272, -0.01, 0.65))
    bc.add_socket("SOC_Neck", soc, arm, "notebook_lanyard", tiago.NOTEBOOK_GRIP)
    bc.add_socket("SOC_Ear_L", soc, arm, "Head", tiago.PENCIL_GRIP)
    bc.add_socket("SOC_Hip_L", soc, arm, "bag", tiago.BAG)


def main():
    scene, cols = bc.new_scene("PegaLanterna")
    arm = tiago.build_rig(cols["Rig"])
    add_sockets(cols, arm)
    pose_grip(arm)
    center, hole = fist(arm)
    print("PUNHO centro", tuple(round(x, 3) for x in center), "furo", tuple(round(x, 3) for x in hole))
    export_clip(arm)
    previews(scene, cols, arm, center, hole)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
