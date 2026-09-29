# Goat physical body

Editable Blender source: `A:/GameDev/Blender/Goat_CC0/Goat_Duo_Physics.blend`.
The rig source is `Goat_Duo_Elastic.blend`, generated from the original
`Goat_Duo_Gameplay.blend` with `Tools/Blender/elasticize_goat_rig.py`.
It adds `ElasticMid` and `ElasticChest` to both goats and redistributes body
mesh weights smoothly across the torso. All old bones and the nine pair
actions remain. The game exports are `Goat_Duo_Refined.fbx` (skinned elastic
mesh) and `Goat_PhysicsActions.fbx` (earlier pose prototypes).
Run **Tools > Goat Descent > Import Refined Blender Goat** in Unity after
rebuilding. The importer also restores pair and physics clips, checking that
their bindings match the rig.

`GoatPhysicalBody` creates a simulated rigidbody and joint for each torso,
head and leg segment. Gravity, joint limits, muscle torque and collision
decide the visible pose. It copies the resulting segment transforms onto
the skinned rig each frame. The edge, free fall and eagle clips are kept as
source material but are no longer selected to pose the body.

`GoatInteraction` still owns the physical grip and load transfer. The eagle's
carried goat remains a dynamic Rigidbody connected to a moving anchor by a
ConfigurableJoint, so it can swing under the bird. While the gameplay capsule
is grounded, the extra body colliders act as triggers; this prevents legs
snagging the terrain during turns. They resume physical contacts in the air.

Earlier Blender pose renders are in `Captures/PhysicsAnimations` at the
project root. Play Mode captures of the physical rig are in
`Captures/PhysicalGoat`.
