using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GoatDescent.Harness
{
    public static class HarnessEvaluator
    {
        private const float DefaultTolerance = .35f;

        public static ScenarioResult Build(AgentScenario scenario, ScenarioRunner runner, float repeatDelta, bool repeated)
        {
            CheckResult[] checks = Evaluate(scenario, runner.StartSnapshot, runner.FinishSnapshot, runner.Samples);
            bool checksOk = true;
            foreach (CheckResult check in checks)
                if (!check.passed) checksOk = false;
            bool repeatOk = !scenario.repeat || !repeated || repeatDelta <= Tolerance(scenario);
            return new ScenarioResult
            {
                id = scenario.id,
                seed = scenario.seed,
                passed = runner.Error == null && checksOk && repeatOk,
                repeatable = repeatOk,
                repeatDelta = repeatDelta,
                duration = runner.StartSnapshot != null && runner.FinishSnapshot != null
                    ? runner.FinishSnapshot.time - runner.StartSnapshot.time : 0f,
                error = runner.Error,
                start = runner.StartSnapshot,
                finish = runner.FinishSnapshot,
                checks = checks
            };
        }

        public static float Tolerance(AgentScenario scenario) => scenario.tolerance > 0f ? scenario.tolerance : DefaultTolerance;

        public static float Delta(GameSnapshot a, GameSnapshot b)
        {
            return Mathf.Max(GoatDelta(a?.primary, b?.primary), GoatDelta(a?.secondary, b?.secondary));
        }

        private static float GoatDelta(GoatSnapshot a, GoatSnapshot b)
        {
            if (a == null || b == null) return 0f;
            return Vector3.Distance(a.position, b.position);
        }

        public static CheckResult[] Evaluate(AgentScenario scenario, GameSnapshot start, GameSnapshot finish,
            IReadOnlyList<GameSnapshot> samples)
        {
            var results = new List<CheckResult>();
            if (scenario == null || scenario.checks == null) return results.ToArray();
            foreach (HarnessCheck check in scenario.checks)
                results.Add(Evaluate(check, start, finish, samples));
            return results.ToArray();
        }

        private static CheckResult Evaluate(HarnessCheck check, GameSnapshot start, GameSnapshot finish,
            IReadOnlyList<GameSnapshot> samples)
        {
            string op = string.IsNullOrEmpty(check.op) ? ">=" : check.op;
            float actual;
            try
            {
                actual = Metric(check.type, check.target, start, finish, samples);
            }
            catch (Exception exception)
            {
                return new CheckResult { check = Describe(check, op), passed = false, detail = exception.Message };
            }
            return new CheckResult
            {
                check = Describe(check, op),
                passed = Compare(op, actual, check.value),
                detail = "actual=" + actual.ToString("F3", CultureInfo.InvariantCulture) + " " + op + " "
                    + check.value.ToString("F3", CultureInfo.InvariantCulture)
            };
        }

        private static string Describe(HarnessCheck check, string op)
        {
            string target = string.IsNullOrEmpty(check.target) ? "" : "[" + check.target + "]";
            return check.type + target + " " + op + " " + check.value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private static float Metric(string type, string target, GameSnapshot start, GameSnapshot finish,
            IReadOnlyList<GameSnapshot> samples)
        {
            switch (type)
            {
                case "alive": return GoatAt(finish, target).dead ? 0f : 1f;
                case "dead": return GoatAt(finish, target).dead ? 1f : 0f;
                case "grounded": return GoatAt(finish, target).grounded ? 1f : 0f;
                case "gripping": return GoatAt(finish, target).gripping ? 1f : 0f;
                case "linked": return GoatAt(finish, target).linked ? 1f : 0f;
                case "completed": return finish.completed ? 1f : 0f;
                case "progress": return GoatAt(finish, target).progress;
                case "progress_gain": return GoatAt(finish, target).progress - GoatAt(start, target).progress;
                case "speed": return GoatAt(finish, target).speed;
                case "slope": return GoatAt(finish, target).slope;
                case "hooves": return GoatAt(finish, target).hooves;
                case "position_y": return GoatAt(finish, target).position.y;
                case "falls": return finish.falls;
                case "distance": return Vector3.Distance(GoatAt(start, target).position, GoatAt(finish, target).position);
                case "airborne": return AnySample(samples, target, sample => !sample.grounded) ? 1f : 0f;
                case "fell": return AnySample(samples, target, sample => sample.dead) ? 1f : 0f;
                case "slipped": return AnySample(samples, target, sample => sample.slipping) ? 1f : 0f;
                case "max_speed": return MaxSpeed(samples, target);
                case "samples": return samples == null ? 0f : samples.Count;
                default: throw new InvalidOperationException("Неизвестная проверка: " + type);
            }
        }

        private static bool AnySample(IReadOnlyList<GameSnapshot> samples, string target, Func<GoatSnapshot, bool> predicate)
        {
            if (samples == null) return false;
            foreach (GameSnapshot sample in samples)
            {
                GoatSnapshot goat = Pick(sample, target);
                if (goat != null && predicate(goat)) return true;
            }
            return false;
        }

        private static float MaxSpeed(IReadOnlyList<GameSnapshot> samples, string target)
        {
            float maximum = 0f;
            if (samples == null) return maximum;
            foreach (GameSnapshot sample in samples)
            {
                GoatSnapshot goat = Pick(sample, target);
                if (goat != null) maximum = Mathf.Max(maximum, goat.speed);
            }
            return maximum;
        }

        private static GoatSnapshot GoatAt(GameSnapshot snapshot, string target)
        {
            if (snapshot == null) throw new InvalidOperationException("Нет снимка состояния");
            GoatSnapshot goat = Pick(snapshot, target);
            if (goat == null) throw new InvalidOperationException("Нет козла " + (string.IsNullOrEmpty(target) ? "a" : target));
            return goat;
        }

        private static GoatSnapshot Pick(GameSnapshot snapshot, string target)
        {
            if (snapshot == null) return null;
            return string.Equals(target, "b", StringComparison.OrdinalIgnoreCase) ? snapshot.secondary : snapshot.primary;
        }

        private static bool Compare(string op, float actual, float expected)
        {
            switch (op)
            {
                case ">": return actual > expected;
                case ">=": return actual >= expected;
                case "<": return actual < expected;
                case "<=": return actual <= expected;
                case "==": return Mathf.Abs(actual - expected) < .0001f;
                case "!=": return Mathf.Abs(actual - expected) >= .0001f;
                default: throw new InvalidOperationException("Неизвестный оператор: " + op);
            }
        }
    }
}
