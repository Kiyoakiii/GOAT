"""Build the deterministic low-poly environment pack used by the Unity world milestone.

Run from the repository root with:
    blender --background --python Tools/Blender/generate_world_asset_pack.py
"""

from pathlib import Path
import math
import random
import sys

import bpy
from mathutils import Vector


SEED = 847291
ROOT = Path(__file__).resolve().parents[2]
EXPORT_ROOT = ROOT / "Assets" / "Art" / "Generated"
BLEND_PATH = ROOT / "Tools" / "Blender" / "Generated" / "ProceduralWorldAssetPack.blend"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)


def material(name, color):
    result = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    return result


def tint_mesh(mesh, color, seed):
    colors = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    for polygon in mesh.polygons:
        variation = 0.82 + 0.18 * ((polygon.index * 31 + seed) % 11) / 10.0
        shaded = tuple(channel * variation for channel in color)
        for loop_index in polygon.loop_indices:
            colors.data[loop_index].color = (*shaded, 1.0)


def make_rock_mesh(name, seed, sides, width, height):
    rng = random.Random(seed)
    vertices, faces = [], []
    rings = ((0.02, 0.92), (height * 0.30, 1.06), (height * 0.86, 0.56))
    for ring_index, (ring_height, radius_scale) in enumerate(rings):
        for side in range(sides):
            angle = math.tau * side / sides + ring_index * 0.10
            radius = width * radius_scale * rng.uniform(0.78, 1.15)
            vertices.append((math.cos(angle) * radius, ring_height + rng.uniform(-0.07, 0.07), math.sin(angle) * radius))
    for side in range(sides):
        next_side = (side + 1) % sides
        faces.append((side, next_side, sides + next_side, sides + side))
        faces.append((sides + side, sides + next_side, sides * 2 + next_side, sides * 2 + side))
    faces.append(tuple(range(sides * 2, sides * 3)))
    faces.append(tuple(reversed(range(sides))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    tint_mesh(mesh, (0.31, 0.34, 0.25), seed)
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return mesh


def make_cliff_mesh(name, seed, width, height, depth, steps):
    rng = random.Random(seed)
    vertices, faces = [], []
    for level in range(steps + 1):
        y = height * level / steps
        inset = (level / steps) * width * 0.18
        for x_sign, z_sign in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            x = x_sign * (width * 0.5 - inset) + rng.uniform(-0.12, 0.12)
            z = z_sign * depth * 0.5 + rng.uniform(-0.16, 0.16)
            vertices.append((x, y + rng.uniform(-0.14, 0.14), z))
    for level in range(steps):
        base = level * 4
        top = (level + 1) * 4
        for side in range(4):
            next_side = (side + 1) % 4
            faces.append((base + side, base + next_side, top + next_side, top + side))
    faces.append(tuple(range(steps * 4, steps * 4 + 4)))
    faces.append((3, 2, 1, 0))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    tint_mesh(mesh, (0.32, 0.30, 0.25), seed)
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return mesh


def link_mesh(root, mesh, name, material_ref):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = root
    obj.data.materials.append(material_ref)
    return obj


def add_cone(root, name, vertices, radius_bottom, radius_top, depth, location, material_ref):
    # Blender's primitive cone is Z-up; the generated meshes use Y as their up axis for Unity.
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_bottom,
        radius2=radius_top,
        depth=depth,
        location=location,
        rotation=(-math.pi * 0.5, 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.parent = root
    obj.data.materials.append(material_ref)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def build_rock_asset(asset_name, dimensions, is_cliff=False):
    root = bpy.data.objects.new(asset_name, None)
    bpy.context.collection.objects.link(root)
    rock_mat = material("M_Rock", (0.31, 0.34, 0.25))
    asset_seed = sum((index + 1) * ord(character) for index, character in enumerate(asset_name))
    for level, multiplier in enumerate((1.0, 0.78, 0.58)):
        if is_cliff:
            mesh = make_cliff_mesh(
                f"{asset_name}_LOD{level}", SEED + asset_seed + level,
                dimensions[0], dimensions[1], dimensions[2], max(1, 3 - level)
            )
        else:
            mesh = make_rock_mesh(
                f"{asset_name}_LOD{level}", SEED + asset_seed + level,
                max(5, int(10 * multiplier)), dimensions[0], dimensions[1]
            )
        link_mesh(root, mesh, f"{asset_name}_LOD{level}", rock_mat)
    return root


def add_tree_segment(root, name, start, end, radius_start, radius_end, sides, trunk_mat):
    """Create one of the tapered, naturally bent branch segments.

    Blender cones grow along local Z.  This pack intentionally uses Y as up so
    the FBX arrives in Unity without the horizontal-tree conversion issue.
    """
    direction = end - start
    if direction.length <= 0.0001:
        return None
    bpy.ops.mesh.primitive_cone_add(
        vertices=sides,
        radius1=radius_start,
        radius2=radius_end,
        depth=direction.length,
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    obj.parent = root
    obj.data.materials.append(trunk_mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def append_tapered_segment(vertices, faces, start, end, radius_start, radius_end, sides):
    direction = end - start
    if direction.length <= 0.0001:
        return
    axis = direction.normalized()
    reference = Vector((0.0, 0.0, 1.0)) if abs(axis.z) < 0.92 else Vector((1.0, 0.0, 0.0))
    side = axis.cross(reference).normalized()
    depth = axis.cross(side).normalized()
    base = len(vertices)
    for center, radius in ((start, radius_start), (end, radius_end)):
        for index in range(sides):
            angle = math.tau * index / sides
            point = center + side * (math.cos(angle) * radius) + depth * (math.sin(angle) * radius)
            vertices.append(tuple(point))
    for index in range(sides):
        following = (index + 1) % sides
        faces.append((base + index, base + following, base + sides + following, base + sides + index))
    faces.append(tuple(reversed(tuple(base + index for index in range(sides)))))
    faces.append(tuple(base + sides + index for index in range(sides)))


def add_leaf_cluster(root, name, position, scale, count_range, rng, leaf_materials):
    """Make the clustered low-poly foliage from the supplied tree generator."""
    leaves = []
    for index in range(rng.randint(*count_range)):
        offset = Vector((
            rng.uniform(-0.45, 0.45),
            rng.uniform(-0.30, 0.45),
            rng.uniform(-0.45, 0.45),
        ))
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=position + offset * scale)
        obj = bpy.context.object
        obj.name = f"{name}_{index}"
        obj.scale = (
            rng.uniform(0.55, 0.90) * scale,
            rng.uniform(0.65, 1.15) * scale,
            rng.uniform(0.55, 0.90) * scale,
        )
        obj.rotation_euler = (
            rng.uniform(0, math.pi),
            rng.uniform(0, math.pi),
            rng.uniform(0, math.pi),
        )
        obj.parent = root
        obj.data.materials.append(leaf_materials[rng.randrange(len(leaf_materials))])
        for polygon in obj.data.polygons:
            polygon.use_smooth = False
        leaves.append(obj)
    return leaves


def join_tree_parts(root, objects, name):
    if not objects:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    joined.parent = root
    return joined


def build_branching_tree_lod(root, asset_name, kind, level, settings, trunk_mat, leaf_materials):
    """Port of the supplied recursive branch algorithm, adapted to Y-up FBX assets."""
    rng = random.Random(settings["seed"] + level * 1609)
    wood_parts, leaf_parts = [], []
    sides = settings["sides"]
    max_depth = settings["max_depth"]
    branch_segments = settings["branch_segments"]

    def create_segment(start, end, radius_start, radius_end):
        segment = add_tree_segment(
            root,
            f"{asset_name}_LOD{level}_WoodPart{len(wood_parts):03d}",
            start, end, radius_start, radius_end, sides, trunk_mat,
        )
        if segment:
            wood_parts.append(segment)

    def child_direction(parent_direction):
        azimuth = rng.uniform(0, math.tau)
        horizontal = Vector((
            math.cos(azimuth),
            rng.uniform(0.15, 0.8),
            math.sin(azimuth),
        ))
        result = parent_direction * rng.uniform(0.15, 0.45) + horizontal
        result.y += rng.uniform(0.15, 0.5)
        return result.normalized()

    def grow_branch(start, direction, length, radius, depth=0, segments=None):
        if segments is None:
            segments = branch_segments
        current = start.copy()
        current_direction = direction.normalized()
        segment_length = length / segments
        last_radius = radius

        for index in range(segments):
            progress = (index + 1) / segments
            bend_strength = 0.08 + depth * 0.035
            current_direction.x += rng.uniform(-bend_strength, bend_strength)
            current_direction.z += rng.uniform(-bend_strength, bend_strength)
            current_direction.y += rng.uniform(0.015, 0.065)
            current_direction.normalize()
            step = segment_length * rng.uniform(0.90, 1.12)
            next_point = current + current_direction * step
            radius_end = max(radius * (1.0 - progress * 0.72), radius * 0.18)
            create_segment(current, next_point, last_radius, radius_end)
            current, last_radius = next_point, radius_end

            if depth < max_depth and index >= 1:
                chance = {0: 0.65, 1: 0.52, 2: 0.35}.get(depth, 0.2)
                if rng.random() < chance:
                    remaining = length * (1.0 - progress)
                    child_length = max(remaining * rng.uniform(0.6, 1.15), length * 0.28)
                    grow_branch(
                        current,
                        child_direction(current_direction),
                        child_length,
                        radius_end * rng.uniform(0.50, 0.70),
                        depth + 1,
                        # The source's BRANCH_SEGMENTS is the intended cap for
                        # child limbs; inheriting the 11 trunk segments here
                        # creates an exponential number of tiny meshes.
                        max(3, min(branch_segments, segments - 1)),
                    )

        if settings["has_leaves"] and depth >= 2:
            leaf_parts.extend(add_leaf_cluster(
                root,
                f"{asset_name}_LOD{level}_Leaves{len(leaf_parts):03d}",
                current,
                settings["leaf_cluster_size"] * rng.uniform(0.7, 1.25),
                settings["leaf_count"],
                rng,
                leaf_materials,
            ))

    trunk_direction = Vector((rng.uniform(-0.04, 0.04), 1, rng.uniform(-0.04, 0.04)))
    grow_branch(Vector((0, 0, 0)), trunk_direction, settings["height"], settings["trunk_radius"], 0, settings["trunk_segments"])
    join_tree_parts(root, wood_parts, f"{asset_name}_LOD{level}_Wood")
    if settings["has_leaves"]:
        join_tree_parts(root, leaf_parts, f"{asset_name}_LOD{level}_Leaves")


def make_tree_material(name, color, vertex_colored=False):
    result = material(name, color)
    result.use_nodes = True
    nodes = result.node_tree.nodes
    links = result.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Roughness"].default_value = 0.78
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    if vertex_colored:
        vertex_color = nodes.new("ShaderNodeVertexColor")
        vertex_color.layer_name = "Col"
        foliage_tint = nodes.new("ShaderNodeMixRGB")
        foliage_tint.blend_type = "MULTIPLY"
        foliage_tint.inputs["Fac"].default_value = 1.0
        foliage_tint.inputs["Color2"].default_value = (0.90, 0.99, 0.91, 1.0)
        links.new(vertex_color.outputs["Color"], foliage_tint.inputs["Color1"])

        frost_amount = nodes.new("ShaderNodeMath")
        frost_amount.operation = "MULTIPLY"
        frost_amount.inputs[1].default_value = 0.84
        links.new(vertex_color.outputs["Alpha"], frost_amount.inputs[0])

        frosted_foliage = nodes.new("ShaderNodeMixRGB")
        frosted_foliage.blend_type = "MIX"
        frosted_foliage.inputs["Color2"].default_value = (0.86, 0.93, 0.99, 1.0)
        links.new(frost_amount.outputs[0], frosted_foliage.inputs["Fac"])
        links.new(foliage_tint.outputs["Color"], frosted_foliage.inputs["Color1"])
        links.new(frosted_foliage.outputs["Color"], shader.inputs["Base Color"])
        emission_color = shader.inputs.get("Emission Color")
        emission_strength = shader.inputs.get("Emission Strength")
        if emission_color:
            links.new(frosted_foliage.outputs["Color"], emission_color)
        if emission_strength:
            emission_strength.default_value = 0.20
    else:
        shader.inputs["Base Color"].default_value = (*color, 1.0)
    return result


def make_tapered_trunk_mesh(name, height, radius, lean, sides, seed):
    rng = random.Random(seed)
    vertices, faces = [], []
    ring_count = max(9, round(height * 1.15))
    for ring in range(ring_count + 1):
        t = ring / ring_count
        y = height * t
        center = Vector((lean[0] * t * t, y, lean[1] * t * t))
        taper = max(0.055, (1.0 - t) ** 1.12)
        ring_radius = radius * taper
        twist = t * 0.12
        for side in range(sides):
            angle = math.tau * side / sides + twist
            bark_ridge = 1.0 + 0.055 * math.sin(angle * 4.0 + t * 8.0)
            bark_noise = rng.uniform(-0.012, 0.012)
            r = ring_radius * (bark_ridge + bark_noise)
            vertices.append((center.x + math.cos(angle) * r, center.y, center.z + math.sin(angle) * r))

    for ring in range(ring_count):
        start = ring * sides
        next_ring = (ring + 1) * sides
        for side in range(sides):
            following = (side + 1) % sides
            faces.append((start + side, start + following, next_ring + following, next_ring + side))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple(range(ring_count * sides, (ring_count + 1) * sides)))

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    return mesh


def append_needle_blade(vertices, faces, colors, start, direction, length, width, color, rng, frost_mask=0.0):
    axis = direction.normalized()
    world_up = Vector((0.0, 1.0, 0.0))
    side = axis.cross(world_up)
    if side.length < 0.001:
        side = axis.cross(Vector((0.0, 0.0, 1.0)))
    side.normalize()
    depth = side.cross(axis).normalized()

    bend = depth * width * rng.uniform(0.06, 0.20)
    mid = start + axis * length * rng.uniform(0.48, 0.62) + bend
    points = (
        start,
        mid - side * width * 0.48,
        mid + side * width * 0.48 + depth * width * 0.16,
        start + axis * length + depth * width * 0.025,
    )
    base_index = len(vertices)
    vertices.extend(tuple(point) for point in points)
    front = ((0, 1, 2), (1, 3, 2))
    for face in front:
        faces.append(tuple(base_index + index for index in face))

    shade = rng.uniform(0.84, 1.12)
    base_color = tuple(min(1.0, channel * shade) for channel in color)
    middle_color = tuple(min(1.0, channel * shade * 0.94) for channel in color)
    tip_color = tuple(min(1.0, channel * min(1.36, shade * 1.22)) for channel in color)
    colors.extend((
        (*base_color, frost_mask),
        (*middle_color, frost_mask),
        (*middle_color, frost_mask),
        (*tip_color, frost_mask),
    ))


def add_needle_spray(vertices, faces, colors, center, axis, scale, palette, rng, needle_count, top_fraction=0.0):
    main_axis = axis.normalized()
    world_up = Vector((0.0, 1.0, 0.0))
    side = main_axis.cross(world_up)
    if side.length < 0.001:
        side = main_axis.cross(Vector((0.0, 0.0, 1.0)))
    side.normalize()
    fan_phase = rng.uniform(0.0, math.tau)

    for index in range(needle_count):
        angle = fan_phase + math.tau * index / needle_count + rng.uniform(-0.12, 0.12)
        lateral = side * math.cos(angle) + world_up * math.sin(angle)
        direction = (main_axis * rng.uniform(0.64, 0.88) + lateral * rng.uniform(0.30, 0.65)
                     + world_up * rng.uniform(-0.10, 0.34)).normalized()
        start = center + lateral * scale * rng.uniform(0.015, 0.13)
        length = scale * rng.uniform(0.60, 0.94)
        # Conifer needles should read as fine sprays, not broad leaf cards.
        # The denser fan layout below restores volume without turning the
        # crown into a set of oversized geometric scales.
        # Broader low-poly sprays read clearly at gameplay distance while
        # retaining the pointed tip that distinguishes conifer foliage.
        width = length * rng.uniform(0.10, 0.18)
        color = palette[rng.choices((0, 1, 2, 3), weights=(0.18, 0.43, 0.31, 0.08), k=1)[0]]
        # Alpine snow should read from gameplay distance, but stay on the
        # upper, exposed sprays instead of whitening the entire crown.
        canopy_exposure = max(0.0, min(1.0, (top_fraction - 0.30) / 0.70))
        upward_exposure = max(0.0, min(1.0, (direction.y - 0.08) / 0.72))
        frost_mask = canopy_exposure * upward_exposure * rng.uniform(0.55, 0.90)
        append_needle_blade(vertices, faces, colors, start, direction, length, width, color, rng, frost_mask)


def build_conifer_lod(root, asset_name, profile, level, trunk_mat, foliage_mat):
    rng = random.Random(profile["seed"] + level * 1609)
    quality = (1.0, 0.64, 0.34)[level]
    height = profile["height"] * (1.0 - level * 0.055)
    tiers = max(5, round(profile["tiers"] * quality))
    branch_count = max(4, round(profile["branches"] * (0.78 + quality * 0.22)))
    fan_count = (7, 5, 4)[level]
    needle_count = (12, 9, 6)[level]
    trunk_sides = (12, 9, 7)[level]
    branch_vertices, branch_faces = [], []
    foliage_vertices, foliage_faces, foliage_colors = [], [], []

    lean_limit = 0.34 if "_Windbent" in asset_name else 0.15
    lean = (rng.uniform(-lean_limit, lean_limit), rng.uniform(-lean_limit * 0.8, lean_limit * 0.8))
    # Let the foliage leader finish the silhouette. Keeping the bare trunk
    # almost to the apex makes a conspicuous brown spike through the sparse
    # low-poly sprays, so stop the wood safely inside the upper crown.
    trunk_top = height * 0.68
    trunk_mesh = make_tapered_trunk_mesh(
        f"{asset_name}_LOD{level}_WoodMesh", trunk_top,
        profile["trunk_radius"], lean, trunk_sides,
        profile["seed"] + 200 + level,
    )
    trunk = link_mesh(root, trunk_mesh, f"{asset_name}_LOD{level}_Wood", trunk_mat)
    for polygon in trunk.data.polygons:
        polygon.use_smooth = False

    for root_index in range(5):
        angle = math.tau * root_index / 5.0 + rng.uniform(-0.12, 0.12)
        radial = Vector((math.sin(angle), 0.0, math.cos(angle)))
        start = Vector((0.0, 0.22, 0.0))
        end = radial * rng.uniform(0.62, 0.94) + Vector((0.0, 0.03, 0.0))
        append_tapered_segment(
            branch_vertices, branch_faces, start, end,
            profile["trunk_radius"] * 0.28, 0.018, max(5, trunk_sides // 2),
        )

    tier_step = (height - profile["crown_start"] - 0.34) / max(1, tiers)
    for tier in range(tiers):
        fraction = (tier + rng.uniform(0.12, 0.78)) / tiers
        y = profile["crown_start"] + fraction * (height - profile["crown_start"] - 0.34)
        top_fraction = (y - profile["crown_start"]) / max(0.001, height - profile["crown_start"])
        taper = max(0.025, 1.0 - top_fraction) ** profile["crown_taper"]
        canopy_radius = profile["crown_radius"] * taper + 0.16
        phase = profile["seed"] * 0.0003 + tier * 2.39996323 + rng.uniform(-0.16, 0.16)
        tier_branches = max(4, branch_count + (1 if tier % 4 == 1 else 0))
        for branch_index in range(tier_branches):
            # Break the obvious circular whorls: branch roots sit at slightly
            # different heights and angles, while their foliage overlaps to
            # keep the outline continuous.
            branch_y = y + rng.uniform(-tier_step * 0.48, tier_step * 0.48)
            branch_center = Vector((
                lean[0] * (branch_y / height) ** 2,
                branch_y,
                lean[1] * (branch_y / height) ** 2,
            ))
            angle = phase + math.tau * branch_index / tier_branches + rng.uniform(-0.30, 0.30)
            radial = Vector((math.sin(angle), 0.0, math.cos(angle)))
            tangent = Vector((radial.z, 0.0, -radial.x))
            length = canopy_radius * rng.uniform(0.78, 1.08)
            droop = profile["droop"] * length * (1.0 - top_fraction * 0.45)
            lift = profile["tip_lift"] * length * (0.68 + top_fraction * 0.45)
            points = (
                branch_center,
                branch_center + radial * length * 0.25 + Vector((0.0, -droop * 0.12, 0.0)),
                branch_center + radial * length * 0.54 + Vector((0.0, -droop, 0.0)),
                branch_center + radial * length * 0.82 + Vector((0.0, -droop * 0.48 + lift * 0.20, 0.0)),
                branch_center + radial * length + Vector((0.0, lift, 0.0)),
            )
            branch_radius = profile["branch_radius"] * max(0.05, 1.0 - top_fraction) ** 0.72 + 0.012
            for segment in range(len(points) - 1):
                append_tapered_segment(
                    branch_vertices, branch_faces,
                    points[segment], points[segment + 1],
                    branch_radius * (1.0 - segment * 0.17), max(0.012, branch_radius * (0.53 - segment * 0.08)),
                    max(5, trunk_sides // 2),
                )

            for fan in range(fan_count):
                t = 0.22 + fan * (0.72 / max(1, fan_count - 1))
                segment = min(len(points) - 2, int(t * (len(points) - 1)))
                segment_t = t * (len(points) - 1) - segment
                center = points[segment].lerp(points[segment + 1], segment_t)
                local_axis = (radial * 0.74 + Vector((0.0, 0.48 + top_fraction * 0.14, 0.0))
                              + tangent * rng.uniform(-0.11, 0.11)).normalized()
                cluster_scale = length * (0.27 - t * 0.055) * rng.uniform(0.88, 1.12)
                add_needle_spray(
                    foliage_vertices, foliage_faces, foliage_colors,
                    center, local_axis, cluster_scale, profile["palette"], rng, needle_count, top_fraction,
                )

            if level < 2:
                split_point = points[2].lerp(points[3], 0.32)
                twig_length = length * (0.32 - top_fraction * 0.08)
                for sign in (-1.0, 1.0):
                    twig_direction = (radial * 0.56 + tangent * sign * 0.66 + Vector((0.0, 0.45, 0.0))).normalized()
                    twig_end = split_point + twig_direction * twig_length
                    append_tapered_segment(
                        branch_vertices, branch_faces,
                        split_point, twig_end, max(0.018, branch_radius * 0.46), 0.012, 5,
                    )
                    add_needle_spray(
                        foliage_vertices, foliage_faces, foliage_colors,
                        split_point.lerp(twig_end, 0.60), twig_direction + radial * 0.24 + Vector((0.0, 0.18, 0.0)),
                        twig_length * 0.86, profile["palette"], rng, max(4, needle_count - 2), top_fraction,
                    )

    leader_center = Vector((lean[0], height - 0.42, lean[1]))
    for fan_index in range(8):
        angle = math.tau * fan_index / 8.0 + rng.uniform(-0.16, 0.16)
        radial = Vector((math.sin(angle), 0.0, math.cos(angle)))
        leader_axis = (radial * 0.34 + Vector((0.0, 0.94, 0.0))).normalized()
        add_needle_spray(
            foliage_vertices, foliage_faces, foliage_colors,
            leader_center + radial * 0.10,
            leader_axis,
            0.58,
            profile["palette"],
            rng,
            needle_count,
            0.76,
        )

    if branch_vertices:
        branch_mesh = bpy.data.meshes.new(f"{asset_name}_LOD{level}_BranchMesh")
        branch_mesh.from_pydata(branch_vertices, [], branch_faces)
        branch_mesh.update()
        branch_tint = profile["branch_color"]
        branch_colors = branch_mesh.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
        branch_colors.data.foreach_set(
            "color",
            [channel for _ in branch_vertices for channel in (*branch_tint, 0.0)],
        )
        branch_mesh.color_attributes.active_color = branch_colors
        branches = link_mesh(root, branch_mesh, f"{asset_name}_LOD{level}_Branches", foliage_mat)
        for polygon in branches.data.polygons:
            polygon.use_smooth = False
    if foliage_vertices:
        foliage_mesh = bpy.data.meshes.new(f"{asset_name}_LOD{level}_NeedleMesh")
        foliage_mesh.from_pydata(foliage_vertices, [], foliage_faces)
        foliage_mesh.update()
        colors = foliage_mesh.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
        colors.data.foreach_set("color", [channel for color in foliage_colors for channel in color])
        foliage_mesh.color_attributes.active_color = colors
        obj = link_mesh(root, foliage_mesh, f"{asset_name}_LOD{level}_Foliage", foliage_mat)
        for polygon in obj.data.polygons:
            polygon.use_smooth = True

    return root


def build_stump_lod(root, asset_name, level, height, radius, trunk_mat):
    rng = random.Random(SEED + 501 + level)
    end = Vector((rng.uniform(-0.12, 0.12), height, rng.uniform(-0.12, 0.12)))
    trunk = add_tree_segment(root, f"{asset_name}_LOD{level}_Wood", Vector((0, 0, 0)), end, radius, radius * 0.72, max(4, 8 - level * 2), trunk_mat)
    if trunk:
        trunk.name = f"{asset_name}_LOD{level}_Wood"


def build_tree_asset(asset_name, kind):
    root = bpy.data.objects.new(asset_name, None)
    bpy.context.collection.objects.link(root)
    trunk_mat = material("M_Tree_Bark", (0.18, 0.07, 0.025))
    leaf_materials = [
        material("M_Leaves_Dark", (0.025, 0.16, 0.035)),
        material("M_Leaves_Mid", (0.05, 0.28, 0.055)),
        material("M_Leaves_Light", (0.10, 0.40, 0.08)),
    ]
    asset_seed = sum((index + 1) * ord(character) for index, character in enumerate(asset_name))

    if kind == "Stump":
        for level, multiplier in enumerate((1.0, 0.82, 0.64)):
            build_stump_lod(root, asset_name, level, 2.2 * multiplier, 0.38 * multiplier, trunk_mat)
        return root

    if kind in ("Pine", "Fir"):
        is_pine = kind == "Pine"
        foliage_mat = make_tree_material("M_Tree_Foliage", (0.08, 0.28, 0.13), vertex_colored=True)
        trunk_mat = make_tree_material("M_Tree_Bark", (0.12, 0.045, 0.020))
        profile = {
            "seed": SEED + asset_seed,
            "height": 15.1 if is_pine else 14.2,
            "trunk_radius": 0.40 if is_pine else 0.32,
            "crown_start": 1.25 if is_pine else 0.80,
            "crown_radius": 4.25 if is_pine else 3.85,
            "crown_taper": 0.90 if is_pine else 0.92,
            "tiers": 19 if is_pine else 21,
            "branches": 9 if is_pine else 9,
            "branch_radius": 0.11 if is_pine else 0.085,
            "droop": 0.17 if is_pine else 0.29,
            "tip_lift": 0.22 if is_pine else 0.10,
            "branch_color": (0.040, 0.130, 0.068) if is_pine else (0.045, 0.145, 0.090),
            "palette": (
                (0.045, 0.145, 0.082),
                (0.075, 0.245, 0.132),
                (0.145, 0.380, 0.220),
                (0.265, 0.520, 0.320),
            ) if is_pine else (
                (0.045, 0.185, 0.150),
                (0.085, 0.330, 0.255),
                (0.155, 0.490, 0.350),
                (0.300, 0.650, 0.455),
            ),
        }
        if asset_name.endswith("_OldGrowth"):
            profile.update(
                height=17.2,
                trunk_radius=0.48,
                crown_start=1.45,
                crown_radius=5.0,
                crown_taper=0.96,
                tiers=23,
                branches=10,
                branch_radius=0.13,
                droop=0.22,
                tip_lift=0.18,
            )
        elif asset_name.endswith("_Windbent"):
            profile.update(
                height=14.0,
                trunk_radius=0.36,
                crown_start=1.05,
                crown_radius=4.7,
                crown_taper=0.82,
                tiers=18,
                branches=8,
                branch_radius=0.10,
                droop=0.30,
                tip_lift=0.13,
            )
        for level in range(3):
            build_conifer_lod(root, asset_name, profile, level, trunk_mat, foliage_mat)
        return root

    profile = (8.0, 0.55, 0.0, False)
    lod_settings = (
        (8, 3, 11, 5, (3, 5)),
        (6, 2, 8, 4, (2, 3)),
        (4, 1, 5, 3, (1, 2)),
    )
    for level, (sides, max_depth, trunk_segments, branch_segments, leaf_count) in enumerate(lod_settings):
        scale = 1.0 - level * 0.11
        build_branching_tree_lod(root, asset_name, kind, level, {
            "seed": SEED + asset_seed,
            "sides": sides,
            "max_depth": max_depth,
            "trunk_segments": trunk_segments,
            "branch_segments": branch_segments,
            "height": profile[0] * scale,
            "trunk_radius": profile[1] * scale,
            "leaf_cluster_size": profile[2] * scale,
            "leaf_count": leaf_count,
            "has_leaves": profile[3],
        }, trunk_mat, leaf_materials)
    return root


def export_asset(root, folder):
    path = EXPORT_ROOT / folder / f"{root.name}.fbx"
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        mesh_smooth_type="OFF",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="AUTO",
    )
    print(f"PROCEDURAL_WORLD_ASSET_OK name={root.name} fbx={path}")


def parse_script_args():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1:]


def remove_existing_tree_roots():
    for name in ("Tree_Pine", "Tree_Pine_OldGrowth", "Tree_Pine_Windbent", "Tree_Fir"):
        root = bpy.data.objects.get(name)
        if root is None:
            continue
        for obj in list(root.children_recursive) + [root]:
            if obj.name in bpy.data.objects:
                bpy.data.objects.remove(obj, do_unlink=True)


def render_tree_preview(roots, output_path):
    scene = bpy.context.scene
    original_objects = set(bpy.data.objects)
    original_hidden = {obj: obj.hide_render for obj in scene.objects}
    original_positions = {root: root.location.copy() for root in roots}
    original_camera = scene.camera
    original_world = scene.world
    original_render = (
        scene.render.engine,
        scene.render.resolution_x,
        scene.render.resolution_y,
        scene.render.resolution_percentage,
        scene.render.filepath,
        scene.render.film_transparent,
    )
    original_cycles = (
        scene.cycles.device,
        scene.cycles.samples,
        scene.cycles.use_denoising,
        scene.cycles.max_bounces,
    )
    original_color = (
        scene.view_settings.view_transform,
        scene.view_settings.look,
        scene.view_settings.exposure,
        scene.view_settings.gamma,
    )
    temporary_world = None
    output_path = Path(output_path).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    try:
        tree_objects = set(roots)
        for root in roots:
            tree_objects.update(root.children_recursive)
        for obj in scene.objects:
            obj.hide_render = (
                obj not in tree_objects
                or ("_LOD" in obj.name and "_LOD0_" not in obj.name)
            )

        preview_spacing = 7.6
        for index, root in enumerate(roots):
            root.location = ((index - (len(roots) - 1) * 0.5) * preview_spacing, 0.0, 0.0)

        bpy.ops.mesh.primitive_plane_add(size=120.0, location=(0.0, -0.035, 0.0), rotation=(-math.pi / 2, 0.0, 0.0))
        ground = bpy.context.object
        ground.name = "TEMP_PinePreview_Ground"
        ground_mat = bpy.data.materials.new("TEMP_PinePreview_GroundMaterial")
        ground_mat.diffuse_color = (0.035, 0.055, 0.052, 1.0)
        ground_mat.use_nodes = True
        ground_mat.node_tree.nodes.clear()
        ground_output = ground_mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
        ground_shader = ground_mat.node_tree.nodes.new("ShaderNodeEmission")
        ground_shader.inputs["Color"].default_value = (0.018 * 0.42, 0.032 * 0.42, 0.042 * 0.42, 1.0)
        ground_mat.node_tree.links.new(ground_shader.outputs["Emission"], ground_output.inputs["Surface"])
        ground.data.materials.append(ground_mat)

        camera_data = bpy.data.cameras.new("TEMP_PinePreview_Camera")
        camera = bpy.data.objects.new("TEMP_PinePreview_Camera", camera_data)
        bpy.context.collection.objects.link(camera)
        camera.location = (0.0, 8.0, 39.0)
        target = Vector((0.0, 8.0, 0.0))
        camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera_data.type = "ORTHO"
        camera_data.ortho_scale = 34.0
        scene.camera = camera

        def add_area_light(name, location, energy, color, size, target_point):
            light_data = bpy.data.lights.new(name, "AREA")
            light_data.energy = energy
            light_data.color = color
            light_data.shape = "DISK"
            light_data.size = size
            light_obj = bpy.data.objects.new(name, light_data)
            bpy.context.collection.objects.link(light_obj)
            light_obj.location = location
            light_obj.rotation_euler = (Vector(target_point) - light_obj.location).to_track_quat("-Z", "Y").to_euler()
            return light_obj

        add_area_light("TEMP_PinePreview_Key", (0.0, 18.0, 14.0), 3400.0, (1.0, 0.83, 0.63), 9.0, (0.0, 6.0, 0.0))
        add_area_light("TEMP_PinePreview_Fill", (-12.0, 10.0, 8.0), 2100.0, (0.58, 0.76, 1.0), 10.0, (0.0, 7.0, 0.0))
        add_area_light("TEMP_PinePreview_Rim", (2.0, 16.0, -11.0), 4100.0, (0.74, 0.91, 1.0), 7.0, (0.0, 8.0, 0.0))

        temporary_world = bpy.data.worlds.new("TEMP_PinePreview_World")
        temporary_world.use_nodes = True
        background = temporary_world.node_tree.nodes.get("Background")
        background.inputs["Color"].default_value = (0.018, 0.032, 0.042, 1.0)
        background.inputs["Strength"].default_value = 0.42
        scene.world = temporary_world

        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 16
        scene.cycles.use_denoising = False
        scene.cycles.max_bounces = 4
        scene.render.resolution_x = 1280
        scene.render.resolution_y = 960
        scene.render.resolution_percentage = 100
        scene.render.film_transparent = False
        scene.render.filepath = str(output_path)
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
        scene.view_settings.exposure = 0.0
        scene.view_settings.gamma = 1.0
        bpy.ops.render.render(write_still=True)
        print(f"PROCEDURAL_WORLD_TREE_PREVIEW_OK path={output_path}")
    finally:
        for obj in list(bpy.data.objects):
            if obj not in original_objects:
                bpy.data.objects.remove(obj, do_unlink=True)
        for obj, hidden in original_hidden.items():
            if obj.name in bpy.data.objects:
                obj.hide_render = hidden
        for root, position in original_positions.items():
            root.location = position
        scene.camera = original_camera
        scene.world = original_world
        if temporary_world and temporary_world.users == 0:
            bpy.data.worlds.remove(temporary_world)
        ground_mat = bpy.data.materials.get("TEMP_PinePreview_GroundMaterial")
        if ground_mat and ground_mat.users == 0:
            bpy.data.materials.remove(ground_mat)
        (scene.render.engine,
         scene.render.resolution_x,
         scene.render.resolution_y,
         scene.render.resolution_percentage,
         scene.render.filepath,
         scene.render.film_transparent) = original_render
        (scene.cycles.device,
         scene.cycles.samples,
         scene.cycles.use_denoising,
         scene.cycles.max_bounces) = original_cycles
        (scene.view_settings.view_transform,
         scene.view_settings.look,
         scene.view_settings.exposure,
         scene.view_settings.gamma) = original_color


def generate_trees_only(args):
    if not BLEND_PATH.exists():
        raise FileNotFoundError(f"Existing Blender asset pack was not found: {BLEND_PATH}")
    remove_existing_tree_roots()
    roots = [
        build_tree_asset("Tree_Pine", "Pine"),
        build_tree_asset("Tree_Pine_OldGrowth", "Pine"),
        build_tree_asset("Tree_Pine_Windbent", "Pine"),
        build_tree_asset("Tree_Fir", "Fir"),
    ]
    preview_path = None
    if "--preview" in args:
        preview_index = args.index("--preview")
        if preview_index + 1 >= len(args):
            raise ValueError("--preview requires an output image path")
        preview_path = args[preview_index + 1]
        try:
            render_tree_preview(roots, preview_path)
        except Exception as exc:
            print(f"PROCEDURAL_WORLD_TREE_PREVIEW_WARN error={exc!r}")

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    for root in roots:
        export_asset(root, "Trees")
    print(f"PROCEDURAL_WORLD_TREES_ONLY_OK assets={len(roots)} blend={BLEND_PATH} preview={preview_path}")


def main():
    args = parse_script_args()
    if "--trees-only" in args:
        generate_trees_only(args)
        return

    clear_scene()
    roots = []
    roots.extend([
        (build_rock_asset("Rock_Small", (0.75, 0.60, 0.75)), "Rocks"),
        (build_rock_asset("Rock_Medium", (1.30, 1.05, 1.20)), "Rocks"),
        (build_rock_asset("Rock_Large", (2.15, 1.65, 1.90)), "Rocks"),
        (build_rock_asset("Mountain_Boulder", (3.40, 2.45, 2.80)), "Rocks"),
    ])
    roots.extend([
        (build_rock_asset("Cliff_Straight", (7.0, 8.0, 2.2), True), "Cliffs"),
        (build_rock_asset("Cliff_Corner", (5.0, 6.0, 5.0), True), "Cliffs"),
        (build_rock_asset("Cliff_Tall", (5.0, 13.0, 2.7), True), "Cliffs"),
        (build_rock_asset("Cliff_Wide", (12.0, 7.0, 2.5), True), "Cliffs"),
        (build_rock_asset("Cliff_Broken", (6.5, 9.0, 3.5), True), "Cliffs"),
        (build_rock_asset("Cliff_Cap", (8.0, 4.5, 4.0), True), "Cliffs"),
    ])
    roots.extend([
        (build_tree_asset("Tree_Pine", "Pine"), "Trees"),
        (build_tree_asset("Tree_Pine_OldGrowth", "Pine"), "Trees"),
        (build_tree_asset("Tree_Pine_Windbent", "Pine"), "Trees"),
        (build_tree_asset("Tree_Fir", "Fir"), "Trees"),
        (build_tree_asset("Tree_Dead", "Dead"), "Trees"),
        (build_tree_asset("Tree_Stump", "Stump"), "Trees"),
    ])
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    for root, folder in roots:
        export_asset(root, folder)
    print(f"PROCEDURAL_WORLD_ASSET_PACK_OK seed={SEED} assets={len(roots)} blend={BLEND_PATH}")


if __name__ == "__main__":
    main()
