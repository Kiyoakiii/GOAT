using UnityEngine;

namespace GoatDescent.Levels
{
    public readonly struct DescentRock
    {
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly bool Rest;
        public readonly bool Difficult;
        public readonly Vector3 Tilt;
        public DescentRock(Vector3 center, float radius, bool rest, bool difficult, Vector3 tilt)
        { Center=center;Radius=radius;Rest=rest;Difficult=difficult;Tilt=tilt; }
        public float Height(Vector3 point) => Center.y+Vector3.Dot(point-Center,Tilt);
    }

    public static class DescentCourseLayout
    {
        public const int JumpCount=96;
        public static DescentRock[] Create()
        {
            var rocks=new DescentRock[JumpCount+1];
            var center=new Vector3(0,328,0);
            for(int i=0;i<rocks.Length;i++)
            {
                bool rest=i%8==0;
                bool difficult=i%8==4 || i%8==5;
                float radius=rest?5.5f:difficult?2.45f:i%8==3?3f:3.6f;
                if(i>0)
                {
                    int section=(i-1)/8;
                    float lateral=section%2==0?1f:-1f;
                    float angle=.58f+.18f*Mathf.Sin(i*1.71f);
                    Vector3 direction=new Vector3(lateral*Mathf.Cos(angle),0,Mathf.Sin(angle));
                    float gap=difficult?3.15f:1.15f;
                    center+=direction*(rocks[i-1].Radius+radius+gap);
                    center.y-=difficult?4f:2.8f;
                }
                rocks[i]=new DescentRock(center,radius,rest,difficult, i%8==3 ? new Vector3(.09f,0,-.055f) : Vector3.zero);
            }
            return rocks;
        }
        public static Vector3 Takeoff(DescentRock from, DescentRock to)
        {
            Vector3 direction=Vector3.ProjectOnPlane(to.Center-from.Center,Vector3.up).normalized;
            var point=from.Center+direction*(from.Radius*.94f-.25f);point.y=from.Height(point);return point;
        }
        public static Vector3 Landing(DescentRock from, DescentRock to)
        {
            Vector3 direction=Vector3.ProjectOnPlane(to.Center-from.Center,Vector3.up).normalized;
            var point=to.Center-direction*(to.Radius*.94f-.25f);point.y=to.Height(point);return point;
        }
    }
}
