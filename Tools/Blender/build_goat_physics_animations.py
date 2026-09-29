"""Author gravity and rescue poses on the existing duo rig without replacing its clips.

Run with Blender 4.5: blender -b Goat_Duo_Elastic.blend --python this_file.py.
The editable result and the Unity FBX are separate from the nine pair actions.
"""
import bpy
import json
import math
import os

from mathutils import Euler, Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
ASSETS = os.path.join(PROJECT, "Assets/GoatDescent/Art/RefinedGoat")
SOURCE = "A:/GameDev/Blender/Goat_CC0/Goat_Duo_Physics.blend"
RIG = bpy.data.objects["GoatRig_A"]
PARTNER = bpy.data.objects["GoatRig_B"]
SCENE = bpy.context.scene

RIG.animation_data_clear()
RIG.animation_data_create()
RIG.animation_data.action = bpy.data.actions["Goat_Idle"]
SCENE.frame_set(1)
BASE = {
    bone.name: (bone.location.copy(), bone.rotation_quaternion.copy(), bone.scale.copy())
    for bone in RIG.pose.bones
}


def pose(root=0, head=0, neck=0, chest=0, spine=0, mid=0, pelvis=0,
         front=0, knee=0, hind=0, hock=0, spread=0, ears=0,
         lift=0, shift=0, tail=0, roll=0, stretch=0):
    result = {
        "Root": (root, 0, roll),
        "Head": (head, 0, 0), "Neck": (neck, 0, 0),
        "Spine": (spine, 0, 0), "Pelvis": (pelvis, 0, 0),
        "ElasticMid": (mid, 0, 0), "ElasticChest": (chest, 0, 0),
        "Tail": (tail, 0, 0), "_lift": lift, "_shift": shift,
        "_stretch": stretch,
    }
    for side, sign in (("L", 1), ("R", -1)):
        result["FrontUpper." + side] = (front, 0, sign * spread)
        result["FrontLower." + side] = (knee, 0, 0)
        result["HindUpper." + side] = (hind, 0, -sign * spread * .55)
        result["HindLower." + side] = (hock, 0, 0)
        result["Ear." + side] = (ears * .25, sign * ears, 0)
    return result


def action(name, keys):
    previous = bpy.data.actions.get(name)
    if previous:
        bpy.data.actions.remove(previous)
    result = bpy.data.actions.new(name)
    result.use_fake_user = True
    RIG.animation_data.action = result
    for frame, values in keys:
        for bone in RIG.pose.bones:
            location, rotation, scale = BASE[bone.name]
            bone.rotation_mode = "QUATERNION"
            bone.location = location
            bone.scale = scale.copy()
            if bone.name in {"Pelvis", "Spine"}:
                bone.scale.y *= 1 + values.get("_stretch", 0)
                bone.scale.z *= 1 - values.get("_stretch", 0) * .3
            angles = values.get(bone.name, (0, 0, 0))
            bone.rotation_quaternion = rotation @ Euler(
                tuple(math.radians(degrees) for degrees in angles), "XYZ"
            ).to_quaternion()
            if bone.name == "Root":
                bone.location = location + bone.bone.matrix_local.to_quaternion().inverted() @ Vector(
                    (0, values.get("_shift", 0), values.get("_lift", 0))
                )
            bone.keyframe_insert("location", frame=frame, group=bone.name)
            bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
            bone.keyframe_insert("scale", frame=frame, group=bone.name)
    for curve in result.fcurves:
        for key in curve.keyframe_points:
            key.interpolation = "BEZIER"
            key.handle_left_type = key.handle_right_type = "AUTO_CLAMPED"
    return result


rear = pose(root=-6, head=8, neck=6, chest=8, spine=12, mid=-22, pelvis=-18,
            front=-19, knee=21, hind=-23, hock=18, spread=5, ears=12,
            lift=.035, shift=-.2, tail=-14, stretch=.025)
