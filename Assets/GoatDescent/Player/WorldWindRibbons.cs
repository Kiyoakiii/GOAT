using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>World-space streamlines at two depths. Recycles outside a softly faded viewing volume.</summary>
    public sealed class WorldWindRibbons : MonoBehaviour
    {
        private const int Count = 168, Segments = 32;
        private Mesh mesh;
        private MeshRenderer display;
        private Material material;
        private Vector3[] vertices;
        private Color[] colors;

        private void Awake()
        {
            mesh = new Mesh { name = "Long atmospheric wind ribbons" };
            mesh.MarkDynamic();
            vertices = new Vector3[Count * (Segments + 1) * 2];
            colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[Count * Segments * 6];
            for (int r = 0; r < Count; r++)
            for (int s = 0; s <= Segments; s++)
            {
                int v = (r * (Segments + 1) + s) * 2;
                uv[v] = new Vector2((float)s / Segments, 0);
                uv[v + 1] = new Vector2((float)s / Segments, 1);
                if (s == Segments) continue;
                int t = (r * Segments + s) * 6;
                indices[t] = v; indices[t + 1] = v + 2; indices[t + 2] = v + 1;
                indices[t + 3] = v + 1; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.triangles = indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            display = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Resources.Load<Shader>("EagleWindRibbon"));
            display.sharedMaterial = material;
            display.shadowCastingMode = ShadowCastingMode.Off;
            display.receiveShadows = false;
            display.lightProbeUsage = LightProbeUsage.Off;
        }

        private void LateUpdate()
        {
            var weather = WorldWeather.Current;
            display.enabled = weather && weather.Wind > .002f;
            var camera = Camera.main;
            if (camera) mesh.bounds = new Bounds(camera.transform.position, Vector3.one * 500f);
        }

        private static float Seed(int i, float factor) => Mathf.Repeat((i + 1) * factor, 1f);
        private static float Fade(float from, float to, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value));
        private static float Wrap(float value, float center, float span) => center + Mathf.Repeat(value - center + span * .5f, span) - span * .5f;

        private void OnWillRenderObject()
        {
            var camera = Camera.current;
            var weather = WorldWeather.Current;
            if (!camera || !weather || !display.enabled) return;
            Vector3 eye = camera.transform.position;
            Vector3 forward = weather.WindDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float cx = Vector3.Dot(eye, right), cz = Vector3.Dot(eye, forward);
            for (int r = 0; r < Count; r++)
            {
                bool far = r >= 112;
                float span = far ? 180 : 76, height = far ? 76 : 32;
                float seed = Seed(r, .618034f), phase = Seed(r, .414214f) * Mathf.PI * 2;
                float length = Mathf.Lerp(far ? 22 : 11, far ? 42 : 27, seed);
                float x = Wrap(Seed(r, .75488f) * span, cx, span);
                float y = Wrap(Seed(r, .56984f) * height, eye.y + height * .1f, height);
                // Each streamline travels in the same direction, with gentle layered gusts.
                float z = Wrap(Seed(r, .32817f) * span + weather.Clock * Mathf.Lerp(9, 15, seed), cz, span);
                float edgeX = 1 - Fade(.6f, 1, Mathf.Abs(x - cx) / (span * .5f));
                float edgeY = 1 - Fade(.65f, 1, Mathf.Abs(y - eye.y - height * .1f) / (height * .5f));
                float edgeZ = 1 - Fade(.55f, 1, Mathf.Abs(z - cz) / (span * .5f));
                float width = Mathf.Lerp(.045f, .13f, Seed(r, .23771f)) * (far ? 1.6f : 1);
                bool gauze = r % 9 == 0;
                if (gauze) width *= 3;
                float grey = Mathf.Lerp(.74f, 1, seed);
                for (int s = 0; s <= Segments; s++)
                {
                    float u = (float)s / Segments, along = (u - .5f) * length;
                    float curl = phase + along * .23f + weather.Clock * .32f;
                    Vector3 p = right * (x + Mathf.Sin(curl) * 1.15f)
                        + Vector3.up * (y + Mathf.Cos(curl * .83f) * .65f + Mathf.Sin(curl * 1.9f) * .13f)
                        + forward * (z + along);
                    Vector3 tangent = forward + right * (Mathf.Cos(curl) * .2645f)
                        - Vector3.up * (Mathf.Sin(curl * .83f) * .124f + Mathf.Cos(curl * 1.9f) * -.057f);
                    Vector3 across = Vector3.Cross(tangent, eye - p).normalized;
                    float taper = Mathf.Pow(Mathf.Sin(u * Mathf.PI), .65f);
                    int v = (r * (Segments + 1) + s) * 2;
                    vertices[v] = p - across * width * taper;
                    vertices[v + 1] = p + across * width * taper;
                    float nearFade = Fade(1.5f, 5f, Vector3.Distance(eye, p));
                    float alpha = weather.Wind * edgeX * edgeY * edgeZ * nearFade * (gauze ? .09f : .28f);
                    colors[v] = colors[v + 1] = new Color(grey, grey + .012f, grey + .022f, alpha);
                }
            }
            mesh.vertices = vertices; mesh.colors = colors;
        }

        private void OnDestroy()
        {
            if (mesh) Destroy(mesh);
            if (material) Destroy(material);
        }
    }
}
