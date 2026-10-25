"""Base compartilhada dos scripts de blockout (personagens, fantasmas e props).

Convenções da ficha "Pipeline Blender → Unity":
    1 BU = 1 m · olhando para -Y · pés (ou a base) na origem · nomes Unity Humanoid.

As medidas vêm dos desenhos do Art Bible (personagens: 1 unidade = 1/300 m; fantasmas
têm escala própria, ver mapper). Cada script pode chamar set_scale(k) para que a altura
final bata com a altura escrita na ficha; posições, raios, ossos e sockets passam todos
por esse fator.

Cada tipo de asset vai para uma pasta própria (ver KINDS).
"""

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

REPO = Path(__file__).resolve().parents[2]
SOURCE_ROOT = REPO / "Art/Source"
UNITY_ROOT = REPO / "MudancaAssombrada/Assets/Art"

# tipo -> prefixo do arquivo. A pasta (Art/Source/<tipo> e Assets/Art/<tipo>) também decide
# o rig ao importar no Unity (Assets/Editor/ArtImportSettings.cs):
# Characters, Ghosts e Bosses = Humanoid; Props e Environment = sem rig.
KINDS = {
    "Characters": "SK_CHR",
    "Ghosts": "SK_GHO",
    "Bosses": "SK_BOS",
    "Props": "SM_PRP",
    "Environment": "SM_ENV",
}

K = 1.0


def set_scale(k):
    global K
    K = k


def mapper(cx, ground, px_per_m=300.0):
    """Converte coordenada do desenho (vista de frente da ficha) para (X, Z) em metros."""
    return lambda x, y: ((x - cx) / px_per_m, (ground - y) / px_per_m)


def scaled(v):
    return Vector(v) * K


# ---------------------------------------------------------------- materiais

def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_color(h):
    h = h.lstrip("#")
    return tuple(srgb_to_linear(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4)) + (1.0,)


class Materials:
    """Tabela {chave: (hex, metallic, roughness, emissão)} → MAT_<prefixo>_<chave>."""

    def __init__(self, prefix, table):
        self.prefix = prefix
        self.table = table

    def get(self, key):
        name = f"{self.prefix}_{key}"
        mat = bpy.data.materials.get(name)
        if mat:
            return mat
        hexc, metal, rough, emit = self.table[key]
        color = hex_color(hexc)
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metal
        bsdf.inputs["Roughness"].default_value = rough
        if emit:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emit
        mat.diffuse_color = color
        mat.metallic = metal
        mat.roughness = rough
        return mat


# ---------------------------------------------------------------- geometria

