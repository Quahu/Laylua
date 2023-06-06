using System;
using System.Diagnostics.CodeAnalysis;

namespace Laylua.Marshaling;

public abstract class UserDataDescriptorProvider
{
    public abstract void SetDescriptor(Type type, UserDataDescriptor? descriptor);

    public abstract bool TryGetDescriptor<T>(T obj, [MaybeNullWhen(false)] out UserDataDescriptor descriptor);
}
