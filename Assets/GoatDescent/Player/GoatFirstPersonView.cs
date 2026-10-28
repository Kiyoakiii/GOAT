using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class GoatFirstPersonView : MonoBehaviour
    {
        [SerializeField] private float eyeHeightAboveChest = .25f;
        [SerializeField] private float eyeForwardFromChest = .75f;
        [SerializeField, Range(0f, 1f)] private float headMotion = .2f;
        [SerializeField] private float maximumHeadDisplacement = .12f;
        [SerializeField] private float bodyFadeNear = .6f;
        [SerializeField] private float bodyFadeFar = .95f;

        private sealed class BodyPart
        {
            public SkinnedMeshRenderer Source, Body;
            public Mesh SourceMesh;
        }

        private readonly List<BodyPart> parts = new List<BodyPart>();
        private MaterialPropertyBlock appearanceProperties;
        private readonly Dictionary<Mesh, Mesh> meshes = new Dictionary<Mesh, Mesh>();
        private readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private readonly List<Renderer> headRenderers = new List<Renderer>();
        private readonly List<Renderer> hidden = new List<Renderer>();
        private readonly List<ShadowCastingMode> shadows = new List<ShadowCastingMode>();
        private GoatController owner;
        private GoatVisualController visual;
        private GoatPhysicalBody physical;
        private Transform head, neck;
        private int skinNumber = -100;
        private bool active;

        private Vector3 BodyForward
        {
            get
            {
                Vector3 forward = visual ? visual.Facing : owner ? owner.transform.forward : Vector3.forward;
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);
                return forward.sqrMagnitude > .01f ? forward.normalized : Vector3.forward;
            }
        }
        public Vector3 CollisionAnchor => physical ? physical.HarnessChest + Vector3.up * .12f
            : owner ? owner.transform.position + Vector3.up * 1.1f : transform.position;
        public Vector3 EyePosition
        {
            get
            {
                if (!owner) return transform.position;
                Vector3 forward = BodyForward;
                if (head && (owner.IsPredatorCarried || physical && physical.RootIsTumbling))
                    return head.position + Vector3.up * .08f;
                Vector3 chest = physical ? physical.HarnessChest
                    : neck ? neck.position : owner.transform.position + Vector3.up * 1.15f;
                Vector3 position = chest + Vector3.up * eyeHeightAboveChest + forward * eyeForwardFromChest;
                if (head && neck)
                {
                    Vector3 headOffset = head.position - neck.position;
                    Vector3 restDirection = forward * .46f + Vector3.up * .14f;
                    position += Vector3.ClampMagnitude((headOffset - restDirection) * headMotion, maximumHeadDisplacement);
                }
                return position;
            }
        }

        public void Configure(GoatController goat, bool firstPerson)
        {
            int number = goat ? goat.GetComponent<GoatSkin>()?.Number ?? 0 : -100;
            bool meshChanged = false;
            foreach (var part in parts)
                if (part.Source && part.Source.sharedMesh != part.SourceMesh) { meshChanged = true; break; }
            if (owner != goat || skinNumber != number || meshChanged)
            {
                ClearBody();
                owner = goat; skinNumber = number;
                visual = owner ? owner.GetComponent<GoatVisualController>() : null;
                physical = owner ? owner.GetComponent<GoatPhysicalBody>() : null;
                head = neck = null;
            }
            active = firstPerson && owner;
            if (active && parts.Count == 0 && headRenderers.Count == 0) BuildBody();
            foreach (var part in parts)
            {
                if (!part.Body || !part.Source) continue;
                part.Body.enabled = false;
            }
        }

        private void BuildBody()
        {
            var renderers = owner.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer source) || !source.enabled || !source.sharedMesh) continue;
                bool hasHead = false;
                foreach (var bone in source.bones)
                {
                    if (!bone) continue;
                    if (bone.name == "Head") { head = bone; hasHead = true; }
                    if (bone.name == "Neck") neck = bone;
                }
                if (!hasHead) continue;
                headRenderers.Add(source);
                if (!meshes.TryGetValue(source.sharedMesh, out var mesh))
                {
                    mesh = ExtractBody(source);
                    meshes[source.sharedMesh] = mesh;
                }
                if (!mesh) continue;
                var node = new GameObject("First person body — original world skeleton");
                node.layer = source.gameObject.layer;
                node.transform.SetParent(source.transform, false);
                var body = node.AddComponent<SkinnedMeshRenderer>();
                body.sharedMesh = mesh;
                var sourceMaterials = source.sharedMaterials;
                var bodyMaterials = new Material[sourceMaterials.Length];
                for (int i = 0; i < bodyMaterials.Length; i++) bodyMaterials[i] = BodyMaterial(sourceMaterials[i]);
                body.sharedMaterials = bodyMaterials;
                body.bones = source.bones;
                body.rootBone = source.rootBone;
                body.quality = source.quality;
                body.localBounds = source.localBounds;
                body.updateWhenOffscreen = true;
                body.shadowCastingMode = ShadowCastingMode.Off;
                body.receiveShadows = source.receiveShadows;
                body.lightProbeUsage = source.lightProbeUsage;
                body.reflectionProbeUsage = source.reflectionProbeUsage;
                body.probeAnchor = source.probeAnchor;
                body.allowOcclusionWhenDynamic = false;
                body.enabled = false;
                parts.Add(new BodyPart { Source = source, Body = body, SourceMesh = source.sharedMesh });
            }
            if (head)
                foreach (var renderer in renderers)
                    if (renderer.enabled && renderer.transform.IsChildOf(head) && !headRenderers.Contains(renderer))
                        headRenderers.Add(renderer);
        }

        private Material BodyMaterial(Material source)
        {
            if (!source) return null;
            if (materials.TryGetValue(source, out var cached)) return cached;
            var shader = Resources.Load<Shader>("GoatFirstPersonBody");
            if (!shader) return source;
            var result = new Material(shader);
            result.CopyPropertiesFromMaterial(source);
            result.name = source.name + " · first person near fade";
            result.SetFloat("_BodyFadeNear", bodyFadeNear);
            result.SetFloat("_BodyFadeFar", bodyFadeFar);
            bool fibers = source.shader && source.shader.name == "GoatDescent/Ibex Fibers";
            result.SetFloat("_UseVertexColor", fibers ? 1f : 0f);
            result.SetFloat("_Cull", fibers ? 0f : 2f);
            if (fibers) { result.SetFloat("_Glossiness", .18f); result.SetFloat("_Metallic", 0f); }
            materials.Add(source, result);
            return result;
        }

        private static Mesh ExtractBody(SkinnedMeshRenderer source)
        {
            var mesh = source.sharedMesh;
            if (!mesh.isReadable) return null;
            var sourceWeights = mesh.boneWeights;
            if (sourceWeights.Length != mesh.vertexCount) return null;
            var bones = source.bones;
            var headBones = new bool[bones.Length];
            var legBones = new bool[bones.Length];
            int neckIndex = -1;
            Transform headBone = null;
            foreach (var bone in bones) if (bone && bone.name == "Head") { headBone = bone; break; }
            for (int i = 0; i < bones.Length; i++)
            {
                if (!bones[i]) continue;
                headBones[i] = headBone && (bones[i] == headBone || bones[i].IsChildOf(headBone));
                if (bones[i].name == "Neck") neckIndex = i;
                legBones[i] = bones[i].name.StartsWith("FrontUpper.") || bones[i].name.StartsWith("FrontLower.")
                    || bones[i].name.StartsWith("HindUpper.") || bones[i].name.StartsWith("HindLower.");
            }
            var keep = new bool[mesh.vertexCount];
            var leg = new float[mesh.vertexCount];
            for (int i = 0; i < keep.Length; i++)
            {
                var w = sourceWeights[i];
                float headWeight = (headBones[w.boneIndex0] ? w.weight0 : 0f)
                    + (headBones[w.boneIndex1] ? w.weight1 : 0f)
                    + (headBones[w.boneIndex2] ? w.weight2 : 0f)
                    + (headBones[w.boneIndex3] ? w.weight3 : 0f);
                float neckWeight = (w.boneIndex0 == neckIndex ? w.weight0 : 0f)
                    + (w.boneIndex1 == neckIndex ? w.weight1 : 0f)
                    + (w.boneIndex2 == neckIndex ? w.weight2 : 0f)
                    + (w.boneIndex3 == neckIndex ? w.weight3 : 0f);
                keep[i] = headWeight < .12f && neckWeight < .65f;
                leg[i] = (legBones[w.boneIndex0] ? w.weight0 : 0f)
                    + (legBones[w.boneIndex1] ? w.weight1 : 0f)
                    + (legBones[w.boneIndex2] ? w.weight2 : 0f)
                    + (legBones[w.boneIndex3] ? w.weight3 : 0f);
            }
            var map = new Dictionary<int, int>();
            var indices = new List<int>();
            var triangles = new List<int>[mesh.subMeshCount];
            for (int s = 0; s < triangles.Length; s++)
            {
                triangles[s] = new List<int>();
                var sourceMaterials = source.sharedMaterials;
                bool fibers = s < sourceMaterials.Length && sourceMaterials[s] && sourceMaterials[s].shader
                    && sourceMaterials[s].shader.name == "GoatDescent/Ibex Fibers";
                var original = mesh.GetTriangles(s);
                for (int i = 0; i < original.Length; i += 3)
                {
                    if (!keep[original[i]] || !keep[original[i + 1]] || !keep[original[i + 2]]) continue;
                    if (fibers && (leg[original[i]] < .35f || leg[original[i + 1]] < .35f || leg[original[i + 2]] < .35f)) continue;
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int old = original[i + corner];
                        if (!map.TryGetValue(old, out int index))
                        { index = indices.Count; map.Add(old, index); indices.Add(old); }
                        triangles[s].Add(index);
                    }
                }
            }
            if (indices.Count == 0) return null;
            var vertices = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
            var uv = mesh.uv; var uv2 = mesh.uv2; var colors = mesh.colors32;
            var resultVertices = new Vector3[indices.Count]; var resultNormals = new Vector3[indices.Count];
            var resultTangents = new Vector4[indices.Count]; var resultUv = new Vector2[indices.Count];
            var resultUv2 = new Vector2[indices.Count]; var resultColors = new Color32[indices.Count];
            var weights = new BoneWeight[indices.Count];
            var mask = new Vector2[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                int old = indices[i]; resultVertices[i] = vertices[old]; weights[i] = sourceWeights[old];
                mask[i] = new Vector2(leg[old], 0f);
                if (normals.Length > old) resultNormals[i] = normals[old];
                if (tangents.Length > old) resultTangents[i] = tangents[old];
                if (uv.Length > old) resultUv[i] = uv[old];
                if (uv2.Length > old) resultUv2[i] = uv2[old];
                if (colors.Length > old) resultColors[i] = colors[old];
            }
            var result = new Mesh { name = "First person world body · " + mesh.name,
                indexFormat = indices.Count + 512 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            result.vertices = resultVertices; result.boneWeights = weights; result.bindposes = mesh.bindposes;
            if (normals.Length > 0) result.normals = resultNormals;
            if (tangents.Length > 0) result.tangents = resultTangents;
            if (uv.Length > 0) result.uv = resultUv;
            if (uv2.Length > 0) result.uv2 = resultUv2;
            if (colors.Length > 0) result.colors32 = resultColors;
            result.uv3 = mask;
            result.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) result.SetTriangles(triangles[i], i);
            CloseKneeOpenings(result, source);
            result.RecalculateBounds();
            return result;
        }

        private static void CloseKneeOpenings(Mesh body, SkinnedMeshRenderer source)
        {
            var vertices = new List<Vector3>(body.vertices);
            var normals = new List<Vector3>(body.normals);
            var tangents = new List<Vector4>(body.tangents);
            var uv = new List<Vector2>(body.uv);
            var uv2 = new List<Vector2>(body.uv2);
            var mask = new List<Vector2>(body.uv3);
            var colors = new List<Color32>(body.colors32);
            var weights = new List<BoneWeight>(body.boneWeights);
            var triangles = new List<int>(body.GetTriangles(0));
            bool hasNormals = normals.Count == vertices.Count, hasTangents = tangents.Count == vertices.Count;
            bool hasUv = uv.Count == vertices.Count, hasUv2 = uv2.Count == vertices.Count, hasColors = colors.Count == vertices.Count;
            var sourceVertices = source.sharedMesh.vertices;
            var sourceUv = source.sharedMesh.uv;
            var surface = source.sharedMesh.GetTriangles(0);
            float scale = Mathf.Max(.001f, Mathf.Abs(source.transform.lossyScale.x));
            const int segments = 16, rings = 4;
            for (int bone = 0; bone < source.bones.Length; bone++)
            {
                string name = source.bones[bone] ? source.bones[bone].name : "";
                if (!name.StartsWith("FrontLower.") && !name.StartsWith("HindLower.")) continue;
                string upperName = name.Replace("Lower", "Upper");
                int upper = System.Array.FindIndex(source.bones, item => item && item.name == upperName);
                if (upper < 0) continue;
                Vector3 center = source.sharedMesh.bindposes[bone].inverse.MultiplyPoint3x4(Vector3.zero);
                Vector3 upperPosition = source.sharedMesh.bindposes[upper].inverse.MultiplyPoint3x4(Vector3.zero);
                Vector3 up = (upperPosition - center).normalized;
                Vector3 right = Vector3.Cross(up, Mathf.Abs(up.z) < .9f ? Vector3.forward : Vector3.right).normalized;
                Vector3 forward = Vector3.Cross(right, up);
                Vector2 sampleUv = Vector2.zero;
                float nearest = float.MaxValue;
                foreach (int vertex in surface)
                {
                    float distance = (sourceVertices[vertex] - center).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    if (sourceUv.Length > vertex) sampleUv = sourceUv[vertex];
                }
                int start = vertices.Count;
                float radius = .095f / scale;
                for (int ring = 0; ring <= rings; ring++)
                for (int segment = 0; segment <= segments; segment++)
                {
                    float elevation = ring * Mathf.PI * .5f / rings;
                    float angle = segment * Mathf.PI * 2f / segments;
                    Vector3 radial = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    Vector3 normal = radial * Mathf.Cos(elevation) + up * Mathf.Sin(elevation);
                    vertices.Add(center + normal * radius);
                    if (hasNormals) normals.Add(normal);
                    if (hasTangents) tangents.Add(new Vector4(right.x, right.y, right.z, 1f));
                    if (hasUv) uv.Add(sampleUv);
                    if (hasUv2) uv2.Add(Vector2.zero);
                    if (hasColors) colors.Add(new Color32(255, 255, 255, 255));
                    mask.Add(Vector2.right);
                    weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1f });
                    if (ring == rings || segment == segments) continue;
                    int a = start + ring * (segments + 1) + segment;
                    int b = a + segments + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }
            body.SetVertices(vertices); body.boneWeights = weights.ToArray(); body.SetUVs(2, mask);
            if (hasNormals) body.SetNormals(normals);
            if (hasTangents) body.SetTangents(tangents);
            if (hasUv) body.SetUVs(0, uv);
            if (hasUv2) body.SetUVs(1, uv2);
            if (hasColors) body.SetColors(colors);
            body.SetTriangles(triangles, 0);
        }

        public void RefreshAppearance(GoatController subject)
        {
            if (owner != subject) return;
            foreach (var material in materials.Values) if (material) Destroy(material);
            materials.Clear();
            foreach (var part in parts)
            {
                if (!part.Source || !part.Body) continue;
                var source = part.Source.sharedMaterials;
                var mapped = new Material[source.Length];
                for (int i = 0; i < source.Length; i++) mapped[i] = BodyMaterial(source[i]);
                part.Body.sharedMaterials = mapped;
            }
        }
        private void OnPreCull()
        {
            RestoreRenderers();
            if (!active || !owner) return;
            foreach (var renderer in headRenderers)
            {
                if (!renderer || !renderer.enabled) continue;
                hidden.Add(renderer); shadows.Add(renderer.shadowCastingMode);
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            foreach (var part in parts)
            {
                if (!part.Body || !part.Source || !part.Source.enabled) continue;
                appearanceProperties ??= new MaterialPropertyBlock();
                part.Source.GetPropertyBlock(appearanceProperties);
                part.Body.SetPropertyBlock(appearanceProperties);
                for (int i = 0; i < part.Source.sharedMesh.blendShapeCount; i++)
                    if (i < part.Body.sharedMesh.blendShapeCount)
                        part.Body.SetBlendShapeWeight(i, part.Source.GetBlendShapeWeight(i));
                part.Body.enabled = true;
            }
        }
        private void OnPostRender() => RestoreRenderers();
        private void RestoreRenderers()
        {
            for (int i = 0; i < hidden.Count; i++) if (hidden[i]) hidden[i].shadowCastingMode = shadows[i];
            hidden.Clear(); shadows.Clear();
            foreach (var part in parts) if (part.Body) part.Body.enabled = false;
        }
        private void ClearBody()
        {
            RestoreRenderers();
            foreach (var part in parts)
                if (part.Body) { part.Body.gameObject.SetActive(false); Destroy(part.Body.gameObject); }
            parts.Clear(); headRenderers.Clear();
        }
        private void OnDisable() { active = false; RestoreRenderers(); }
        private void OnDestroy()
        {
            ClearBody();
            foreach (var mesh in meshes.Values) if (mesh) Destroy(mesh);
            meshes.Clear();
            foreach (var material in materials.Values) if (material) Destroy(material);
            materials.Clear();
        }
    }
}
