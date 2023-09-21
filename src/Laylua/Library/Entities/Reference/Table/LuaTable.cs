using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua table.
/// </summary>
/// <remarks>
///     <inheritdoc cref="LuaReference"/>
/// </remarks>
public unsafe partial class LuaTable : LuaReference
{
    /// <summary>
    ///     Gets a view over the keys of this table.
    /// </summary>
    public KeyCollection Keys => new(this);

    /// <summary>
    ///     Gets a view over the values of this table.
    /// </summary>
    public ValueCollection Values => new(this);

    /// <summary>
    ///     Gets a view over the sequence part of this table.
    /// </summary>
    public SequenceCollection Sequence => new(this);

    /// <summary>
    ///     Gets whether this table is empty.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            ThrowIfInvalid();

            using (Lua.Stack.SnapshotCount())
            {
                PushValue(this);
                var L = Lua.GetStatePointer();
                lua_pushnil(L);
                return !lua_next(L, -2);
            }
        }
    }

    /// <summary>
    ///     Gets the length of this table.
    /// </summary>
    /// <remarks>
    ///     <b>If you want to get the amount of key/value pairs in the table,
    ///     use <see cref="Count"/> instead, as it works for all keys.</b>
    ///     <para/>
    ///     If the keys of the table are not consecutive integers
    ///     or if there are holes in the sequence,
    ///     this may not return the expected result.
    ///     <br/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#3.4.7">Lua manual</a>.
    /// </remarks>
    public lua_Integer Length
    {
        get
        {
            ThrowIfInvalid();

            Lua.Stack.EnsureFreeCapacity(2);

            var L = Lua.GetStatePointer();
            using (Lua.Stack.SnapshotCount())
            {
                PushValue(this);
                return luaL_len(L, -1);
            }
        }
    }

    /// <inheritdoc cref="LuaReference._reference"/>
    /// <summary>
    ///     Gets or sets a value with the given key in the table.
    /// </summary>
    /// <remarks>
    ///     For type safety (especially so you can dispose any <see cref="LuaReference"/>s returned)
    ///     and for performance reasons you should prefer the generic methods, i.e.
    ///     <see cref="TryGetValue{TKey,TValue}"/> and <see cref="SetValue{TKey,TValue}"/>.
    /// </remarks>
    /// <param name="key"> The key of the value to get or set. </param>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when the table does not contain a value with the specified key.
    /// </exception>
    public object this[object key]
    {
        get => GetValue<object, object>(key);
        set => SetValue(key, value);
    }

    internal LuaTable()
    { }

    internal static LuaTable CreateGlobalsTable(Lua lua)
    {
        var table = new LuaTable();
        table.Lua = lua;
        table.Reference = LuaRegistry.Indices.Globals;

#pragma warning disable CA1816
        GC.SuppressFinalize(table);
#pragma warning restore CA1816

        return table;
    }

    /// <inheritdoc cref="LuaReference.Clone{T}"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual LuaTable Clone()
    {
        return Clone<LuaTable>();
    }

    /// <summary>
    ///     Attempts to get the metatable of this table.
    /// </summary>
    /// <param name="metatable"> The metatable or <see langword="null"/> if one is not set. </param>
    /// <returns>
    ///     <see langword="true"/> if this table has a metatable; <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryGetMetatable([NotNullWhen(true)] out LuaTable? metatable)
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            var hasMetatable = lua_getmetatable(L, -1);
            if (hasMetatable)
            {
                metatable = Lua.Marshaler.GetValue<LuaTable>(-1)!;
                return true;
            }

            metatable = null;
            return false;
        }
    }

    /// <summary>
    ///     Attempts to get the metatable of this table.
    /// </summary>
    /// <returns>
    ///     The metatable of this table.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     This table does not have a metatable set.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LuaTable GetMetatable()
    {
        return TryGetMetatable(out var metatable)
            ? metatable
            : throw new InvalidOperationException("This table does not have a metatable set.");
    }

    /// <summary>
    ///     Sets the metatable of this table.
    /// </summary>
    /// <param name="metatable"> The metatable to set or <see langword="null"/> which indicates no metatable. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetMetatable(LuaTable? metatable)
    {
        ThrowIfInvalid();

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            Lua.Marshaler.PushValue(metatable);
            lua_setmetatable(L, -2);
        }
    }

    /// <summary>
    ///     Enumerates and counts the elements of this table.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Count()
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        var count = 0;
        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                lua_pop(L);
                count++;
            }

            return count;
        }
    }

    /// <summary>
    ///     Checks whether this table contains the specified key.
    /// </summary>
    /// <param name="key"> The key to check. </param>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool ContainsKey<TKey>(TKey key)
        where TKey : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            Lua.Marshaler.PushValue(key);
            var L = Lua.GetStatePointer();
            return lua_gettable(L, -2) > LuaType.Nil;
        }
    }

    /// <summary>
    ///     Checks whether this table contains the specified value.
    /// </summary>
    /// <param name="value"> The value to check. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool ContainsValue<TValue>(TValue value)
        where TValue : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(4);

        using (Lua.Stack.SnapshotCount())
        {
            Lua.Marshaler.PushValue(value);
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (lua_compare(L, -1, -4, LuaComparison.Equal))
                    return true;

                lua_pop(L);
            }
        }

        return false;
    }

    /// <inheritdoc cref="LuaReference._reference"/>
    /// <summary>
    ///     Gets a value with the given key in the table.
    /// </summary>
    /// <param name="key"> The key of the value to get. </param>
    /// <param name="value"> The result out value. </param>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryGetValue<TKey, TValue>(TKey key, [MaybeNullWhen(false)] out TValue value)
        where TKey : notnull
        where TValue : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            Lua.Marshaler.PushValue(key);
            var L = Lua.GetStatePointer();
            if (!lua_gettable(L, -2).IsNoneOrNil() && Lua.Marshaler.TryGetValue(-1, out value))
            {
                Debug.Assert(value != null);
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <inheritdoc cref="LuaReference._reference"/>
    /// <summary>
    ///     Gets a value with the given key in the table.
    /// </summary>
    /// <param name="key"> The key of the value to get. </param>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when the table does not contain a value with the specified key.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TValue GetValue<TKey, TValue>(TKey key)
        where TKey : notnull
        where TValue : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            Lua.Marshaler.PushValue(key);
            var L = Lua.GetStatePointer();
            if (lua_gettable(L, -2).IsNoneOrNil())
            {
                Throw.KeyNotFoundException();
            }

            return Lua.Marshaler.GetValue<TValue>(-1)!;
        }
    }

    /// <summary>
    ///     Sets a value with the given key in the table.
    /// </summary>
    /// <param name="key"> The key of the value to set. </param>
    /// <param name="value"> The value to set for the key. </param>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetValue<TKey, TValue>(TKey key, TValue? value)
        where TKey : notnull
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(3);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            Lua.Marshaler.PushValue(key);
            Lua.Marshaler.PushValue(value);
            var L = Lua.GetStatePointer();
            lua_settable(L, -3);
        }
    }

    /// <summary>
    ///     Clears this table by setting all values to nil.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Clear()
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(4);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                lua_pop(L);
                lua_pushvalue(L, -1);
                lua_pushnil(L);
                lua_settable(L, -4);
            }
        }
    }

    /// <summary>
    ///     Copies the elements of this table to the target table.
    /// </summary>
    /// <param name="table"> The table to copy the contents to. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CopyTo(LuaTable table)
    {
        Guard.IsNotNull(table);
        Guard.IsNotEqualTo<LuaReference>(table, this);

        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(5);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(table);
            PushValue(this);

            var L = Lua.GetStatePointer();
            if (lua_rawequal(L, -2, -1))
            {
                Throw.ArgumentException("The target table must be a different table.", nameof(table));
            }

            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                lua_pushvalue(L, -2);
                lua_insert(L, -2);

                lua_settable(L, -5);
            }
        }
    }

    /// <summary>
    ///     Gets a dictionary containing the key/value pairs of this table.
    /// </summary>
    /// <remarks>
    ///     This method skips non-string keys
    ///     or throws if <paramref name="throwOnNonStringKeys"/> is <see langword="true"/>.
    ///     <para/>
    ///     This method throws for values that cannot be converted
    ///     or skips them if <paramref name="throwOnNonConvertibleValues"/> is <see langword="false"/>.
    /// </remarks>
    /// <typeparam name="TValue"> The type to convert the values to. </typeparam>
    /// <param name="throwOnNonStringKeys"> Whether to throw on non-string keys. </param>
    /// <param name="throwOnNonConvertibleValues"> Whether to throw on non-convertible values. </param>
    /// <returns>
    ///     The output dictionary.
    /// </returns>
    public Dictionary<string, TValue> ToRecordDictionary<TValue>(bool throwOnNonStringKeys = false, bool throwOnNonConvertibleValues = true)
        where TValue : notnull
    {
        ThrowIfInvalid();

        var lua = Lua;
        lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<string, TValue>(lua.Comparer);
        using (lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = lua.GetStatePointer();
            var marshaler = lua.Marshaler;
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (lua_type(L, -2) != LuaType.String)
                {
                    if (throwOnNonStringKeys)
                    {
                        Throw.InvalidOperationException("Cannot convert a table containing non-string keys.");
                    }
                }
                else
                {
                    if (!marshaler.TryGetValue<TValue>(-1, out var value))
                    {
                        if (throwOnNonConvertibleValues)
                        {
                            Throw.InvalidOperationException($"Failed to convert the value {lua.Stack[-1]} to type {typeof(TValue)}.");
                        }
                    }
                    else
                    {
                        var key = marshaler.GetValue<string>(-2);
                        Debug.Assert(key != null);
                        Debug.Assert(value != null);
                        dictionary[key] = value;
                    }
                }

                lua_pop(L);
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     Gets a dictionary containing the key/value pairs of this table.
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

        var lua = Lua;
        lua.Stack.EnsureFreeCapacity(3);

        var dictionary = new Dictionary<TKey, TValue>();

        using (lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = lua.GetStatePointer();
            var marshaler = lua.Marshaler;
            lua_pushnil(L);
            while (lua_next(L, -2))
            {
                if (!marshaler.TryGetValue<TKey>(-2, out var key))
                {
                    if (throwOnNonConvertible)
                    {
                        Throw.InvalidOperationException($"Failed to convert the key {lua.Stack[-2]} to type {typeof(TKey)}.");
                    }
                }
                else if (!marshaler.TryGetValue<TValue>(-1, out var value))
                {
                    if (throwOnNonConvertible)
                    {
                        Throw.InvalidOperationException($"Failed to convert the value {lua.Stack[-1]} to type {typeof(TValue)}.");
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

    /// <summary>
    ///     Gets an enumerable lazily yielding the key/value pairs of this table.
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
    public IEnumerable<KeyValuePair<TKey, TValue>> ToEnumerable<TKey, TValue>(bool throwOnNonConvertible = true)
        where TKey : notnull
        where TValue : notnull
    {
        foreach (var (keyStackValue, valueStackValue) in this)
        {
            if (!keyStackValue.TryGetValue<TKey>(out var key))
            {
                if (throwOnNonConvertible)
                {
                    Throw.InvalidOperationException($"Failed to convert the key {keyStackValue} to type {typeof(TKey)}.");
                }
            }
            else if (!valueStackValue.TryGetValue<TValue>(out var value))
            {
                if (throwOnNonConvertible)
                {
                    Throw.InvalidOperationException($"Failed to convert the value {valueStackValue} to type {typeof(TValue)}.");
                }
            }
            else
            {
                yield return new KeyValuePair<TKey, TValue>(key!, value!);
            }
        }
    }

    /// <summary>
    ///     Returns an enumerator that lazily produces key/value pairs from this table.
    /// </summary>
    /// <remarks>
    ///     The enumerator uses the Lua stack
    ///     which you should keep in mind when pushing your own data onto the stack
    ///     during enumeration.
    /// </remarks>
    /// <returns>
    ///     An enumerator wrapping this table.
    /// </returns>
    public Enumerator GetEnumerator()
    {
        ThrowIfInvalid();

        return new(this);
    }

    /// <summary>
    ///     Represents an enumerator that can be used to enumerate the <see cref="LuaTable"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<LuaStackValue, LuaStackValue>>
    {
        /// <inheritdoc/>
        public readonly KeyValuePair<LuaStackValue, LuaStackValue> Current
        {
            get
            {
                if (_moveTop == 0)
                    return default;

                var key = _table.Lua.Stack[-2];
                var value = _table.Lua.Stack[-1];
                return new(key, value);
            }
        }

        object IEnumerator.Current => Current;

        private readonly LuaTable _table;
        private readonly int _initialTop;
        private int _moveTop;

        internal Enumerator(LuaTable table)
        {
            table.ThrowIfInvalid();

            _table = table;
            var L = _table.Lua.GetStatePointer();
            _initialTop = lua_gettop(L);
            _moveTop = 0;

            table.Lua.Stack.EnsureFreeCapacity(2);

            try
            {
                PushValue(_table);
                lua_pushnil(L);
            }
            catch
            {
                lua_settop(L, _initialTop);
                throw;
            }
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            var L = _table.Lua.GetStatePointer();
            if (_moveTop != 0)
            {
                lua_settop(L, _moveTop);
                _moveTop = 0;
            }

            if (!lua_next(L, -2))
                return false;

            _moveTop = _initialTop + 2;
            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            Dispose();
            this = new Enumerator(_table);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _moveTop = 0;
            var L = _table.Lua.GetStatePointer();
            lua_settop(L, _initialTop);
        }
    }
}
