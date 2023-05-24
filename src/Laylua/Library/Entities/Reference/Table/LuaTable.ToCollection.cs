using System;
using System.Collections.Generic;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

public unsafe partial class LuaTable
{
    /// <summary>
    ///     Converts this table to an array.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="ToList"/>
    /// </remarks>
    /// <param name="throwOnNonIntegerKeys"> Whether to throw on non-integer keys. </param>
    /// <returns>
    ///     The output array.
    /// </returns>
    public object[] ToArray(bool throwOnNonIntegerKeys = false)
    {
        var list = ToList(throwOnNonIntegerKeys);
        return list.ToArray();
    }

    /// <summary>
    ///     Converts this table to a list.
    /// </summary>
    /// <remarks>
    ///     This method skips non-integer keys
    ///     or throws if <paramref name="throwOnNonIntegerKeys"/> is <see langword="true"/>.
    /// </remarks>
    /// <param name="throwOnNonIntegerKeys"> Whether to throw on non-integer keys. </param>
    /// <returns>
    ///     The output list.
    /// </returns>
    public List<object> ToList(bool throwOnNonIntegerKeys = false)
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            var length = (int) luaL_len(L, -1);
            var list = new List<object>(length);
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (lua_type(L, -2) != LuaType.Number)
                {
                    if (throwOnNonIntegerKeys)
                    {
                        throw new InvalidOperationException("Cannot convert a table containing non-integer keys.");
                    }

                    lua_pop(L);
                    continue;
                }

                var value = Lua.Marshaler.PopObject();
                Debug.Assert(value != null);
                list.Add(value);
            }

            return list;
        }
    }

    /// <summary>
    ///     Converts this table to a dictionary.
    /// </summary>
    /// <returns>
    ///     The output dictionary.
    /// </returns>
    public Dictionary<object, object> ToDictionary()
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<object, object>(Lua.Comparer);
        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                var key = Lua.Marshaler.ToObject(-2);
                var value = Lua.Marshaler.ToObject(-1);
                Debug.Assert(key != null);
                Debug.Assert(value != null);
                dictionary[key] = value;
                lua_pop(L);
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     Converts this table to a dictionary.
    /// </summary>
    /// <remarks>
    ///     This method skips non-string keys
    ///     or throws if <paramref name="throwOnNonStringKeys"/> is <see langword="true"/>.
    /// </remarks>
    /// <param name="throwOnNonStringKeys"> Whether to throw on non-string keys. </param>
    /// <returns>
    ///     The output dictionary.
    /// </returns>
    public Dictionary<string, object> ToRecordDictionary(bool throwOnNonStringKeys = false)
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<string, object>(Lua.Comparer);
        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (lua_type(L, -2) != LuaType.String)
                {
                    if (throwOnNonStringKeys)
                    {
                        throw new InvalidOperationException("Cannot convert a table containing non-string keys.");
                    }

                    lua_pop(L);
                    continue;
                }

                var key = Lua.Marshaler.ToObject<string>(-2);
                var value = Lua.Marshaler.ToObject(-1);
                Debug.Assert(key != null);
                Debug.Assert(value != null);
                dictionary[key] = value;
                lua_pop(L);
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     Converts this table to a dictionary.
    /// </summary>
    /// <remarks>
    ///     This method skips non-string keys
    ///     or throws if <paramref name="throwOnNonStringKeys"/> is <see langword="true"/>.
    /// </remarks>
    /// <typeparam name="TValue"> The type to convert the values to. </typeparam>
    /// <param name="throwOnNonStringKeys"> Whether to throw on non-string keys. </param>
    /// <returns>
    ///     The output dictionary.
    /// </returns>
    public Dictionary<string, TValue> ToRecordDictionary<TValue>(bool throwOnNonStringKeys = false)
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<string, TValue>(Lua.Comparer);
        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (lua_type(L, -2) != LuaType.String)
                {
                    if (throwOnNonStringKeys)
                    {
                        throw new InvalidOperationException("Cannot convert a table containing non-string keys.");
                    }

                    lua_pop(L);
                    continue;
                }

                var key = Lua.Marshaler.ToObject<string>(-2);
                var value = Lua.Marshaler.ToObject<TValue>(-1);
                Debug.Assert(key != null);
                Debug.Assert(value != null);
                dictionary[key] = value;
                lua_pop(L);
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     Converts this table to a dictionary.
    /// </summary>
    /// <remarks>
    ///     This method throws for key/value pairs when either of the two cannot be converted
    ///     to <typeparamref name="TKey"/>/<typeparamref name="TValue"/> respectively
    ///     or skips them if <paramref name="throwOnNonConvertible"/> is <see langword="false"/>.
    /// </remarks>
    /// <typeparam name="TKey"> The type to convert the keys to. </typeparam>
    /// <typeparam name="TValue"> The type to convert the values to. </typeparam>
    /// <param name="throwOnNonConvertible"> Whether to throw on non-convertible key/value pairs. </param>
    /// <returns>
    ///     The output dictionary.
    /// </returns>
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(bool throwOnNonConvertible = true)
        where TKey : notnull
        where TValue : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<TKey, TValue>();

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (!Lua.Marshaler.TryToObject<TKey>(-2, out var key))
                {
                    if (throwOnNonConvertible)
                    {
                        throw new InvalidOperationException($"Failed to convert the key {Lua.Stack[-2]} to type {typeof(TKey)}.");
                    }
                }
                else if (!Lua.Marshaler.TryToObject<TValue>(-1, out var value))
                {
                    if (throwOnNonConvertible)
                    {
                        throw new InvalidOperationException($"Failed to convert the value {Lua.Stack[-1]} to type {typeof(TValue)}..");
                    }
                }
                else
                {
                    Debug.Assert(key != null);
                    Debug.Assert(value != null);
                    dictionary[key] = value;
                }

                lua_pop(L);
            }

            return dictionary;
        }
    }
}
