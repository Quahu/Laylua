using System;
using Laylua.Moon;
using Qommon.Collections.ThreadSafe;

namespace Laylua.Marshaling;

public class DefaultUserDataDescriptorProvider : UserDataDescriptorProvider
{
    private readonly DelegateUserDataDescriptor _delegateDescriptor;
    private readonly IThreadSafeDictionary<Type, UserDataDescriptor?> _descriptors;

    public DefaultUserDataDescriptorProvider()
    {
        _descriptors = ThreadSafeDictionary.ConcurrentDictionary.Create<Type, UserDataDescriptor?>();
        _delegateDescriptor = new DelegateUserDataDescriptor();
    }

    public override void SetDescriptor(Type type, UserDataDescriptor descriptor)
    {
        _descriptors[type] = descriptor;
    }

    protected virtual UserDataDescriptor? CreateDescriptor<T>(T obj)
    {
        return null;
    }

    public override UserDataDescriptor? GetDescriptor<T>(T obj)
    {
        if (obj is Delegate)
        {
            if (obj is LuaCFunction)
                return null;

            return _delegateDescriptor;
        }

        return _descriptors.GetOrAdd(typeof(T), (_, state) =>
        {
            var (@this, obj) = state;
            return @this.CreateDescriptor(obj);
        }, (this, obj));
    }
}
