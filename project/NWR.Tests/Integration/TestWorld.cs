using System;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Universe;

namespace NWR.Tests.Integration
{
    public static class TestWorld
    {
        public static Item SpawnItem(Player player, string sign, int count, bool identified)
        {
            int id = GlobalVars.nwrGame.FindDataEntry(sign).GUID;
            int before = player.Items.Count;
            Item.GenItem(player, id, count, identified);
            if (player.Items.Count <= before) {
                return null;
            }
            return player.Items[player.Items.Count - 1];
        }

        public static void SpawnItemInContainer(Item container, string sign, int count)
        {
            NWGameSpace game = GlobalVars.nwrGame;
            int id = game.FindDataEntry(sign).GUID;
            var inner = new Item(game, container);
            inner.CLSID = id;
            inner.Count = (ushort)Math.Max(1, count);
            inner.Identified = true;
            container.Contents.Add(inner);
        }

        public static void ApplyEffect(Player player, EffectID effectId)
        {
            player.AddEffect(effectId, ItemState.is_Normal, EffectAction.ea_Persistent, false, "");
        }

        public static void RunTurns(NWGameSpace game, int turns)
        {
            for (int i = 0; i < turns; i++) {
                game.DoPlayerAction(CreatureAction.caWait, 0);
                game.ProcessGameStep();
            }
        }

        public static int CountContainerItems(Player player)
        {
            int total = player.Items.Count;
            for (int i = 0; i < player.Items.Count; i++) {
                Item item = player.Items[i];
                if (item.Container) {
                    total += item.Contents.Count;
                }
            }
            return total;
        }

        /// <summary>
        /// Loaded fixtures do not restore fQuests; call after LoadGame for quest tests.
        /// </summary>
        public static void EnsureMainQuests(NWGameSpace game)
        {
            game.GenMainQuests();
        }

        public static NWCreature PlaceCreatureNearPlayer(NWGameSpace game, int creatureId)
        {
            Player player = game.Player;
            NWField fld = player.CurrentField;
            int px = -1;
            int py = -1;
            for (int dx = -1; dx <= 1 && px < 0; dx++) {
                for (int dy = -1; dy <= 1; dy++) {
                    if (dx == 0 && dy == 0) {
                        continue;
                    }
                    int nx = player.PosX + dx;
                    int ny = player.PosY + dy;
                    if (player.CanMove(fld, nx, ny) && fld.FindCreature(nx, ny) == null) {
                        px = nx;
                        py = ny;
                        break;
                    }
                }
            }
            if (px < 0) {
                throw new InvalidOperationException("no adjacent tile for creature " + creatureId);
            }

            NWCreature cr = game.FindCreature(creatureId);
            if (cr != null) {
                cr.TransferTo(player.LayerID, fld.Coords.X, fld.Coords.Y, px, py, StaticData.MapArea, true, false);
                return cr;
            }

            cr = game.AddCreatureEx(player.LayerID, fld.Coords.X, fld.Coords.Y, px, py, creatureId);
            if (cr == null) {
                throw new InvalidOperationException("AddCreatureEx failed for " + creatureId);
            }
            return cr;
        }

        public static QuestItemState HandInArtefact(NWGameSpace game, string itemSign, int artefactId, int deityId, int maxTurns)
        {
            Item item = SpawnItem(game.Player, itemSign, 1, true);
            if (item == null) {
                throw new InvalidOperationException("could not spawn " + itemSign);
            }
            if (game.CheckQuestItem(artefactId, deityId) != QuestItemState.Founded) {
                throw new InvalidOperationException(itemSign + ": expected Founded after spawn");
            }

            PlaceCreatureNearPlayer(game, deityId);
            for (int i = 0; i < maxTurns; i++) {
                if (game.CheckQuestItem(artefactId, deityId) == QuestItemState.Completed) {
                    return QuestItemState.Completed;
                }
                RunTurns(game, 1);
            }
            return game.CheckQuestItem(artefactId, deityId);
        }
    }
}
