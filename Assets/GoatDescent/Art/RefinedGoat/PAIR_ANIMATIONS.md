# Pair gameplay animations

Authored in Blender 4.5.12 using the existing Goat_Duo_Refined.blend rig and its
two-goat scene. Original assets and existing locomotion/duet actions are retained.

Editable source: A:/GameDev/Blender/Goat_CC0/Goat_Duo_Gameplay.blend

Generator: Tools/Blender/build_pair_gameplay_animations.py

Game export: Goat_PairGameplay.fbx (Legacy clips, same hierarchy as the existing
GoatDuoRefined prefab). Import from Tools > Goat Descent > Import Pair Gameplay
Animations. The importer checks every animation binding against the existing rig.

24 fps action timing:

| Clip | Frames | Use |
| --- | --- | --- |
| Goat_Push | 1–17 | Anticipation, horn thrust, recovery; impulse at 0.25 seconds |
| Goat_PushReact | 1–17 | Head recoil, foreleg stagger, recovery |
| Goat_GrabStart | 1–11 | Reach down and engage horns |
| Goat_GrabbedStart | 1–10 | Partner raises head and braces |
| Goat_GrabHold | 1–25 | Rescuer stance and breathing loop |
| Goat_GrabbedHold | 1–25 | Grounded partner hold loop |
| Goat_Pull | 1–25 | Head lift with alternating backward hoof steps |
| Goat_Hang | 1–25 | Hanging partner with bent forelegs and searching feet |
| Goat_Release | 1–9 | Unhook and recover |

These are skeletal actions, blended by the existing Animation component. They
never translate the gameplay Rigidbody. GoatInteraction applies the shove at
the authored contact time and creates the joint after the reach animation.
