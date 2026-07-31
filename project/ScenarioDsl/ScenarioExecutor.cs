using System;
using System.Globalization;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using NWR.Universe;

namespace NWR.ScenarioDsl
{
    public static class ScenarioExecutor
    {
        public static void Run(Scenario scenario, IScenarioEnv env, string repoRoot)
        {
            if (scenario == null) {
                throw new ArgumentNullException("scenario");
            }
            if (env == null) {
                throw new ArgumentNullException("env");
            }

            var ctx = new ScenarioContext {
                RepoRoot = repoRoot,
                Env = env,
                Game = env.Bootstrap(repoRoot)
            };

            for (int i = 0; i < scenario.Steps.Count; i++) {
                ExecuteStep(ctx, scenario.Steps[i], i);
            }
        }

        private static void ExecuteStep(ScenarioContext ctx, ScenarioStep step, int index)
        {
            try {
                switch (step.Op) {
                    case ScenarioOp.Param:
                        ctx.Params[step.Arg] = step.CheckFailMessage ?? "";
                        break;
                    case ScenarioOp.LoadFixture:
                        envLoad(ctx, ctx.Resolve(step.Arg));
                        break;
                    case ScenarioOp.SpawnItem:
                        SpawnItem(ctx, ctx.Resolve(step.Arg), step.N, step.Flag);
                        break;
                    case ScenarioOp.SpawnCreature:
                        SpawnCreature(ctx, ctx.Resolve(step.Arg));
                        break;
                    case ScenarioOp.UseItem:
                        UseLastItem(ctx);
                        break;
                    case ScenarioOp.ApplyEffect:
                        ApplyEffect(ctx, ctx.Resolve(step.Arg));
                        break;
                    case ScenarioOp.WaitTurns:
                        WaitTurns(ctx, step.N);
                        break;
                    case ScenarioOp.SaveGame:
                        ctx.Game.SaveGame(ctx.Env.TestSlot);
                        break;
                    case ScenarioOp.LoadGame:
                        ctx.Game.LoadGame(ctx.Env.TestSlot);
                        break;
                    case ScenarioOp.SaveLoadRoundtrip:
                        SaveLoadRoundtrip(ctx);
                        break;
                    case ScenarioOp.Capture:
                        ctx.Capture(ctx.Resolve(step.Arg));
                        break;
                    case ScenarioOp.HalfHp:
                        HalfHp(ctx);
                        break;
                    case ScenarioOp.Check:
                        RunCheck(ctx, step);
                        break;
                    case ScenarioOp.RequireLog:
                        RequireLog(ctx, ctx.Resolve(step.Arg));
                        break;
                    default:
                        throw new InvalidOperationException("unknown op " + step.Op);
                }
            } catch (Exception ex) {
                throw new InvalidOperationException(
                    "step " + index + " (" + step.Op + "): " + ex.Message, ex);
            }
        }

        private static void envLoad(ScenarioContext ctx, string fixture)
        {
            ctx.Env.CopyFixtureToSlot(ctx.RepoRoot, fixture, ctx.Env.TestSlot);
            ctx.Game.LoadGame(ctx.Env.TestSlot);
        }

        private static void SpawnItem(ScenarioContext ctx, string sign, int count, bool identified)
        {
            if (ctx.Player == null) {
                throw new InvalidOperationException("no player");
            }
            int id = GlobalVars.nwrGame.FindDataEntry(sign).GUID;
            int before = ctx.Player.Items.Count;
            Item.GenItem(ctx.Player, id, Math.Max(1, count), identified);
            if (ctx.Player.Items.Count <= before) {
                throw new InvalidOperationException("could not spawn " + sign);
            }
            ctx.LastItem = ctx.Player.Items[ctx.Player.Items.Count - 1];
        }

        private static void SpawnCreature(ScenarioContext ctx, string signOrId)
        {
            if (ctx.Game == null || ctx.Player == null) {
                throw new InvalidOperationException("no game");
            }
            int creatureId;
            if (!int.TryParse(signOrId, NumberStyles.Integer, CultureInfo.InvariantCulture, out creatureId)) {
                creatureId = GlobalVars.nwrGame.FindDataEntry(signOrId).GUID;
            }
            ctx.LastCreature = PlaceCreatureNearPlayer(ctx.Game, creatureId);
        }

