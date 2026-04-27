namespace Laylua.Marshaling;

/// <summary>
///     Controls what happens when Lua accesses or writes an unknown property key on userdata.
/// </summary>
public enum LuaUnknownPropertyBehavior
{
    /// <summary>
    ///     Silently ignore unknown property access.
    /// </summary>
    /// <remarks>
    ///     <c>__index</c> returns <c>nil</c> and <c>__newindex</c> ignores the write.
    /// </remarks>
    Silent = 1,

    /// <summary>
    ///     Raise errors when accessing unknown property names.
    /// </summary>
    /// <remarks>
    ///     <c>__index</c> and <c>__newindex</c> raise a Lua error for unknown keys.
    /// </remarks>
    Error = 2,
}
