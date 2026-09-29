using UnityEngine;

namespace GoatDescent
{
    /// <summary>Brief hoof grip on steep sandstone, with a climb and a wall kick.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(GoatGroundDetector))]
    public sealed class GoatCliffGrip : MonoBehaviour
    {
        [SerializeField] private float gripSeconds = 2.2f;
        [SerializeField] private float gripReach = 1.65f;
        [SerializeField] private float climbSpeed = 1.65f;
        [SerializeField] private float shimmySpeed = 1.8f;
        [SerializeField] private float wallKickUp = 5.8f;
        [SerializeField] private float wallKickOut = 3.6f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private GoatHornVault hornVault;
        private float stamina;
        private float blockedUntil;
        private bool wantsGrip;
        private bool remoteGrip;
        private float remoteSide, remoteClimb;
        private Vector3 remoteCameraRight;
        public void SetRemoteInput(bool grip, float side, float climb, Vector3 cameraRight)
        { remoteGrip = grip; remoteSide = side; remoteClimb = climb; remoteCameraRight = cameraRight; }
        public void NetworkWallKick() { if (IsGripping) WallKick(); }
        private Vector3 wallNormal;

        public bool IsGripping { get; private set; }
        public float Stamina01 => gripSeconds > 0f ? Mathf.Clamp01(stamina / gripSeconds) : 0f;
        public int LastWallKickFrame { get; private set; } = -1;

        private void Awake() => CacheComponents();
        private void OnEnable() { CacheComponents(); stamina = gripSeconds; blockedUntil = -100f; LastWallKickFrame = -1; }
        private void OnDisable()
        {
            IsGripping = false;
            wantsGrip = false;
            if (body) body.useGravity = true;
        }
        private void CacheComponents()
        {
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
            hornVault ??= GetComponent<GoatHornVault>();
        }
        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            if (GetComponent<GoatController>()?.NetworkOwnedByRemote ?? false)
            { wantsGrip = remoteGrip; return; }
            if (!GoatLocalControl.AllowsInput(this)) { ClearInput(); return; }
            wantsGrip = Input.GetKey(KeyCode.LeftShift);
            if (IsGripping && Input.GetKeyDown(KeyCode.Space)) WallKick();
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            CacheComponents();
            if (!body || !ground) return;
            if (GetComponent<GoatController>()?.IsPredatorCarried ?? false) { Release(); return; }
            if (hornVault && hornVault.IsVaulting) { Release(); return; }
            if (ground.IsGrounded && ground.SlopeAngle < 55f)
            {
                Release();
                stamina = Mathf.MoveTowards(stamina, gripSeconds, Time.fixedDeltaTime * 1.8f);
                return;
            }
            if (!wantsGrip || stamina <= 0f || Time.time < blockedUntil)
            {
                Release();
                return;
            }
            if (!TryFindWall(out RaycastHit wall))
            {
                Release();
                return;
            }
            IsGripping = true;
            wallNormal = wall.normal;
            body.useGravity = false;
            stamina = Mathf.Max(0f, stamina - Time.fixedDeltaTime);

            bool remote = GetComponent<GoatController>()?.NetworkOwnedByRemote ?? false;
            Vector3 cameraRight = remote ? remoteCameraRight : Camera.main ? Camera.main.transform.right : transform.right;
            Vector3 alongWall = Vector3.ProjectOnPlane(cameraRight, wallNormal);
            alongWall.y = 0f;
            if (alongWall.sqrMagnitude < .01f) alongWall = Vector3.Cross(Vector3.up, wallNormal);
            alongWall.Normalize();
            float side = remote ? remoteSide : (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float climb = remote ? remoteClimb : (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 targetVelocity = alongWall * (side * shimmySpeed) + Vector3.up * (climb * climbSpeed - .25f) - wallNormal * .35f;
            var interaction = GetComponent<GoatInteraction>();
            if (interaction && interaction.IsLinked)
            {
                // Limited grip effort: a partner can pull the climber off the rock.
                body.useGravity = true;
                body.AddForce(Vector3.ClampMagnitude((targetVelocity - body.linearVelocity) * 8f - Physics.gravity, 16f), ForceMode.Acceleration);
                stamina = Mathf.Max(0f, stamina - Time.fixedDeltaTime * .6f);
            }
            else body.linearVelocity = targetVelocity;
        }

        private bool TryFindWall(out RaycastHit best)
        {
            best = default;
            float closest = float.PositiveInfinity;
            Vector3 origin = transform.position + Vector3.up * .72f;
            int playerLayer = LayerMask.NameToLayer("Player");
            int worldMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                if (!Physics.SphereCast(origin, .3f, direction, out RaycastHit hit, gripReach, worldMask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y > .55f || hit.normal.y < -.25f) continue;
                bool climbable = hit.collider.name == "Stratified sandstone" || hit.collider.GetComponentInParent<PillarDescentLevel>() != null;
                if (!climbable || hit.distance >= closest) continue;
                closest = hit.distance;
                best = hit;
            }
            return closest < float.PositiveInfinity;
        }

        private void WallKick()
        {
            LastWallKickFrame = Time.frameCount;
            Release();
            blockedUntil = Time.time + .35f;
            body.linearVelocity = wallNormal * wallKickOut + Vector3.up * wallKickUp;
        }

        private void Release()
        {
            if (!IsGripping) return;
            IsGripping = false;
            body.useGravity = true;
        }
        public void ClearInput() { wantsGrip = false; Release(); }
        public void BreakGrip(float seconds) { ClearInput(); blockedUntil = Time.time + seconds; }
    }
}
