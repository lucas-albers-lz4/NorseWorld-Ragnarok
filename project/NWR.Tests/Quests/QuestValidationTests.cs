using System.IO;
using NUnit.Framework;
using NWR.Game;
using NWR.Game.Quests;
using NWR.Game.Types;
using NWR.Items;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Quests
{
    [TestFixture]
    public class QuestValidationTests
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

        private static NWGameSpace ReloadSlot8(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            Assert.AreEqual(6, game.QuestsCount);
            return game;
        }

        [Test]
        public void AllSixArtefacts_HandIn_CompleteWithIsComplete()
        {
            // Fresh fixture per artefact so deities do not crowd the player's field.
            string repoRoot = FindRepoRoot();

            var cases = new[] {
                new { Sign = "Gjall", Artefact = GlobalVars.iid_Gjall, Deity = GlobalVars.cid_Heimdall },
                new { Sign = "Mjollnir", Artefact = GlobalVars.iid_Mjollnir, Deity = GlobalVars.cid_Thor },
                new { Sign = "DwarvenArm", Artefact = GlobalVars.iid_DwarvenArm, Deity = GlobalVars.cid_Tyr },
                new { Sign = "Mimming", Artefact = GlobalVars.iid_Mimming, Deity = GlobalVars.cid_Freyr },
                new { Sign = "Gungnir", Artefact = GlobalVars.iid_Gungnir, Deity = GlobalVars.cid_Odin },
            };

            for (int i = 0; i < cases.Length; i++) {
                NWGameSpace game = ReloadSlot8(repoRoot);
                Assert.AreEqual(QuestItemState.Completed,
                    TestWorld.HandInArtefact(game, cases[i].Sign, cases[i].Artefact, cases[i].Deity, MaxHandinTurns),
                    cases[i].Sign);
                Assert.IsTrue(RequireQuest(game, cases[i].Artefact).IsComplete, cases[i].Sign);
            }

            NWGameSpace helaGame = ReloadSlot8(repoRoot);
            Item ring = TestWorld.SpawnItem(helaGame.Player, "Ring_SoulTrapping", 1, true);
            Assert.IsNotNull(ring);
            for (int i = 0; i < helaGame.Player.Items.Count; i++) {
                Item it = helaGame.Player.Items[i];
                if (it.CLSID == GlobalVars.iid_SoulTrapping_Ring) {
                    it.Bonus = GlobalVars.cid_Thokk;
                }
            }
            TestWorld.PlaceCreatureNearPlayer(helaGame, GlobalVars.cid_Hela);
            TestWorld.RunTurns(helaGame, MaxHandinTurns);
            Assert.AreEqual(QuestItemState.Completed,
                helaGame.CheckQuestItem(GlobalVars.iid_SoulTrapping_Ring, GlobalVars.cid_Hela));
            Assert.IsTrue(RequireQuest(helaGame, GlobalVars.iid_SoulTrapping_Ring).IsComplete);
        }
    }
}
