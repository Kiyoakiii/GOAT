using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatSpawner : MonoBehaviour
    {
        [SerializeField] private float initialYaw = 0f;

        private void Start()
        {
            StartCoroutine(SpawnNextFrame());
        }

        private IEnumerator SpawnNextFrame()
        {
            yield return null;
            var marker = GameObject.Find("Pillar Goat Spawn");
            Vector3 position = marker ? marker.transform.position : transform.position;
            float yaw = marker ? marker.transform.rotation.eulerAngles.y : initialYaw;
            Debug.Log("GOAT_SPAWNER_START marker=" + (marker != null) + " pos=" + position + " mainCamera=" + (Camera.main != null));
            GoatPlayerFactory.Create(position, yaw, true);
            Debug.Log("GOAT_SPAWNER_DONE");
        }
    }
}