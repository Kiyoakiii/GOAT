using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Wet hooves affect every surface without editing shared terrain materials.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GoatWeatherResponse : MonoBehaviour
    {
        private Collider[] contacts;
        private PhysicsMaterial[] originals;
        private PhysicsMaterial[] wetHooves;
        private float nextExposureCheck, exposure = 1f, appliedWetness = -1f;
        public float Wetness => WorldWeather.Current ? WorldWeather.Current.Wetness : 0f;
        public Vector3 Wind => WorldWeather.Current ? WorldWeather.Current.WindVelocity * exposure : Vector3.zero;

        private IEnumerator Start()
        {
            WorldWeather.Ensure();
            yield return null; // The articulated body creates its contact shapes in Start.
            var physicalBody = GetComponent<GoatPhysicalBody>();
            contacts = physicalBody ? physicalBody.GetRideColliders() : GetComponentsInChildren<Collider>();
            originals = new PhysicsMaterial[contacts.Length];
            wetHooves = new PhysicsMaterial[contacts.Length];
            for (int i = 0; i < contacts.Length; i++)
            {
                originals[i] = contacts[i].sharedMaterial;
                wetHooves[i] = originals[i] ? Instantiate(originals[i]) : new PhysicsMaterial();
                wetHooves[i].name = "Wet goat contact (runtime)";
                wetHooves[i].frictionCombine = PhysicsMaterialCombine.Minimum;
            }
        }

        private void FixedUpdate()
        {
            if (wetHooves == null) return;
            if (!Mathf.Approximately(appliedWetness, Wetness))
            {
                for (int i = 0; i < contacts.Length; i++)
                    if (contacts[i])
                    {
                        wetHooves[i].dynamicFriction = Mathf.Lerp(originals[i] ? originals[i].dynamicFriction : .6f, .025f, Wetness);
                        wetHooves[i].staticFriction = Mathf.Lerp(originals[i] ? originals[i].staticFriction : .6f, .035f, Wetness);
                        var material = Wetness > .001f ? wetHooves[i] : originals[i];
                        if (contacts[i].sharedMaterial != material) contacts[i].sharedMaterial = material;
                    }
                appliedWetness = Wetness;
            }
            if (Time.time < nextExposureCheck) return;
            nextExposureCheck = Time.time + .2f;
            var weather = WorldWeather.Current;
            exposure = weather && WorldWeather.ClearAir(transform.position + Vector3.up,
                -weather.WindDirection, 12f) ? 1f : 0f;
        }

        private void OnDestroy()
        {
            if (contacts != null)
                for (int i = 0; i < contacts.Length; i++) if (contacts[i]) contacts[i].sharedMaterial = originals[i];
            if (wetHooves != null) foreach (var material in wetHooves) if (material) Destroy(material);
        }
    }
}
