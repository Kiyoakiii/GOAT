using UnityEngine;

namespace GoatDescent
{
    [DisallowMultipleComponent]
    public sealed class GoatBeerEffect : MonoBehaviour
    {
        [SerializeField] private BeerPuddleSettings settings;
        private float drunkUntil,nextStumble;
        private bool stationaryJump;
        private bool jumpDrunk;
        private GoatController motor;
        public float Remaining => Mathf.Max(0f,drunkUntil-Time.time);
        public bool Intoxicated => Remaining>0f;
        public float StabilityMultiplier => Intoxicated && settings ? settings.drunkStability : 1f;
        public float SwayDegrees => Intoxicated && settings ? settings.drunkSwayDegrees : 0f;
        public bool UseCalmJump => stationaryJump && !jumpDrunk;
        public float JumpDamping => jumpDrunk && settings ? settings.originalJumpDamping :
            stationaryJump ? GoatMechanics.For(this).stationaryJumpDamping : GoatMechanics.For(this).movingJumpDamping;
        private void Awake() => motor=GetComponent<GoatController>();
        public void BeginJump(bool onSpot) {stationaryJump=onSpot;jumpDrunk=Intoxicated;}
        public void Bathe(BeerPuddleSettings value)
        {
            if(!MountainAuthority.IsHost || GetComponent<RespawnController>()?.IsDead==true) return;
            settings=value;
            drunkUntil=Time.time+settings.intoxicationSeconds;
            nextStumble=Time.time+settings.stumbleInterval;
            GetComponent<GoatHealth>()?.HealFully();
            GetComponent<GoatJumpController>()?.AnnounceTrick("ПИВНАЯ ВАННА!");
        }
        private void Update()
        {
            if(!MountainAuthority.IsHost || !Intoxicated || !settings) return;
            motor ??= GetComponent<GoatController>();
            if(GetComponent<RespawnController>()?.IsDead==true) {Clear();return;}
            if(Time.time<nextStumble) return;
            var ground=GetComponent<GoatGroundDetector>();
            if(!motor || !motor.Grounded || motor.IsPredatorCarried || !ground ||
                ground.SlopeAngle>settings.maxStumbleSlope || motor.Velocity.magnitude>settings.maxStumbleSpeed) return;
            // A wobble becomes a stumble on a turn, a slope, or after standing
            // still for a while. Linked rescue and other interactions own the pose.
            var comedy=GetComponent<GoatComedyActions>();
            if(comedy && comedy.TryDrunkenStumble(settings.stumbleSeconds))
                nextStumble=Time.time+settings.stumbleInterval;
            else nextStumble=Time.time+.5f;
        }
        public void ApplyNetwork(float remaining)
        {
            if(MountainAuthority.IsHost) return;
            if(!settings) settings=Resources.Load<BeerPuddleSettings>("BeerPuddleSettings");
            drunkUntil=Time.time+Mathf.Max(0f,remaining);
        }
        public void Clear() {drunkUntil=nextStumble=0f;stationaryJump=jumpDrunk=false;}
    }
}
