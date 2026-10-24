using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Cliff-attached Level 1 recovered from 34b62b3, using the current shared gameplay.</summary>
    public sealed class PillarDescentLevel : MonoBehaviour
    {
        private readonly List<LandingPlatform> route=new List<LandingPlatform>();
        public IReadOnlyList<LandingPlatform> Route => route;
        public Vector3 SpawnPoint {get;private set;}
        public Vector3 Forward {get;private set;}
        public Vector3 Outward {get;private set;}
        public Material RockMaterial {get;private set;}
        public int LedgeCount => route.Count;
        public float MaxJumpGap {get;private set;}
        public LevelOneCave Cave {get;private set;}
        public BeerPuddle RoutePuddle {get;private set;}
        public const int PuddleRestIndex = 12;
        private struct LedgeSite
        {
            public Vector3 Wall,Outward,Center;
            public float Angle;
            public bool Clear;
        }

        public static PillarDescentLevel Build(Transform pillar,Vector3 summit)
        {
            var cliff=pillar.Find("Stratified sandstone");
            var collider=cliff ? cliff.GetComponent<MeshCollider>() : null;
            if(!collider) {Debug.LogError("LEVEL1_ROUTE missing start pillar collider");return null;}
            var trees=GetTreeBounds(pillar);
            if(trees.Count==0) {Debug.LogError("LEVEL1_ROUTE needs readable spawn-pillar vegetation for clearance");return null;}
            var sites=new List<LedgeSite[]>();
            Vector3 face=pillar.forward;
            const float drop=4.45f;
            const int angles=72;
            // Keep the recovered first 51 ledges, then continue to the actual foot
            // of this pillar instead of leaving the route suspended halfway down.
            float bottom=collider.bounds.min.y+10f;
            int count=Mathf.Clamp(Mathf.CeilToInt((summit.y-4.2f-bottom)/drop)+1,2,128);
            for(int i=0;i<count;i++)
            {
                float y=summit.y-4.2f-i*drop;
                var ring=new LedgeSite[angles];
                for(int a=0;a<angles;a++)
                {
                    float angle=-180f+a*5f;
                    Vector3 outward=Quaternion.AngleAxis(angle,Vector3.up)*face;
                    var ray=new Ray(new Vector3(pillar.position.x,y,pillar.position.z)+outward*110f,-outward);
                    if(!collider.Raycast(ray,out var hit,160f)) continue;
                    ring[a]=new LedgeSite {Wall=hit.point,Outward=outward,Center=hit.point+outward*2.3f,
                        Angle=angle,Clear=IsTreeClear(hit.point,outward,LedgeWidth(i),trees)};
                }
                sites.Add(ring);
            }
            int originalCount=Mathf.Min(51,sites.Count);
            int[] chosen=PlanRoute(sites.GetRange(0,originalCount),summit);
            if(chosen!=null && sites.Count>originalCount)
            {
                int[] tail=PlanRoute(sites.GetRange(originalCount-1,sites.Count-originalCount+1),
                    sites[originalCount-1][chosen[originalCount-1]].Center,chosen[originalCount-1]);
                if(tail==null) chosen=null;
                else
                {
                    var complete=new int[sites.Count];
                    System.Array.Copy(chosen,complete,chosen.Length);
                    System.Array.Copy(tail,1,complete,originalCount,tail.Length-1);
                    chosen=complete;
                }
            }
            if(chosen==null || chosen.Length<2) {Debug.LogError("LEVEL1_ROUTE no supported tree-free route on the current pillar");return null;}
            var root=new GameObject("Level 1 — cliff descent");root.transform.SetParent(pillar,false);
            var level=root.AddComponent<PillarDescentLevel>();
            level.RockMaterial=new Material(cliff.GetComponent<Renderer>().sharedMaterial) {name="Level 1 ledge stone"};
            for(int i=0;i<chosen.Length;i++)
            {
                var site=sites[i][chosen[i]];
                Vector3 point=CreateLedge(root.transform,site.Wall,site.Outward,level.RockMaterial,i,out var shelf);
                shelf.AddComponent<MountainSlopeSurface>();
                bool checkpoint=i==12 || i==27 || i==41 || (i>51 && i%14==13);
                bool nearCheckpoint=Mathf.Abs(i-12)<=1 || Mathf.Abs(i-27)<=1 || Mathf.Abs(i-41)<=1;
                bool fragile=i>=5 && i<chosen.Length-3 && (i%4==2 || i%7==4) && !nearCheckpoint && !checkpoint;
                if(fragile)
                {
                    var body=shelf.AddComponent<Rigidbody>();body.mass=28f;
                    shelf.AddComponent<CrumblingPlatform>().Configure($"LEVEL1-{i+1:000}");
                }
                level.route.Add(new LandingPlatform {Index=i,Center=point,Radius=LedgeWidth(i)*.5f,
                    Progress=i/(float)(chosen.Length-1),Type=fragile ? LandingPlatformType.Crumbling :
                        checkpoint ? LandingPlatformType.Rest : LandingPlatformType.Standard,IsCheckpoint=checkpoint});
                if(i>0) level.MaxJumpGap=Mathf.Max(level.MaxJumpGap,HorizontalGap(point,level.route[i-1].Center));
                if(i==0)
                {
                    level.SpawnPoint=site.Wall+site.Outward*1.65f+Vector3.up*.12f;
                    level.Outward=site.Outward;
                }
                if(i==PuddleRestIndex) level.PlaceRoutePuddle(shelf.transform,point);
            }
            var lastSite=sites[chosen.Length-1][chosen[chosen.Length-1]];
            level.Cave=LevelOneCave.Build(root.transform,lastSite.Wall,lastSite.Outward,level.RockMaterial);
            level.route.Add(new LandingPlatform {Index=level.route.Count,Center=level.Cave.Approach,
                Radius=4f,Type=LandingPlatformType.Rest});
            level.route.Add(new LandingPlatform {Index=level.route.Count,Center=level.Cave.Entrance,
                Radius=3f,Type=LandingPlatformType.Rest});
            level.route.Add(new LandingPlatform {Index=level.route.Count,Center=level.Cave.FinishPoint,
                Radius=3f,Type=LandingPlatformType.Rest});
            for(int i=0;i<level.route.Count;i++)
            {
                var point=level.route[i];point.Progress=i/(float)(level.route.Count-1);level.route[i]=point;
                if(i>0) level.MaxJumpGap=Mathf.Max(level.MaxJumpGap,HorizontalGap(point.Center,level.route[i-1].Center));
            }
            level.Forward=Vector3.ProjectOnPlane(level.route[1].Center-level.route[0].Center,Vector3.up).normalized;
            MountainGenerator.UseRoute(level.route);
            Physics.SyncTransforms();
            Debug.Log($"LEVEL1_ROUTE_READY ledges={level.LedgeCount} maxJumpGap={level.MaxJumpGap:F1} trees={trees.Count} spawn={level.SpawnPoint} cave={level.Cave.FinishPoint} puddle={level.RoutePuddle?.transform.position}");
            return level;
        }

        private void PlaceRoutePuddle(Transform shelf,Vector3 point)
        {
            var prefab=Resources.Load<GameObject>("BeerPuddle");
            if(!prefab) {Debug.LogError("LEVEL1_ROUTE missing BeerPuddle prefab");return;}
            var instance=Instantiate(prefab,point,shelf.rotation,shelf);
            instance.name="Emerald water — Level 1 rest ledge";
            RoutePuddle=instance.GetComponent<BeerPuddle>();
        }

        private static float LedgeWidth(int index) => index==PuddleRestIndex ? 6.4f : index%7==4 ? 3.4f : 4.1f;

        private static List<Bounds> GetTreeBounds(Transform pillar)
        {
            var result = new List<Bounds>();
            AddMeshPartBounds(pillar.Find("Dense broadleaf forest and ledge shrubs"), result);
            AddMeshPartBounds(pillar.Find("Tree trunks and branches"), result);
            return result;
        }

        private static void AddMeshPartBounds(Transform part, List<Bounds> result)
        {
            var mesh = part ? part.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (!mesh || !mesh.isReadable) return;
            // The vista builder appends every crown and branch as a separate,
            // contiguous piece of mesh. Recover those pieces from triangle order.
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int first = -1, last = -1;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int lo = Mathf.Min(triangles[i], Mathf.Min(triangles[i + 1], triangles[i + 2]));
                int hi = Mathf.Max(triangles[i], Mathf.Max(triangles[i + 1], triangles[i + 2]));
                if (first >= 0 && lo > last)
                {
                    AddVertexBounds(part, vertices, first, last, result);
                    first = -1;
                }
                if (first < 0) first = lo;
                last = Mathf.Max(last, hi);
            }
            if (first >= 0) AddVertexBounds(part, vertices, first, last, result);
        }

        private static void AddVertexBounds(Transform part, Vector3[] vertices, int first, int last, List<Bounds> result)
        {
            Bounds bounds = new Bounds(part.TransformPoint(vertices[first]), Vector3.zero);
            for (int i = first + 1; i <= last; i++) bounds.Encapsulate(part.TransformPoint(vertices[i]));
            bounds.Expand(.15f);
            result.Add(bounds);
        }

        private static bool IsTreeClear(Vector3 wall, Vector3 outward, float width, List<Bounds> trees)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, outward);
            Vector3 halfSide = tangent * (width * .5f + .15f);
            Bounds landing = new Bounds(wall + outward * 2f + Vector3.up * 1.35f, Vector3.zero);
            foreach (float side in new[] { -1f, 1f })
            foreach (float depth in new[] { .9f, width>6f ? 5.2f : 3.9f })
            foreach (float height in new[] { -.15f, 3.25f })
                landing.Encapsulate(wall + halfSide * side + outward * depth + Vector3.up * height);
            foreach (Bounds tree in trees)
                if (tree.Intersects(landing)) return false;
            return true;
        }

        private static int[] PlanRoute(List<LedgeSite[]> sites, Vector3 spawn,int firstSite=-1)
        {
            if (sites.Count == 0) return null;
            const float unreachable = 1e12f;
            int count = sites[0].Length;
            var cost = new float[sites.Count, count];
            var previous = new int[sites.Count, count];
            for (int i = 0; i < sites.Count; i++)
            for (int j = 0; j < count; j++) { cost[i, j] = unreachable; previous[i, j] = -1; }

            for (int j = 0; j < count; j++)
            {
                LedgeSite site = sites[0][j];
                if (!site.Clear || (firstSite>=0 && j!=firstSite)) continue;
                float distance = HorizontalGap(spawn, site.Center);
                // Spawn on the chosen clear edge, rather than forcing a route
                // through the old summit clearing on a changed pillar layout.
                cost[0, j] = distance * .15f + Mathf.Abs(site.Angle) * .03f;
            }

            for (int i = 1; i < sites.Count; i++)
            {
                float progress = i / (float)(sites.Count - 1);
                float targetGap = Mathf.Lerp(4.4f, 7.5f, progress);
                float minimumGap = Mathf.Lerp(1.5f, 2.3f, progress);
                for (int j = 0; j < count; j++)
                {
                    LedgeSite site = sites[i][j];
                    if (!site.Clear) continue;
                    for (int k = 0; k < count; k++)
                    {
                        if (cost[i - 1, k] >= unreachable) continue;
                        float gap = HorizontalGap(sites[i - 1][k].Center, site.Center);
                        if (gap < minimumGap || gap > 8.2f) continue;
                        float candidateCost = cost[i - 1, k] + (gap - targetGap) * (gap - targetGap) + Mathf.Abs(site.Angle) * .001f;
                        if (candidateCost >= cost[i, j]) continue;
                        cost[i, j] = candidateCost;
                        previous[i, j] = k;
                    }
                }
                int clearCount = 0, reachableCount = 0;
                for (int j = 0; j < count; j++)
                {
                    if (sites[i][j].Clear) clearCount++;
                    if (cost[i, j] < unreachable) reachableCount++;
                }
                if (reachableCount == 0)
                {
                    Debug.LogWarning($"LEVEL1_ROUTE blocked at ring {i + 1}/{sites.Count}: clearSites={clearCount}");
                    return null;
                }
            }

            int best = -1;
            float bestCost = unreachable;
            int end = sites.Count - 1;
            for (int j = 0; j < count; j++)
                if (cost[end, j] < bestCost) { bestCost = cost[end, j]; best = j; }
            if (best < 0) return null;
            var route = new int[sites.Count];
            for (int i = end; i >= 0; i--)
            {
                route[i] = best;
                best = previous[i, best];
            }
            return route;
        }

        private static float HorizontalGap(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static Vector3 CreateLedge(Transform parent, Vector3 wallPoint, Vector3 outward, Material material, int index, out GameObject shelf)
        {
            shelf = new GameObject($"Cliff ledge {index + 1:00} — rooted in rock");
            shelf.transform.SetParent(parent);
            shelf.transform.SetPositionAndRotation(wallPoint, Quaternion.LookRotation(outward, Vector3.up));
            float width = LedgeWidth(index);
            float half = width * .5f;
            float front=index==PuddleRestIndex ? 5.2f : 3.5f;
            var vertices = new[]
            {
                new Vector3(-half * 1.1f, 0f, -3.2f), new Vector3(half * 1.1f, 0f, -3.2f),
                new Vector3(-half, 0f, front), new Vector3(half, 0f, front-.3f),
                new Vector3(-half * .85f, -1.7f, -2.7f), new Vector3(half * .85f, -1.7f, -2.7f),
                new Vector3(-half * .42f, -1.2f, 2.4f), new Vector3(half * .42f, -1.2f, 2.2f)
            };
            int[] triangles =
            {
                0, 2, 1, 1, 2, 3,
                0, 4, 2, 2, 4, 6, 3, 7, 1, 1, 7, 5,
                2, 6, 3, 3, 6, 7, 4, 5, 6, 5, 7, 6
            };
            var mesh = new Mesh { name = "Attached cliff shelf", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            shelf.AddComponent<MeshFilter>().sharedMesh = mesh;
            shelf.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = shelf.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, -.34f, index==PuddleRestIndex ? 1.1f : .3f);
            collider.size = new Vector3(width * .9f, .68f, index==PuddleRestIndex ? 7.2f : 5.6f);
            return wallPoint + outward * 2.3f;
        }

    }
}
