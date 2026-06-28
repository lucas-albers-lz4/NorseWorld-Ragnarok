using System.IO;
using NUnit.Framework;
using NWR.Game;
using NWR.Items;
using ZRLib.Core;

namespace NWR.Tests.Serialization
{
    [TestFixture]
    public class ItemStreamTests
    {
        [Test]
        public void BootstrapLayers()
        {
            GlobalVars.nwrGame = null;
            NWGameSpace game = TestHost.CreateGameSpace(null);
            Assert.Greater(game.LayersCount, 0);
        }

        [Test]
        public void ContainerRoundTrip()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            int flaskId = game.FindDataEntry("Flask").GUID;
            int torchId = game.FindDataEntry("Torch").GUID;

            var flask = new Item(game, null);
            flask.CLSID = flaskId;
            flask.Count = 1;
            flask.Identified = true;

            var torch = new Item(game, null);
            torch.CLSID = torchId;
            torch.Count = 1;
            torch.Identified = true;
            flask.Contents.Add(torch);

            Assert.IsTrue(flask.Container);
            Assert.AreEqual(1, flask.Contents.Count);

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            flask.SaveToStream(writer, NWGameSpace.RGF_Version);
            writer.Flush();

            ms.Position = 0;
            var reader = new BinaryReader(ms);
            var loaded = new Item(game, null);
            loaded.LoadFromStream(reader, NWGameSpace.RGF_Version);

            Assert.AreEqual(flaskId, loaded.CLSID);
            Assert.IsTrue(loaded.Container);
            Assert.AreEqual(1, loaded.Contents.Count);
            Assert.AreEqual(torchId, loaded.Contents[0].CLSID);
            Assert.AreEqual(ms.Length, ms.Position);
        }
    }
}
