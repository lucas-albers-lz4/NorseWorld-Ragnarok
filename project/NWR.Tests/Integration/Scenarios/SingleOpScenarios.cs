using System;
using System.IO;
using NWR.Database;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;

namespace NWR.Tests.Integration.Scenarios
{
    /// <summary>
    /// Catalog-driven single-operation smoke (#14). Per-entry exceptions are recorded;
    /// recipes/dialogs deferred. Targeted scrolls/wands are skipped (need EffectExt targets / GUI).
    /// </summary>
    public static class SingleOpScenarios
    {
        public static string ReportPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "catalogs", "single-op-report.json");
        }

        public static void SingleOpSmoke(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            var json = new JsonBuilder();
            json.BeginObject();
            json.Property("generated", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            json.Property("deferred", "recipes,dialogs,targeted_scrolls_wands");

            int itemPass = 0, itemFail = 0, itemSkip = 0;
            json.Key("items");
            json.BeginArray();
            RunItemOps(game, json, ref itemPass, ref itemFail, ref itemSkip);
            json.EndArray();

            int equipPass = 0, equipFail = 0, equipSkip = 0;
            json.Key("equipment");
            json.BeginArray();
            RunEquipOps(game, json, ref equipPass, ref equipFail, ref equipSkip);
            json.EndArray();

            // Fresh field for effects (avoid stacking from prior ops)
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            int effectPass = 0, effectFail = 0, effectSkip = 0;
            json.Key("effects");
            json.BeginArray();
            RunEffectOps(game, json, ref effectPass, ref effectFail, ref effectSkip);
            json.EndArray();

            json.Key("summary");
            json.BeginObject();
            json.Property("itemPass", itemPass);
            json.Property("itemFail", itemFail);
            json.Property("itemSkip", itemSkip);
            json.Property("equipPass", equipPass);
            json.Property("equipFail", equipFail);
            json.Property("equipSkip", equipSkip);
            json.Property("effectPass", effectPass);
            json.Property("effectFail", effectFail);
            json.Property("effectSkip", effectSkip);
            json.EndObject();
            json.EndObject();

            string outPath = ReportPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, json.ToString());
            Console.WriteLine("wrote " + outPath
                + " items=" + itemPass + "/" + itemFail + "/" + itemSkip
                + " equip=" + equipPass + "/" + equipFail + "/" + equipSkip
                + " effects=" + effectPass + "/" + effectFail + "/" + effectSkip);
        }

        private static void RunItemOps(NWGameSpace game, JsonBuilder json, ref int pass, ref int fail, ref int skip)
        {
            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(GlobalVars.dbItems[i]) as ItemEntry;
                if (entry == null || entry.Meta) {
                    continue;
                }

                string op = ClassifyUseOp(entry.ItmKind);
                if (op == "skip") {
                    skip++;
                    WriteResult(json, entry.Sign, "skip", "kind_not_auto_usable");
                    continue;
                }
                if (op == "needs_target") {
                    skip++;
                    WriteResult(json, entry.Sign, "skip", "needs_target");
                    continue;
                }

                try {
                    Item item = TestWorld.SpawnItem(game.Player, entry.Sign, 1, true);
                    if (item == null) {
                        fail++;
                        WriteResult(json, entry.Sign, "fail", "spawn_null");
                        continue;
                    }

                    game.Player.UseItem(item, null);
                    if (game.Player.Items.IndexOf(item) >= 0) {
                        game.Player.Items.Extract(item);
                    }
                    pass++;
                    WriteResult(json, entry.Sign, "pass", op);
                } catch (Exception ex) {
                    fail++;
                    WriteResult(json, entry.Sign, "fail", Truncate(ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static void RunEquipOps(NWGameSpace game, JsonBuilder json, ref int pass, ref int fail, ref int skip)
        {
            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(GlobalVars.dbItems[i]) as ItemEntry;
                if (entry == null || entry.Meta || !IsEquippable(entry.ItmKind)) {
                    continue;
                }
                if (entry.EqKind == BodypartType.bp_None) {
                    skip++;
                    WriteResult(json, entry.Sign, "skip", "eqKind_none");
                    continue;
                }

                try {
                    Item item = TestWorld.SpawnItem(game.Player, entry.Sign, 1, true);
                    if (item == null) {
                        fail++;
                        WriteResult(json, entry.Sign, "fail", "spawn_null");
                        continue;
                    }
                    item.InUse = true;
                    item.InUse = false;
                    game.Player.Items.Extract(item);
                    pass++;
                    WriteResult(json, entry.Sign, "pass", "equip_toggle");
                } catch (Exception ex) {
                    fail++;
                    WriteResult(json, entry.Sign, "fail", Truncate(ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static void RunEffectOps(NWGameSpace game, JsonBuilder json, ref int pass, ref int fail, ref int skip)
        {
            EffectRec[] effects = EffectsData.dbEffects;
            for (int i = 0; i < effects.Length; i++) {
                EffectID eid = (EffectID)i;
                if (i == 0) {
                    skip++;
                    WriteResult(json, eid.ToString(), "skip", "reserved");
                    continue;
                }

                try {
                    game.Player.AddEffect(eid, ItemState.is_Normal, EffectAction.ea_Persistent, false, "");
                    // Do not Effects.Execute() here — FinAction paths (e.g. Prowling) are
                    // covered elsewhere; this smoke only verifies AddEffect does not throw.
                    while (game.Player.Effects.Count > 0) {
                        game.Player.Effects.Delete(0);
                    }
                    pass++;
                    WriteResult(json, eid.ToString(), "pass", "add_clear");
                } catch (Exception ex) {
                    fail++;
                    WriteResult(json, eid.ToString(), "fail", Truncate(ex.GetType().Name + ": " + ex.Message));
                    try {
                        while (game.Player.Effects.Count > 0) {
                            game.Player.Effects.Delete(0);
                        }
                    } catch {
                    }
                }
            }
        }

        private static string ClassifyUseOp(ItemKind kind)
        {
            switch (kind) {
                case ItemKind.ik_Potion:
                case ItemKind.ik_Food:
                case ItemKind.ik_Tool:
                case ItemKind.ik_MusicalTool:
                    return "use";
                case ItemKind.ik_Scroll:
                case ItemKind.ik_Wand:
                    return "needs_target";
                default:
                    return "skip";
            }
        }

        private static bool IsEquippable(ItemKind kind)
        {
            switch (kind) {
                case ItemKind.ik_Armor:
                case ItemKind.ik_HeavyArmor:
                case ItemKind.ik_MediumArmor:
                case ItemKind.ik_LightArmor:
                case ItemKind.ik_Helmet:
                case ItemKind.ik_Shield:
                case ItemKind.ik_Clothing:
                case ItemKind.ik_Ring:
                case ItemKind.ik_Amulet:
                case ItemKind.ik_BluntWeapon:
                case ItemKind.ik_ShortBlade:
                case ItemKind.ik_LongBlade:
                case ItemKind.ik_Spear:
                case ItemKind.ik_Axe:
                case ItemKind.ik_Bow:
                case ItemKind.ik_CrossBow:
                    return true;
                default:
                    return false;
            }
        }

        private static void WriteResult(JsonBuilder json, string sign, string status, string detail)
        {
            json.BeginObject();
            json.Property("sign", sign ?? "");
            json.Property("status", status);
            json.Property("detail", detail ?? "");
            json.EndObject();
        }

        private static string Truncate(string s)
        {
            if (string.IsNullOrEmpty(s)) {
                return "";
            }
            return s.Length > 160 ? s.Substring(0, 160) : s;
        }
    }
}
