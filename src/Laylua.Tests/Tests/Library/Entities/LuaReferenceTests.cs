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

    [Test]
    public void MainThreadReference_MainThreadDisposed_InvalidatesReference()
    {
        // Arrange
        var lua1 = CreateLua();
        var reference = lua1.Evaluate<LuaTable>("return {}")!;

        // Act
        lua1.Dispose();

        // Assert
        Assert.That(() => reference.IsEmpty, Throws.TypeOf<ObjectDisposedException>().With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(typeof(LuaTable).FullName));
    }

    [Test]
    public void GCCallback_TableGarbageCollectedObject_InvokesCallbacks()
    {
        // Arrange
        var callback1Invoked = false;
        var callback2Invoked = false;
        var callback3Invoked = false;
        Action callback1 = () => callback1Invoked = true;
        Action callback2 = () => callback2Invoked = true;
        Action callback3 = () => callback3Invoked = true;
        var table = Lua.CreateTable();

        Lua.State.GC.RegisterCallback(table, callback1);
        Lua.State.GC.RegisterCallback(table, callback2);
        Lua.State.GC.RegisterCallback(table, callback3);

        // Act
        table.Dispose();
        Lua.State.GC.Collect();

        // Assert
        Assert.That(callback1Invoked, Is.True);
        Assert.That(callback2Invoked, Is.True);
        Assert.That(callback3Invoked, Is.True);
        GC.KeepAlive(callback1);
        GC.KeepAlive(callback2);
        GC.KeepAlive(callback3);
    }

    [Test]
    public void GCCallback_ThreadGarbageCollectedObject_InvokesCallbacks()
    {
        // Arrange
        var callback1Invoked = false;
        var callback2Invoked = false;
        var callback3Invoked = false;
        Action callback1 = () => callback1Invoked = true;
        Action callback2 = () => callback2Invoked = true;
        Action callback3 = () => callback3Invoked = true;
        var thread = Lua.CreateThread();

        Lua.State.GC.RegisterCallback(thread, callback1);
        Lua.State.GC.RegisterCallback(thread, callback2);
        Lua.State.GC.RegisterCallback(thread, callback3);

        // Act
        thread.Dispose();
        Lua.State.GC.Collect();

        // Assert
        Assert.That(callback1Invoked, Is.True);
        Assert.That(callback2Invoked, Is.True);
        Assert.That(callback3Invoked, Is.True);
        GC.KeepAlive(callback1);
        GC.KeepAlive(callback2);
        GC.KeepAlive(callback3);
    }

    [Test]
    public void GCCallback_Collect_ThrowingDelegate_RaisesError()
    {
        const string ExceptionMessage = "Callback error.";

        // Arrange
        Action callback = () => throw new Exception(ExceptionMessage);
        var thread = Lua.CreateThread();

        Lua.State.GC.RegisterCallback(thread, callback);

        // Act & Assert
        thread.Dispose();

        Assert.That(() => Lua.State.GC.Collect(), Throws.Nothing);

        GC.KeepAlive(callback);
    }

    [Test]
    public void GCCallback_Allocating_ThrowingDelegate_RaisesError()
    {
        const string ExceptionMessage = "Callback error.";

        // Arrange
        Action callback = static () => throw new Exception(ExceptionMessage);
        var thread = Lua.CreateThread();

        Lua.State.GC.RegisterCallback(thread, callback);

        // Act & Assert
        thread.Dispose();

        Assert.That(() =>
        {
            var tables = new List<LuaTable>();
            for (var i = 0; i < 100; i++)
            {
                tables.Add(Lua.CreateTable());
            }

            LuaReference.Dispose(tables);
        }, Throws.Nothing);

        GC.KeepAlive(callback);
    }

    [Test]
    public void GCCallback_Collect_InNestedFunction_ThrowingDelegate_RaisesError()
    {
        const string ExceptionMessage = "Callback error.";

        // Arrange
        Action callback = static () => throw new Exception(ExceptionMessage);
        var thread = Lua.CreateThread();

        Lua.State.GC.RegisterCallback(thread, callback);

        var nestedDelegate = () => Lua.State.GC.Collect();
        Lua.SetGlobal(nameof(nestedDelegate), nestedDelegate);
        using var nestedDelegateFunction = Lua.GetGlobal<LuaFunction>(nameof(nestedDelegate))!;

        // Act & Assert
        thread.Dispose();

        Assert.That(() => nestedDelegateFunction.Call(), Throws.Nothing);

        GC.KeepAlive(callback);
        GC.KeepAlive(nestedDelegate);
    }
}
