namespace Laylua.Tests;

public class LuaUserDataTests : LuaTestBase
{
    [Test]
    public unsafe void Size_LuaIOStdout_ReturnsCorrectSize()
    {
        // Arrange
        luaL_requiref(L, LUA_IOLIBNAME, luaopen_io, true);
        lua_pop(L);

        using var stdoutUserData = Lua.Evaluate<LuaUserData>("return io.stdout");

        // Act & Assert
        Assert.That(stdoutUserData, Is.Not.Null);
        Assert.That(stdoutUserData!.Size, Is.EqualTo(16));
    }
}
