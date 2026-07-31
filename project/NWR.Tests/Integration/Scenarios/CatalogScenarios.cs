using System;
using System.IO;
using NWR.Creatures;
using NWR.Database;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Universe;

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

        public static string CreaturesCatalogPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "catalogs", "creatures.json");
        }

        public static string EffectsCatalogPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "catalogs", "effects.json");
        }

        public static void CreatureCatalog(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            if (GlobalVars.dbCreatures == null || GlobalVars.dbCreatures.Count == 0) {
                throw new InvalidOperationException("dbCreatures is empty");
            }

            Player player = game.Player;
            NWField fld = player.CurrentField;
            int spawnOk = 0;
            int spawnFail = 0;

            var json = new JsonBuilder();
            json.BeginObject();
            json.Property("generated", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            json.Property("count", GlobalVars.dbCreatures.Count);
            json.Key("creatures");
            json.BeginArray();

            for (int i = 0; i < GlobalVars.dbCreatures.Count; i++) {
                int guid = GlobalVars.dbCreatures[i];
                CreatureEntry entry = GlobalVars.nwrDB.GetEntry(guid) as CreatureEntry;
                if (entry == null) {
                    throw new InvalidOperationException("dbCreatures GUID " + guid + " is not CreatureEntry");
                }

                string predictedBrain = PredictBrainType(entry);
                string spawnStatus = "fail";
                string actualBrain = "";

                NWCreature cr = null;
                try {
                    int px = player.PosX;
                    int py = player.PosY;
                    // Prefer a free adjacent tile
                    for (int dx = -1; dx <= 1 && cr == null; dx++) {
                        for (int dy = -1; dy <= 1; dy++) {
                            if (dx == 0 && dy == 0) {
                                continue;
                            }
                            int nx = px + dx;
                            int ny = py + dy;
                            if (fld.FindCreature(nx, ny) == null) {
                                cr = game.AddCreatureEx(player.LayerID, fld.Coords.X, fld.Coords.Y, nx, ny, guid);
                                break;
                            }
                        }
                    }
                    if (cr == null) {
                        cr = game.AddCreatureEx(player.LayerID, fld.Coords.X, fld.Coords.Y, -1, -1, guid);
                    }
                    if (cr != null) {
                        spawnOk++;
                        spawnStatus = "ok";
                        if (cr.Brain != null) {
                            actualBrain = cr.Brain.GetType().Name;
                        }
                        // Remove from field to avoid crowding unique/volatile slots
                        if (cr.CurrentField != null) {
                            cr.CurrentField.Creatures.Extract(cr);
                        }
                    } else {
                        spawnFail++;
                    }
                } catch (Exception ex) {
                    spawnFail++;
                    spawnStatus = "error:" + ex.GetType().Name;
                }

                json.BeginObject();
                json.Property("sign", entry.Sign ?? "");
                json.Property("guid", entry.GUID);
                json.Property("race", entry.Race.ToString());
                json.Property("flags", entry.Flags != null ? entry.Flags.Signature : "");
                json.Property("speed", entry.Speed);
                json.Property("ac", entry.AC);
                json.Property("minHP", entry.MinHP);
                json.Property("maxHP", entry.MaxHP);
                json.Property("toHit", entry.ToHit);
                json.Property("attacks", entry.Attacks);
                json.Property("level", entry.Level);
                json.Property("alignment", entry.Alignment.ToString());
                json.Property("sex", entry.Sex.ToString());
                json.Property("survey", entry.Survey);
                json.Property("predictedBrain", predictedBrain);
                json.Property("actualBrain", actualBrain);
                json.Property("spawn", spawnStatus);

                if (entry.Abilities != null && entry.Abilities.Count > 0) {
                    json.Key("abilities");
                    json.BeginObject();
                    for (int a = 0; a < entry.Abilities.Count; a++) {
                        var attr = entry.Abilities.GetItem(a);
                        json.Property(((AbilityID)attr.AID).ToString(), attr.AValue);
                    }
                    json.EndObject();
                }
                if (entry.Skills != null && entry.Skills.Count > 0) {
                    json.Key("skills");
                    json.BeginObject();
                    for (int s = 0; s < entry.Skills.Count; s++) {
                        var attr = entry.Skills.GetItem(s);
                        json.Property(((SkillID)attr.AID).ToString(), attr.AValue);
                    }
                    json.EndObject();
                }
                json.EndObject();
            }

            json.EndArray();
            json.Property("spawnOk", spawnOk);
            json.Property("spawnFail", spawnFail);
            json.EndObject();

            string outPath = CreaturesCatalogPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, json.ToString());
            Console.WriteLine("wrote " + outPath + " creatures=" + GlobalVars.dbCreatures.Count
                + " spawnOk=" + spawnOk + " spawnFail=" + spawnFail);
        }

        public static void EffectCatalog(string repoRoot)
        {
            HarnessBootstrap.Init(repoRoot);

            EffectRec[] effects = EffectsData.dbEffects;
            if (effects == null || effects.Length == 0) {
                throw new InvalidOperationException("dbEffects is empty");
            }

            var json = new JsonBuilder();
            json.BeginObject();
            json.Property("generated", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            json.Property("count", effects.Length);
            json.Key("effects");
            json.BeginArray();

            int rayCount = 0;
            for (int i = 0; i < effects.Length; i++) {
                EffectRec rec = effects[i];
                EffectID eid = (EffectID)i;
                bool isRay = rec.Flags != null && rec.Flags.Contains(EffectFlags.ef_Ray);
                if (isRay) {
                    rayCount++;
                }

                json.BeginObject();
                json.Property("id", i);
                json.Property("effectID", eid.ToString());
                json.Property("nameRS", rec.NameRS);
                json.Property("flags", rec.Flags != null ? rec.Flags.Signature : "");
                json.Property("reqParams", rec.ReqParams != null ? rec.ReqParams.Signature : "");
                json.Property("animation", rec.AnimationKind.ToString());
                json.Property("durationMin", rec.Duration.Min);
                json.Property("durationMax", rec.Duration.Max);
                json.Property("magnitudeMin", rec.Magnitude.Min);
                json.Property("magnitudeMax", rec.Magnitude.Max);
                json.Property("mpReq", rec.MPReq);
                json.Property("levReq", rec.LevReq);
                json.Property("resistance", rec.Resistance.ToString());
                json.Property("damageMin", rec.Damage.Min);
                json.Property("damageMax", rec.Damage.Max);
                json.Property("sfx", rec.SFX ?? "");
                json.Property("gfx", rec.GFX ?? "");
                json.Property("isRay", isRay);
                json.Property("rayHandler", ResolveRayHandler(eid, isRay));
                json.EndObject();
            }

            json.EndArray();
            json.Property("rayCount", rayCount);
            json.EndObject();

            string outPath = EffectsCatalogPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, json.ToString());
            Console.WriteLine("wrote " + outPath + " effects=" + effects.Length + " rays=" + rayCount);
        }

        private static string PredictBrainType(CreatureEntry entry)
        {
            string sign = entry.Sign ?? "";
            if (sign.Equals("Agnar") || sign.Equals("Haddingr") || sign.Equals("Ketill")) {
                return "VictimBrain";
            }
            if (sign.Equals("Eitri")) {
                return "EitriBrain";
            }
            if (sign.Equals("Raven")) {
                return "RavenBrain";
            }
            if (sign.Equals("Guardsman") || sign.Equals("Jarl")) {
                return "WarriorBrain";
            }
            // Player heroes use LeaderBrain only when instantiated as Player
            if (sign.Equals("Viking") || sign.Equals("Alchemist") || sign.Equals("Blacksmith")
                || sign.Equals("Conjurer") || sign.Equals("Sage") || sign.Equals("Woodsman")) {
                return "LeaderBrain(player)|SentientBrain(npc)";
            }
            if (entry.Flags != null && entry.Flags.Contains(CreatureFlags.esMind)) {
                return "SentientBrain";
            }
            return "BeastBrain";
        }

        private static string ResolveRayHandler(EffectID eid, bool isRay)
        {
            switch (eid) {
                case EffectID.eid_Annihilation:
                    return "AnnihilationRay";
                case EffectID.eid_Cancellation:
                    return "CancellationRay";
                case EffectID.eid_Deanimation:
                    return "DeanimationRay";
                case EffectID.eid_Death:
                    return "DeathRay";
                case EffectID.eid_Fire:
                    return "FireRay";
                case EffectID.eid_FireVision:
                    return "FireVisionRay";
                case EffectID.eid_Flaying:
                    return "FlayingRay";
                case EffectID.eid_GrapplingHookUse:
                    return "GrapplingHookRay";
                case EffectID.eid_Ice:
                    return "IceRay";
                case EffectID.eid_Polymorph:
                    return "PolymorphRay";
                case EffectID.eid_Stoning:
                    return "StoningRay";
                case EffectID.eid_Transmutation:
                    return "TransmutationRay";
                case EffectID.eid_Tunneling:
                    return "TunnelingRay";
                case EffectID.eid_BlackGemUse:
                    return "BlackGemRay";
                default:
                    if (isRay) {
                        return "MonsterSkillRay";
                    }
                    return "";
            }
        }
    }
}
