using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Laylua.Moon;
using Qommon;

namespace Laylua;

public unsafe partial class LuaTable
{
    /// <summary>
    ///     Represents a view over the sequence part of a <see cref="LuaTable"/>.
    /// </summary>
    /// <remarks>
    ///     • When enumerating over <see cref="SequenceCollection"/>, note the following:
    ///     <inheritdoc cref="GetEnumerator"/>
    /// </remarks>
    public readonly struct SequenceCollection
    {
        private readonly LuaTable _table;

        internal SequenceCollection(LuaTable table)
        {
            _table = table;
        }

        /// <summary>
        ///     Gets an array containing the values of the sequence part of the table.
        /// </summary>
        /// <remarks>
        ///     <inheritdoc cref="ToList{T}"/>
        /// </remarks>
        /// <param name="throwOnNonConvertible"> Whether to throw on non-convertible values. </param>
        /// <returns>
        ///     The output array.
        /// </returns>
        public T[] ToArray<T>(bool throwOnNonConvertible = true)
            where T : notnull
        {
            var list = ToList<T>(throwOnNonConvertible);
            return list.ToArray();
        }

        /// <summary>
        ///     Gets a list containing the values of the sequence part of the table.
        /// </summary>
        /// <remarks>
        ///     This method throws for values that cannot be converted to <typeparamref name="T"/>
        ///     or skips them if <paramref name="throwOnNonConvertible"/> is <see langword="false"/>.
        /// </remarks>
        /// <param name="throwOnNonConvertible"> Whether to throw on non-convertible values. </param>
        /// <returns>
        ///     The output list.
        /// </returns>
        public List<T> ToList<T>(bool throwOnNonConvertible = true)
            where T : notnull
        {
            var lua = _table.Lua;
            lua.Stack.EnsureFreeCapacity(3);

            using (_table.Lua.Stack.SnapshotCount())
            {
                PushValue(_table);
                var L = lua.GetStatePointer();
                var marshaler = lua.Marshaler;
                var length = (int) luaL_len(L, -1);
                var list = new List<T>(Math.Min(length, 256));
                var index = 1;
                while (!lua_geti(L, -1, index++).IsNoneOrNil())
                {
                    if (!marshaler.TryGetValue<T>(-1, out var value))
                    {
                        if (throwOnNonConvertible)
                        {
                            Throw.InvalidOperationException($"Failed to convert the value {lua.Stack[-1]} to type {typeof(T)}.");
                        }
                    }
                    else
                    {
                        Debug.Assert(value != null);
                        list.Add(value);
                    }

                    lua_pop(L);
                }

                return list;
            }
        }

        /// <summary>
        ///     Creates an enumerable lazily yielding the values of the sequence part of the table.
        /// </summary>
        /// <remarks>
        ///     This method throws for values that cannot be converted to <typeparamref name="T"/>
        ///     or skips them if <paramref name="throwOnNonConvertible"/> is <see langword="false"/>.
        ///     <para/>
        ///     <inheritdoc cref="LuaTable.GetEnumerator"/>
        /// </remarks>
        /// <param name="throwOnNonConvertible"> Whether to throw on non-convertible values. </param>
        /// <returns>
        ///     The output enumerable.
        /// </returns>
        public IEnumerable<T> AsEnumerable<T>(bool throwOnNonConvertible = true)
            where T : notnull
        {
            foreach (var (_, stackValue) in this)
            {
                if (!stackValue.TryGetValue<T>(out var value))
                {
                    if (throwOnNonConvertible)
                    {
                        Throw.InvalidOperationException($"Failed to convert the key {stackValue} to type {typeof(T)}.");
                    }
                }

                yield return value!;
            }
        }

        /// <summary>
        ///     Returns an enumerator that enumerates the sequence part of the table.
        /// </summary>
        /// <remarks>
        ///     This basically mimics the behavior of <c>ipairs</c>.<br/>
        ///     See <a href="https://www.lua.org/manual/5.4/manual.html#pdf-ipairs">Lua manual</a>.
        ///     <para/>
        ///     <inheritdoc cref="LuaTable.GetEnumerator"/>
        /// </remarks>
        /// <returns>
        ///     An enumerator wrapping the sequence part of the table.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new(_table);
        }

        /// <summary>
        ///     Represents an enumerator that can be used to enumerate the sequence part of a <see cref="LuaTable"/>.
        /// </summary>
        /// <remarks>
        ///     <inheritdoc cref="LuaTable.GetEnumerator"/>
        /// </remarks>
        public struct Enumerator : IEnumerator<KeyValuePair<lua_Integer, LuaStackValue>>
        {
            /// <inheritdoc/>
            public readonly KeyValuePair<lua_Integer, LuaStackValue> Current
            {
                get
                {
                    if (_moveTop == 0)
                        return default;

                    var value = _table.Lua.Stack[-1];
                    return new(_index, value);
                }
            }

            object IEnumerator.Current => Current;

            private readonly LuaTable _table;
            private readonly int _initialTop;
            private int _moveTop;
            private int _index;

            internal Enumerator(LuaTable table)
            {
                table.ThrowIfInvalid();

                _table = table;
                var L = _table.Lua.GetStatePointer();
                _initialTop = lua_gettop(L);
                _moveTop = 0;
                _index = 0;

                table.Lua.Stack.EnsureFreeCapacity(2);

                try
                {
                    PushValue(_table);
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

                if (_index == -1)
                {
                    return false;
                }

                if (lua_geti(L, _initialTop + 1, ++_index).IsNoneOrNil())
                    return false;

                _moveTop = _initialTop + 1;
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
                _index = 0;
                var L = _table.Lua.GetStatePointer();
                lua_settop(L, _initialTop);
            }
        }
    }
}
