using System;
using System.Reflection;
using NUnit.Framework;
using BSLib;
using NWR.Creatures;
using NWR.Game;
using NWR.Game.Types;
using NWR.GUI;
using NWR.GUI.Controls;
using NWR.Items;
using ZRLib.Core;

namespace NWR.Tests
{
    /// <summary>
    /// Golden-value tests for the numeric-semantics sweep (wave issue #79):
    /// cs/equality-on-floats (NWCreature.cs) and cs/loss-of-precision
    /// (NWCreature.cs, ProgressBar.cs, SoundEngine.cs, BaseMainWindow.cs).
    /// Each test pins the pre-change value as a golden constant together with
    /// its inputs; discriminating cases (overflow / lost fraction) prove the bug.
    /// Where the production expression is not reachable from a unit test
    /// (UI-bound setters, the paint loop), verbatim pre-/post-change replicas
    /// are pinned instead and the source line is cited.
    /// </summary>
    [TestFixture]
    public class NumericRoundtripTests
    {
        private sealed class ProbeCreature : NWCreature
        {
            public ProbeCreature(NWGameSpace space)
                : base(space, null)
            {
            }

            public int ProjectileToHitAgainst(NWCreature enemy, Item projectile)
            {
                return CalcAttackInfo(AttackKind.Melee, enemy, null, projectile).ToHit;
            }
        }

        // ---- cs/equality-on-floats ----------------------------------------

        // Site: project/Creatures/NWCreature.cs:4355 (manifest :4351).
        // Div-zero guard on the divisor of enemy.Level / div. Fix applied:
        // Math.Abs(div) < float.Epsilon, provably identical to == 0.0f for
        // binary32 (the only float with magnitude < float.Epsilon is +/-0).
        [Test]
        public void AttackExp_DivisorGuard_PinsZeroBoundary()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new NWCreature(game, null);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new NWCreature(game, null);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);
            enemy.HPMax_Renamed = 40;

            // div = Level + enemy.Level == 0 exactly -> guard path (dLevel = 0,
            // then snapped to 1 by the result-check below it).
            attacker.Level = 0;
            enemy.Level = 0;
            Assert.AreEqual(40, attacker.GetAttackExp(enemy));

            // Boundary just above zero: div = 1 (smallest nonzero; levels are
            // ints) -> no guard, dLevel = 1/1 = 1.
            attacker.Level = 0;
            enemy.Level = 1;
            Assert.AreEqual(40, attacker.GetAttackExp(enemy));

