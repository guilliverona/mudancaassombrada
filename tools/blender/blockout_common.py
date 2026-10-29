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
from mathutils import Euler, Matrix, Quaternion, Vector

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

    def box(self, center, size, mat, bone, rot_y=0.0, rot_z=0.0):
        """rot_y / rot_z: rotação em graus em volta do próprio centro (rachaduras, porta entreaberta)."""
        m = (Matrix.Scale(K, 4) @ Matrix.Translation(center)
             @ Matrix.Rotation(math.radians(rot_z), 4, "Z")
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

    def hand(self, side, arm_pts, spec, mat, bone_prefix=None, nail=None, grow=0.0, knuckle_pad=None):
        """Mão da ficha "cabeça e mãos": palma, quatro dedos e polegar, cada dedo um tubo
        contínuo pelas três falanges (o peso passa de um osso para o outro na junta, então o
        dedo dobra sem abrir). Posições em metros reais (já com a escala), vindas de hand_chains.

        side: "Left"/"Right"; arm_pts: o ARMS[side] do script; spec: medidas da ficha (ver hand_chains).
        nail: material das unhas (None = sem unha, ex.: luva). grow: engrossa tudo (casca da luva).
        knuckle_pad: material de um reforço sobre os nós, no dorso (luva do Marcelo).
        """
        wrist, a, t, n = hand_frame(side, arm_pts)
        chains = hand_chains(side, arm_pts, spec)
        hand_bone = f"{bone_prefix or side}Hand"
        before = set(self.bm.faces)   # para virar as normais só do que esta mão criou (ver o fim)
        mi = self._index(self.mats, mat)
        hg = self._index(self.groups, hand_bone)

        # Palma: esfera "quadrada" (superelipsoide): cantos redondos, quase uma caixa.
        palm = spec["length"] * (1 - spec["middle"])
        w0, w1 = spec["wrist"] + 2 * grow, spec["knuckles"] + 2 * grow
        thick = spec["thick"] + 2 * grow
        sph = bmesh.ops.create_uvsphere(self.bm, u_segments=12, v_segments=8, radius=1.0)["verts"]
        sq = lambda c, e: math.copysign(abs(c) ** e, c)
        for v in sph:
            x, y, z = sq(v.co.x, 0.45), sq(v.co.y, 0.45), sq(v.co.z, 0.8)
            width = w0 + (w1 - w0) * (y + 1) / 2
            v.co = (wrist + a * ((y + 1) / 2 * (palm + 0.006 + grow) - 0.006 - grow)
                    + t * (x * width / 2) + n * (z * thick / 2))
            v[self.deform][hg] = 1.0
        for f in {f for v in sph for f in v.link_faces}:
            f.material_index, f.smooth = mi, True

        seg, per_bone = 8, 3
        for finger, pts in chains.items():
            names = [f"{bone_prefix or side}{finger}{p}" for p in ("Proximal", "Intermediate", "Distal")]
            gis = [self._index(self.groups, nm) for nm in names]
            r0 = spec["radius"][finger] + grow
            P = [p.copy() for p in pts]
            P[0] = P[0] - (P[1] - P[0]).normalized() * 0.01  # nasce dentro da palma: sem fresta no nó
            us = [k + i / per_bone for k in range(3) for i in range(per_bone)] + [3.0]

            def point(u):
                k = min(int(u), 2)
                return P[k].lerp(P[k + 1], u - k)

            def direction(u):
                k = min(int(u), 2)
                d = (P[k + 1] - P[k]).normalized()
                if u - k < 1e-6 and k > 0:   # na junta, a média das duas falanges
                    d = (d + (P[k] - P[k - 1]).normalized()).normalized()
                return d

            def weights(u):
                for j in (1, 2):
                    if abs(u - j) <= 0.3:
                        s = (u - (j - 0.3)) / 0.6
                        return ((gis[j - 1], 1 - s), (gis[j], s))
                return ((gis[min(int(u), 2)], 1.0),)

            rings = []
            for u in us:
                d = direction(u)
                up = (n - d * n.dot(d)).normalized()   # "cima" do anel = lado da palma (o tubo não torce)
                lat = d.cross(up)
                bulge = 1.0 + spec.get("knuckle_bulge", 0.04) * (1 - 2 * min(abs(u - round(u)), 0.5))
                r = r0 * (1.0 - 0.10 * u / 3.0) * bulge
                c = point(u)
                ring = []
                for s in range(seg):
                    ang = 2 * math.pi * s / seg
                    v = self.bm.verts.new(c + up * math.cos(ang) * r * 0.88 + lat * math.sin(ang) * r)
                    for gi, w in weights(u):
                        v[self.deform][gi] = w
                    ring.append(v)
                rings.append(ring)
            # ponta arredondada
            d = direction(3.0); up = (n - d * n.dot(d)).normalized(); lat = d.cross(up)
            for i in (1, 2):
                ang = math.pi / 2 * i / 3
                ring = []
                for s in range(seg):
                    aa = 2 * math.pi * s / seg
                    v = self.bm.verts.new(P[3] + d * r * math.sin(ang)
                                          + (up * math.cos(aa) * 0.88 + lat * math.sin(aa)) * r * math.cos(ang))
                    v[self.deform][gis[2]] = 1.0
                    ring.append(v)
                rings.append(ring)
            tip = self.bm.verts.new(P[3] + d * r); tip[self.deform][gis[2]] = 1.0
            base = self.bm.verts.new(P[0]); base[self.deform][gis[0]] = 1.0
            faces = []
            for A, B in zip(rings, rings[1:]):
                for s in range(seg):
                    faces.append(self.bm.faces.new((A[s], A[(s + 1) % seg], B[(s + 1) % seg], B[s])))
            for s in range(seg):
                faces.append(self.bm.faces.new((rings[-1][s], rings[-1][(s + 1) % seg], tip)))
                faces.append(self.bm.faces.new((rings[0][(s + 1) % seg], rings[0][s], base)))
            for f in faces:
                f.material_index, f.smooth = mi, True

            # unha: placa arredondada no dorso da falange distal
            if nail:
                ni = self._index(self.mats, nail)
                d = (P[3] - P[2]).normalized()
                back = -(n - d * n.dot(d)).normalized()
                lat = d.cross(back)
                length = (P[3] - P[2]).length * 0.55
                center = P[3] - d * length * 0.55 + back * r * 0.80
                m = Matrix((lat * r * 0.72, d * length * 0.5, back * r * 0.22)).transposed().to_4x4()
                m.translation = center
                vs = bmesh.ops.create_uvsphere(self.bm, u_segments=8, v_segments=4, radius=1.0, matrix=m)["verts"]
                for v in vs:
                    v[self.deform][gis[2]] = 1.0
                for f in {f for v in vs for f in v.link_faces}:
                    f.material_index, f.smooth = ni, True

        # reforço sobre os nós (dorso), preso à mão
        if knuckle_pad:
            ki = self._index(self.mats, knuckle_pad)
            center = wrist + a * (palm * 0.85) - n * (thick / 2 + 0.002)
            m = Matrix((t * w1 * 0.5, a * palm * 0.16, -n * 0.008)).transposed().to_4x4()
            m.translation = center
            vs = bmesh.ops.create_uvsphere(self.bm, u_segments=12, v_segments=4, radius=1.0, matrix=m)["verts"]
            for v in vs:
                v[self.deform][hg] = 1.0
            for f in {f for v in vs for f in v.link_faces}:
                f.material_index, f.smooth = ki, True

        # Normais para fora. A palma e os dedos são montados à mão (eixos t, a, n), e dependendo
        # do lado a ordem dos vértices fica invertida: o Unity só desenha a frente da face e a
        # mão aparecia "oca" (as prévias do Blender desenham os dois lados e escondem isso).
        bmesh.ops.recalc_face_normals(self.bm, faces=[f for f in self.bm.faces if f not in before])

    def cut_front_opening(self, faces, half_width):
        """Abre a frente de peças tipo colete (faces com centro na frente e |x| pequeno)."""
        kill = [f for f in faces if f.is_valid and f.calc_center_median().y < 0
                and abs(f.calc_center_median().x) < half_width * K]
        bmesh.ops.delete(self.bm, geom=kill, context="FACES")

    def cut_below(self, faces, z):
        """Remove as faces abaixo de z (domos: boné, gorro)."""
        kill = [f for f in faces if f.is_valid and f.calc_center_median().z < z * K]
        bmesh.ops.delete(self.bm, geom=kill, context="FACES")

    def cloth_box(self, center, size, mat, bone="root", cuts=8, radius=0.05):
        """Caixa de PANO: cortada em grade (cuts por aresta), para o wrinkle ter vértices onde
        dobrar, e com as quinas arredondadas (radius, em metros). Pano esticado sobre uma quina
        nunca faz canto vivo. Base dos lençóis sobre poltrona, cômoda...

        O arredondamento é feito na própria malha (e não com o modificador de bisel, que corta a
        largura pelo tamanho da célula da grade): as linhas da grade são puxadas para perto das
        bordas, e cada vértice é empurrado para a "caixa de cantos redondos"."""
        before = set(self.bm.faces)
        self.box(center, size, mat, bone)
        new = [f for f in self.bm.faces if f not in before]
        edges = list({e for f in new for e in f.edges})
        bmesh.ops.subdivide_edges(self.bm, edges=edges, cuts=cuts, use_grid_fill=True)
        faces = {f for f in self.bm.faces if f not in before}
        c = Vector(center) * K
        half = Vector(size) * (K / 2)
        r = min(radius * K, min(half) * 0.9)
        inner = half - Vector((r, r, r))
        for v in {v for f in faces for v in f.verts}:
            p = v.co - c
            # grade mais densa perto das bordas (é onde a curva precisa de vértices)
            p = Vector([math.copysign(abs(p[i] / half[i]) ** 0.55, p[i]) * half[i] for i in range(3)])
            q = Vector([max(-inner[i], min(inner[i], p[i])) for i in range(3)])
            d = p - q
            if d.length > 1e-9:
                p = q + d.normalized() * r
            v.co = c + p
        for f in faces:
            f.smooth = True
        return faces

    def wrinkle(self, faces, amount, scale=0.12, seed=0.0, stretch_z=1.0):
        """Dobras de pano: empurra cada vértice destas faces para fora ou para dentro, com um
        ruído suave (dobras de ~scale metros). Tira a cara de "massinha" dos lençóis e estofados.
        Só funciona em peças com muitos vértices (elipsoides, tubos), não em caixas.
        stretch_z > 1 estica as dobras na altura: pregas de pano caindo (barra de lençol, cortina).
        Os vértices repetidos (peças empilhadas) são soldados antes, para a dobra não abrir degrau."""
        from mathutils import noise
        verts = list({v for f in faces if f.is_valid for v in f.verts})
        bmesh.ops.remove_doubles(self.bm, verts=verts, dist=0.0005 * K)
        self.bm.normal_update()
        verts = {v for f in faces if f.is_valid for v in f.verts}
        moves = {}
        for v in verts:
            c = v.co / (scale * K)
            p = Vector((c.x, c.y, c.z / stretch_z)) + Vector((seed, seed * 1.7, seed * 2.3))
            # ruído "turbulento": vincos (vales finos) e não só ondas redondas
            n = noise.turbulence(p, 3, True, noise_basis="PERLIN_ORIGINAL") - 0.45
            moves[v] = v.normal * (n * amount * K)
        for v, d in moves.items():
            v.co += d

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

    def to_static(self, collection, bevel=None):
        """Malha sem rig (móveis, caminhão), com a origem na base, como foi modelada.

        bevel: largura (m) do bisel nas quinas (None = DEFAULT_BEVEL; 0 = sem, para objetos
        miúdos como a cartela de pilhas, em que o bisel engoliria os detalhes de papel). Quina
        viva de caixa não pega luz e o móvel parece de massinha; um bisel de 1 a 2 cm faz o fio
        da quina brilhar. As faces planas continuam planas (harden_normals), e o FBX sai com o
        bisel aplicado.
        """
        bevel = DEFAULT_BEVEL if bevel is None else bevel
        obj = bpy.data.objects.new(self.name, self._mesh())
        collection.objects.link(obj)
        if bevel > 0:
            apply_bevel(obj, bevel)
        metric_uvs(obj)
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


# Bisel que todo to_static usa quando o script não pede outro (0 = sem). Os scripts de móveis e
# objetos ligam (ex.: bc.DEFAULT_BEVEL = 0.006); o kit de paredes e pisos NÃO: peça modular com
# a quina arredondada deixa um sulco escuro em cada emenda.
DEFAULT_BEVEL = 0.0

# Materiais que guardam UV próprio e não podem ganhar o UV em metros: o espelho (o shader do
# reflexo usa o UV 0..1), vidros, lentes e o que brilha (sem textura de detalhe mesmo).
KEEP_UV = ("Espelho", "Vidro", "Lente", "Lampada", "Chama", "Farol")


def metric_uvs(obj):
    """UV em METROS, projetado de cada face pelo eixo mais perto da normal (caixa / triplanar).
    Assim a textura de detalhe do Unity (madeira, tecido...) tem o mesmo tamanho em qualquer peça:
    uma tábua de 2 m e um botão de 2 cm não esticam o veio igual. Feito depois do bisel."""
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv = mesh.uv_layers.active.data
    keep = {i for i, m in enumerate(mesh.materials) if m and any(k in m.name for k in KEEP_UV)}
    for poly in mesh.polygons:
        if poly.material_index in keep:
            continue
        n = poly.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        coords = []
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            if ax == 0:
                u, v = co.y * math.copysign(1, n.x), co.z
            elif ax == 1:
                u, v = -co.x * math.copysign(1, n.y), co.z
            else:
                u, v = co.x, co.y * math.copysign(1, n.z)
            coords.append((li, u, v))
        # O veio da madeira corre em U: U vai no lado MAIS COMPRIDO da face (ao longo da tábua,
        # de pé na perna da mesa). Sem isso, o veio do assoalho atravessava as tábuas.
        span_u = max(c[1] for c in coords) - min(c[1] for c in coords)
        span_v = max(c[2] for c in coords) - min(c[2] for c in coords)
        swap = span_v > span_u * 1.05
        for li, u, v in coords:
            uv[li].uv = (v, u) if swap else (u, v)


def apply_bevel(obj, width, segments=2, angle=35.0):
    """Bisel só nas quinas mais fechadas que `angle` (as curvas das esferas ficam como estão).
    Aplicado de vez na malha: o .blend e o FBX ficam iguais, sem modificador para esquecer."""
    for poly in obj.data.polygons:
        poly.use_smooth = True   # o harden_normals precisa; ele mesmo deixa as faces planas retas
    mod = obj.modifiers.new("Bisel", "BEVEL")
    mod.width = width * K
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(angle)
    mod.harden_normals = True
    mod.miter_outer = "MITER_ARC"
    mod.use_clamp_overlap = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)


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


