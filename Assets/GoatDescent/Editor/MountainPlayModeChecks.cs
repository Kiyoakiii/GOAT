#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.Editor
{
    [InitializeOnLoad]
    public static class MountainPlayModeChecks
    {
        private const string PendingKey = "GoatDescent.MountainChecks.Pending";
        private static bool attached;
        static MountainPlayModeChecks()
        {
            if (SessionState.GetBool(PendingKey, false)) EditorApplication.update += Attach;
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/GoatDescentPrototype.unity");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.update += Attach;
            EditorApplication.isPlaying = true;
        }
        private static void Attach()
        {
            if (!EditorApplication.isPlaying || attached || !LocalGoatPair.Instance
                || !Object.FindFirstObjectByType<PillarDescentLevel>()) return;
            attached = true;
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Attach;
            new GameObject("Mountain physical checks").AddComponent<MountainPhysicalCheckRunner>();
        }
    }

    public sealed class MountainPhysicalCheckRunner : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private bool passed = true;
        private void Check(string label, bool okay, string detail)
        {
            passed &= okay;
            string line = (okay ? "PASS " : "FAIL ") + label + " " + detail;
            results.Add(line);
            Debug.Log("MOUNTAIN_CHECK " + line);
        }
        private static void CaptureView(string fileName)
        {
            var camera = Camera.main;
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
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures/MountainRuntime"));
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
                Debug.Log("MOUNTAIN_VISUAL_CAPTURE " + path);
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
        private static bool StoneVisibleFromCamera(Camera camera, RollingStone stone)
        {
            if (!camera || !stone) return false;
            Vector3 screen = camera.WorldToViewportPoint(stone.transform.position);
            if (screen.z <= 0f || screen.x < .04f || screen.x > .96f
                || screen.y < .04f || screen.y > .96f) return false;
            Vector3 delta = stone.transform.position - camera.transform.position;
            float closest = float.PositiveInfinity;
            Collider first = null;
            foreach (var hit in Physics.RaycastAll(camera.transform.position, delta.normalized,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody && hit.rigidbody.GetComponent<GoatController>()) continue;
                if (hit.distance < closest) { closest = hit.distance; first = hit.collider; }
            }
            return first && first.GetComponent<RollingStone>() == stone;
        }
        private IEnumerator Start()
        {
            var pair = LocalGoatPair.Instance;
            var route = FindFirstObjectByType<PillarDescentLevel>();
            var ledge = route.GetLedge(10);
            var a = pair.Primary; var b = pair.Secondary;
            Check("stable_id_capacity", ledge && ledge.StableId == "LEDGE-011"
                && Mathf.Abs(ledge.CapacityKg - 95f) < .01f, ledge ? ledge.StableId : "missing");
            Check("local_two_goats_default", a && b && pair.Active == a && !pair.NetworkMode,
                $"A={(a != null)} B={(b != null)} network={pair.NetworkMode}");
            pair.Select(b);
            Check("local_control_switch", pair.Active == b
                && b.GetComponent<GoatLocalControl>().IsControlled
                && !a.GetComponent<GoatLocalControl>().IsControlled,
                $"active={pair.Active.name}");
            pair.Select(a);

            var snowLedge = route.GetLedge(5);
            var snow = snowLedge.GetComponent<LooseSurface>();
            Check("marked_snow_site", snow && snow.StableId == "SNOW-06",
                snow ? snow.StableId : "missing");
            a.GetComponent<RespawnController>().ResetTo(
                snowLedge.transform.TransformPoint(new Vector3(-.55f, .16f, 0f)), Quaternion.identity);
            b.GetComponent<RespawnController>().ResetTo(
                snowLedge.transform.TransformPoint(new Vector3(.55f, .16f, 0f)), Quaternion.identity);
            yield return new WaitForSeconds(.7f);
            Check("snow_load_reduces_traction", snow.IsLoose && snow.TractionMultiplier < .2f,
                $"loose={snow.IsLoose} traction={snow.TractionMultiplier:0.00}");
            var snowState = snow.CaptureState();
            MountainAuthority.SetHost(false);
            snow.ApplyState(snowState);
            Check("late_snow_state", snow.IsLoose && Mathf.Abs(snow.SecondsLeft - snowState.secondsLeft) < .15f,
                $"loose={snow.IsLoose} seconds={snow.SecondsLeft:0.00}");
            MountainAuthority.SetHost(true);
            b.GetComponent<RespawnController>().Respawn();
            yield return null;
            Check("personal_respawn_preserves_snow", snow.IsLoose, $"loose={snow.IsLoose}");
            pair.ResetScenario(0);
            yield return new WaitForSeconds(6.5f);
            Check("snow_recovers_by_level_rule", !snow.IsLoose, $"loose={snow.IsLoose}");

            pair.Select(b);
            pair.ResetRockPractice();
            yield return new WaitForSeconds(.6f);
            var scenarioSnapshot = JsonUtility.FromJson<MountainSnapshot>(
                JsonUtility.ToJson(route.CaptureSnapshot(1)));
            Check("rock_scenario_in_snapshot", pair.ScenarioCode == 11
                && scenarioSnapshot.schema == 5 && scenarioSnapshot.scenario == 11,
                $"local={pair.ScenarioCode} received={scenarioSnapshot.scenario}");
            var rockInteraction = a.GetComponent<GoatInteraction>();
            int chainsBefore = MountainHazardDirector.Current.RecentChains.Count;
            bool shoved = rockInteraction.TryShove();
            if (!shoved)
            {
                var rocks = FindObjectsByType<RollingStone>(FindObjectsSortMode.None);
                foreach (var rock in rocks)
                {
                    Vector3 from = a.GetComponent<Rigidbody>().position + Vector3.up * 1.1f;
                    Vector3 to = rock.transform.position - from;
                    if (to.magnitude > 3f) continue;
                    string contacts = "";
                    foreach (var hit in Physics.RaycastAll(from, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
                        contacts += hit.collider.name + "|";
                    Debug.Log($"MOUNTAIN_ROCK_DIAG goat={a.transform.position} alive={!a.GetComponent<RespawnController>().IsDead} facing={rockInteraction.Facing} rock={rock.StableId} at={rock.transform.position} hits={contacts} status={rockInteraction.Status}");
                }
            }
            var rollingStones = FindObjectsByType<RollingStone>(FindObjectsSortMode.None);
            RollingStone upperStone = null;
            foreach (var rock in rollingStones)
                if (rock.StableId == "STONE-02") { upperStone = rock; break; }
            bool lowerSawPath = false;
            int visibleRockSamples = 0;
            float firstVisibleDistance = 0f;
            float firstVisibleAt = 0f;
            Vector3 lastViewport = Vector3.zero;
            for (int step = 0; step < 22; step++)
            {
                yield return new WaitForSeconds(.1f);
                if (!upperStone || !upperStone.IsMoving || !Camera.main) continue;
                lastViewport = Camera.main.WorldToViewportPoint(upperStone.transform.position);
                if (StoneVisibleFromCamera(Camera.main, upperStone))
                {
                    visibleRockSamples++;
                    if (!lowerSawPath)
                    {
                        lowerSawPath = true;
                        firstVisibleDistance = Vector3.Distance(b.transform.position, upperStone.transform.position);
                        firstVisibleAt = step * .1f;
                        CaptureView("rock_lower_view.png");
                    }
                }
                if (step == 6 && !lowerSawPath) CaptureView("rock_lower_view_probe.png");
            }
            Check("physical_rock_shove", shoved && MountainHazardDirector.Current.ActiveStones > 0
                && MountainHazardDirector.Current.RecentChains.Count > chainsBefore,
                $"requested={shoved} active={MountainHazardDirector.Current.ActiveStones}");
            Check("lower_goat_sees_rock_path", lowerSawPath && visibleRockSamples >= 3
                && firstVisibleDistance >= 3f,
                $"visibleSamples={visibleRockSamples} firstTime={firstVisibleAt:0.0}s firstDistance={firstVisibleDistance:0.0}m moving={(upperStone && upperStone.IsMoving)} viewport={lastViewport}");
            pair.Select(a);

            pair.ResetFragilePractice();
            yield return new WaitForSeconds(.7f);
            Check("one_goat_load", ledge.Phase == DescentLedge.LedgePhase.Intact
                && ledge.LoadKg >= 69f && ledge.LoadKg <= 76f && ledge.VisibleCrackCount == 0,
                $"phase={ledge.Phase} load={ledge.LoadKg:0.0} cracks={ledge.VisibleCrackCount}");
            yield return new WaitForSeconds(20f);
            Check("one_goat_twenty_seconds", ledge.Phase == DescentLedge.LedgePhase.Intact
                && ledge.LoadKg >= 69f && ledge.LoadKg <= 76f,
                $"phase={ledge.Phase} load={ledge.LoadKg:0.0}");
            CaptureView("ledge_one_goat.png");

            b.GetComponent<RespawnController>().ResetTo(ledge.transform.TransformPoint(new Vector3(.65f,.16f,0f)),
                Quaternion.LookRotation(-ledge.transform.right));
            yield return new WaitForSeconds(.55f);
            Check("two_goats_crack", ledge.Phase == DescentLedge.LedgePhase.Cracking
                && ledge.LoadKg >= 140f && ledge.VisibleCrackCount == 3,
                $"phase={ledge.Phase} load={ledge.LoadKg:0.0} cracks={ledge.VisibleCrackCount}");
            CaptureView("ledge_cracking.png");
            var lateState = ledge.CaptureState();
            MountainAuthority.SetHost(false);
            ledge.ApplyState(lateState);
            Check("late_snapshot_phase_and_countdown",
                ledge.Phase == DescentLedge.LedgePhase.Cracking
                && Mathf.Abs(ledge.SecondsLeft - lateState.secondsLeft) < .15f
                && ledge.VisibleCrackCount == 3,
                $"phase={ledge.Phase} hostSeconds={lateState.secondsLeft:0.00} clientSeconds={ledge.SecondsLeft:0.00} cracks={ledge.VisibleCrackCount}");
            MountainAuthority.SetHost(true);
            yield return new WaitForSeconds(2.5f);
            Check("overload_breaks_once", ledge.Phase == DescentLedge.LedgePhase.Broken
                && !ledge.GetComponent<BoxCollider>().enabled && ledge.VisibleCrackCount == 0,
                $"phase={ledge.Phase} cracks={ledge.VisibleCrackCount} reason={ledge.LastReason}");
            b.GetComponent<RespawnController>().Respawn();
            yield return null;
            Check("personal_respawn_preserves_break", ledge.Phase == DescentLedge.LedgePhase.Broken,
                $"phase={ledge.Phase}");

            pair.ResetScenario(0);
            yield return new WaitForSeconds(13.3f);
            Check("safe_global_restore", ledge.Phase == DescentLedge.LedgePhase.Intact
                && ledge.GetComponent<BoxCollider>().enabled, $"phase={ledge.Phase}");
            pair.ResetFragilePractice();
            yield return new WaitForSeconds(.5f);
            b.GetComponent<RespawnController>().ResetTo(ledge.transform.TransformPoint(new Vector3(.65f,.16f,0f)),
                Quaternion.LookRotation(-ledge.transform.right));
            yield return new WaitForSeconds(.7f);
            b.GetComponent<RespawnController>().ResetTo(ledge.transform.TransformPoint(new Vector3(2.8f,.16f,-.85f)),
                Quaternion.LookRotation(-ledge.transform.right));
            yield return new WaitForSeconds(4.5f);
            Check("warning_recovers_when_load_leaves", ledge.Phase == DescentLedge.LedgePhase.Intact
                && ledge.VisibleCrackCount == 0,
                $"phase={ledge.Phase} load={ledge.LoadKg:0.0} cracks={ledge.VisibleCrackCount}");

            pair.ResetFragilePractice();
            b.GetComponent<Rigidbody>().position = ledge.transform.TransformPoint(new Vector3(1.9f,.16f,0f));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.45f);
            var grip = a.GetComponent<GoatInteraction>();
            bool reached = grip.TryGrab();
            yield return new WaitForSeconds(.38f);
            var hangingBody = b.GetComponent<Rigidbody>();
            hangingBody.position = ledge.transform.TransformPoint(new Vector3(2.5f,-1f,2.2f));
            hangingBody.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.3f);
            Check("hanging_goat_adds_load", reached && grip.IsHolding && ledge.LoadKg >= 140f
                && ledge.Contributions.Contains("через сцепление"),
                $"grabbed={grip.IsHolding} load={ledge.LoadKg:0.0} parts={ledge.Contributions}");
            grip.CancelAll();

            route.Complete(a);
            Check("one_finish_waits", !route.Completed, "team still descending");
            route.Complete(b);
            Check("team_finish", route.Completed, "both finished");

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures/MountainRuntime"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "checks.txt"), string.Join("\n", results));
            Debug.Log("MOUNTAIN_CHECKS_FINISHED passed=" + passed + "\n" + string.Join("\n", results));
            yield return null;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
#endif
