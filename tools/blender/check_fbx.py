"""Reimporta um FBX num Blender vazio e lista o que veio (conferência rápida antes do Unity).

    blender --background --factory-startup --python tools/blender/check_fbx.py -- caminho/do/arquivo.fbx
"""

import sys

import bpy

path = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)

for obj in sorted(bpy.data.objects, key=lambda o: o.name):
    dims = tuple(round(v, 3) for v in obj.dimensions)
    info = f"{obj.type:9} {obj.name:24} dims={dims}"
    if obj.parent:
        info += f" parent={obj.parent.name}" + (f":{obj.parent_bone}" if obj.parent_bone else "")
    if obj.type == "MESH":
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        groups = [g.name for g in obj.vertex_groups]
        mats = [m.name for m in obj.data.materials]
        info += f" tris={tris} groups={len(groups)} mats={mats}"
    if obj.type == "ARMATURE":
        info += f" bones={[b.name for b in obj.data.bones]}"
    print("FBX", info)
