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
            // Locate the langs.xml data path used by Locale.cs:118 (LoadLangs).
            string langsPath = Path.Combine(
                NWResourceManager.GetAppPath(),
                Locale.LANGS_FOLDER,
                "langs.xml"
            );
            Assert.IsTrue(File.Exists(langsPath), "langs.xml not found at " + langsPath);

            // First load — opens and must dispose every FileStream
            // (LoadLangs:118, LoadLangDB:147, LoadLangTexts:196, LoadLangDialog:210).
            Assert.DoesNotThrow(() => TestHost.CreateGameSpace(null));

            // Idempotent re-load — succeeds only if the first load released
            // all handles. On Windows an un-disposed FileStream blocks
            // re-open; on Linux Mono's fcntl locks enforce FileShare, so
            // an unclosed handle would also throw.
            Assert.DoesNotThrow(() => TestHost.CreateGameSpace(null));

            // Direct verification: open langs.xml with exclusive sharing
            // (FileShare.None). If the LoadLangs FileStream was not disposed,
            // this throws. Wrapped in DoesNotThrow for Linux configs that
            // don't enforce fcntl locks.
            Assert.DoesNotThrow(() => {
                using (var fs = new FileStream(
                    langsPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                    Assert.IsTrue(fs.CanRead);
                }
            });
        }
    }
}