torso = pose(root=-12, head=20, neck=16, chest=16, spine=20, mid=-36, pelvis=-34,
             front=-35, knee=39, hind=-15, hock=26, spread=8, ears=20,
             lift=-.015, shift=-.7, tail=-18, stretch=.055)
edge_hang = pose(root=-22, head=26, neck=25, chest=22, spine=24, mid=-38, pelvis=-42,
                 front=-43, knee=49, hind=-9, hock=17, spread=12, ears=26,
                 lift=-.07, shift=-1.4, tail=-20, stretch=.08)
brace = pose(root=-2, head=8, neck=8, chest=-4, spine=-7, mid=-7, pelvis=-9,
             front=-26, knee=33, hind=16, hock=-12, spread=11,
             ears=16, lift=-.04, tail=13, stretch=.03)
grip_strain = pose(root=-5, head=18, neck=20, chest=-14, spine=-18, mid=-20, pelvis=-12,
                   front=-51, knee=58, hind=-15, hock=25, spread=14,
                   ears=26, lift=-.02, tail=-18, stretch=.05)
fall = pose(root=-5, head=-16, neck=-5, chest=-7, spine=-9, mid=-9, pelvis=-8,
            front=-48, knee=49, hind=27, hock=-27, spread=18, ears=27,
            lift=.02, tail=23, stretch=.035)
carry = pose(root=-8, head=20, neck=22, chest=-18, spine=-21, mid=-22, pelvis=-15,
             front=-33, knee=29, hind=-10, hock=17, spread=12,
             ears=21, lift=-.06, tail=-24, stretch=.07)

ACTIONS = [
    action("Goat_EdgeRearSlip", [(1, rear), (9, pose(root=-7, head=9, neck=7, chest=9,
                                                   spine=13, mid=-24, pelvis=-20,
                                                   front=-22, knee=24, hind=-27, hock=21,
                                                   spread=6, ears=16, lift=.025, shift=-.23,
                                                   tail=-18, stretch=.03)), (25, rear)]),
    action("Goat_EdgeTorsoDrop", [(1, torso), (10, pose(root=-14, head=23, neck=18,
                                                      chest=18, spine=22, mid=-38, pelvis=-37,
                                                      front=-39, knee=42, hind=-18,
                                                      hock=29, spread=9, ears=23, lift=-.025,
                                                      shift=-.76,
                                                      tail=-19, stretch=.065)), (25, torso)]),
    action("Goat_EdgeHang", [(1, edge_hang), (12, pose(root=-24, head=29, neck=27,
                                                     chest=24, spine=26, mid=-41, pelvis=-45,
                                                     front=-46, knee=52,
                                                     hind=-10, hock=18, spread=13, ears=27,
                                                     lift=-.085, shift=-1.46,
                                                     tail=-23, stretch=.09)), (25, edge_hang)]),
    action("Goat_RescueBrace", [(1, brace), (11, pose(root=-3, head=9, neck=9,
                                                    chest=-5, spine=-8, mid=-8, pelvis=-10,
                                                    front=-29, knee=36,
                                                    hind=19, hock=-13, spread=12, ears=19,
                                                    lift=-.05, tail=15, stretch=.04)), (25, brace)]),
    action("Goat_GripStrain", [(1, grip_strain), (10, pose(root=-6, head=20,
                                                     neck=22, chest=-16, spine=-20,
                                                     mid=-22, pelvis=-14,
                                                     front=-56, knee=60, hind=-17, hock=27,
                                                     spread=15, ears=29, lift=-.035,
                                                     tail=-22, stretch=.065)), (25, grip_strain)]),
    action("Goat_FreeFall", [(1, fall),
                             (7, pose(root=-3, head=-11, neck=-8, chest=-6, spine=-8,
                                      mid=-8, pelvis=-7,
                                      front=-30, knee=28, hind=13, hock=-13,
                                      spread=20, ears=23, roll=7, tail=27, stretch=.025)),
                             (13, pose(root=-9, head=-23, neck=-17, chest=-10, spine=-13,
                                       mid=-13, pelvis=-11, front=-57, knee=55, hind=37,
                                       hock=-34, spread=21, ears=29, roll=-8,
                                       tail=18, stretch=.05)),
                             (19, pose(root=-4, head=-15, neck=-10, chest=-7, spine=-10,
                                       mid=-10, pelvis=-8, front=-35, knee=35, hind=19,
                                       hock=-19, spread=19, ears=24, roll=5,
                                       tail=25, stretch=.03)), (25, fall)]),
    action("Goat_EagleCarry", [(1, carry),
                               (7, pose(root=-9, head=23, neck=24, chest=-20, spine=-23,
                                        mid=-24, pelvis=-17, front=-40, knee=33, hind=-12,
                                        hock=20, spread=13, ears=24, lift=-.075,
                                        roll=8, tail=-27, stretch=.08)),
                               (13, pose(root=-7, head=17, neck=20, chest=-16, spine=-19,
                                         mid=-20, pelvis=-13, front=-28, knee=26, hind=-8,
                                         hock=14, spread=11, ears=18, lift=-.05,
                                         roll=-7, tail=-21, stretch=.06)),
                               (19, pose(root=-9, head=22, neck=23, chest=-20, spine=-23,
                                         mid=-24, pelvis=-17, front=-37, knee=32, hind=-12,
                                         hock=20, spread=13, ears=23, lift=-.07,
                                         roll=6, tail=-26, stretch=.08)), (25, carry)]),
]

