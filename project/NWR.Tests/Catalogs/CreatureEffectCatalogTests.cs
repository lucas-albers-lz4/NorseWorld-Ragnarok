using System.IO;
using NUnit.Framework;
using NWR.Effects;
using NWR.Game;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Catalogs
{
    [TestFixture]
    public class CreatureEffectCatalogTests
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
        public void CreatureCatalog_WritesJsonWithBrains()
        {
            string repoRoot = FindRepoRoot();
            CatalogScenarios.CreatureCatalog(repoRoot);
            string path = CatalogScenarios.CreaturesCatalogPath(repoRoot);
            Assert.IsTrue(File.Exists(path));
            string text = File.ReadAllText(path);
            Assert.IsTrue(text.Contains("\"count\":" + GlobalVars.dbCreatures.Count));
            Assert.IsTrue(text.Contains("\"predictedBrain\":"));
            Assert.IsTrue(text.Contains("\"sign\":\"Werewolf\""));
            Assert.IsTrue(text.Contains("\"sign\":\"Cockatrice\""));
        }

        [Test]
        public void EffectCatalog_WritesJsonWithRays()
        {
            string repoRoot = FindRepoRoot();
            CatalogScenarios.EffectCatalog(repoRoot);
            string path = CatalogScenarios.EffectsCatalogPath(repoRoot);
            Assert.IsTrue(File.Exists(path));
            string text = File.ReadAllText(path);
            Assert.IsTrue(text.Contains("\"count\":" + EffectsData.dbEffects.Length));
            Assert.IsTrue(text.Contains("\"effectID\":\"eid_Stoning\""));
            Assert.IsTrue(text.Contains("\"rayHandler\":\"StoningRay\""));
            Assert.IsTrue(text.Contains("\"isRay\":"));
        }
    }
}
