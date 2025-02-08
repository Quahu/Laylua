using System;
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

    /// <inheritdoc cref="RegisterCallbackCore"/>
    public void RegisterCallback(LuaReference reference, Action callback)
    {
        reference.ThrowIfInvalid();
        RegisterCallbackCore(_state.L, reference, callback);
    }

    /// <summary>
    ///     Registers a callback that will trigger when the object referenced by the given <see cref="LuaReference"/> is garbage collected by Lua.
    ///     The callback is registered and executed from the global state.
    /// </summary>
    /// <param name="reference"> The object to register the GC callback for. </param>
    /// <param name="callback"> The callback to register. </param>
    private static void RegisterCallbackCore(lua_State* L, LuaReference reference, Action callback)
    {
        var lua = Lua.FromExtraSpace(L);
        lua.Stack.EnsureFreeCapacity(5);

        using (lua.Stack.SnapshotCount())
        {
            if (!luaL_getsubtable(L, LuaRegistry.Index, GCCallbacksTableName))
            {
                lua_createtable(L, 0, 1);

                lua_pushstring(L, LuaMetatableKeysUtf8.__mode);
                lua_pushstring(L, "k"u8);
                lua_rawset(L, -3);

                lua_setmetatable(L, -2);
            }

            lua.Stack.Push(reference);
            if (lua_rawget(L, -1).IsNoneOrNil())
            {
                lua_pop(L);

                lua_createtable(L, 1, 0);

                lua_createtable(L, 0, 1);

                lua_pushstring(L, LuaMetatableKeysUtf8.__gc);
                lua_pushvalue(L, -3);
                lua_pushcclosure(L, _gcCallback, 1);

                lua_rawset(L, -3);

                lua_setmetatable(L, -2);

                lua.Stack.Push(reference);
                lua_pushvalue(L, -2);
                lua_rawset(L, -4);
            }

            var index = luaL_len(L, -1) + 1;
            lua_pushinteger(L, index);
            lua.Stack.Push(callback);

            lua_rawset(L, -3);
        }
    }

    internal const string GCCallbacksTableName = "__laylua__internal_gccallbacks";

    private static readonly LuaCFunction _gcCallback = static L =>
    {
        // TODO: how can I tell a delegate happens to be running inside pcall?
        // Maybe it's just __gc, so won't affect user code in most cases.
        // Could check for CIST_FIN in L->ci->callstatus?

        var laylua = LayluaState.FromExtraSpace(L);
        using (laylua.TemporarilyPopPanic(onlyIfNotPCall: true))
        {
            var tableIndex = lua_upvalueindex(1);
            lua_pushvalue(L, tableIndex);

            using (var thread = LuaThread.FromExtraSpace(L))
            using (var table = thread.Stack[-1].GetValue<LuaTable>()!)
            {
                foreach (var value in table.Values)
                {
                    using (var function = value.GetValue<LuaFunction>()!)
                    {
                        try
                        {
                            function.Call().Dispose();
                        }
                        catch (Exception ex)
                        {
                            var unwrapped = ex is LuaException luaException
                                ? luaException.UnwrapException()
                                : ex;

                            thread.EmitWarning($"An exception occurred while executing a GC callback. {unwrapped.GetType()}: {unwrapped.Message}");
                        }
                    }
                }
            }

            return 0;
        }
    };
}
