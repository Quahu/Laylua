namespace Laylua.Marshalling;

public abstract partial class CallbackBasedUserDataDescriptor
{
    public virtual int Pairs(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Add(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Subtract(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Multiply(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Modulo(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Power(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Divide(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int FloorDivide(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseAnd(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseOr(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseExclusiveOr(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftLeft(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftRight(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Negate(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int BitwiseNot(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Concat(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Length(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Equal(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThan(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThanOrEqual(Lua lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Index(Lua lua, LuaStackValue userData, LuaStackValue key)
    {
        return 0;
    }

    public virtual int NewIndex(Lua lua, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        return 0;
    }

    public virtual int Call(Lua lua, LuaStackValue userData, LuaStackValueRange arguments)
    {
        return 0;
    }

    public virtual int Close(Lua lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int ToString(Lua lua, LuaStackValue userData)
    {
        return 0;
    }
}
