using UnityEngine;

namespace GoatDescent
{
    /// <summary>Starts the normal player in the small scene used to try movement changes.</summary>
    public sealed class MovementTestBootstrap : MonoBehaviour
    {
        private void Start()
        {
            if (FindFirstObjectByType<GoatController>() != null) return;


            var marker = GameObject.Find("Goat Spawn");
            Vector3 spawn = marker ? marker.transform.position : new Vector3(0f, 0.12f, -9f);
            GoatPlayerFactory.Create(spawn);
            gameObject.AddComponent<MovementTestTimeControls>();
        }
    }
}
