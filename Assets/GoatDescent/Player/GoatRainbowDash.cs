using UnityEngine;

namespace GoatDescent
{
    /// <summary>One spectacular forward dash per flight, refreshed by landing.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class GoatRainbowDash : MonoBehaviour
    {
        private const float DashSpeed = 16f;
        private const float DashDuration = .38f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private TrailRenderer trail;
        private Camera view;
        private Vector3 direction;
        private float dashEnds;
        private bool usedInAir;
        private bool wasAirborne;

        public bool Ready => !usedInAir;
        public void ResetDash()
        {
            dashEnds = -1f; usedInAir = false; wasAirborne = false;
            if (trail) { trail.emitting = false; trail.Clear(); }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ground = GetComponent<GoatGroundDetector>();
        }

        private void Start()
        {
            view = Camera.main;
            var trailObject = new GameObject("Rainbow dash trail");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = new Vector3(0f, .8f, -.4f);
            trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = .8f;
            trail.minVertexDistance = .08f;
            trail.widthMultiplier = .85f;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            trail.colorGradient = Rainbow();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
        }

        private void Update()
        {
            if (!body || !ground || GetComponent<GoatWallJumpController>()?.IsAiming == true) return;
            if (!ground.IsGrounded) wasAirborne = true;
            else if (wasAirborne)
            {
                usedInAir = false;
                wasAirborne = false;
            }

            if (Input.GetKeyDown(KeyCode.Q) && !ground.IsGrounded && !usedInAir)
                Dash();

            if (trail) trail.emitting = Time.time < dashEnds;
        }

        private void FixedUpdate()
        {
            if (!body || Time.time >= dashEnds) return;
            body.linearVelocity = direction * DashSpeed + Vector3.up * Mathf.Max(1.5f, body.linearVelocity.y);
        }

        private void Dash()
        {
            GetComponent<GoatGripController>()?.ReleaseForJump();
            view ??= Camera.main;
            direction = view ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : transform.forward;
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            usedInAir = true;
            dashEnds = Time.time + DashDuration;
            body.linearVelocity = direction * DashSpeed + Vector3.up * Mathf.Max(2.5f, body.linearVelocity.y);
            trail?.Clear();
            GetComponent<GoatVisualController>()?.PlayTakeoff(.26f);
            GetComponent<GoatSpectacle>()?.RainbowDash();
            GetComponent<GoatJumpController>()?.AnnounceTrick("RAINBOW GOAT!  ZOOOOM!");
            RainbowDashRing.Create(transform.position + Vector3.up * .8f, direction);
        }

        private static Gradient Rainbow()
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(1f, .35f, .50f), 0f),
                    new GradientColorKey(new Color(1f, .85f, .32f), .25f),
                    new GradientColorKey(new Color(.35f, .95f, .65f), .5f),
                    new GradientColorKey(new Color(.30f, .70f, 1f), .75f),
                    new GradientColorKey(new Color(.80f, .45f, 1f), 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            };
        }
    }

    public sealed class RainbowDashRing : MonoBehaviour
    {
        private LineRenderer line;
        private float born;

        public static void Create(Vector3 position, Vector3 forward)
        {
            var go = new GameObject("Rainbow dash shockwave");
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(forward);
            go.AddComponent<RainbowDashRing>();
        }

        private void Awake()
        {
            born = Time.time;
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = .13f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            float progress = (Time.time - born) / .48f;
            if (progress >= 1f) { Destroy(gameObject); return; }
            float radius = .5f + progress * 3.2f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
            line.startColor = new Color(1f, .42f, .68f, 1f - progress);
            line.endColor = new Color(.4f, .9f, 1f, 1f - progress);
        }
    }
}
