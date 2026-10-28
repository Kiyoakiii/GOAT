using UnityEngine;

namespace GoatDescent
{
    [CreateAssetMenu(menuName = "Goat Descent/Herd and coat settings")]
    public sealed class GoatHerdSettings : ScriptableObject
    {
        [Header("Separation")]
        [Min(1f)] public float separationDistance = 10f;
        [Min(1f)] public float reunionDistance = 8.5f;
        [Min(0f)] public float graceSeconds = 5f;
        [Header("Eagles")]
        [Range(0, 1)] public float normalHuntChance = .1f;
        [Range(0, 1)] public float lonelyHuntChance = .4f;
        [Min(1f)] public float lonelyTargetWeight = 4f;
        [Tooltip("Force a hunt after this many idle checks with an exposed target. 0 keeps pure probability. Busy hunters and real shelter do not advance the counter.")]
        [Min(0)] public int maxHuntRollsWithoutAttack = 4;
        [Header("Panic bleating")]
        [Min(1f)] public float quietToInhaleSeconds = 4f;
        [Min(.2f)] public float inhaleSeconds = 1.2f;
        [Min(3f)] public float repeatBleatSeconds = 14f;
        [Header("Electricity")]
        [Min(1f)] public float fullChargeSeconds = 35f;
        [Range(.01f, 1)] public float minimumShockCharge = .25f;
        [Min(0f)] public float calmDischargePerSecond = .0125f;
        [Min(.1f)] public float axeGroundingSeconds = 2f;
        [Min(0f)] public float shockImpulse = 100f;
        [Min(.1f)] public float gripInterruptSeconds = .65f;
        [Header("Water and fur")]
        [Min(.5f)] public float drinkingSeconds = 3f;
        [Min(.1f)] public float drinkingReach = 1.1f;
        [Min(1f)] public float furRegrowthSeconds = 600f;
        public Color bareSkinColor = new Color(.88f, .66f, .55f);

        private static GoatHerdSettings cached;
        public static GoatHerdSettings Get()
        {
            if (!cached) cached = Resources.Load<GoatHerdSettings>("GoatHerdSettings");
            if (!cached) { cached = CreateInstance<GoatHerdSettings>(); cached.hideFlags = HideFlags.HideAndDontSave; }
            return cached;
        }
    }
}
