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

    [Test]
    public void GC_IsRunning_ReturnsTrue()
    {
        // Arrange
        var gc = Lua.State.GC;

        // Act
        var isRunning = gc.IsRunning;

        // Assert
        Assert.That(isRunning, Is.True);
    }

    [Test]
    public void GC_IsRunning_ReturnsFalseAfterStopping()
    {
        // Arrange
        var gc = Lua.State.GC;

        // Act
        gc.Stop();
        var isRunning = gc.IsRunning;

        // Assert
        Assert.That(isRunning, Is.False);
    }

    [Test]
    public void GC_AllocatedBytes_ReturnsValue()
    {
        // Arrange
        var gc = Lua.State.GC;

        // Act
        var allocatedBytes = gc.AllocatedBytes;

        // Assert
        Assert.That(allocatedBytes, Is.Not.Zero);
    }

    [Test]
    public void GC_SetGenerationalMode_ReturnsIncrementalMode()
    {
        // Arrange
        var gc = Lua.State.GC;

        // Act
        var previousMode = gc.SetGenerationalMode(20, 100);

        // Assert
        Assert.That(previousMode, Is.EqualTo(LuaGCOperation.Incremental));
    }

    [Test]
    public void GC_SetIncrementalMode_ReturnsIncrementalMode()
    {
        // Arrange
        var gc = Lua.State.GC;

        // Act
        var previousMode = gc.SetIncrementalMode(200, 100, 13);

        // Assert
        Assert.That(previousMode, Is.EqualTo(LuaGCOperation.Incremental));
    }
}
