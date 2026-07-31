using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.SingleOp
{
    [TestFixture]
    public class SingleOpSmokeTests
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
        public void SingleOpSmoke_WritesReport_AndHasPasses()
        {
            string repoRoot = FindRepoRoot();
            Assert.DoesNotThrow(() => SingleOpScenarios.SingleOpSmoke(repoRoot));

            string path = SingleOpScenarios.ReportPath(repoRoot);
            Assert.IsTrue(File.Exists(path), path);
            string text = File.ReadAllText(path);
            Assert.IsTrue(text.Contains("\"summary\":"));
            Assert.IsTrue(text.Contains("\"deferred\":"));
            Assert.IsTrue(Regex.IsMatch(text, "\"itemPass\":[1-9]"), "expected itemPass > 0");
            Assert.IsTrue(Regex.IsMatch(text, "\"equipPass\":[1-9]"), "expected equipPass > 0");
            Assert.IsTrue(Regex.IsMatch(text, "\"effectPass\":[1-9]"), "expected effectPass > 0");
            Assert.IsTrue(Regex.IsMatch(text, "\"itemFail\":0"), "expected zero item failures");
            Assert.IsTrue(Regex.IsMatch(text, "\"effectFail\":0"), "expected zero effect failures");
        }
    }
}
