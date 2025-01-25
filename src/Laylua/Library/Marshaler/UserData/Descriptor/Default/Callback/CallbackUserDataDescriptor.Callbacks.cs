namespace Laylua.Marshaling;

public abstract partial class CallbackUserDataDescriptor
{
    public virtual int Pairs(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Add(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Subtract(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Multiply(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Modulo(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Power(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Divide(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int FloorDivide(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseAnd(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseOr(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseExclusiveOr(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftLeft(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftRight(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Negate(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int BitwiseNot(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Concat(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Length(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Equal(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThan(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThanOrEqual(LuaThread lua, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Index(LuaThread lua, LuaStackValue userData, LuaStackValue key)
    {
        return 0;
    }

    public virtual int NewIndex(LuaThread lua, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        return 0;
    }

    public virtual int Call(LuaThread lua, LuaStackValue userData, LuaStackValueRange arguments)
    {
        return 0;
    }

    public virtual int Close(LuaThread lua, LuaStackValue userData, LuaStackValue error)
    {
        return 0;
    }

    public virtual int ToString(LuaThread lua, LuaStackValue userData)
    {
        return 0;
    }
}
