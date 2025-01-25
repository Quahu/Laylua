using System;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua.Marshaling;

public abstract class TypeUserDataDescriptor : CallbackUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => Type.ToTypeString().ToIdentifier();

    /// <inheritdoc/>
    public override CallbackUserDataDescriptorFlags Callbacks
    {
        get
        {
            var flags = CallbackUserDataDescriptorFlags.None;

            if (_indexDelegate != null)
            {
                flags |= CallbackUserDataDescriptorFlags.Index;
            }

            if (_newIndexDelegate != null)
            {
                flags |= CallbackUserDataDescriptorFlags.NewIndex;
            }

            return flags;
        }
    }

    /// <summary>
    ///     Gets the type of this descriptor.
    /// </summary>
    public Type Type { get; }

    private readonly UserDataDescriptorUtilities.IndexDelegate? _indexDelegate;
    private readonly UserDataDescriptorUtilities.NewIndexDelegate? _newIndexDelegate;

    protected TypeUserDataDescriptor(Type type, bool isTypeDefinition,
        TypeMemberProvider? memberProvider,
        UserDataNamingPolicy? namingPolicy,
        CallbackUserDataDescriptorFlags disabledCallbacks)
    {
        Guard.IsTrue(RuntimeFeature.IsDynamicCodeSupported);

        if (type.IsGenericType)
        {
            if (!type.IsConstructedGenericType)
            {
                Throw.ArgumentException("Generic types must be constructed generic types.", nameof(type));
            }
        }

        memberProvider ??= TypeMemberProvider.Default;
        namingPolicy ??= UserDataNamingPolicy.Original;
        if (!disabledCallbacks.HasFlag(CallbackUserDataDescriptorFlags.Index))
        {
            _indexDelegate = UserDataDescriptorUtilities.CreateIndex(type, isTypeDefinition, memberProvider, namingPolicy);
        }

        if (!disabledCallbacks.HasFlag(CallbackUserDataDescriptorFlags.NewIndex))
        {
            _newIndexDelegate = UserDataDescriptorUtilities.CreateNewIndex(type, isTypeDefinition, memberProvider, namingPolicy);
        }

        Type = type;
    }

    public override int Add(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Subtract(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Multiply(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Modulo(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Power(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Divide(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int FloorDivide(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseAnd(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseOr(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseExclusiveOr(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftLeft(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftRight(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Negate(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int BitwiseNot(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int Concat(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Length(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int Equal(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThan(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThanOrEqual(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Index(LuaThread lua, LuaStackValue userData, LuaStackValue key)
    {
        if (_indexDelegate != null)
        {
            return _indexDelegate(lua, userData, key);
        }

        return 0;
    }

    public override int NewIndex(LuaThread lua, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        if (_newIndexDelegate != null)
        {
            return _newIndexDelegate(lua, userData, key, value);
        }

        return 0;
    }

    public override int Close(LuaThread lua, LuaStackValue userData, LuaStackValue error)
    {
        // if (_closeDelegate != null)
        // {
        //     return _closeDelegate(lua, userData);
        // }

        return 0;
    }

    public override int ToString(LuaThread lua, LuaStackValue userData)
    {
        // if (_toStringDelegate != null)
        // {
        //     return _toStringDelegate(lua, userData);
        // }

        lua.Stack.Push(Type.ToTypeString());
        return 1;
    }
}
