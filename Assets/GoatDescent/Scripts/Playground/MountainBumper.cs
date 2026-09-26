using UnityEngine;

namespace GoatDescent
{
    public sealed class MountainBumper : MonoBehaviour
    {
        private float nextHit;
        private Vector3 baseScale;

        private void Start() { baseScale = transform.localScale; }
        private void Update()
        {
            if (baseScale == Vector3.zero) return;
            transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 4f + transform.position.z) * .06f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time < nextHit) return;
            Rigidbody body = collision.rigidbody;
            if (!body || !body.GetComponent<GoatController>()) return;
            nextHit = Time.time + .35f;
            Vector3 away = (body.position - transform.position).normalized;
            away.y = .5f;
            body.linearVelocity += away.normalized * 7f + Vector3.up * 5f;
            body.GetComponent<GoatVisualController>()?.PlayLanding(10f);
            body.GetComponent<GoatSpectacle>()?.Bumper();
            body.GetComponent<GoatJumpController>()?.AnnounceTrick("BONK! FLYING GOAT!");
        }
    }
}
