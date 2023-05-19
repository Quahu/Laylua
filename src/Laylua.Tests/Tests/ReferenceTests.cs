using System;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests;

[Order(-2)]
public class ReferenceTests : LuaFixture
{
    [Test]
    public void LuaReference_Disposed_ThrowsObjectDisposedException()
    {
        var reference = lua.Evaluate<LuaTable>("return {}")!;
        reference.Dispose();
        Assert.Throws<ObjectDisposedException>(() => LuaReference.GetReference(reference));
    }

    [Test]
    public unsafe void LuaReference_NoLuaReferencesToObject_ObjectIsNotGarbageCollected()
    {
        using (var reference = lua.Evaluate<LuaTable>("return {}")!)
        {
            lua_gc(L, LuaGC.Collect);

            LuaReference.PushValue(reference);
            Assert.AreEqual(LuaType.Table, lua_type(L, -1));

            lua_pop(L);
            GC.KeepAlive(reference);
        }
    }

    [Test]
    public unsafe void DisposedLuaReference_NoLuaReferencesToObject_ObjectIsGarbageCollected()
    {
        var reference = lua.Evaluate<LuaTable>("return {}")!;
        var referenceValue = LuaReference.GetReference(reference);
        reference.Dispose();

        lua_gc(L, LuaGC.Collect);

        // Note: This isn't checking for nil because Lua changed the structure of the registry
        // after 5.4.2 causing the lookup to return some dummy number at the end instead of nil.
        Assert.AreNotEqual(LuaType.Table, lua_rawgeti(L, LuaRegistry.Index, referenceValue), "The disposed LuaReference's object was not garbage collected.");

        lua_pop(L);
    }
}
