using System;
using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Runs on demand in a standalone player to inspect the integrated scene.</summary>
    public sealed class MountainIntegrationProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3f);
            var pair = LocalGoatPair.Instance;
            var route = MountainGenerator.CurrentRoute;
            var mountain = transform.Find("Procedural Mountain/Mountain Surface");
            var mesh = mountain ? mountain.GetComponent<MeshFilter>()?.sharedMesh : null;
            var renderer = mountain ? mountain.GetComponent<MeshRenderer>() : null;
            var birds = FindObjectsByType<SkyPredatorEpisode>(FindObjectsSortMode.None);
            int eagleModels = birds.Length > 0 ? birds[0].transform.childCount : 0;
            var goatModels = FindObjectsByType<GoatVisualController>(FindObjectsSortMode.None);
            var net = GetComponent<MountainNetSession>();
            Debug.Log($"MOUNTAIN_VERIFY route={route?.Count ?? 0} vertices={mesh?.vertexCount ?? 0} " +
                $"shader={renderer?.sharedMaterial?.shader?.name} goats={goatModels.Length} " +
                $"birdDirectors={birds.Length} birds={eagleModels} " +
                $"stones={FindObjectsByType<RollingStone>(FindObjectsSortMode.None).Length} " +
                $"platforms={FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None).Length} " +
                $"map={GetComponent<MountainSessionState>()?.MapSignature} " +
                $"network={net?.Connected} localPair={(pair && pair.Primary && pair.Secondary)}");
            if (route == null || route.Count < 10 || !mesh || !renderer ||
                !pair || !pair.Primary || !pair.Secondary || goatModels.Length != 2 ||
                birds.Length != 1 || eagleModels != 5)
                Debug.LogError("MOUNTAIN_VERIFY_FAILED scene contents");
            foreach (var arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("--goat-capture="))
                    ScreenCapture.CaptureScreenshot(arg.Substring("--goat-capture=".Length));
            bool exercise = Array.IndexOf(Environment.GetCommandLineArgs(), "--goat-exercise") >= 0;
            if (exercise && MountainAuthority.IsHost && pair)
            {
                pair.ResetScenario(1);
                yield return new WaitForFixedUpdate();
                var interaction = pair.Primary.GetComponent<GoatInteraction>();
                bool shoveStarted = interaction.TryShove();
                yield return new WaitForSecondsRealtime(.6f);
                Debug.Log($"MOUNTAIN_VERIFY_SHOVE started={shoveStarted} applied={interaction.ShovesApplied}");
                pair.ResetScenario(1);
                yield return new WaitForFixedUpdate();
                bool grabStarted = interaction.TryGrab();
                yield return new WaitForSecondsRealtime(.6f);
                Debug.Log($"MOUNTAIN_VERIFY_GRAB started={grabStarted} linked={interaction.IsLinked} " +
                    $"count={interaction.GrabsStarted}");
                bool capturingGrab = false;
                foreach (var arg in Environment.GetCommandLineArgs())
                    if (arg.StartsWith("--goat-grab-capture="))
                    {
                        ScreenCapture.CaptureScreenshot(arg.Substring("--goat-grab-capture=".Length));
                        capturingGrab = true;
                    }
                if (capturingGrab) yield return new WaitForEndOfFrame();
                interaction.ReleaseGrip();
                var stone = FindFirstObjectByType<RollingStone>();
                bool stoneStarted = stone && stone.ReceiveImpulse(Vector3.forward * 90f, "verification");
                bool eagleStarted = SkyPredatorEpisode.Current &&
                    SkyPredatorEpisode.Current.ForceAttack(pair.Secondary);
                Debug.Log($"MOUNTAIN_VERIFY_EVENTS stone={stoneStarted} eagle={eagleStarted}");
            }
            yield return new WaitForSecondsRealtime(MountainAuthority.IsHost ? 25f : 8f);
            Debug.Log($"MOUNTAIN_VERIFY_NETWORK connected={net?.Connected} " +
                $"role={(MountainAuthority.IsHost ? "host" : "guest")} " +
                $"partsA={pair?.Primary?.GetComponent<GoatPhysicalBody>()?.SimulatedParts ?? 0} " +
                $"partsB={pair?.Secondary?.GetComponent<GoatPhysicalBody>()?.SimulatedParts ?? 0}");
            Application.Quit();
        }
    }
}
