import bpy
print('PAIR_SOURCE', bpy.data.filepath)
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        print('RIG', obj.name, 'BONES', [b.name for b in obj.data.bones])
for action in bpy.data.actions:
    print('ACTION', action.name, list(action.frame_range))
