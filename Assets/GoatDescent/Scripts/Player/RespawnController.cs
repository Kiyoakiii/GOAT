using UnityEngine;

namespace GoatDescent
{
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField] private float killPlane = -12f;
        private Rigidbody body; private Vector3 spawn;
        public void Configure(Rigidbody targetBody, Vector3 targetSpawn) { body = targetBody; spawn = targetSpawn; }
        private void Awake() { body ??= GetComponent<Rigidbody>(); if (spawn == Vector3.zero) spawn = MountainPrototypeBuilder.SpawnPoint; }
        private void Update() { body ??= GetComponent<Rigidbody>(); if (Input.GetKeyDown(KeyCode.R) || transform.position.y < killPlane) Respawn(); }
        public void Respawn() { transform.SetPositionAndRotation(spawn, Quaternion.identity); if (body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; } }
    }
}