# ---------------------------------------------------------------- mãos com dedos
#
# As fichas "cabeça e mãos" dão o comprimento da mão (punho até a ponta do médio), quanto
# dele é o dedo médio, a largura da palma e o jeito dos dedos. Os pivôs são os da ficha:
# metacarpo (o nó, na ponta da palma), proximal, média e distal. Os ossos seguem os nomes
# do Unity Humanoid (LeftIndexProximal...), que o Avatar reconhece sozinho.
#
# spec (metros reais): length, middle (fração do médio), knuckles / wrist (largura da palma
# nos nós e no punho), thick (espessura), radius {dedo: raio}, ratio {dedo: comprimento em
# relação ao médio}, drop {dedo: quanto a base desce}, splay {dedo: graus de afastamento do médio},
# thumb_len, phalanges (frações das três falanges), knuckle_bulge (nó mais grosso).

FINGERS = ("Thumb", "Index", "Middle", "Ring", "Little")


def hand_frame(side, arm_pts):
    """Eixos da mão em repouso (braço caído): a = para os dedos, t = para o lado do polegar
    (a frente, -Y), n = para a palma (que olha para o corpo). Devolve (punho, a, t, n)."""
    _, _, _, wr, hd = arm_pts
    wrist, tip = scaled(wr), scaled(hd)
    a = (tip - wrist).normalized()
    t = Vector((0, -1, 0))
    t = (t - a * t.dot(a)).normalized()
    n = a.cross(t)
    inward = Vector((-math.copysign(1.0, wrist.x), 0, 0))
    if n.dot(inward) < 0:
        n = -n
    return wrist, a, t, n


