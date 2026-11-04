using System;
using System.Collections.Generic;
using GoatDescent.Harness;
using NUnit.Framework;
using UnityEngine;

public class HarnessScenarioTests
{
    private sealed class FakeGame : IAgentGame
    {
        public readonly List<GameCommand> Executed = new List<GameCommand>();
        public readonly List<string> Resets = new List<string>();
        public string ThrowOn;

        public bool Ready => true;
        public string MapSignature => "fake:1";

        public void Reset(string scenarioId, int seed) => Resets.Add(scenarioId + ":" + seed);

        public void Execute(GameCommand command)
        {
            if (command.type == ThrowOn) throw new InvalidOperationException("boom");
            Executed.Add(command);
        }

        public GameSnapshot Snapshot(int tick) => new GameSnapshot
        {
            tick = tick,
            seed = 7,
            scenario = "fake",
            map = MapSignature,
            primary = new GoatSnapshot
            {
                label = "A",
                position = new Vector3(Executed.Count, 0f, 0f),
                grounded = true
            }
        };
    }

    private static AgentScenario Scenario(params string[] types)
    {
        var scenario = new AgentScenario { id = "fake", seed = 7, commands = new GameCommand[types.Length] };
        for (int i = 0; i < types.Length; i++)
            scenario.commands[i] = new GameCommand { type = types[i], seconds = .2f };
        return scenario;
    }

    private static void Drain(ScenarioRunner runner, float step = .05f, int limit = 400)
    {
        float now = 0f;
        for (int i = 0; i < limit && !runner.Finished; i++)
        {
            runner.Tick(now);
            now += step;
        }
    }

    [Test]
    public void ScenarioJsonRoundTrips()
    {
        var source = new AgentScenarioSet
        {
            scene = "Assets/Scenes/GoatPlayground.unity",
            scenarios = new[]
            {
                new AgentScenario
                {
                    id = "walk", seed = 42, repeat = true, tolerance = .25f,
                    commands = new[] { new GameCommand { type = "walk", x = 0f, z = 1f, seconds = .4f } },
                    checks = new[] { new HarnessCheck { type = "progress", op = ">=", value = .1f } }
                }
            }
        };
        AgentScenarioSet restored = JsonUtility.FromJson<AgentScenarioSet>(JsonUtility.ToJson(source));
        Assert.AreEqual("walk", restored.scenarios[0].id);
        Assert.AreEqual(42, restored.scenarios[0].seed);
        Assert.IsTrue(restored.scenarios[0].repeat);
        Assert.AreEqual(.25f, restored.scenarios[0].tolerance, .0001f);
        Assert.AreEqual("walk", restored.scenarios[0].commands[0].type);
        Assert.AreEqual(1f, restored.scenarios[0].commands[0].z, .0001f);
        Assert.AreEqual("progress", restored.scenarios[0].checks[0].type);
        Assert.AreEqual(.1f, restored.scenarios[0].checks[0].value, .0001f);
    }

    [Test]
    public void RunnerResetsThenExecutesEveryCommandInOrder()
    {
        var game = new FakeGame();
        var runner = new ScenarioRunner();
        runner.Begin(game, Scenario("walk", "jump", "stop"), 0f);
        Drain(runner);

        Assert.IsTrue(runner.Finished);
        Assert.IsNull(runner.Error);
        CollectionAssert.AreEqual(new[] { "fake:7" }, game.Resets);
        CollectionAssert.AreEqual(new[] { "walk", "jump", "stop" },
            game.Executed.ConvertAll(command => command.type));
    }

    [Test]
    public void RunnerStopsAndReportsWhenCommandThrows()
    {
        var game = new FakeGame { ThrowOn = "jump" };
        var runner = new ScenarioRunner();
        runner.Begin(game, Scenario("walk", "jump", "stop"), 0f);
        Drain(runner);

        Assert.IsTrue(runner.Finished);
        Assert.AreEqual("boom", runner.Error);
        CollectionAssert.AreEqual(new[] { "walk" }, game.Executed.ConvertAll(command => command.type));
    }

