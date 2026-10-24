using UnityEngine;

namespace GoatDescent
{
    /// <summary>A reusable authored ledge beside the spawn; independent of terrain generation.</summary>
    public static class IbexCliffStage
    {
        public static Vector3 SpawnAbovePlatform(GameObject stage,Vector3 requested)
        {
            var ledge=stage ? stage.transform.Find("LedgePlatform") : null;
            var collider=ledge ? ledge.GetComponent<Collider>() : null;
            if(collider) requested.y=collider.bounds.max.y+.12f;
            return requested;
        }

        public static GameObject PlaceAtSpawn(Vector3 spawn, float yaw, Transform parent)
        {
            var prefab=Resources.Load<GameObject>("ReferenceIbex/IbexCliffStage");
            if(!prefab) return null;
            Vector3 surface=spawn;
            int mask=~LayerMask.GetMask("Player");
            if(Physics.Raycast(spawn+Vector3.up*8f,Vector3.down,out var hit,25f,mask,QueryTriggerInteraction.Ignore))
                surface=hit.point;
            var instance=Object.Instantiate(prefab,surface+Vector3.up*.75f,
                Quaternion.Euler(0f,yaw,0f),parent);
            instance.name="Ibex stone platform and cliff";
            Physics.SyncTransforms();
            return instance;
        }
    }
}
