using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Small runtime smoke check: it reports only facts observed after physics has started.</summary>
    public sealed class PrototypeRuntimeValidator : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Let Awake/Start and several physics ticks establish grounding and the following camera.
            for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();

            var goat = FindFirstObjectByType<GoatController>();
            var body = goat ? goat.GetComponent<Rigidbody>() : null;
            var camera = Camera.main;
            int ledges = GameObject.Find("Ledges — guaranteed descent route")?.transform.childCount ?? 0;
            bool ready = goat && body && !body.isKinematic && camera && ledges >= 11;
            Debug.Log($"GOAT_DESCENT_RUNTIME_CHECK ready={ready} goat={(goat ? goat.name : "missing")} " +
                      $"body={(body ? "dynamic" : "missing")} camera={(camera ? "present" : "missing")} " +
                      $"ledges={ledges} grounded={(goat && goat.Grounded)} speed={(goat ? goat.Velocity.magnitude : 0f):0.00}");
            if (!ready) Debug.LogError("GOAT_DESCENT_RUNTIME_CHECK failed: required playable objects were not created.");
        }
    }
}
