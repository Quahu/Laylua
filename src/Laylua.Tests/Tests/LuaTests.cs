using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private static IEnumerable<TestCaseData> CodeStreams
        {
            get
            {
                var bytes = "return 42"u8.ToArray();
                yield return new TestCaseData(new MemoryStream(bytes, 0, bytes.Length, false, true))
                    .SetArgDisplayNames("PublicBufferMemoryStream");

                yield return new TestCaseData(new MemoryStream(bytes, 0, bytes.Length, false, false))
                    .SetArgDisplayNames("PrivateBufferMemoryStream");
            }
        }

        [Test]
        [TestCaseSource(nameof(CodeStreams))]
        public void Load_Stream_LoadsCodeAndReturnsValidFunction(Stream stream)
        {
            // Arrange
            using var function = lua.Load(stream, "streamChunk");

            // Act
            using var results = function.Call();

            // Assert
            Assert.That(results.First.GetValue<int>(), Is.EqualTo(42));
        }

        [Test]
        [TestCaseSource(nameof(CodeStreams))]
        public void Load_StreamCustomReader_LoadsCodeAndReturnsValidFunction(Stream stream)
        {
            // Arrange
            using var reader = new MyStreamLuaChunkReader(stream);
            using var function = lua.Load(reader, "readerChunk");

            // Act
            using var results = function.Call();

            // Assert
            Assert.That(results.First.GetValue<int>(), Is.EqualTo(42));
        }

        private sealed class MyStreamLuaChunkReader(Stream stream) : LuaChunkReader
        {
            private readonly byte[] _buffer = GC.AllocateArray<byte>(128, pinned: true);

            protected override unsafe byte* Read(lua_State* L, out nuint bytesRead)
            {
                bytesRead = (nuint) stream.Read(_buffer);
                return (byte*) Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_buffer));
            }
        }
    }
}
