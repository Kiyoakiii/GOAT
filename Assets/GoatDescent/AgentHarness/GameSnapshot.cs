using System;
using UnityEngine;

namespace GoatDescent.Harness
{
    [Serializable]
    public class GoatSnapshot
    {
        public string label;
        public Vector3 position;
        public Vector3 velocity;
        public float speed;
        public bool grounded;
        public bool dead;
        public float slope;
        public float progress;
        public bool gripping;
        public bool linked;
        public bool slipping;
        public int hooves;
    }

    [Serializable]
    public class GameSnapshot
    {
        public int tick;
        public float time;
        public int seed;
        public string scenario;
        public string map;
        public GoatSnapshot primary;
        public GoatSnapshot secondary;
        public bool completed;
        public int falls;
        public float elapsed;
        public int platforms;
        public string error;
    }
}
