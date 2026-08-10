using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Database;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using ZRLib.Core;

namespace NWR.Tests
{
    /// <summary>
    /// Characterization tests for the code-scanning wave
    /// "cs/dereferenced-value-may-be-null" (wave issue #80, manifest
    /// null-deref-manifest.txt, 14 sites). NO guard was chosen in this phase:
    /// every test pins what the CURRENT code does at the flagged dereference
    /// (throws / swallowed by an enclosing try/catch / silently skipped by an
    /// upstream guard), so the product owner can pick the guard from evidence.
    /// Sites that are provably unreachable (EditBox.cs:83, FindMedicineItem)
    /// or not exercisable from the server test host (GUI drag/pay handlers)
    /// carry no test here; they are marked dismiss/defer in the decision table.
    /// </summary>
    [TestFixture]
    public class NullDerefCharacterizationTests
    {
        private sealed class ProbeCreature : NWCreature
        {
            public ProbeCreature(NWGameSpace space)
                : base(space, null)
            {
            }

            public int ToHitWith(AttackKind kind, NWCreature enemy, Item weapon, Item projectile)
            {
                return CalcAttackInfo(kind, enemy, weapon, projectile).ToHit;
            }

            public int DamageWith(AttackKind kind, NWCreature enemy, Item weapon, Item projectile)
            {
                return CalcAttackInfo(kind, enemy, weapon, projectile).Damage;
            }
        }

        private static NWCreature NewViking(NWGameSpace game)
        {
            var creature = new NWCreature(game, null);
            creature.InitEx(GlobalVars.cid_Viking, true, false);
            return creature;
        }

        private static Item NewItem(NWGameSpace game, string sign)
        {
            var item = new Item(game, null);
            item.CLSID = game.FindDataEntry(sign).GUID;
            return item;
        }

        // Site: project/Creatures/NWCreature.cs:1799-1803 (manifest :1801, :1804).
        // CalcAttackInfo dereferences weapon.CLSID / weapon.Bonus when the
        // projectile is an Arrow or Bolt (the source is the null weapon
        // argument). The whole method body is inside try/catch
        // (NWCreature.cs:1775-1868), so today a null weapon NREs and is
        // swallowed: the method returns the default AttackInfo (ToHit == 0).
        [Test]
        public void CalcAttackInfo_NullWeaponWithArrow_SwallowedNre_ReturnsDefault()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            attacker.Strength = 14;
            attacker.Luck = 0;
            var enemy = NewViking(game);
            enemy.ArmorClass = 0;

            Item arrow = NewItem(game, "Arrow");
            Item knife = NewItem(game, "FlintKnife");

            // Control: the SAME Arrow projectile with a non-null weapon that is
            // not a bow computes a real ToHit (> 0). This proves the Arrow
            // branch itself is healthy, so the ToHit == 0 below can only come
            // from the swallowed null-weapon deref.
            Assert.Greater(attacker.ToHitWith(AttackKind.Melee, enemy, knife, arrow), 0);

            // Null weapon + Arrow: NRE at weapon.CLSID is caught by the method
            // try/catch and the default AttackInfo is returned.
            Assert.DoesNotThrow(() => attacker.ToHitWith(AttackKind.Melee, enemy, null, arrow));
            Assert.AreEqual(0, attacker.ToHitWith(AttackKind.Melee, enemy, null, arrow));
            Assert.AreEqual(0, attacker.DamageWith(AttackKind.Melee, enemy, null, arrow));
        }

        // Site: project/Creatures/NWCreature.cs:1802-1803 (manifest :1801, :1804).
        // Same null-weapon deref reached through a Bolt projectile.
        [Test]
        public void CalcAttackInfo_NullWeaponWithBolt_SwallowedNre_ReturnsDefault()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = NewViking(game);

            Item bolt = NewItem(game, "Bolt");

