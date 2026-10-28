using UnityEngine;

namespace GoatDescent
{
    [CreateAssetMenu(menuName="Goat Descent/Healing puddle",fileName="BeerPuddleSettings")]
    public sealed class BeerPuddleSettings : ScriptableObject
    {
        [Header("Drinking")]
        [Tooltip("Health lost once after finishing a drink of contaminated purple water. Standing in water has no effect.")]
        [Min(0f)] public float contaminatedDrinkDamage=25f;
        [Header("Legacy beer effect")]
        [Min(1f)] public float intoxicationSeconds=30f;
        [Range(.2f,1f)] public float drunkStability=.82f;
        [Range(0f,25f)] public float drunkSwayDegrees=5f;
        [Min(1f)] public float stumbleInterval=7f;
        [Min(.3f)] public float stumbleSeconds=1.7f;
        [Range(0f,70f)] public float maxStumbleSlope=60f;
        [Min(.1f)] public float maxStumbleSpeed=4f;
        [Min(.1f)] public float originalJumpDamping=1.25f;

        [Header("Independent puddle shape")]
        public Vector2 radii=new Vector2(1.6f,1.15f);
        [Range(0f,.2f)] public float surfaceOffset=.035f;
        [UnityEngine.Serialization.FormerlySerializedAs("beerColor")]
        public Color healingColor=new Color(.12f,.64f,.39f,.8f);
        public Color foamColor=new Color(.74f,1f,.88f,.65f);
        public Color aromaColor=new Color(.42f,.94f,.67f,.48f);
        [Header("Purple water after urine hits the puddle")]
        public Color purpleColor=new Color(.48f,.23f,.76f,.8f);
        public Color purpleFoamColor=new Color(.89f,.8f,1f,.65f);
        public Color purpleAromaColor=new Color(.74f,.48f,.96f,.48f);
        [Min(.05f)] public float colorChangeSeconds=.8f;
        [Header("Foam and aroma")]
        [Range(6,100)] public int foamBubbles=26;
        [Range(1,20)] public int skyLines=5;
        [Min(.1f)] public float skyLineHeight=1.8f;
        [Min(.1f)] public float nearbyNoseRadius=2f;
        [Range(2,3)] public int noseLines=3;
        [Min(.005f)] public float aromaLineWidth=.028f;
    }
}
