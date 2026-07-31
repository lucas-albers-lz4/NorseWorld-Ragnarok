using System;
using NWR.Game;
using NWR.Harness.Dsl;
using NWR.Items;
using NWR.ScenarioDsl;

namespace NWR.Harness.Scenarios
{
    public static class GameplayScenarios
    {
        public static void ContainerRoundtrip(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "container", SaveLoadScenarios.TestSlot);

            game.LoadGame(SaveLoadScenarios.TestSlot);
            int itemsBefore = TestWorld.CountContainerItems(game.Player);

            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            int itemsAfter = TestWorld.CountContainerItems(game.Player);
            if (itemsBefore != itemsAfter) {
                throw new InvalidOperationException(
                    "container-roundtrip item count mismatch: " + itemsBefore + " vs " + itemsAfter);
            }
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok", "terrainsLoad(): ok");
        }

        public static void EffectPersist(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "effects", SaveLoadScenarios.TestSlot);

            game.LoadGame(SaveLoadScenarios.TestSlot);
            int effectsBefore = game.Player.Effects.Count;

            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            if (game.Player.Effects.Count != effectsBefore) {
                throw new InvalidOperationException(
                    "effect-persist count mismatch: " + effectsBefore + " vs " + game.Player.Effects.Count);
            }
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok", "terrainsLoad(): ok");
        }

        public static void WaitTurns(string repoRoot)
        {
            Scenario.Create("wait-turns")
                .LoadFixture("slot8")
                .Capture("turn")
                .WaitTurns(5)
                .Check("turnAdvanced")
                .Run(HarnessScenarioEnv.Instance, repoRoot);
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
                .Run(HarnessScenarioEnv.Instance, repoRoot);
        }
    }
}
