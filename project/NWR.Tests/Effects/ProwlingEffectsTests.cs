using System.IO;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Types;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Effects
{
    [TestFixture]
    public class ProwlingEffectsTests
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

        [Test]
        public void ProwlingEnd_NullImage_DoesNotThrow()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var cr = new NWCreature(game, null);
            cr.InitEx(GlobalVars.cid_Viking, true, false);
            cr.Prowling = true;
            cr.ProwlImage = null;
            cr.ProwlingEnd();
            Assert.IsFalse(cr.Prowling);
        }

        [Test]
        public void ProwlingLastTick_DoesNotThrowDuringEffectsExecute()
        {
            string repoRoot = FindRepoRoot();
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            Player player = game.Player;
            player.ProwlingBegin(EffectID.eid_Insanity);
            Assert.IsTrue(player.Prowling);
            Assert.IsNotNull(player.ProwlImage);

            Effect prowling = player.Effects.FindEffectByID(EffectID.eid_Prowling);
            Assert.IsNotNull(prowling);
            prowling.Duration = 1;

            Assert.DoesNotThrow(() => player.DoTurn());
            Assert.IsFalse(player.Prowling);
        }
    }
}
