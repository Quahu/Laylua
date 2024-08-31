using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Laylua.Tests;

public class LuaTests : LuaTestBase
{
    [Test]
    public unsafe void CreateTable_SetsTableInRegistry()
    {
        // Arrange
        using var table = Lua.CreateTable();

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
        using var userData = Lua.CreateUserData(0);

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
        var mainThread = Lua.MainThread;
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
        var globals = Lua.Globals;
        var globalsReference = LuaReference.GetReference(globals);

        // Act
        lua_rawgeti(L, LuaRegistry.Index, globalsReference);
        var type = lua_type(L, -1);
        lua_pop(L);

        // Assert
        Assert.That(LuaRegistry.IsPersistentReference(globalsReference), Is.True);
        Assert.That(type, Is.EqualTo(LuaType.Table));
    }

    [Test]
    public void Execute_String_ExecutesCode()
    {
        // Arrange
        Lua.SetGlobal("x", 0);

        // Act
        Lua.Execute("x = 42");

        // Assert
        Assert.That(Lua.GetGlobal<int>("x"), Is.EqualTo(42));
    }

    [Test]
    public void Evaluate_String_EvaluatesCode()
    {
        // Arrange & Act
        var result = Lua.Evaluate<int>("return 42");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Load_String_LoadsCodeAndReturnsValidFunction()
    {
        // Arrange
        using var function = Lua.Load("return 42");

        // Act
        using var results = function.Call();

        // Assert
        Assert.That(results.First.GetValue<int>(), Is.EqualTo(42));
    }

    private static IEnumerable<TestCaseData> CodeStreams
    {
        get
        {
            var bytes = "return 42"u8.ToArray();
            yield return MakeMemoryStream(bytes, true);
            yield return MakeMemoryStream(bytes, false);

            static TestCaseData MakeMemoryStream(byte[] bytes, bool publiclyVisible)
            {
                return new TestCaseData(new MemoryStream(bytes, 0, bytes.Length, false, publiclyVisible))
                    .SetArgDisplayNames($"MemoryStream {(publiclyVisible ? "Public" : "Private")}Buffer");
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(CodeStreams))]
    public void Load_Stream_LoadsCodeAndReturnsValidFunction(Stream stream)
    {
        // Arrange
        using var _ = stream;

        // Act
        using var function = Lua.Load(stream, "streamChunk");
        using var results = function.Call();

        // Assert
        Assert.That(results.First.GetValue<int>(), Is.EqualTo(42));
    }

    // Q: This test also validates that there's no leaked PanicInfo from lua_load + lua_error.
    [Test]
    public void Load_ThrowingStream_ThrowsExceptionCorrectly()
    {
        // Arrange
        using var stream = new IOThrowingStream();

        // Act & Assert
        Assert.That(() =>
        {
            using var function = Lua.Load(stream, "streamChunk");
        }, Throws.TypeOf<LuaException>().And.InnerException.TypeOf<IOException>());
    }

    private sealed class IOThrowingStream() : MemoryStream([], 0, 0, true, false)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("Test IO exception.");
        }
    }

    // Q: This test also validates that there's no leaked PanicInfo from lua_error.
    [Test]
    public void Load_ThrowingChunkReader_ThrowsExceptionCorrectly()
    {
        // Arrange
        var reader = new ThrowingLuaChunkReader();

        // Act & Assert
        Assert.That(() =>
        {
            using var function = Lua.Load(reader, "readerChunk");
        }, Throws.TypeOf<LuaException>().And.InnerException.TypeOf<IOException>());
    }

    private sealed class ThrowingLuaChunkReader : LuaChunkReader
    {
        protected override unsafe byte* Read(lua_State* L, out UIntPtr bytesRead)
        {
            throw new IOException("Test IO exception.");
        }
    }

    [Test]
    [TestCaseSource(nameof(CodeStreams))]
    public void Load_StreamCustomReader_LoadsCodeAndReturnsValidFunction(Stream stream)
    {
        // Arrange
        using var _ = stream;
        var reader = new MyStreamLuaChunkReader(stream);
        using var function = Lua.Load(reader, "readerChunk");

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

    [Test]
    public void GetThread_MainThread_ReturnsMainThread()
    {
        // Act
        using var thread = Lua.GetThread();

        // Assert
        Assert.That(thread, Is.EqualTo(Lua.MainThread));
    }

    [Test]
    public unsafe void GetThread_NewThread_ReturnsNewThread()
    {
        // Arrange
        using var newLua = Lua.CreateThread();

        // Act
        using var oldThread = Lua.GetThread();
        using var newThread = newLua.GetThread();

        // Assert
        Assert.That(newThread, Is.Not.EqualTo(oldThread));
    }

    [Test]
    public unsafe void GetThread_WorksWithMultipleThreads()
    {
        const int ThreadCount = 10;

        // Arrange
        var luas = new Lua[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            luas[i] = Lua.CreateThread();
        }

        // Act
        var threads = new LuaThread[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            threads[i] = luas[i].GetThread();
        }

        // Assert
        for (var i = 0; i < ThreadCount; i++)
        {
            Assert.That((IntPtr) threads[i].L, Is.EqualTo((IntPtr) luas[i].State.L));
        }
    }
}
