"""Generate one deterministic low-poly rock for the Unity pipeline infrastructure check."""

from pathlib import Path
import math
import random

import bpy


SEED = 847291
SIDES = 9
ROOT = Path(__file__).resolve().parents[2]
EXPORT_PATH = ROOT / "Assets" / "Art" / "Generated" / "Rocks" / "Infrastructure" / "LowPolyRock_Prototype.fbx"
BLEND_PATH = ROOT / "Tools" / "Blender" / "Generated" / "LowPolyRock_Prototype.blend"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in bpy.data.meshes:
        bpy.data.meshes.remove(mesh)


def create_rock():
    rng = random.Random(SEED)
    vertices = []
    faces = []

    # Three broad rings make a grounded boulder rather than a pointed mountain peak.
    # The bottom sits at y=0 so Unity can put every instance directly on Terrain.SampleHeight.
    for ring_index, (height, radius_scale) in enumerate(((0.02, 1.08), (0.30, 1.20), (0.82, 0.68))):
        for side in range(SIDES):
            angle = (math.tau * side / SIDES) + (0.09 if ring_index else 0.0)
            radius = radius_scale * rng.uniform(0.77, 1.18)
            vertices.append((math.cos(angle) * radius, height + rng.uniform(-0.1, 0.1), math.sin(angle) * radius))

    for side in range(SIDES):
        next_side = (side + 1) % SIDES
        lower = side
        lower_next = next_side
        upper = SIDES + side
        upper_next = SIDES + next_side
        cap = SIDES * 2 + side
        cap_next = SIDES * 2 + next_side
        faces.append((lower, lower_next, upper_next, upper))
        faces.append((upper, upper_next, cap_next, cap))

    faces.append(tuple(range(SIDES * 2, SIDES * 3)))
    faces.append(tuple(reversed(range(SIDES))))

    mesh = bpy.data.meshes.new("LowPolyRock_Prototype_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    color_attribute = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    for polygon in mesh.polygons:
        tone = 0.78 + 0.16 * ((polygon.index * 17 + SEED) % 7) / 6.0
        color = (0.24 * tone, 0.31 * tone, 0.20 * tone, 1.0)
        for loop_index in polygon.loop_indices:
            color_attribute.data[loop_index].color = color

    rock = bpy.data.objects.new("LowPolyRock_Prototype", mesh)
    bpy.context.collection.objects.link(rock)
    rock.rotation_euler[2] = math.radians(-3.0)

    material = bpy.data.materials.new("LowPolyRock_VertexColor")
    material.diffuse_color = (0.28, 0.36, 0.23, 1.0)
    rock.data.materials.append(material)

    for polygon in mesh.polygons:
        polygon.use_smooth = False

    bpy.context.view_layer.objects.active = rock
    rock.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return rock


def export_rock(rock):
    EXPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    bpy.ops.object.select_all(action="DESELECT")
    rock.select_set(True)
    bpy.context.view_layer.objects.active = rock
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_PATH),
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="OFF",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="AUTO",
    )
    print(f"PROCEDURAL_WORLD_BLENDER_ROCK_OK seed={SEED} fbx={EXPORT_PATH} blend={BLEND_PATH}")


if __name__ == "__main__":
    clear_scene()
    export_rock(create_rock())