def hand_chains(side, arm_pts, spec):
    """{dedo: [base, junta 1, junta 2, ponta]} em metros reais."""
    wrist, a, t, n = hand_frame(side, arm_pts)
    length, middle = spec["length"], spec["length"] * spec["middle"]
    palm = length - middle
    knuckles = spec["knuckles"]
    phal = spec.get("phalanges", (0.44, 0.31, 0.25))
    out = {}
    for i, finger in enumerate(("Index", "Middle", "Ring", "Little")):
        across = (0.375 - 0.25 * i) * knuckles            # do indicador (lado do polegar) ao mindinho
        base = wrist + a * (palm - spec["drop"].get(finger, 0.0)) + t * across
        # splay: quanto o dedo se afasta do médio. Girar "a" em volta de n por +θ leva para +t
        # (n = a × t), então o sinal do giro é o do lado em que o dedo está.
        d = (Quaternion(n, math.radians(spec["splay"][finger] * math.copysign(1.0, across))) @ a).normalized()
        size = middle * spec["ratio"][finger]
        pts = [base]
        for f in phal:
            pts.append(pts[-1] + d * size * f)
        out[finger] = pts
    # polegar: sai do lado da palma, perto do punho, aberto em diagonal e um pouco para a palma
    base = wrist + a * (palm * 0.24) + t * (spec["wrist"] / 2 - spec["radius"]["Thumb"] * 0.5) + n * spec["thick"] * 0.2
    d = (a * math.cos(math.radians(38)) + t * math.sin(math.radians(38)) + n * 0.3).normalized()
    pts = [base]
    for f in spec.get("thumb_phalanges", (0.40, 0.33, 0.27)):
        pts.append(pts[-1] + d * spec["thumb_len"] * f)
    out["Thumb"] = pts
    return out


