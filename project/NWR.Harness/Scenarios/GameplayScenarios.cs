using System;
using NWR.Game;
using NWR.Items;

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
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            int turnBefore = game.Player.Turn;
            TestWorld.RunTurns(game, 5);
            if (game.Player.Turn <= turnBefore) {
                throw new InvalidOperationException("wait-turns: player turn did not advance");
            }
        }

        public static void ItemUsePotion(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            int hpBefore = game.Player.HPCur;
            if (game.Player.HPCur >= game.Player.HPMax_Renamed) {
                game.Player.HPCur = Math.Max(1, game.Player.HPMax_Renamed / 2);
                hpBefore = game.Player.HPCur;
            }

            Item potion = TestWorld.SpawnItem(game.Player, "Potion_Curing", 1, true);
            if (potion == null) {
                throw new InvalidOperationException("item-use-potion: could not spawn Potion_Curing");
            }
            game.Player.UseItem(potion, null);

            if (game.Player.HPCur <= hpBefore) {
                throw new InvalidOperationException("item-use-potion: HP did not increase");
            }
        }
    }
}
