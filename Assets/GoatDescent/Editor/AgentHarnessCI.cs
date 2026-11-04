using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GoatDescent.Harness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class AgentHarnessCI
    {
        private const string Armed = "GOAT.AgentHarness.Armed";
        private const string ScenariosKey = "GOAT.AgentHarness.Scenarios";
        private const string SceneKey = "GOAT.AgentHarness.Scene";
        private const string OutputKey = "GOAT.AgentHarness.Output";
        private const string DefaultScene = "Assets/GoatDescent/Scenes/GoatPlayground.unity";
        private const string DefaultScenarios = "Assets/GoatDescent/AgentHarness/Scenarios/default.json";
        private const string DefaultOutput = "Library/AgentHarness/report.json";
        private const float PlayModeTimeout = 90f;
        private const float StartupTimeout = 120f;
        private const float TotalTimeout = 600f;

        private static GoatAgentAdapter adapter;
        private static AgentScenarioSet set;
        private static ScenarioRunner runner;
        private static List<ScenarioResult> results;
        private static GameSnapshot repeatBaseline;
        private static int index;
        private static bool started;
        private static bool waitingForScene;
        private static bool done;
        private static double armedAt;

        static AgentHarnessCI() => EditorApplication.update += Poll;

        public static void Run()
        {
            string scenarioPath = Env("GOAT_HARNESS_SCENARIOS", DefaultScenarios);
            string scenePath = Env("GOAT_HARNESS_SCENE", "");
            if (string.IsNullOrEmpty(scenePath))
            {
                try { scenePath = LoadSet(scenarioPath)?.scene; }
                catch (Exception exception) { Debug.LogWarning("HARNESS_WARN " + exception.Message); }
                if (string.IsNullOrEmpty(scenePath)) scenePath = DefaultScene;
            }
            SessionState.SetString(ScenariosKey, scenarioPath);
            SessionState.SetString(SceneKey, scenePath);
            SessionState.SetString(OutputKey, Env("GOAT_HARNESS_OUT", DefaultOutput));
            SessionState.SetBool(Armed, true);
            armedAt = EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Poll()
        {
            if (done || !SessionState.GetBool(Armed, false)) return;
            if (!started)
            {
                if (!EditorApplication.isPlaying)
                {
                    if (EditorApplication.timeSinceStartup - armedAt > PlayModeTimeout)
                        Fail("Редактор не вошёл в Play Mode за " + PlayModeTimeout + " с");
                    return;
                }
                started = true;
                waitingForScene = true;
                results = new List<ScenarioResult>();
                index = 0;
                if (!Init()) return;
                return;
            }
            if (set == null || set.scenarios == null || set.scenarios.Length == 0) return;
            if (waitingForScene)
            {
                AdoptAdapter();
                if (!adapter || !adapter.Ready)
                {
                    if (EditorApplication.timeSinceStartup - armedAt > StartupTimeout)
                        Fail("Сцена не собралась за " + StartupTimeout + " с");
                    return;
                }
                waitingForScene = false;
                StartScenario(set.scenarios[index]);
                return;
            }
            if (EditorApplication.timeSinceStartup - armedAt > TotalTimeout)
            {
                Fail("Общий таймаут прогона " + TotalTimeout + " с");
                return;
            }
            if (runner == null) return;
            runner.Tick(Time.time);
            if (!runner.Finished) return;
            if (runner.Scenario.repeat && repeatBaseline == null && runner.Error == null)
            {
                repeatBaseline = runner.FinishSnapshot;
                StartScenario(set.scenarios[index]);
                return;
            }
            float delta = repeatBaseline != null ? HarnessEvaluator.Delta(repeatBaseline, runner.FinishSnapshot) : 0f;
            results.Add(HarnessEvaluator.Build(runner.Scenario, runner, delta, repeatBaseline != null));
            repeatBaseline = null;
            index++;
            if (index >= set.scenarios.Length) { Complete(); return; }
            StartScenario(set.scenarios[index]);
        }

        private static void AdoptAdapter()
        {
            if (adapter) return;
            adapter = UnityEngine.Object.FindFirstObjectByType<GoatAgentAdapter>();
            if (!adapter) adapter = new GameObject("Agent Harness").AddComponent<GoatAgentAdapter>();
        }

        private static bool Init()
        {
            try
            {
                set = LoadSet(SessionState.GetString(ScenariosKey, DefaultScenarios));
            }
            catch (Exception exception)
            {
                Fail("Сценарии не загружены: " + exception.Message);
                return false;
            }
            if (set == null || set.scenarios == null || set.scenarios.Length == 0)
            {
                Fail("Пустой набор сценариев");
                return false;
            }
            return true;
        }

        private static void StartScenario(AgentScenario scenario)
        {
            runner = new ScenarioRunner();
            try
            {
                runner.Begin(adapter, scenario, Time.time);
            }
            catch (Exception exception)
            {
                results.Add(new ScenarioResult
                {
                    id = scenario.id, seed = scenario.seed, passed = false, repeatable = true,
                    error = exception.Message, checks = new CheckResult[0]
                });
                repeatBaseline = null;
                index++;
                if (index >= set.scenarios.Length) { Complete(); return; }
                StartScenario(set.scenarios[index]);
            }
        }

        private static void Complete()
        {
            if (done) return;
            done = true;
            int passed = CountPassed();
            int failed = results.Count - passed;
            var report = new HarnessReport
            {
                scene = SessionState.GetString(SceneKey, DefaultScene),
                map = adapter ? adapter.MapSignature : "",
                seconds = (float)(EditorApplication.timeSinceStartup - armedAt),
                scenarios = set.scenarios.Length,
                passed = passed,
                failed = failed,
                ok = failed == 0 && results.Count == set.scenarios.Length,
                results = results.ToArray()
            };
            foreach (ScenarioResult result in results)
                Debug.Log("HARNESS_SCENARIO id=" + result.id + " passed=" + result.passed + " repeatable=" + result.repeatable
                    + " delta=" + result.repeatDelta.ToString("F3", CultureInfo.InvariantCulture)
                    + " duration=" + result.duration.ToString("F2", CultureInfo.InvariantCulture)
                    + (string.IsNullOrEmpty(result.error) ? "" : " error=" + result.error));
            Write(report);
            Debug.Log("HARNESS_RESULT ok=" + report.ok + " passed=" + passed + " failed=" + failed
                + " scenarios=" + report.scenarios + " map=" + report.map + " report=" + AbsoluteOutput());
            SessionState.SetBool(Armed, false);
            EditorApplication.Exit(report.ok ? 0 : 1);
        }

        private static void Fail(string message)
        {
            if (done) return;
            done = true;
            Debug.LogError("HARNESS_ERROR " + message);
            var report = new HarnessReport
            {
                scene = SessionState.GetString(SceneKey, DefaultScene),
                map = adapter ? adapter.MapSignature : "",
                seconds = (float)(EditorApplication.timeSinceStartup - armedAt),
                scenarios = set != null && set.scenarios != null ? set.scenarios.Length : 0,
                passed = results != null ? CountPassed() : 0,
                failed = 1,
                ok = false,
                results = results != null ? results.ToArray() : new ScenarioResult[0]
            };
            Write(report);
            SessionState.SetBool(Armed, false);
            EditorApplication.Exit(1);
        }

        private static int CountPassed()
        {
            int passed = 0;
            if (results == null) return passed;
            foreach (ScenarioResult result in results)
                if (result.passed) passed++;
            return passed;
        }

        private static AgentScenarioSet LoadSet(string path)
        {
            string full = Absolute(path);
            if (!File.Exists(full)) throw new FileNotFoundException("Нет файла сценариев", full);
            return JsonUtility.FromJson<AgentScenarioSet>(File.ReadAllText(full));
        }

        private static void Write(HarnessReport report)
        {
            string full = AbsoluteOutput();
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(report, true));
        }

        private static string AbsoluteOutput() => Absolute(SessionState.GetString(OutputKey, DefaultOutput));

        private static string Absolute(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            string root = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(root, path));
        }

        private static string Env(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
