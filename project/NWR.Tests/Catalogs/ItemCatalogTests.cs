using System.IO;
using NUnit.Framework;
using NWR.Database;
using NWR.Game;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Catalogs
{
    [TestFixture]
    public class ItemCatalogTests
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
        public void DbItems_AllAreItemEntriesWithSignAndKind()
        {
            HarnessBootstrap.Init(FindRepoRoot());
            Assert.Greater(GlobalVars.dbItems.Count, 100);
            for (int i = 0; i < GlobalVars.dbItems.Count; i++) {
                ItemEntry entry = GlobalVars.nwrDB.GetEntry(GlobalVars.dbItems[i]) as ItemEntry;
                Assert.IsNotNull(entry, "guid " + GlobalVars.dbItems[i]);
                Assert.IsFalse(string.IsNullOrEmpty(entry.Sign));
                Assert.IsNotNull(entry.Flags);
            }
        }

        [Test]
        public void ItemCatalog_WritesJsonWithEveryDbItem()
        {
            string repoRoot = FindRepoRoot();
            CatalogScenarios.ItemCatalog(repoRoot);
            string path = CatalogScenarios.ItemsCatalogPath(repoRoot);
            Assert.IsTrue(File.Exists(path), path);
            string text = File.ReadAllText(path);
            Assert.IsTrue(text.Contains("\"count\":" + GlobalVars.dbItems.Count));
            Assert.IsTrue(text.Contains("\"kind\":"));
            Assert.IsTrue(text.Contains("\"flags\":"));
            Assert.IsTrue(text.Contains("\"sign\":\"Mjollnir\"") || text.Contains("\"sign\": \"Mjollnir\""));
            // Our writer has no spaces after colon
            Assert.IsTrue(text.Contains("\"sign\":\"Mjollnir\""));
        }
    }
}
