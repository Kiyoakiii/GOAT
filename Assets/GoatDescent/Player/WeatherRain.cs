using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>Silver rain, wind-driven drops and brief ground rings; no screen-space darkening.</summary>
    public sealed class WeatherRain : MonoBehaviour
    {
        private ParticleSystem rain, splashes;
        private Material dropMaterial, splashMaterial;
        private float nextShelterCheck;
        private bool sheltered;

        private void Start()
        {
            rain = CreateParticles("Rain drops", out dropMaterial, false);
            var main = rain.main;
            main.maxParticles = 5000; main.startLifetime = 2.6f; main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .065f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.65f, .73f, .8f, .24f), new Color(.85f, .9f, .95f, .46f));
            var shape = rain.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(46, 2, 46);
            var emission = rain.emission; emission.rateOverTime = 0;
            var collision = rain.collision;
            collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.enableDynamicColliders = false; collision.dampen = 1; collision.bounce = 0;
            collision.lifetimeLoss = 1; collision.maxCollisionShapes = 128;
            int playerLayer = LayerMask.NameToLayer("Player");
            collision.collidesWith = playerLayer < 0 ? ~0 : ~(1 << playerLayer);
            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2; renderer.velocityScale = .023f;
            splashes = CreateParticles("Rain ground rings", out splashMaterial, true);
            main = splashes.main; main.maxParticles = 180; main.startLifetime = .28f;
            main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.13f, .3f);
            main.startColor = new Color(.65f, .75f, .82f, .24f);
            var splashShape = splashes.shape; splashShape.enabled = false;
            emission = splashes.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            var size = splashes.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .15f, 1, 1));
            var color = splashes.colorOverLifetime; color.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = fade;
            splashes.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            var sub = rain.subEmitters; sub.enabled = true;
            sub.AddSubEmitter(splashes, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, .12f);
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
            if (Time.time >= nextShelterCheck)
            {
                bool wasSheltered = sheltered;
                sheltered = !WorldWeather.ClearAir(camera.transform.position, Vector3.up, 200);
                nextShelterCheck = Time.time + .2f;
                if (sheltered && !wasSheltered) rain.Clear(true);
            }
            Vector3 drift = weather.WindDirection * (weather.Wind * weather.Gust * 7);
            rain.transform.position = camera.transform.position + Vector3.up * 15 - drift * .7f;
            var emission = rain.emission; emission.rateOverTime = sheltered ? 0 : weather.Rain * 1900;
            var velocity = rain.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = drift.x; velocity.y = -22; velocity.z = drift.z;
        }

        private void OnDestroy()
        {
            if (dropMaterial) Destroy(dropMaterial);
            if (splashMaterial) Destroy(splashMaterial);
        }
    }
}
