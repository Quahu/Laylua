namespace Laylua.Marshaling;

public abstract partial class CallbackUserDataDescriptor
{
    public virtual int Pairs(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Add(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Subtract(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Multiply(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Modulo(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Power(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Divide(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int FloorDivide(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseAnd(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseOr(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int BitwiseExclusiveOr(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftLeft(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int ShiftRight(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Negate(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int BitwiseNot(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Concat(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Length(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }

    public virtual int Equal(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThan(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int LessThanOrEqual(LuaThread thread, LuaStackValue left, LuaStackValue right)
    {
        return 0;
    }

    public virtual int Index(LuaThread thread, LuaStackValue userData, LuaStackValue key)
    {
        return 0;
    }

    public virtual int NewIndex(LuaThread thread, LuaStackValue userData, LuaStackValue key, LuaStackValue value)
    {
        return 0;
    }

    public virtual int Call(LuaThread thread, LuaStackValue userData, LuaStackValueRange arguments)
    {
        return 0;
    }

    public virtual int Close(LuaThread thread, LuaStackValue userData, LuaStackValue error)
    {
        return 0;
    }

    public virtual int ToString(LuaThread thread, LuaStackValue userData)
    {
        return 0;
    }
}
