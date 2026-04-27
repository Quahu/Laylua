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
        LuaCFunction function = L =>
        {
            luaL_error(L, "error message");
            value = 1;
            return 0;
        };

        lua_register(L, "error", function);
        GC.KeepAlive(function);

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

    [Test]
    public void LuaError_NestedLuaFunction_ProtectedLongJmp_GetsCaughtWithLuaException()
    {
        // Arrange
        Lua.SetGlobal("print", (Action<int>) Console.WriteLine);
        Lua.SetGlobal("func", (string code) => Lua.Execute(code));

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute(@"print(func('print(\'abc\')'))"));
    }

    [Test]
    public unsafe void LuaArith_ThrowingMetamethod_GetsCaughtWithLuaPanicException()
    {
        // Arrange
        const string ExceptionMessage = "Metamethod error.";

        Action throwingMetamethod = static () => throw new Exception(ExceptionMessage);

        using var metatable = Lua.CreateTable();

        metatable.SetValue(LuaMetatableKeys.__add, throwingMetamethod);

        using var objTable = Lua.CreateTable();
        objTable.SetMetatable(metatable);

        Lua.Stack.Push(objTable);
        Lua.Stack.Push(objTable);

        // Act & Assert
        Assert.That(() => lua_arith(L, LuaOperation.Add), Throws.TypeOf<LuaPanicException>().With.InnerException.TypeOf<Exception>().And.InnerException.Message.EqualTo(ExceptionMessage));

        Lua.Stack.Pop(2);
        GC.KeepAlive(throwingMetamethod);
    }

    [Test]
    public void RaiseError_String_PreservesMessage()
    {
        // Arrange
        const string ErrorMessage = "custom error from callback";
        Lua.SetGlobal("func", () =>
        {
            Lua.RaiseError(ErrorMessage);
        });

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func()"));
        Assert.That(ex!.Message, Does.Contain(ErrorMessage));
    }

    [Test]
    public void RaiseError_Exception_PreservesMessage()
    {
        // Arrange
        const string ErrorMessage = "custom exception message";
        Lua.SetGlobal("func", () =>
        {
            Lua.RaiseError(new InvalidOperationException(ErrorMessage));
        });

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func()"));
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo(ErrorMessage));
    }

    [Test]
    public void RaiseError_StringAndException_PreservesMessage()
    {
        // Arrange
        const string ErrorMessage = "lua-visible error";
        const string InnerMessage = "inner exception detail";
        Lua.SetGlobal("func", () =>
        {
            Lua.RaiseError(ErrorMessage, new InvalidOperationException(InnerMessage));
        });

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func()"));
        Assert.That(ex!.Message, Does.Contain(ErrorMessage));
        Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo(InnerMessage));
    }

    [Test]
    public void RaiseArgumentError_PreservesMessage()
    {
        // Arrange
        const string ErrorMessage = "must be positive";
        Lua.SetGlobal("func", () =>
        {
            Lua.RaiseArgumentError(1, ErrorMessage);
        });

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func(42)"));
        Assert.That(ex!.Message, Does.Contain(ErrorMessage));
        Assert.That(ex.Message, Does.Contain("bad argument #1"));
        Assert.That(ex.Message, Does.Contain("func"));
    }

    [Test]
    public void RaiseError_FromHookSubclass_PreservesMessage()
    {
        // Arrange
        const string ErrorMessage = "hook timeout exceeded";
        Lua.SetGlobal("func", () =>
        {
            // This simulates what a user hook would do: throw an exception
            // that should be visible as the Lua error message
            throw new InvalidOperationException(ErrorMessage);
        });

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func()"));
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo(ErrorMessage));
    }

    [Test]
    public void CustomLongjmp_StressTest_NoLeaksOrCorruption()
    {
        // Arrange
        // Runs the custom longjmp path thousands of times.
        // The test base's NativeMemoryLuaAllocator asserts zero bytes at teardown,
        // so any Lua-side memory leak will fail the test.
        // Stack is also checked to be empty at teardown.
        Lua.OpenLibrary(LuaLibraries.Standard.Base);

        // Act
        // Lua-side error() -> pcall longjmp
        for (var i = 0; i < 2000; i++)
        {
            var ex = Assert.Throws<LuaException>(() => Lua.Execute($"error('iteration {i}')"));
            Assert.That(ex!.Message, Does.Contain($"iteration {i}"));
        }

        // C# callback -> RaiseError -> LuaRaisedException -> ManagedPanicAsm -> lua_error -> longjmp
        Lua.SetGlobal("raiser", (string msg) =>
        {
            Lua.RaiseError(msg);
        });

        for (var i = 0; i < 2000; i++)
        {
            var ex = Assert.Throws<LuaException>(() => Lua.Execute($"raiser('callback {i}')"));
            Assert.That(ex!.Message, Does.Contain($"callback {i}"));
        }

        // nested pcalls - Lua pcall inside C# callback inside Lua pcall
        Lua.SetGlobal("nester", () =>
        {
            Lua.Execute("error('inner')");
        });

        for (var i = 0; i < 1000; i++)
        {
            var ex = Assert.Throws<LuaException>(() => Lua.Execute("nester()"));

            // Inner LuaException from Lua.Execute("error('inner')") is wrapped as InnerException
            Assert.That(ex!.InnerException, Is.TypeOf<LuaException>());
            Assert.That(ex.InnerException!.Message, Does.Contain("inner"));
        }

        // exception thrown from C# callback (not RaiseError)
        Lua.SetGlobal("thrower", new Action(() => throw new InvalidOperationException("boom")));

        for (var i = 0; i < 1000; i++)
        {
            var ex = Assert.Throws<LuaException>(() => Lua.Execute("thrower()"));
            Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(0), "Stack corrupted after stress test");
    }

    [Test]
    public void LuaError_NonStringErrorObject_DoesNotLeaveStackDirty()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Base);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("error(42)"));
        Assert.That(Lua.Stack.Count, Is.EqualTo(0), "Stack dirty after non-string error");
    }

    [Test]
    public void LuaError_TableErrorObject_DoesNotLeaveStackDirty()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Base);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("error({})"));
        Assert.That(Lua.Stack.Count, Is.EqualTo(0), "Stack dirty after table error");
    }

    [Test]
    public void LuaError_BooleanErrorObject_DoesNotLeaveStackDirty()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Base);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("error(true)"));
        Assert.That(Lua.Stack.Count, Is.EqualTo(0), "Stack dirty after boolean error");
    }

    [Test]
    public unsafe void LuaError_DisposingAnotherLua_DoesNotInvalidatePendingErrorInfo()
    {
        // Arrange
        using var otherLua = new Lua();
        using var pendingErrorLua = new Lua();
        pendingErrorLua.SetGlobal("raiser", new Action(() =>
        {
            pendingErrorLua.RaiseError("state 2 message", new InvalidOperationException("state 2 inner"));
        }));

        lua_getglobal(pendingErrorLua.State.L, "raiser");
        var status = lua_pcall(pendingErrorLua.State.L, 0, 0, 0);
        Assert.That(status, Is.EqualTo(LuaStatus.RuntimeError));
        Assert.That(lua_gettop(pendingErrorLua.State.L), Is.EqualTo(1));

        // Act
        otherLua.Dispose();
        var exception = ConstructLuaExceptionFromStack(pendingErrorLua);

        // Assert
        Assert.That(exception.Message, Does.Contain("state 2 message"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("state 2 inner"));
        Assert.That(lua_gettop(pendingErrorLua.State.L), Is.EqualTo(0));
    }

    private static LuaException ConstructLuaExceptionFromStack(Lua lua)
    {
        var method = typeof(LuaException).GetMethod("ConstructFromStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (LuaException) method!.Invoke(null, [lua])!;
    }
}
