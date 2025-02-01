namespace Laylua.Tests;

public class LuaReferenceTests : LuaTestBase
{
    [Test]
    public void GetReference_ThrowsObjectDisposedExceptionIfDisposed()
    {
        // Arrange
        var reference = Lua.Evaluate<LuaTable>("return {}")!;

        // Act
        reference.Dispose();

        // Assert
        Assert.That(() => LuaReference.GetReference(reference), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public unsafe void NoLuaReferencesToAliveObject_ObjectIsNotGarbageCollected()
    {
        // Arrange
        using var reference = Lua.Evaluate<LuaTable>("return {}")!;

        // Act
        Lua.State.GC.Collect();

        Lua.Stack.Push(reference);
        var type = lua_type(L, -1);
        lua_pop(L);

        GC.KeepAlive(reference);

        // Assert
        Assert.That(type, Is.EqualTo(LuaType.Table));
    }

    [Test]
    public unsafe void NoReferencesToDeadObject_ObjectIsGarbageCollected()
    {
        // Arrange
        var reference = Lua.Evaluate<LuaTable>("return {}")!;

        // Act
        var referenceValue = LuaReference.GetReference(reference);
        reference.Dispose();

        Lua.State.GC.Collect();

        var type = lua_rawgeti(L, LuaRegistry.Index, referenceValue);
        lua_pop(L);

        // Assert
        // Note: This isn't checking for nil because Lua changed the structure of the registry
        // after 5.4.2 causing the lookup to return some dummy number at the end instead of nil.
        Assert.That(type, Is.Not.EqualTo(LuaType.Table), "The disposed LuaReference's object was not garbage collected.");
    }

    [Test]
    public void MainThreadReference_PushedToChildThreadStack_PushesReferenceToChildThreadStack()
    {
        // Arrange
        var reference = Lua.Evaluate<LuaTable>("return {}")!;
        using var thread = Lua.CreateThread();

        // Act
        thread.Stack.Push(reference);

        // Assert
        Assert.That(Lua.Stack.Count, Is.Zero);
        Assert.That(thread.Stack.Count, Is.EqualTo(1));
    }
}