        private static NWCreature PlaceCreatureNearPlayer(NWGameSpace game, int creatureId)
        {
            Player player = game.Player;
            NWField fld = player.CurrentField;
            int px = -1;
            int py = -1;
            for (int dist = 1; dist <= 3 && px < 0; dist++) {
                for (int dx = -dist; dx <= dist && px < 0; dx++) {
                    for (int dy = -dist; dy <= dist; dy++) {
                        if (dx == 0 && dy == 0) {
                            continue;
                        }
                        if (Math.Abs(dx) != dist && Math.Abs(dy) != dist) {
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
            }
            if (px < 0) {
                throw new InvalidOperationException("no nearby tile for creature " + creatureId);
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

        private static void UseLastItem(ScenarioContext ctx)
        {
            if (ctx.LastItem == null) {
                throw new InvalidOperationException("UseItem with no LastItem; call SpawnItem first");
            }
            ctx.Player.UseItem(ctx.LastItem, null);
        }

        private static void ApplyEffect(ScenarioContext ctx, string effectName)
        {
            EffectID eid = ParseEffect(effectName);
            ctx.Player.AddEffect(eid, ItemState.is_Normal, EffectAction.ea_Persistent, false, "");
        }

        private static EffectID ParseEffect(string name)
        {
            if (string.IsNullOrEmpty(name)) {
                throw new InvalidOperationException("empty effect name");
            }
            string n = name.Trim();
            if (!n.StartsWith("eid_", StringComparison.OrdinalIgnoreCase)) {
                n = "eid_" + n;
            }
            try {
                return (EffectID)Enum.Parse(typeof(EffectID), n, true);
            } catch (Exception) {
                throw new InvalidOperationException("unknown EffectID: " + name);
            }
        }

        private static void WaitTurns(ScenarioContext ctx, int turns)
        {
            for (int i = 0; i < turns; i++) {
                ctx.Game.DoPlayerAction(CreatureAction.caWait, 0);
                ctx.Game.ProcessGameStep();
            }
        }

        private static void HalfHp(ScenarioContext ctx)
        {
            Player p = ctx.Player;
            if (p.HPCur >= p.HPMax_Renamed) {
                p.HPCur = Math.Max(1, p.HPMax_Renamed / 2);
            }
        }

        private static void SaveLoadRoundtrip(ScenarioContext ctx)
        {
            int turnBefore = ctx.Player.Turn;
            int hpBefore = ctx.Player.HPCur;
            int effectsBefore = ctx.Player.Effects.Count;
            ctx.Game.SaveGame(ctx.Env.TestSlot);
            ctx.Game.LoadGame(ctx.Env.TestSlot);
            if (ctx.Player.Turn != turnBefore || ctx.Player.HPCur != hpBefore || ctx.Player.Effects.Count != effectsBefore) {
                throw new InvalidOperationException(
                    "saveLoadRoundtrip mismatch turn/hp/effects " +
                    turnBefore + "/" + hpBefore + "/" + effectsBefore + " vs " +
                    ctx.Player.Turn + "/" + ctx.Player.HPCur + "/" + ctx.Player.Effects.Count);
            }
        }

        private static void RunCheck(ScenarioContext ctx, ScenarioStep step)
        {
            if (step.CheckFunc != null) {
                if (!step.CheckFunc(ctx)) {
                    throw new InvalidOperationException(step.CheckFailMessage ?? "check failed");
                }
                return;
            }

            string name = ctx.Resolve(step.Arg);
            string arg = step.CheckFailMessage != null ? ctx.Resolve(step.CheckFailMessage) : null;
            string key = name != null ? name.Trim().ToLowerInvariant() : "";

            bool ok;
            if (key == "turnadvanced") {
                ok = ctx.Player.Turn > ctx.GetCaptured("turn");
            } else if (key == "hpincreased") {
                ok = ctx.Player.HPCur > ctx.GetCaptured("hp");
            } else if (key == "effectsunchanged") {
                ok = ctx.Player.Effects.Count == ctx.GetCaptured("effects");
            } else if (key == "effectpresent") {
                EffectID eid = ParseEffect(arg);
                ok = ctx.Player.Effects.FindEffectByID(eid) != null;
            } else if (key == "effectabsent") {
                EffectID eid = ParseEffect(arg);
                ok = ctx.Player.Effects.FindEffectByID(eid) == null;
            } else {
                throw new InvalidOperationException("unknown check: " + name);
            }

            if (!ok) {
                throw new InvalidOperationException("check '" + name + "' failed");
            }
        }

        private static void RequireLog(ScenarioContext ctx, string joined)
        {
            if (string.IsNullOrEmpty(joined)) {
                return;
            }
            string[] markers = joined.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            ctx.Env.RequireLogMarkers(markers);
        }
    }
}
