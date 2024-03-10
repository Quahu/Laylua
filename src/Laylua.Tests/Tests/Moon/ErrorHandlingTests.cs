namespace Laylua.Tests;

public class ErrorHandlingTests : LuaTestBase
{
    [Test]
    public void LuaError_LongJmp_GetsCaughtWithLuaPanicException()
    {
        // Act & Assert
        Assert.Throws<LuaPanicException>(() => Lua.RaiseError("This must be caught!"));
    }

    [Test]
    public void LuaError_ProtectedLongJmp_GetsCaughtWithLuaException()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Base);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("error('This must be caught!')"));
    }

    [Test]
    public unsafe void LuaError_LuaCFunction_ProtectedLongJmp_GetsCaughtWithLuaException()
    {
        // Arrange
        var value = 0;
        lua_register(L, "error", L =>
        {
            luaL_error(L, "error message");
            value = 1;
            return 0;
        });

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Throws<LuaException>(() => Lua.Execute("error()"));
            Assert.That(value, Is.Zero, "Lua error did not longjmp.");
        });
    }

    [Test]
    public void LuaError_LuaFunction_ProtectedLongJmp_GetsCaughtWithLuaException()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        using var function = Lua.Evaluate<LuaFunction>("return function() error('error message') end")!;

        // Act & Assert
        Assert.Throws<LuaException>(() => function.Call());
    }
}
