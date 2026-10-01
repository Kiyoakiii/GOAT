"""Render the authored rescue, fall and eagle-carry poses for visual review."""
import bpy
import math
import os

from mathutils import Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT = os.path.join(PROJECT, "Captures/PhysicsAnimations")
os.makedirs(OUT, exist_ok=True)
scene = bpy.context.scene
a = bpy.data.objects["GoatRig_A"]
b = bpy.data.objects["GoatRig_B"]
actors = {a, b, *a.children_recursive, *b.children_recursive}
for obj in scene.objects:
    if obj.type == "MESH" and obj not in actors:
        obj.hide_render = True
    if obj in actors:
        obj.hide_render = False
        for modifier in obj.modifiers:
            if modifier.type == "PARTICLE_SYSTEM":
                modifier.show_render = False
for obj in actors:
    if any(token in obj.name.lower() for token in
           ("wool", "fur", "hair", "dropping", "grass", "pee")):
        obj.hide_render = True

a.location = (0, 0, 0)
a.rotation_euler = (0, 0, 0)
b.location = (0, -1.05, 0)
b.rotation_euler = (0, 0, math.pi)
a.animation_data_clear()
a.animation_data_create()
b.animation_data_clear()
b.animation_data_create()
for rig in (a, b):
    for bone in rig.pose.bones:
        bone.rotation_mode = "QUATERNION"

bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -1.0, -.17))
ledge = bpy.context.object
ledge.name = "Preview cliff shelf"
ledge.dimensions = (3.3, 2.55, .34)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
ledge_mat = bpy.data.materials.new("Preview sandstone")
ledge_mat.diffuse_color = (.28, .35, .29, 1)
ledge.data.materials.append(ledge_mat)

camera = scene.camera
camera.location = (2.75, 2.6, 1.55)
camera.rotation_euler = (Vector((0, -.38, .25)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 3.1
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 900
scene.render.resolution_y = 650
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world.color = (.16, .19, .22)

shots = [
    ("edge_rear_slip", "Goat_EdgeRearSlip", "Goat_GrabbedHold", 9, True, .1),
    ("edge_torso_drop", "Goat_EdgeTorsoDrop", "Goat_GrabbedHold", 10, True, .45),
    ("edge_full_hang", "Goat_EdgeHang", "Goat_GrabbedHold", 12, True, .8),
    ("rescue_brace", "Goat_RescueBrace", "Goat_GrabbedHold", 11, True, 0),
    ("grip_strain", "Goat_GripStrain", "Goat_Idle", 10, False, 0),
    ("free_fall", "Goat_FreeFall", "Goat_Idle", 13, False, 0),
    ("eagle_carry", "Goat_EagleCarry", "Goat_Idle", 7, False, 0),
]
only = set(filter(None, os.environ.get("GOAT_PREVIEW_ONLY", "").split(",")))
for filename, action_a, action_b, frame, show_partner, goat_y in shots:
    if only and filename not in only:
        continue
    a.location = (0, goat_y, 0)
    a.animation_data.action = bpy.data.actions[action_a]
    b.animation_data.action = bpy.data.actions[action_b]
    for obj in (b, *b.children_recursive):
        obj.hide_render = not show_partner
    ledge.hide_render = not show_partner
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(OUT, filename + ".png")
    bpy.ops.render.render(write_still=True)
print("GOAT_PHYSICS_PREVIEWS_READY", OUT)
