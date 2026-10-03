using UnityEngine;

namespace GoatDescent
{
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField] private float killPlane = -12f;
        [SerializeField] private float fatalLandingSpeed = 11.5f;
        [SerializeField] private float respawnDelay = 1.2f;
        private Rigidbody body;
        private Vector3 spawn;
        private Vector3 summitSpawn;
        private RouteProgressTracker routeProgress;
        private float fallingSpeed;
        private float respawnAt;
        public bool IsDead { get; private set; }
        public bool HasCheckpoint { get; private set; }
        public void ApplyNetworkState(bool dead, bool hasCheckpoint)
        { if (!MountainAuthority.IsHost) { IsDead = dead; HasCheckpoint = hasCheckpoint; } }
        public void Configure(Rigidbody targetBody, Vector3 targetSpawn, RouteProgressTracker progress = null)
        { body = targetBody; spawn = summitSpawn = targetSpawn; routeProgress = progress; }
        public void SetCheckpoint(Vector3 point) { spawn = point; HasCheckpoint = true; }
        private void Awake()
        {
            body ??= GetComponent<Rigidbody>();
            routeProgress ??= GetComponent<RouteProgressTracker>();
            if (spawn == Vector3.zero) spawn = summitSpawn = transform.position;
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            body ??= GetComponent<Rigidbody>();
            if (!IsDead && body) fallingSpeed = Mathf.Min(0f, body.linearVelocity.y);
        }
        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            if (GoatLocalControl.AllowsInput(this) && Input.GetKeyDown(KeyCode.R)) Respawn();
            else if (GoatLocalControl.AllowsInput(this) && Input.GetKeyDown(KeyCode.Backspace))
            {
                spawn = summitSpawn; HasCheckpoint = false;
                routeProgress?.ResetToSummit();
                Teleport(summitSpawn);
            }
            else if (!IsDead && transform.position.y < killPlane) Die();
            else if (IsDead && Time.time >= respawnAt) Respawn();
            if (routeProgress && (routeProgress.CheckpointPosition - summitSpawn).sqrMagnitude > 1f)
                HasCheckpoint = true;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!MountainAuthority.IsHost) return;
            if (IsDead) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float impactSpeed = Mathf.Max(-fallingSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal)));
                if (normal.y > .45f && impactSpeed >= fatalLandingSpeed) { Die(); return; }
            }
        }
        private void Die()
        {
            SkyPredatorEpisode.Current?.ReleaseGoat(GetComponent<GoatController>(), false);
            GetComponent<GoatInteraction>()?.CancelAll();
            IsDead = true;
            respawnAt = Time.time + respawnDelay;
            if (body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true; }
            GetComponent<GoatController>().enabled = false;
            GetComponent<GoatJumpController>().enabled = false;
            GetComponent<GoatLandingAssist>().enabled = false;
            var grip = GetComponent<GoatCliffGrip>(); if (grip) grip.enabled = false;
        }
        public void Respawn()
        {
            Vector3 destination = routeProgress ? routeProgress.CheckpointPosition : spawn;
            Teleport(destination);
            routeProgress?.ResetToCheckpoint();
            SlopeRun.ResetRun();
        }
        public void Teleport(Vector3 destination)
        {
            SkyPredatorEpisode.Current?.ReleaseGoat(GetComponent<GoatController>(), false);
            GetComponent<GoatInteraction>()?.CancelAll();
            GetComponent<GoatController>()?.ClearInput();
            GetComponent<GoatCrashExplosion>()?.Restore();
            IsDead = false;
            transform.SetPositionAndRotation(destination, Quaternion.identity);
            if (body)
            {
                body.isKinematic = false; body.position = destination; body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.WakeUp();
            }
            GetComponent<GoatPhysicalBody>()?.ResetPose();
            fallingSpeed = 0f;
            GetComponent<GoatController>().enabled = true;
            var jump = GetComponent<GoatJumpController>();
            if (jump) { jump.enabled = false; jump.enabled = true; jump.ResetJumpState(); }
            GetComponent<GoatLandingAssist>().enabled = true;
            var grip = GetComponent<GoatCliffGrip>();
            if (grip) { grip.enabled = false; grip.enabled = true; }
            GetComponent<GoatWallJumpController>()?.ResetWallJumps();
            GetComponent<GoatGripController>()?.ResetGrip();
            GetComponent<GoatSlopeBalance>()?.ResetBalance();
            GetComponent<GoatRainbowDash>()?.ResetDash();
            Camera.main?.GetComponent<ThirdPersonGoatCamera>()?.Snap();
        }
        public void ResetTo(Vector3 point, Quaternion rotation)
        {
            spawn = point;
            Teleport(point);
            transform.rotation = rotation;
            body.position = point;
            body.rotation = rotation;
            GetComponent<GoatPhysicalBody>()?.ResetPose();
        }
    }
}
