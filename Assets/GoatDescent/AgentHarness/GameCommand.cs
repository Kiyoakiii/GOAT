using System;

namespace GoatDescent.Harness
{
    [Serializable]
    public class GameCommand
    {
        public string type;
        public string target;
        public float x;
        public float y;
        public float z;
        public float value;
        public float seconds;
    }

    [Serializable]
    public class HarnessCheck
    {
        public string type;
        public string target;
        public string op;
        public float value;
    }

    [Serializable]
    public class AgentScenario
    {
        public string id;
        public int seed;
        public float timeout;
        public float tolerance;
        public bool repeat;
        public GameCommand[] commands;
        public HarnessCheck[] checks;
    }

    [Serializable]
    public class AgentScenarioSet
    {
        public string scene;
        public AgentScenario[] scenarios;
    }
}
