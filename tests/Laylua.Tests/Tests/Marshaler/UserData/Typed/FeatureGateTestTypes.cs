using Laylua.Marshaling;

namespace Laylua.Tests;

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Properties)]
public partial class TestGateNoProperties
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Methods)]
public partial class TestGateNoMethods
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Fields)]
public partial class TestGateNoFields
{
    public int Value { get; set; } = 42;

    public int Field = 99;
}

[LuaType(Members = LuaTypeMembers.Properties)]
public partial class TestGatePropertiesOnly
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }

    public event Action? TestEvent;
}

[LuaType(Members = LuaTypeMembers.Methods)]
public partial class TestGateMethodsOnly
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Constants)]
public partial class TestGateNoConstants
{
    public const int ConstantValue = 42;

    public static int StaticValue { get; set; } = 7;
}
