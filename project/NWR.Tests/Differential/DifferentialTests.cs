using System;
using System.IO;
using NUnit.Framework;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Differential
{
    [TestFixture]
    public class DifferentialTests
    {
        [Test]
        public void AbDiff_JavaDist_ParitiesAndCsharpOps()
        {
            string root = FindRepoRoot();
            string jar = Path.Combine(root, "nwr-dist-v0.11.0-win", "Ragnarok.jar");
            if (!File.Exists(jar)) {
                Console.WriteLine("SKIP DifferentialTests.AbDiff (no Java dist; ./dev_info/fetch-java-dist.sh)");
                return;
            }
            DifferentialScenarios.AbDiff(root);
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
