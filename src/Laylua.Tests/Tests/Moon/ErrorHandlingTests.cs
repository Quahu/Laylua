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
        lua_register(L, "error", L =>
        {
            luaL_error(L, "error message");
            value = 1;
            return 0;
        });

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
}