SCENE.render.fps = 24
SCENE.frame_start = 1
SCENE.frame_end = 25
RIG.animation_data.action = bpy.data.actions["Goat_EdgeRearSlip"]
PARTNER.animation_data_clear()
PARTNER.animation_data_create()
PARTNER.animation_data.action = bpy.data.actions["Goat_GripStrain"]
RIG.location = (0, .51, 0)
RIG.rotation_euler = (0, 0, 0)
PARTNER.location = (0, -.51, 0)
PARTNER.rotation_euler = (0, 0, math.pi)
SCENE.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=SOURCE)

RIG.animation_data_clear()
RIG.animation_data_create()
for clip in ACTIONS:
    track = RIG.animation_data.nla_tracks.new()
    track.name = clip.name
    strip = track.strips.new(clip.name, 1, clip)
    strip.name = clip.name
    strip.action_frame_start, strip.action_frame_end = clip.frame_range
    strip.blend_type = "REPLACE"
    strip.extrapolation = "NOTHING"
# One body mesh keeps the same imported rig path as the gameplay prefab.
# The small eye meshes generate polygon warnings but are not needed by the clips.
objects = [RIG, bpy.data.objects["goat_0"]]
bpy.ops.object.select_all(action="DESELECT")
for obj in objects:
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.select_set(True)
    if obj != RIG and obj.animation_data:
        obj.animation_data_clear()
RIG.location = (0, 0, 0)
RIG.rotation_euler = (0, 0, 0)
RIG.scale = (1, 1, 1)
bpy.context.view_layer.objects.active = RIG
SCENE.frame_set(1)
fbx = os.path.join(ASSETS, "Goat_PhysicsActions.fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx, use_selection=True, object_types={"MESH", "ARMATURE"},
    add_leaf_bones=False, axis_forward="-Z", axis_up="Y",
    bake_anim=True, bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=True, bake_anim_simplify_factor=0,
    path_mode="COPY", embed_textures=True,
)
print("GOAT_PHYSICS_ANIMATIONS_EXPORTED", json.dumps({
    "source": SOURCE, "fbx": fbx, "bytes": os.path.getsize(fbx),
    "actions": [(clip.name, list(clip.frame_range)) for clip in ACTIONS],
}))
