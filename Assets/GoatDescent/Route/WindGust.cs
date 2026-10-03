using UnityEngine;

namespace GoatDescent
{
    public sealed class WindGust : MonoBehaviour
    {
        [SerializeField] private float strength = 12f;
        private Vector3 direction = Vector3.right;

        public void Configure(Vector3 pushDirection, float force)
        {
            direction = pushDirection.sqrMagnitude > .001f ? pushDirection.normalized : Vector3.right;
            strength = force;
        }

        private void OnTriggerStay(Collider other)
        {
            var goat = other.GetComponentInParent<GoatController>();
            if (!goat || goat.Grounded) return;
            var body = goat.GetComponent<Rigidbody>();
            if (body) body.AddForce(direction * strength, ForceMode.Acceleration);
        }
    }
}
