using System;
using System.IO;
using NWR.ScenarioDsl;
using NWR.Tests.Integration.Dsl;

namespace NWR.Tests.Integration.Scenarios
{
    public static class DslScenarios
    {
        public static void WaitTurns(string repoRoot)
        {
            Scenario.Create("wait-turns")
                .LoadFixture("slot8")
                .Capture("turn")
                .WaitTurns(5)
                .Check("turnAdvanced")
                .Run(TestsScenarioEnv.Instance, repoRoot);
        }

        public static void ItemUsePotion(string repoRoot)
        {
            Scenario.Create("item-use-potion")
                .Param("item", "Potion_Curing")
                .LoadFixture("slot8")
                .HalfHp()
                .Capture("hp")
                .SpawnItem("${item}")
                .UseItem()
                .Check("hpIncreased")
                .Run(TestsScenarioEnv.Instance, repoRoot);
        }

        public static void EffectApplyProwling(string repoRoot)
        {
            Scenario.Create("effect-apply-prowling")
                .LoadFixture("slot8")
                .ApplyEffect("Prowling")
                .Check("effectPresent", "Prowling")
                .Run(TestsScenarioEnv.Instance, repoRoot);
        }

        /// <summary>
        /// Runs every *.json under dev_info/fixtures/scenarios/.
        /// </summary>
        public static void RunJsonFixtures(string repoRoot)
        {
            string dir = Path.Combine(repoRoot, "dev_info", "fixtures", "scenarios");
            if (!Directory.Exists(dir)) {
                throw new InvalidOperationException("missing scenarios dir: " + dir);
            }
            string[] files = Directory.GetFiles(dir, "*.json");
            if (files.Length == 0) {
                throw new InvalidOperationException("no scenario JSON in " + dir);
            }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++) {
                Scenario scenario = Scenario.FromFile(files[i]);
                scenario.Run(TestsScenarioEnv.Instance, repoRoot);
                Console.WriteLine("  OK  dsl-json:" + Path.GetFileNameWithoutExtension(files[i]));
            }
        }
    }
}
