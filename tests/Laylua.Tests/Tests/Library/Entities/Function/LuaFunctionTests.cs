namespace Laylua.Tests;

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
        // Arrange
        Lua.Execute("""
                    function func(x)
                        return x
                    end
                    """);

        // Act
        using (var function = Lua.GetGlobal<LuaFunction>("func"))
        using (var results = function.Call())
        {
            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First.Value, Is.Null);
        }
    }

    [Test]
    public void CallMultiReturnLuaFunction()
    {
        // Arrange
        Lua.Execute("""
                    function func(x, y, z)
                        return z, y, x
                    end
                    """);

        // Act
        using (var function = Lua.GetGlobal<LuaFunction>("func"))
        using (var results = function.Call(3, 2, 1))
        {
            var expectedValues = new[]
            {
                1, 2, 3
            };

            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            // Assert
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
        // Arrange
        Lua.Execute("""
                    function func(x, ...)
                        local temp = { ... }
                        return x, #temp
                    end
                    """);

        // Act
        using (var function = Lua.GetGlobal<LuaFunction>("func"))
        using (var results = function.Call(42, 1, 2, 3))
        {
            var expectedValues = new[]
            {
                42, 3
            };

            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            // Assert
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
        // Arrange
        Lua.Execute("""
                    function func(table)
                        return table
                    end
                    """);

        // Act
        using (var function = Lua.GetGlobal<LuaFunction>("func"))
        using (var results = function.Call(new[]
        {
            1, 2, 3
        }))
        using (var table = results.First.GetValue<LuaTable>()!)
        {
            var actual = table.Values.ToArray<int>();
            try
            {
                var expected = new[]
                {
                    1, 2, 3
                };

                // Assert
                Assert.That(actual, Is.EquivalentTo(expected));
            }
            finally
            {
                LuaReference.Dispose(actual);
            }
        }
    }

    [Test]
    public void Call_CallParametersWithNullCaller_CallsFunctionOnMainThread()
    {
        // Arrange
        var function = Lua.Load("return 42");
        var parameters = new LuaFunctionCallParameters();

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(Lua.Stack[-1].GetValue<int>(), Is.EqualTo(42));

        Lua.Stack.Pop();
    }

    [Test]
    public void Call_CallParametersWithChildThreadCaller_CallsFunctionOnChildThread()
    {
        // Arrange
        var thread = Lua.CreateThread();
        var function = Lua.Load("return 42");
        var parameters = new LuaFunctionCallParameters
        {
            Caller = thread
        };

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(thread.Stack[-1].GetValue<int>(), Is.EqualTo(42));

        thread.Stack.Pop();
    }

    [Test]
    public void Call_CallParametersWithLimitedResults_RestrictsFunctionResults([Values(1, 3, 5, 6)] int resultCount)
    {
        // Arrange
        var function = Lua.Load("return 1, 2, 3, 4, 5");
        var parameters = new LuaFunctionCallParameters
        {
            ResultCount = resultCount
        };

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(resultCount));

        for (var i = 1; i <= resultCount && i < 6; i++)
        {
            Assert.That(Lua.Stack[i].GetValue<int>(), Is.EqualTo(i));
        }

        Lua.Stack.Pop(resultCount);
    }

    [Test]
    public void Call_CallParametersWithStackArgument_ReturnsExpectedResult()
    {
        // Arrange
        using var function = Lua.Load("local x = ... return x + 1");
        Lua.Stack.EnsureFreeCapacity(1);
        Lua.Stack.Push(41);

        var parameters = new LuaFunctionCallParameters
        {
            ArgumentCount = 1,
            ResultCount = 1
        };

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(1));
        Assert.That(Lua.Stack[-1].GetValue<int>(), Is.EqualTo(42));

        Lua.Stack.Pop();
    }

    [Test]
    public void Call_CallParametersWithChildThreadStackArgument_ReturnsExpectedResult()
    {
        // Arrange
        using var thread = Lua.CreateThread();
        using var function = Lua.Load("local x = ... return x + 1");
        thread.Stack.EnsureFreeCapacity(1);
        thread.Stack.Push(41);

        var parameters = new LuaFunctionCallParameters
        {
            Caller = thread,
            ArgumentCount = 1,
            ResultCount = 1
        };

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(thread.Stack.Count, Is.EqualTo(1));
        Assert.That(thread.Stack[-1].GetValue<int>(), Is.EqualTo(42));

        thread.Stack.Pop();
    }

    [Test]
    public void Call_CallParametersWithCallableArgument_CallsTargetFunction()
    {
        // Arrange
        using var callableArgument = Lua.Load("return 'argument'");
        using var function = Lua.Load("return 'target'");
        Lua.Stack.EnsureFreeCapacity(1);
        Lua.Stack.Push(callableArgument);

        var parameters = new LuaFunctionCallParameters
        {
            ArgumentCount = 1,
            ResultCount = 1
        };

        // Act
        function.Call(in parameters);

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(1));
        Assert.That(Lua.Stack[-1].GetValue<string>(), Is.EqualTo("target"));

        Lua.Stack.Pop();
    }
}
