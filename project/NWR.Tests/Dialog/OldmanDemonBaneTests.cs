using System;
using System.IO;
using NUnit.Framework;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Dialog
{
    [TestFixture]
    public class OldmanDemonBaneTests
    {
        [Test]
        public void OldmanDemonBane_DialogScripts_TransferSword()
        {
            DialogScenarios.OldmanDemonBane(FindRepoRoot());
        }

        private static string FindRepoRoot()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8; i++) {
                if (File.Exists(Path.Combine(dir, "play-cs.sh"))) {
                    return dir;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
