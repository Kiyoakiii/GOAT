"""Author paired gameplay actions on the user's refined duo rig.
Run with Blender 4.5: blender -b Goat_Duo_Refined.blend --python this_file.py.
The original blend and its existing actions are preserved in a separate source.
"""
import bpy, math, os, json
from mathutils import Euler, Vector, Quaternion

PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
OUT = os.path.join(PROJECT, 'Assets/GoatDescent/Art/RefinedGoat')
SOURCE = 'A:/GameDev/Blender/Goat_CC0/Goat_Duo_Gameplay.blend'
PREVIEW = os.path.join(PROJECT, 'Captures/PairAnimations')
os.makedirs(PREVIEW, exist_ok=True)
rig = bpy.data.objects['GoatRig_A']
partner = bpy.data.objects['GoatRig_B']
rig.animation_data_clear(); rig.animation_data_create()
rig.animation_data.action = bpy.data.actions['Goat_Idle']
bpy.context.scene.frame_set(1)
base = {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in rig.pose.bones}

def pose(head=0, neck=0, spine=0, pelvis=0, front=0, knee=0, hind=0, hock=0, spread=0, ears=0, roll=0, lift=0):
    p = {'Head': (head, 0, 0), 'Neck': (neck, 0, 0), 'Spine': (spine, 0, roll), 'Pelvis': (pelvis, 0, 0)}
    for side, sign in [('L', 1), ('R', -1)]:
        p['FrontUpper.'+side] = (front, 0, sign*spread)
        p['FrontLower.'+side] = (knee, 0, 0)
        p['HindUpper.'+side] = (hind, 0, -sign*spread*.4)
        p['HindLower.'+side] = (hock, 0, 0)
        p['Ear.'+side] = (ears*.25, sign*ears, 0)
    p['_lift'] = lift
    return p

neutral = pose()
brace = pose(head=12, neck=9, spine=3, front=-12, knee=17, hind=7, hock=-5, spread=7, ears=-12, lift=-.025)
hold = pose(head=14, neck=7, spine=2, front=-8, knee=10, hind=6, hock=-4, spread=5, ears=-8, lift=-.012)
grabbed = pose(head=-9, neck=-6, front=-7, knee=8, spread=4, ears=13)

def action(name, keys):
    old = bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    a = bpy.data.actions.new(name); a.use_fake_user = True
    rig.animation_data.action = a
    for frame, values in keys:
        for b in rig.pose.bones:
            loc, rot, scale = base[b.name]
            b.rotation_mode = 'QUATERNION'
            b.location = loc; b.scale = scale
            angles = values.get(b.name, (0,0,0))
            b.rotation_quaternion = rot @ Euler(tuple(math.radians(v) for v in angles), 'XYZ').to_quaternion()
            if b.name == 'Root':
                b.location = loc + b.bone.matrix_local.to_quaternion().inverted() @ Vector((0,0,values.get('_lift',0)))
            b.keyframe_insert('location', frame=frame, group=b.name)
            b.keyframe_insert('rotation_quaternion', frame=frame, group=b.name)
            b.keyframe_insert('scale', frame=frame, group=b.name)
    for curve in a.fcurves:
        for k in curve.keyframe_points:
            k.interpolation = 'BEZIER'; k.handle_left_type = k.handle_right_type = 'AUTO_CLAMPED'
    return a

