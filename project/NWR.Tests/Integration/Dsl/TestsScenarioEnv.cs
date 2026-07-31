using NWR.Game;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;
using NWR.ScenarioDsl;

namespace NWR.Tests.Integration.Dsl
{
    public sealed class TestsScenarioEnv : IScenarioEnv
    {
        public static readonly TestsScenarioEnv Instance = new TestsScenarioEnv();

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
