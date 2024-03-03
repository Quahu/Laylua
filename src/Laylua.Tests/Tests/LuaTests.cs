using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class LuaTests : LuaTestBase
    {
        [Test]
        public unsafe void CreateTable_SetsTableInRegistry()
        {
            // Arrange
            using var table = lua.CreateTable();

            // Act
            lua_rawgeti(L, LuaRegistry.Index, LuaReference.GetReference(table));
            var type = lua_type(L, -1);
            lua_pop(L);

            // Assert
            Assert.That(type, Is.EqualTo(LuaType.Table));
        }

        [Test]
        public unsafe void CreateUserData_SetsUserDataInRegistry()
        {
            // Arrange
            using var userData = lua.CreateUserData(0);

            // Act
            lua_rawgeti(L, LuaRegistry.Index, LuaReference.GetReference(userData));
            var type = lua_type(L, -1);
            lua_pop(L);

            // Assert
            Assert.That(type, Is.EqualTo(LuaType.UserData));
        }

        [Test]
        public unsafe void MainThread_ReturnsValidEntity()
        {
            // Arrange
            var mainThread = lua.MainThread;
            var mainThreadReference = LuaReference.GetReference(mainThread);

            // Act
            lua_rawgeti(L, LuaRegistry.Index, mainThreadReference);
            var type = lua_type(L, -1);
            lua_pop(L);

            // Assert
            Assert.That(LuaRegistry.IsPersistentReference(mainThreadReference), Is.True);
            Assert.That(type, Is.EqualTo(LuaType.Thread));
        }

        [Test]
        public unsafe void Globals_ReturnsValidEntity()
        {
            // Arrange
            var globals = lua.Globals;
            var globalsReference = LuaReference.GetReference(globals);

            // Act
            lua_rawgeti(L, LuaRegistry.Index, globalsReference);
            var type = lua_type(L, -1);
            lua_pop(L);

            // Assert
            Assert.That(LuaRegistry.IsPersistentReference(globalsReference), Is.True);
            Assert.That(type, Is.EqualTo(LuaType.Table));
        }
    }
}
