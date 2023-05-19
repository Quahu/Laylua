using Qommon;

namespace Laylua.Marshalling;

public class TypeUserDataDescriptor<TUserData> : CallbackBasedUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => typeof(TUserData).ToTypeString().ToIdentifier();

    /// <inheritdoc/>
    public override CallbackUserDataDescriptorFlags Flags
    {
        get
        {
            var flags = CallbackUserDataDescriptorFlags.None;

            return flags;
        }
    }

    public TypeUserDataDescriptor()
    {
        Guard.IsFalse(typeof(TUserData).IsInterface);

        if (typeof(TUserData).IsGenericType)
            Guard.IsTrue(typeof(TUserData).IsConstructedGenericType);
    }

    public override int Add(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Subtract(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Multiply(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Modulo(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Power(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Divide(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int FloorDivide(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseAnd(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseOr(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int BitwiseExclusiveOr(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftLeft(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int ShiftRight(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Negate(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int BitwiseNot(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int Concat(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Length(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int Equal(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThan(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int LessThanOrEqual(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public override int Index(Lua lua, LuaStackValue userData, LuaStackValue key)
    {
        return 0;
    }

    public override int NewIndex(Lua lua, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        return 0;
    }

    public override int Close(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public override int ToString(Lua lua, LuaStackValue userData)
    {
        return 0;
    }
}
