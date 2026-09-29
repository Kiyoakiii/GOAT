#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Records an actual gameplay demonstration from a side camera.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class GoatPairDemo : MonoBehaviour
    {
        private LocalGoatPair pair;
        private Camera view;
        private ThirdPersonGoatCamera follow;
        private Vector3 forward, drive;
        private bool driving, recording;
        private RenderTexture target;
        private Texture2D pixels;
        private int frame, oldCaptureRate;
        private string folder;
        private readonly FieldInfo input = typeof(GoatController).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic);
        public bool Finished { get; private set; }
        public string Stage { get; private set; }
        private IEnumerator Start()
        {
            pair = LocalGoatPair.Instance; view = Camera.main; follow = view.GetComponent<ThirdPersonGoatCamera>();
            folder = Path.Combine(Application.dataPath, "../Captures/PairDemoFrames"); Directory.CreateDirectory(folder);
            target = new RenderTexture(960, 540, 24); pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            oldCaptureRate = Time.captureFramerate; Time.captureFramerate = 24;
            pair.ResetScenario(0); forward = pair.Primary.GetComponent<GoatVisualController>().Facing;
            follow.enabled = false; yield return new WaitForSeconds(.5f);
            recording = true; StartCoroutine(Record());
            var interaction = pair.Primary.GetComponent<GoatInteraction>();
            Stage = "Толчок";
            yield return new WaitForSeconds(.65f);
            interaction.Press(); yield return new WaitForSeconds(.08f); interaction.ReleaseButton();
            yield return new WaitForSeconds(1.4f);
            recording = false; pair.ResetScenario(0); yield return new WaitForSeconds(.5f); recording = true;
            Stage = "Захват и отпускание";
            yield return new WaitForSeconds(.5f); interaction.Press(); yield return new WaitForSeconds(1.6f);
            interaction.ReleaseButton(); yield return new WaitForSeconds(.7f);
            recording = false; pair.ResetScenario(2); yield return new WaitForSeconds(.5f); recording = true;
            Stage = "Спасение с нижнего выступа";
            yield return new WaitForSeconds(.5f); interaction.Press(); yield return new WaitForSeconds(.95f);
            drive = -forward; driving = true; yield return new WaitForSeconds(2.5f); driving = false;
            yield return new WaitForSeconds(.5f); interaction.ReleaseButton(); yield return new WaitForSeconds(1f);
            recording = false; Finished = true; Time.captureFramerate = oldCaptureRate;
            pair.ResetScenario(0); follow.enabled = true;
            File.WriteAllText(Path.Combine(folder,"recording.txt"), "frames=" + frame + " fps=24 source=Unity gameplay camera");
            Debug.Log("GOAT_PAIR_DEMO_RECORDED frames=" + frame);
        }
        private void LateUpdate()
        {
            if (!pair || Finished) return;
            Vector3 center=(pair.Primary.transform.position + pair.Secondary.transform.position)*.5f + Vector3.up * .9f;
            var side = Vector3.Cross(forward, Vector3.up).normalized;
            view.transform.position = center + side*5.6f - forward*2.1f + Vector3.up*2.2f;
            view.transform.LookAt(center);
            if (driving)
            {
                var f=Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
                var r=Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
                input.SetValue(pair.Primary,new Vector2(Vector3.Dot(drive,r),Vector3.Dot(drive,f)));
            }
        }
        private IEnumerator Record()
        {
            while (!Finished)
            {
                yield return new WaitForEndOfFrame();
                if (!recording) continue;
                var previous=view.targetTexture; var active=RenderTexture.active;
                try
                {
                    view.targetTexture=target; view.Render(); RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,960,540),0,0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,$"frame-{frame++:D4}.png"),pixels.EncodeToPNG());
                }
                finally { view.targetTexture=previous; RenderTexture.active=active; }
            }
        }
        private void OnDestroy()
        {
            if (!Finished) Time.captureFramerate=oldCaptureRate;
            if (follow) follow.enabled=true;
            if (target) { target.Release(); Destroy(target); }
            if (pixels) Destroy(pixels);
        }
    }
}
#endif
