using System;
using System.IO;
using NWR.Creatures;
using NWR.Game;
using NWR.Tests.Integration;

namespace NWR.Tests.Integration.Scenarios
{
    public static class SaveLoadScenarios
    {
        public const int TestSlot = 8;

        public static string FixtureDir(string repoRoot, string name)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "save", name);
        }

        public static void CopyFixtureToSlot(string repoRoot, string fixtureName, int slot)
        {
            string src = FixtureDir(repoRoot, fixtureName);
            string saveDir = Path.Combine(repoRoot, "save");
            Directory.CreateDirectory(saveDir);
            foreach (string ext in new[] { "rgp", "rgt", "rgj" }) {
                string from = Path.Combine(src, "rgame_8." + ext);
                string to = Path.Combine(saveDir, "rgame_" + slot + "." + ext);
                if (!File.Exists(from)) {
                    throw new FileNotFoundException("Fixture missing: " + from);
                }
                File.Copy(from, to, true);
            }
        }

        public static void SaveLoadRoundtrip(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            CopyFixtureToSlot(repoRoot, "slot8", TestSlot);

            game.LoadGame(TestSlot);
            PlayerSnapshot before = PlayerSnapshot.Capture(game.Player);
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok", "terrainsLoad(): ok");

            game.SaveGame(TestSlot);
            game.LoadGame(TestSlot);
            PlayerSnapshot after = PlayerSnapshot.Capture(game.Player);
            before.AssertMatches(after, "save-load-roundtrip");
            LogAssert.RequireNoFailurePatterns(HarnessBootstrap.LogPath);
        }

        public static void SaveOverwrite(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            CopyFixtureToSlot(repoRoot, "slot8", TestSlot);
            game.LoadGame(TestSlot);
            game.SaveGame(TestSlot);
            game.SaveGame(TestSlot);
            LogAssert.RequireNoFailurePatterns(HarnessBootstrap.LogPath);
        }

        public static void SaveErase(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            CopyFixtureToSlot(repoRoot, "slot8", TestSlot);
            game.LoadGame(TestSlot);
            game.EraseGame(TestSlot);

            string saveDir = Path.Combine(repoRoot, "save");
            foreach (string ext in new[] { "rgp", "rgt", "rgj" }) {
                string path = Path.Combine(saveDir, "rgame_" + TestSlot + "." + ext);
                if (File.Exists(path)) {
                    throw new InvalidOperationException("File still exists after erase: " + path);
                }
            }
        }

        public static void PlayerMetadata(string repoRoot)
        {
            HarnessBootstrap.Init(repoRoot);
            CopyFixtureToSlot(repoRoot, "slot8", TestSlot);
            var preview = new Player(GlobalVars.nwrGame, null);
            NWGameSpace.LoadPlayer(TestSlot, preview);
            if (string.IsNullOrEmpty(preview.Name)) {
                throw new InvalidOperationException("LoadPlayer: empty player name");
            }
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok");
        }
    }
}
