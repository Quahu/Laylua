﻿namespace Laylua.Tests;

public class LuaFunctionTests : LuaTestBase
{
    [Test]
    public void Dump_Stream_WritesBinaryData()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var stream = new MemoryStream();

        // Act
        var errorCode = function.Dump(stream);

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(stream.Length, Is.Not.Zero);
    }

    [Test]
    public void Dump_StreamChunkWriter_WritesBinaryData()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var ms = new MemoryStream();
        var writer = new MyStreamLuaChunkWriter(ms);

        // Act
        var errorCode = function.Dump(writer);

        // Assert
        Assert.That(errorCode, Is.Zero);
        Assert.That(ms.Length, Is.Not.Zero);
    }

    private sealed class MyStreamLuaChunkWriter(Stream stream) : LuaChunkWriter
    {
        protected override int Write(ReadOnlySpan<byte> data)
        {
            stream.Write(data);
            return 0;
        }
    }

    [Test]
    public void Dump_ThrowingStream_ThrowsExceptionCorrectly()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var stream = new IOThrowingStream();

        // Act & Assert
        Assert.That(() =>
        {
            _ = function.Dump(stream);
        }, Throws.TypeOf<LuaException>().And.InnerException.TypeOf<IOException>());
    }

    private sealed class IOThrowingStream() : MemoryStream([], 0, 0, true, false)
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("Test IO exception.");
        }
    }

    [Test]
    public void Dump_ThrowingWriter_ThrowsExceptionCorrectly()
    {
        // Arrange
        using var function = Lua.Load("return 42");
        var writer = new ThrowingLuaChunkWriter();

        // Act & Assert
        Assert.That(() =>
        {
            _ = function.Dump(writer);
        }, Throws.TypeOf<LuaException>().And.InnerException.TypeOf<IOException>());
    }

    private sealed class ThrowingLuaChunkWriter : LuaChunkWriter
    {
        protected override int Write(ReadOnlySpan<byte> data)
        {
            throw new IOException("Test IO exception.");
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

    [Test]
    public void CallLuaFunctionWithNull()
    {
        Lua.Execute("""
                    function func(x)
                        return x
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call())
        {
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First.Value, Is.Null);
        }
    }

    [Test]
    public void CallMultiReturnLuaFunction()
    {
        Lua.Execute("""
                    function func(x, y, z)
                        return z, y, x
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(3, 2, 1))
        {
            var expectedValues = new[] { 1, 2, 3 };
            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            var i = 0;
            foreach (var result in results)
            {
                var expected = expectedValues[i++];
                var actual = result.GetValue<int>();
                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void CallVariadicLuaFunction()
    {
        Lua.Execute("""
                    function func(x, ...)
                        local temp = { ... }
                        return x, #temp
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(42, 1, 2, 3))
        {
            var expectedValues = new[] { 42, 3 };

            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            var i = 0;
            foreach (var result in results)
            {
                var expected = expectedValues[i++];
                var actual = result.GetValue<int>();
                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void CallTableLuaFunction()
    {
        Lua.Execute("""
                    function func(table)
                        return table
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(new[] { 1, 2, 3 }))
        using (var table = results.First.GetValue<LuaTable>()!)
        {
            var actual = table.Values.ToArray<int>();
            try
            {
                var expected = new[] { 1, 2, 3 };
                Assert.That(actual, Is.EquivalentTo(expected));
            }
            finally
            {
                LuaReference.Dispose(actual);
            }
        }
    }
}
