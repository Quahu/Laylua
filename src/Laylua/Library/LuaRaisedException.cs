using System;

namespace Laylua;

/// <summary>
///     Internal sentinel for RaiseError inside pcall.
///     Preserves the caller's error message through the catch -> lua_error path.
/// </summary>
/// <seealso cref="LuaThread.RaiseError(string)"/>
internal sealed class LuaRaisedException : Exception
{
    public string? LuaMessage { get; }
    public Exception? LuaInnerException { get; }

    public LuaRaisedException(string? luaMessage, Exception? luaInnerException)
        : base(luaMessage, luaInnerException)
    {
        LuaMessage = luaMessage;
        LuaInnerException = luaInnerException;
    }
}
