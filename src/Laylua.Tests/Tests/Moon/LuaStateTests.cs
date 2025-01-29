namespace Laylua.Tests;

public class LuaStateTests : LuaTestBase
{
    [Test]
    public void NativeMemoryLuaAllocator_MaxBytes_DeniesAllocation()
    {
        // Arrange
        using var lua = CreateLua(CreateLuaAllocator(8192));

        // Act
        var ex = Assert.Throws<LuaPanicException>(() => lua.CreateTable(tableCapacity: 4096).Dispose());

        // Assert
        Assert.That(ex, Has.Message.Contains("not enough memory"));
    }

    [Test]
    public void CancellationTokenLuaHook_CancelsExecutionWithError()
    {
        // Arrange
        using var cts = new CancellationTokenSource(20);
        Lua.State.Hook = new CancellationTokenLuaHook(cts.Token);

        // Act & Assert
        Assert.That(() => Lua.Execute("while true do end"), Throws.TypeOf<LuaException>()
            .With.InnerException.TypeOf<OperationCanceledException>()
            .And.InnerException.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cts.Token));
    }

    [Test]
    public void MaxInstructionCountLuaHook_CancelsExecutionWithError()
    {
        // Arrange
        Lua.State.Hook = new MaxInstructionCountLuaHook(100);

        // Act & Assert
        Assert.That(() => Lua.Execute("while true do end"), Throws.TypeOf<LuaException>()
            .With.InnerException.TypeOf<MaxInstructionCountReachedException>()
            .And.InnerException.Property(nameof(MaxInstructionCountReachedException.InstructionCount)).EqualTo(100));
    }

    private sealed class InstructionCountLuaHook(int instructionCount) : LuaHook
    {
        protected override LuaEventMask EventMask => LuaEventMask.Count;

        protected override int InstructionCount { get; } = instructionCount;

        public int TimesCalled { get; private set; }

        protected override void Execute(LuaThread lua, ref LuaDebug debug)
        {
            TimesCalled++;
        }
    }

    [Test]
    public void CombinedCountLuaHooks_CallsBothAsExpected()
    {
        // Arrange
        var twoInstructionsHook = new InstructionCountLuaHook(2);
        var fiveInstructionsHook = new InstructionCountLuaHook(5);
        var combinedHook = LuaHook.Combine(twoInstructionsHook, fiveInstructionsHook);
        Lua.State.Hook = combinedHook;

        // Act
        for (var i = 0; i < 10; i++)
        {
            Lua.Execute("");
        }

        // Assert
        Assert.That(combinedHook.Hooks, Is.EqualTo(new LuaHook[] { twoInstructionsHook, fiveInstructionsHook }));
        Assert.That(twoInstructionsHook.TimesCalled, Is.EqualTo(5));
        Assert.That(fiveInstructionsHook.TimesCalled, Is.EqualTo(2));
    }

    private sealed class FunctionNameGrabberLuaHook : LuaHook
    {
        protected override LuaEventMask EventMask => LuaEventMask.Call;

        protected override int InstructionCount => 0;

        public List<string> FunctionNames { get; } = [];

        protected override void Execute(LuaThread lua, ref LuaDebug debug)
        {
            var functionName = debug.FunctionName.ToString();
            if (!string.IsNullOrWhiteSpace(functionName))
            {
                FunctionNames.Add(functionName);
            }
        }
    }

    [Test]
    public void FunctionNameGrabberLuaHook_GrabsFunctionNames()
    {
        // Arrange
        var hook = new FunctionNameGrabberLuaHook();
        Lua.State.Hook = hook;

        Lua.SetGlobal("func1", static () => { });
        Lua.SetGlobal("func2", static () => { });
        Lua.Execute("function func3() end");

        // Act
        Lua.Execute("func1() func2() func3()");

        // Assert
        Assert.That(hook.FunctionNames, Is.EqualTo(new[] { "func1", "func2", "func3" }));
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
