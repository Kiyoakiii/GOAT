using UnityEngine;

namespace GoatDescent
{
    /// <summary>Comic-book feedback shared by every goat move.</summary>
    public sealed class GoatSpectacle : MonoBehaviour
    {
        private static readonly Color Pink = new Color(1f, .31f, .52f);
        private static readonly Color Yellow = new Color(1f, .83f, .27f);
        private static readonly Color Blue = new Color(.22f, .82f, 1f);
        private static readonly Color Mint = new Color(.36f, 1f, .65f);

        private static Material[] confettiMaterials;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private ThirdPersonGoatCamera cameraRig;
        private GUIStyle frontStyle;
        private GUIStyle shadowStyle;
        private string caption;
        private Color captionColor;
        private float captionEnd;
        private float nextFootstep;
        private Material snowDust;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ground = GetComponent<GoatGroundDetector>();
        }

        private void Start()
        {
            snowDust = new Material(Shader.Find("Standard")) { color = new Color(.73f, .81f, .85f) };
            cameraRig = Camera.main ? Camera.main.GetComponent<ThirdPersonGoatCamera>() : null;
            if (confettiMaterials == null)
            {
                confettiMaterials = new Material[4];
                Color[] colors = { Pink, Yellow, Blue, Mint };
                for (int i = 0; i < colors.Length; i++)
                    confettiMaterials[i] = new Material(Shader.Find("Standard")) { color = colors[i] };
            }
        }

        private void Update()
        {
            if (!body || !ground) return;
            Vector3 horizontal = body.linearVelocity;
            horizontal.y = 0f;
            if (ground.IsGrounded && horizontal.magnitude > 3f && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time + .17f;
                Emit(transform.position + Vector3.up * .1f, 3, .7f, .10f, true);
            }
        }

        public void Jump(bool rocket)
        {
            Show(rocket ? "ROCKET GOAT!" : "BOING!", rocket ? Pink : Yellow,
                rocket ? 2.2f : 1.2f, rocket ? 38 : 18);
        }

        public void AirJump() => Show("DOUBLE BOING!", Blue, 1.5f, 25);
        public void Stomp() => Show("HOOVES OF DOOM!", Pink, 1.7f, 20);
        public void StompBounce() => Show("MEGA BOUNCE!", Mint, 2.3f, 38);
        public void WallJump() => Show("WALL BOING!", Blue, 1.7f, 30);
        public void SuperHooves() => Show("СУПЕРКОПЫТА! ДЕРЖИСЬ!", Mint, 1.8f, 28);
        public void RainbowDash() => Show("RAINBOW GOAT!", Pink, 2.6f, 55);
        public void Bumper() => Show("BONK!", Blue, 2.2f, 42);
        public void Crash() => Show("БА-БАХ! КОЗЁЛ ВДРЕБЕЗГИ!", Yellow, 3.4f, 55);
        public void Finish() => Show("MOUNTAIN SURVIVED!", Mint, 3f, 90);
        public void AvalancheHit() => Show("SNOWBALL BONK!", Blue, 2.8f, 65);

        public void Land(float impact)
        {
            if (impact < 3f) return;
            float strength = Mathf.Clamp(impact / 7f, .8f, 2f);
            Show(impact > 8f ? "KABOOM!" : "THUMP!", Yellow, strength, impact > 8f ? 35 : 18,
                transform.position + Vector3.up * .12f);
        }

        private void Show(string text, Color color, float strength, int particles)
        {
            Show(text, color, strength, particles, transform.position + Vector3.up * .75f);
        }

        private void Show(string text, Color color, float strength, int particles, Vector3 position)
        {
            caption = text;
            captionColor = color;
            captionEnd = Time.unscaledTime + .8f;
            Emit(position, particles, 2.2f + strength, .12f + strength * .035f);
            ComicImpactRing.Create(position, color, strength);
            cameraRig ??= Camera.main ? Camera.main.GetComponent<ThirdPersonGoatCamera>() : null;
            cameraRig?.Kick(strength * .09f, strength * 2.2f);
        }

