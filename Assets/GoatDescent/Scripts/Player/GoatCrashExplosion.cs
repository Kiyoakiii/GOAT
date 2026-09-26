using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>A toy-like crash: model pieces fly apart, then the goat returns to the summit.</summary>
    public sealed class GoatCrashExplosion : MonoBehaviour
    {
        private readonly List<GameObject> pieces = new List<GameObject>();
        private Rigidbody body;
        private Transform visual;
        private float recoverAt, graceUntil;
        private bool controllerWasEnabled, jumpWasEnabled, wallWasEnabled, dashWasEnabled;
        public bool IsExploding { get; private set; }

        private void Awake() => body = GetComponent<Rigidbody>();
        private void OnCollisionEnter(Collision collision)
        {
            if (IsExploding || Time.unscaledTime < graceUntil || collision.contactCount == 0) return;
            ContactPoint contact = collision.GetContact(0);
            float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
            if (collision.collider.GetComponent<MountainSlopeSurface>())
            {
                // Sliding along the mountain is normal; a hard head-on impact or huge landing explodes.
                if (impact >= 18f) Explode(contact.normal, impact);
            }
            else if (contact.normal.y < .55f && impact >= 6.2f) Explode(contact.normal, impact);
        }
        public void Explode(Vector3 away, float impact)
        {
            if (IsExploding || Time.unscaledTime < graceUntil || !body) return;
            visual = transform.Find("VisualRoot — replaceable goat model");
            if (!visual) return;
            Vector3 momentum = body.linearVelocity;
            IsExploding = true; recoverAt = Time.unscaledTime + 1.25f;
            var movement = GetComponent<GoatController>(); controllerWasEnabled = movement.enabled; movement.enabled = false;
            var jump = GetComponent<GoatJumpController>(); jumpWasEnabled = jump.enabled; jump.enabled = false;
            var wall = GetComponent<GoatWallJumpController>(); wallWasEnabled = wall.enabled; wall.enabled = false;
            var dash = GetComponent<GoatRainbowDash>(); dashWasEnabled = dash.enabled; dash.ResetDash(); dash.enabled = false;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.detectCollisions = false;
            foreach (var source in visual.GetComponentsInChildren<MeshFilter>())
            {
                if (pieces.Count >= 32) break;
                var sourceRenderer = source.GetComponent<Renderer>();
                // When the refined FBX is visible, the hidden toy parts still supply
                // the funny detachable fragments on impact.
                bool refined = visual.Find("Torso Motion/Refined FBX goat");
                if (!sourceRenderer || (!sourceRenderer.enabled && !refined) || !source.sharedMesh) continue;
                var piece = new GameObject("Flying goat piece — " + source.name);
                piece.layer = 2;
                piece.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                piece.transform.localScale = source.transform.lossyScale;
                piece.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                piece.AddComponent<MeshRenderer>().sharedMaterial = sourceRenderer.sharedMaterial;
                var collider = piece.AddComponent<BoxCollider>();
                collider.center = source.sharedMesh.bounds.center;
                collider.size = source.sharedMesh.bounds.size * .75f;
                var fragment = piece.AddComponent<Rigidbody>();
                fragment.mass = .35f;
                fragment.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Vector3 radial = (source.transform.position - transform.position - Vector3.up * .7f).normalized;
                fragment.linearVelocity = momentum * .18f + (radial + away * .5f) * Mathf.Clamp(impact * .55f, 4f, 8f)
                    + Vector3.up * Random.Range(3f, 6f);
                fragment.angularVelocity = Random.insideUnitSphere * 17f;
                pieces.Add(piece);
            }
            visual.gameObject.SetActive(false);
            GetComponent<GoatSpectacle>()?.Crash();
            SlopeRun.Instance?.Notify("БА-БАХ! Козёл разлетелся на детали.");
        }
        private void Update()
        {
            if (!IsExploding || Time.unscaledTime < recoverAt) return;
            GetComponent<RespawnController>()?.Respawn();
        }
        public void Restore()
        {
            graceUntil = Time.unscaledTime + .7f;
            if (!IsExploding) return;
            IsExploding = false;
            foreach (var piece in pieces) if (piece) Destroy(piece);
            pieces.Clear();
            if (visual) visual.gameObject.SetActive(true);
            body.isKinematic = false; body.detectCollisions = true;
            GetComponent<GoatController>().enabled = controllerWasEnabled;
            GetComponent<GoatJumpController>().enabled = jumpWasEnabled;
            GetComponent<GoatWallJumpController>().enabled = wallWasEnabled;
            GetComponent<GoatRainbowDash>().enabled = dashWasEnabled;
        }
        private void OnDestroy() { foreach (var piece in pieces) if (piece) Destroy(piece); }
    }
}
