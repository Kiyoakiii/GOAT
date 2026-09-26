using UnityEngine;

namespace GoatDescent
{
    /// <summary>Hold E against a steep face, aim with the mouse, release to launch.</summary>
    public sealed class GoatWallJumpController : MonoBehaviour
    {
        private const float AimSeconds = 1.5f, LaunchSpeed = 11.5f;
        private GoatController controller;
        private GoatGroundDetector ground;
        private Rigidbody body;
        private int jumpsUsed;
        private float lastJump = -100f, aimStarted, aimYaw, aimElevation;
        private Vector3 normal;
        private LineRenderer arc, marker;
        private Material lineMaterial;
        public bool IsAiming { get; private set; }
        public bool WallNearby { get; private set; }
        public int JumpsLeft => Mathf.Max(0, 2 - jumpsUsed);
        public float AimTimeLeft => IsAiming ? Mathf.Max(0f, AimSeconds - (Time.unscaledTime - aimStarted)) : 0f;
        public Vector3 LaunchVelocity
        {
            get
            {
                Vector3 away = Quaternion.AngleAxis(aimYaw, Vector3.up) * normal;
                float angle = aimElevation * Mathf.Deg2Rad;
                return (away * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * LaunchSpeed;
            }
        }
        public void Configure(GoatController c, GoatGroundDetector g) { controller = c; ground = g; }
        private void Awake() { controller = GetComponent<GoatController>(); ground = GetComponent<GoatGroundDetector>(); body = GetComponent<Rigidbody>(); }
        private void Start()
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            arc = MakeLine("Wall jump flight preview", .065f);
            marker = MakeLine("Wall jump landing preview", .055f);
            marker.loop = true;
        }
        private LineRenderer MakeLine(string name, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.material = lineMaterial; line.useWorldSpace = true;
            line.widthMultiplier = width; line.enabled = false;
            line.startColor = new Color(1f,.76f,.25f); line.endColor = new Color(1f,.91f,.6f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
        public void SetAimAngles(float yaw, float elevation)
        { aimYaw = Mathf.Clamp(yaw,-70f,70f); aimElevation = Mathf.Clamp(elevation,10f,65f); }
        public bool TryBeginAim()
        {
            if (IsAiming || !body || body.isKinematic || JumpsLeft == 0 || Time.time - lastJump < .22f
                || !GoatCliffProbe.Find(transform,out var hit)) return false;
            normal = Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
            if (normal.sqrMagnitude < .1f) return false;
            SetAimAngles(0f,38f); aimStarted = Time.unscaledTime; IsAiming = true;
            GetComponent<GoatRainbowDash>()?.ResetDash();
            body.linearVelocity = Vector3.zero;
            MovementTestTimeControls.Instance?.SetAimSlow(true);
            return true;
        }
        public bool ReleaseAim()
        {
            if (!IsAiming) return false;
            Vector3 velocity = LaunchVelocity;
            CancelAim();
            GetComponent<GoatGripController>()?.ReleaseForJump();
            body.linearVelocity = velocity;
            jumpsUsed++; lastJump = Time.time;
            GetComponent<GoatVisualController>()?.PlayTakeoff(.22f);
            GetComponent<GoatSpectacle>()?.WallJump();
            return true;
        }
        public void CancelAim()
        {
            IsAiming = false;
            MovementTestTimeControls.Instance?.SetAimSlow(false);
            if (arc) arc.enabled = false;
            if (marker) marker.enabled = false;
        }
        public void ResetWallJumps() { CancelAim(); jumpsUsed = 0; lastJump = -100f; WallNearby = false; }
        private void Update()
        {
            if (!body || body.isKinematic) { CancelAim(); return; }
            if (ground && ground.IsGrounded && ground.SlopeAngle < 38f && Time.time-lastJump > .25f && !IsAiming) jumpsUsed = 0;
            WallNearby = JumpsLeft > 0 && GoatCliffProbe.Find(transform,out _);
            if (Input.GetKeyDown(KeyCode.E)) TryBeginAim();
            if (!IsAiming) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelAim(); return; }
            SetAimAngles(aimYaw + Input.GetAxis("Mouse X") * 3f, aimElevation + Input.GetAxis("Mouse Y") * 2.5f);
            if (Input.GetKeyUp(KeyCode.E) || AimTimeLeft <= 0f) { ReleaseAim(); return; }
            DrawPreview();
        }
        private void FixedUpdate()
        {
            if (!IsAiming || !body || body.isKinematic) return;
            body.linearVelocity = Vector3.zero;
            body.AddForce(-Physics.gravity,ForceMode.Acceleration);
        }
        private void DrawPreview()
        {
            if (!arc || !marker) return;
            arc.enabled = true; marker.enabled = false;
            int playerLayer = LayerMask.NameToLayer("Player");
            int mask = playerLayer >= 0 ? ~(1<<playerLayer) : ~0;
            Vector3 start = transform.position + Vector3.up*.6f;
            var points = new Vector3[37]; points[0] = start; int count = 1;
            for(int i=1;i<points.Length;i++)
            {
                float t=i*.07f;
                Vector3 point=start+LaunchVelocity*t+Physics.gravity*(.5f*t*t);
                Vector3 segment=point-points[i-1];
                if(Physics.SphereCast(points[i-1],.35f,segment.normalized,out var hit,segment.magnitude,mask,QueryTriggerInteraction.Ignore))
                {
                    points[count++]=hit.point+hit.normal*.35f;
                    Vector3 tangent=Vector3.Cross(hit.normal,Vector3.forward).normalized;
                    if(tangent.sqrMagnitude<.1f)tangent=Vector3.right;
                    Vector3 bitangent=Vector3.Cross(hit.normal,tangent);
                    marker.enabled=true;marker.positionCount=24;
                    for(int j=0;j<24;j++){float a=j*Mathf.PI*2f/24f;marker.SetPosition(j,hit.point+hit.normal*.04f+(tangent*Mathf.Cos(a)+bitangent*Mathf.Sin(a))*.55f);}
                    break;
                }
                points[count++]=point;
            }
            arc.positionCount=count;
            for(int i=0;i<count;i++)arc.SetPosition(i,points[i]);
        }
        private void OnDisable()=>CancelAim();
        private void OnDestroy(){if(lineMaterial)Destroy(lineMaterial);}
    }
}
