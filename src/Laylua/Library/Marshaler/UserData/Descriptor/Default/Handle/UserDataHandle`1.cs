using System;
using System.Diagnostics.CodeAnalysis;

namespace Laylua.Marshaling;

public sealed class UserDataHandle<T> : UserDataHandle
{
    /// <summary>
    ///     The value of this handle.
    /// </summary>
    public T Value;

    internal UserDataHandle(Lua lua, T value, UserDataDescriptor descriptor)
        : base(lua, descriptor)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public override bool TryGetType(out Type type)
    {
        type = typeof(T);
        return true;
    }

    /// <inheritdoc/>
    public override bool TryGetValue<TTarget>([MaybeNullWhen(false)] out TTarget value)
    {
        if (Value is TTarget)
        {
            value = (TTarget) (object) Value;
            return true;
        }

        value = default;
        return false;
    }
}
