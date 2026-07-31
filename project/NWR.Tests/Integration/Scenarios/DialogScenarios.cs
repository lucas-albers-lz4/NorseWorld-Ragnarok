using System;
using NWR.Creatures;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Universe;

namespace NWR.Tests.Integration.Scenarios
{
    /// <summary>
    /// Oldman DemonBane dialog scripts (#59) — conditions/actions from ru_dlg_oldman.xml via Jint.
    /// </summary>
    public static class DialogScenarios
    {
        public static void OldmanDemonBane(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            Player player = game.Player;
            NWCreature oldman = FindOrPlaceOldman(game);
            EnsureDemonBane(oldman);

            // Topic visible only if NPC.hasItem('DemonBane')
            if (!DialogScript.Check(oldman, player, "NPC.hasItem('DemonBane')")) {
                throw new InvalidOperationException("expected NPC.hasItem('DemonBane') true");
            }

            // Without the sword, condition fails
            Item sword = oldman.FindItem("DemonBane");
            oldman.Items.Extract(sword);
            if (DialogScript.Check(oldman, player, "NPC.hasItem('DemonBane')")) {
                throw new InvalidOperationException("expected hasItem false after extract");
            }
            oldman.AddItem(sword);

            // Clear hostiles so isFieldCleared matches reward topic
            ClearHostileCreatures(oldman);
            if (!DialogScript.Check(oldman, player, "NPC.isFieldCleared()")) {
                throw new InvalidOperationException("expected NPC.isFieldCleared() true after clear");
            }

            bool hadBefore = player.HasItem("DemonBane");
            DialogScript.RunAction(oldman, player, "NPC.transferItem(player, 'DemonBane');");
            if (!player.HasItem("DemonBane")) {
                throw new InvalidOperationException("player missing DemonBane after transferItem");
            }
            if (oldman.HasItem("DemonBane")) {
                throw new InvalidOperationException("Oldman still holds DemonBane after transfer");
            }
            if (hadBefore) {
                throw new InvalidOperationException("player already had DemonBane before transfer");
            }

            Console.WriteLine("oldman-demonbane: hasItem/isFieldCleared/transferItem ok");
        }

        private static NWCreature FindOrPlaceOldman(NWGameSpace game)
        {
            Player player = game.Player;
            NWField fld = player.CurrentField;
            for (int i = 0; i < fld.Creatures.Count; i++) {
                NWCreature cr = fld.Creatures[i];
                if (cr != null && cr.CLSID == GlobalVars.cid_Oldman) {
                    return cr;
                }
            }
            return TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Oldman);
        }

        private static void EnsureDemonBane(NWCreature oldman)
        {
            if (oldman.HasItem("DemonBane")) {
                return;
            }
            int id = GlobalVars.nwrGame.FindDataEntry("DemonBane").GUID;
            var item = new Item(GlobalVars.nwrGame, oldman);
            item.CLSID = id;
            item.Count = 1;
            item.Identified = true;
            oldman.AddItem(item);
            if (!oldman.HasItem("DemonBane")) {
                throw new InvalidOperationException("failed to give Oldman DemonBane");
            }
        }

        private static void ClearHostileCreatures(NWCreature oldman)
        {
            NWField fld = oldman.CurrentField;
            if (fld == null) {
                return;
            }
            // Snapshot UIDs — Death mutates creature list
            var toKill = new System.Collections.Generic.List<NWCreature>();
            for (int i = 0; i < fld.Creatures.Count; i++) {
                NWCreature cr = fld.Creatures[i];
                if (cr == null || cr.Equals(oldman) || cr.IsPlayer) {
                    continue;
                }
                if (oldman.IsEnemy(cr)) {
                    toKill.Add(cr);
                }
            }
            for (int i = 0; i < toKill.Count; i++) {
                if (toKill[i].State != CreatureState.Dead) {
                    toKill[i].Death("dialog-scenario clear", oldman);
                }
            }
        }
    }
}
