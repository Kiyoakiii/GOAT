#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Runs the real gameplay in the open map; no fake physics results.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class GoatPairRuntimeChecks : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private readonly FieldInfo input = typeof(GoatController).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic);
        private GoatController driven;
        private Vector3 driveDirection;
        private LocalGoatPair pair;
        private string output;
        public bool Finished { get; private set; }
        public bool Passed { get; private set; } = true;
        public string Summary => string.Join("\n", results);
        private void LateUpdate()
        {
            if (!driven) return;
            var view = Camera.main.transform;
            Vector3 f = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            input.SetValue(driven, new Vector2(Vector3.Dot(driveDirection, r), Vector3.Dot(driveDirection, f)));
        }
        private void Check(string name, bool ok, string detail = "")
        {
            Passed &= ok;
            results.Add((ok ? "PASS " : "FAIL ") + name + " " + detail);
            Debug.Log("GOAT_PAIR_CHECK " + results[results.Count - 1]);
        }
        private IEnumerator Start()
        {
            output = Path.Combine(Application.dataPath, "../Captures/PairRuntime"); Directory.CreateDirectory(output);
            while (!LocalGoatPair.Instance || !LocalGoatPair.Instance.Secondary) yield return null;
            pair = LocalGoatPair.Instance;
            var a = pair.Primary; var b = pair.Secondary;
            var ai = a.GetComponent<GoatInteraction>(); var bi = b.GetComponent<GoatInteraction>();
            var ar = a.GetComponent<Rigidbody>(); var br = b.GetComponent<Rigidbody>();
            pair.ResetScenario(0); yield return new WaitForSeconds(1f);
            Check("two_goats", FindObjectsByType<GoatController>(FindObjectsSortMode.None).Length == 2);
            Check("dynamic_bodies", !ar.isKinematic && !br.isKinematic);
            Check("one_active_camera", FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled) == 1);
            var animation = a.GetComponentInChildren<Animation>();
            string[] clips = {"Goat_Push", "Goat_PushReact", "Goat_GrabStart", "Goat_GrabbedStart", "Goat_GrabHold", "Goat_GrabbedHold", "Goat_Pull", "Goat_Hang", "Goat_Release"};
            foreach (string clip in clips) Check("clip_" + clip, animation && animation[clip] != null);
            Capture("01-pair");
            var aBefore = ar.position; var bBefore = br.position;
            pair.Select(b); driven = b; driveDirection = Camera.main.transform.right;
            yield return new WaitForSeconds(.45f); driven = null;
            Check("only_selected_moves", Vector3.Distance(ar.position, aBefore) < .15f && Vector3.Distance(br.position, bBefore) > .25f);
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            bBefore = br.position; int shoves = ai.ShovesApplied;
            ai.Press(); yield return new WaitForSeconds(.06f); ai.ReleaseButton();
            yield return new WaitForSeconds(.22f); Capture("02-push-contact");
            yield return new WaitForSeconds(.55f);
            Check("tap_shoves_once", ai.ShovesApplied == shoves + 1 && Vector3.Distance(br.position, bBefore) > .35f, "travel=" + Vector3.Distance(br.position, bBefore));
            pair.ResetScenario(0); yield return new WaitForSeconds(.6f);
            shoves = ai.ShovesApplied;
            ai.Press(); yield return new WaitForSeconds(.8f);
            Check("hold_grabs_two_dynamic_bodies", ai.IsHolding && bi.IsLinked && !ar.isKinematic && !br.isKinematic);
            Capture("03-grip");
            ai.ReleaseButton(); yield return new WaitForSeconds(.15f);
            Check("release_without_shove", !ai.IsLinked && !bi.IsLinked && ai.ShovesApplied == shoves && a.GetComponents<Joint>().Length == 0);
            pair.ResetScenario(2); yield return new WaitForSeconds(.5f);
            float lowY = br.position.y;
            ai.Press(); yield return new WaitForSeconds(.85f);
            Check("catch_lower_goat", ai.IsHolding, "vertical_gap=" + (ar.position.y - br.position.y));
            Capture("04-rescue-catch");
            aBefore = ar.position;
            driven = a; driveDirection = -Vector3.ProjectOnPlane(br.position - ar.position, Vector3.up).normalized;
            yield return new WaitForSeconds(2.5f); driven = null;
            Check("rescue_reaches_upper_shelf", ai.IsHolding && br.position.y > lowY + .5f && b.Grounded, "rise=" + (br.position.y-lowY) + " rescuer_travel=" + Vector3.Distance(ar.position,aBefore) + " force=" + ai.GripForce + " ground=" + b.GetComponent<GoatGroundDetector>().GroundHit.collider?.name);
            Capture("05-rescue-pull");
            ai.ReleaseButton(); yield return new WaitForSeconds(.2f);
            pair.ResetScenario(2); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.85f);
            aBefore = ar.position; lowY = br.position.y;
            pair.RemoveLowerSupport();
            yield return new WaitForSeconds(.8f);
            Check("hanging_weight_moves_holder", Vector3.ProjectOnPlane(ar.position-aBefore, Vector3.up).magnitude > .04f,
                "travel=" + Vector3.Distance(ar.position,aBefore) + " load=" + a.GetComponent<GoatGripBalance>().HangingLoad);
            Capture("07-hanging-weight");
            yield return new WaitForSeconds(2.5f);
            Check("idle_pair_falls_from_edge", ar.position.y < aBefore.y-.5f && br.position.y < lowY-.5f,
                "holder_drop=" + (aBefore.y-ar.position.y) + " partner_drop=" + (lowY-br.position.y));
            pair.ResetScenario(2); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.85f);
            lowY = br.position.y; pair.RemoveLowerSupport();
            driven = a; driveDirection = -Vector3.ProjectOnPlane(br.position-ar.position,Vector3.up).normalized;
            yield return new WaitForSeconds(2.5f); driven = null;
            Check("unsupported_partner_can_be_rescued", ai.IsHolding && b.Grounded && br.position.y > lowY+.3f,
                "rise=" + (br.position.y-lowY));
            Capture("08-unsupported-rescue");
            // Lift the linked pair together into empty air, preserving their relative pose.
            ar.position += Vector3.up*10f; br.position += Vector3.up*10f;
            ar.linearVelocity = br.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSeconds(.12f);
            float initialVelocity = (ar.linearVelocity.y+br.linearVelocity.y)*.5f;
            float started = Time.fixedTime;
            yield return new WaitForSeconds(.4f);
            float measuredAcceleration = ((ar.linearVelocity.y+br.linearVelocity.y)*.5f-initialVelocity)/(Time.fixedTime-started);
            Check("airborne_pair_keeps_gravity", ai.IsHolding && !a.Grounded && !b.Grounded
                && measuredAcceleration < -7f && measuredAcceleration > -12f
                && a.GetComponent<GoatGripBalance>().Unbalance01 == 0f,
                "center_acceleration=" + measuredAcceleration);
            ai.ReleaseButton();
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.7f); pair.Select(b); yield return null;
            Check("switch_releases_joint", !ai.IsLinked && !bi.IsLinked && a.GetComponents<Joint>().Length == 0);
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.7f);
            int falls = FindFirstObjectByType<PillarDescentLevel>().Falls;
            b.GetComponent<RespawnController>().Respawn(); yield return null;
            Check("partner_respawn_cleanup", !ai.IsLinked && !bi.IsLinked && a.GetComponents<Joint>().Length == 0 && FindFirstObjectByType<PillarDescentLevel>().Falls == falls);
            pair.ResetScenario(1); yield return new WaitForSeconds(.5f);
            lowY = br.position.y; ai.TryShove(); yield return new WaitForSeconds(1.3f);
            Check("edge_shove_falls", br.position.y < lowY - .3f, "drop=" + (lowY-br.position.y));
            Capture("06-edge-push");
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            Vector3 direction = (br.position - ar.position).normalized;
            b.GetComponent<RespawnController>().ResetTo(ar.position + direction * 6f, b.transform.rotation);
            Check("out_of_reach_rejected", !ai.TryShove() && !ai.TryGrab());
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = (ar.position+br.position)*.5f + Vector3.up;
            wall.transform.localScale = new Vector3(.5f, 3f, .5f); Physics.SyncTransforms();
            Check("wall_blocks_interaction", !ai.TryShove() && !ai.TryGrab()); Destroy(wall);
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            var jumpStart = ar.position;
            a.Jump(5.15f); yield return new WaitForSeconds(.2f);
            Check("ordinary_jump", ar.position.y > jumpStart.y + .4f && !a.Grounded);
            bool repeated = true;
            for (int cycle = 0; cycle < 5; cycle++)
            {
                pair.ResetScenario(0); yield return new WaitForSeconds(.8f);
                int count = ai.ShovesApplied;
                ai.TryShove(); yield return new WaitForSeconds(.65f);
                repeated &= ai.ShovesApplied == count + 1 && ar.linearVelocity.magnitude < 10f && br.linearVelocity.magnitude < 10f;
            }
            Check("five_shoves_stable", repeated);
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.7f);
            b.SendMessage("Die"); yield return null;
            Check("death_clears_grip", !ai.IsLinked && !bi.IsLinked && a.GetComponents<Joint>().Length == 0);
            yield return new WaitForSeconds(1.4f);
            Check("inactive_respawn_stays_inactive", !b.GetComponent<RespawnController>().IsDead && !b.GetComponent<GoatLocalControl>().IsControlled && !br.isKinematic);
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.7f); bi.enabled = false; yield return null;
            Check("disable_clears_grip", !ai.IsLinked && a.GetComponents<Joint>().Length == 0); bi.enabled = true;
            pair.ResetScenario(0); yield return new WaitForSeconds(.5f);
            ai.Press(); yield return new WaitForSeconds(.7f);
            var stone = FindFirstObjectByType<HornLaunchStone>();
            if (stone)
            {
                a.GetComponent<GoatHornVault>().Launch(stone); yield return new WaitForSeconds(.08f);
                Check("existing_vault_and_grip_cleanup", a.GetComponent<GoatHornVault>().IsVaulting && ar.linearVelocity.magnitude > 2f && !ai.IsLinked && !bi.IsLinked && a.GetComponents<Joint>().Length == 0);
            }
            else Check("existing_vault_available", false);
            pair.ResetScenario(0);
            Finished = true;
            File.WriteAllText(Path.Combine(output, "checks.txt"), Summary);
            Debug.Log("GOAT_PAIR_CHECKS_FINISHED passed=" + Passed + "\n" + Summary);
        }
        private void Capture(string name)
        {
            var camera = Camera.main;
            var texture = RenderTexture.GetTemporary(1280, 720, 24);
            var former = camera.targetTexture; var active = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            }
            finally { camera.targetTexture = former; RenderTexture.active=active; RenderTexture.ReleaseTemporary(texture); Destroy(image); }
        }
        private void OnDisable() { driven = null; }
    }
}
#endif