        private void OnDestroy() { if (snowDust) Destroy(snowDust); }
        private void Emit(Vector3 position, int count, float speed, float size, bool dust = false)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 velocity = Random.onUnitSphere * speed;
                velocity.y = Mathf.Abs(velocity.y) + speed * .22f;
                GameObject piece = GameObject.CreatePrimitive(dust ? PrimitiveType.Sphere : PrimitiveType.Quad);
                piece.name = dust ? "Powder snow" : "Paper confetti";
                piece.transform.position = position;
                piece.transform.localScale = Vector3.one * size * Random.Range(.65f, 1.4f);
                Destroy(piece.GetComponent<Collider>());
                piece.GetComponent<Renderer>().sharedMaterial = dust ? snowDust : confettiMaterials[i % 4];
                piece.AddComponent<GoatConfettiPiece>().Launch(velocity, Random.Range(.45f, .95f));
            }
        }

        private void OnGUI()
        {
            float remaining = captionEnd - Time.unscaledTime;
            if (remaining <= 0f || string.IsNullOrEmpty(caption)) return;
            float opacity = Mathf.Clamp01(remaining * 3f);
            float pop = 1f + Mathf.Clamp01((.8f - remaining) * 10f) * .08f;
            int size = Mathf.RoundToInt(32f * pop);
            frontStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            shadowStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            frontStyle.fontSize = size;
            shadowStyle.fontSize = size;
            Rect banner = new Rect(Screen.width * .5f - 270f, Screen.height * .67f, 540f, 58f);
            GUI.color = new Color(.16f, .13f, .24f, opacity * .75f);
            GUI.DrawTexture(new Rect(banner.x + 25f, banner.y + 7f, banner.width - 50f, banner.height - 14f), Texture2D.whiteTexture);
            shadowStyle.normal.textColor = new Color(.1f, .1f, .16f, opacity);
            frontStyle.normal.textColor = new Color(captionColor.r, captionColor.g, captionColor.b, opacity);
            GUI.color = Color.white;
            GUI.Label(new Rect(banner.x + 4f, banner.y + 4f, banner.width, banner.height), caption, shadowStyle);
            GUI.Label(banner, caption, frontStyle);
            GUI.color = Color.white;
        }
    }

    public sealed class GoatConfettiPiece : MonoBehaviour
    {
        private Vector3 velocity;
        private float lifetime;
        private float born;
        private Vector3 initialScale;

        public void Launch(Vector3 initialVelocity, float duration)
        {
            velocity = initialVelocity;
            lifetime = duration;
            born = Time.time;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            float age = Time.time - born;
            if (age >= lifetime) { Destroy(gameObject); return; }
            velocity += Physics.gravity * Time.deltaTime * .55f;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(190f * Time.deltaTime, 280f * Time.deltaTime, 90f * Time.deltaTime);
            transform.localScale = initialScale * (1f - age / lifetime);
        }
    }

    public sealed class ComicImpactRing : MonoBehaviour
    {
        private LineRenderer line;
        private Color tint;
        private float strength;
        private float born;

        public static void Create(Vector3 position, Color color, float power)
        {
            GameObject go = new GameObject("Comic impact ring");
            go.transform.position = position;
            var ring = go.AddComponent<ComicImpactRing>();
            ring.tint = color;
            ring.strength = power;
        }

        private void Awake()
        {
            born = Time.time;
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.widthMultiplier = .12f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            float t = (Time.time - born) / .48f;
            if (t >= 1f) { Destroy(gameObject); return; }
            float radius = .3f + t * (1.6f + strength * .65f);
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .03f, Mathf.Sin(angle) * radius));
            }
            Color color = new Color(tint.r, tint.g, tint.b, 1f - t);
            line.startColor = color;
            line.endColor = color;
        }
    }
}
