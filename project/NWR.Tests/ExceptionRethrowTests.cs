using System;
using System.IO;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Creatures.Brain;
using NWR.Game;

namespace NWR.Tests
{
    [TestFixture]
    public class ExceptionRethrowTests
    {
        [Test]
        public void LoadFromStream_CorruptStream_PreservesOriginStackFrame()
        {
            NWGameSpace game = TestBootstrap.EnsureGame(null);
            var creature = new NWCreature(game, null);
            creature.InitEx(GlobalVars.cid_Viking, true, false);
            var brain = new BeastBrain(creature);

            var corrupt = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });
            using (var reader = new BinaryReader(corrupt)) {
                // ReadInt on a 3-byte stream throws EndOfStreamException (subclass
                // of Exception — NUnit Assert.Throws<Exception> requires the EXACT
                // type, so assert the concrete type).
                var ex = Assert.Throws<EndOfStreamException>(() => brain.LoadFromStream(reader, NWGameSpace.RGF_Version));
                Assert.IsNotNull(ex);
                // With `throw ex;` the stack resets at the rethrow (origin frame
                // lost) → this fails red. With `throw;` the origin frame survives.
                StringAssert.Contains("ReadInt", ex.StackTrace);
            }
        }
    }
}
