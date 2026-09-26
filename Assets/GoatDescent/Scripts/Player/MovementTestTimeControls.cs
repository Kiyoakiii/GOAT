using UnityEngine;

namespace GoatDescent
{
    public sealed class MovementTestTimeControls : MonoBehaviour
    {
        public static MovementTestTimeControls Instance { get; private set; }
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void SetAimSlow(bool enabled) { }
    }
}