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
    public class QuestHandinTests
    {
        private const int MaxHandinTurns = 40;

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
            Assert.AreEqual(6, game.QuestsCount, "LoadGame should GenMainQuests");
            return game;
        }

        private static MainQuest RequireQuest(NWGameSpace game, int artefactId)
        {
            for (int i = 0; i < game.QuestsCount; i++) {
                MainQuest mq = game.GetQuest(i) as MainQuest;
                if (mq != null && mq.ArtefactID == artefactId) {
                    return mq;
                }
            }
            Assert.Fail("missing MainQuest for artefact " + artefactId);
            return null;
        }

        private static void AssertHandinComplete(NWGameSpace game, string sign, int artefactId, int deityId)
        {
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, sign, artefactId, deityId, MaxHandinTurns));
            MainQuest quest = RequireQuest(game, artefactId);
            Assert.AreEqual(QuestItemState.Completed, quest.Stage);
            Assert.IsTrue(quest.IsComplete, sign + " should set IsComplete for journal");
        }

        [Test]
        public void Handin_Gjall_Heimdall()
        {
            AssertHandinComplete(LoadSlot8(), "Gjall", GlobalVars.iid_Gjall, GlobalVars.cid_Heimdall);
        }

        [Test]
        public void Handin_Mjollnir_Thor()
        {
            AssertHandinComplete(LoadSlot8(), "Mjollnir", GlobalVars.iid_Mjollnir, GlobalVars.cid_Thor);
        }

        [Test]
        public void Handin_DwarvenArm_Tyr()
        {
            AssertHandinComplete(LoadSlot8(), "DwarvenArm", GlobalVars.iid_DwarvenArm, GlobalVars.cid_Tyr);
        }

        [Test]
        public void Handin_Mimming_Freyr()
        {
            AssertHandinComplete(LoadSlot8(), "Mimming", GlobalVars.iid_Mimming, GlobalVars.cid_Freyr);
        }

        [Test]
        public void Handin_Gungnir_Odin()
        {
            AssertHandinComplete(LoadSlot8(), "Gungnir", GlobalVars.iid_Gungnir, GlobalVars.cid_Odin);
        }

        [Test]
        public void Handin_Hela_RejectsRingWithoutThokk()
        {
            NWGameSpace game = LoadSlot8();
            Item ring = TestWorld.SpawnItem(game.Player, "Ring_SoulTrapping", 1, true);
            Assert.IsNotNull(ring);
            Assert.AreEqual(0, ring.Bonus);

            TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Hela);
            TestWorld.RunTurns(game, MaxHandinTurns);

            Assert.AreEqual(QuestItemState.Founded,
                game.CheckQuestItem(GlobalVars.iid_SoulTrapping_Ring, GlobalVars.cid_Hela));
            Assert.IsNotNull(game.Player.Items.FindByCLSID(GlobalVars.iid_SoulTrapping_Ring));
            Assert.IsFalse(RequireQuest(game, GlobalVars.iid_SoulTrapping_Ring).IsComplete);
        }

        [Test]
        public void Handin_Hela_AcceptsRingWithThokkBonus()
        {
            NWGameSpace game = LoadSlot8();
            // Hero start kit already includes Ring_SoulTrapping; TakeItemDef uses FindByCLSID
            // (first match), so every pack copy must carry Thokk or Hela rejects.
            Item ring = TestWorld.SpawnItem(game.Player, "Ring_SoulTrapping", 1, true);
            Assert.IsNotNull(ring);
            for (int i = 0; i < game.Player.Items.Count; i++) {
                Item it = game.Player.Items[i];
                if (it.CLSID == GlobalVars.iid_SoulTrapping_Ring) {
                    it.Bonus = GlobalVars.cid_Thokk;
                }
            }

            TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Hela);
            TestWorld.RunTurns(game, MaxHandinTurns);

            Assert.AreEqual(QuestItemState.Completed,
                game.CheckQuestItem(GlobalVars.iid_SoulTrapping_Ring, GlobalVars.cid_Hela));
            Assert.IsTrue(RequireQuest(game, GlobalVars.iid_SoulTrapping_Ring).IsComplete);
        }

        [Test]
        public void Eitri_TradesPlatinumAnvilForDwarvenArm()
        {
            NWGameSpace game = LoadSlot8();
            Item anvil = TestWorld.SpawnItem(game.Player, "PlatinumAnvil", 1, true);
            Assert.IsNotNull(anvil);

            NWCreature eitri = TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Eitri);
            Assert.IsTrue(game.Player.TransferItem(eitri, "PlatinumAnvil"));
            Assert.IsTrue(eitri.TransferItem(game.Player, "DwarvenArm"));
            Assert.IsNotNull(game.Player.Items.FindByCLSID(GlobalVars.iid_DwarvenArm));
            Assert.AreEqual(QuestItemState.Founded,
                game.CheckQuestItem(GlobalVars.iid_DwarvenArm, GlobalVars.cid_Tyr));
        }
    }
}
