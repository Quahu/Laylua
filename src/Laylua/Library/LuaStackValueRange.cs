using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a range of values on the Lua stack.
/// </summary>
public readonly struct LuaStackValueRange
{
    /// <summary>
    ///     Gets an empty range.
    /// </summary>
    public static LuaStackValueRange Empty => default;

    /// <summary>
    ///     Gets whether this range is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => _count == 0;
    }

    /// <summary>
    ///     Gets the amount of values this range contains.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => _count;
    }

    /// <summary>
    ///     Gets or sets the value at the given index on the stack.
    /// </summary>
    /// <param name="index"> The index within the range. </param>
    public LuaStackValue this[int index]
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            ThrowArgumentOutOfRangeIfInvalid(index);

            return _stack[_index + (index - 1)];
        }
    }

    /// <summary>
    ///     Gets the first value of this range.
    /// </summary>
    /// <remarks>
    ///     Useful for functions known to take/return a single value.
    /// </remarks>
    public LuaStackValue First
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            ThrowArgumentOutOfRangeIfInvalid(_index);

            return _stack[_index];
        }
    }

    /// <summary>
    ///     Gets the last value of this range.
    /// </summary>
    /// <remarks>
    ///     Useful for functions known to take/return a single value.
    /// </remarks>
    public LuaStackValue Last
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            var index = _index + _count - 1;
            ThrowArgumentOutOfRangeIfInvalid(index);

            return _stack[index];
        }
    }

    internal readonly LuaStack _stack;
    internal readonly int _index;
    internal readonly int _count;

    internal LuaStackValueRange(LuaStack stack, int index, int count)
    {
        _stack = stack;
        _index = index;
        _count = count;
    }

    internal static LuaStackValueRange FromTop(LuaStack stack, int oldTop, int newTop)
    {
        var count = newTop - oldTop;
        if (count == 0)
            return Empty;

        return new LuaStackValueRange(stack, oldTop + 1, count);
    }

    private void ThrowArgumentOutOfRangeIfInvalid(int index)
    {
        if (_stack == null)
            Throw.ArgumentOutOfRangeException(nameof(index), index, null);
    }

    /// <summary>
    ///     Pushes the values of this stack value range
    ///     onto the stack.
    /// </summary>
    public void PushValues()
    {
        if (IsEmpty)
            return;

        for (var i = 0; i < _count; i++)
        {
            this[i + 1].PushValue();
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<LuaStackValue>
    {
        private readonly LuaStackValueRange _range;

        /// <inheritdoc/>
        public LuaStackValue Current
        {
            get
            {
                if (_index == 0)
                    return default;

                return _range[_index];
            }
        }

        object IEnumerator.Current => Current;

        private int _index;

        internal Enumerator(LuaStackValueRange range)
        {
            _range = range;

            Reset();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index == _range.Count)
            {
                return false;
            }

            _index++;
            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _index = 0;
        }

        void IDisposable.Dispose()
        { }
    }
}