    [Test]
    public void RunnerTimesOutWhenCommandNeverCompletes()
    {
        var game = new FakeGame();
        var scenario = Scenario("walk");
        scenario.timeout = 5f;
        scenario.commands[0].seconds = 100f;
        var runner = new ScenarioRunner();
        runner.Begin(game, scenario, 0f);
        Drain(runner, 1f, 20);

        Assert.IsTrue(runner.Finished);
        Assert.AreEqual("Таймаут сценария", runner.Error);
    }

    [Test]
    public void ChecksEvaluateAgainstFinishSnapshot()
    {
        var scenario = new AgentScenario
        {
            id = "checks",
            checks = new[]
            {
                new HarnessCheck { type = "alive", op = ">=", value = 1f },
                new HarnessCheck { type = "progress", op = ">=", value = .5f },
                new HarnessCheck { type = "fell", op = "==", value = 0f },
                new HarnessCheck { type = "airborne", op = ">=", value = 1f }
            }
        };
        var start = Snapshot(progress: .1f, dead: false, grounded: true);
        var finish = Snapshot(progress: .6f, dead: false, grounded: true);
        var samples = new List<GameSnapshot> { start, Snapshot(.6f, false, false), finish };

        CheckResult[] results = HarnessEvaluator.Evaluate(scenario, start, finish, samples);

        Assert.AreEqual(4, results.Length);
        foreach (CheckResult result in results)
            Assert.IsTrue(result.passed, result.check + " → " + result.detail);
    }

    [Test]
    public void FailingCheckIsReported()
    {
        var scenario = new AgentScenario
        {
            id = "checks",
            checks = new[] { new HarnessCheck { type = "progress", op = ">=", value = .9f } }
        };
        var snapshot = Snapshot(progress: .2f, dead: false, grounded: true);
        CheckResult[] results = HarnessEvaluator.Evaluate(scenario, snapshot, snapshot,
            new List<GameSnapshot> { snapshot });

        Assert.AreEqual(1, results.Length);
        Assert.IsFalse(results[0].passed);
        StringAssert.Contains("actual=0.200", results[0].detail);
    }

    [Test]
    public void UnknownCheckTypeIsNotSilentlyPassed()
    {
        var scenario = new AgentScenario
        {
            id = "checks",
            checks = new[] { new HarnessCheck { type = "nonsense", op = ">=", value = 1f } }
        };
        var snapshot = Snapshot(progress: .2f, dead: false, grounded: true);
        CheckResult[] results = HarnessEvaluator.Evaluate(scenario, snapshot, snapshot,
            new List<GameSnapshot> { snapshot });

        Assert.IsFalse(results[0].passed);
    }

    [Test]
    public void DeltaMeasuresReproducibilityDrift()
    {
        var a = Snapshot(progress: 0f, dead: false, grounded: true);
        var b = Snapshot(progress: 0f, dead: false, grounded: true);
        b.primary.position = a.primary.position + new Vector3(2f, 0f, 0f);
        b.secondary = new GoatSnapshot { label = "B", position = new Vector3(0f, 5f, 0f) };
        a.secondary = new GoatSnapshot { label = "B", position = Vector3.zero };

        Assert.AreEqual(5f, HarnessEvaluator.Delta(a, b), .0001f);
    }

    [Test]
    public void BuildMarksRunAsFailedWhenRepeatDriftsBeyondTolerance()
    {
        var scenario = new AgentScenario { id = "repeat", seed = 7, repeat = true, tolerance = .25f };
        var game = new FakeGame();
        var runner = new ScenarioRunner();
        runner.Begin(game, scenario, 0f);
        Drain(runner);

        ScenarioResult failing = HarnessEvaluator.Build(scenario, runner, 1.5f, true);
        Assert.IsFalse(failing.passed);
        Assert.IsFalse(failing.repeatable);
        ScenarioResult passing = HarnessEvaluator.Build(scenario, runner, .1f, true);
        Assert.IsTrue(passing.passed);
        Assert.IsTrue(passing.repeatable);
    }

    private static GameSnapshot Snapshot(float progress, bool dead, bool grounded)
    {
        var goat = new GoatSnapshot
        {
            label = "A",
            position = new Vector3(progress, 0f, 0f),
            grounded = grounded,
            dead = dead,
            progress = progress
        };
        return new GameSnapshot
        {
            tick = 0,
            seed = 7,
            scenario = "checks",
            map = "fake:1",
            primary = goat,
            secondary = new GoatSnapshot { label = "B" }
        };
    }
}
