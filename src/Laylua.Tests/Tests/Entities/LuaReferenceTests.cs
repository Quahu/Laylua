using System;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests;

public class LuaReferenceTests : LuaTestBase
{
    [Test]
    public void GetReference_ThrowsObjectDisposedExceptionIfDisposed()
    {
        // Arrange
        var reference = lua.Evaluate<LuaTable>("return {}")!;

        // Act
        reference.Dispose();

        // Assert
        Assert.That(() => LuaReference.GetReference(reference), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public unsafe void NoLuaReferencesToAliveObject_ObjectIsNotGarbageCollected()
    {
        // Arrange
        using var reference = lua.Evaluate<LuaTable>("return {}")!;

        // Act
        lua_gc(L, LuaGC.Collect);

        LuaReference.PushValue(reference);
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
        var reference = lua.Evaluate<LuaTable>("return {}")!;

        // Act
        var referenceValue = LuaReference.GetReference(reference);
        reference.Dispose();

        lua_gc(L, LuaGC.Collect);

        var type = lua_rawgeti(L, LuaRegistry.Index, referenceValue);
        lua_pop(L);

        // Assert
        // Note: This isn't checking for nil because Lua changed the structure of the registry
        // after 5.4.2 causing the lookup to return some dummy number at the end instead of nil.
        Assert.That(type, Is.Not.EqualTo(LuaType.Table), "The disposed LuaReference's object was not garbage collected.");
    }

    [Test]
    public unsafe void NoReferencesToAliveObject_MarshalerFiresLeakedReferenceEvent()
    {
        // Arrange
        var reference = CreateTable();
        var leakedReference = -1;

        // Act
        lua_gc(L, LuaGC.Collect);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert
        Assert.That(leakedReference, Is.EqualTo(reference));
        return;

        int CreateTable()
        {
            var table = lua.Evaluate<LuaTable>("return {}")!;
            var reference = LuaReference.GetReference(table);
            lua.Marshaler.ReferenceLeaked += (_, e) =>
            {
                leakedReference = LuaReference.GetReference(e.Reference);
            };

            return reference;
        }
    }
}