actions = []
actions.append(action('Goat_Push', [(1,neutral),(4,pose(head=-17,neck=-9,spine=-3,front=-15,knee=15,hind=12,hock=-10,ears=-20,lift=-.035)),(7,pose(head=23,neck=16,spine=5,front=18,knee=-8,hind=-15,hock=10,ears=-22,lift=-.018)),(10,pose(head=16,neck=7,front=8,knee=-3,hind=-4,ears=12)),(17,neutral)]))
actions.append(action('Goat_PushReact', [(1,neutral),(3,pose(head=-21,neck=-10,spine=-8,front=-34,knee=24,hind=15,hock=-12,roll=5,ears=27,lift=.045)),(7,pose(head=13,neck=5,spine=5,front=24,knee=20,hind=-18,hock=16,roll=-4,ears=-12,lift=-.025)),(12,pose(head=-4,front=-6,knee=10,hind=7,ears=10)),(17,neutral)]))
actions.append(action('Goat_GrabStart', [(1,neutral),(4,pose(head=-8,neck=-4,front=-6,knee=8,ears=10)),(7,pose(head=24,neck=14,spine=4,front=11,knee=-4,hind=-5,ears=-17,lift=-.025)),(11,hold)]))
actions.append(action('Goat_GrabbedStart', [(1,neutral),(4,pose(head=-17,neck=-9,front=-12,knee=18,ears=22)),(10,grabbed)]))
actions.append(action('Goat_GrabHold', [(1,hold),(7,brace),(13,hold),(19,pose(head=12,neck=8,front=-9,knee=13,hind=6,spread=5,ears=-4,lift=-.018)),(25,hold)]))
actions.append(action('Goat_GrabbedHold', [(1,grabbed),(13,pose(head=-12,neck=-7,front=-10,knee=11,spread=5,ears=8,lift=-.01)),(25,grabbed)]))
pull_keys = []
for f in [1,7,13,19,25]:
    p = pose(head=-11,neck=-12,spine=-6,front=-13,knee=16,hind=9,hock=-6,spread=7,ears=-16,lift=-.012)
    phase = math.sin((f-1)/24*math.tau)
    for side, sign in [('L',1),('R',-1)]:
        p['FrontUpper.'+side] = (-13+phase*sign*13,0,sign*7)
        p['FrontLower.'+side] = (16+max(0,phase*sign)*11,0,0)
        p['HindUpper.'+side] = (9-phase*sign*11,0,-sign*3)
        p['HindLower.'+side] = (-6-max(0,-phase*sign)*8,0,0)
    pull_keys.append((f,p))
actions.append(action('Goat_Pull', pull_keys))
hang = pose(head=-16,neck=-13,spine=-7,front=-32,knee=35,hind=13,hock=-15,spread=5,ears=24)
actions.append(action('Goat_Hang', [(1,hang),(7,pose(head=-13,neck=-10,spine=-5,front=-22,knee=28,hind=8,hock=-5,ears=17)),(13,hang),(19,pose(head=-18,neck=-12,spine=-6,front=-28,knee=33,hind=16,hock=-12,ears=20)),(25,hang)]))
actions.append(action('Goat_Release', [(1,hold),(4,pose(head=-11,neck=-5,front=-3,knee=5,ears=16)),(9,neutral)]))

scene = bpy.context.scene
scene.render.fps = 24; scene.frame_start = 1; scene.frame_end = 25
rig.animation_data_clear(); rig.animation_data_create(); rig.animation_data.action = bpy.data.actions['Goat_Push']
partner.animation_data_clear(); partner.animation_data_create(); partner.animation_data.action = bpy.data.actions['Goat_PushReact']
rig.location = (0,.51,0); rig.rotation_euler = (0,0,0)
partner.location = (0,-.51,0); partner.rotation_euler = (0,0,math.pi)
for b in partner.pose.bones:
    b.rotation_mode = 'QUATERNION'
for marker in list(scene.timeline_markers): scene.timeline_markers.remove(marker)
for name,frame in [('ANTICIPATION',4),('PUSH CONTACT 0.25s',7),('GRIP CONTACT',7),('RECOVERY',17)]: scene.timeline_markers.new(name,frame=frame)
scene.frame_set(7)
bpy.ops.wm.save_as_mainfile(filepath=SOURCE)

# Export the same hierarchy as the existing model, with only the new actions.
rig.animation_data_clear(); rig.animation_data_create()
for a in actions:
    track = rig.animation_data.nla_tracks.new(); track.name = a.name
    strip = track.strips.new(a.name,1,a); strip.name = a.name
    strip.action_frame_start, strip.action_frame_end = a.frame_range
    strip.blend_type = 'REPLACE'; strip.extrapolation = 'NOTHING'
objects = [rig] + [o for o in rig.children_recursive if o.type == 'MESH' and o.name not in ('GoatDroppings','GoatGrassPatch') and not any(t in o.name.lower() for t in ('fur','wool','hair'))]
bpy.ops.object.select_all(action='DESELECT')
for o in objects:
    o.hide_set(False); o.hide_viewport=False; o.select_set(True)
    if o != rig and o.animation_data: o.animation_data_clear()
rig.location = (0,0,0); rig.rotation_euler=(0,0,0); rig.scale=(1,1,1)
bpy.context.view_layer.objects.active=rig; scene.frame_set(1)
fbx=os.path.join(OUT,'Goat_PairGameplay.fbx')
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
print('PAIR_ANIMATIONS_EXPORTED', json.dumps({'source':SOURCE,'fbx':fbx,'bytes':os.path.getsize(fbx),'actions':[(a.name,list(a.frame_range)) for a in actions]}))
