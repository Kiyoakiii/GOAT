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
            if (!body || Time.time - lastLanding < landingCooldown || collision.contactCount == 0) return;
            var normal = collision.GetContact(0).normal;
            float intoSurface = Vector3.Dot(body.linearVelocity, normal);
            if (intoSurface < -1f)
            {
                body.linearVelocity -= normal * intoSurface * normalVelocityDamping;
                lastLanding = Time.time;
            }
        }
    }
}
