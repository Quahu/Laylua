using System.Threading;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class LuaStateTests : LuaTestBase
    {
        [Test]
        [Timeout(100)]
        public void CancellationTokenLuaHook_CancelsExecutionWithError()
        {
            // Arrange
            using var cts = new CancellationTokenSource(20);
            lua.State.Hook = new CancellationTokenLuaHook(cts.Token);

            // Act & Assert
            Assert.Throws<LuaException>(() => lua.Execute("while true do end"));
        }

        [Test]
        [Timeout(100)]
        public void MaxInstructionCountLuaHook_CancelsExecutionWithError()
        {
            // Arrange
            lua.State.Hook = new MaxInstructionCountLuaHook(100);

            // Act & Assert
            Assert.Throws<LuaException>(() => lua.Execute("while true do end"));
        }
    }
}
