using UnityEngine;

namespace GoatDescent
{
    [CreateAssetMenu(menuName="Goat Descent/Goat mechanics",fileName="GoatMechanics")]
    public sealed class GoatMechanicsSettings : ScriptableObject
    {
        [Header("Body sway and muscles")]
        [Min(.1f)] public float muscleStrength=1f;
        [Min(.1f)] public float groundedMuscleStrength=1f;
        [Min(.1f)] public float jumpMuscleStrength=1.05f;
        [Min(.1f)] public float groundedDamping=1.8f;
        [Min(.1f)] public float airborneDamping=1.25f;
        [Min(1f)] public float maxMuscleAcceleration=900f;
        [Range(0f,2f)] public float walkingAnimationAmplitude=.9f;
        [Range(1f,25f)] public float spineBendDegrees=10f;
        [Range(1f,22f)] public float elasticBendDegrees=6f;
        [Range(1f,60f)] public float upperLegSideDegrees=16f;
        [Range(1f,45f)] public float lowerLegSideDegrees=7f;
        [Range(5f,70f)] public float pelvisSwingDegrees=32f;
        [Range(1f,45f)] public float headBendDegrees=25f;
        [Range(1f,35f)] public float neckBendDegrees=20f;

        [Header("Walking head and neck sway")]
        [Tooltip("Small head movements following the walking cycle. Zero disables them.")]
        [Range(0f,8f)] public float walkingHeadSwayDegrees=3f;
        [Tooltip("Small neck movements following the walking cycle. Zero disables them.")]
        [Range(0f,5f)] public float walkingNeckSwayDegrees=1.6f;

        [Header("Jump on the spot: old lively jump = 1.0 / 1.25 damping")]
        [Min(0f)] public float stationaryJumpSpeed=1.1f;
        [Range(0f,1f)] public float stationaryJumpPose=.72f;
        [Range(0f,1f)] public float stationaryJumpFrontLead=.65f;
        [Min(.1f)] public float stationaryJumpDamping=1.65f;
        [Min(.1f)] public float movingJumpDamping=1.25f;
        [Min(.1f)] public float jumpPoseSeconds=.72f;
        [Min(.1f)] public float jumpStabilizationSeconds=1.25f;

        [Header("Jump height and charge")]
        [Min(.1f)] public float jumpVelocity=5.15f;
        [Min(.1f)] public float airJumpVelocity=3.6f;
        [Min(0f)] public float chargedVerticalBonus=1.15f;
        [Min(0f)] public float chargedForwardBoost=2.4f;
        [Min(.05f)] public float fullChargeSeconds=.72f;
        [Min(0f)] public float coyoteTime=.12f;
        [Min(0f)] public float jumpBuffer=.15f;

        [Header("Ground movement and balance")]
        [Min(.1f)] public float maxGroundSpeed=5.35f;
        [Min(.1f)] public float groundAcceleration=19f;
        [Min(0f)] public float airAcceleration=5f;
        [Range(0f,89f)] public float steepGripAngle=73f;
        [Range(0f,89f)] public float slideAngle=86f;
        [Range(.1f,1f)] public float steepSpeedMultiplier=.85f;
        [Min(.1f)] public float stability=1f;
        [Range(.01f,.8f)] public float balanceSlipThreshold=.28f;
        [Min(.05f)] public float balanceSlipDelay=.35f;
        [Min(.1f)] public float balanceRecoverySpeed=2.8f;
        [Range(0f,45f)] public float maxBalanceLean=22f;
        [Min(.1f)] public float passengerMassMultiplier=.25f;
        [Header("Landing damage (health is always 100)")]
        [Min(0f)] public float landingDamageStartSpeed=6f;
        [Min(.1f)] public float fatalLandingSpeed=11.5f;
        [Min(0f)] public float maxLandingDamage=45f;
        [Min(0f)] public float landingDamageCooldown=.35f;
    }

}
