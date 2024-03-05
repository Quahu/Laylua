using System;
using System.IO;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests;

public class LuaFunctionTests : LuaTestBase
{
    [Test]
    public void Dump_Stream_WritesBinaryData()
    {
        // Arrange
        using var function = lua.Load("return 42");
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
        using var function = lua.Load("return 42");
        var ms = new MemoryStream();
        using var writer = new MyStreamLuaWriter(ms);

        // Act
        var errorCode = function.Dump(writer);

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(ms.Length, Is.Not.Zero);
    }

    private sealed class MyStreamLuaWriter(Stream stream) : LuaWriter
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
        using var originalFunction = lua.Load("return 42");
        var ms = new MemoryStream();

        // Act
        var errorCode = originalFunction.Dump(ms);
        ms.Position = 0;
        using var dumpedFunction = lua.Load(ms);
        using var originalResults = originalFunction.Call();
        using var dumpedResults = dumpedFunction.Call();

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(originalResults.First.GetValue<int>(), Is.EqualTo(dumpedResults.First.GetValue<int>()));
    }
}
