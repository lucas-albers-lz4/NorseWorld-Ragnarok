using System;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Universe;

namespace NWR.Tests.Integration.Scenarios
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
                throw new InvalidOperationException("container-roundtrip mismatch: " + itemsBefore + " vs " + itemsAfter);
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
                throw new InvalidOperationException("effect-persist mismatch");
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
                throw new InvalidOperationException("wait-turns: turn did not advance");
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
                throw new InvalidOperationException("could not spawn Potion_Curing");
            }
            game.Player.UseItem(potion, null);
            if (game.Player.HPCur <= hpBefore) {
                throw new InvalidOperationException("item-use-potion: HP did not increase");
            }
        }

        public static void TeleportTrap(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            Player player = game.Player;
            NWField fld = player.CurrentField;
            player.SetAbility(AbilityID.Resist_Teleport, 0);

            int origX = player.PosX;
            int origY = player.PosY;
            int trapX = -1;
            int trapY = -1;

            for (int dx = -1; dx <= 1; dx++) {
                for (int dy = -1; dy <= 1; dy++) {
                    if (dx == 0 && dy == 0) {
                        continue;
                    }
                    int nx = origX + dx;
                    int ny = origY + dy;
                    if (player.CanMove(fld, nx, ny) && fld.FindCreature(nx, ny) == null) {
                        trapX = nx;
                        trapY = ny;
                        break;
                    }
                }
                if (trapX >= 0) {
                    break;
                }
            }

            if (trapX < 0) {
                throw new InvalidOperationException("teleport-trap: no adjacent walkable tile");
            }

            NWTile trapTile = (NWTile)fld.GetTile(trapX, trapY);
            trapTile.Foreground = PlaceID.pid_TeleportTrap;
            player.MoveTo(trapX, trapY);

            if (player.PosX == origX && player.PosY == origY) {
                throw new InvalidOperationException("teleport-trap: player was not relocated");
            }
        }
    }
}
