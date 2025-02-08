using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        /// <summary>
        ///     Gets the length of the sequence.
        ///     This returns the same value as the length operator (<c>#table</c>).
        /// </summary>
        /// <remarks>
        ///     <b>To get the number of key/value pairs in the table,
        ///     use <see cref="Count"/> instead, as it works for all keys.</b>
        ///     <para/>
        ///     <inheritdoc cref="Add{T}"/>
        ///     <br/>
        ///     See <a href="https://www.lua.org/manual/5.4/manual.html#3.4.7">Lua manual</a>.
        /// </remarks>
        public lua_Integer Length
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var lua = _table.Lua;
                var L = lua.State.L;

                lua.Stack.EnsureFreeCapacity(1);

                using (lua.Stack.SnapshotCount())
                {
                    lua.Stack.Push(_table);
                    return luaL_len(L, -1);
                }
            }
        }

        private readonly LuaTable _table;

        internal SequenceCollection(LuaTable table)
        {
            _table = table;
        }

        /// <summary>
        ///     Adds the value at the end of the sequence.
        /// </summary>
        /// <remarks>
        ///     If the table is not a sequence,
        ///     i.e. if the keys of the table are not consecutive integers,
        ///     this might not return the expected result.
        /// </remarks>
        /// <param name="value"> The value to add. </param>
        public void Add<T>(T value)
        {
            var length = Length;
            var lua = _table.Lua;
            var L = lua.State.L;

            lua.Stack.EnsureFreeCapacity(2);

            using (lua.Stack.SnapshotCount())
            {
                lua.Stack.Push(_table);
                lua.Stack.Push(value);
                lua_seti(L, -2, length + 1);
            }
        }

        /// <summary>
        ///     Inserts the value at the given index in the sequence.
        ///     Shifts other values accordingly.
        /// </summary>
        /// <remarks>
        ///     <inheritdoc cref="Add{T}"/>
        /// </remarks>
        /// <param name="index"> The one-based index in the sequence. </param>
        /// <param name="value"> The value to insert. </param>
        public void Insert<T>(lua_Integer index, T value)
        {
            var length = Length;
            if (index < 1 || index > length + 1)
            {
                Throw.ArgumentOutOfRangeException(nameof(index), "The index is outside the bounds of the sequence.");
            }

            var lua = _table.Lua;
            var L = lua.State.L;

            lua.Stack.EnsureFreeCapacity(2);

            using (lua.Stack.SnapshotCount())
            {
                lua.Stack.Push(_table);
                var tableIndex = lua.Stack[-1].Index;

                for (var i = length + 1; i > index; i--)
                {
                    lua_geti(L, tableIndex, i - 1);
                    lua_seti(L, tableIndex, i);
                }

                lua.Stack.Push(value);
                lua_seti(L, tableIndex, index);
            }
        }

        /// <summary>
        ///     Removes the value at the specified index in the sequence.
        ///     Shifts other values accordingly.
        /// </summary>
        /// <remarks>
        ///     <inheritdoc cref="Add{T}"/>
        /// </remarks>
        /// <param name="index"> The one-based index in the sequence. </param>
        public void RemoveAt(lua_Integer index)
        {
            var length = Length;
            if (index < 1 || index > length)
            {
                Throw.ArgumentOutOfRangeException(nameof(index), "The index is outside the bounds of the sequence.");
            }

            var lua = _table.Lua;
            var L = lua.State.L;

            lua.Stack.EnsureFreeCapacity(2);

            using (lua.Stack.SnapshotCount())
            {
                lua.Stack.Push(_table);
                var tableIndex = lua.Stack[-1].Index;

                for (; index < length; index++)
                {
                    lua_geti(L, tableIndex, index + 1);
                    lua_seti(L, tableIndex, index);
                }

                lua_pushnil(L);
                lua_seti(L, tableIndex, index);
            }
        }

        /// <summary>
        ///     Clears the values in the sequence.
        /// </summary>
        /// <remarks>
        ///     <inheritdoc cref="Add{T}"/>
        /// </remarks>
        public void Clear()
        {
            var length = Length;
            var lua = _table.Lua;
            var L = lua.State.L;

            lua.Stack.EnsureFreeCapacity(2);

            using (lua.Stack.SnapshotCount())
            {
                lua.Stack.Push(_table);

                for (var i = length; i > 0; i--)
                {
                    lua_pushnil(L);
                    lua_seti(L, -2, i);
                }
            }
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
            var thread = _table.Lua;
            thread.Stack.EnsureFreeCapacity(3);

            var L = thread.Lua.State.L;
            using (_table.Lua.Stack.SnapshotCount())
            {
                thread.Stack.Push(_table);
                var length = (int) luaL_len(L, -1);
                var list = new List<T>(Math.Min(length, 256));
                var index = 1;
                while (!lua_geti(L, -1, index++).IsNoneOrNil())
                {
                    if (!thread.Stack[-1].TryGetValue<T>(out var value))
                    {
                        if (throwOnNonConvertible)
                        {
                            Throw.InvalidOperationException($"Failed to convert the value {thread.Stack[-1]} to type {typeof(T)}.");
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
                var L = _table.Lua.State.L;
                _initialTop = lua_gettop(L);
                _moveTop = 0;
                _index = 0;

                table.Lua.Stack.EnsureFreeCapacity(2);

                try
                {
                    _table.Lua.Stack.Push(_table);
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
                var L = _table.Lua.State.L;
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
                var L = _table.Lua.State.L;
                lua_settop(L, _initialTop);
            }
        }
    }
}
