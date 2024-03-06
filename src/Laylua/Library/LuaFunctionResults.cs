using System;
using System.Collections;
using System.Collections.Generic;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a range of values on the Lua stack
///     that are the results of calling a Lua function.
/// </summary>
/// <remarks>
///     After you have finished working with the results,
///     you can dispose of this object to remove the function results from the stack.
///     <para/>
///     If you manually remove any of the function results,
///     do not dispose of this object and instead remove the remaining results manually.
/// </remarks>
public struct LuaFunctionResults : IDisposable
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
        {
            Throw.ObjectDisposedException(nameof(LuaStackValueRange));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed && _range._stack != null)
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
