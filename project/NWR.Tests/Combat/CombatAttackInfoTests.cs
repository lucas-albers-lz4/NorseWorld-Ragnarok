using NUnit.Framework;
using NWR.Creatures;
using NWR.Game;
using NWR.Game.Types;
using NWR.Items;
using ZRLib.Core;

namespace NWR.Tests.Combat
{
    [TestFixture]
    public class CombatAttackInfoTests
    {
        private sealed class ProbeCreature : NWCreature
        {
            public ProbeCreature(NWGameSpace space)
                : base(space, null)
            {
            }

            public int MeleeToHitAgainst(NWCreature enemy)
            {
                return CalcAttackInfo(AttackKind.Melee, enemy, null, null).ToHit;
            }

            public int MeleeDamageAgainst(NWCreature enemy)
            {
                return CalcAttackInfo(AttackKind.Melee, enemy, null, null).Damage;
            }
        }

        [Test]
        public void ClampHitChance_DoesNotAbsNegatives()
        {
            Assert.AreEqual(0, NWCreature.ClampHitChance(-40));
            Assert.AreEqual(0, NWCreature.ClampHitChance(-1));
            Assert.AreEqual(50, NWCreature.ClampHitChance(50));
            Assert.AreEqual(100, NWCreature.ClampHitChance(100));
            Assert.AreEqual(100, NWCreature.ClampHitChance(150));
        }

        [Test]
        public void MeleeToHit_DecreasesWithEnemyArmorClass()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new ProbeCreature(game);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            enemy.ArmorClass = 0;
            int toHitNaked = attacker.MeleeToHitAgainst(enemy);

            enemy.ArmorClass = 8;
            int toHitArmored = attacker.MeleeToHitAgainst(enemy);

            Assert.Greater(toHitNaked, toHitArmored);
            Assert.AreEqual(toHitNaked - 16, toHitArmored);
        }

        [Test]
        public void MeleeToHit_ArmorEquipRaisesDefense()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new ProbeCreature(game);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            int before = attacker.MeleeToHitAgainst(enemy);

            int plateId = game.FindDataEntry("PlateMail").GUID;
            var plate = new Item(game, enemy);
            plate.CLSID = plateId;
            plate.Count = 1;
            plate.Identified = true;
            enemy.AddItem(plate);
            plate.InUse = true;

            Assert.Greater(enemy.ArmorClass, 0);
            int after = attacker.MeleeToHitAgainst(enemy);
            Assert.Greater(before, after);
        }

        [Test]
        public void MeleeDamage_DecreasesWithEnemyArmorSkill()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var attacker = new ProbeCreature(game);
            attacker.InitEx(GlobalVars.cid_Viking, true, false);
            var enemy = new ProbeCreature(game);
            enemy.InitEx(GlobalVars.cid_Viking, true, false);

            // InitEx may already equip light armor on torso; unequip so PlateMail is the skill source.
            Item worn = enemy.GetItemByEquipmentKind(BodypartType.bp_Torso);
            if (worn != null) {
                worn.InUse = false;
            }

            int plateId = game.FindDataEntry("PlateMail").GUID;
            var plate = new Item(game, enemy);
            plate.CLSID = plateId;
            plate.Count = 1;
            plate.Identified = true;
            enemy.AddItem(plate);
            plate.InUse = true;

            // Fixed damage range so the only delta is armor-skill reduction.
            attacker.DBMin = 20;
            attacker.DBMax = 20;

            enemy.SetAbility(AbilityID.Ab_HeavyArmor, 0);
            int dmgLow = attacker.MeleeDamageAgainst(enemy);

            enemy.SetAbility(AbilityID.Ab_HeavyArmor, 50);
            int dmgHigh = attacker.MeleeDamageAgainst(enemy);

            Assert.AreEqual(20, dmgLow);
            Assert.AreEqual(15, dmgHigh);
        }
    }
}
