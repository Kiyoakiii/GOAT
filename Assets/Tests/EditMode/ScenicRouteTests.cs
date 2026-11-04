using GoatDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace GoatDescent.Tests
{
    public sealed class ScenicRouteTests
    {
        [Test]
        public void RampOverCliffKeepsEverySampleAboveTerrainAndBelowGradeLimit()
        {
            var terrain = new[] { Vector3.zero, new Vector3(5,0,0), new Vector3(10,20,0), new Vector3(15,20,0), new Vector3(20,0,0) };
            var route = ScenicRouteMath.RaiseToGrade(terrain, 32f);
            for (int i = 0; i < route.Length; i++)
            {
                Assert.GreaterOrEqual(route[i].y, terrain[i].y);
                Assert.AreEqual(terrain[i].x, route[i].x);
                if (i > 0) Assert.LessOrEqual(ScenicRouteMath.Grade(route[i-1],route[i]),32.001f);
            }
            Assert.AreEqual(0f, terrain[0].y);
        }

        [Test]
        public void FlatTrailDoesNotMoveAndDuplicatePositionsRemainFinite()
        {
            var route = ScenicRouteMath.RaiseToGrade(new[] { Vector3.zero, Vector3.zero, new Vector3(10,0,0) },32f);
            foreach (var point in route) Assert.AreEqual(0f,point.y);
        }

        [TestCase("GoatDescent/ScenicRock")]
        [TestCase("GoatDescent/TerracePaintedRock")]
        public void ScenicRockShaderIsAvailableWithoutCompilerErrors(string name)
        {
            var shader = Shader.Find(name);
            Assert.IsNotNull(shader);
            Assert.IsTrue(shader.isSupported);
            Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(shader));
        }

        [Test]
        public void InvalidRouteIsRejected()
        {
            Assert.Throws<System.ArgumentException>(() => ScenicRouteMath.RaiseToGrade(new Vector3[0],32f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ScenicRouteMath.RaiseToGrade(new[] { Vector3.zero,Vector3.one },90f));
        }
    }
}
