﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represent a weak reference to a Lua object.
/// </summary>
/// <typeparam name="TReference"> The type of the object, specified by a <see cref="LuaReference"/> type. </typeparam>
public readonly unsafe struct LuaWeakReference<TReference>
    where TReference : LuaReference
{
    internal LuaThread Thread
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfInvalid();
            return _thread;
        }
    }

    private readonly LuaThread _thread;
    private readonly void* _pointer;

    internal LuaWeakReference(LuaThread thread, void* pointer)
    {
        _thread = thread;
        _pointer = pointer;
    }

    /// <summary>
    ///     Checks whether this weak reference is alive and attempts to retrieve the weakly referenced <see cref="LuaReference"/>.
    /// </summary>
    /// <returns>
    ///     The weakly referenced <see cref="LuaReference"/> or <see langword="null"/> if the reference is not alive.
    /// </returns>
    public TReference? GetValue()
    {
        if (_thread == null || !_thread.Stack.TryEnsureFreeCapacity(2))
        {
            return default;
        }

        var L = _thread.State.L;
        var top = lua_gettop(L);
        try
        {
            if (lua_getfield(L, LuaRegistry.Index, LuaWeakReference.WeakReferencesTableName) != LuaType.Table)
            {
                return default;
            }

            var type = lua_rawgetp(L, -1, _pointer);
            if (type.IsNoneOrNil())
            {
                return default;
            }

            return Thread.Stack[-1].TryGetValue(out TReference? value) ? value : null;
        }
        catch
        {
            return default;
        }
        finally
        {
            lua_settop(L, top);
        }
    }

    [MemberNotNull(nameof(_thread))]
    private void ThrowIfInvalid()
    {
        if (_thread == null)
        {
            throw new InvalidOperationException($"This '{GetType().ToTypeString()}' has not been initialized.");
        }
    }
}

internal static class LuaWeakReference
{
    internal const string WeakReferencesTableName = "__laylua__internal_weakreferences";

    public static unsafe bool TryCreate<TReference>(LuaThread thread, int stackIndex, out LuaWeakReference<TReference> weakReference)
        where TReference : LuaReference
    {
        if (!TryCreate(thread, stackIndex, out var targetPointer))
        {
            weakReference = default;
            return false;
        }

        weakReference = new LuaWeakReference<TReference>(thread, targetPointer);
        return true;
    }

    private static unsafe bool TryCreate(LuaThread thread, int stackIndex, out void* targetPointer)
    {
        var L = thread.State.L;
        if (!lua_checkstack(L, 4))
        {
            targetPointer = null;
            return false;
        }

        stackIndex = lua_absindex(L, stackIndex);
        var top = lua_gettop(L);
        try
        {
            if (!luaL_getsubtable(L, LuaRegistry.Index, WeakReferencesTableName))
            {
                lua_createtable(L, 0, 1);

                lua_pushstring(L, LuaMetatableKeysUtf8.__mode);
                lua_pushstring(L, "v"u8);
                lua_rawset(L, -3);

                lua_setmetatable(L, -2);
            }

            lua_pushvalue(L, stackIndex);
            targetPointer = lua_topointer(L, -1);
            lua_rawsetp(L, -2, targetPointer);
            return true;
        }
        finally
        {
            lua_settop(L, top);
        }
    }
}
