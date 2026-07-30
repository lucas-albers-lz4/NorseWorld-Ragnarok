using System.IO;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Game;
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

        private static NWGameSpace LoadSlot8WithQuests()
        {
            string repoRoot = FindRepoRoot();
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            TestWorld.EnsureMainQuests(game);
            return game;
        }

        [Test]
        public void Handin_Gjall_Heimdall()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "Gjall", GlobalVars.iid_Gjall, GlobalVars.cid_Heimdall, MaxHandinTurns));
        }

        [Test]
        public void Handin_Mjollnir_Thor()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "Mjollnir", GlobalVars.iid_Mjollnir, GlobalVars.cid_Thor, MaxHandinTurns));
        }

        [Test]
        public void Handin_DwarvenArm_Tyr()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "DwarvenArm", GlobalVars.iid_DwarvenArm, GlobalVars.cid_Tyr, MaxHandinTurns));
        }

        [Test]
        public void Handin_Mimming_Freyr()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "Mimming", GlobalVars.iid_Mimming, GlobalVars.cid_Freyr, MaxHandinTurns));
        }

        [Test]
        public void Handin_Gungnir_Odin()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Assert.AreEqual(QuestItemState.Completed,
                TestWorld.HandInArtefact(game, "Gungnir", GlobalVars.iid_Gungnir, GlobalVars.cid_Odin, MaxHandinTurns));
        }

        [Test]
        public void Handin_Hela_RejectsRingWithoutThokk()
        {
            NWGameSpace game = LoadSlot8WithQuests();
            Item ring = TestWorld.SpawnItem(game.Player, "Ring_SoulTrapping", 1, true);
            Assert.IsNotNull(ring);
            Assert.AreEqual(0, ring.Bonus);

            TestWorld.PlaceCreatureNearPlayer(game, GlobalVars.cid_Hela);
            TestWorld.RunTurns(game, MaxHandinTurns);

            Assert.AreEqual(QuestItemState.Founded,
                game.CheckQuestItem(GlobalVars.iid_SoulTrapping_Ring, GlobalVars.cid_Hela));
            Assert.IsNotNull(game.Player.Items.FindByCLSID(GlobalVars.iid_SoulTrapping_Ring));
        }

        [Test]
        public void Handin_Hela_AcceptsRingWithThokkBonus()
        {
            NWGameSpace game = LoadSlot8WithQuests();
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
        }

        [Test]
        public void Eitri_TradesPlatinumAnvilForDwarvenArm()
        {
            NWGameSpace game = LoadSlot8WithQuests();
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
