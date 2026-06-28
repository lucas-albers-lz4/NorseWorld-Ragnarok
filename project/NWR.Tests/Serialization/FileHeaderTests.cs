using System.IO;
using NUnit.Framework;
using NWR.Game;
using ZRLib.Core;

namespace NWR.Tests.Serialization
{
    [TestFixture]
    public class FileHeaderTests
    {
        [Test]
        public void WriteReadRoundTrip()
        {
            var header = new FileHeader(NWGameSpace.RGP_Sign, NWGameSpace.RGF_Version.Clone() as FileVersion);
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            header.Write(writer);
            writer.Flush();

            ms.Position = 0;
            var reader = new BinaryReader(ms);
            var read = new FileHeader();
            read.Read(reader);

            Assert.AreEqual('R', read.Sign[0]);
            Assert.AreEqual('G', read.Sign[1]);
            Assert.AreEqual('P', read.Sign[2]);
            Assert.AreEqual(1, read.Version.Release);
            Assert.AreEqual(21, read.Version.Revision);
            Assert.AreEqual(ms.Length, ms.Position);
        }
    }
}
