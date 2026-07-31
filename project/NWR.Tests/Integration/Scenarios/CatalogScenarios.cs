using System;
using System.IO;
using NWR.Database;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;

namespace NWR.Tests.Integration.Scenarios
{
    public static class CatalogScenarios
    {
        public static string ItemsCatalogPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "catalogs", "items.json");
        }

        public static void ItemCatalog(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            if (GlobalVars.dbItems == null || GlobalVars.dbItems.Count == 0) {
                throw new InvalidOperationException("dbItems is empty");
            }

            int spawnOk = 0;
            int spawnFail = 0;
            int metaCount = 0;

            var json = new JsonBuilder();
            json.BeginObject();
            json.Property("generated", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            json.Property("count", GlobalVars.dbItems.Count);
            json.Key("items");
            json.BeginArray();

            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                int guid = GlobalVars.dbItems[i];
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(guid) as ItemEntry;
                if (entry == null) {
                    throw new InvalidOperationException("dbItems[" + i + "] GUID " + guid + " is not ItemEntry");
                }

                json.BeginObject();
                json.Property("sign", entry.Sign ?? "");
                json.Property("guid", entry.GUID);
                json.Property("kind", entry.ItmKind.ToString());
                json.Property("flags", entry.Flags != null ? entry.Flags.Signature : "");
                json.Property("meta", entry.Meta);
                json.Property("unique", entry.Unique);
                json.Property("countable", entry.Countable);
                json.Property("weight", entry.Weight);
                json.Property("price", entry.Price);
                json.Property("satiety", entry.Satiety);
                json.Property("eqKind", entry.EqKind.ToString());
                json.Property("material", entry.Material.ToString());
                json.Property("frequency", entry.Frequency);

                json.Key("attributes");
                json.BeginObject();
                json.Property("Defense", entry.Attributes[0]);
                json.Property("DamageMin", entry.Attributes[1]);
                json.Property("DamageMax", entry.Attributes[2]);
                json.Property("Mdf_Str", entry.Attributes[3]);
                json.Property("Mdf_Luck", entry.Attributes[4]);
                json.Property("Mdf_Speed", entry.Attributes[5]);
                json.Property("Mdf_Attacks", entry.Attributes[6]);
                json.Property("Mdf_ToHit", entry.Attributes[7]);
                json.Property("Mdf_Health", entry.Attributes[8]);
                json.Property("Mdf_Mana", entry.Attributes[9]);
                json.EndObject();

                if (entry.Effects != null && entry.Effects.Length > 0) {
                    json.Key("effects");
                    json.BeginArray();
                    for (int e = 0; e < entry.Effects.Length; e++) {
                        json.BeginObject();
                        json.Property("effID", entry.Effects[e].EffID.ToString());
                        json.Property("extData", entry.Effects[e].ExtData);
                        json.EndObject();
                    }
                    json.EndArray();
                }

                string spawnStatus = "skipped_meta";
                if (entry.Meta) {
                    metaCount++;
                } else {
                    int before = game.Player.Items.Count;
                    Item.GenItem(game.Player, entry.GUID, 1, true);
                    if (game.Player.Items.Count > before) {
                        spawnOk++;
                        spawnStatus = "ok";
                        // Drop extras so inventory does not balloon across 200+ items
                        Item spawned = game.Player.Items[game.Player.Items.Count - 1];
                        game.Player.Items.Extract(spawned);
                    } else {
                        spawnFail++;
                        spawnStatus = "fail";
                    }
                }
                json.Property("spawn", spawnStatus);
                json.EndObject();
            }

            json.EndArray();
            json.Property("spawnOk", spawnOk);
            json.Property("spawnFail", spawnFail);
            json.Property("metaCount", metaCount);
            json.EndObject();

            if (spawnFail > 0) {
                throw new InvalidOperationException("item-catalog: " + spawnFail + " non-meta items failed to spawn");
            }

            string outPath = ItemsCatalogPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, json.ToString());
            Console.WriteLine("wrote " + outPath + " items=" + GlobalVars.dbItems.Count
                + " spawnOk=" + spawnOk + " meta=" + metaCount);
        }
    }
}
