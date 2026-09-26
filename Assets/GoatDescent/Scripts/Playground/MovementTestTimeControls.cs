using UnityEngine;

namespace GoatDescent
{
    /// <summary>Slow motion is a practice tool available only in the movement test scene.</summary>
    public sealed class MovementTestTimeControls : MonoBehaviour
    {
        [SerializeField] private float slowScale = .3f;
        private float normalFixedDeltaTime;
        private bool aimSlow;
        public static MovementTestTimeControls Instance { get; private set; }
        public bool IsSlow { get; private set; }

        private void Awake() { Instance = this; normalFixedDeltaTime = Time.fixedDeltaTime; }
        public void SetAimSlow(bool value) { aimSlow = value; Apply(); }
        private void Apply()
        {
            Time.timeScale = aimSlow ? Mathf.Min(.18f, IsSlow ? slowScale : 1f) : IsSlow ? slowScale : 1f;
            Time.fixedDeltaTime = normalFixedDeltaTime * Time.timeScale;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.T)) return;
            IsSlow = !IsSlow;
            Apply();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = normalFixedDeltaTime;
            if (Instance == this) Instance = null;
        }
    }
}
