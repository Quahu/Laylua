namespace Laylua.Tests;

public class LuaWeakReferenceTests : LuaTestBase
{
    [Test]
    public unsafe void WeakLuaReference_RetrievesTargetCorrectly()
    {
        // Arrange
        using var reference = Lua.Evaluate<LuaTable>("return {}")!;

        Lua.Stack.Push(reference);
        var expectedTargetPointer = lua_topointer(L, -1);
        Lua.Stack.Pop();

        var weakReference = reference.CreateWeakReference();

        // Act
        Lua.State.GC.Collect();

        var isAlive = weakReference.TryGetValue(out var table);
        using var _ = table;

        Lua.Stack.Push(table);
        var targetPointer = lua_topointer(L, -1);
        Lua.Stack.Pop();

        // Assert
        Assert.That(isAlive, Is.True);
        Assert.That(table, Is.Not.Null);
        Assert.That((IntPtr) targetPointer, Is.EqualTo((IntPtr) expectedTargetPointer));
    }

    [Test]
    public void NoLuaReferencesToTarget_ObjectIsGarbageCollected()
    {
        // Arrange
        var weakReference = Lua.Evaluate<LuaWeakReference<LuaTable>>("return {}");

        // Act
        Lua.State.GC.Collect();

        var isAlive = weakReference.TryGetValue(out var function);
        using var _ = function;

        // Assert
        Assert.That(isAlive, Is.False);
        Assert.That(function, Is.Null);
    }
}