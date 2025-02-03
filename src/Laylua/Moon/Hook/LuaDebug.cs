using System;
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
    ///     Gets the event that triggered the hook.
    /// </summary>
    public readonly LuaEvent Event => ActivationRecord->@event;

    /// <summary>
    ///     Gets a reasonable name for the given function.
    /// </summary>
    public LuaString FunctionName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Name);
            return new(ActivationRecord->name);
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
            return new(ActivationRecord->namewhat);
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
            return new(ActivationRecord->what);
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
            return new(ActivationRecord->source, ActivationRecord->srclen);
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
            return ActivationRecord->currentline;
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
            return ActivationRecord->linedefined;
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
            return ActivationRecord->lastlinedefined;
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
            return ActivationRecord->nups;
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
            return ActivationRecord->nparams;
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
            return ActivationRecord->isvararg;
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
            return ActivationRecord->istailcall;
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
            return ActivationRecord->ftransfer;
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
            return ActivationRecord->ntransfer;
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
            return new(ActivationRecord->short_src, LUA_IDSIZE);
        }
    }

    internal readonly lua_Debug* ActivationRecord;

    private readonly LuaThread _thread;
    private LuaDebugInfo _currentInfo;

    internal LuaDebug(LuaThread thread, lua_Debug* activationRecord)
    {
        _thread = thread;
        ActivationRecord = activationRecord;
    }

    private void EnsureInfoRetrieved(LuaDebugInfo neededInfo)
    {
        if (!_currentInfo.HasFlag(neededInfo))
        {
            lua_getinfo(_thread.State.L, GetStringForInfo(neededInfo), ActivationRecord);
            _currentInfo |= neededInfo;
        }
    }

    /// <summary>
    ///     Gets the function running at the current level.
    /// </summary>
    /// <returns></returns>
    public readonly LuaFunction GetRunningFunction()
    {
        lua_getinfo(_thread.State.L, "f"u8, ActivationRecord);
        try
        {
            return _thread.Stack[-1].GetValue<LuaFunction>()!;
        }
        finally
        {
            _thread.Stack.Pop();
        }
    }

    /// <summary>
    ///     Gets a table whose indices are the lines on the function with some associated code, that is, the lines where you can put a break point.
    ///     Lines with no code include empty lines and comments.
    /// </summary>
    /// <returns></returns>
    public readonly LuaTable GetLinesTable()
    {
        lua_getinfo(_thread.State.L, "L"u8, ActivationRecord);
        try
        {
            return _thread.Stack[-1].GetValue<LuaTable>()!;
        }
        finally
        {
            _thread.Stack.Pop();
        }
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
