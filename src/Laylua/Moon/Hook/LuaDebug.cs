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

    public LuaString FunctionName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Name);
            return new(ActivationRecord->name);
        }
    }

    public LuaString FunctionTypeName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Name);
            return new(ActivationRecord->namewhat);
        }
    }

    public LuaString ChunkName
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return new(ActivationRecord->what);
        }
    }

    public LuaString Source
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return new(ActivationRecord->source, ActivationRecord->srclen);
        }
    }

    public int CurrentLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.CurrentLine);
            return ActivationRecord->currentline;
        }
    }

    public int FunctionDefinitionStartLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return ActivationRecord->linedefined;
        }
    }

    public int FunctionDefinitionEndLineNumber
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Source);
            return ActivationRecord->lastlinedefined;
        }
    }

    public int FunctionUpvalueCount
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return ActivationRecord->nups;
        }
    }

    public int FunctionParameterCount
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return ActivationRecord->nparams;
        }
    }

    public bool IsFunctionVariadic
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.Upvalues);
            return ActivationRecord->isvararg;
        }
    }

    public bool IsTailCall
    {
        get
        {
            EnsureInfoRetrieved(LuaDebugInfo.TailCall);
            return ActivationRecord->istailcall;
        }
    }

    // TODO: locals

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
        lua_getinfo(_thread.State.L, "f", ActivationRecord);
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
        lua_getinfo(_thread.State.L, "L", ActivationRecord);
        try
        {
            return _thread.Stack[-1].GetValue<LuaTable>()!;
        }
        finally
        {
            _thread.Stack.Pop();
        }
    }

    private static string GetStringForInfo(LuaDebugInfo what)
    {
        return what switch
        {
            LuaDebugInfo.CurrentLine => "l",
            LuaDebugInfo.Name => "n",
            LuaDebugInfo.Transfer => "r",
            LuaDebugInfo.Source => "S",
            LuaDebugInfo.TailCall => "t",
            LuaDebugInfo.Upvalues => "u",
            _ => Throw.ArgumentOutOfRangeException<string>(nameof(what))
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
