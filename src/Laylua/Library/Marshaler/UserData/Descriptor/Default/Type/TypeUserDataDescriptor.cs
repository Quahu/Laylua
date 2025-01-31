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

    public override int Add(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Subtract(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Multiply(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Modulo(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Power(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Divide(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int FloorDivide(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseAnd(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseOr(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseExclusiveOr(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftLeft(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftRight(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Negate(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public override int BitwiseNot(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public override int Concat(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Length(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public override int Equal(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThan(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThanOrEqual(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Index(LuaThread thread, LuaStackValue userData, LuaStackValue key)
    {
        if (_indexDelegate != null)
        {
            return _indexDelegate(thread, userData, key);
        }

        return 0;
    }

    public override int NewIndex(LuaThread thread, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        if (_newIndexDelegate != null)
        {
            return _newIndexDelegate(thread, userData, key, value);
        }

        return 0;
    }

    public override int Close(LuaThread thread, LuaStackValue userData, LuaStackValue error)
    {
        // if (_closeDelegate != null)
        // {
        //     return _closeDelegate(lua, userData);
        // }

        return 0;
    }

    public override int ToString(LuaThread thread, LuaStackValue userData)
    {
        // if (_toStringDelegate != null)
        // {
        //     return _toStringDelegate(lua, userData);
        // }

        thread.Stack.Push(Type.ToTypeString());
        return 1;
    }
}
