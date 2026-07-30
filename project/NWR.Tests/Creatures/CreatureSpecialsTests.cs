using NUnit.Framework;
using NWR.Creatures;
using NWR.Creatures.Brain;
using NWR.Creatures.Brain.Goals;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using ZRLib.Core.Brain;

namespace NWR.Tests.Creatures
{
    [TestFixture]
    public class CreatureSpecialsTests
    {
        [Test]
        public void CockatriceGaze_AppliesStoning()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var cockatrice = new NWCreature(game, null);
            cockatrice.InitEx(game.FindDataEntry("Cockatrice").GUID, true, false);
            var victim = new NWCreature(game, null);
            victim.InitEx(GlobalVars.cid_Viking, true, false);
            victim.SetAbility(AbilityID.Resist_Petrification, 0);

            Assert.IsNull(victim.Effects.FindEffectByID(EffectID.eid_Stoning));
            cockatrice.AttackSpecialEffect(victim);
            Assert.IsNotNull(victim.Effects.FindEffectByID(EffectID.eid_Stoning));
        }

        [Test]
        public void CockatriceGaze_RespectsPetrifyResist()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var cockatrice = new NWCreature(game, null);
            cockatrice.InitEx(game.FindDataEntry("Cockatrice").GUID, true, false);
            var victim = new NWCreature(game, null);
            victim.InitEx(GlobalVars.cid_Viking, true, false);
            victim.SetAbility(AbilityID.Resist_Petrification, 100);

            cockatrice.AttackSpecialEffect(victim);
            Assert.IsNull(victim.Effects.FindEffectByID(EffectID.eid_Stoning));
        }

        [Test]
        public void WerewolfBite_AppliesLycanthropyOnHitSpecial()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var wolf = new NWCreature(game, null);
            wolf.InitEx(GlobalVars.cid_Werewolf, true, false);
            var victim = new NWCreature(game, null);
            victim.InitEx(GlobalVars.cid_Viking, true, false);

            Assert.IsNull(victim.Effects.FindEffectByID(EffectID.eid_Lycanthropy));
            wolf.ApplyMeleeHitSpecials(victim);
            Assert.IsNotNull(victim.Effects.FindEffectByID(EffectID.eid_Lycanthropy));
        }

        [Test]
        public void EnemyChaseGoal_NullEnemy_CompletesWithoutThrow()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var self = new NWCreature(game, null);
            self.InitEx(GlobalVars.cid_Viking, true, false);
            var brain = new BeastBrain(self);
            var goal = new EnemyChaseGoal(brain);
            goal.Enemy = null;
            Assert.DoesNotThrow(() => goal.Execute());
            Assert.IsTrue(goal.IsComplete);
        }

        [Test]
        public void WareReturnGoal_NullHouse_CompletesWithoutThrow()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var merchant = new NWCreature(game, null);
            merchant.InitEx(game.FindDataEntry("Merchant").GUID, true, false);
            var brain = new TraderBrain(merchant);
            var goal = new WareReturnGoal(brain);
            Assert.IsNull(merchant.FindHouse());
            Assert.DoesNotThrow(() => goal.Execute());
            Assert.IsTrue(goal.IsComplete);
        }

        [Test]
        public void DebtTakeGoal_NullDebtor_CompletesWithoutThrow()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var merchant = new NWCreature(game, null);
            merchant.InitEx(game.FindDataEntry("Merchant").GUID, true, false);
            var brain = new TraderBrain(merchant);
            var goal = new DebtTakeGoal(brain);
            goal.Debtor = null;
            Assert.DoesNotThrow(() => goal.Execute());
            Assert.IsTrue(goal.IsComplete);
        }
    }
}
