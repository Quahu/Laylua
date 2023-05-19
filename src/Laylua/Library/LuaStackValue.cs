using System;
using System.Runtime.CompilerServices;
using Laylua.Marshalling;
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
            if (_lua == null)
                return LuaType.None;

            return lua_type(_lua.GetStatePointer(), Index);
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

    private readonly Lua _lua;

    internal LuaStackValue(Lua lua, int index)
    {
        _lua = lua;
        Index = index;
    }

    private void ThrowIfInvalid()
    {
        if (Type == LuaType.None)
            throw new InvalidOperationException($"The index of this {nameof(LuaStackValue)} is not valid.");
    }

    /// <summary>
    ///     Pushes the value of this stack value
    ///     onto the stack.
    /// </summary>
    public void PushValue()
    {
        ThrowIfInvalid();

        var L = _lua.GetStatePointer();
        lua_pushvalue(L, Index);
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

        return _lua.Marshaler.ToObject<T>(Index);
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
        ThrowIfInvalid();

        return _lua.Marshaler.TryToObject(Index, out value);
    }

    /// <summary>
    ///     Sets the value of this stack value.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    public void SetValue<T>(T value)
    {
        ThrowIfInvalid();

        _lua.Stack.EnsureFreeCapacity(1);

        _lua.Marshaler.PushObject(value);
        lua_replace(_lua.GetStatePointer(), Index);
    }

    /// <inheritdoc/>
    public bool Equals(LuaStackValue other)
    {
        if (_lua == null && other._lua == null)
            return true;

        if (_lua != other._lua)
            return false;

        return lua_compare(_lua.GetStatePointer(), Index, other.Index, LuaComparison.Equal);
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
        if (type == LuaType.None || _lua.IsDisposed)
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
