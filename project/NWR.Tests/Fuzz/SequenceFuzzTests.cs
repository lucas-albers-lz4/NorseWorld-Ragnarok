using System;
using System.IO;
using NUnit.Framework;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests.Fuzz
{
    [TestFixture]
    public class SequenceFuzzTests
    {
        [Test]
        public void SequenceFuzz_DefaultSeed_Completes()
        {
            // Full N=1000 is in --all via sequence-fuzz; unit smoke uses smaller N.
            Environment.SetEnvironmentVariable("NWR_FUZZ_N", "50");
            Environment.SetEnvironmentVariable("NWR_FUZZ_SEED", "15");
            try {
                FuzzScenarios.SequenceFuzz(FindRepoRoot());
                Assert.IsTrue(File.Exists(FuzzScenarios.ReportPath(FindRepoRoot())));
            } finally {
                Environment.SetEnvironmentVariable("NWR_FUZZ_N", null);
            }
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
