using System;
using System.Diagnostics.CodeAnalysis;
using Qommon;

namespace Laylua.Marshaling;

public class DescribedUserData<T> : DescribedUserData
    where T : notnull
{
    public T Value { get; }

    public DescribedUserData(T value, UserDataDescriptor descriptor)
        : base(descriptor)
    {
        if (value is DescribedUserData)
        {
            Throw.ArgumentException($"Nesting {nameof(DescribedUserData)} instances is not supported.", nameof(value));
        }

        Value = value;
    }

    internal override UserDataHandle CreateUserDataHandle(Lua lua)
    {
        return new UserDataHandle<T>(lua, Value, Descriptor);
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