def add_finger_bones(armature, arms, specs):
    """Ossos dos dedos (três por dedo), filhos do osso Hand. specs: {lado: spec}."""
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    eb = armature.data.edit_bones
    for side, pts_arm in arms.items():
        _, _, _, n = hand_frame(side, pts_arm)
        for finger, pts in hand_chains(side, pts_arm, specs[side]).items():
            parent = eb[f"{side}Hand"]
            for k, part in enumerate(("Proximal", "Intermediate", "Distal")):
                b = eb.new(f"{side}{finger}{part}")
                b.head, b.tail = pts[k], pts[k + 1]
                b.align_roll(n)    # Z para a palma: girar +X fecha o dedo na palma (para quem anima)
                b.parent = parent
                b.use_connect = k > 0
                parent = b
    bpy.ops.object.mode_set(mode="OBJECT")


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


def export_fbx(path, animation=False):
    """Exporta para o Unity com a frente do personagem em +Z (a frente do Player).

    Medido no Unity (Bake Axis Conversion ligado): o -Y do Blender chega como -Z, então o
    personagem ficava de costas. Giramos as raízes 180° só durante a exportação; o .blend e
    as prévias continuam com o personagem olhando para -Y, como no Art Bible.

    animation=True: só o esqueleto (e vazios), com a ação dele (clipe para Assets/Art/Animations).
    """
    path.parent.mkdir(parents=True, exist_ok=True)
    kinds = {"ARMATURE", "EMPTY"} if animation else {"ARMATURE", "MESH", "EMPTY"}
    roots = [o for o in bpy.data.objects if o.parent is None and o.type in kinds]
    for o in roots:
        o.rotation_euler.z += math.pi
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=False, object_types=kinds,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", add_leaf_bones=False, primary_bone_axis="Y",
        secondary_bone_axis="X", use_armature_deform_only=False, bake_anim=animation,
        bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
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
    # Igual ao Unity: só a frente da face aparece (normal virada para dentro = buraco na prévia).
    scene.display.shading.show_backface_culling = True
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


def finish(scene, cols, name, height, kind="Characters", frame=None, center_y=0.0, before_previews=None):
    """Exporta o FBX, gera as prévias e salva o .blend nos caminhos padrão do repositório.

    Ex.: kind="Props", name="Caixa" -> Assets/Art/Props/Caixa/SM_PRP_Caixa.fbx
    before_previews: chamado depois do FBX e antes das prévias (ex.: pôr uma pose só nas fotos).
    """
    prefix = KINDS[kind]
    source = SOURCE_ROOT / kind / name
    fbx = UNITY_ROOT / kind / name / f"{prefix}_{name}.fbx"
    blend = source / f"{prefix}_{name}_Blockout.blend"
    export_fbx(fbx)
    if before_previews:
        before_previews()
    render_previews(scene, cols["Preview"], source / "previews", name.lower(), height, frame, center_y)
    save_blend(blend)
    print("SAVED", blend, fbx)
