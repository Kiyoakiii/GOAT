using UnityEngine;

namespace GoatDescent
{
    public sealed class MountainFinish : MonoBehaviour
    {
        private float nextCelebrate;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < nextCelebrate) return;
            Rigidbody body = other.GetComponentInParent<Rigidbody>();
            if (!body || !body.GetComponent<GoatController>()) return;
            nextCelebrate = Time.time + 3f;
            body.GetComponent<GoatSpectacle>()?.Finish();
            body.GetComponent<GoatJumpController>()?.AnnounceTrick("MOUNTAIN SURVIVED! BAA-A-A!");
        }
    }
}