            // Ordinary ratios, no guard: dLevel = 1/4 and 2/4.
            attacker.Level = 3;
            enemy.Level = 1;
            Assert.AreEqual(10, attacker.GetAttackExp(enemy));
            attacker.Level = 2;
            enemy.Level = 2;
            Assert.AreEqual(20, attacker.GetAttackExp(enemy));
        }

        // Site: project/Creatures/NWCreature.cs:4357 (manifest :4353) -- DEFERRED
        // (defer-owner). This is a result-check on the computed ratio, NOT a
        // div-zero guard: exact zero is treated specially (snap to 1 = full
        // experience). The check is left unchanged pending owner decision; this
        // test pins current behavior. Note: float.Epsilon would be provably
        // identical here too (min nonzero |ratio| = 1/2^31 >> 1.4e-45); any
        // practical tolerance WOULD change behavior.
        [Test]
        public void AttackExp_ZeroResultSnap_PinsCurrentSemantics()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new NWCreature(game, null);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new NWCreature(game, null);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);
            enemy.HPMax_Renamed = 40;

            // Exact-zero result: enemy.Level = 0 -> dLevel == 0f -> snap to 1.
            attacker.Level = 5;
            enemy.Level = 0;
            Assert.AreEqual(40, attacker.GetAttackExp(enemy));

            // Just-above-zero result: dLevel = 1/4 = 0.25 -> NO snap.
            attacker.Level = 3;
            enemy.Level = 1;
            Assert.AreEqual(10, attacker.GetAttackExp(enemy));
        }

        // Site: project/Creatures/NWCreature.cs:4375 (manifest :4371).
        // Div-zero guard on s_daf (own damage factor). Fix applied:
        // Math.Abs(s_daf) < float.Epsilon (provably identical, see above).
        [Test]
        public void AttackRate_SourceDamageGuard_PinsZeroBoundary()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new NWCreature(game, null);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new NWCreature(game, null);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            // s_daf == 0 exactly (DBMin + DBMax == 0) -> guard: s_af = 1.
            // t_daf = (2+2)/2*1 = 2 -> t_af = HPCur/2 = 20.
            // kinsfolks=1 -> kf = 1/(0.6+1) = 0.625f; result = (1/21)/0.625.
            attacker.DBMin = 0;
            attacker.DBMax = 0;
            attacker.Attacks = 1;
            enemy.DBMin = 2;
            enemy.DBMax = 2;
            enemy.Attacks = 1;
            attacker.HPCur = 40;
            Assert.AreEqual(0.0761905f, attacker.GetAttackRate(enemy, 1), 1e-6f);

            // Boundary just above zero: s_daf = (0+1)/2*1 = 0.5 -> NO guard,
            // s_af = enemy.HPCur/0.5 = 20 -> (20/40)/0.625 = 0.8.
            attacker.DBMin = 0;
            attacker.DBMax = 1;
            enemy.HPCur = 10;
            Assert.AreEqual(0.8f, attacker.GetAttackRate(enemy, 1), 1e-6f);
        }

        // Site: project/Creatures/NWCreature.cs:4381 (manifest :4377).
        // Div-zero guard on t_daf (target damage factor). Fix applied:
        // Math.Abs(t_daf) < float.Epsilon (provably identical, see above).
        [Test]
        public void AttackRate_TargetDamageGuard_PinsZeroBoundary()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new NWCreature(game, null);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new NWCreature(game, null);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            // s_daf = (2+4)/2*1 = 3 -> s_af = enemy.HPCur/3 = 10.
            // t_daf == 0 exactly -> guard: t_af = 1.
            // result = (10/11)/0.625 = 1.4545455f.
            attacker.DBMin = 2;
            attacker.DBMax = 4;
            attacker.Attacks = 1;
            enemy.HPCur = 30;
            enemy.DBMin = 0;
            enemy.DBMax = 0;
            enemy.Attacks = 1;
            Assert.AreEqual(1.4545455f, attacker.GetAttackRate(enemy, 1), 1e-6f);

            // Boundary just above zero: t_daf = 0.5 -> NO guard,
            // t_af = HPCur/0.5 = 40; s guard: s_af = 1 -> (1/41)/0.625.
            attacker.DBMin = 0;
            attacker.DBMax = 0;
            attacker.HPCur = 20;
            enemy.DBMin = 0;
            enemy.DBMax = 1;
            Assert.AreEqual(0.0390244f, attacker.GetAttackRate(enemy, 1), 1e-6f);

            // Both guards: s_af = t_af = 1 -> (1/2)/0.625 = 0.8.
            enemy.DBMin = 0;
            enemy.DBMax = 0;
            Assert.AreEqual(0.8f, attacker.GetAttackRate(enemy, 1), 1e-6f);
        }

        // ---- cs/loss-of-precision, class B1: int overflow before double cast

        // Site: project/Creatures/NWCreature.cs:1787 -- 2 * enemy.ArmorClass.
        [Test]
        public void ProjectileToHit_ArmorTerm_CastBeforeMultiply()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            attacker.Strength = 14; // <= 18: no strength bonus term at play
            attacker.Luck = 0;
            var enemy = new ProbeCreature(game);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            int knifeId = game.FindDataEntry("FlintKnife").GUID;
            var knife = new Item(game, attacker);
            knife.CLSID = knifeId;
            knife.Count = 1;

            // In-range golden through the REAL code (delta cancels the
            // ability/parry terms): 2 * 8 = 16, identical before and after.
            enemy.ArmorClass = 0;
            int toHitNoArmor = attacker.ProjectileToHitAgainst(enemy, knife);
            enemy.ArmorClass = 8;
            int toHitArmored = attacker.ProjectileToHitAgainst(enemy, knife);
            Assert.AreEqual(16, toHitNoArmor - toHitArmored);

            // Discriminating case through the REAL code: ArmorClass large
            // enough that `2 * AC` overflows int. Pre-change the wrapped
            // negative term (2*1.5e9 -> -1294967296) makes ToHit overflow
            // POSITIVE -> ClampHitChance returns 100; post-change the exact
            // 3e9 term makes it overflow NEGATIVE -> clamp returns 0.
            // AC=2^30 is the overflow threshold for `2 * AC`; 1.5e9 wraps.
            enemy.ArmorClass = 1500000000;
            int toHitBigArmor = attacker.ProjectileToHitAgainst(enemy, knife);
            Assert.AreEqual(0, toHitBigArmor); // post-change; pre-change was 100

            // Golden replica of the pre-change multiply (inputs included) --
            // documents the overflow the production clamp above discriminates:
            int bigAC = 1500000000;
            int oldMul = unchecked(2 * bigAC);      // pre-change: int x int
            double newMul = 2.0 * bigAC;            // post-change: cast first
            Assert.AreEqual(-1294967296, oldMul);   // pinned overflowed golden
            Assert.AreEqual(3000000000.0, newMul);

            // Full-expression equivalence for in-range inputs, golden 32 both
            // ways (inputs: Strength=70, ArmorClass=8, Luck=50, Bonus=3):
            int oldHit = (int)Math.Round(70 / 7.0 - 2 * 8 + 50 / 10.0 + 30.0 + 3.0);
            int newHit = (int)Math.Round(70 / 7.0 - 2.0 * 8 + 50 / 10.0 + 30.0 + 3.0);
            Assert.AreEqual(32, oldHit);
            Assert.AreEqual(32, newHit);
        }

        // Site: project/GUI/Controls/ProgressBar.cs:59 -- fPos * 100.
        // The production Pos setter repaints MainWindow (not unit-testable),
        // so the arithmetic lives in ProgressBar.PercentOf (internal static
        // seam) and THIS test calls the production method.
        [Test]
        public void ProgressBarPercent_Overflow_CastBeforeMultiply()
        {
            // Normal range: identical results (behavior preserved).
            Assert.AreEqual(50, ProgressBar.PercentOf(50, 100));

            // Discriminating overflow: fPos = 100M > ~21.47M threshold.
            // Production (post-change) is correct; the replica below pins
            // the pre-change overflowed golden (7).
            Assert.AreEqual(50, ProgressBar.PercentOf(100000000, 200000000)); // fixed
            Assert.AreEqual(7, OldProgressPercent(100000000, 200000000));     // golden: overflowed

            // Overflow threshold: fPos*100 <= int.MaxValue up to 21474836.
            Assert.AreEqual(1, ProgressBar.PercentOf(21474836, int.MaxValue));
            Assert.AreEqual(-1, OldProgressPercent(21474837, int.MaxValue)); // golden: overflowed
            Assert.AreEqual(1, ProgressBar.PercentOf(21474837, int.MaxValue));
        }

        // Verbatim pre-change replica, kept ONLY as a documenting golden of
        // the overflowed value the production seam now avoids.
        private static int OldProgressPercent(int fPos, int max)
        {
            return (int)((long)Math.Round((double)unchecked(fPos * 100) / (double)max));
        }

        // Site: project/Game/SoundEngine.cs:393 -- dx * dx + dy * dy.
        [Test]
        public void DistanceVolume_LargeCoordinates_NoIntOverflow()
        {
            var method = typeof(SoundEngine).GetMethod("ApplyDistanceVolume", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "SoundEngine.ApplyDistanceVolume not found");

            // In-range golden through the REAL code (old == new):
            // dist = sqrt(50^2+50^2) = 70.7107, scale = 1 - 70.7107/80,
            // (int)(255 * 0.11611652) = 29.
            object v1 = method.Invoke(null, new object[] { 255, new ExtPoint(0, 0), new ExtPoint(50, 50) });
            Assert.AreEqual(29, (int)v1);

            // Discriminating case through the REAL code: dx=dy=100000 made the
            // int math overflow (pre-change garbage); post-change
            // dist = sqrt(2)*1e5 = 141421.36 >= MaxDistance 80 -> silence.
            object v2 = method.Invoke(null, new object[] { 255, new ExtPoint(0, 0), new ExtPoint(100000, 100000) });
            Assert.AreEqual(0, (int)v2);

            // Golden replica proving the pre-change bug at the dist level
            // (inputs included): dx = dy = 100000.
            int dx = 100000;
            int dy = 100000;
            int oldSq = unchecked(dx * dx);                          // pinned: 1410065408
            Assert.AreEqual(1410065408, oldSq);
            double oldDist = Math.Sqrt(unchecked(oldSq + oldSq));    // sqrt(-1474836480)
            Assert.IsTrue(double.IsNaN(oldDist));                    // pre-change: garbage
            double newDist = Math.Sqrt((double)dx * dx + (double)dy * dy);
            Assert.AreEqual(141421.3562373095, newDist, 1e-6);       // sqrt(2)*1e5
        }

        // ---- cs/loss-of-precision, class B2: fraction lost in int division

        // Site: project/Creatures/NWCreature.cs:514 -- MPCur / MPMax.
        [Test]
        public void ManaPercent_KeepsFraction()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var creature = new NWCreature(game, null);
            creature.InitEx(GlobalVars.cid_Viking, true, false);

            // Discriminating case through the REAL Props: 33/100 truncated to
            // 0 pre-change (pinned golden below), 33 post-change.
            creature.MPMax = 100;
            creature.MPCur = 33;
            Assert.AreEqual(33, GetManaPercent(creature));

            // Preserved cases: 100/100 -> 100, 0/100 -> 0, guard MPMax == 0 -> 0.
            creature.MPMax = 100;
            creature.MPCur = 100;
            Assert.AreEqual(100, GetManaPercent(creature));
            creature.MPCur = 0;
            Assert.AreEqual(0, GetManaPercent(creature));
            creature.MPMax = 0;
            Assert.AreEqual(0, GetManaPercent(creature));

            // Rounding-mode pin (unchanged Math.Round default, ToEven):
            // 1/8 -> 12.5 -> 12; 2/3 -> 66.67 -> 67.
            creature.MPMax = 8;
            creature.MPCur = 1;
            Assert.AreEqual(12, GetManaPercent(creature));
            creature.MPMax = 3;
            creature.MPCur = 2;
            Assert.AreEqual(67, GetManaPercent(creature));

            // Golden replicas pinning the pre-change values (inputs included).
            Assert.AreEqual(0, OldManaPercent(33, 100));
            Assert.AreEqual(0, OldManaPercent(1, 8));
            Assert.AreEqual(0, OldManaPercent(2, 3));
        }

        private static int OldManaPercent(int mpCur, int mpMax)
        {
            return (int)Math.Round((mpCur / mpMax) * 100.0f);
        }

        private static int GetManaPercent(NWCreature creature)
        {
            string mpLabel = BaseLocale.GetStr(RS.rs_MP) + ": ";
            StringList props = creature.Props;
            int fallback = -1;
            for (int i = 0; i < props.Count; i++) {
                string line = props[i];
                if (line.EndsWith(" %")) {
                    fallback = i; // last percent line is the MP line
                    if (line.StartsWith(mpLabel)) {
                        return int.Parse(line.Substring(mpLabel.Length, line.Length - mpLabel.Length - 2));
                    }
                }
            }
            if (fallback >= 0) {
                string line = props[fallback];
                int sep = line.IndexOf(": ");
                return int.Parse(line.Substring(sep + 2, line.Length - sep - 4));
            }
            Assert.Fail("MP line not found in Props");
            return -1;
        }

        // Site: project/GUI/BaseMainWindow.cs:353 -- 1000 * fFrameCount / elapsed.
        // BaseMainWindow.Repaint() needs a live screen, so the arithmetic
        // lives in BaseMainWindow.ComputeFps (internal static seam) and THIS
        // test calls the production method. FPS is declared as float.
        [Test]
        public void FpsCounter_KeepsFraction()
        {
            // Whole-second golden (old == new).
            Assert.AreEqual(60f, BaseMainWindow.ComputeFps(60, 1000));

            // Discriminating case: fraction 1.5 lost by integer division -> 1.
            Assert.AreEqual(1.5f, BaseMainWindow.ComputeFps(3, 2000)); // fixed
            Assert.AreEqual(1f, OldFps(3, 2000));                      // golden: truncated

            // Truncation (not rounding) pinned: 1000000/1001 = 999.000999...
            Assert.AreEqual(999f, OldFps(1000, 1001));
            Assert.AreEqual(999.00098f, BaseMainWindow.ComputeFps(1000, 1001), 1e-3f);
        }

        // Verbatim pre-change replica, kept ONLY as a documenting golden of
        // the truncated value the production seam now avoids.
        private static float OldFps(int frameCount, long elapsed)
        {
            return 1000 * frameCount / elapsed;
        }
    }
}
