using UnityEngine;

namespace GoatDescent
{
    /// <summary>A deliberately plain, self-contained skill prototype for the first level.</summary>
    public sealed class CliffBalancePrototypeBootstrap : MonoBehaviour
    {
        public const float TimeLimit = 90f;
        public CliffBalanceGoat Goat { get; private set; }
        public CliffVeinField Veins { get; private set; }
        public bool Started { get; private set; }
        public bool Completed { get; private set; }
        public bool CheckpointReached { get; private set; }
        public bool PredatorActive { get; private set; }
        public float Elapsed => Started ? (Completed ? finishTime : Time.time - startTime) : 0f;
        public float Remaining => Mathf.Max(0f, TimeLimit - Elapsed);
        public string Message { get; private set; } = string.Empty;

        private float startTime, finishTime, messageUntil;
        private Material stone, darkStone, caveStone, ember, fire;
        private Transform caveRoot;

        private void Start()
        {
            Build();
            gameObject.AddComponent<CliffBalanceHud>().Initialize(this);
        }

        private void Update()
        {
            if (Started && !Completed && !PredatorActive && Elapsed >= TimeLimit)
            {
                PredatorActive = true;
                ShowMessage("PredatorActive — пора укрыться в пещере", 6f);
            }
            if (messageUntil > 0f && Time.time >= messageUntil && !Completed)
            {
                Message = string.Empty;
                messageUntil = 0f;
            }
        }

        public void BeginRun()
        {
            if (Started) return;
            Started = true;
            startTime = Time.time;
            Goat.Arm();
        }

        public void ResetRun()
        {
            Started = Completed = CheckpointReached = PredatorActive = false;
            startTime = finishTime = 0f;
            Message = string.Empty;
            messageUntil = 0f;
            Goat.ResetBody(true);
        }

        public void ReachCave()
        {
            if (Completed) return;
            finishTime = Elapsed;
            Completed = CheckpointReached = true;
            Message = "ПЕЩЕРА: безопасный чекпоинт. R — повторить спуск";
        }

        public void NoteFall() => ShowMessage("Полный срыв! Через секунду — снова с вершины.", 2.5f);

        private void ShowMessage(string value, float duration)
        {
            Message = value;
            messageUntil = Time.time + duration;
        }

        private void Build()
        {
            stone = Mat("Unpainted test rock", new Color(.43f, .44f, .45f));
            darkStone = Mat("Cave rim", new Color(.29f, .30f, .31f));
            caveStone = Mat("Cave darkness", new Color(.07f, .085f, .09f));
            ember = Mat("Fire embers", new Color(.88f, .20f, .055f));
            fire = Mat("Campfire", new Color(1f, .66f, .10f));

            Transform wall = Part("Near-vertical test cliff — 20% grade", PrimitiveType.Cube,
                new Vector3(0f, 9f, 0f), new Vector3(12f, 22f, .5f), stone);
            wall.rotation = Quaternion.Euler(CliffSlope.AngleDegrees, 0f, 0f);
            Veins = new GameObject("2–5 cm natural rock veins").AddComponent<CliffVeinField>();
            Veins.transform.SetParent(transform, false);
            Veins.Build();
            BuildCave();

            var goatObject = new GameObject("Goat — centre of mass and four hooves");
            goatObject.transform.SetParent(transform, false);
            Goat = goatObject.AddComponent<CliffBalanceGoat>();
            Goat.Initialize(Veins, this);

            var cameraObject = new GameObject("Balance descent camera");
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.77f, .79f, .80f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CliffBalanceCamera>().Initialize(Goat);

            var lightObject = new GameObject("Plain inspection light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
        }

        private void BuildCave()
        {
            // The opening is in front of the cliff face, so it reads as shelter at the bottom.
            caveRoot = new GameObject("Cave follows the tilted wall").transform;
            caveRoot.SetParent(transform, false);
            caveRoot.localPosition = new Vector3(0f, 0f, CliffSlope.WallZ(0f) + .25f);
            caveRoot.localRotation = Quaternion.Euler(CliffSlope.AngleDegrees, 0f, 0f);
            Part("Cave opening / checkpoint", PrimitiveType.Cube, new Vector3(0f, .18f, -.30f), new Vector3(2.7f, 2.7f, .08f), caveStone);
            Part("Cave left rim", PrimitiveType.Cube, new Vector3(-1.43f, .15f, -.37f), new Vector3(.28f, 3.0f, .23f), darkStone);
            Part("Cave right rim", PrimitiveType.Cube, new Vector3(1.43f, .15f, -.37f), new Vector3(.28f, 3.0f, .23f), darkStone);
            Part("Cave roof", PrimitiveType.Cube, new Vector3(0f, 1.65f, -.37f), new Vector3(3.15f, .34f, .23f), darkStone);
            Part("Cave floor", PrimitiveType.Cube, new Vector3(0f, -1.15f, -.65f), new Vector3(3.0f, .16f, 1.4f), darkStone);
            for (int i = 0; i < 3; i++)
            {
                Transform log = Part("Campfire log", PrimitiveType.Cylinder,
                    new Vector3((i - 1) * .18f, -.86f, -.96f), new Vector3(.095f, .40f, .095f), darkStone);
                log.localRotation = Quaternion.Euler(0f, 0f, 58f - i * 58f);
            }
            Part("Glowing coals", PrimitiveType.Sphere, new Vector3(0f, -.75f, -1.08f), new Vector3(.53f, .15f, .30f), ember);
            var flame = Part("Living campfire flame", PrimitiveType.Sphere,
                new Vector3(0f, -.42f, -1.12f), new Vector3(.27f, .63f, .22f), fire);
            flame.gameObject.AddComponent<CliffCampfireFlame>();
            var glow = new GameObject("Campfire warm light");
            glow.transform.SetParent(caveRoot, false);
            glow.transform.localPosition = new Vector3(0f, -.44f, -1.45f);
            var lamp = glow.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, .42f, .12f);
            lamp.range = 4f;
            lamp.intensity = 2.5f;
        }

        private Transform Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(caveRoot ? caveRoot : transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        private static Material Mat(string name, Color color) => new Material(Shader.Find("Standard")) { name = name, color = color };
    }

    public sealed class CliffBalanceCamera : MonoBehaviour
    {
        private CliffBalanceGoat goat;
        public void Initialize(CliffBalanceGoat target)
        {
            goat = target;
            SetPose(true);
        }
        private void LateUpdate()
        {
            if (!goat) return;
            SetPose(false);
        }

        private void SetPose(bool snap)
        {
            Vector3 focus = new Vector3(Mathf.Clamp(goat.transform.position.x, -2f, 2f),
                Mathf.Clamp(goat.Height - 1.10f, 2.4f, 16.8f), goat.transform.position.z);
            Vector3 target = focus + new Vector3(-4.2f, 4.6f, -4.2f);
            transform.position = snap ? target : Vector3.Lerp(transform.position, target,
                1f - Mathf.Exp(-3.5f * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }

    public sealed class CliffCampfireFlame : MonoBehaviour
    {
        private Vector3 initial;
        private void Start() => initial = transform.localScale;
        private void Update()
        {
            float t = Time.time;
            transform.localScale = new Vector3(initial.x * (1f + Mathf.Sin(t * 13f) * .13f),
                initial.y * (1f + Mathf.Sin(t * 9.7f) * .17f), initial.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 8f) * 9f);
        }
    }
}
