namespace Laylua.Moon;

/// <summary>
///     Thrown instead of allowing Lua's panic handler to abort the application.
/// </summary>
public class LuaPanicException : LuaException
{
    public LuaPanicException(Lua lua, string? message)
        : base(lua, message)
    { }
}
