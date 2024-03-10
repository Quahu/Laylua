namespace Laylua.Tests;

public class LuaFunctionTests : LuaTestBase
{
    [Test]
    public void Dump_Stream_WritesBinaryData()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var ms = new MemoryStream();

        // Act
        var errorCode = function.Dump(ms);

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(ms.Length, Is.Not.Zero);
    }

    [Test]
    public void Dump_Writer_WritesBinaryData()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var ms = new MemoryStream();
        using var writer = new MyStreamLuaChunkWriter(ms);

        // Act
        var errorCode = function.Dump(writer);

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(ms.Length, Is.Not.Zero);
    }

    private sealed class MyStreamLuaChunkWriter(Stream stream) : LuaChunkWriter
    {
        protected override unsafe int Write(lua_State* L, byte* data, nuint length)
        {
            stream.Write(new Span<byte>(data, (int) length));
            return 0;
        }
    }

    [Test]
    public void Dump_Stream_WritesBinaryData_CanBeLoaded()
    {
        // Arrange
        using var originalFunction = Lua.Load("return 42");
        var ms = new MemoryStream();

        // Act
        var errorCode = originalFunction.Dump(ms);
        Assume.That(errorCode, Is.Zero);

        ms.Position = 0;
        using var dumpedFunction = Lua.Load(ms);
        using var originalFunctionResults = originalFunction.Call();
        using var dumpedFunctionResults = dumpedFunction.Call();

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(originalFunctionResults.First.GetValue<int>(), Is.EqualTo(dumpedFunctionResults.First.GetValue<int>()));
    }
}
