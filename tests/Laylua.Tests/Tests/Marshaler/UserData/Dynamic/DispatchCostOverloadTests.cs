using System;
using Laylua.Marshaling;

namespace Laylua.Tests;

public class DispatchCostIntVsDouble
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(double value)
    {
        return $"double:{value}";
    }
}

public class DispatchCostIntVsObject
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(object value)
    {
        return $"object:{value}";
    }
}

public class DispatchCostStringVsNumericParse
{
    public string Resolve(string value)
    {
        return $"string:{value}";
    }

    public string Resolve(int value)
    {
        return $"int:{value}";
    }
}

public class DispatchCostFunctionRefVsDelegate
{
    public string Resolve(LuaFunction value)
    {
        return "luafunction";
    }

    public string Resolve(Action value)
    {
        value();
        return "action";
    }
}

public class DispatchCostTableRefVsMaterialized
{
    public string Resolve(LuaTable value)
    {
        return "luatable";
    }

    public string Resolve(ImplicitConversionTarget value)
    {
        return $"materialized:{value.Name}:{value.Value}";
    }
}

public class DispatchCostWideningVsStringVsObject
{
    public string Resolve(double value)
    {
        return $"double:{value}";
    }

    public string Resolve(string value)
    {
        return $"string:{value}";
    }

    public string Resolve(object value)
    {
        return $"object:{value}";
    }
}

public class DispatchCostIntVsEnum
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }
}

public class DispatchCostStringVsCharVsEnum
{
    public string Resolve(string value)
    {
        return $"string:{value}";
    }

    public string Resolve(char value)
    {
        return $"char:{value}";
    }

    public string Resolve(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }
}

public class DispatchCostByteVsLong
{
    public string Resolve(byte value)
    {
        return $"byte:{value}";
    }

    public string Resolve(long value)
    {
        return $"long:{value}";
    }
}

public class DispatchCostFloatToIntVsDouble
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(double value)
    {
        return $"double:{value}";
    }
}

public class DispatchCostIntOnly
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(bool value)
    {
        return $"bool:{value}";
    }
}

public class DispatchCostOverloadTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        UnregisterTestTypes();
    }

    public override void TearDown()
    {
        UnregisterTestTypes();
        base.TearDown();
    }

    private static void UnregisterTestTypes()
    {
        LuaTypeRegistry.Unregister<DispatchCostIntVsDouble>();
        LuaTypeRegistry.Unregister<DispatchCostIntVsObject>();
        LuaTypeRegistry.Unregister<DispatchCostStringVsNumericParse>();
        LuaTypeRegistry.Unregister<DispatchCostFunctionRefVsDelegate>();
        LuaTypeRegistry.Unregister<DispatchCostTableRefVsMaterialized>();
        LuaTypeRegistry.Unregister<DispatchCostWideningVsStringVsObject>();
        LuaTypeRegistry.Unregister<DispatchCostIntVsEnum>();
        LuaTypeRegistry.Unregister<DispatchCostStringVsCharVsEnum>();
        LuaTypeRegistry.Unregister<DispatchCostByteVsLong>();
        LuaTypeRegistry.Unregister<DispatchCostFloatToIntVsDouble>();
        LuaTypeRegistry.Unregister<DispatchCostIntOnly>();
        LuaTypeRegistry.Unregister<ImplicitConversionTarget>();
    }

    [Test]
    public void IntVsDouble_IntegerInput_SelectsIntOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostIntVsDouble>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostIntVsDouble();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void IntVsObject_IntegerInput_SelectsIntOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostIntVsObject>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostIntVsObject();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void StringVsNumericParse_StringInput_SelectsStringOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostStringVsNumericParse>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostStringVsNumericParse();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve('42')");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void StringVsNumericParse_IntegerInput_SelectsIntOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostStringVsNumericParse>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostStringVsNumericParse();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void WideningVsStringVsObject_IntegerInput_SelectsDoubleOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostWideningVsStringVsObject>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostWideningVsStringVsObject();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(42)");

        // Assert
        Assert.That(result, Is.EqualTo("double:42"));
    }

    [Test]
    public void FunctionRefVsDelegate_FunctionInput_SelectsLuaFunctionOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostFunctionRefVsDelegate>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostFunctionRefVsDelegate();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(function() end)");

        // Assert
        Assert.That(result, Is.EqualTo("luafunction"));
    }

    [Test]
    public void TableRefVsMaterialized_TableInput_SelectsLuaTableOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>();

        LuaTypeRegistry.ConfigureDynamic<DispatchCostTableRefVsMaterialized>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostTableRefVsMaterialized();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve({Name = 'test', Value = 1})");

        // Assert
        Assert.That(result, Is.EqualTo("luatable"));
    }

    [Test]
    public void IntVsEnum_IntegerInput_SelectsIntOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostIntVsEnum>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostIntVsEnum();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(1)");

        // Assert
        Assert.That(result, Is.EqualTo("int:1"));
    }

    [Test]
    public void ByteVsLong_InRangeInput_SelectsByte()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostByteVsLong>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        Lua["obj"] = new DispatchCostByteVsLong();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(5)");

        // Assert
        Assert.That(result, Is.EqualTo("byte:5"));
    }

    [Test]
    public void ByteVsLong_OutOfRangeInput_SelectsLong()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostByteVsLong>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        Lua["obj"] = new DispatchCostByteVsLong();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(300)");

        // Assert
        Assert.That(result, Is.EqualTo("long:300"));
    }

    [Test]
    public void IntVsDouble_FloatInput_SelectsDoubleOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostFloatToIntVsDouble>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostFloatToIntVsDouble();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(3.14)");

        // Assert
        Assert.That(result, Is.EqualTo("double:3.14"));
    }

    [Test]
    public void IntVsObject_IntegerInput_ObjectFallbackDisabled_SelectsInt()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostIntVsObject>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All & ~LuaAllowedValueConversions.ObjectFallback;
        });

        Lua["obj"] = new DispatchCostIntVsObject();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void StringVsNumericParse_StringInput_StringToPrimitiveDisabled_SelectsStringOnly()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostStringVsNumericParse>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All & ~LuaAllowedValueConversions.StringToPrimitive;
        });

        Lua["obj"] = new DispatchCostStringVsNumericParse();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve('42')");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void StringVsCharVsEnum_SingleCharString_SelectsStringOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostStringVsCharVsEnum>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostStringVsCharVsEnum();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve('A')");

        // Assert
        Assert.That(result, Is.EqualTo("string:A"));
    }

    [Test]
    public void StringVsCharVsEnum_EnumNameString_SelectsStringOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostStringVsCharVsEnum>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostStringVsCharVsEnum();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve('Active')");

        // Assert
        Assert.That(result, Is.EqualTo("string:Active"));
    }

    [Test]
    public void IntOnly_TableInput_RaisesNoOverloadError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DispatchCostIntOnly>(static options =>
        {
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["obj"] = new DispatchCostIntOnly();

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Resolve({})"));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("no overload accepts 1 argument"));
    }
}
