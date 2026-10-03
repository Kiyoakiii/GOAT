"""Add two torso joints and smooth weights to the existing paired goat rig.

Run against Goat_Duo_Gameplay.blend. The original file is never overwritten.
The result keeps every old bone name and animation action intact.
"""
import bpy
import math


OUTPUT = "A:/GameDev/Blender/Goat_CC0/Goat_Duo_Elastic.blend"


def influence(y, center, width, maximum):
    t = max(0.0, 1.0 - abs(y - center) / width)
    return maximum * t * t * (3.0 - 2.0 * t)


def add_torso_bones(rig):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = rig.data.edit_bones
    pelvis = bones["Pelvis"]
    spine = bones["Spine"]
    neck = bones["Neck"]
    mid = bones.get("ElasticMid") or bones.new("ElasticMid")
    mid.head = (0, .24, .59)
    mid.tail = spine.head.copy()
    mid.parent = pelvis
    mid.use_connect = False
    chest = bones.get("ElasticChest") or bones.new("ElasticChest")
    chest.head = (0, -.06, .64)
    chest.tail = neck.head.copy()
    chest.parent = spine
    chest.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")


def reweight_mesh(mesh):
    groups = {group.name: group for group in mesh.vertex_groups}
    if not {"Pelvis", "Spine", "Neck"}.issubset(groups):
        return 0
    mid = mesh.vertex_groups.get("ElasticMid") or mesh.vertex_groups.new(name="ElasticMid")
    chest = mesh.vertex_groups.get("ElasticChest") or mesh.vertex_groups.new(name="ElasticChest")
    index_to_name = {group.index: group.name for group in mesh.vertex_groups}
    changed = 0
    for vertex in mesh.data.vertices:
        weights = {index_to_name[entry.group]: entry.weight for entry in vertex.groups}
        pelvis = weights.get("Pelvis", 0.0)
        spine = weights.get("Spine", 0.0)
        neck = weights.get("Neck", 0.0)
        f_mid = influence(vertex.co.y, .18, .40, .70)
        f_chest = influence(vertex.co.y, -.13, .30, .62)
        mid_from_pelvis = pelvis * f_mid
        mid_from_spine = spine * f_mid
        spine -= mid_from_spine
        chest_from_spine = spine * f_chest
        chest_from_neck = neck * f_chest
        assignments = {
            "Pelvis": pelvis - mid_from_pelvis,
            "Spine": spine - chest_from_spine,
            "Neck": neck - chest_from_neck,
            "ElasticMid": mid_from_pelvis + mid_from_spine,
            "ElasticChest": chest_from_spine + chest_from_neck,
        }
        for name, weight in assignments.items():
            if weight > .0001:
                groups.get(name, mid if name == "ElasticMid" else chest).add(
                    [vertex.index], weight, "REPLACE"
                )
            elif name in ("ElasticMid", "ElasticChest"):
                groups.get(name, mid if name == "ElasticMid" else chest).remove([vertex.index])
        if assignments["ElasticMid"] > .001 or assignments["ElasticChest"] > .001:
            changed += 1
    return changed


for rig_name in ("GoatRig_A", "GoatRig_B"):
    rig = bpy.data.objects[rig_name]
    add_torso_bones(rig)
    count = 0
    for child in rig.children_recursive:
        if child.type == "MESH" and any(m.type == "ARMATURE" for m in child.modifiers):
            count += reweight_mesh(child)
    print("ELASTIC_RIG_WEIGHTED", rig_name, count)

bpy.ops.wm.save_as_mainfile(filepath=OUTPUT)
print("ELASTIC_GOAT_SOURCE_READY", OUTPUT)
