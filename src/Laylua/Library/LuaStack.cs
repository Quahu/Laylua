using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents the Lua stack.
/// </summary>
/// <remarks>
///     See <a href="https://www.lua.org/manual/5.4/manual.html#4.1">Lua manual</a> for information
///     on how the stack works.
/// </remarks>
public unsafe class LuaStack
{
    /// <summary>
    ///     Gets whether the stack is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => lua_gettop(_thread.State.L) == 0;
    }

    /// <summary>
    ///     Gets or sets the amount of values on the stack.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => lua_gettop(_thread.State.L);

        [MethodImpl(MethodImplOptions.NoInlining)]
        set => lua_settop(_thread.State.L, value);
    }

    /// <summary>
    ///     Gets or sets the value at the given index on the stack.
    /// </summary>
    /// <param name="index"> The index on the stack. </param>
    /// <remarks>
    ///     <inheritdoc cref="LuaStackValue"/>
    /// </remarks>
    public LuaStackValue this[int index]
    {
        get
        {
            ValidateIndex(index);

            return new(_thread, lua_absindex(_thread.State.L, index));
        }
    }

    /// <summary>
    ///     Gets the first value on the stack,
    ///     i.e. the value at the bottom of the stack.
    /// </summary>
    public LuaStackValue First => this[1];

    /// <summary>
    ///     Gets the last value on the stack,
    ///     i.e. the value at the top of the stack.
    /// </summary>
    /// <remarks>
    ///     The returned stack value refers to the last value
    ///     on the stack at the time of retrieval.
    ///     See <see cref="LuaStackValue"/> remarks for more information.
    /// </remarks>
    public LuaStackValue Last => this[-1];

    private readonly LuaThread _thread;

    internal LuaStack(LuaThread thread)
    {
        _thread = thread;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(int index, string? parameterName = null)
    {
        if (index == 0 || Math.Abs(index) > Count)
        {
            Throw.ArgumentOutOfRangeException(parameterName ?? nameof(index), index, "The index specified is not a valid stack index.");
        }
    }

    /// <summary>
    ///     Returns a range starting at the specified absolute index.
    /// </summary>
    /// <param name="index"> The absolute index on the stack. </param>
    /// <returns>
    ///     The range.
    /// </returns>
    public LuaStackValueRange GetRange(int index)
    {
        if (index < 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(index), "The index specified is not an absolute index.");
        }

        ValidateIndex(index);

        return new LuaStackValueRange(this, index, Count - (index - 1));
    }

    /// <summary>
    ///     Returns a range starting at the specified absolute index
    ///     with n (<paramref name="count"/>) values.
    /// </summary>
    /// <param name="index"> The absolute index on the stack. </param>
    /// <param name="count"> The amount of values. </param>
    /// <returns>
    ///     The range.
    /// </returns>
    public LuaStackValueRange GetRange(int index, int count)
    {
        if (index < 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(index), "The index specified is not an absolute index.");
        }

        ValidateIndex(index);

        if (count < 0 || index + count - 1 > Count + 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(count), "The count specified is not valid.");
        }

        return new LuaStackValueRange(this, index, count);
    }

    /// <summary>
    ///     Returns the absolute index for the given index.
    /// </summary>
    /// <param name="index"> The index on the stack. </param>
    /// <returns>
    ///     The absolute index on the stack.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetAbsoluteIndex(int index)
    {
        return lua_absindex(_thread.State.L, index);
    }

    /// <summary>
    ///     Tries to ensure n (<paramref name="count"/>) values can be pushed onto the stack.
    /// </summary>
    /// <param name="count"> The amount of values to be pushed onto the stack. </param>
    /// <returns>
    ///     <see langword="false"/> if the stack could not be resized.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryEnsureFreeCapacity(int count)
    {
        return lua_checkstack(_thread.State.L, count);
    }

    /// <summary>
    ///     Ensures n (<paramref name="count"/>) values can be pushed onto the stack.
    /// </summary>
    /// <param name="count"> The amount of values to be pushed onto the stack. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureFreeCapacity(int count)
    {
        if (!lua_checkstack(_thread.State.L, count))
        {
            Throw.InvalidOperationException($"The stack could not be resized to fit {count} extra values.");
        }
    }

    /// <summary>
    ///     Pops <paramref name="count"/> values from the stack.
    /// </summary>
    /// <param name="count"> The amount of values to pop. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Pop(int count = 1)
    {
        lua_pop(_thread.State.L, count);
    }

    /// <summary>
    ///     Pushes the value onto the stack.
    /// </summary>
    /// <remarks>
    ///     <b>Use <see cref="LuaStack.EnsureFreeCapacity"/> prior to calling this method.</b>
    ///     <br/>
    ///     Ensuring free stack space is left up to the caller for performance reasons.
    /// </remarks>
    /// <param name="value"> The value to push. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Push<T>(T value)
    {
        _thread.Marshaler.PushValue(_thread, value);
    }

    /// <summary>
    ///     Pushes nil onto the stack.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="Push{T}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PushNil()
    {
        lua_pushnil(_thread.State.L);
    }

    /// <summary>
    ///     Pushes a new <see cref="LuaTable"/> onto the stack.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="Push{T}"/>
    /// </remarks>
    /// <param name="sequenceCapacity"> The size hint for how many items keyed with integers the table will hold. </param>
    /// <param name="tableCapacity"> The size hint for how many items keyed with non-integers the table will hold. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PushNewTable(int sequenceCapacity = 0, int tableCapacity = 0)
    {
        lua_createtable(_thread.State.L, sequenceCapacity, tableCapacity);
    }

    /// <summary>
    ///     Inserts the value at the given index on the stack,
    ///     shifting up the values above the index.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="Push{T}"/>
    /// </remarks>
    /// <param name="index"> The index on the stack. </param>
    /// <param name="value"> The value to insert. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Insert<T>(int index, T value)
    {
        ValidateIndex(index);

        Push(value);
        lua_insert(_thread.State.L, index);
    }

    /// <summary>
    ///     Copies the stack value from <paramref name="fromIndex"/> to <paramref name="toIndex"/>,
    ///     overwriting the existing value.
    /// </summary>
    /// <remarks>
    ///     <paramref name="toIndex"/> must be a valid existing stack index.
    /// </remarks>
    /// <param name="fromIndex"> The index on the stack to copy from. </param>
    /// <param name="toIndex"> The index on the stack to copy to. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Copy(int fromIndex, int toIndex)
    {
        ValidateIndex(fromIndex, nameof(fromIndex));
        ValidateIndex(toIndex, nameof(toIndex));

        lua_copy(_thread.State.L, fromIndex, toIndex);
    }

    /// <summary>
    ///     Rotates the stack values between the index and the top of the stack.
    /// </summary>
    /// <param name="index"> The index on the stack. </param>
    /// <param name="count"> The amount of indices to rotate. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Rotate(int index, int count)
    {
        ValidateIndex(index);

        lua_rotate(_thread.State.L, index, count);
    }

    /// <summary>
    ///     Removes the stack value at the given index and shifts values
    ///     to fill the gap.
    /// </summary>
    /// <param name="index"> The index on the stack. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(int index)
    {
        ValidateIndex(index);

        lua_remove(_thread.State.L, index);
    }

    /// <summary>
    ///     Removes <paramref name="count"/> values from the stack
    ///     starting at the given index and shifting the remaining values to fill the gaps.
    /// </summary>
    /// <param name="index"> The starting index on the stack to remove from. </param>
    /// <param name="count"> The amount of stack values to remove. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveRange(int index, int count)
    {
        ValidateIndex(index);

        var top = Count;
        index = GetAbsoluteIndex(index);

        if (index + count - 1 > top)
        {
            Throw.ArgumentOutOfRangeException(nameof(count), "The amount of stack values to remove must not be greater than the amount of values available from the given index.");

            // count = top - index + 1;
        }

        Rotate(index, -count);
        Pop(count);
    }

    /// <summary>
    ///     Moves <paramref name="count"/> stack values from this thread's stack
    ///     to the <paramref name="target"/> thread's stack.
    ///     The threads must share the same main thread.
    /// </summary>
    /// <remarks>
    ///     This method ensures <paramref name="target"/> has enough stack space to fit <paramref name="count"/> stack values.
    /// </remarks>
    /// <param name="target"> The target thread to move the stack values to. </param>
    /// <param name="count"> The amount of stack values to move. </param>
    public void XMove(LuaThread target, int count)
    {
        if (count < 0 || count > Count)
        {
            Throw.ArgumentOutOfRangeException(nameof(count));
        }

        LuaReference.ValidateOwnership(_thread, target);

        target.Stack.EnsureFreeCapacity(count);
        lua_xmove(from: _thread.State.L, to: target.State.L, count);
    }

    /// <summary>
    ///     Snapshots the current stack count and returns a disposable instance
    ///     that will restore it upon disposal.
    /// </summary>
    /// <remarks>
    ///     If the amount of values on the stack is lower than the initial amount,
    ///     then when the snapshot is disposed the missing values are filled with nil.
    /// </remarks>
    /// <example>
    ///     The purpose of this is to easily prevent
    ///     leaving garbage values on the Lua stack when
    ///     performing multiple stack operations where any of them might fail.
    ///     <code language="csharp">
    ///     using (/* count snapshot */)
    ///     {
    ///         // push x
    ///         // push y
    ///         // logic
    ///     }
    ///     </code>
    ///     In the above example, if <c>push y</c> or <c>logic</c> fails, i.e. throws an exception,
    ///     the stack will be restored to the count prior to the code within the using block.
    /// </example>
    /// <returns>
    ///     A <see cref="LuaStackCountSnapshot"/>.
    /// </returns>
    public LuaStackCountSnapshot SnapshotCount()
    {
        return new LuaStackCountSnapshot(this);
    }

    /// <summary>
    ///     Returns an enumerator that can be used to enumerate the stack.
    /// </summary>
    /// <returns>
    ///     An enumerator wrapping the stack.
    /// </returns>
    public Enumerator GetEnumerator()
    {
        return new(_thread, Count);
    }

    /// <summary>
    ///     Represents an enumerator that can be used to enumerate the <see cref="LuaStack"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<LuaStackValue>
    {
        /// <inheritdoc/>
        public readonly LuaStackValue Current => _current;

        /// <inheritdoc/>
        readonly object IEnumerator.Current => Current;

        private LuaStackValue _current;
        private int _index;

        private readonly LuaThread _thread;
        private readonly int _top;

        internal Enumerator(LuaThread thread, int top)
        {
            _thread = thread;
            _top = top;
            _current = default;
            _index = 1;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            var index = _index++;
            if (index > _top)
            {
                _current = default;
                return false;
            }

            _current = _thread.Stack[index];
            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this = new Enumerator(_thread, _top);
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        { }
    }
}

/// <summary>
///     Represents a snapshot of <see cref="LuaStack.Count"/> which
///     will be reset when this type is disposed.
/// </summary>
public struct LuaStackCountSnapshot : IDisposable
{
    /// <summary>
    ///     Gets the snapshotted <see cref="LuaStack.Count"/>.
    /// </summary>
    public readonly int Count => _count;

    private LuaStack? _stack;
    private readonly int _count;

    /// <summary>
    ///     Instantiates a new <see cref="LuaStackCountSnapshot"/>.
    /// </summary>
    /// <param name="stack"> The stack. </param>
    internal LuaStackCountSnapshot(LuaStack stack)
    {
        _stack = stack;
        _count = stack.Count;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var stack = _stack;
        if (stack == null)
            return;

        stack.Count = _count;
        _stack = null;
    }
}
