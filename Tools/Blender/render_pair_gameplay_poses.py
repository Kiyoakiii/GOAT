import bpy, os, math
from mathutils import Vector

project = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
out = os.path.join(project, 'Captures/PairAnimations')
os.makedirs(out, exist_ok=True)
scene = bpy.context.scene
a = bpy.data.objects['GoatRig_A']; b = bpy.data.objects['GoatRig_B']
actors = {a,b,*a.children_recursive,*b.children_recursive}
for o in scene.objects:
    if o.type == 'MESH' and o not in actors: o.hide_render = True
    if o in actors:
        o.hide_render = False
        for mod in o.modifiers:
            if mod.type == 'PARTICLE_SYSTEM': mod.show_render = False
for o in actors:
    if any(t in o.name.lower() for t in ('wool','fur','hair','dropping','grass','pee')): o.hide_render=True
a.location=(0,.51,0); a.rotation_euler=(0,0,0)
b.location=(0,-.51,0); b.rotation_euler=(0,0,math.pi)
for rig in [a,b]:
    rig.animation_data_clear(); rig.animation_data_create()
    for bone in rig.pose.bones: bone.rotation_mode='QUATERNION'
bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.003))
floor=bpy.context.object
mat=bpy.data.materials.new('Preview slate'); mat.diffuse_color=(.12,.16,.19,1); floor.data.materials.append(mat)
cam=scene.camera
cam.location=(2.7,-3,1.8)
cam.rotation_euler=(Vector((0,0,.55))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=2.6
scene.render.engine='BLENDER_EEVEE_NEXT'
scene.render.resolution_x=900; scene.render.resolution_y=650; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.world.color=(.2,.2,.2)
for name,ac,bc,frame in [('push_anticipation','Goat_Push','Goat_Idle',4),('push_contact','Goat_Push','Goat_PushReact',7),('grip','Goat_GrabHold','Goat_GrabbedHold',7),('pull','Goat_Pull','Goat_Hang',7)]:
    a.animation_data.action=bpy.data.actions[ac]; b.animation_data.action=bpy.data.actions[bc]
    scene.frame_set(frame)
    scene.render.filepath=os.path.join(out,name+'.png')
    bpy.ops.render.render(write_still=True)
print('PAIR_PREVIEWS_READY',out)
