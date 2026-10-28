using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatRockfallShelter : MonoBehaviour
    {
        private GoatController motor;
        private GoatInteraction interaction;
        private Rigidbody body;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private float settled, localCover, remoteCover;
        public bool NearWall { get; private set; }
        public bool Still { get; private set; }
        public bool Bracing { get; private set; }
        public float Protection
        {
            get
            {
                if (!MountainAuthority.IsHost) return remoteCover;
                var partner = interaction ? interaction.Partner : null;
                var shelter = partner ? partner.GetComponent<GoatRockfallShelter>() : null;
                return Mathf.Clamp01(localCover + (shelter ? Mathf.Min(localCover, shelter.localCover) * .28f : 0));
            }
        }

        private void Awake()
        {
            motor = GetComponent<GoatController>();
        }

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            motor ??= GetComponent<GoatController>();
            interaction ??= GetComponent<GoatInteraction>();
            var episode = RockfallEpisode.Current;
            if (!motor || !episode || !episode.IsDangerous || !episode.InArea(transform.position, 28)
                || GetComponent<RespawnController>()?.IsDead == true || motor.IsPredatorCarried)
            { NearWall = Still = Bracing = false; settled = localCover = 0; return; }
            float distance = 1.8f;
            Vector3 wallNormal = Vector3.zero;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            for (int ray = 0; ray < 12; ray++)
            {
                Vector3 direction = Quaternion.Euler(0, ray * 30, 0) * Vector3.forward;
                int count = Physics.RaycastNonAlloc(origin, direction, hits, 1.8f, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (Mathf.Abs(hit.normal.y) > .45f || hit.collider.GetComponentInParent<GoatController>()
                        || hit.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                    }
                }
            }
            NearWall = distance < 1.5f;
            Still = motor.Grounded && !motor.HasMovementInput && motor.Velocity.magnitude < .65f;
            Bracing = motor.WantsBrake;
            // Bracing is physical: gently lean into the nearby cliff, resisting
            // the horn joint pulling a stationary partner out of shelter. Never
            // attract an airborne or walking goat, or bypass wall collisions.
            if (NearWall && Bracing && motor.Grounded && !motor.HasMovementInput)
            {
                body ??= GetComponent<Rigidbody>();
                float towardSpeed = Vector3.Dot(motor.Velocity, -wallNormal);
                float acceleration = Mathf.Clamp((distance - .75f) * 14 - towardSpeed * 6, 0, 6);
                if (body && !body.isKinematic) body.AddForce(-wallNormal * acceleration, ForceMode.Acceleration);
            }
            settled = Mathf.MoveTowards(settled, NearWall && Still ? 1 : 0, Time.fixedDeltaTime * (Still ? 2.5f : 8));
            float closeness = Mathf.InverseLerp(1.8f, .8f, distance);
            localCover = closeness * settled * (Bracing ? .78f : .58f);
        }

        public RockfallShelterState Capture() => new RockfallShelterState
        { protection = Protection, wall = NearWall, still = Still, bracing = Bracing };

        public void Apply(RockfallShelterState state)
        {
            if (MountainAuthority.IsHost) return;
            remoteCover = state.protection; NearWall = state.wall; Still = state.still; Bracing = state.bracing;
        }
    }
}
