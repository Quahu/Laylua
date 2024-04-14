using System;
using System.Diagnostics.CodeAnalysis;

namespace Laylua.Marshaling;

public abstract class DescribedUserData
{
    public UserDataDescriptor Descriptor { get; }

    protected DescribedUserData(UserDataDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    internal abstract UserDataHandle CreateUserDataHandle(Lua lua);

    /// <summary>
    ///     Attempts to get the type of the value of this described userdata.
    /// </summary>
    public virtual bool TryGetType([MaybeNullWhen(false)] out Type type)
    {
        type = default;
        return false;
    }

    /// <summary>
    ///     Attempts to get the value of this described userdata.
    /// </summary>
    public virtual bool TryGetValue<TTarget>([MaybeNullWhen(false)] out TTarget value)
    {
        value = default;
        return false;
    }

    public static DescribedUserData<Type> Definition<TType>(
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = default)
    {
        return Definition(typeof(TType), memberProvider, namingPolicy, disabledCallbacks);
    }

    public static DescribedUserData<Type> Definition(Type type,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = default)
    {
        return new DescribedUserData<Type>(type, new DefinitionTypeUserDataDescriptor(type, memberProvider, namingPolicy, disabledCallbacks));
    }

    public static DescribedUserData<TValue> Instance<TValue>(TValue value, UserDataDescriptor descriptor)
        where TValue : notnull
    {
        return new DescribedUserData<TValue>(value, descriptor);
    }

    public static DescribedUserData<TValue> Instance<TValue>(TValue value,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = default)
        where TValue : notnull
    {
        return new DescribedUserData<TValue>(value, new InstanceTypeUserDataDescriptor(value.GetType(), memberProvider, namingPolicy, disabledCallbacks));
    }
}
