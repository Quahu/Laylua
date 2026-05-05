namespace Laylua;

/// <summary>
///     Configures how Laylua awaits CLR awaitables during async Lua execution.
/// </summary>
/// <remarks>
///     These options apply to <see cref="LuaThread.ExecuteAsync(string, string?, LuaAsyncOptions, System.Threading.CancellationToken)"/>,
///     <see cref="LuaThread.EvaluateAsync(string, string?, LuaAsyncOptions, System.Threading.CancellationToken)"/>,
///     and <see cref="LuaFunction.CallAsync(LuaAsyncOptions, System.Threading.CancellationToken)"/>.
///     They control the host-side await behavior used while resuming Lua coroutines.
/// </remarks>
public readonly struct LuaAsyncOptions
{
    /// <summary>
    ///     Gets whether the caller synchronization context should be captured and continuations should run on it.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/>.
    /// </remarks>
    public bool ContinueOnCapturedContext { get; init; }
}
