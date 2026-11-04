using GoatDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace GoatDescent.Tests
{
    public sealed class DescentCourseTests
    {
        [Test]
        public void EveryTransitionDescendsAndEveryRockIsSeparated()
        {
            var rocks=DescentCourseLayout.Create();
            Assert.AreEqual(97,rocks.Length);
            for(int i=1;i<rocks.Length;i++)
            {
                Assert.Less(rocks[i].Center.y,rocks[i-1].Center.y);
                float span=Vector3.ProjectOnPlane(rocks[i].Center-rocks[i-1].Center,Vector3.up).magnitude;
                Assert.Greater(span,rocks[i].Radius+rocks[i-1].Radius+1);
                var from=DescentCourseLayout.Takeoff(rocks[i-1],rocks[i]);
                var to=DescentCourseLayout.Landing(rocks[i-1],rocks[i]);
                Assert.That(from.y-to.y,Is.InRange(2f,4.6f));
                float impact=Mathf.Sqrt(6.3f*6.3f+2*9.81f*(from.y-to.y));
                Assert.Less(impact,11.5f);
            }
            Assert.Greater(rocks[0].Center.y-rocks[96].Center.y,290);
        }
        [Test]
        public void DifficultSequencesHaveRegularSafeRests()
        {
            var rocks=DescentCourseLayout.Create();int hard=0,rests=0,slabs=0;
            for(int i=0;i<rocks.Length;i++)
            {
                if(rocks[i].Difficult)hard++;
                if(rocks[i].Rest){rests++;Assert.GreaterOrEqual(rocks[i].Radius,5f);}
                if(rocks[i].Tilt!=Vector3.zero)slabs++;
            }
            Assert.AreEqual(24,hard);Assert.AreEqual(13,rests);Assert.AreEqual(12,slabs);
        }
    }
}
