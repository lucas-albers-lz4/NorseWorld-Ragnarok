using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NWR.Creatures;
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
    /// Targeted roguelike edge cases (#17). Each case reloads slot8, exercises one hazard,
    /// attempts save/load, and records pass/fail/bug in a JSON report.
    /// </summary>
    public static class EdgeCaseScenarios
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
            EffectID.eid_StrengthReduce,
            EffectID.eid_Withered,
            EffectID.eid_Blindness
        };

        public static string ReportPath(string repoRoot)
        {
            return Path.Combine(repoRoot, "dev_info", "fixtures", "edge-cases", "edge-case-report.json");
        }

        public static void EdgeCaseSmoke(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            var results = new List<CaseResult>();
            RunCase(game, repoRoot, "empty-inventory", results, EmptyInventory);
            RunCase(game, repoRoot, "full-inventory", results, FullInventory);
            RunCase(game, repoRoot, "zero-hp", results, ZeroHp);
            RunCase(game, repoRoot, "negative-values", results, NegativeValues);
            RunCase(game, repoRoot, "container-nesting", results, ContainerNesting);
            RunCase(game, repoRoot, "effect-combination", results, EffectCombination);
            RunCase(game, repoRoot, "empty-field-turns", results, EmptyFieldTurns);
            RunCase(game, repoRoot, "field-edge", results, FieldEdge);
            RunCase(game, repoRoot, "many-creatures", results, ManyCreatures);
            RunCase(game, repoRoot, "dead-entity", results, DeadEntity);

            WriteReport(repoRoot, results);

            int fail = 0;
            for (int i = 0; i < results.Count; i++) {
                string mark = results[i].Status == "pass" ? "OK  " : (results[i].Status == "bug" ? "BUG " : "FAIL");
                Console.WriteLine("  " + mark + " " + results[i].Name + " — " + results[i].Detail);
                if (results[i].Status == "fail") {
                    fail++;
                }
            }
            if (fail > 0) {
                throw new InvalidOperationException("edge-cases: " + fail + " failure(s)");
            }
        }

        private delegate string CaseBody(NWGameSpace game, string repoRoot);

        private static void RunCase(NWGameSpace game, string repoRoot, string name, List<CaseResult> results, CaseBody body)
        {
            try {
                SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
                game.LoadGame(SaveLoadScenarios.TestSlot);
                string detail = body(game, repoRoot);
                results.Add(CaseResult.Pass(name, detail));
            } catch (Exception ex) {
                if (ex.Message != null && ex.Message.StartsWith("bug:", StringComparison.Ordinal)) {
                    results.Add(CaseResult.Bug(name, ex.Message.Substring(4).Trim()));
                } else {
                    results.Add(CaseResult.Fail(name, ex.Message));
                }
            }
        }

        private static string EmptyInventory(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            p.DropAll();
            if (p.Items.Count != 0) {
                throw new InvalidOperationException("DropAll left " + p.Items.Count + " items");
            }

            bool useThrew = false;
            try {
                p.UseItem(null, null);
            } catch (Exception) {
                useThrew = true;
            }

            bool wearThrew = false;
            try {
                p.WearItem(null);
            } catch (Exception) {
                wearThrew = true;
            }

            SaveLoadCheck(game, repoRoot);
            return "items=0 useNullThrows=" + useThrew + " wearNullThrows=" + wearThrew;
        }

        private static string FullInventory(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            int spawned = 0;
            int rejected = 0;
            // Heavy armor until weight gate rejects
            for (int i = 0; i < 80; i++) {
                Item probe = CreateLooseItem(game, "PlateMail");
                if (probe == null) {
                    probe = CreateLooseItem(game, "ScaleMail");
                }
                if (probe == null) {
                    throw new InvalidOperationException("could not create heavy item");
                }
                if (p.CanTake(probe, false)) {
                    p.AddItem(probe);
                    spawned++;
                } else {
                    rejected++;
                    try { probe.Dispose(); } catch (Exception) { }
                    break;
                }
            }
            if (rejected == 0 && spawned < 2) {
                throw new InvalidOperationException("weight gate never rejected (spawned=" + spawned + ")");
            }
            float weight = p.TotalWeight;
            float max = p.MaxItemsWeight;
            SaveLoadCheck(game, repoRoot);
            return "spawned=" + spawned + " rejected=" + rejected + " weight=" +
                weight.ToString("0.#", CultureInfo.InvariantCulture) + "/" +
                max.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string ZeroHp(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            p.HPCur = 0;
            p.Death("edge-case zero hp", null);
            if (p.State != CreatureState.Dead) {
                throw new InvalidOperationException("expected Dead after Death(), got " + p.State);
            }
            // Save/load after player death is undefined; verify ProcessGameStep does not throw.
            game.ProcessGameStep();
            return "state=Dead hp=" + p.HPCur + " stepOk=true";
        }

        private static string NegativeValues(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            p.HPCur = -5;
            p.Satiety = -100;
            // Document clamping behavior (Satiety setter only caps max; HPCur caps max only).
            if (p.HPCur >= 0) {
                throw new InvalidOperationException("bug: HPCur clamped unexpectedly to " + p.HPCur);
            }
            if (p.Satiety >= 0) {
                throw new InvalidOperationException("bug: Satiety clamped unexpectedly to " + p.Satiety);
            }
            SaveLoadCheck(game, repoRoot);
            Player after = game.Player;
            return "hp=" + after.HPCur + " satiety=" + after.Satiety + " (no lower clamp; persisted)";
        }

        private static string ContainerNesting(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            Item outer = TestWorld.SpawnItem(p, "Flask", 1, true);
            Item mid = TestWorld.SpawnItem(p, "Flask", 1, true);
            if (outer == null || mid == null || !outer.Container || !mid.Container) {
                throw new InvalidOperationException("Flask spawn/container failed");
            }
            TestWorld.SpawnItemInContainer(mid, "Torch", 1);
            // Nest mid flask inside outer (depth 2: outer → mid → torch)
            p.Items.Extract(mid);
            mid.Owner = outer;
            outer.Contents.Add(mid);

            if (outer.Contents.Count < 1) {
                throw new InvalidOperationException("outer contents empty after nest");
            }
            Item nested = (Item)outer.Contents[0];
            if (!nested.Container || nested.Contents.Count < 1) {
                throw new InvalidOperationException("mid nest/torch missing");
            }

            int before = CountNested(p);
            SaveLoadCheck(game, repoRoot);
            int after = CountNested(game.Player);
            if (after < before) {
                throw new InvalidOperationException("nested count lost: " + before + " → " + after);
            }
            return "nestedItems=" + after + " depth>=2";
        }

        private static string EffectCombination(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            int applied = 0;
            for (int i = 0; i < SafeEffects.Length; i++) {
                TestWorld.ApplyEffect(p, SafeEffects[i]);
                applied++;
            }
            if (p.Effects.Count < 10) {
                throw new InvalidOperationException("expected >=10 effects, got " + p.Effects.Count);
            }
            int before = p.Effects.Count;
            TestWorld.RunTurns(game, 3);
            SaveLoadCheck(game, repoRoot);
            return "applied=" + applied + " effectsBeforeTurns=" + before +
                " afterLoad=" + game.Player.Effects.Count;
        }

        private static string EmptyFieldTurns(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            NWField fld = p.CurrentField;
            // Remove non-player hostiles if any; village may have NPCs — just run turns.
            int turnBefore = p.Turn;
            TestWorld.RunTurns(game, 5);
            if (p.Turn <= turnBefore) {
                throw new InvalidOperationException("turn did not advance");
            }
            if (p.CurrentField == null) {
                throw new InvalidOperationException("player left field");
            }
            SaveLoadCheck(game, repoRoot);
            return "turns+" + (p.Turn - turnBefore) + " creatures=" + fld.Creatures.Count;
        }

        private static string FieldEdge(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            NWField fld = p.CurrentField;
            // Walk toward western edge; MoveTo must not throw OOB
            for (int i = 0; i < StaticData.FieldWidth + 5; i++) {
                int nx = p.PosX - 1;
                int ny = p.PosY;
                if (nx < 0 || ny < 0 || nx >= StaticData.FieldWidth || ny >= StaticData.FieldHeight) {
                    // Attempt move that would go OOB via DoPlayerAction
                    game.DoPlayerAction(CreatureAction.caMove, Directions.DtWest);
                    game.ProcessGameStep();
                    break;
                }
                if (p.CanMove(fld, nx, ny)) {
                    game.DoPlayerAction(CreatureAction.caMove, Directions.DtWest);
                    game.ProcessGameStep();
                } else {
                    game.DoPlayerAction(CreatureAction.caMove, Directions.DtWest);
                    game.ProcessGameStep();
                    break;
                }
            }
            if (p.PosX < 0 || p.PosY < 0 ||
                p.PosX >= StaticData.FieldWidth || p.PosY >= StaticData.FieldHeight) {
                throw new InvalidOperationException("player OOB at (" + p.PosX + "," + p.PosY + ")");
            }
            SaveLoadCheck(game, repoRoot);
            return "pos=(" + p.PosX + "," + p.PosY + ") inBounds";
        }

        private static string ManyCreatures(NWGameSpace game, string repoRoot)
        {
            Player p = game.Player;
            NWField fld = p.CurrentField;
            int cid = GlobalVars.nwrGame.FindDataEntry("Rat").GUID;
            int added = 0;
            for (int y = 1; y < StaticData.FieldHeight - 1 && added < 120; y++) {
                for (int x = 1; x < StaticData.FieldWidth - 1 && added < 120; x++) {
                    if (x == p.PosX && y == p.PosY) {
                        continue;
                    }
                    if (fld.FindCreature(x, y) != null) {
                        continue;
                    }
                    if (!p.CanMove(fld, x, y)) {
                        continue;
                    }
                    NWCreature cr = game.AddCreatureEx(p.LayerID, fld.Coords.X, fld.Coords.Y, x, y, cid);
                    if (cr != null) {
                        added++;
                    }
                }
            }
            if (added < 100) {
                throw new InvalidOperationException("only placed " + added + " rats (need 100+)");
            }
            TestWorld.RunTurns(game, 1);
            SaveLoadCheck(game, repoRoot);
            return "rats=" + added + " fieldCreatures=" + game.Player.CurrentField.Creatures.Count;
        }

        private static string DeadEntity(NWGameSpace game, string repoRoot)
        {
            NWCreature rat = TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.nwrGame.FindDataEntry("Rat").GUID);
            int rx = rat.PosX;
            int ry = rat.PosY;
            rat.Death("edge-case kill", game.Player);
            if (rat.State != CreatureState.Dead) {
                throw new InvalidOperationException("rat not dead");
            }
            // Turn processing with a corpse on the field must not crash
            TestWorld.RunTurns(game, 2);
            Item potion = TestWorld.SpawnItem(game.Player, "Potion_Curing", 1, true);
            if (potion != null) {
                game.Player.UseItem(potion, null);
            }
            SaveLoadCheck(game, repoRoot);
            return "ratDead at (" + rx + "," + ry + ") turns+use+saveOk";
        }

        private static Item CreateLooseItem(NWGameSpace game, string sign)
        {
            try {
                int id = GlobalVars.nwrGame.FindDataEntry(sign).GUID;
                var item = new Item(game, null);
                item.CLSID = id;
                item.Count = 1;
                item.Identified = true;
                return item;
            } catch (Exception) {
                return null;
            }
        }

        private static int CountNested(Player player)
        {
            int total = 0;
            for (int i = 0; i < player.Items.Count; i++) {
                total += 1 + CountContents(player.Items[i]);
            }
            return total;
        }

        private static int CountContents(Item item)
        {
            if (item == null || !item.Container) {
                return 0;
            }
            int n = item.Contents.Count;
            for (int i = 0; i < item.Contents.Count; i++) {
                n += CountContents((Item)item.Contents[i]);
            }
            return n;
        }

        private static void SaveLoadCheck(NWGameSpace game, string repoRoot)
        {
            PlayerSnapshot before = PlayerSnapshot.Capture(game.Player);
            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            // Name/layer/pos should survive; HP may change for negative-values case — compare pos/layer/name
            PlayerSnapshot after = PlayerSnapshot.Capture(game.Player);
            if (before.Name != after.Name || before.LayerID != after.LayerID) {
                throw new InvalidOperationException("save/load identity mismatch");
            }
            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok");
        }

        private static void WriteReport(string repoRoot, List<CaseResult> results)
        {
            string path = ReportPath(repoRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var jb = new JsonBuilder();
            jb.BeginObject();
            jb.Property("generated", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            jb.Property("issue", 17);
            jb.Key("cases");
            jb.BeginArray();
            int pass = 0, fail = 0, bug = 0;
            for (int i = 0; i < results.Count; i++) {
                jb.BeginObject();
                jb.Property("name", results[i].Name);
                jb.Property("status", results[i].Status);
                jb.Property("detail", results[i].Detail ?? "");
                jb.EndObject();
                if (results[i].Status == "pass") pass++;
                else if (results[i].Status == "bug") bug++;
                else fail++;
            }
            jb.EndArray();
            jb.Key("summary");
            jb.BeginObject();
            jb.Property("pass", pass);
            jb.Property("fail", fail);
            jb.Property("bug", bug);
            jb.Property("total", results.Count);
            jb.EndObject();
            jb.EndObject();
            File.WriteAllText(path, jb.ToString(), Encoding.UTF8);
            Console.WriteLine("wrote " + path + " pass=" + pass + " fail=" + fail + " bug=" + bug);
        }

        private struct CaseResult
        {
            public string Name;
            public string Status;
            public string Detail;

            public static CaseResult Pass(string name, string detail)
            {
                return new CaseResult { Name = name, Status = "pass", Detail = detail };
            }

            public static CaseResult Fail(string name, string detail)
            {
                return new CaseResult { Name = name, Status = "fail", Detail = detail };
            }

            public static CaseResult Bug(string name, string detail)
            {
                return new CaseResult { Name = name, Status = "bug", Detail = detail };
            }
        }
    }
}
