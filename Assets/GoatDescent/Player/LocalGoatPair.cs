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
        private Vector3 origin, forward, side;
        private float yaw;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(GoatController a, GoatController b, ThirdPersonGoatCamera camera,
            Vector3 summit, float initialYaw, Transform unusedPillar)
        {
            Primary = a;
            Secondary = b;
            follow = camera;
            origin = summit;
            var route = MountainGenerator.CurrentRoute;
            forward = route != null && route.Count > 1
                ? Vector3.ProjectOnPlane(route[1].Center - route[0].Center, Vector3.up).normalized
                : Quaternion.Euler(0f, initialYaw, 0f) * Vector3.forward;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            side = Vector3.Cross(Vector3.up, forward).normalized;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            a.GetComponent<GoatLocalControl>().Label = "A";
            b.GetComponent<GoatLocalControl>().Label = "B";
            b.GetComponent<GoatLocalControl>().IsRoutePlayer = true;
            Select(a);
            ScenarioCode = 0;
            Scenario = "Tab — сменить козла, F — толчок или сцепление рогами";
        }

        private void Update()
        {
            if (!Primary || !Secondary) return;
            if (NetworkMode) return;
            if (Input.GetKeyDown(KeyCode.Tab)) Select(Active == Primary ? Secondary : Primary);
            if (Input.GetKeyDown(KeyCode.F6)) ResetScenario(0);
            if (Input.GetKeyDown(KeyCode.F7)) ResetScenario(1);
            if (Input.GetKeyDown(KeyCode.F8)) ResetScenario(2);
            if (Input.GetKeyDown(KeyCode.F9)) RemoveLowerSupport();
            if (Input.GetKeyDown(KeyCode.F10)) ResetFragilePractice();
            if (Input.GetKeyDown(KeyCode.F11)) ResetRockPractice();
        }

        public void Select(GoatController goat)
        {
            if (!goat || (goat != Primary && goat != Secondary)) return;
            Primary.GetComponent<GoatLocalControl>().SetControlled(goat == Primary);
            Secondary.GetComponent<GoatLocalControl>().SetControlled(goat == Secondary);
            Active = goat;
            if (follow) follow.Configure(goat.transform);
        }

        public void ConfigureNetwork(bool host)
        {
            NetworkMode = true;
            Select(host ? Primary : Secondary);
            Secondary.SetRemoteControl(host);
            if (host) return;
            Primary.GetComponent<Rigidbody>().isKinematic = true;
            Secondary.GetComponent<Rigidbody>().isKinematic = true;
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
            Select(Primary);
            Vector3 a = origin - side * .85f;
            Vector3 b = origin + side * .85f;
            if (scenario == 1)
            {
                a = origin - forward * .8f;
                b = origin + forward * 1.2f;
                Scenario = "КРАЙ: короткое F может столкнуть напарника";
            }
            else if (scenario == 2)
            {
                a = origin - forward * .65f;
                b = origin + forward * 2.6f - Vector3.up * .7f;
                Scenario = "СПАСЕНИЕ: держи F и отходи назад";
            }
            else Scenario = "ПЛОЩАДКА: толчок и сцепление рогами";
            Place(a, b);
        }

        public void RemoveLowerSupport()
        {
            ScenarioCode = 9;
            Scenario = "ПРОПАСТЬ: второй козёл тянет к краю";
            Place(origin - forward * .7f, origin + forward * 3f - Vector3.up * 1.2f);
        }

        public void ResetFragilePractice()
        {
            var route = MountainGenerator.CurrentRoute;
            if (route == null) return;
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i].Type != LandingPlatformType.Crumbling) continue;
                ScenarioCode = 10;
                Scenario = "F10: выступ выдерживает одного козла, но не двоих";
                Vector3 center = route[i].Center + Vector3.up * 1.5f;
                Place(center - side * 1.5f, center + side * 1.5f);
                return;
            }
        }

        public void ResetRockPractice()
        {
            var route = MountainGenerator.CurrentRoute;
            if (route == null || route.Count < 5) return;
            ScenarioCode = 11;
            Scenario = "F11: камень катится с верхнего выступа к нижнему";
            Place(route[3].Center + Vector3.up * 1.5f,
                route[4].Center + Vector3.up * 1.5f);
        }

        public void ApplyNetworkScenario(int code)
        {
            if (!NetworkMode || MountainAuthority.IsHost || ScenarioCode == code) return;
            ScenarioCode = code;
            Scenario = code == 10 ? "ХРУПКИЙ ВЫСТУП" : code == 11 ? "КАМЕНЬ СВЕРХУ"
                : code == 9 ? "ПРОПАСТЬ" : "ТОЛЧОК И СЦЕПЛЕНИЕ РОГОВ";
        }

        private void Place(Vector3 a, Vector3 b)
        {
            Primary.GetComponent<GoatInteraction>()?.CancelAll();
            Secondary.GetComponent<GoatInteraction>()?.CancelAll();
            Primary.GetComponent<RespawnController>().ResetTo(a, Quaternion.LookRotation(forward));
            Secondary.GetComponent<RespawnController>().ResetTo(b, Quaternion.LookRotation(-forward));
            Primary.GetComponent<GoatVisualController>().SetFacing(forward);
            Secondary.GetComponent<GoatVisualController>().SetFacing(-forward);
            if (follow) follow.Configure(Primary.transform, yaw + 35f);
            Physics.SyncTransforms();
        }
    }
}
