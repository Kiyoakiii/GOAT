using UnityEngine;
namespace GoatDescent
{
    public sealed class GoatSkinDefinition : ScriptableObject
    {
        public int number;
        public string displayName;
        public Mesh mesh;
        public Material material;
        public Material[] materials;
        public string[] boneNames;
        public Texture2D preview;
        public AnimationClip[] animations;
        public bool useCustomHeadAttachments;
        public Vector3 headCollisionOffset;
        public Vector3 noseOffset;
        public Vector3 hornOffset;
        public GameObject tonguePrefab;
        public Vector3 tongueOffset;
        public Quaternion tongueRotation=Quaternion.identity;
        public bool useCustomBodyAttachments;
        public Vector3 droppingsOffset;
        public Vector3 urineOffset;
        public float grassNeckBendDegrees = 35f;
    }
}
