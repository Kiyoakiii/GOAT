#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace GoatDescent.Editor
{
    /// <summary>Headless Play Mode validation usable from the Unity command line.</summary>
    [InitializeOnLoad]
    public static class GoatDescentPlayModeSmoke
    {
        private static int ticks;
        private static bool hooked;
        private static float motorTravel;
        private const string PendingKey = "GoatDescent.PlayModeSmoke.Pending";

        static GoatDescentPlayModeSmoke()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying) HookVerification();
        }

        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            ticks = 0;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false)) HookVerification();
        }

        private static void HookVerification()
        {
            if (hooked) return;
            hooked = true;
            EditorApplication.update += Verify;
        }

        private static void Verify()
        {
            if (++ticks < 20) return;
            EditorApplication.update -= Verify;
            hooked = false;
            SessionState.SetBool(PendingKey, false);
            var goat = Object.FindFirstObjectByType<GoatController>();
            var body = goat ? goat.GetComponent<Rigidbody>() : null;
            var route = GameObject.Find("Ledges — guaranteed descent route");
            var camera = Camera.main;
            int ledgeObjects = route ? route.transform.childCount : 0;
            int solidLandingColliders = route ? route.GetComponentsInChildren<BoxCollider>(true).Length : 0;
            bool reliableLandings = solidLandingColliders >= 55;
            bool ready = goat && body && !body.isKinematic && camera && ledgeObjects >= 38 && reliableLandings;

            SettleSpawnAndCamera(camera);
            // Exercise the actual controller motor with a simulated held W key and a manual physics step.
            Vector3 before = body ? body.position : Vector3.zero;
            bool bodyCanMove = SimulateControllerMove(goat, body);
            float cameraDistance = goat && camera ? Vector3.Distance(goat.transform.position, camera.transform.position) : 0f;
            bool cameraIsFollowing = cameraDistance > .5f && cameraDistance < 24f;
            ready &= bodyCanMove && cameraIsFollowing;
            Debug.Log($"GOAT_DESCENT_PLAYMODE_SMOKE ready={ready} ledges={ledgeObjects} solidLandingColliders={solidLandingColliders} " +
                      $"cameraDistance={cameraDistance:0.00} " +
                      $"bodyCanMove={bodyCanMove} motorSpeed={motorTravel:0.000} bodyStartY={before.y:0.00}");
            if (!ready) Debug.LogError("GOAT_DESCENT_PLAYMODE_SMOKE failed.");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(ready ? 0 : 1);
        }

        private static bool SimulateControllerMove(GoatController goat, Rigidbody body)
        {
            if (!goat || !body) return false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var input = typeof(GoatController).GetField("input", flags);
            var motor = typeof(GoatController).GetMethod("FixedUpdate", flags);
            if (input == null || motor == null) return false;
            Vector3 before = body.position;
            input.SetValue(goat, new Vector2(0f, 1f));
            motor.Invoke(goat, null);
            motorTravel = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            return motorTravel > .1f;
        }

        private static void SettleSpawnAndCamera(Camera camera)
        {
            var oldMode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                for (int i = 0; i < 40; i++) Physics.Simulate(.02f);
            }
            finally { Physics.simulationMode = oldMode; }
            camera?.GetComponent<ThirdPersonGoatCamera>()?.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
        }
    }
}
#endif
