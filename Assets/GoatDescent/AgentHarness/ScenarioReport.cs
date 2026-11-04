using System;

namespace GoatDescent.Harness
{
    [Serializable]
    public class CheckResult
    {
        public string check;
        public bool passed;
        public string detail;
    }

    [Serializable]
    public class ScenarioResult
    {
        public string id;
        public int seed;
        public bool passed;
        public bool repeatable;
        public float repeatDelta;
        public float duration;
        public string error;
        public GameSnapshot start;
        public GameSnapshot finish;
        public CheckResult[] checks;
    }

    [Serializable]
    public class HarnessReport
    {
        public string scene;
        public string map;
        public float seconds;
        public int scenarios;
        public int passed;
        public int failed;
        public bool ok;
        public ScenarioResult[] results;
    }
}
