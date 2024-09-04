using System;

namespace Laylua;

/// <summary>
///     Represents event data for <see cref="Lua.Warning"/>.
/// </summary>
public readonly struct LuaWarningEventArgs
{
    /// <summary>
    ///     Gets the warning message Lua emitted.
    /// </summary>
    public ReadOnlyMemory<char> Message { get; }

    internal LuaWarningEventArgs(ReadOnlyMemory<char> message)
    {
        Message = message;
    }
}
