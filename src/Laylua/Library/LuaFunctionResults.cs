using System;
using System.Collections;
using System.Collections.Generic;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a range of values on the stack
///     that are the results of calling a Lua function.
/// </summary>
/// <remarks>
///     After you are done working with the results,
///     you can dispose of this type to remove the function results from the stack.
///     <para/>
///     If you remove any of the function results yourself,
///     do not dispose of this type and instead remove all results yourself.
/// </remarks>
public struct LuaFunctionResults : IEnumerable<LuaStackValue>, IDisposable
{
    /// <inheritdoc cref="LuaStackValueRange.IsEmpty"/>
    public readonly bool IsEmpty
    {
        get
        {
            ThrowIfDisposed();

            return _range.IsEmpty;
        }
    }

    /// <inheritdoc cref="LuaStackValueRange.Count"/>
    public readonly int Count
    {
        get
        {
            ThrowIfDisposed();

            return _range.Count;
        }
    }

    /// <inheritdoc cref="LuaStackValueRange.First"/>
    public readonly LuaStackValue First
    {
        get
        {
            ThrowIfDisposed();

            return _range.First;
        }
    }

    /// <inheritdoc cref="LuaStackValueRange.Last"/>
    public readonly LuaStackValue Last
    {
        get
        {
            ThrowIfDisposed();

            return _range.Last;
        }
    }

    private readonly LuaStackValueRange _range;
    private bool _isDisposed;

    internal LuaFunctionResults(LuaStackValueRange range)
    {
        _range = range;
    }

    private readonly void ThrowIfDisposed()
    {
        if (_isDisposed)
            Throw.ObjectDisposedException(nameof(LuaStackValueRange));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _range._stack.RemoveRange(_range._index, _range._count);
        }

        _isDisposed = true;
    }

    public readonly Enumerator GetEnumerator()
    {
        ThrowIfDisposed();

        return new Enumerator(this);
    }

    IEnumerator<LuaStackValue> IEnumerable<LuaStackValue>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<LuaStackValue>
    {
        /// <inheritdoc/>
        public LuaStackValue Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        private LuaStackValueRange.Enumerator _enumerator;

        internal Enumerator(LuaFunctionResults results)
        {
            _enumerator = results._range.GetEnumerator();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _enumerator.Reset();
        }

        void IDisposable.Dispose()
        { }
    }
}
