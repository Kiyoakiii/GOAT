using System;
using UnityEngine;

namespace GoatDescent.Levels
{
    public static class ScenicRouteMath
    {
        public static Vector3[] RaiseToGrade(Vector3[] source, float degrees)
        {
            if (source == null || source.Length < 2) throw new ArgumentException("A route needs at least two points.");
            if (degrees <= 0f || degrees >= 60f) throw new ArgumentOutOfRangeException(nameof(degrees));
            var result = (Vector3[])source.Clone();
            float grade = Mathf.Tan(degrees * Mathf.Deg2Rad);
            for (int i = 1; i < result.Length; i++)
                result[i].y = Mathf.Max(result[i].y, result[i - 1].y - Horizontal(result[i], result[i - 1]) * grade);
            for (int i = result.Length - 2; i >= 0; i--)
                result[i].y = Mathf.Max(result[i].y, result[i + 1].y - Horizontal(result[i], result[i + 1]) * grade);
            return result;
        }

        public static float Horizontal(Vector3 a, Vector3 b)
            => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        public static float Grade(Vector3 a, Vector3 b)
            => Mathf.Atan2(Mathf.Abs(a.y - b.y), Mathf.Max(.001f, Horizontal(a, b))) * Mathf.Rad2Deg;
    }
}
