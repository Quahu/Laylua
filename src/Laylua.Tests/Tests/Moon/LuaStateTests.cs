namespace Laylua.Tests;

public class LuaStateTests : LuaTestBase
{
    [Test]
    public void CancellationTokenLuaHook_CancelsExecutionWithError()
    {
        // Arrange
        using var cts = new CancellationTokenSource(20);
        Lua.State.Hook = new CancellationTokenLuaHook(cts.Token);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("while true do end"));
    }

    [Test]
    public void MaxInstructionCountLuaHook_CancelsExecutionWithError()
    {
        // Arrange
        Lua.State.Hook = new MaxInstructionCountLuaHook(100);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("while true do end"));
    }
}
