using System.IO;
using NUnit.Framework;
using NWR.Game;
using NWR.Game.Story;
using ZRLib.Core;

namespace NWR.Tests.Serialization
{
    [TestFixture]
    public class JournalStreamTests
    {
        [Test]
        public void SaveLoadRoundTrip()
        {
            var journal = new Journal();
            journal.StoreTime(new NWDateTime { Year = 609, Month = 7, Day = 15 });

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            Journal.RSJ_Header.Write(writer);
            StreamUtils.WriteInt(writer, 0);
            writer.Flush();

            string path = Path.Combine(Path.GetTempPath(), "nwr-journal-test.rgj");
            try {
                File.WriteAllBytes(path, ms.ToArray());
                journal.Load(path);
            } finally {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
        }
    }
}
