using System;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

/// <summary>
///     Represents a value on the Lua stack.
/// </summary>
/// <remarks>
///     The stack value is represented using an absolute index.
///     This means that if you retrieve a stack value using
///     a relative index such as <c>-1</c>
///     and, for example, the stack count is <c>3</c>,
///     then the stack value will have its index set to <c>3</c>.
///     From then onwards it will always refer to the third element on the stack
///     even if more values get pushed afterwards.
/// </remarks>
public readonly unsafe struct LuaStackValue : IEquatable<LuaStackValue>
{
    /// <summary>
    ///     Gets the index of this stack value.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets the type of this stack value.
    /// </summary>
    public LuaType Type
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (_thread == null)
                return LuaType.None;

            return lua_type(_thread.State.L, Index);
        }
    }

    /// <summary>
    ///     Gets whether this stack value is an integer.
    /// </summary>
    /// <remarks>
    ///     This can be used when <see cref="Type"/> returns <see cref="LuaType.Number"/>
    ///     to determine whether this value should be treated as an integer or a floating-point number.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the value is an integer number;
    ///     <see langword="false"/> if the value is a floating-point number.
    /// </returns>
    public bool IsInteger
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (_thread == null)
                return false;

            return lua_isinteger(_thread.State.L, Index);
        }
    }

    /// <summary>
    ///     Gets or sets the .NET value of this stack value.
    /// </summary>
    public object? Value
    {
        get => GetValue<object>();
        set => SetValue(value);
    }

    private readonly LuaThread _thread;

    internal LuaStackValue(LuaThread thread, int index)
    {
        _thread = thread;
        Index = index;
    }

    private void ThrowIfInvalid()
    {
        if (Type == LuaType.None)
            throw new InvalidOperationException($"The index of this {nameof(LuaStackValue)} is not valid.");
    }

    internal static void ValidateOwnership(LuaThread thread, LuaStackValue value)
    {
        if (thread.State.L != value._thread.State.L)
        {
            throw new InvalidOperationException($"The given stack value is owned by a different Lua thread.");
        }
    }

    /// <summary>
    ///     Gets the .NET value of this stack value.
    /// </summary>
    /// <typeparam name="T"> The expected type of the value. </typeparam>
    /// <returns>
    ///     The .NET value.
    /// </returns>
    public T? GetValue<T>()
    {
        ThrowIfInvalid();

        return _thread.Marshaler.GetValue<T>(_thread, Index);
    }

    /// <summary>
    ///     Attempts to get the .NET value of this stack value.
    /// </summary>
    /// <typeparam name="T"> The expected type of the value. </typeparam>
    /// <returns>
    ///     The .NET value.
    /// </returns>
    public bool TryGetValue<T>(out T? value)
    {
        if (Type == LuaType.None)
        {
            value = default;
            return false;
        }

        return _thread.Marshaler.TryGetValue(_thread, Index, out value);
    }

    /// <summary>
    ///     Sets the value of this stack value.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    public void SetValue<T>(T value)
    {
        ThrowIfInvalid();

        _thread.Stack.EnsureFreeCapacity(1);

        _thread.Marshaler.PushValue(_thread, value);
        lua_replace(_thread.State.L, Index);
    }

    /// <inheritdoc/>
    public bool Equals(LuaStackValue other)
    {
        if (_thread == null && other._thread == null)
            return true;

        if (_thread == null || other._thread == null)
            return false;

        if (_thread.State.L != other._thread.State.L)
            return false;

        return lua_compare(_thread.State.L, Index, other.Index, LuaComparison.Equal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not LuaStackValue other)
            return false;

        return Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Index;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var type = Type;
        if (type == LuaType.None || _thread.IsDisposed)
            return "<no value>";

        return $"{Index}: {type}";
    }

    public static bool operator ==(LuaStackValue left, LuaStackValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LuaStackValue left, LuaStackValue right)
    {
        return !left.Equals(right);
    }
}
