using UnityEngine;

namespace GoatDescent
{
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField] private float killPlane = -12f;
        private Rigidbody body; private Vector3 spawn;
        public void Configure(Rigidbody targetBody, Vector3 targetSpawn) { body = targetBody; spawn = targetSpawn; }
        private void Awake() { body ??= GetComponent<Rigidbody>(); }
        private void Update()
        {
            body ??= GetComponent<Rigidbody>();
            if (Input.GetKeyDown(KeyCode.R)) Respawn();
            else if (transform.position.y < killPlane) Respawn();
        }
        public void Respawn()
        {
            Teleport(spawn);
            SlopeRun.Instance?.ResetRun();
        }
        public void Teleport(Vector3 position)
        {
            GetComponent<GoatCrashExplosion>()?.Restore();
            transform.SetPositionAndRotation(position, Quaternion.identity);
            if (body) { body.position = position; body.rotation = Quaternion.identity; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.WakeUp(); }
            GetComponent<GoatJumpController>()?.ResetJumpState();
            GetComponent<GoatWallJumpController>()?.ResetWallJumps();
            GetComponent<GoatGripController>()?.ResetGrip();
            GetComponent<GoatSlopeBalance>()?.ResetBalance();
            GetComponent<GoatRainbowDash>()?.ResetDash();
            Camera.main?.GetComponent<ThirdPersonGoatCamera>()?.Snap();
        }
    }
}
