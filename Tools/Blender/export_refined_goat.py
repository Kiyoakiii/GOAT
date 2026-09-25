import bpy
import os

OUT = r'A:/GameDev/Projects/goat/Assets/GoatDescent/Art/RefinedGoat'
ACTIONS = (
    'Goat_Idle', 'Goat_Walk', 'Goat_Jump',
    'Goat_EatGrass', 'Goat_Pee', 'Goat_Poop',
    'Goat_Sequence', 'GoatA_Duo_Performance', 'GoatB_Duo_Performance',
)
os.makedirs(OUT, exist_ok=True)

# Preserve the painted coat texture if it is packed in the Blender file.
painted = bpy.data.images.get('painted')
if painted:
    painted.filepath_raw = OUT + '/GoatPainted.png'
    painted.file_format = 'PNG'
    painted.save()

rig = bpy.data.objects['GoatRig_A']
# Hair particles and Blender-only generated fur are deliberately excluded from
# the game export. The original Blender source remains untouched.
old_fur = bpy.data.objects.get('GoatFur_GameExport')
if old_fur:
    bpy.data.objects.remove(old_fur, do_unlink=True)

rig.animation_data_clear()
rig.animation_data_create()
for action_name in ACTIONS:
    action = bpy.data.actions.get(action_name)
    if action is None:
        raise RuntimeError('Missing animation action: ' + action_name)
    track = rig.animation_data.nla_tracks.new()
    track.name = action_name
    strip = track.strips.new(action_name, 1, action)
    strip.name = action_name
    strip.action_frame_start = action.frame_range[0]
    strip.action_frame_end = action.frame_range[1]
    strip.blend_type = 'REPLACE'
    strip.extrapolation = 'NOTHING'
rig.animation_data.action = None

selected_objects = [rig] + [
    obj for obj in rig.children_recursive
    if obj.type == 'MESH'
    and obj.name not in ('GoatDroppings', 'GoatGrassPatch')
    and not any(tag in obj.name.lower() for tag in ('fur', 'wool', 'hair'))
]
bpy.ops.object.select_all(action='DESELECT')
for obj in selected_objects:
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.select_set(True)
    if obj is not rig and obj.animation_data:
        obj.animation_data_clear()
    for slot in obj.material_slots:
        material = slot.material
        if material and material.use_nodes:
            principled = next((node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED'), None)
            if principled:
                material.diffuse_color = principled.inputs['Base Color'].default_value

rig.location = (0, 0, 0)
rig.rotation_euler = (0, 0, 0)
rig.scale = (1, 1, 1)
bpy.context.view_layer.objects.active = rig
bpy.context.scene.frame_set(1)
fbx = OUT + '/Goat_Duo_Refined.fbx'
bpy.ops.export_scene.fbx(
    filepath=fbx, use_selection=True, object_types={'MESH', 'ARMATURE'},
    add_leaf_bones=False, axis_forward='-Z', axis_up='Y', bake_anim=True,
    bake_anim_use_all_actions=False, bake_anim_use_nla_strips=True,
    bake_anim_simplify_factor=0, path_mode='COPY', embed_textures=True,
)
print('REFINED_GOAT_EXPORTED', os.path.getsize(fbx), 'bytes', 'mesh_objects', len(selected_objects), 'actions', ','.join(ACTIONS))
