using System;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;

namespace NWR.Harness
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
    }
}
