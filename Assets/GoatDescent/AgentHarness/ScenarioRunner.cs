using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.Harness
{
    public sealed class ScenarioRunner
    {
        private const float MinimumStep = .06f;
        private const float SampleInterval = .1f;
        private const float DefaultTimeout = 45f;

        private readonly List<GameSnapshot> samples = new List<GameSnapshot>();
        private IAgentGame game;
        private AgentScenario scenario;
        private int index = -1;
        private int tick;
        private float stepUntil;
        private float nextSample;
        private float startedAt;
        private string error;

        public bool Finished { get; private set; }
        public string Error => error;
        public AgentScenario Scenario => scenario;
        public GameSnapshot StartSnapshot { get; private set; }
        public GameSnapshot FinishSnapshot { get; private set; }
        public IReadOnlyList<GameSnapshot> Samples => samples;

        public void Begin(IAgentGame target, AgentScenario definition, float now)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            game = target;
            scenario = definition;
            index = -1;
            tick = 0;
            error = null;
            Finished = false;
            samples.Clear();
            startedAt = now;
            nextSample = now;
            game.Reset(definition.id, definition.seed);
            StartSnapshot = game.Snapshot(tick);
            samples.Add(StartSnapshot);
            stepUntil = 0f;
        }

        public void Tick(float now)
        {
            if (Finished) return;
            if (now - startedAt > (scenario.timeout > 0f ? scenario.timeout : DefaultTimeout))
            {
                error = "Таймаут сценария";
                Complete();
                return;
            }
            if (now < stepUntil)
            {
                if (now >= nextSample)
                {
                    nextSample = now + SampleInterval;
                    samples.Add(game.Snapshot(++tick));
                }
                return;
            }
            index++;
            if (scenario.commands == null || index >= scenario.commands.Length)
            {
                Complete();
                return;
            }
            try
            {
                game.Execute(scenario.commands[index]);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Complete();
                return;
            }
            stepUntil = now + Mathf.Max(MinimumStep, scenario.commands[index].seconds);
        }

        private void Complete()
        {
            FinishSnapshot = game.Snapshot(++tick);
            samples.Add(FinishSnapshot);
            Finished = true;
        }
    }
}
