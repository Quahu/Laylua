namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook event.
/// </summary>
public enum  LuaEvent
{
    /// <summary>
    ///     The hook is called when Lua calls a function.
    /// </summary>
    Call = 0,

    /// <summary>
    ///     The hook is called when Lua returns from a function.
    /// </summary>
    Return = 1,

    /// <summary>
    ///     The hook is called when Lua is about to start the execution of a new line of code.
    /// </summary>
    Line = 2,

    /// <summary>
    ///     The hook is called after Lua has executed a set instruction count.
    /// </summary>
    Count = 3,

    /// <summary>
    ///     The hook is called on a tail call.
    /// </summary>
    TailCall = 4
}
