using System;

namespace Laylua;

/// <summary>
///     Represents event data for <see cref="Lua.WarningEmitted"/>.
/// </summary>
public readonly struct LuaWarningEmittedEventArgs
{
    /// <summary>
    ///     Gets the warning message Lua emitted.
    /// </summary>
    public ReadOnlyMemory<char> Message { get; }

    internal LuaWarningEmittedEventArgs(ReadOnlyMemory<char> message)
    {
        Message = message;
    }
}
