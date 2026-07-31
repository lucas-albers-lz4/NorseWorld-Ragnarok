using System;
using System.IO;
using NUnit.Framework;
using NWR.ScenarioDsl;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Dsl
{
    [TestFixture]
    public class ScenarioDslTests
    {
        [Test]
        public void JsonRoundtrip_PreservesNamedSteps()
        {
            Scenario original = Scenario.Create("item-use-potion")
                .Param("item", "Potion_Curing")
                .LoadFixture("slot8")
                .HalfHp()
                .Capture("hp")
                .SpawnItem("${item}")
                .UseItem()
                .Check("hpIncreased");

            string json = original.ToJson();
            Scenario restored = Scenario.FromJson(json);

            Assert.AreEqual(original.Name, restored.Name);
            Assert.AreEqual(original.Steps.Count, restored.Steps.Count);
            for (int i = 0; i < original.Steps.Count; i++) {
                Assert.AreEqual(original.Steps[i].Op, restored.Steps[i].Op);
                Assert.AreEqual(original.Steps[i].Arg, restored.Steps[i].Arg);
                Assert.AreEqual(original.Steps[i].N, restored.Steps[i].N);
            }
        }

        [Test]
        public void FromFile_WaitTurnsFixture_Parses()
        {
            string path = Path.Combine(FindRepoRoot(), "dev_info", "fixtures", "scenarios", "wait-turns.json");
            Scenario s = Scenario.FromFile(path);
            Assert.AreEqual("wait-turns", s.Name);
            Assert.AreEqual(4, s.Steps.Count);
            Assert.AreEqual(ScenarioOp.LoadFixture, s.Steps[0].Op);
            Assert.AreEqual(ScenarioOp.Check, s.Steps[3].Op);
        }

        [Test]
        public void Fluent_WaitTurns_Runs()
        {
            DslScenarios.WaitTurns(FindRepoRoot());
        }

        private static string FindRepoRoot()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8; i++) {
                if (File.Exists(Path.Combine(dir, "play-cs.sh"))) {
                    return dir;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
