using System;

namespace Laylua.Moon;

/// <summary>
///     Thrown instead of allowing Lua's panic handler to abort the application.
/// </summary>
public class LuaPanicException : LuaException
{
    internal LuaPanicException(string? message)
        : base(message)
    { }

    internal LuaPanicException(string? message, Exception? innerException)
        : base(message, innerException)
    { }
}
