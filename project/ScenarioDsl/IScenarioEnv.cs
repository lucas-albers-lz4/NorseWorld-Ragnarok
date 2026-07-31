using NWR.Game;

namespace NWR.ScenarioDsl
{
    /// <summary>
    /// Host adapter so the same DSL runs from NWR.Tests and NWR.Harness.
    /// </summary>
    public interface IScenarioEnv
    {
        int TestSlot { get; }
        string LogPath { get; }

        NWGameSpace Bootstrap(string repoRoot);
        void CopyFixtureToSlot(string repoRoot, string fixtureName, int slot);
        void RequireLogMarkers(params string[] markers);
    }
}
