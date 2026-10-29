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
        private readonly float[] tapers = new float[Segments + 1];
        private readonly CurveSample[] curve = new CurveSample[Count * (Segments + 1)];

        private struct CurveSample
        {
            public float along, sin, cos, sinSlow, cosSlow, sinFast, cosFast;
        }

        private void Awake()
        {
            mesh = new Mesh { name = "Long atmospheric wind ribbons" };
            mesh.MarkDynamic();
            vertices = new Vector3[Count * (Segments + 1) * 2];
            colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[Count * Segments * 6];
            for (int s = 0; s <= Segments; s++)
                tapers[s] = s == 0 || s == Segments ? 0f
                    : Mathf.Pow(Mathf.Max(0f, Mathf.Sin((float)s / Segments * Mathf.PI)), .65f);
            for (int r = 0; r < Count; r++)
            for (int s = 0; s <= Segments; s++)
            {
                int v = (r * (Segments + 1) + s) * 2;
                bool far = r >= 112;
                float length = Mathf.Lerp(far ? 22 : 11, far ? 42 : 27, Seed(r, .618034f));
                float along = ((float)s / Segments - .5f) * length;
                float angle = Seed(r, .414214f) * Mathf.PI * 2 + along * .23f;
                curve[v / 2] = new CurveSample
                {
                    along = along, sin = Mathf.Sin(angle), cos = Mathf.Cos(angle),
                    sinSlow = Mathf.Sin(angle * .83f), cosSlow = Mathf.Cos(angle * .83f),
                    sinFast = Mathf.Sin(angle * 1.9f), cosFast = Mathf.Cos(angle * 1.9f)
                };
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
            // Angle addition reuses each curve's fixed shape. Only six trig calls
            // per camera update instead of six for every segment of every ribbon.
            float time = weather.Clock * .32f;
            float sinTime = Mathf.Sin(time), cosTime = Mathf.Cos(time);
            float sinSlow = Mathf.Sin(time * .83f), cosSlow = Mathf.Cos(time * .83f);
            float sinFast = Mathf.Sin(time * 1.9f), cosFast = Mathf.Cos(time * 1.9f);
            for (int r = 0; r < Count; r++)
            {
                bool far = r >= 112;
                float span = far ? 180 : 76, height = far ? 76 : 32;
                float seed = Seed(r, .618034f);
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
                    int v = (r * (Segments + 1) + s) * 2;
                    var sample = curve[v / 2];
                    float wave = sample.sin * cosTime + sample.cos * sinTime;
                    float waveCos = sample.cos * cosTime - sample.sin * sinTime;
                    float slow = sample.sinSlow * cosSlow + sample.cosSlow * sinSlow;
                    float slowCos = sample.cosSlow * cosSlow - sample.sinSlow * sinSlow;
                    float fast = sample.sinFast * cosFast + sample.cosFast * sinFast;
                    float fastCos = sample.cosFast * cosFast - sample.sinFast * sinFast;
                    Vector3 p = right * (x + wave * 1.15f)
                        + Vector3.up * (y + slowCos * .65f + fast * .13f)
                        + forward * (z + sample.along);
                    Vector3 tangent = forward + right * (waveCos * .2645f)
                        - Vector3.up * (slow * .124f + fastCos * -.057f);
                    Vector3 across = Vector3.Cross(tangent, eye - p).normalized;
                    float taper = tapers[s];
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
