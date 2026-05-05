namespace Laylua.Marshaling;

/// <summary>
///     Controls how awaitable CLR method and delegate results are returned to Lua.
/// </summary>
public enum LuaAwaitableReturnHandling
{
    /// <summary>
    ///     Awaitable results are yielded implicitly during async-capable Lua execution.
    /// </summary>
    ImplicitAwait = 1,

    /// <summary>
    ///     Awaitable results are returned to Lua as ordinary awaitable userdata and must be awaited explicitly.
    /// </summary>
    ReturnAwaitable = 2
}
