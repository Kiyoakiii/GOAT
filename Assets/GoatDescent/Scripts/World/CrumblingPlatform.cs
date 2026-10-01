using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>A ledge carries one goat, then cracks under the measured load of two.</summary>
    public sealed class CrumblingPlatform : MonoBehaviour
    {
        [SerializeField] private float capacityKg = 95f;
        [SerializeField] private float warningDuration = .75f;
        [SerializeField] private float resetDelay = 5f;

        private readonly Dictionary<GoatController, float> contacts = new Dictionary<GoatController, float>();
        private Rigidbody body;
        private Collider platformCollider;
        private Renderer platformRenderer;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float impactUntil, warningUntil;
        private bool cracking;

        public string StableId { get; private set; }
        public float LoadKg { get; private set; }
        public bool IsBroken { get; private set; }
        public float SecondsLeft => cracking ? Mathf.Max(0f, warningUntil - Time.time) : 0f;
        public void Configure(string id) => StableId = id;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            platformCollider = GetComponent<Collider>();
            platformRenderer = GetComponent<Renderer>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!MountainAuthority.IsHost || IsBroken) return;
            var goat = collision.collider.GetComponentInParent<GoatController>();
            if (goat) contacts[goat] = Time.time;
        }

        private void OnCollisionExit(Collision collision)
        {
            var goat = collision.collider.GetComponentInParent<GoatController>();
            if (goat) contacts.Remove(goat);
        }

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost || IsBroken) return;
            LoadKg = 0f;
            foreach (var pair in contacts)
            {
                if (!pair.Key || Time.time - pair.Value > .12f) continue;
                var goatBody = pair.Key.GetComponent<Rigidbody>();
                if (goatBody) LoadKg += goatBody.mass;
            }
            if (!cracking && (LoadKg > capacityKg || Time.time < impactUntil))
                StartCoroutine(Crumble());
        }

        public void RegisterRockImpact(float impulse)
        {
            if (MountainAuthority.IsHost && impulse >= 70f)
                impactUntil = Time.time + warningDuration;
        }

        public LedgeState CaptureState() => new LedgeState
        {
            id = StableId,
            phase = (byte)(IsBroken ? 2 : cracking ? 1 : 0),
            loadKg = LoadKg,
            capacityKg = capacityKg,
            secondsLeft = SecondsLeft,
            reason = Time.time < impactUntil ? "rock" : "weight"
        };

        public void ApplyState(LedgeState state)
        {
            if (MountainAuthority.IsHost || state.id != StableId) return;
            IsBroken = state.phase == 2;
            cracking = state.phase == 1;
            LoadKg = state.loadKg;
            warningUntil = Time.time + state.secondsLeft;
            if (body) body.isKinematic = true;
            if (platformCollider) platformCollider.enabled = !IsBroken;
            if (platformRenderer) platformRenderer.enabled = !IsBroken;
            if (!IsBroken) transform.SetPositionAndRotation(startPosition, startRotation);
        }

        private IEnumerator Crumble()
        {
            cracking = true;
            warningUntil = Time.time + warningDuration;
            while (Time.time < warningUntil)
            {
                if (LoadKg <= capacityKg && Time.time >= impactUntil)
                {
                    cracking = false;
                    transform.SetPositionAndRotation(startPosition, startRotation);
                    yield break;
                }
                float progress = 1f - (warningUntil - Time.time) / warningDuration;
                float wobble = Mathf.Sin(Time.time * 42f) * .025f * progress;
                transform.SetPositionAndRotation(startPosition + new Vector3(wobble, 0f, -wobble),
                    startRotation * Quaternion.Euler(0f, 0f, wobble * 150f));
                yield return null;
            }
            cracking = false;
            IsBroken = true;
            MountainHazardDirector.Current?.OnPlatformBroken(this);
            if (body)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.AddForce(Vector3.down * 1.5f, ForceMode.Impulse);
            }
            yield return new WaitForSeconds(resetDelay);
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(startPosition, startRotation);
            if (platformCollider) platformCollider.enabled = true;
            if (platformRenderer) platformRenderer.enabled = true;
            contacts.Clear();
            LoadKg = 0f;
            IsBroken = false;
        }
    }
}
