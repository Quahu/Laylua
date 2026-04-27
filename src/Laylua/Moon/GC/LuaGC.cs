using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents a type that controls the Lua garbage collector.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#2.5">Lua manual</a> for more information about garbage collection. </para>
/// </summary>
public readonly unsafe struct LuaGC
{
    private const string GCCallbackStateMetatableName = $"__Laylua:{nameof(GcCallbackState)}";

    private sealed class GcCallbackState
    {
        public readonly List<Action> Callbacks = [];
    }

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
    /// <param name="L"> The Lua state. </param>
    /// <param name="reference"> The object to register the GC callback for. </param>
    /// <param name="callback"> The callback to register. </param>
    private static void RegisterCallbackCore(lua_State* L, LuaReference reference, Action callback)
    {
        var lua = Lua.FromExtraSpace(L);
        lua.Stack.EnsureFreeCapacity(4);

        using (lua.Stack.SnapshotCount())
        {
            EnsureCallbacksTable(L);
            lua.Stack.Push(reference);
            if (lua_rawget(L, -2).IsNoneOrNil())
            {
                lua_pop(L);

                var callbackState = PushCallbackState(L);

                lua.Stack.Push(reference);
                lua_pushvalue(L, -2);
                lua_rawset(L, -4);

                callbackState.Callbacks.Add(callback);
                return;
            }

            var existingCallbackState = GetCallbackState(L, -1);
            existingCallbackState.Callbacks.Add(callback);
        }
    }

    internal const string GCCallbacksTableName = "__laylua__internal_gccallbacks";

    private static readonly LuaCFunction _gcCallback = static L =>
    {
        var statePointer = (nint*) lua_touserdata(L, 1);
        if (statePointer == null)
        {
            return 0;
        }

        var handlePointer = *statePointer;
        if (handlePointer == 0)
        {
            return 0;
        }

        *statePointer = 0;

        var handle = GCHandle.FromIntPtr(handlePointer);
        try
        {
            if (handle.Target is not GcCallbackState callbackState)
            {
                return 0;
            }

            var laylua = LayluaState.FromExtraSpace(L);
            using (laylua.TemporarilyPopPanic(onlyIfNotPCall: true))
            using (var thread = LuaThread.FromExtraSpace(L))
            {
                foreach (var callback in callbackState.Callbacks)
                {
                    try
                    {
                        callback();
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
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        return 0;
    };

    private static void EnsureCallbacksTable(lua_State* L)
    {
        if (!luaL_getsubtable(L, LuaRegistry.Index, GCCallbacksTableName))
        {
            lua_createtable(L, 0, 1);

            lua_pushstring(L, LuaMetatableKeysUtf8.__mode);
            lua_pushstring(L, "k"u8);
            lua_rawset(L, -3);

            lua_setmetatable(L, -2);
        }
    }

    private static GcCallbackState PushCallbackState(lua_State* L)
    {
        var statePointer = (nint*) lua_newuserdatauv(L, (nuint) sizeof(nint), 0);
        *statePointer = 0;

        GCHandle handle = default;
        var hasMetatableOnStack = false;
        try
        {
            var callbackState = new GcCallbackState();
            handle = GCHandle.Alloc(callbackState);
            *statePointer = GCHandle.ToIntPtr(handle);

            var createdMetatable = luaL_newmetatable(L, GCCallbackStateMetatableName);
            hasMetatableOnStack = true;
            if (createdMetatable)
            {
                try
                {
                    lua_pushcfunction(L, _gcCallback);
                    lua_setfield(L, -2, "__gc");

                    lua_pushboolean(L, false);
                    lua_setfield(L, -2, "__metatable");
                }
                catch
                {
                    lua_pushnil(L);
                    lua_setfield(L, LuaRegistry.Index, GCCallbackStateMetatableName);
                    throw;
                }
            }

            lua_setmetatable(L, -2);
            hasMetatableOnStack = false;
            return callbackState;
        }
        catch
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            *statePointer = 0;
            lua_pop(L, hasMetatableOnStack ? 2 : 1);
            throw;
        }
    }

    private static GcCallbackState GetCallbackState(lua_State* L, int stackIndex)
    {
        var statePointer = (nint*) lua_touserdata(L, stackIndex);
        if (statePointer == null || *statePointer == 0)
        {
            throw new InvalidOperationException("Invalid GC callback state.");
        }

        var handle = GCHandle.FromIntPtr(*statePointer);
        if (!handle.IsAllocated || handle.Target is not GcCallbackState callbackState)
        {
            throw new InvalidOperationException("Failed to retrieve the GC callback state.");
        }

        return callbackState;
    }
}
