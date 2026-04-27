using System;

namespace Laylua;

/// <summary>
///     Internal sentinel for Lua errors inside pcall.
///     Preserves the caller's error message through the catch -> lua_error path.
/// </summary>
/// <seealso cref="LuaThread.RaiseError(string)"/>
internal sealed class LuaRaisedException(string? luaMessage, Exception? luaInnerException)
    : Exception(luaMessage, luaInnerException)
{
    public string? LuaMessage { get; } = luaMessage;

    public Exception? LuaInnerException { get; } = luaInnerException;
}
