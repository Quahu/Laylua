using System;

namespace Laylua;

/// <summary>
///     Represents errors that occur when executing Lua operations not through a <i>protected call</i> (see <a href="https://www.lua.org/manual/5.4/manual.html#2.3">Lua manual</a>).
/// </summary>
/// <remarks>
///     <inheritdoc/>
///     <para/>
///     For any Lua operation that raises an error, but is not executed within a <i>protected call</i>,
///     Laylua will attempt to throw <see cref="LuaPanicException"/> to avoid aborting the process.
/// </remarks>
public sealed class LuaPanicException : LuaException
{
    internal LuaPanicException(string? message, Exception? innerException)
        : base(message, innerException)
    { }
}
