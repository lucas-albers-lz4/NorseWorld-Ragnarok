using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BSLib;
using NWR.Creatures;
using NWR.Database;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Universe;
using ZRLib.Core;

namespace NWR.Tests.Integration.Scenarios
{
    /// <summary>
    /// Seeded random operation sequences (#15). Reloads slot8 per sequence; records
    /// minimal failing sequences with seed for replay.
    /// </summary>
    public static class FuzzScenarios
    {
        private static readonly EffectID[] SafeEffects = {
            EffectID.eid_Blessing,
            EffectID.eid_Confusion,
            EffectID.eid_Deafness,
            EffectID.eid_Diseased,
            EffectID.eid_Hastening,
            EffectID.eid_Invisibility,
            EffectID.eid_Invulnerable,
            EffectID.eid_Paralysis,
            EffectID.eid_Poisoned,
            EffectID.eid_Protection,
            EffectID.eid_Regeneration,
            EffectID.eid_Strength,
            EffectID.eid_Blindness,
            EffectID.eid_Withered
        };

        private static readonly int[] MoveDirs = {
            Directions.DtNorth, Directions.DtSouth, Directions.DtWest, Directions.DtEast,
            Directions.DtNorthWest, Directions.DtNorthEast, Directions.DtSouthWest, Directions.DtSouthEast
        };

        private enum FuzzOp
        {
            SpawnItem,
            ApplyEffect,
            UseItem,
            WaitTurns,
            SaveLoad,
            Move,
            Attack,
            Drop,
            PickUp,
            Wield
        }

