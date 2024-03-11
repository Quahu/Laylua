using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents a type that controls the Lua garbage collector.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#2.5">Lua manual</a> for more information about garbage collection. </para>
/// </summary>
public readonly unsafe struct LuaGC
{
    /// <summary>
    ///     Gets whether the garbage collector is running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            ThrowIfInvalid();
            return lua_gc(_state.L, LuaGCOperation.IsRunning) == 1;
        }
    }

    /// <summary>
    ///     Gets the amount of bytes currently in use by Lua.
    /// </summary>
    public long AllocatedBytes
    {
        get
        {
            ThrowIfInvalid();
            return lua_gc(_state.L, LuaGCOperation.Count) * 1024l + lua_gc(_state.L, LuaGCOperation.CountRemainder);
        }
    }

    private readonly LuaState _state;

    internal LuaGC(LuaState state)
    {
        _state = state;
    }

    private void ThrowIfInvalid()
    {
        if (_state == null)
        {
            Throw.InvalidOperationException($"This {typeof(LuaGC)} instance is not initialized.");
        }

        if (_state.IsDisposed)
        {
            Throw.ObjectDisposedException(GetType().FullName!, "The Lua state has been disposed.");
        }
    }

    /// <summary>
    ///     Performs a full garbage collection cycle.
    /// </summary>
    public void Collect()
    {
        ThrowIfInvalid();
        _ = lua_gc(_state.L, LuaGCOperation.Collect);
    }

    /// <summary>
    ///     Stops the garbage collector.
    /// </summary>
    public void Stop()
    {
        ThrowIfInvalid();
        _ = lua_gc(_state.L, LuaGCOperation.Stop);
    }

    /// <summary>
    ///     Restarts the garbage collector.
    /// </summary>
    public void Restart()
    {
        ThrowIfInvalid();
        _ = lua_gc(_state.L, LuaGCOperation.Restart);
    }

    /// <summary>
    ///     Performs an incremental step of garbage collection.
    /// </summary>
    /// <param name="stepSize"> The corresponding step size in kibibytes. </param>
    public void Step(int stepSize)
    {
        ThrowIfInvalid();
        _ = lua_gc(_state.L, LuaGCOperation.Step, stepSize);
    }

    /// <summary>
    ///     Changes the garbage collector to incremental mode with the given parameters.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#2.5.1">Lua manual</a>. </para>
    /// </summary>
    /// <param name="pause"> Controls how long the collector waits before starting a new cycle. </param>
    /// <param name="stepMultiplier"> Controls the speed of the collector relative to memory allocation. </param>
    /// <param name="stepSize"> Controls the size of each incremental step. </param>
    /// <returns>
    ///     The previous garbage collection mode as <see cref="LuaGCOperation"/>.
    /// </returns>
    public LuaGCOperation SetIncrementalMode(int pause, int stepMultiplier, int stepSize)
    {
        ThrowIfInvalid();
        return (LuaGCOperation) lua_gc(_state.L, LuaGCOperation.Incremental, pause, stepMultiplier, stepSize);
    }

    /// <summary>
    ///     Changes the garbage collector to generational mode with the given parameters.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#2.5.2">Lua manual</a>. </para>
    /// </summary>
    /// <param name="minorMultiplier"> Controls the frequency of minor collections. </param>
    /// <param name="majorMultiplier"> Controls the frequency of major collections. </param>
    /// <returns>
    ///     The previous garbage collection mode as <see cref="LuaGCOperation"/>.
    /// </returns>
    public LuaGCOperation SetGenerationalMode(int minorMultiplier, int majorMultiplier)
    {
        ThrowIfInvalid();
        return (LuaGCOperation) lua_gc(_state.L, LuaGCOperation.Generational, minorMultiplier, majorMultiplier);
    }
}
