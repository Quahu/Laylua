using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Laylua.Moon;

namespace Laylua.Marshaling;

public class DefaultUserDataDescriptorProvider : UserDataDescriptorProvider
{
    private readonly Dictionary<Type, UserDataDescriptor?> _descriptorDictionary;
    private readonly List<(Type, UserDataDescriptor?)> _descriptorList;
    private readonly DelegateUserDataDescriptor _delegateDescriptor;

    public DefaultUserDataDescriptorProvider()
    {
        _descriptorDictionary = new Dictionary<Type, UserDataDescriptor?>();
        _descriptorList = new List<(Type, UserDataDescriptor?)>();
        _delegateDescriptor = new DelegateUserDataDescriptor();
    }

    /// <inheritdoc/>
    public override void SetDescriptor(Type type, UserDataDescriptor? descriptor)
    {
        _descriptorDictionary[type] = descriptor;

        var existingIndex = _descriptorList.IndexOf((type, descriptor));
        if (existingIndex == -1)
        {
            _descriptorList.Add((type, descriptor));
            _descriptorList.Sort(static (a, b) =>
            {
                var aType = a.Item1;
                var bType = b.Item1;
                if (aType == bType)
                    return 0;

                if (aType.IsAssignableFrom(bType))
                    return -1;

                if (bType.IsAssignableFrom(aType))
                    return 1;

                return 0;
            });
        }
        else
        {
            _descriptorList[existingIndex] = (type, descriptor);
        }
    }

    /// <inheritdoc/>
    public override bool TryGetDescriptor<T>(T obj, [MaybeNullWhen(false)] out UserDataDescriptor descriptor)
    {
        if (obj is Delegate)
        {
            if (obj is LuaCFunction)
            {
                descriptor = null;
                return false;
            }

            descriptor = _delegateDescriptor;
            return true;
        }

        var objType = obj!.GetType();
        if (_descriptorDictionary.TryGetValue(objType, out descriptor))
        {
            return descriptor != null;
        }

        for (var i = 0; i < _descriptorList.Count; i++)
        {
            var tuple = _descriptorList[i];
            if (tuple.Item1.IsAssignableFrom(objType))
            {
                descriptor = tuple.Item2;
                return descriptor != null;
            }
        }

        descriptor = default;
        return false;
    }
}
