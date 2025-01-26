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

    [Test]
    public void Execute_Utf8String_ExecutesCode()
    {
        // Arrange
        Lua.SetGlobal("x", 0);

        // Act
        Lua.Execute("x = 42"u8);

        // Assert
        Assert.That(Lua.GetGlobal<int>("x"), Is.EqualTo(42));
    }

    [Test]
    public void Evaluate_Utf8String_EvaluatesCode()
    {
        // Arrange & Act
        var result = Lua.Evaluate<int>("return 42"u8);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Load_Utf8String_LoadsCodeAndReturnsValidFunction()
    {
        // Arrange
        using var function = Lua.Load("return 42"u8);

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
        protected override int Read(Span<byte> buffer)
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
        protected override int Read(Span<byte> buffer)
        {
            return stream.Read(buffer);
        }
    }

    [Test]
    public void MainThread_ForMainThread_ReturnsMainThread()
    {
        // Act
        var mainThread = Lua.MainThread;

        // Assert
        Assert.That(mainThread, Is.EqualTo(Lua));
    }

    [Test]
    public unsafe void MainThread_ForNewThread_ReturnsMainThread()
    {
        // Arrange
        using var newThread = Lua.CreateThread();

        // Act
        var oldMainThread = Lua.MainThread;
        var newMainThread = newThread.MainThread;

        // Assert
        Assert.That((IntPtr) lua_getextraspace(oldMainThread.State.L), Is.EqualTo((IntPtr) lua_getextraspace(newMainThread.State.L)));
        Assert.That(newMainThread, Is.EqualTo(oldMainThread));
    }

    [Test]
    public unsafe void MainThread_WorksWithMultipleThreads()
    {
        const int ThreadCount = 100;

        // Arrange
        var threads = new LuaThread[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            threads[i] = Lua.CreateThread();
        }

        // Act
        var mainThreads = new Lua[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            mainThreads[i] = threads[i].MainThread;
        }

        // Assert
        for (var i = 0; i < ThreadCount; i++)
        {
            Assert.That((IntPtr) mainThreads[i].State.L, Is.EqualTo((IntPtr) threads[i].MainThread.State.L));
        }
    }

    [Test]
    public unsafe void LuaFromExtraSpace_ForMainThread_ReturnsMainThread()
    {
        // Act
        var fromExtraSpace = Lua.FromExtraSpace(L);

        // Assert
        Assert.That(fromExtraSpace, Is.EqualTo(Lua));
    }

    [Test]
    public unsafe void LuaFromExtraSpace_ForChildThread_ReturnsMainThread()
    {
        // Act
        using var thread = Lua.CreateThread();
        var fromExtraSpace = Lua.FromExtraSpace(thread.State.L);

        // Assert
        Assert.That(fromExtraSpace, Is.EqualTo(Lua));
    }

    [Test]
    public unsafe void LuaThreadFromExtraSpace_ForChildThread_ReturnsChildThread()
    {
        // Act
        using var thread = Lua.CreateThread();
        var fromExtraSpace = LuaThread.FromExtraSpace(thread.State.L);

        // Assert
        Assert.That(fromExtraSpace, Is.EqualTo(thread));
    }

    [Test]
    public void Warn_OnePieceMessage_TriggersWarningEvent()
    {
        // Arrange
        const string Message = "This is a warning message.";
        var expectedWarnings = new[]
        {
            Message
        };

        var actualWarnings = new List<string>();

        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.WarningEmitted += (_, e) =>
        {
            actualWarnings.Add(e.Message.ToString());
        };

        // Act
        Lua.Execute($"warn('{Message}')");

        // Assert
        Assert.That(actualWarnings, Is.EqualTo(expectedWarnings));
    }

    [Test]
    public void EmitWarning_OnePieceMessage_TriggersWarningEvent()
    {
        // Arrange
        const string Message = "This is a warning message.";
        var expectedWarnings = new[]
        {
            Message
        };

        var actualWarnings = new List<string>();

        Lua.WarningEmitted += (_, e) =>
        {
            actualWarnings.Add(e.Message.ToString());
        };

        // Act
        Lua.EmitWarning(Message);

        // Assert
        Assert.That(actualWarnings, Is.EqualTo(expectedWarnings));
    }

    [Test]
    public void Warn_MultiPieceMessage_TriggersSingleWarningEvent()
    {
        // Arrange
        const string Message = "Youshallnotpass.";
        const string SplitMessage = "'You', 'shall', 'not', 'pass.'";
        var expectedWarnings = new[]
        {
            Message
        };

        var actualWarnings = new List<string>();

        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.WarningEmitted += (_, e) =>
        {
            actualWarnings.Add(e.Message.ToString());
        };

        // Act
        Lua.Execute($"warn({SplitMessage})"); // 

        // Assert
        Assert.That(actualWarnings, Is.EqualTo(expectedWarnings));
    }

    [Test]
    public void Warn_OffOnControlMessages_TogglesEmitsWarningsAccordingly()
    {
        // Arrange
        const string Message = "This is a warning message.";
        var expectedWarnings = new[]
        {
            "@off",
            "@off",
            "@on",
            Message
        };

        var actualWarnings = new List<string>();

        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.WarningEmitted += (_, e) =>
        {
            actualWarnings.Add(e.Message.ToString());
        };

        // Act & Assert
        Lua.Execute("warn('@off')");
        Assert.That(Lua.EmitsWarnings, Is.False);

        Lua.Execute("warn('@off')");
        Lua.Execute($"warn('{Message}')");

        Lua.Execute("warn('@on')");
        Assert.That(Lua.EmitsWarnings, Is.True);

        Lua.Execute($"warn('{Message}')");

        Assert.That(actualWarnings, Is.EqualTo(expectedWarnings));
    }

    [Test]
    public void Warn_CustomControlMessage_TriggersWarningEventWithIsControlTrue()
    {
        // Arrange
        const string Message = "@custom-control-message";
        var expectedWarnings = new[]
        {
            Message
        };

        var actualWarnings = new List<string>();

        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.WarningEmitted += (_, e) =>
        {
            actualWarnings.Add(e.Message.ToString());
        };

        // Act
        Lua.Execute($"warn('{Message}')");

        // Assert
        Assert.That(actualWarnings, Is.EqualTo(expectedWarnings));
    }

    [Test]
    public void Warn_ThrowingExceptionEventHandler_Errors()
    {
        // Arrange
        const string ExceptionMessage = "Warning event handler exception.";

        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.WarningEmitted += static (_, _) => throw new Exception(ExceptionMessage);

        // Act & Assert
        Assert.That(() => Lua.Execute("warn('')"), Throws.TypeOf<LuaException>().With.InnerException.Not.Null.And.InnerException.Message.EqualTo(ExceptionMessage));
    }

    [Test]
    public void EmitWarning_ThrowingExceptionEventHandler_Throws()
    {
        // Arrange
        const string ExceptionMessage = "Warning event handler exception.";

        Lua.WarningEmitted += static (_, _) => throw new Exception(ExceptionMessage);

        // Act & Assert
        Assert.That(() => Lua.EmitWarning(""), Throws.TypeOf<LuaPanicException>().With.InnerException.Not.Null.And.InnerException.Message.EqualTo(ExceptionMessage));
    }
}
