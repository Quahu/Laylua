using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Laylua.Moon;

namespace Laylua.Marshaling;

public class DefaultUserDataDescriptorProvider : UserDataDescriptorProvider
{
    private readonly Dictionary<Type, UserDataDescriptor> _typeDescriptorDictionary;
    private readonly Dictionary<Type, UserDataDescriptor> _valuesOfTypeDescriptorDictionary;
    private readonly List<(Type Type, UserDataDescriptor Descriptor)> _valuesOfTypeDescriptorList;
    private readonly DelegateUserDataDescriptor _delegateDescriptor;

    public DefaultUserDataDescriptorProvider()
    {
        _typeDescriptorDictionary = new Dictionary<Type, UserDataDescriptor>();
        _valuesOfTypeDescriptorDictionary = new Dictionary<Type, UserDataDescriptor>();
        _valuesOfTypeDescriptorList = new List<(Type, UserDataDescriptor)>();
        _delegateDescriptor = new DelegateUserDataDescriptor();
    }

    public void SetDescriptorForType(Type type, UserDataDescriptor? descriptor)
    {
        lock (_typeDescriptorDictionary)
        {
            if (descriptor != null)
            {
                _typeDescriptorDictionary[type] = descriptor;
            }
            else
            {
                _typeDescriptorDictionary.Remove(type);
            }
        }
    }

    public void SetDescriptorForValuesOfType(Type type, UserDataDescriptor? descriptor)
    {
        lock (_valuesOfTypeDescriptorDictionary)
        {
            if (descriptor != null)
            {
                _valuesOfTypeDescriptorDictionary[type] = descriptor;
            }
            else
            {
                _valuesOfTypeDescriptorDictionary.Remove(type);
            }
        }

        lock (_valuesOfTypeDescriptorList)
        {
            if (descriptor != null)
            {
                var existingIndex = _valuesOfTypeDescriptorList.IndexOf((type, descriptor));
                if (existingIndex == -1)
                {
                    _valuesOfTypeDescriptorList.Add((type, descriptor));
                    _valuesOfTypeDescriptorList.Sort(static (a, b) =>
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
                    _valuesOfTypeDescriptorList[existingIndex] = (type, descriptor);
                }
            }
            else
            {
                var count = _valuesOfTypeDescriptorList.Count;
                for (var i = 0; i < count; i++)
                {
                    if (_valuesOfTypeDescriptorList[i].Type != type)
                        continue;

                    _valuesOfTypeDescriptorList.RemoveAt(i);
                    break;
                }
            }
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

        if (obj is Type)
        {
            lock (_typeDescriptorDictionary)
            {
                if (_typeDescriptorDictionary.TryGetValue((Type) (object) obj, out descriptor))
                {
                    return true;
                }
            }
        }

        var objType = obj!.GetType();
        lock (_valuesOfTypeDescriptorDictionary)
        {
            if (_valuesOfTypeDescriptorDictionary.TryGetValue(objType, out descriptor))
            {
                return true;
            }
        }

        lock (_valuesOfTypeDescriptorList)
        {
            for (var i = 0; i < _valuesOfTypeDescriptorList.Count; i++)
            {
                var tuple = _valuesOfTypeDescriptorList[i];
                if (tuple.Type.IsAssignableFrom(objType))
                {
                    descriptor = tuple.Descriptor;
                    return true;
                }
            }
        }

        descriptor = default;
        return false;
    }
}
