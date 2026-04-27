using System.Reflection;
using Laylua.Marshaling;

namespace Laylua.Tests;

public class UserDataTests : LuaTestBase
{
    private unsafe delegate bool TryPushCachedIdentityDelegate(lua_State* state, Lua lua, object obj);

    private class StrictUserData1
    {
        public int Property { get; set; }

        public int Field;

        public int Method(int value)
        {
            return value;
        }
    }

    public override void SetUp()
    {
        base.SetUp();
        Marshaler.AllowsDynamicMarshaling = true;
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void UserData_PropertyFieldMethod_AccessibleFromLua()
    {
        // Arrange
        Lua["ud"] = new StrictUserData1();

        // Act
        var propertyResult = Lua.Evaluate<int>("ud.Property = 42 return ud.Property");
        var fieldResult = Lua.Evaluate<int>("ud.Field = 42 return ud.Field");
        var methodResult = Lua.Evaluate<int>("return ud:Method(42)");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(propertyResult, Is.EqualTo(42));
            Assert.That(fieldResult, Is.EqualTo(42));
            Assert.That(methodResult, Is.EqualTo(42));
        });
    }

    [Test]
    public void UserData_GetGlobalAsObject_ReturnsClrInstance()
    {
        // Arrange
        var instance = new StrictUserData1();
        Lua.SetGlobal("ud", instance);

        // Act
        var ud = Lua.GetGlobal<object>("ud");

        // Assert
        Assert.That(ud, Is.InstanceOf<StrictUserData1>());
    }

    [Test]
    public void UserData_MarshaledAsObjectType_MarshalsCorrectly()
    {
        // Arrange
        var ud = new StrictUserData1();
        Lua.SetGlobal("ud", ud);

        // Act
        var udAsObject = Lua.GetGlobal<object>("ud");

        // Assert
        Assert.That(udAsObject, Is.SameAs(ud));
    }

    [Test]
    public void UserData_ObjectIdentity_SameInstancePushedTwice_RawEqual()
    {
        // Arrange
        var instance = new StrictUserData1();
        Lua.OpenStandardLibraries();
        Lua["a"] = instance;
        Lua["b"] = instance;

        // Act & Assert
        Lua.Execute("assert(rawequal(a, b))");
    }

    [Test]
    public unsafe void UserData_IdentityCacheKey_IsDecoupledFromUserDataHandle()
    {
        // Arrange
        var instance = new StrictUserData1();
        Lua["ud"] = instance;

        // Act
        lua_getglobal(L, "ud");
        var userdata = (nint*) lua_touserdata(L, -1);
        if (userdata == null)
        {
            Assert.Fail("Expected userdata on the Lua stack.");
        }

        var handle = userdata![0];
        lua_pop(L, 1);

        var key = GetIdentityCacheKey(instance);

        // Assert
        Assert.That(key, Is.Not.EqualTo(handle));
    }

    [Test]
    public unsafe void UserData_IdentityCacheLookup_AfterLuaDisposing_ReturnsFalse()
    {
        // Arrange
        var instance = new StrictUserData1();
        Lua["ud"] = instance;

        var onLuaDisposingMethod = typeof(DefaultLuaMarshaler).GetMethod("OnLuaDisposing", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tryPushCachedIdentityMethod = typeof(DefaultLuaMarshaler).GetMethod("TryPushCachedIdentity", BindingFlags.Static | BindingFlags.NonPublic)!;
        var tryPushCachedIdentity = (TryPushCachedIdentityDelegate) tryPushCachedIdentityMethod.CreateDelegate(typeof(TryPushCachedIdentityDelegate));

        // Act
        onLuaDisposingMethod.Invoke(Marshaler, [Lua]);
        var wasPushed = tryPushCachedIdentity(L, Lua, instance);

        // Assert
        Assert.That(wasPushed, Is.False);
        AssertStackCount(0);
    }

    [Test]
    public void UserData_PushedToChildThread_AccessibleFromChildStack()
    {
        // Arrange
        var userData = new StrictUserData1
        {
            Property = 99
        };

        using var thread = Lua.CreateThread();
        thread.Stack.Push(userData);

        // Act & Assert
        Assert.That(thread.Stack.Count, Is.EqualTo(1));
    }

    private nint GetIdentityCacheKey(object obj)
    {
        var identityCachesField = typeof(DefaultLuaMarshaler).GetField("_identityCaches", BindingFlags.NonPublic | BindingFlags.Static)!;
        var identityCaches = identityCachesField.GetValue(null)!;
        var tryGetCacheMethod = identityCaches.GetType().GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public)!;
        var cacheArguments = new object?[]
        {
            Lua, null
        };

        var cacheFound = (bool) tryGetCacheMethod.Invoke(identityCaches, cacheArguments)!;
        Assert.That(cacheFound, Is.True);
        Assert.That(cacheArguments[1], Is.Not.Null);

        var cache = cacheArguments[1]!;
        var refsField = cache.GetType().GetField("Refs", BindingFlags.Instance | BindingFlags.Public)!;
        var refs = refsField.GetValue(cache)!;
        var tryGetEntryMethod = refs.GetType().GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public)!;
        var entryArguments = new[]
        {
            obj, null
        };

        var entryFound = (bool) tryGetEntryMethod.Invoke(refs, entryArguments)!;
        Assert.That(entryFound, Is.True);
        Assert.That(entryArguments[1], Is.Not.Null);

        var entry = entryArguments[1]!;
        var keyField = entry.GetType().GetField("Key", BindingFlags.Instance | BindingFlags.Public)!;
        return (nint) keyField.GetValue(entry)!;
    }
}
