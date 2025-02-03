using System;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents Lua hook debug information.
///     <para/>
///     See <a href="https://www.lua.org/manual/5.4/manual.html#4.7">Lua manual</a>.
/// </summary>
public unsafe ref struct LuaDebug
{
    /// <summary>
    ///     Gets a reasonable name for the given function.
    /// </summary>
    public LuaString FunctionName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Name);
            return new(Ar->name);
        }
    }

    /// <summary>
    ///     Gets a value explaining the name field.
    ///     This can be "global", "local", "method", "field", "upvalue", or "", according to how the function was called.
    /// </summary>
    public LuaString FunctionTypeName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Name);
            return new(Ar->namewhat);
        }
    }

    /// <summary>
    ///     Gets the string "Lua" if the function is a Lua function, "C" if it is a C function, "main" if it is the main part of a chunk.
    /// </summary>
    public LuaString ChunkName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return new(Ar->what);
        }
    }

    /// <summary>
    ///     Gets the source of the chunk that created the function.
    ///     If source starts with a '@', it means that the function was defined in a file where the file name follows the '@'.
    ///     If source starts with a '=', the remainder of its contents describes the source in a user-dependent manner.
    ///     Otherwise, the function was defined in a string where source is that string.
    /// </summary>
    public LuaString Source
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return new(Ar->source, Ar->srclen);
        }
    }

    /// <summary>
    ///     Gets the current line where the given function is executing.
    ///     When no line information is available, this returns <c>-1</c>.
    /// </summary>
    public int CurrentLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.CurrentLine);
            return Ar->currentline;
        }
    }

    /// <summary>
    ///     Gets the line number where the definition of the function starts.
    /// </summary>
    public int FunctionDefinitionStartLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return Ar->linedefined;
        }
    }

    /// <summary>
    ///     Gets the line number where the definition of the function ends.
    /// </summary>
    public int FunctionDefinitionEndLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return Ar->lastlinedefined;
        }
    }

    /// <summary>
    ///     Gets the number of upvalues of the function.
    /// </summary>
    public int FunctionUpvalueCount
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return Ar->nups;
        }
    }

    /// <summary>
    ///     Gets the number of parameters of the function (always <c>0</c> for C functions).
    /// </summary>
    public int FunctionParameterCount
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return Ar->nparams;
        }
    }

    /// <summary>
    ///     Gets whether the function is a variadic function (always true for C functions).
    /// </summary>
    public bool IsFunctionVariadic
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return Ar->isvararg;
        }
    }

    /// <summary>
    ///     Gets whether the function invocation was called by a tail call.
    ///     In this case, the caller of this level is not in the stack.
    /// </summary>
    public bool IsTailCall
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.TailCall);
            return Ar->istailcall;
        }
    }

    /// <summary>
    ///     Gets the index in the stack of the first value being "transferred", that is,
    ///     parameters in a call or return values in a return. (The other values are in consecutive indices.)
    ///     Using this index, you can access and modify these values through lua_getlocal and lua_setlocal.
    ///     This field is only meaningful during a call hook, denoting the first parameter, or a return hook, denoting the first value being returned.
    ///     (For call hooks, this value is always 1.)
    /// </summary>
    public int FirstTransferredValueIndex
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Transfer);
            return Ar->ftransfer;
        }
    }

    /// <summary>
    ///     The number of values being transferred (see previous item).
    ///     (For calls of Lua functions, this value is always equal to nparams.)
    /// </summary>
    public int TransferredValueCount
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Transfer);
            return Ar->ntransfer;
        }
    }

    /// <summary>
    ///     Gets a "printable" version of <see cref="Source"/>, to be used in error messages.
    /// </summary>
    public LuaString ShortSource
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return new(Ar->short_src, LUA_IDSIZE);
        }
    }

    private readonly lua_Debug* Ar => (lua_Debug*) Unsafe.AsPointer(ref Unsafe.AsRef(in _activationRecord));

    private readonly lua_State* L;
    private readonly lua_Debug _activationRecord;
    private LuaDebugInfo _currentInfo;

    internal LuaDebug(lua_State* L)
    {
        this.L = L;
    }

    internal LuaDebug(lua_State* L, lua_Debug* activationRecord)
    {
        this.L = L;
        _activationRecord = *activationRecord;
    }

    private void EnsureInfoRetrieved(LuaDebugInfo neededInfo)
    {
        if (Ar->i_ci == null)
        {
            Throw.InvalidOperationException($"This {nameof(LuaDebug)} instance has not been initialized.");
        }

        if (!_currentInfo.HasFlag(neededInfo))
        {
            GetInfo(GetStringForInfo(neededInfo));
            _currentInfo |= neededInfo;
        }
    }

    /// <summary>
    ///     Gets the function running at the current level.
    /// </summary>
    /// <returns></returns>
    public readonly LuaFunction GetRunningFunction()
    {
        GetInfo("f"u8);

        using (var thread = LuaThread.FromExtraSpace(L))
        {
            try
            {
                return thread.Stack[-1].GetValue<LuaFunction>()!;
            }
            finally
            {
                thread.Stack.Pop();
            }
        }
    }

    /// <summary>
    ///     Gets a table whose indices are the lines on the function with some associated code, that is, the lines where you can put a break point.
    ///     Lines with no code include empty lines and comments.
    /// </summary>
    /// <returns></returns>
    public readonly LuaTable GetLinesTable()
    {
        GetInfo("L"u8);

        using (var thread = LuaThread.FromExtraSpace(L))
        {
            try
            {
                return thread.Stack[-1].GetValue<LuaTable>()!;
            }
            finally
            {
                thread.Stack.Pop();
            }
        }
    }

    private readonly void GetInfo(ReadOnlySpan<byte> what)
    {
        lua_getinfo(L, what, Ar);
    }

    private static ReadOnlySpan<byte> GetStringForInfo(LuaDebugInfo what)
    {
        return what switch
        {
            LuaDebugInfo.CurrentLine => "l"u8,
            LuaDebugInfo.Name => "n"u8,
            LuaDebugInfo.Transfer => "r"u8,
            LuaDebugInfo.Source => "S"u8,
            LuaDebugInfo.TailCall => "t"u8,
            LuaDebugInfo.Upvalues => "u"u8,
            _ => throw new ArgumentOutOfRangeException(nameof(what), what, null)
        };
    }

    [Flags]
    internal enum LuaDebugInfo
    {
        None = 0,
        CurrentLine = 1 << 0,
        Name = 1 << 1,
        Transfer = 1 << 2,
        Source = 1 << 3,
        TailCall = 1 << 4,
        Upvalues = 1 << 5
    }
}
