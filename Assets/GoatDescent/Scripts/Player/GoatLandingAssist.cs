using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatLandingAssist : MonoBehaviour
    {
        [SerializeField] private float normalVelocityDamping = .7f;
        [SerializeField] private float landingCooldown = .12f;
        private Rigidbody body; private GoatGroundDetector ground; private float lastLanding = -10f;
        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround) { body = targetBody; ground = targetGround; }
        private void Awake() => CacheComponents();
        private void CacheComponents() { body ??= GetComponent<Rigidbody>(); ground ??= GetComponent<GoatGroundDetector>(); }
        private void OnCollisionEnter(Collision collision)
        {
            CacheComponents();
            if (!body || body.isKinematic || Time.time - lastLanding < landingCooldown || collision.contactCount == 0) return;
            var normal = collision.GetContact(0).normal;
            float intoSurface = Vector3.Dot(body.linearVelocity, normal);
            if (normal.y > .5f && collision.relativeVelocity.magnitude > 1.5f)
            {
                GetComponent<GoatVisualController>()?.PlayLanding(collision.relativeVelocity.magnitude);
                GetComponent<GoatSpectacle>()?.Land(collision.relativeVelocity.magnitude);
            }
            if (intoSurface < -1f)
            {
                body.linearVelocity -= normal * intoSurface * normalVelocityDamping;
                lastLanding = Time.time;
            }
        }
    }
}
