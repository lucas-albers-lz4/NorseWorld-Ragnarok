using System;
using System.IO;
using NUnit.Framework;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.EdgeCases
{
    [TestFixture]
    public class EdgeCaseTests
    {
        [Test]
        public void EdgeCaseSmoke_AllCases_NoHardFailures()
        {
            EdgeCaseScenarios.EdgeCaseSmoke(FindRepoRoot());
            string report = EdgeCaseScenarios.ReportPath(FindRepoRoot());
            Assert.IsTrue(File.Exists(report), "missing report " + report);
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
