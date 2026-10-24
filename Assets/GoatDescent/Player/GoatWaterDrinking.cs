using UnityEngine;

namespace GoatDescent
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-20)]
    public sealed class GoatWaterDrinking : MonoBehaviour
    {
        private BeerPuddle puddle;
        private float finishAt;
        public bool Active => finishAt > 0f;
        public float Remaining => Mathf.Max(0, finishAt - Time.time);
        // E is contextual: water has priority over a nearby madness chest.
        // Pee has a separate P binding so contaminating and drinking are distinct.
        public bool TryInteract()
        {
            if(!MountainAuthority.IsHost) return false;
            if(Active) {Cancel();return true;}
            var motor=GetComponent<GoatController>();
            if(!motor || GetComponent<RespawnController>()?.IsDead==true) return false;
            if(BeerPuddle.FindDrinkable(motor,GoatHerdSettings.Get().drinkingReach)) return TryDrink();
            return MadnessChest.Current && MadnessChest.Current.TryOpen(motor);
        }
        public bool TryDrink()
        {
            if (!MountainAuthority.IsHost) return false;
            if (Active) { Cancel(); return true; }
            var motor = GetComponent<GoatController>();
            if (!motor || !motor.Grounded || motor.IsPredatorCarried || motor.Velocity.magnitude > 1.7f
                || GetComponent<RespawnController>()?.IsDead == true || GetComponent<GoatToiletAction>()?.Active == true
                || GetComponent<GoatInteraction>()?.IsBusy == true || GetComponent<GoatComedyActions>()?.Busy == true
                || GetComponent<GoatPunchBuff>()?.PerformingPair == true || GetComponent<GoatDescentTricks>()?.Busy == true)
                return false;
            puddle = BeerPuddle.FindDrinkable(motor, GoatHerdSettings.Get().drinkingReach);
            if (!puddle) { GetComponent<GoatJumpController>()?.AnnounceTrick("ПОДОЙДИ К ВОДЕ"); return false; }
            finishAt = Time.time + GoatHerdSettings.Get().drinkingSeconds;
            Vector3 toward = Vector3.ProjectOnPlane(puddle.transform.position - transform.position, Vector3.up).normalized;
            if (toward.sqrMagnitude < .1f) toward = GetComponent<GoatVisualController>()?.Facing ?? transform.forward;
            GetComponent<GoatVisualController>()?.PlayInteraction("Goat_EatGrass", Remaining, toward);
            motor.ClearInput(); GetComponent<GoatJumpController>()?.ResetInput();
            return true;
        }
        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            var motor = GetComponent<GoatController>();
            if (finishAt > 0)
            {
                if (!motor || !motor.Grounded || motor.IsPredatorCarried || GetComponent<RespawnController>()?.IsDead == true
                    || !puddle || !puddle.CanDrink(motor, GoatHerdSettings.Get().drinkingReach)
                    || GetComponent<GoatInteraction>()?.IsLinked == true) { Cancel(); return; }
                if (Time.time >= finishAt)
                {
                    var source=puddle;
                    Cancel();
                    source.ApplyDrink(motor);
                    // Do not restart from the same key press on the completion frame.
                    return;
                }
            }
            var gate = GetComponent<GoatLocalControl>();
            if (!motor || motor.NetworkOwnedByRemote || gate && !gate.IsControlled
                || GoatSettingsMenu.BlocksInput || GoatSkinMenu.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        }
        public void Cancel() { finishAt = 0; puddle = null; GetComponent<GoatVisualController>()?.EndDrinkingPose(); }
        public void Apply(float remaining) { if (!MountainAuthority.IsHost) finishAt = remaining > 0 ? Time.time + remaining : 0; }
        private void OnDisable() => Cancel();
    }
}
