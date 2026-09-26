using UnityEngine;

namespace GoatDescent
{
    public sealed class SlopeRun : MonoBehaviour
    {
        public static SlopeRun Instance { get; private set; }
        public bool Finished { get; private set; }
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Notify(string message) { }
        public void ResetRun() => Finished = false;
    }
}