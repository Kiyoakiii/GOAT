#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;

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
            var goat = LocalGoatPair.Instance ? LocalGoatPair.Instance.Primary : Object.FindFirstObjectByType<GoatController>();
            var body = goat ? goat.GetComponent<Rigidbody>() : null;
            var route = Object.FindFirstObjectByType<PillarDescentLevel>();
            var camera = Camera.main;
            int ledgeObjects = route ? route.LedgeCount : 0;
            int solidLandingColliders = route ? route.GetComponentsInChildren<BoxCollider>(true).Length : 0;
            bool reliableLandings = ledgeObjects >= 20 && solidLandingColliders >= ledgeObjects && route.MaxJumpGap <= 8f;
            bool ready = goat && body && !body.isKinematic && camera && reliableLandings;

            SettleSpawnAndCamera(camera);
            CaptureFrame(camera, "GoatDescentLevel1Spawn.png");
            // Exercise the actual controller motor with a simulated held W key and a manual physics step.
            Vector3 before = body ? body.position : Vector3.zero;
            bool bodyCanMove = SimulateControllerMove(goat, body);
            float cameraDistance = goat && camera ? Vector3.Distance(goat.transform.position, camera.transform.position) : 0f;
            bool cameraIsFollowing = cameraDistance > .5f && cameraDistance < 24f;
            ready &= bodyCanMove && cameraIsFollowing;
            if (route) CaptureRouteOverview(camera, route, goat.transform.position.y);
            bool fatalFallWorks = SimulateFatalFall(goat, body, route);
            bool finishWorks = SimulateFinish(goat, body, route);
            ready &= fatalFallWorks && finishWorks;
            Debug.Log($"GOAT_DESCENT_PLAYMODE_SMOKE ready={ready} ledges={ledgeObjects} solidLandingColliders={solidLandingColliders} " +
                      $"cameraDistance={cameraDistance:0.00} " +
                      $"bodyCanMove={bodyCanMove} fatalFallWorks={fatalFallWorks} finishWorks={finishWorks} motorSpeed={motorTravel:0.000} bodyStartY={before.y:0.00}");
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

        private static bool SimulateFatalFall(GoatController goat, Rigidbody body, PillarDescentLevel route)
        {
            if (!goat || !body || !route) return false;
            var life = goat.GetComponent<RespawnController>();
            // The tall cliff projects outward above lower shelves. Drop above
            // the open summit practice shelf so the fall is not inside rock.
            Vector3 target = goat.transform.position + Vector3.up * 18f;
            body.position = target;
            body.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            var previous = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                for (int i = 0; i < 145 && !life.IsDead; i++) Physics.Simulate(.02f);
                Debug.Log($"GOAT_FATAL_FALL_CHECK start={target} end={body.position} velocity={body.linearVelocity} dead={life.IsDead}");
                return life.IsDead;
            }
            finally
            {
                Physics.simulationMode = previous;
            }
        }

        private static bool SimulateFinish(GoatController goat, Rigidbody body, PillarDescentLevel route)
        {
            var goal = route ? route.GetComponentInChildren<PillarFinishTrigger>() : null;
            var second = LocalGoatPair.Instance ? LocalGoatPair.Instance.Secondary : null;
            var secondBody = second ? second.GetComponent<Rigidbody>() : null;
            if (!goat || !body || !goal || !secondBody) return false;
            goat.GetComponent<RespawnController>().Respawn();
            Vector3 finishPoint = goal.transform.position + Vector3.up;
            goat.transform.position = finishPoint;
            body.position = finishPoint;
            body.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            var previous = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                for (int i = 0; i < 5; i++) Physics.Simulate(.02f);
                bool firstWaits = !route.Completed;
                second.transform.position = finishPoint + Vector3.right * 1.1f;
                secondBody.position = second.transform.position;
                secondBody.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                for (int i = 0; i < 5 && !route.Completed; i++) Physics.Simulate(.02f);
                return firstWaits && route.Completed;
            }
            finally { Physics.simulationMode = previous; }
        }

        private static void CaptureRouteOverview(Camera camera, PillarDescentLevel route, float summitY)
        {
            Vector3 center = route.transform.position + Vector3.up * (summitY - route.transform.position.y - 95f);
            Vector3 outward = route.transform.forward;
            camera.transform.position = center + outward * 125f + Vector3.Cross(Vector3.up, outward) * 75f + Vector3.up * 30f;
            camera.transform.LookAt(center);
            CaptureFrame(camera, "GoatDescentLevel1Route.png");
        }

        private static void CaptureFrame(Camera camera, string fileName)
        {
            if (!camera || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string path = Path.GetFullPath("Library/" + fileName);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
                Debug.Log("GOAT_DESCENT_LEVEL1_CAPTURE " + path);
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
#endif
