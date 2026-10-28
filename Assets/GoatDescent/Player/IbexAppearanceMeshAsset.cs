using UnityEngine;

namespace GoatDescent
{
    // Store the large appearance mesh in binary without changing project-wide
    // serialization preferences or converting unrelated assets.
    [PreferBinarySerialization]
    public sealed class IbexAppearanceMeshAsset : ScriptableObject
    {
        public Mesh mesh;
    }
}
