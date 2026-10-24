using UnityEngine;
namespace GoatDescent
{
    /// <summary>Equipment follows simulated anatomy instead of the gameplay capsule.</summary>
    public static class GoatClimbingEquipment
    {
        public static Transform CreateAxe(Material metal,Material rubber)
        {
            var root=new GameObject("Hoof held curved ice axe").transform;
            Segment(root,"Curved upper shaft",new Vector3(0,-.1f,0),new Vector3(0,.23f,.025f),.025f,metal);
            Segment(root,"Bent neck",new Vector3(0,.23f,.025f),new Vector3(0,.4f,.11f),.027f,metal);
            Segment(root,"Rubber grip",new Vector3(0,-.13f,0),new Vector3(0,.08f,0),.045f,rubber);
            for(int i=0;i<6;i++) Segment(root,"Grip rib",new Vector3(-.043f,-.105f+i*.032f,0),new Vector3(.043f,-.105f+i*.032f,0),.006f,metal);
            var mesh=new Mesh { name="Goat forged ice axe head" };
            Vector3[] outline={new Vector3(-.09f,.42f,.11f),new Vector3(.11f,.45f,.11f),new Vector3(.42f,.3f,.11f),new Vector3(.14f,.35f,.11f),new Vector3(-.09f,.37f,.11f)};
            var vertices=new Vector3[10];
            for(int i=0;i<5;i++) { vertices[i]=outline[i]+Vector3.forward*.025f; vertices[i+5]=outline[i]-Vector3.forward*.025f; }
            int[] triangles={0,1,2,0,2,3,0,3,4,5,7,6,5,8,7,5,9,8,0,5,6,0,6,1,1,6,7,1,7,2,2,7,8,2,8,3,3,8,9,3,9,4,4,9,5,4,5,0};
            mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals();
            var head=new GameObject("Forged toothed pick"); head.transform.SetParent(root,false);
            head.AddComponent<MeshFilter>().sharedMesh=mesh; head.AddComponent<MeshRenderer>().sharedMaterial=metal;
            for(int i=0;i<4;i++) Segment(root,"Pick tooth",new Vector3(.12f+i*.045f,.35f-i*.02f,.11f),new Vector3(.12f+i*.045f,.325f-i*.02f,.11f),.009f,metal);
            return root;
        }
        public static void Segment(Transform parent,string name,Vector3 from,Vector3 to,float radius,Material material)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cylinder); part.name=name; part.transform.SetParent(parent,false);
            part.transform.localPosition=(from+to)*.5f; part.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
            part.transform.localScale=new Vector3(radius*2,(to-from).magnitude*.5f,radius*2);
            if(Application.isPlaying) Object.Destroy(part.GetComponent<Collider>());
            else Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial=material;
        }
        public static LineRenderer Line(Transform parent,string name,Material material,float width,Color color,int points)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent,false);
            line.sharedMaterial=material; line.useWorldSpace=true; line.positionCount=points; line.widthMultiplier=width;
            line.startColor=line.endColor=color; line.numCapVertices=5; line.numCornerVertices=4;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            return line;
        }
        public static void Loop(LineRenderer line,Vector3 center,Vector3 right,Vector3 up,float width,float height)
        {
            for(int i=0;i<line.positionCount;i++) { float angle=i*Mathf.PI*2/(line.positionCount-1);
                line.SetPosition(i,center+right*Mathf.Cos(angle)*width+up*Mathf.Sin(angle)*height); }
        }
    }
}
