using System;

namespace Laylua.Marshaling;

public abstract class UserDataDescriptorProvider
{
    public abstract void SetDescriptor(Type type, UserDataDescriptor descriptor);

    public abstract UserDataDescriptor? GetDescriptor<T>(T obj);
}
