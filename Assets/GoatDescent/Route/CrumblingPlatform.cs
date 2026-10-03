using System.Collections;
using UnityEngine;

namespace GoatDescent
{
    public sealed class CrumblingPlatform : MonoBehaviour
    {
        [SerializeField] private float warningDuration = .75f;
        [SerializeField] private float resetDelay = 5f;

        private Rigidbody body;
        private Collider platformCollider;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private bool activated;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            platformCollider = GetComponent<Collider>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (activated || !collision.collider.GetComponentInParent<GoatController>()) return;
            StartCoroutine(Crumble());
        }

        private IEnumerator Crumble()
        {
            activated = true;
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float wobble = Mathf.Sin(elapsed * 42f) * .025f * (elapsed / warningDuration);
                transform.SetPositionAndRotation(startPosition + new Vector3(wobble, 0f, -wobble),
                    startRotation * Quaternion.Euler(0f, 0f, wobble * 150f));
                yield return null;
            }

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
            activated = false;
        }
    }
}