            Assert.DoesNotThrow(() => attacker.ToHitWith(AttackKind.Melee, enemy, null, bolt));
            Assert.AreEqual(0, attacker.ToHitWith(AttackKind.Melee, enemy, null, bolt));
        }

        // Site: project/Creatures/NWCreature.cs:2194 (manifest :2196).
        // AttackSpecialEffect dereferences `enemy` unguarded for several signs
        // (Preden :2113, Gorm :2016, Wight :2194, ...). The Cockatrice branch
        // is the only one with an `enemy != null` guard (:1970). There is no
        // try/catch in the method, so a null enemy NREs and propagates.
        [Test]
        public void AttackSpecialEffect_WightWithNullEnemy_ThrowsNre()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var wight = new NWCreature(game, null);
            wight.InitEx(game.FindDataEntry("Wight").GUID, true, false);

            Assert.Throws<NullReferenceException>(() => wight.AttackSpecialEffect(null));
        }

        // Documents the ONLY guarded deref of `enemy` in AttackSpecialEffect:
        // the Cockatrice branch checks `enemy != null` first, so a null enemy
        // passes through silently. This is the guard CodeQL points at for the
        // whole method ("as suggested by this null check").
        [Test]
        public void AttackSpecialEffect_CockatriceWithNullEnemy_IsGuarded()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var cockatrice = new NWCreature(game, null);
            cockatrice.InitEx(game.FindDataEntry("Cockatrice").GUID, true, false);

            Assert.DoesNotThrow(() => cockatrice.AttackSpecialEffect(null));
        }

        // Site: project/Creatures/NWCreature.cs:4233 (manifest :4232).
        // UseItem dereferences `item` (item.ItemContainer, item.Kind, ...)
        // with no null check and no try/catch in the method, so a null item
        // NREs and propagates to the caller.
        [Test]
        public void UseItem_NullItem_ThrowsNre()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            Player player = game.Player;

            Assert.Throws<NullReferenceException>(() => player.UseItem(null, null));
        }

        // Site: project/Creatures/NamesLib.cs:125, :128.
        // GenNorseName initializes `fRec = null` and only assigns it inside the
        // male / female branches of the gender switch. A gender that is neither
        // male nor female (sex csNone / csHermaphrodite map to gUndefined /
        // gNeutral via StaticData.GenderBySex) leaves fRec null, so the
        // `fRec.Name` / `fRec.Rus_name` deref NREs. No try/catch exists in
        // GenNorseName or its public caller GenerateName.
        [Test]
        public void GenerateName_SexNone_ThrowsNre()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var creature = new NWCreature(game, null);
            creature.Sex = CreatureSex.csNone;

            Assert.Throws<NullReferenceException>(() => game.GenerateName(creature, NamesLib.NameGen_NorseDic));
        }

        // Site: project/Database/NWDatabase.cs:157.
        // LoadXML reads root.SelectSingleNode("Entries") and the null check is
        // commented out (:153-155), so a resource without an Entries node NREs
        // at `entries.Attributes`. The whole method body is in try/catch
        // (:133-185), so today the load aborts silently (EntriesCount stays 0).
        // The control fixture (Entries Count="1") proves the fixture directory
        // is readable and that a present Entries node does get its Count read.
        [Test]
        public void LoadXML_MissingEntriesNode_AbortsWithoutThrowing()
        {
            string resourcesDir = Path.Combine(NWResourceManager.GetAppPath(), "resources");
            if (!Directory.Exists(resourcesDir)) {
                Directory.CreateDirectory(resourcesDir);
            }
            string missingPath = Path.Combine(resourcesDir, "__nwr_null_deref_missing.xml");
            string presentPath = Path.Combine(resourcesDir, "__nwr_null_deref_present.xml");
            try {
                File.WriteAllText(missingPath, "<RDB><Version Release=\"4\" Revision=\"47\"></Version></RDB>");
                File.WriteAllText(presentPath,
                    "<RDB><Version Release=\"4\" Revision=\"47\"></Version>" +
                    "<Entries Count=\"1\"><Entry Kind=\"ek_Information\" GUID=\"0\" Sign=\"CtrlEntry\"></Entry></Entries></RDB>");

                Assert.IsTrue(File.Exists(missingPath), "missing-entries fixture not written");
                Assert.IsTrue(File.Exists(presentPath), "present-entries fixture not written");

                var withEntries = new NWDatabase();
                Assert.DoesNotThrow(() => withEntries.LoadXML(Path.GetFileName(presentPath)));
                Assert.AreEqual(1, withEntries.EntriesCount);

                var missingEntries = new NWDatabase();
                Assert.DoesNotThrow(() => missingEntries.LoadXML(Path.GetFileName(missingPath)));
                Assert.AreEqual(0, missingEntries.EntriesCount);
            } finally {
                try { File.Delete(missingPath); } catch (Exception) { }
                try { File.Delete(presentPath); } catch (Exception) { }
            }
        }

        // Site: project/Game/Locale.cs:161.
        // LoadLangDB has the same commented-out null check (:158-160); a langs
        // RDB without an Entries node NREs at `entries.ChildNodes`. The outer
        // try/catch (:188-190) catches it, so the load aborts silently. The
        // method is private, so it is invoked by reflection (the same pattern
        // NumericRoundtripTests uses for SoundEngine.ApplyDistanceVolume).
        [Test]
        public void LoadLangDB_MissingEntriesNode_AbortsWithoutThrowing()
        {
            string fixturePath = Path.Combine(Path.GetTempPath(), "nwr_null_deref_locale_fixture.xml");
            try {
                File.WriteAllText(fixturePath, "<RDB></RDB>");
                Assert.IsTrue(File.Exists(fixturePath), "locale fixture not written");

                var method = typeof(Locale).GetMethod("LoadLangDB", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(method, "Locale.LoadLangDB not found");

                var locale = new Locale();
                Assert.DoesNotThrow(() => method.Invoke(locale, new object[] { fixturePath }));
            } finally {
                try { File.Delete(fixturePath); } catch (Exception) { }
            }
        }
    }

    /// <summary>
    /// Characterization tests for the flagship site
    /// project/Game/Player.cs:758 (manifest :758): RecruitMercenary calls
    /// collocutor.AddMoney(hPrice) right after SubMoney(hPrice) with no null
    /// check. The production caller FreeVictim (NWCreature.cs:4468) passes
    /// byMoney=false, which never reaches the deref. Today the hire path
    /// (byMoney=true) with a null collocutor and enough money NREs; with too
    /// little money it returns silently before the deref.
    /// </summary>
    [TestFixture]
    public class RecruitMercenaryTests
    {
        [Test]
        public void NullCollocutor_WithEnoughMoney_ThrowsNre()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            Player player = game.Player;
            player.AddMoney(10001);
            var mercenary = new NWCreature(game, null);
            mercenary.InitEx(GlobalVars.cid_Viking, true, false);

            // Money (10001) >= HirePrice (Viking default 10000) so the code
            // reaches SubMoney then collocutor.AddMoney -> NRE on the null
            // collocutor. Order-independent: coins added here leave exactly one
            // coin behind, still far below the hire price for the sibling test.
            Assert.Throws<NullReferenceException>(() => player.RecruitMercenary(null, mercenary, true));
        }

        [Test]
        public void NullCollocutor_WithoutEnoughMoney_ReturnsSilently()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            Player player = game.Player;
            var mercenary = new NWCreature(game, null);
            mercenary.InitEx(GlobalVars.cid_Viking, true, false);

            // Money < HirePrice -> the deref is never reached (the money guard
            // short-circuits to a "no money" message). Pins current behavior:
            // a null collocutor on the hire path is silent only while the
            // player cannot afford the hire.
            Assert.DoesNotThrow(() => player.RecruitMercenary(null, mercenary, true));
            Assert.Less(player.Money, 10000);
        }
    }
}
