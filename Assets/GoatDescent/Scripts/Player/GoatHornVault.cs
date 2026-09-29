using UnityEngine;

namespace GoatDescent
{
    /// <summary>A deliberate horn strike sends the goat across a short, skippable section of cliff.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GoatHornVault : MonoBehaviour
    {
        [SerializeField] private float flightSeconds = 2.2f;
        [SerializeField] private float activationReach = 2.4f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private RespawnController life;
        private HornLaunchStone[] stones;
        private Collider destination;
        private float launchedAt;
        private int protectedLandingFrame = -1;

        public bool IsVaulting { get; private set; }
        public HornLaunchStone NearbyStone { get; private set; }

        private void Awake() => CacheComponents();
        private void OnEnable() { CacheComponents(); stones = null; Cancel(); }
        private void OnDisable() => Cancel();

        private void CacheComponents()
        {
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
            life ??= GetComponent<RespawnController>();
        }

        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            CacheComponents();
            if (stones == null) stones = FindObjectsByType<HornLaunchStone>(FindObjectsSortMode.None);
            NearbyStone = null;
            if (IsVaulting || !ground || !ground.IsGrounded || (life && life.IsDead)
                || (GetComponent<GoatController>()?.IsPredatorCarried ?? false)) return;

            float closest = activationReach * activationReach;
            foreach (var stone in stones)
            {
                if (!stone || !stone.gameObject.activeInHierarchy) continue;
                float distance = (stone.LaunchPoint - transform.position).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                NearbyStone = stone;
            }
            if (NearbyStone && GoatLocalControl.AllowsInput(this) && Input.GetKeyDown(KeyCode.G)) Launch(NearbyStone);
        }

        public void Launch(HornLaunchStone stone)
        {
            CacheComponents();
            if (!body || !stone || IsVaulting || (GetComponent<GoatController>()?.IsPredatorCarried ?? false)) return;
            GetComponent<GoatInteraction>()?.CancelAll();
            Vector3 start = stone.LaunchPoint;
            Vector3 end = stone.Destination.LandingPoint + Vector3.up * .18f;
            transform.position = start;
            body.position = start;
            GetComponent<GoatPhysicalBody>()?.ResetPose();
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = (end - start) / flightSeconds - Physics.gravity * (.5f * flightSeconds);
            Physics.SyncTransforms();
            destination = stone.Destination.GetComponent<BoxCollider>();
            IsVaulting = true;
            launchedAt = Time.time;
            protectedLandingFrame = -1;
            stone.Level.NotifyVaultStarted(stone);
        }

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            if (IsVaulting && Time.time - launchedAt > flightSeconds + 1.25f) Cancel();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsVaulting || collision.collider != destination) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < .45f) continue;
                protectedLandingFrame = Time.frameCount;
                IsVaulting = false;
                body.linearVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up) * .25f;
                collision.collider.GetComponent<DescentLedge>()?.Level.NotifyVaultCompleted();
                return;
            }
        }

        public bool ProtectsLanding(Collision collision) =>
            collision.collider == destination && (IsVaulting || protectedLandingFrame == Time.frameCount);

        public void Cancel()
        {
            IsVaulting = false;
            NearbyStone = null;
            destination = null;
            protectedLandingFrame = -1;
        }
    }

    public sealed class HornLaunchStone : MonoBehaviour
    {
        public PillarDescentLevel Level { get; private set; }
        public DescentLedge Destination { get; private set; }
        public Vector3 LaunchPoint => transform.position + Vector3.up * .08f;
        public int SkippedLedges { get; private set; }

        public void Initialize(PillarDescentLevel level, DescentLedge source, DescentLedge destination)
        {
            Level = level;
            Destination = destination;
            SkippedLedges = destination.Index - source.Index - 1;
        }
    }
}