        public static string ReportPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "fuzz", "fuzz-report.json");
        }

        public static void SequenceFuzz(string repoRoot)
        {
            int masterSeed = ParseEnvInt("NWR_FUZZ_SEED", 15);
            int sequenceCount = ParseEnvInt("NWR_FUZZ_N", 1000);
            int minSteps = ParseEnvInt("NWR_FUZZ_MIN_STEPS", 3);
            int maxSteps = ParseEnvInt("NWR_FUZZ_MAX_STEPS", 8);
            if (maxSteps < minSteps) {
                maxSteps = minSteps;
            }

            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            List<string> usableItems = BuildUsableItems();
            List<string> equipItems = BuildEquipItems();
            if (usableItems.Count == 0) {
                throw new InvalidOperationException("fuzz: no usable items in catalog");
            }

            var master = new Random(masterSeed);
            int pass = 0;
            int fail = 0;
            var failures = new List<FailureRec>();

            for (int i = 0; i < sequenceCount; i++) {
                int seqSeed = master.Next();
                var seqRng = new Random(seqSeed);
                RandomHelper.RandSeed = seqSeed;
                RandomHelper.Randomize();

                SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
                game.LoadGame(SaveLoadScenarios.TestSlot);
                try {
                    if (File.Exists(HarnessBootstrap.LogPath)) {
                        File.WriteAllText(HarnessBootstrap.LogPath, "");
                    }
                } catch (Exception) {
                    // ignore log truncate races
                }

                int steps = minSteps + seqRng.Next(maxSteps - minSteps + 1);
                var ops = new List<string>();
                try {
                    for (int s = 0; s < steps; s++) {
                        string opDesc = ExecuteRandomOp(game, seqRng, usableItems, equipItems);
                        ops.Add(opDesc);
                        CheckInvariants(game, s);
                    }
                    pass++;
                } catch (Exception ex) {
                    fail++;
                    failures.Add(new FailureRec {
                        Index = i,
                        Seed = seqSeed,
                        Steps = string.Join(" | ", ops.ToArray()),
                        Error = Truncate(ex.Message)
                    });
                    if (failures.Count >= 20) {
                        // Cap recorded failures; keep running to exercise harness stability
                    }
                }
            }

            WriteReport(repoRoot, masterSeed, sequenceCount, minSteps, maxSteps, pass, fail, failures);
            Console.WriteLine("fuzz sequences=" + sequenceCount + " pass=" + pass + " fail=" + fail
                + " seed=" + masterSeed);

            if (fail > 0) {
                throw new InvalidOperationException(
                    "sequence-fuzz: " + fail + " failing sequence(s); see " + ReportPath(repoRoot));
            }
        }

        private static string ExecuteRandomOp(NWGameSpace game, Random rng, List<string> usable, List<string> equip)
        {
            Array values = Enum.GetValues(typeof(FuzzOp));
            FuzzOp op = (FuzzOp)values.GetValue(rng.Next(values.Length));
            Player p = game.Player;

            switch (op) {
                case FuzzOp.SpawnItem: {
                    string sign = usable[rng.Next(usable.Count)];
                    Item item = TestWorld.SpawnItem(p, sign, 1, true);
                    return "Spawn(" + sign + (item == null ? ":null" : "") + ")";
                }
                case FuzzOp.ApplyEffect: {
                    EffectID eid = SafeEffects[rng.Next(SafeEffects.Length)];
                    TestWorld.ApplyEffect(p, eid);
                    return "ApplyEffect(" + eid + ")";
                }
                case FuzzOp.UseItem: {
                    Item item = FindUsableInventoryItem(p, usable);
                    if (item == null) {
                        string sign = usable[rng.Next(usable.Count)];
                        item = TestWorld.SpawnItem(p, sign, 1, true);
                    }
                    if (item == null) {
                        return "UseItem(skip)";
                    }
                    string used = item.Entry != null ? item.Entry.Sign : "?";
                    p.UseItem(item, null);
                    return "UseItem(" + used + ")";
                }
                case FuzzOp.WaitTurns: {
                    int n = 1 + rng.Next(3);
                    TestWorld.RunTurns(game, n);
                    return "WaitTurns(" + n + ")";
                }
                case FuzzOp.SaveLoad: {
                    PlayerSnapshot before = PlayerSnapshot.Capture(p);
                    game.SaveGame(SaveLoadScenarios.TestSlot);
                    game.LoadGame(SaveLoadScenarios.TestSlot);
                    PlayerSnapshot after = PlayerSnapshot.Capture(game.Player);
                    if (before.Name != after.Name || before.LayerID != after.LayerID) {
                        throw new InvalidOperationException("SaveLoad identity mismatch");
                    }
                    return "SaveLoad";
                }
                case FuzzOp.Move: {
                    int dir = MoveDirs[rng.Next(MoveDirs.Length)];
                    game.DoPlayerAction(CreatureAction.caMove, dir);
                    game.ProcessGameStep();
                    return "Move(" + dir + ")";
                }
                case FuzzOp.Attack: {
                    int dir = MoveDirs[rng.Next(MoveDirs.Length)];
                    NWField fld = p.CurrentField;
                    int nx = p.PosX + Directions.Data[dir].DX;
                    int ny = p.PosY + Directions.Data[dir].DY;
                    NWCreature enemy = fld != null ? (NWCreature)fld.FindCreature(nx, ny) : null;
                    if (enemy == null || enemy.State == CreatureState.Dead || enemy.IsPlayer) {
                        return "Attack(skip)";
                    }
                    game.DoPlayerAction(CreatureAction.caAttackMelee, dir);
                    game.ProcessGameStep();
                    return "Attack(" + dir + ")";
                }
                case FuzzOp.Drop: {
                    Item item = PickDroppable(p);
                    if (item == null) {
                        return "Drop(skip)";
                    }
                    string sign = item.Entry != null ? item.Entry.Sign : "?";
                    p.DropItem(item);
                    return "Drop(" + sign + ")";
                }
                case FuzzOp.PickUp: {
                    // Drop something first if field has nothing useful
                    Item onField = FindFieldItem(p);
                    if (onField == null) {
                        Item drop = PickDroppable(p);
                        if (drop != null) {
                            p.DropItem(drop);
                            onField = drop;
                        }
                    }
                    if (onField == null) {
                        return "PickUp(skip)";
                    }
                    p.PickupItem(onField);
                    return "PickUp";
                }
                case FuzzOp.Wield: {
                    Item item = null;
                    for (int i = 0; i < p.Items.Count; i++) {
                        Item it = p.Items[i];
                        if (it != null && !it.InUse && p.CanBeUsed(it)) {
                            item = it;
                            break;
                        }
                    }
                    if (item == null && equip.Count > 0) {
                        item = TestWorld.SpawnItem(p, equip[rng.Next(equip.Count)], 1, true);
                    }
                    if (item == null || !p.CanBeUsed(item)) {
                        return "Wield(skip)";
                    }
                    p.WearItem(item);
                    return "Wield(" + (item.Entry != null ? item.Entry.Sign : "?") + ")";
                }
                default:
                    return "Nop";
            }
        }

        private static void CheckInvariants(NWGameSpace game, int step)
        {
            Player p = game.Player;
            if (p == null) {
                throw new InvalidOperationException("step " + step + ": null player");
            }
            if (p.CurrentField == null) {
                throw new InvalidOperationException("step " + step + ": null field");
            }
            if (p.PosX < 0 || p.PosY < 0 ||
                p.PosX >= StaticData.FieldWidth || p.PosY >= StaticData.FieldHeight) {
                throw new InvalidOperationException(
                    "step " + step + ": OOB (" + p.PosX + "," + p.PosY + ")");
            }
            // Dead player: still must not leave invalid field; further ops may be skipped by death paths
            LogAssert.RequireNoFailurePatterns(HarnessBootstrap.LogPath);
        }

        private static List<string> BuildUsableItems()
        {
            var list = new List<string>();
            if (GlobalVars.dbItems == null) {
                return list;
            }
            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(GlobalVars.dbItems[i]) as ItemEntry;
                if (entry == null || entry.Meta) {
                    continue;
                }
                switch (entry.ItmKind) {
                    case ItemKind.ik_Potion:
                    case ItemKind.ik_Food:
                    case ItemKind.ik_Tool:
                    case ItemKind.ik_MusicalTool:
                        list.Add(entry.Sign);
                        break;
                }
            }
            return list;
        }

        private static List<string> BuildEquipItems()
        {
            var list = new List<string>();
            if (GlobalVars.dbItems == null) {
                return list;
            }
            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(GlobalVars.dbItems[i]) as ItemEntry;
                if (entry == null || entry.Meta || entry.EqKind == BodypartType.bp_None) {
                    continue;
                }
                switch (entry.ItmKind) {
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
                        list.Add(entry.Sign);
                        break;
                }
            }
            return list;
        }

        private static Item FindUsableInventoryItem(Player p, List<string> usable)
        {
            for (int i = 0; i < p.Items.Count; i++) {
                Item it = p.Items[i];
                if (it == null || it.Entry == null) {
                    continue;
                }
                for (int u = 0; u < usable.Count; u++) {
                    if (it.Entry.Sign == usable[u]) {
                        return it;
                    }
                }
            }
            return null;
        }

        private static Item PickDroppable(Player p)
        {
            for (int i = p.Items.Count - 1; i >= 0; i--) {
                Item it = p.Items[i];
                if (it != null && !it.InUse) {
                    return it;
                }
            }
            return null;
        }

        private static Item FindFieldItem(Player p)
        {
            NWField fld = p.CurrentField;
            if (fld == null || fld.Items == null) {
                return null;
            }
            for (int i = 0; i < fld.Items.Count; i++) {
                Item it = fld.Items[i];
                if (it != null && it.PosX == p.PosX && it.PosY == p.PosY) {
                    return it;
                }
            }
            return null;
        }

        private static void WriteReport(string repoRoot, int seed, int n, int minSteps, int maxSteps,
            int pass, int fail, List<FailureRec> failures)
        {
            string path = ReportPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var jb = new JsonBuilder();
            jb.BeginObject();
            jb.Property("generated", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            jb.Property("issue", 15);
            jb.Property("masterSeed", seed);
            jb.Property("sequences", n);
            jb.Property("minSteps", minSteps);
            jb.Property("maxSteps", maxSteps);
            jb.Property("pass", pass);
            jb.Property("fail", fail);
            jb.Key("failures");
            jb.BeginArray();
            int limit = Math.Min(failures.Count, 20);
            for (int i = 0; i < limit; i++) {
                jb.BeginObject();
                jb.Property("index", failures[i].Index);
                jb.Property("seed", failures[i].Seed);
                jb.Property("steps", failures[i].Steps);
                jb.Property("error", failures[i].Error);
                jb.EndObject();
            }
            jb.EndArray();
            jb.EndObject();
            File.WriteAllText(path, jb.ToString());
            Console.WriteLine("wrote " + path);
        }

        private static int ParseEnvInt(string name, int fallback)
        {
            string v = Environment.GetEnvironmentVariable(name);
            int parsed;
            if (!string.IsNullOrEmpty(v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) {
                return parsed;
            }
            return fallback;
        }

        private static string Truncate(string s)
        {
            if (string.IsNullOrEmpty(s)) {
                return "";
            }
            return s.Length > 200 ? s.Substring(0, 200) : s;
        }

        private struct FailureRec
        {
            public int Index;
            public int Seed;
            public string Steps;
            public string Error;
        }
    }
}
