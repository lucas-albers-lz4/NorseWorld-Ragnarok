using System;
using System.IO;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;

namespace NWR.Harness.Scenarios
{
    public static class FixtureBuilder
    {
        /// <summary>
        /// Generates dev_info/fixtures/save/slot8 from a fresh headless new game (slow; run once).
        /// </summary>
        public static void BuildSlot8Fixture(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            GlobalVars.nwrHost.GameState = GameState.gsWorldGen;
            game.InitBegin();
            game.SelectHero("Viking", "HarnessHero");
            GlobalVars.nwrHost.GameState = GameState.gsDefault;
            game.InitEnd();

            int slot = SaveLoadScenarios.TestSlot;
            game.SaveGame(slot);

            string dst = SaveLoadScenarios.FixtureDir(repoRoot, "slot8");
            Directory.CreateDirectory(dst);
            string saveDir = Path.Combine(repoRoot, "save");
            foreach (string ext in new[] { "rgp", "rgt", "rgj" }) {
                string from = Path.Combine(saveDir, "rgame_" + slot + "." + ext);
                string to = Path.Combine(dst, "rgame_8." + ext);
                File.Copy(from, to, true);
            }

            LogAssert.RequireNoFailurePatterns(HarnessBootstrap.LogPath);
        }

        public static void BuildContainerFixture(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            Item flask = TestWorld.SpawnItem(game.Player, "Flask", 1, true);
            TestWorld.SpawnItemInContainer(flask, "Torch", 1);

            game.SaveGame(SaveLoadScenarios.TestSlot);
            CopyFixtureDir(repoRoot, "container", SaveLoadScenarios.TestSlot);
        }

        public static void BuildEffectsFixture(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            TestWorld.ApplyEffect(game.Player, EffectID.eid_Prowling);
            game.SaveGame(SaveLoadScenarios.TestSlot);
            CopyFixtureDir(repoRoot, "effects", SaveLoadScenarios.TestSlot);
        }

        private static void CopyFixtureDir(string repoRoot, string name, int slot)
        {
            string dst = SaveLoadScenarios.FixtureDir(repoRoot, name);
            Directory.CreateDirectory(dst);
            string saveDir = Path.Combine(repoRoot, "save");
            foreach (string ext in new[] { "rgp", "rgt", "rgj" }) {
                File.Copy(
                    Path.Combine(saveDir, "rgame_" + slot + "." + ext),
                    Path.Combine(dst, "rgame_8." + ext),
                    true);
            }
        }
    }
}
