using System;
using System.Collections.Generic;
using System.IO;

namespace NWR.ScenarioDsl
{
    /// <summary>
    /// Fluent scenario builder. Steps are JSON-serializable (except Check lambdas).
    /// </summary>
    public sealed class Scenario
    {
        public string Name { get; private set; }
        public List<ScenarioStep> Steps { get; private set; }

        private Scenario(string name)
        {
            Name = name ?? "unnamed";
            Steps = new List<ScenarioStep>();
        }

        public static Scenario Create(string name)
        {
            return new Scenario(name);
        }

        public Scenario Param(string key, string value)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.Param, Arg = key, CheckFailMessage = value });
            return this;
        }

        public Scenario LoadFixture(string fixtureName)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.LoadFixture, Arg = fixtureName });
            return this;
        }

        public Scenario SpawnItem(string sign, int count = 1, bool identified = true)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.SpawnItem, Arg = sign, N = count, Flag = identified });
            return this;
        }

        public Scenario SpawnCreature(string signOrId)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.SpawnCreature, Arg = signOrId });
            return this;
        }

        public Scenario UseItem()
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.UseItem });
            return this;
        }

        public Scenario ApplyEffect(string effectIdName)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.ApplyEffect, Arg = effectIdName });
            return this;
        }

        public Scenario WaitTurns(int turns)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.WaitTurns, N = turns });
            return this;
        }

        public Scenario SaveGame()
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.SaveGame });
            return this;
        }

        public Scenario LoadGame()
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.LoadGame });
            return this;
        }

        public Scenario SaveLoadRoundtrip()
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.SaveLoadRoundtrip });
            return this;
        }

        public Scenario Capture(string key)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.Capture, Arg = key });
            return this;
        }

        public Scenario HalfHp()
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.HalfHp });
            return this;
        }

        /// <summary>
        /// Named predicate (JSON-safe): turnAdvanced, hpIncreased, effectPresent, effectAbsent, effectsUnchanged.
        /// </summary>
        public Scenario Check(string namedCheck, string arg = null)
        {
            Steps.Add(new ScenarioStep { Op = ScenarioOp.Check, Arg = namedCheck, CheckFailMessage = arg });
            return this;
        }

        /// <summary>
        /// C#-only predicate; omitted from ToJson.
        /// </summary>
        public Scenario Check(Func<ScenarioContext, bool> predicate, string failMessage = null)
        {
            Steps.Add(new ScenarioStep {
                Op = ScenarioOp.Check,
                CheckFunc = predicate,
                CheckFailMessage = failMessage ?? "check failed"
            });
            return this;
        }

        public Scenario RequireLog(params string[] markers)
        {
            string joined = markers != null ? string.Join("|", markers) : "";
            Steps.Add(new ScenarioStep { Op = ScenarioOp.RequireLog, Arg = joined });
            return this;
        }

        public void Run(IScenarioEnv env, string repoRoot)
        {
            ScenarioExecutor.Run(this, env, repoRoot);
        }

        public string ToJson()
        {
            return ScenarioJson.ToJson(this);
        }

        public static Scenario FromJson(string json)
        {
            return ScenarioJson.FromJson(json);
        }

        public static Scenario FromFile(string path)
        {
            return FromJson(File.ReadAllText(path));
        }

        public Scenario Clone()
        {
            var copy = new Scenario(Name);
            for (int i = 0; i < Steps.Count; i++) {
                copy.Steps.Add(Steps[i].Clone());
            }
            return copy;
        }
    }
}
