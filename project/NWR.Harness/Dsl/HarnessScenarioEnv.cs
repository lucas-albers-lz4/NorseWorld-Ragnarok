using NWR.Game;
using NWR.Harness.Scenarios;
using NWR.ScenarioDsl;

namespace NWR.Harness.Dsl
{
    public sealed class HarnessScenarioEnv : IScenarioEnv
    {
        public static readonly HarnessScenarioEnv Instance = new HarnessScenarioEnv();

        public int TestSlot
        {
            get { return SaveLoadScenarios.TestSlot; }
        }

        public string LogPath
        {
            get { return HarnessBootstrap.LogPath; }
        }

        public NWGameSpace Bootstrap(string repoRoot)
        {
            return HarnessBootstrap.Init(repoRoot);
        }

        public void CopyFixtureToSlot(string repoRoot, string fixtureName, int slot)
        {
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, fixtureName, slot);
        }

        public void RequireLogMarkers(params string[] markers)
        {
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, markers);
        }
    }
}
