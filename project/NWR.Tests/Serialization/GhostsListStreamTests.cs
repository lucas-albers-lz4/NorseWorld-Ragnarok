using System.IO;
using NUnit.Framework;
using NWR.Game;
using NWR.Game.Ghosts;
using ZRLib.Core;

namespace NWR.Tests.Serialization
{
    [TestFixture]
    public class GhostsListStreamTests
    {
        [Test]
        public void SaveUsesNonNullFileVersionHeader()
        {
            Assert.IsNotNull(GhostsList.RGL_Header.Version);
            Assert.AreEqual(1, GhostsList.RGL_Header.Version.Release);
            Assert.AreEqual(0, GhostsList.RGL_Header.Version.Revision);
        }

        [Test]
        public void EmptyListSaveLoadRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "nwr-ghosts-test.rgl");
            try {
                var list = new GhostsList();
                list.Save(path);
                Assert.IsTrue(File.Exists(path));

                var loaded = new GhostsList();
                loaded.Load(path);
                Assert.AreEqual(0, loaded.GhostCount);

                using (var fs = new FileStream(path, FileMode.Open))
                using (var reader = new BinaryReader(fs)) {
                    var header = new FileHeader();
                    header.Read(reader);
                    Assert.AreEqual('R', header.Sign[0]);
                    Assert.AreEqual('G', header.Sign[1]);
                    Assert.AreEqual('L', header.Sign[2]);
                    Assert.AreEqual(1, header.Version.Release);
                    Assert.AreEqual(0, header.Version.Revision);
                    Assert.AreEqual(0, StreamUtils.ReadInt(reader));
                }
            } finally {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
        }
    }
}
