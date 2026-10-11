using UnityEngine;

namespace GoatDescent
{
    /// <summary>
    /// Finite hoof balance for the upright capsule motor. FreezeRotation must
    /// not supply unlimited resistance to a partner hanging from the horns.
    /// An unsupported forward load shifts pressure past the front hooves,
    /// reduces traction and converts the remaining tipping moment into a slip.
    /// Both airborne bodies keep ordinary gravity and joint forces only.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class GoatGripBalance : MonoBehaviour
    {
        [SerializeField] private float frontSupportExtent = .32f;
        [SerializeField] private float maxSlipAcceleration = 3.5f;
        [SerializeField] private float loadBuildUpSeconds = .45f;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private GoatGroundDetector ground;
        private GoatInteraction interaction;
        private PhysicsMaterial originalMaterial, slippingMaterial;
        private bool slipping;
        public float Unbalance01 { get; private set; }
        public float HangingLoad { get; private set; }
        public float TippingMoment { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>();
            ground = GetComponent<GoatGroundDetector>(); interaction = GetComponent<GoatInteraction>();
            originalMaterial = capsule.sharedMaterial;
            slippingMaterial = new PhysicsMaterial("Goat overloaded hooves")
            {
                staticFriction = .06f, dynamicFriction = .04f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f, bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }
        private void FixedUpdate()
        {
            HangingLoad = TippingMoment = 0f;
            var partner = interaction ? interaction.Partner : null;
            bool supported = ground && ground.IsGrounded && ground.SlopeAngle < 55f;
            bool partnerHanging = partner && !partner.GetComponent<GoatGroundDetector>().IsGrounded
                && partner.transform.position.y < transform.position.y - .15f;
            if (!supported || !partnerHanging || body.isKinematic)
            { RestoreTraction(); Unbalance01 = 0f; return; }

            Vector3 normal = ground.GroundNormal;
            Vector3 outward = Vector3.ProjectOnPlane(partner.transform.position - transform.position, normal).normalized;
            Vector3 force = interaction.ForceOnBody;
            HangingLoad = Mathf.Max(0f, -Vector3.Dot(force, normal));
            float ownWeight = body.mass * Physics.gravity.magnitude;
            Vector3 lever = interaction.GripPoint - body.worldCenterOfMass;
            float reach = Mathf.Max(0f, Vector3.Dot(lever, outward));
            float height = Mathf.Max(.2f, Vector3.Dot(interaction.GripPoint - transform.position, normal));
            float overturning = HangingLoad * Mathf.Max(0f, reach - frontSupportExtent)
                + Mathf.Max(0f, Vector3.Dot(force, outward)) * height;
            TippingMoment = Mathf.Max(0f, overturning - ownWeight * frontSupportExtent);
            if (HangingLoad < ownWeight * .3f || TippingMoment <= 0f)
            { RestoreTraction(); Unbalance01 = 0f; return; }

            Unbalance01 = Mathf.MoveTowards(Unbalance01, 1f, Time.fixedDeltaTime / Mathf.Max(.05f, loadBuildUpSeconds));
            if (!slipping) { capsule.sharedMaterial = slippingMaterial; slipping = true; }
            float centerHeight = Mathf.Max(.3f, Vector3.Dot(body.worldCenterOfMass - transform.position, normal));
            float forceMagnitude = Mathf.Min(TippingMoment / centerHeight, body.mass * maxSlipAcceleration);
            body.WakeUp();
            body.AddForce(outward * (forceMagnitude * Unbalance01), ForceMode.Force);
        }
        private void RestoreTraction()
        {
            if (!slipping || !capsule) return;
            capsule.sharedMaterial = originalMaterial; slipping = false;
        }
        private void OnDisable() { RestoreTraction(); Unbalance01 = 0f; }
        private void OnDestroy() { RestoreTraction(); if (slippingMaterial) Destroy(slippingMaterial); }
    }
}
