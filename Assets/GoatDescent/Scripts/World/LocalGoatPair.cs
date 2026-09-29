using UnityEngine;

namespace GoatDescent
{
    [DefaultExecutionOrder(-100)]
    public sealed class LocalGoatPair : MonoBehaviour
    {
        public static LocalGoatPair Instance { get; private set; }
        public GoatController Primary { get; private set; }
        public GoatController Secondary { get; private set; }
        public GoatController Active { get; private set; }
        public string Scenario { get; private set; }
        public int ScenarioCode { get; private set; }
        public bool NetworkMode { get; private set; }
        private ThirdPersonGoatCamera follow;
        private Vector3 origin, forward;
        private float yaw;
        private GameObject lowerShelf;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Configure(GoatController a, GoatController b, ThirdPersonGoatCamera camera, Vector3 summit, float initialYaw, Transform pillar)
        {
            Primary = a; Secondary = b; follow = camera; origin = summit;
            forward = pillar ? pillar.forward : Quaternion.Euler(0f, initialYaw, 0f) * Vector3.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            // The existing summit slopes gently for ten metres before the cliff.
            // Put the practice lip over that cliff, clear of the summit mesh.
            origin += forward * 10f;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            a.GetComponent<GoatLocalControl>().Label = "A";
            b.GetComponent<GoatLocalControl>().Label = "B";
            b.GetComponent<GoatLocalControl>().IsRoutePlayer = true;
            CreatePracticeRocks(pillar);
            Select(a);
            ResetScenario(0);
        }
        private void Update()
        {
            if (!Primary || !Secondary) return;
            if ((!NetworkMode || MountainAuthority.IsHost) && Input.GetKeyDown(KeyCode.F10))
                ResetFragilePractice();
            if ((!NetworkMode || MountainAuthority.IsHost) && Input.GetKeyDown(KeyCode.F11))
                ResetRockPractice();
            if (NetworkMode) return;
            if (Input.GetKeyDown(KeyCode.Tab)) Select(Active == Primary ? Secondary : Primary);
            if (Input.GetKeyDown(KeyCode.F6)) ResetScenario(0);
            if (Input.GetKeyDown(KeyCode.F7)) ResetScenario(1);
            if (Input.GetKeyDown(KeyCode.F8)) ResetScenario(2);
            if (Input.GetKeyDown(KeyCode.F9)) RemoveLowerSupport();
        }
        public void Select(GoatController goat)
        {
            if (!goat || (goat != Primary && goat != Secondary)) return;
            Primary.GetComponent<GoatLocalControl>().SetControlled(goat == Primary);
            Secondary.GetComponent<GoatLocalControl>().SetControlled(goat == Secondary);
            Active = goat;
            follow.Configure(goat.transform);
            FindFirstObjectByType<PillarDescentLevel>()?.RefreshTargetMarkers();
        }
        public void ConfigureNetwork(bool host)
        {
            NetworkMode = true;
            Select(host ? Primary : Secondary);
            Secondary.SetRemoteControl(host);
            if (!host)
            {
                Primary.GetComponent<Rigidbody>().isKinematic = true;
                Secondary.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
        public void RestoreLocal()
        {
            if (!NetworkMode) return;
            MountainAuthority.SetHost(true);
            NetworkMode = false;
            foreach (var goat in new[] { Primary, Secondary })
            {
                if (!goat) continue;
                goat.SetRemoteControl(false);
                goat.SetRemoteMove(Vector3.zero);
                var life = goat.GetComponent<RespawnController>();
                if (!goat.IsPredatorCarried && !(life && life.IsDead))
                    goat.GetComponent<Rigidbody>().isKinematic = false;
            }
            SkyPredatorEpisode.Current?.OnLocalAuthorityRestored();
            Select(Primary);
        }
        public void ResetScenario(int scenario)
        {
            ScenarioCode = scenario;
            if (lowerShelf) lowerShelf.SetActive(true);
            Select(Primary);
            Primary.GetComponent<GoatInteraction>().CancelAll();
            Secondary.GetComponent<GoatInteraction>().CancelAll();
            Vector3 a, b;
            if (scenario == 2)
            {
                a = origin - forward * .85f;
                b = origin + forward * 1.25f - Vector3.up * .85f;
                Scenario = "СПАСЕНИЕ: удерживай F и отходи назад";
            }
            else if (scenario == 1)
            {
                a = origin - forward * 2.25f;
                b = origin - forward * .1f;
                Scenario = "КРАЙ: короткое F столкнёт напарника вниз";
            }
            else
            {
                a = origin - forward * 4.9f;
                b = origin - forward * 2.65f;
                Scenario = "ПЛОЩАДКА: толчок и сцепление рогами";
            }
            var rotation = Quaternion.LookRotation(forward);
            Primary.GetComponent<RespawnController>().ResetTo(a, rotation);
            Secondary.GetComponent<RespawnController>().ResetTo(b, Quaternion.LookRotation(-forward));
            Primary.GetComponent<GoatVisualController>().SetFacing(forward);
            Secondary.GetComponent<GoatVisualController>().SetFacing(-forward);
            follow.Configure(Primary.transform, yaw + 35f);
            Physics.SyncTransforms();
        }
        public void RemoveLowerSupport()
        {
            if (lowerShelf) lowerShelf.SetActive(false);
            ScenarioCode = 9;
            Scenario = "ПРОПАСТЬ: вес напарника тянет к краю";
        }
        public void ResetFragilePractice()
        {
            var level = FindFirstObjectByType<PillarDescentLevel>();
            var ledge = level ? level.GetLedge(10) : null;
            if (!ledge || !ledge.IsCrumbling) return;
            ScenarioCode = 10;
            Vector3 a = ledge.transform.TransformPoint(new Vector3(0f, .16f, 0f));
            Vector3 b = ledge.transform.TransformPoint(new Vector3(2.8f, .16f, -.85f));
            Vector3 towardB = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized;
            Primary.GetComponent<RespawnController>().ResetTo(a, Quaternion.LookRotation(towardB));
            Secondary.GetComponent<RespawnController>().ResetTo(b, Quaternion.LookRotation(-towardB));
            Primary.GetComponent<GoatVisualController>().SetFacing(towardB);
            Secondary.GetComponent<GoatVisualController>().SetFacing(-towardB);
            AimCameraFromOutside(ledge);
            Scenario = "F10: маленький выступ — один козёл на камне, второй на прочной боковой опоре";
            Physics.SyncTransforms();
        }
        public void ResetRockPractice()
        {
            var level = FindFirstObjectByType<PillarDescentLevel>();
            var upper = level ? level.GetLedge(6) : null;
            var lower = level ? level.GetLedge(7) : null;
            if (!upper || !lower) return;
            ScenarioCode = 11;
            Vector3 a = upper.transform.TransformPoint(new Vector3(.05f, .16f, .45f));
            Vector3 b = lower.transform.TransformPoint(new Vector3(0f, .16f, .2f));
            Primary.GetComponent<RespawnController>().ResetTo(a, Quaternion.LookRotation(upper.transform.right));
            Secondary.GetComponent<RespawnController>().ResetTo(b, Quaternion.LookRotation(-lower.transform.forward));
            Primary.GetComponent<GoatVisualController>().SetFacing(upper.transform.right);
            Secondary.GetComponent<GoatVisualController>().SetFacing(-lower.transform.forward);
            AimCameraFromOutside(Active == Secondary ? lower : upper);
            Scenario = "F11: козёл A толкает камень сверху, козёл B ждёт на нижнем выступе";
            Physics.SyncTransforms();
        }
        public void ApplyNetworkScenario(int code)
        {
            if (!NetworkMode || MountainAuthority.IsHost || ScenarioCode == code) return;
            ScenarioCode = code;
            var level = FindFirstObjectByType<PillarDescentLevel>();
            if (code == 10)
            {
                Scenario = "F10: хрупкий выступ и боковая опора";
                var ledge = level ? level.GetLedge(10) : null;
                if (ledge) AimCameraFromOutside(ledge);
            }
            else if (code == 11)
            {
                Scenario = "F11: камень сверху, козёл B ниже";
                var ledge = level ? level.GetLedge(7) : null;
                if (ledge) AimCameraFromOutside(ledge);
            }
            else if (code == 9)
                Scenario = "ПРОПАСТЬ: вес напарника тянет к краю";
            else
            {
                Scenario = "ПЛОЩАДКА: толчок и сцепление рогами";
                follow.Configure(Active.transform, yaw + 35f, 20f);
            }
            Debug.Log($"MOUNTAIN_NET_SCENARIO code={code} active={Active.name}");
        }
        private void AimCameraFromOutside(DescentLedge ledge)
        {
            Vector3 inward = -Vector3.ProjectOnPlane(ledge.transform.forward, Vector3.up).normalized;
            float cameraYaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            follow.Configure(Active.transform, cameraYaw, 20f);
        }
        private void CreatePracticeRocks(Transform pillar)
        {
            var root = new GameObject("Pair practice — summit stone shelves");
            var source = pillar ? pillar.Find("Stratified sandstone")?.GetComponent<Renderer>() : null;
            var material = source ? source.sharedMaterial : new Material(Shader.Find("Standard")) { color = new Color(.47f, .43f, .36f) };
            AddRock(root.transform, "Upper practice shelf", origin - forward * 3.5f - Vector3.up * .37f, new Vector3(8f, .5f, 7.4f), material);
            AddRock(root.transform, "Lower rescue shelf", origin + forward * 1.4f - Vector3.up * 1.22f, new Vector3(3.3f, .5f, 2f), material);
        }
        private void AddRock(Transform parent, string label, Vector3 position, Vector3 scale, Material material)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = label; rock.transform.SetParent(parent);
            rock.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            rock.transform.localScale = scale;
            rock.GetComponent<Renderer>().sharedMaterial = material;
            if (label == "Lower rescue shelf") lowerShelf = rock;
        }
    }
}
