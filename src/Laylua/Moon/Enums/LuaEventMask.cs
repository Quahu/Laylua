using System;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook event mask.
/// </summary>
/// <remarks>
///     Used for specifying what events the hook should trigger on.
/// </remarks>
[Flags]
public enum LuaEventMask
{
    /// <summary>
    ///     The hook triggers for no events.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The hook triggers for <see cref="LuaEvent.Call"/>.
    /// </summary>
    Call = 1 << LuaEvent.Call,

    /// <summary>
    ///     The hook triggers for <see cref="LuaEvent.Return"/>.
    /// </summary>
    Return = 1 << LuaEvent.Return,

    /// <summary>
    ///     The hook triggers for <see cref="LuaEvent.Line"/>.
    /// </summary>
    Line = 1 << LuaEvent.Line,

    /// <summary>
    ///     The hook triggers for <see cref="LuaEvent.Count"/>.
    /// </summary>
    Count = 1 << LuaEvent.Count,
}