class Part:
    """Acumula primitivas num único bmesh, cada uma com material e osso (peso rígido)."""

    def __init__(self, name, materials):
        self.name = name
        self.materials = materials
        self.bm = bmesh.new()
        self.deform = self.bm.verts.layers.deform.verify()
        self.mats = []
        self.groups = []

    def _index(self, items, key):
        if key not in items:
            items.append(key)
        return items.index(key)

    def _finish(self, verts, mat, bone, smooth):
        mi = self._index(self.mats, mat)
        gi = self._index(self.groups, bone)
        vs = set(verts)
        for v in vs:
            v[self.deform][gi] = 1.0
        faces = {f for v in vs for f in v.link_faces}
        for f in faces:
            f.material_index = mi
            f.smooth = smooth
        return faces

    def ellipsoid(self, center, radii, mat, bone, seg=20, rings=10, smooth=True, rot=(0, 0, 0)):
        """rot: rotação em graus (X, Y, Z) em volta do próprio centro (ex.: orelhas inclinadas)."""
        r = Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
        m = Matrix.Scale(K, 4) @ Matrix.Translation(center) @ r @ Matrix.Diagonal((*radii, 1.0))
        r = bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=rings,
                                      radius=1.0, matrix=m, calc_uvs=True)
        return self._finish(r["verts"], mat, bone, smooth)

    def tube(self, p0, p1, r0, r1, mat, bone, seg=12, smooth=True, caps=True, squash_y=1.0):
        p0, p1 = Vector(p0), Vector(p1)
        axis = p1 - p0
        rot = Vector((0, 0, 1)).rotation_difference(axis).to_matrix().to_4x4()
        m = (Matrix.Scale(K, 4) @ Matrix.Translation((p0 + p1) / 2)
             @ Matrix.Diagonal((1, squash_y, 1, 1)) @ rot)
        r = bmesh.ops.create_cone(self.bm, cap_ends=caps, cap_tris=False, segments=seg,
                                  radius1=r0, radius2=r1, depth=axis.length,
                                  matrix=m, calc_uvs=True)
        return self._finish(r["verts"], mat, bone, smooth)

    def box(self, center, size, mat, bone, rot_y=0.0):
        m = (Matrix.Scale(K, 4) @ Matrix.Translation(center)
             @ Matrix.Rotation(math.radians(rot_y), 4, "Y") @ Matrix.Diagonal((*size, 1.0)))
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=m, calc_uvs=True)
        return self._finish(r["verts"], mat, bone, False)

    def wheel(self, center, radius, width, mat, bone="root", seg=16):
        """Cilindro deitado no eixo X (rodas)."""
        c = Vector(center)
        half = Vector((width / 2, 0, 0))
        return self.tube(c - half, c + half, radius, radius, mat, bone, seg=seg, smooth=False)

    def limb(self, pts, radii, mat, bones, seg=12):
        """Cadeia de tubos com esferas nas juntas (braços, pernas)."""
        for i in range(len(pts) - 1):
            self.tube(pts[i], pts[i + 1], radii[i], radii[i + 1], mat, bones[i], seg=seg, caps=False)
            self.ellipsoid(pts[i], (radii[i],) * 3, mat, bones[i], seg=seg, rings=6)

    def polyline(self, pts, radius, mat, bone, seg=8):
        for i in range(len(pts) - 1):
            self.tube(pts[i], pts[i + 1], radius, radius, mat, bone, seg=seg, caps=False)
            if i:
                self.ellipsoid(pts[i], (radius,) * 3, mat, bone, seg=seg, rings=4)

    def cut_front_opening(self, faces, half_width):
        """Abre a frente de peças tipo colete (faces com centro na frente e |x| pequeno)."""
        kill = [f for f in faces if f.is_valid and f.calc_center_median().y < 0
                and abs(f.calc_center_median().x) < half_width * K]
        bmesh.ops.delete(self.bm, geom=kill, context="FACES")

    def cut_below(self, faces, z):
        """Remove as faces abaixo de z (domos: boné, gorro)."""
        kill = [f for f in faces if f.is_valid and f.calc_center_median().z < z * K]
        bmesh.ops.delete(self.bm, geom=kill, context="FACES")

    def _mesh(self):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        self.bm.free()
        for key in self.mats:
            mesh.materials.append(self.materials.get(key))
        return mesh

    def to_object(self, collection, armature):
        """Malha com pesos rígidos, filha do rig com modificador Armature."""
        obj = bpy.data.objects.new(self.name, self._mesh())
        collection.objects.link(obj)
        for g in self.groups:
            obj.vertex_groups.new(name=g)
        obj.parent = armature
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        return obj

    def to_static(self, collection):
        """Malha sem rig (móveis, caminhão), com a origem na base, como foi modelada."""
        obj = bpy.data.objects.new(self.name, self._mesh())
        collection.objects.link(obj)
        return obj

    def to_prop(self, collection, socket, grip):
        """Prop estático (sem pesos). Geometria em coordenadas do personagem; origem vai para o ponto de pega."""
        mesh = self._mesh()
        mesh.transform(Matrix.Translation(-scaled(grip)))
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        obj.parent = socket
        obj.matrix_parent_inverse = Matrix.Identity(4)
        obj.location = (0, 0, 0)
        return obj


# ---------------------------------------------------------------- rig e sockets

def build_armature(name, collection, bones):
    """bones: lista de (nome, head, tail, pai, conectado). Posições antes da escala."""
    arm_data = bpy.data.armatures.new(name)
    arm = bpy.data.objects.new(name, arm_data)
    collection.objects.link(arm)
    arm_data.display_type = "STICK"
    arm.show_in_front = True
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones
    for bname, head, tail, parent, connect in bones:
        b = eb.new(bname)
        b.head, b.tail = scaled(head), scaled(tail)
        if parent:
            b.parent = eb[parent]
            b.use_connect = connect
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def humanoid_limbs(arms, legs, foot_fwd=0.07, toe_fwd=0.11):
    """Ossos de braços e pernas no padrão Unity Humanoid.

    arms[side] = (ombro_interno, ombro, cotovelo, pulso, ponta_da_mão)
    legs[side] = (quadril, joelho, tornozelo)   side em {"Left", "Right"}
    """
    out = []
    for side, (inner, sh, el, wr, hd) in arms.items():
        out += [(f"{side}Shoulder", inner, sh, "UpperChest", False),
                (f"{side}UpperArm", sh, el, f"{side}Shoulder", True),
                (f"{side}LowerArm", el, wr, f"{side}UpperArm", True),
                (f"{side}Hand", wr, hd, f"{side}LowerArm", True)]
    for side, (hip, knee, ankle) in legs.items():
        x = ankle[0]
        foot = (x, -foot_fwd, 0.02)
        toe = (x, -toe_fwd, 0.02)
        out += [(f"{side}UpperLeg", hip, knee, "Hips", False),
                (f"{side}LowerLeg", knee, ankle, f"{side}UpperLeg", True),
                (f"{side}Foot", ankle, foot, f"{side}LowerLeg", True),
                (f"{side}Toes", foot, toe, f"{side}Foot", True)]
    return out


