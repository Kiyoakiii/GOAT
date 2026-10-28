using UnityEngine;

namespace GoatDescent
{
    [DisallowMultipleComponent]
    public sealed class GoatMechanics : MonoBehaviour
    {
        [SerializeField] private GoatMechanicsSettings settings;
        private static GoatMechanicsSettings fallback;
        public GoatMechanicsSettings Settings
        {
            get
            {
                if(settings) return settings;
                settings=Resources.Load<GoatMechanicsSettings>("GoatMechanics");
                if(settings) return settings;
                if(!fallback) {fallback=ScriptableObject.CreateInstance<GoatMechanicsSettings>();fallback.hideFlags=HideFlags.DontSave;}
                return fallback;
            }
        }
        public void Configure(GoatMechanicsSettings value) => settings=value;
        public static GoatMechanicsSettings For(Component component)
        {
            var tuning=component.GetComponent<GoatMechanics>();
            if(!tuning) tuning=component.gameObject.AddComponent<GoatMechanics>();
            return tuning.Settings;
        }
    }
}
