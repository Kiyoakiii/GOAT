using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>Reusable aura; particles linger briefly after the ten-second buff.</summary>
    public sealed class GoatBuffEffect : MonoBehaviour
    {
        private static Material sparkMaterial, ringMaterial;
        private Transform effectRoot, chest, pelvis;
        private ParticleSystem sparks;
        private readonly LineRenderer[] rings = new LineRenderer[3];
        private readonly Color cyan = new Color(.08f, .9f, 1f);
        private readonly Color gold = new Color(1f, .64f, .12f);
        private float born, remaining;
        private bool playing;

        private void Awake()
        {
            EnsureMaterials();
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            foreach (var bone in skin.bones)
            {
                if (bone.name == "ElasticChest") chest = bone;
                if (bone.name == "Pelvis") pelvis = bone;
            }
            effectRoot = new GameObject("Punch buff aura").transform;
            effectRoot.SetParent(transform, false);
            for (int i = 0; i < rings.Length; i++)
            {
                var go = new GameObject("Orbit arc " + i);
                go.transform.SetParent(effectRoot, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = ringMaterial;
                line.useWorldSpace = false;
                line.positionCount = 48;
                line.widthMultiplier = .045f;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                rings[i] = line;
            }
            var particles = new GameObject("Turquoise and gold sparks");
            particles.transform.SetParent(effectRoot, false);
            sparks = particles.AddComponent<ParticleSystem>();
            var main = sparks.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .65f);
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .11f);
            main.startColor = new ParticleSystem.MinMaxGradient(cyan, gold);
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .7f;
            shape.radiusThickness = .15f;
            var emission = sparks.emission;
            emission.rateOverTime = 32f;
            emission.rateOverDistance = 9f;
            var color = sparks.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sparkMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void EnsureMaterials()
        {
            if (sparkMaterial) return;
            ringMaterial = new Material(Shader.Find("Sprites/Default"));
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Pow(Mathf.Clamp01(1f - radius), 4f);
                float cross = Mathf.Exp(-Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy)) * 30f)
                    * Mathf.Pow(Mathf.Clamp01(1f - radius), 1.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + cross)));
            }
            texture.Apply(false, true);
            sparkMaterial = new Material(Resources.Load<Shader>("GoatBuffSpark"));
            sparkMaterial.mainTexture = texture;
        }

        public void StartEffect()
        {
            born = Time.time;
            remaining = GoatPunchBuff.Duration;
            playing = true;
            sparks.Clear();
            sparks.Play();
            sparks.Emit(45);
        }

        public void SetRemaining(float seconds) => remaining = seconds;

        public void StopEffect()
        {
            playing = false;
            if (sparks) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foreach (var ring in rings) if (ring) ring.enabled = false;
        }

        private void LateUpdate()
        {
            if (!effectRoot) return;
            Vector3 center = chest && pelvis ? (chest.position + pelvis.position) * .5f
                : transform.position + Vector3.up * 1.2f;
            effectRoot.position = center;
            if (!playing) return;
            float fade = Mathf.Min(Mathf.Clamp01((Time.time - born) * 5f), Mathf.Clamp01(remaining * 2f));
            float age = Time.time - born;
            for (int arc = 0; arc < rings.Length; arc++)
            {
                var line = rings[arc];
                line.enabled = true;
                Color tint = arc == 1 ? gold : cyan;
                line.startColor = new Color(tint.r, tint.g, tint.b, fade * .15f);
                line.endColor = new Color(tint.r, tint.g, tint.b, fade);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 1.45f
                        + age * (arc == 1 ? -2.2f : 1.8f) + arc * 2.1f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * (.58f + arc * .045f),
                        Mathf.Sin(angle * 2f + arc) * .2f + (arc - 1) * .18f,
                        Mathf.Sin(angle) * .95f));
                }
            }
        }

        private void OnDisable() => StopEffect();
    }
}
