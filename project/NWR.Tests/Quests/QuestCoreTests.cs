using System.IO;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Game;
using NWR.Game.Quests;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Quests
{
    [TestFixture]
    public class QuestCoreTests
    {
        private static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8; i++) {
                if (File.Exists(Path.Combine(dir, "play-cs.sh"))) {
                    return dir;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return Directory.GetCurrentDirectory();
        }

        private static NWGameSpace LoadSlot8()
        {
            string repoRoot = FindRepoRoot();
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            return game;
        }

        [Test]
        public void GenMainQuests_RegistersSixArtefactQuests()
        {
            NWGameSpace game = LoadSlot8();
            Assert.AreEqual(6, game.QuestsCount);

            var q0 = (MainQuest)game.GetQuest(0);
            Assert.AreEqual(GlobalVars.iid_SoulTrapping_Ring, q0.ArtefactID);
            Assert.AreEqual(GlobalVars.cid_Hela, q0.DeityID);

            var q5 = (MainQuest)game.GetQuest(5);
            Assert.AreEqual(GlobalVars.iid_Gungnir, q5.ArtefactID);
            Assert.AreEqual(GlobalVars.cid_Odin, q5.DeityID);
        }

        [Test]
        public void CheckQuestItem_NoneFoundedCompleted()
        {
            NWGameSpace game = LoadSlot8();
            int iid = GlobalVars.iid_Mjollnir;
            int deity = GlobalVars.cid_Thor;

            Assert.AreEqual(QuestItemState.None, game.CheckQuestItem(iid, deity));

            Item hammer = TestWorld.SpawnItem(game.Player, "Mjollnir", 1, true);
            Assert.IsNotNull(hammer);
            Assert.AreEqual(QuestItemState.Founded, game.CheckQuestItem(iid, deity));

            NWCreature thor = TestWorld.PlaceCreatureNearPlayer(game, deity);
            Item taken = (Item)game.Player.Items.Extract(hammer);
            thor.Items.Add(taken, false);
            Assert.AreEqual(QuestItemState.Completed, game.CheckQuestItem(iid, deity));
        }

        [Test]
        public void MainQuest_PickupSetsFounded_GiveupSetsComplete()
        {
            NWGameSpace game = LoadSlot8();
            var quest = (MainQuest)game.GetQuest(2);
            Item hammer = TestWorld.SpawnItem(game.Player, "Mjollnir", 1, true);
            Assert.IsNotNull(hammer);

            Assert.IsFalse(quest.PickupItem(hammer));
            Assert.AreEqual(QuestItemState.Founded, quest.Stage);
            Assert.IsFalse(quest.IsComplete);

            NWCreature thor = TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Thor);
            Assert.IsTrue(quest.GiveupItem(hammer, thor));
            Assert.AreEqual(QuestItemState.Completed, quest.Stage);
            Assert.IsTrue(quest.IsComplete);
        }

        [Test]
        public void ItemQuest_CompletesOnMatchingPickup()
        {
            NWGameSpace game = LoadSlot8();
            var quest = new ItemQuest(game, GlobalVars.iid_Gjall);
            Item wrong = TestWorld.SpawnItem(game.Player, "Mjollnir", 1, true);
            Item right = TestWorld.SpawnItem(game.Player, "Gjall", 1, true);
            Assert.IsFalse(quest.PickupItem(wrong));
            Assert.IsFalse(quest.IsComplete);
            Assert.IsTrue(quest.PickupItem(right));
            Assert.IsTrue(quest.IsComplete);
        }

        [Test]
        public void EnemyQuest_CompletesAfterRequiredKills()
        {
            NWGameSpace game = LoadSlot8();
            var quest = new EnemyQuest(game, GlobalVars.cid_Thokk, 2);
            var dummy = new NWCreature(game, null);
            dummy.InitEx(GlobalVars.cid_Thokk, true, false);

            Assert.IsFalse(quest.KillMonster(dummy));
            Assert.AreEqual(1, quest.Remains);
            Assert.IsFalse(quest.IsComplete);
            Assert.IsTrue(quest.KillMonster(dummy));
            Assert.IsTrue(quest.IsComplete);
        }

        [Test]
        public void SaveLoad_FoundedStage_WithoutManualGenMainQuests()
        {
            string repoRoot = FindRepoRoot();
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            Assert.AreEqual(6, game.QuestsCount);

            TestWorld.SpawnItem(game.Player, "Gungnir", 1, true);
            Assert.AreEqual(QuestItemState.Founded, game.CheckQuestItem(GlobalVars.iid_Gungnir, GlobalVars.cid_Odin));

            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            Assert.AreEqual(6, game.QuestsCount);
            Assert.AreEqual(QuestItemState.Founded, game.CheckQuestItem(GlobalVars.iid_Gungnir, GlobalVars.cid_Odin));
            var quest = (MainQuest)game.GetQuest(5);
            Assert.AreEqual(QuestItemState.Founded, quest.Stage);
            Assert.IsFalse(quest.IsComplete);
        }

        [Test]
        public void SaveLoad_CompletedStage_RestoresIsComplete()
        {
            string repoRoot = FindRepoRoot();
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "Mjollnir", GlobalVars.iid_Mjollnir, GlobalVars.cid_Thor, 40));
            var before = (MainQuest)game.GetQuest(2);
            Assert.IsTrue(before.IsComplete);

            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            Assert.AreEqual(6, game.QuestsCount);
            Assert.AreEqual(QuestItemState.Completed,
                game.CheckQuestItem(GlobalVars.iid_Mjollnir, GlobalVars.cid_Thor));
            var after = (MainQuest)game.GetQuest(2);
            Assert.AreEqual(QuestItemState.Completed, after.Stage);
            Assert.IsTrue(after.IsComplete);
        }
    }
}
