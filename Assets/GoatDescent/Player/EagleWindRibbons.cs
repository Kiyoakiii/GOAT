using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>A single reusable mesh of tapered, curling air ribbons.</summary>
    public sealed class EagleWindRibbons : MonoBehaviour
    {
        private const int RibbonCount = 64, Segments = 24;
        private Mesh mesh;
        private Material material;
        private MeshRenderer display;
        private Vector3[] vertices;
        private Color[] colors;
        private float strength, flowTime;

        private void Awake()
        {
            mesh = new Mesh { name = "Eagle silken wind ribbons" };
            mesh.MarkDynamic();
            vertices = new Vector3[RibbonCount * (Segments + 1) * 2];
            colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[RibbonCount * Segments * 6];
            for (int r = 0; r < RibbonCount; r++)
            for (int s = 0; s <= Segments; s++)
            {
                int v = (r * (Segments + 1) + s) * 2;
                uv[v] = new Vector2((float)s / Segments, 0);
                uv[v + 1] = new Vector2((float)s / Segments, 1);
                if (s == Segments) continue;
                int t = (r * Segments + s) * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
            mesh.bounds = new Bounds(new Vector3(0, 0, 3.5f), new Vector3(10, 8, 14));
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            display = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Resources.Load<Shader>("EagleWindRibbon"));
            display.sharedMaterial = material;
            display.shadowCastingMode = ShadowCastingMode.Off;
            display.receiveShadows = false;
            display.lightProbeUsage = LightProbeUsage.Off;
            display.enabled = false;
        }

        public void SetFlow(Vector3 origin, Quaternion rotation, float amount, float time)
        {
            transform.SetPositionAndRotation(origin, rotation);
            strength = Mathf.Clamp01(amount); flowTime = time;
            display.enabled = strength > .002f;
        }

        private static float Seed(int i, float factor) => Mathf.Repeat((i + 1) * factor, 1f);

        private void OnWillRenderObject()
        {
            var camera = Camera.current;
            if (!camera || !display.enabled) return;
            Vector3 eye = transform.InverseTransformPoint(camera.transform.position);
            for (int r = 0; r < RibbonCount; r++)
            {
                float seed = Seed(r, .618034f), offset = Seed(r, .414214f);
                float age = Mathf.Repeat(flowTime * Mathf.Lerp(.65f, 1.05f, offset) + seed, 1f);
                float length = Mathf.Lerp(1.1f, 3.1f, Seed(r, .75488f));
                float head = age * (8f + length);
                float opacity = Mathf.Sin(age * Mathf.PI) * strength;
                float angle = offset * Mathf.PI * 2;
                float radius = Mathf.Lerp(.25f, 1.75f, Seed(r, .56984f));
                float width = Mathf.Lerp(.035f, .14f, Seed(r, .32817f));
                // Broad, faint gauze mixed with finer bright streamlines.
                if (r % 8 == 0) width *= 2.2f;
                float grey = Mathf.Lerp(.68f, 1f, seed);
                for (int s = 0; s <= Segments; s++)
                {
                    float u = (float)s / Segments;
                    float z = head - length + u * length;
                    float curl = angle + z * (.38f + seed * .32f) + flowTime * .45f;
                    float spread = radius * Mathf.Lerp(.65f, 1.25f, Mathf.Clamp01(z / 7f));
                    Vector3 p = new Vector3(Mathf.Cos(curl) * spread,
                        Mathf.Sin(curl) * spread * .62f + Mathf.Sin(z * 1.7f + angle) * .12f, z);
                    // All ribbons emerge from the two wing fans, then roll together.
                    p.x += (r % 2 == 0 ? -1f : 1f) * .75f * Mathf.Exp(-Mathf.Max(0, z) * .65f);
                    Vector3 tangent = new Vector3(-Mathf.Sin(curl) * spread * .5f,
                        Mathf.Cos(curl) * spread * .31f, 1);
                    Vector3 across = Vector3.Cross(tangent, eye - p).normalized;
                    if (across.sqrMagnitude < .01f) across = Vector3.up;
                    float taper = Mathf.Pow(Mathf.Sin(u * Mathf.PI), .7f);
                    float fade = Mathf.SmoothStep(0, 1, z / .5f)
                        * (1f - Mathf.SmoothStep(0, 1, (z - 6f) / 2f));
                    int v = (r * (Segments + 1) + s) * 2;
                    vertices[v] = p - across * (width * taper);
                    vertices[v + 1] = p + across * (width * taper);
                    Color color = new Color(grey, grey + .015f, grey + .025f,
                        opacity * fade * (r % 8 == 0 ? .12f : .3f));
                    colors[v] = colors[v + 1] = color;
                }
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
        }

        private void OnDestroy()
        {
            if (mesh) Destroy(mesh);
            if (material) Destroy(material);
        }
    }
}
