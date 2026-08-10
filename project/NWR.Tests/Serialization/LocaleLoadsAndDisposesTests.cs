using System.IO;
using NUnit.Framework;
using NWR.Game;
using ZRLib.Core;

namespace NWR.Tests.Serialization
{
    [TestFixture]
    public class LocaleLoadsAndDisposesTests
    {
        [Test]
        public void LocaleLoadsAndDisposesStreams()
        {
            // Locale.cs opens four stream sites during SetLang("English"):
            //   LoadLangs    (Locale.cs:118) -> languages/langs.xml
            //   LoadLangDB   (Locale.cs:147) -> languages/<prefix>_db.xml
            //   LoadLangTexts(Locale.cs:196) -> languages/<prefix>_texts.xml
            //   LoadLangDialog(Locale.cs:210) -> only when a creature entry
            //     references an external dialog file — current en/ru data has
            //     ZERO such entries, so this path is not exercised by the
            //     test host; it is structurally covered by the identical
            //     `using` pattern (documented, not directly assertable here).
            string langsPath = Path.Combine(
                NWResourceManager.GetAppPath(),
                Locale.LANGS_FOLDER,
                "langs.xml"
            );
            string dbPath = Path.Combine(
                NWResourceManager.GetAppPath(),
                Locale.LANGS_FOLDER,
                "en_db.xml"
            );
            string textsPath = Path.Combine(
                NWResourceManager.GetAppPath(),
                Locale.LANGS_FOLDER,
                "en_texts.xml"
            );
            Assert.IsTrue(File.Exists(langsPath), "langs.xml not found at " + langsPath);
            Assert.IsTrue(File.Exists(dbPath), "en_db.xml not found at " + dbPath);
            Assert.IsTrue(File.Exists(textsPath), "en_texts.xml not found at " + textsPath);

            // First load — opens every FileStream (LoadLangs, LoadLangDB,
            // LoadLangTexts) and must dispose each.
            Assert.DoesNotThrow(() => TestHost.CreateGameSpace(null));

            // Idempotent re-load — succeeds only if the first load released
            // all handles. On Windows an un-disposed FileStream blocks
            // re-open; on Linux Mono's fcntl locks enforce FileShare, so
            // an unclosed handle would also throw.
            Assert.DoesNotThrow(() => TestHost.CreateGameSpace(null));

            // Direct verification for EACH reachable site: open the file
            // with exclusive sharing (FileShare.None). If the corresponding
            // FileStream was not disposed, the open throws IOException.
            // Wrapped in DoesNotThrow for Linux configs that don't enforce
            // fcntl locks (the re-load assertion above is the primary guard).
            //
            // KNOWN LIMITATION (verified 2026-08-10, Mono 6.12): Mono DOES
            // enforce FileShare.None while a stream is alive, but an
            // undisposed local becomes unreachable at method end and the GC
            // may finalize it (releasing the handle) before this assertion
            // runs — so a leaked-but-unreachable stream can pass here.
            // The deterministic disposal guarantee comes from the `using`
            // patterns in Locale.cs (verified by code review); these
            // assertions are a best-effort regression smoke for live leaks.
            AssertExclusiveOpen(langsPath, "LoadLangs (Locale.cs:118)");
            AssertExclusiveOpen(dbPath, "LoadLangDB (Locale.cs:147)");
            AssertExclusiveOpen(textsPath, "LoadLangTexts (Locale.cs:196)");
        }

        private static void AssertExclusiveOpen(string path, string site)
        {
            Assert.DoesNotThrow(() => {
                using (var fs = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.None)) {
                    Assert.IsTrue(fs.CanRead, site + ": stream not readable");
                }
            }, site + ": file is still held open after locale load");
        }
    }
}
