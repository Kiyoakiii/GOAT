using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    // One deterministic authored heightfield. The surface itself supplies every obstacle.
    public static class SteepMountain
    {
        public const float StartZ = -140f, EndZ = 335f, HalfWidth = 150f;
        private static readonly float[] BreakZ = { -76,-56,-16,4,24,60,78,112,150,168,215,245,290,335 };
        private static readonly float[] BreakY = { 330,327,285,279,220,194,145,127,103,62,32,14,4,0 };
        public static Vector3 Spawn => new Vector3(Center(-66f), HeightAt(Center(-66f),-66f)+.2f,-66f);
        public static float Center(float z) => Mathf.Sin(z*.028f)*18f + Mathf.Sin(z*.068f)*6f;
        private static float Bell(float value, float center, float width) => Mathf.Exp(-Mathf.Pow((value-center)/width,2f));
        public static Vector3 WallPracticeShelf => new Vector3(Center(-28f)+4.5f, BaseHeight(-28f)+1.2f, -21f);
        private static float Shelf(float current,float x,float z,float centerX,float centerZ,float rx,float rz,float elevation)
        {
            float distance=Mathf.Sqrt(Mathf.Pow((x-centerX)/rx,2f)+Mathf.Pow((z-centerZ)/rz,2f));
            float weight=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.62f,1.65f,distance));
            return Mathf.Lerp(current,elevation,weight);
        }
        public static float BaseHeight(float z)
        {
            for(int i=0;i<BreakZ.Length-1;i++)
                if(z<=BreakZ[i+1])
                {
                    float t=Mathf.InverseLerp(BreakZ[i],BreakZ[i+1],z);
                    // Retain sustained slopes, but soften the joins so crests launch the goat naturally.
                    float blend=Mathf.Lerp(t,t*t*(3f-2f*t),.62f);
                    return Mathf.Lerp(BreakY[i],BreakY[i+1],blend);
                }
            return BreakY[BreakY.Length-1];
        }
        public static float HeightAt(float x,float z)
        {
            float d=x-Center(z);
            float bank=Mathf.Pow(Mathf.Abs(d)/22f,1.45f)*9f;
            float branch=Center(z)+Mathf.Sin(z*.018f+.8f)*29f;
            float sideBank=Mathf.Pow(Mathf.Abs(x-branch)/18f,1.5f)*9f+4.5f;
            bank=Mathf.Min(bank,sideBank);
            float splitRidge=12f*Bell(d,0f,4.5f)*Bell(z,104f,24f);
            float erodedFlanks=-18f*Bell(z,146f,12f)*(1f-Bell(d,0f,5f));
            float takeoff=4.5f*Bell(z,54f,6f)*Bell(d,0f,16f);
            float bankedTurn=Mathf.Sin(z*.035f)*d*.16f;
            float bowl=-7f*Bell(z,213f,19f)*Bell(d,0f,21f);
            float texture=(Mathf.PerlinNoise(x*.048f+17f,z*.038f+31f)-.5f)*3.2f;
            float features=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(-61f,-42f,z)) *
                (1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(275f,315f,z)));
            float core=BaseHeight(z)+features*(bank+splitRidge+erodedFlanks+takeoff+bankedTurn+bowl+texture);
            // A short traversal lesson sculpted into the same mesh: catch, wall kick, shelf, basin.
            core=Shelf(core,x,z,Center(-44f),-44f,7f,4.2f,BaseHeight(-44f)+.6f);
            float cliffX=Center(-28f)-5f;
            core+=15f*Bell(x,cliffX,1.8f)*Bell(z,-27f,10f);
            Vector3 shelf=WallPracticeShelf;
            core=Shelf(core,x,z,shelf.x,shelf.z,4.5f,4.5f,shelf.y);
            core+=9f*Bell(x,shelf.x+6f,2f)*Bell(z,-9f,7f);
            core-=3.5f*Bell(x,shelf.x,12f)*Bell(z,-4f,6f);
            float sides=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(65f,HalfWidth,Mathf.Abs(x)));
            float back=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(StartZ,-92f,z));
            return Mathf.Lerp(-12f,core,sides*back);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MovementTestScene")return;
            // Disable the previous mountain together with its trees, gates, bumpers and finish arch.
            var previous=GameObject.Find("WILD MOUNTAIN — branching descent");
            if(previous)previous.SetActive(false);
            foreach(var bumper in Object.FindObjectsByType<MountainBumper>(FindObjectsSortMode.None))bumper.gameObject.SetActive(false);
            foreach(var finish in Object.FindObjectsByType<MountainFinish>(FindObjectsSortMode.None))finish.gameObject.SetActive(false);
            var root=new GameObject("WILD SLOPE — 330 metre descent");
            var art=new MountainArt(root);
            const int columns=192,rows=336;
            var vertices=new List<Vector3>((columns+1)*(rows+1));
            var triangles=new List<int>(columns*rows*6);
            for(int row=0;row<=rows;row++)
            {
                float z=Mathf.Lerp(StartZ,EndZ,(float)row/rows);
                for(int col=0;col<=columns;col++)
                {
                    float x=Mathf.Lerp(-HalfWidth,HalfWidth,(float)col/columns);
                    vertices.Add(new Vector3(x,HeightAt(x,z),z));
                }
            }
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                int a=row*(columns+1)+col,b=a+columns+1;
                triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            // Rock sides give the playable landmass depth when the camera looks over its edge.
            for(int row=0;row<rows;row++)
            {
                foreach(int col in new[]{0,columns})
                {
                    Vector3 a=vertices[row*(columns+1)+col],b=vertices[(row+1)*(columns+1)+col];
                    Vector3 c=new Vector3(b.x,-35f,b.z),d=new Vector3(a.x,-35f,a.z);
                    if(col==0)MountainArt.Quad(vertices,triangles,a,d,c,b);
                    else MountainArt.Quad(vertices,triangles,a,b,c,d);
                }
            }
            Mesh mesh=art.MakeMesh("Sculpted steep slope",vertices,triangles);
            var surface=art.MeshPart(root.transform,"The mountain IS the level",mesh,Vector3.zero,art.Snow,true);
            surface.AddComponent<MountainSlopeSurface>();
            root.AddComponent<SlopeRun>();
            var marker=GameObject.Find("Goat Spawn");
            if(!marker)marker=new GameObject("Goat Spawn");
            marker.transform.position=Spawn;
            MountainAtmosphere.Build(root.transform,surface.GetComponent<MeshCollider>(),art);
            MountainAssetScenery.Build(root.transform,art);
            Physics.SyncTransforms();
        }
    }
    public sealed class MountainSlopeSurface : MonoBehaviour { }
}
