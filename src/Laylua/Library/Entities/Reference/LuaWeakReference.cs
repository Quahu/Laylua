using System;
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
    private readonly LuaTable? _key;

    internal LuaWeakReference(LuaThread thread, LuaTable key)
    {
        _thread = thread;
        _key = key;
    }

    /// <summary>
    ///     Checks whether this weak reference is alive and attempts to retrieve the weakly referenced <see cref="LuaReference"/>.
    /// </summary>
    /// <returns>
    ///     The weakly referenced <see cref="LuaReference"/> or <see langword="null"/> if the reference is not alive.
    /// </returns>
    public TReference? GetValue()
    {
        if (_thread == null || _key == null || !_thread.Stack.TryEnsureFreeCapacity(2))
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

            _thread.Stack.Push(_key);
            var type = lua_rawget(L, -2);
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

    [MemberNotNull(nameof(_thread), nameof(_key))]
    private void ThrowIfInvalid()
    {
        if (_thread == null || _key == null)
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
        if (!TryCreate(thread, stackIndex, out var key))
        {
            weakReference = default;
            return false;
        }

        weakReference = new LuaWeakReference<TReference>(thread, key);
        return true;
    }

    private static unsafe bool TryCreate(LuaThread thread, int stackIndex, [MaybeNullWhen(false)] out LuaTable key)
    {
        var L = thread.State.L;
        if (!lua_checkstack(L, 5))
        {
            key = null;
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
                lua_pushstring(L, "kv"u8);
                lua_rawset(L, -3);

                lua_setmetatable(L, -2);
            }

            lua_createtable(L, 0, 0);
            var keyIndex = lua_gettop(L);
            if (!thread.Stack[keyIndex].TryGetValue(out LuaTable? createdKey) || createdKey == null)
            {
                key = null;
                return false;
            }

            key = createdKey;
            lua_pushvalue(L, keyIndex);
            lua_pushvalue(L, stackIndex);
            lua_rawset(L, -4);
            return true;
        }
        finally
        {
            lua_settop(L, top);
        }
    }
}
