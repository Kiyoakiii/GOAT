using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>Silver rain and inexpensive blue-grey square splashes.</summary>
    [DefaultExecutionOrder(300)]
    public sealed class WeatherRain : MonoBehaviour
    {
        private ParticleSystem rain, splashes;
        private Material dropMaterial, splashMaterial;
        private float nearEmission, distantEmission;
        private int environmentMask;
        private readonly System.Random random = new System.Random(48271);

        private void Start()
        {
            rain = CreateParticles("Rain drops", out dropMaterial, false);
            var main = rain.main;
            main.maxParticles = 6000; main.startLifetime = 2.6f; main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .065f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.65f, .73f, .8f, .24f), new Color(.85f, .9f, .95f, .46f));
            var shape = rain.shape; shape.enabled = false;
            // Emit exposed world positions individually. A roof above the viewer
            // must never switch off rain outside the cave or beside a cliff.
            var emission = rain.emission; emission.enabled = false;
            var collision = rain.collision;
            collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.enableDynamicColliders = false; collision.dampen = 1; collision.bounce = 0;
            collision.lifetimeLoss = 1; collision.maxCollisionShapes = 128;
            int playerLayer = LayerMask.NameToLayer("Player");
            environmentMask = playerLayer < 0 ? ~0 : ~(1 << playerLayer);
            collision.collidesWith = environmentMask;
            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2; renderer.velocityScale = .023f;
            splashes = CreateParticles("Rain square splashes", out splashMaterial, true);
            // Two tiny quads on a small fraction of existing rain collisions.
            // No splash colliders, lights, textures or per-impact GameObjects.
            main = splashes.main; main.maxParticles = 128;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.24f, .38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.9f, 1.6f);
            main.gravityModifier = .55f;
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .085f);
            main.startColor = new Color(.48f, .61f, .7f, .72f);
            splashes.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var splashShape = splashes.shape; splashShape.enabled = true;
            splashShape.shapeType = ParticleSystemShapeType.Cone;
            splashShape.angle = 50; splashShape.radius = .025f;
            emission = splashes.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 2) });
            var size = splashes.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .45f));
            var color = splashes.colorOverLifetime; color.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = fade;
            splashes.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            var sub = rain.subEmitters; sub.enabled = true;
            sub.AddSubEmitter(splashes, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, .1f);
            splashes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rain.Play();
        }

        private ParticleSystem CreateParticles(string label, out Material material, bool splash)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.playOnAwake = false; main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            material = new Material(Resources.Load<Shader>("WeatherRain")); material.SetFloat("_Splash", splash ? 1 : 0);
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return particles;
        }

        private void LateUpdate()
        {
            var camera = Camera.main; var weather = WorldWeather.Current;
            if (!rain || !camera || !weather) return;
            Vector3 drift = weather.WindDirection * (weather.Wind * weather.Gust * 7);
            Vector3 eye = camera.transform.position;
            rain.transform.position = eye;
            // Fill a vertical volume, rather than making every new drop travel
            // down from a sheet above the camera. This also follows fast descents.
            EmitExposed(eye, 46, drift, weather.Rain * 2400, ref nearEmission);
            Vector3 look = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            // Keep rain visible through an entrance even from deeper in a cave.
            EmitExposed(eye + look * 42, 64, drift, weather.Rain * 800, ref distantEmission);
        }

        private void EmitExposed(Vector3 center, float width, Vector3 drift, float rate, ref float remainder)
        {
            remainder += rate * Mathf.Min(Time.deltaTime, .1f);
            int count = Mathf.FloorToInt(remainder);
            remainder -= count;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = center + new Vector3((Next() - .5f) * width,
                    Mathf.Lerp(-6, 18, Next()), (Next() - .5f) * width);
                // Overlap catches samples inside thick roofs; the upward ray
                // rejects only this column. Existing outdoor drops stay alive.
                if (Physics.CheckSphere(position, .03f, environmentMask, QueryTriggerInteraction.Ignore)
                    || !WorldWeather.ClearAir(position, Vector3.up, 1024)) continue;
                rain.Emit(new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = drift + Vector3.down * 22,
                    applyShapeToPosition = false
                }, 1);
            }
        }

        private float Next() => (float)random.NextDouble();

        private void OnDestroy()
        {
            if (dropMaterial) Destroy(dropMaterial);
            if (splashMaterial) Destroy(splashMaterial);
        }
    }
}
