using UnityEngine;

namespace GoatDescent
{
    /// <summary>Input ownership is independent of simulation and lifetime.</summary>
    public sealed class GoatLocalControl : MonoBehaviour
    {
        public bool IsControlled { get; private set; } = true;
        public bool IsRoutePlayer { get; set; } = true;
        public string Label { get; set; } = "A";
        public void SetControlled(bool value)
        {
            if (IsControlled == value) return;
            GetComponent<GoatInteraction>()?.CancelAll();
            GetComponent<GoatController>()?.ClearInput();
            GetComponent<GoatJumpController>()?.ResetInput();
            GetComponent<GoatCliffGrip>()?.ClearInput();
            IsControlled = value;
        }
        public static bool AllowsInput(Component component)
        {
            var gate = component.GetComponent<GoatLocalControl>();
            var life = component.GetComponent<RespawnController>();
            return (!gate || gate.IsControlled) && (!life || !life.IsDead);
        }
        public static bool CountsForRoute(Component component)
        {
            var gate = component.GetComponent<GoatLocalControl>();
            return !gate || gate.IsRoutePlayer;
        }
    }
}