def add_socket(name, collection, armature, bone_name, location, size=0.05):
    """Empty preso ao osso (parent tipo BONE) mantendo a posição em mundo."""
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = "ARROWS"
    emp.empty_display_size = size
    collection.objects.link(emp)
    emp.parent = armature
    emp.parent_type = "BONE"
    emp.parent_bone = bone_name
    bpy.context.view_layer.update()
    pb = armature.pose.bones[bone_name]
    parent_mat = armature.matrix_world @ pb.matrix @ Matrix.Translation((0, pb.bone.length, 0))
    emp.matrix_parent_inverse = parent_mat.inverted()
    emp.location = scaled(location)
    return emp


def add_child_empty(name, collection, parent, local_location, shape="SPHERE", size=0.02):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = shape
    emp.empty_display_size = size
    collection.objects.link(emp)
    emp.parent = parent
    emp.location = scaled(local_location)
    return emp


# ---------------------------------------------------------------- cena, export, prévias

def new_scene(name, kind="Characters"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    root = bpy.data.collections.new(f"{KINDS[kind].split('_')[1]}_{name}")
    scene.collection.children.link(root)
    cols = {}
    for key in ("Rig", "Geo", "Sockets", "Props"):
        cols[key] = bpy.data.collections.new(key)
        root.children.link(cols[key])
    cols["Preview"] = bpy.data.collections.new("_Preview")
    scene.collection.children.link(cols["Preview"])
    return scene, cols


def report_tris(objs):
    bpy.context.view_layer.update()
    tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs}
    print("TRIS", tris, "TOTAL", sum(tris.values()))
    return tris


def export_fbx(path):
    """Exporta para o Unity com a frente do personagem em +Z (a frente do Player).

    Medido no Unity (Bake Axis Conversion ligado): o -Y do Blender chega como -Z, então o
    personagem ficava de costas. Giramos as raízes 180° só durante a exportação; o .blend e
    as prévias continuam com o personagem olhando para -Y, como no Art Bible.
    """
    path.parent.mkdir(parents=True, exist_ok=True)
    roots = [o for o in bpy.data.objects if o.parent is None and o.type in {"ARMATURE", "MESH", "EMPTY"}]
    for o in roots:
        o.rotation_euler.z += math.pi
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=False, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", add_leaf_bones=False, primary_bone_axis="Y",
        secondary_bone_axis="X", use_armature_deform_only=False, bake_anim=False,
    )
    for o in roots:
        o.rotation_euler.z -= math.pi
    bpy.context.view_layer.update()


def render_previews(scene, collection, out_dir, prefix, height, frame=None, center_y=0.0):
    """Frente, perfil (câmera em -X, igual ao desenho da ficha), costas e 3/4.

    frame: tamanho do enquadramento (padrão: a altura). Objetos largos ou compridos,
    como o caminhão, passam a maior medida para caber inteiros.
    center_y: onde fica o meio do objeto em Y (o caminhão tem a cabine à frente da origem).
    """
    frame = frame or height
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.display.shading.show_shadows = False
    scene.render.resolution_x = 700
    scene.render.resolution_y = 900
    scene.render.film_transparent = False
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = hex_color("#131118")[:3]
    out_dir.mkdir(parents=True, exist_ok=True)
    target = Vector((0, center_y, height / 2))
    far = max(6.0, frame * 2)
    views = {
        "frente": ((0, center_y - far, height / 2), True),
        "perfil": ((-far, center_y, height / 2), True),
        "costas": ((0, center_y + far, height / 2), True),
        "tres_quartos": ((frame * 1.5, center_y - frame * 2.3, height / 2 + frame * 0.35), False),
    }
    for name, (loc, ortho) in views.items():
        cam_data = bpy.data.cameras.new(f"CAM_{name}")
        if ortho:
            cam_data.type = "ORTHO"
            cam_data.ortho_scale = frame * 1.15
        else:
            cam_data.lens = 85
        cam = bpy.data.objects.new(f"CAM_{name}", cam_data)
        collection.objects.link(cam)
        cam.location = loc
        cam.rotation_euler = (target - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        scene.render.filepath = str(out_dir / f"{prefix}_{name}.png")
        bpy.ops.render.render(write_still=True)


def save_blend(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(path), check_existing=False)


def finish(scene, cols, name, height, kind="Characters", frame=None, center_y=0.0):
    """Exporta o FBX, gera as prévias e salva o .blend nos caminhos padrão do repositório.

    Ex.: kind="Props", name="Caixa" -> Assets/Art/Props/Caixa/SM_PRP_Caixa.fbx
    """
    prefix = KINDS[kind]
    source = SOURCE_ROOT / kind / name
    fbx = UNITY_ROOT / kind / name / f"{prefix}_{name}.fbx"
    blend = source / f"{prefix}_{name}_Blockout.blend"
    export_fbx(fbx)
    render_previews(scene, cols["Preview"], source / "previews", name.lower(), height, frame, center_y)
    save_blend(blend)
    print("SAVED", blend, fbx)
