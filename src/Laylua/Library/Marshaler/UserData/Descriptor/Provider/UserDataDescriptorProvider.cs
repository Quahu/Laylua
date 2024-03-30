using System;
using System.Diagnostics.CodeAnalysis;

namespace Laylua.Marshaling;

public abstract class UserDataDescriptorProvider
{
    /// <summary>
    ///     Gets the default shared instance of <see cref="DefaultUserDataDescriptorProvider"/>.
    /// </summary>
    public static DefaultUserDataDescriptorProvider Default { get; } = new();

    public abstract void SetDescriptor(Type type, UserDataDescriptor? descriptor);

    public abstract bool TryGetDescriptor<T>(T obj, [MaybeNullWhen(false)] out UserDataDescriptor descriptor);
}
