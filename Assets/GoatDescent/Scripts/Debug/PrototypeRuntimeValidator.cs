using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Small runtime smoke check: it reports only facts observed after physics has started.</summary>
    public sealed class PrototypeRuntimeValidator : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Wait through the spawn drop: this verifies a real dynamic Rigidbody-to-platform contact, not just scene creation.
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();

            var goat = FindFirstObjectByType<GoatController>();
            var body = goat ? goat.GetComponent<Rigidbody>() : null;
            var camera = Camera.main;
            var route = GameObject.Find("Ledges — guaranteed descent route")?.transform;
            int ledges = route ? route.childCount : 0;
            int solidLandings = 0;
            if (route)
            {
                foreach (var collider in route.GetComponentsInChildren<BoxCollider>(true))
                    if (collider.enabled && collider.size.y >= .38f) solidLandings++;
            }
            bool groundedOnRoute = goat && goat.Grounded && goat.GetComponent<GoatGroundDetector>()?.GroundHit.collider is BoxCollider;
            bool ready = goat && body && !body.isKinematic && camera && ledges >= 11 && solidLandings >= 55 && groundedOnRoute;
            Debug.Log($"GOAT_DESCENT_RUNTIME_CHECK ready={ready} goat={(goat ? goat.name : "missing")} " +
                      $"body={(body ? "dynamic" : "missing")} camera={(camera ? "present" : "missing")} " +
                      $"ledges={ledges} solidLandings={solidLandings} groundedOnRoute={groundedOnRoute} " +
                      $"speed={(goat ? goat.Velocity.magnitude : 0f):0.00}");
            if (!ready) Debug.LogError("GOAT_DESCENT_RUNTIME_CHECK failed: required playable objects were not created.");
        }
    }
}
