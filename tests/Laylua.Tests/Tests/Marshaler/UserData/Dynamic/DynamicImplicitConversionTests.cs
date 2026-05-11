using System;
using System.Globalization;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua.Tests;

public class ImplicitConversionTarget
{
    public string Name { get; set; } = "";

    public int Value { get; set; }

    public ImplicitConversionTarget()
    { }

    public ImplicitConversionTarget(string name, int value)
    {
        Name = name;
        Value = value;
    }
}

public class ImplicitConversionOverloaded
{
    public string Dispatch(string text)
    {
        return $"string:{text}";
    }

    public string Dispatch(int number)
    {
        return $"int:{number}";
    }

    public string Dispatch(object fallback)
    {
        return $"object:{fallback}";
    }

    public string NumericOnly(int number)
    {
        return $"int:{number}";
    }

    public string NumericOnly(double number)
    {
        return $"double:{number}";
    }

    public string ByteOnly(byte number)
    {
        return $"byte:{number}";
    }

    public string NarrowOrWide(byte number)
    {
        return $"byte:{number}";
    }

    public string NarrowOrWide(long number)
    {
        return $"long:{number}";
    }

    public string AcceptsDelegate(Action callback)
    {
        callback();
        return "delegate-called";
    }

    public string AcceptsTarget(ImplicitConversionTarget target)
    {
        return $"target:{target.Name}:{target.Value}";
    }

    public string AcceptsConstructorTarget(DynamicTableConstructorTarget target)
    {
        return $"target:{target.Name}:{target.Health}:{target.ConstructorUsed}";
    }

    public string NumericOrObject(double number)
    {
        return $"double:{number}";
    }

    public string NumericOrObject(object fallback)
    {
        return $"object:{fallback}";
    }

    public string StringOnly(string text)
    {
        return $"string:{text}";
    }

    public string LuaStringOnly(LuaString text)
    {
        return $"luastring:{text}";
    }

    public string PointerOnly(IntPtr pointer)
    {
        return $"ptr:0x{pointer:x}";
    }

    public string CharOnly(char value)
    {
        return $"char:{value}";
    }

    public string CharOrString(char value)
    {
        return $"char:{value}";
    }

    public string CharOrString(string text)
    {
        return $"string:{text}";
    }

    public string EnumOnly(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }

    public string FlagsOnly(ImplicitConversionFlags value)
    {
        return $"flags:{value}";
    }

    public string StringOrEnum(string text)
    {
        return $"string:{text}";
    }

    public string StringOrEnum(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }
}

public class ImplicitConversionConstructable(ImplicitConversionTarget target)
{
    public string Summary { get; } = $"target:{target.Name}:{target.Value}";
}

public class DynamicTableRequiredInitTarget
{
    public required string Name { get; init; }

    public int Health { get; set; }
}

public class DynamicTableConstructorTarget
{
    public string Name { get; }

    public int Health { get; set; }

    public bool ConstructorUsed { get; }

    public DynamicTableConstructorTarget()
    {
        Name = "parameterless";
    }

    public DynamicTableConstructorTarget(string name)
    {
        Name = name;
        ConstructorUsed = true;
    }
}

public class DynamicImplicitConversionTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        LuaTypeRegistry.Unregister<ImplicitConversionTarget>();
        LuaTypeRegistry.Unregister<ImplicitConversionOverloaded>();
        LuaTypeRegistry.Unregister<ImplicitConversionConstructable>();
        LuaTypeRegistry.Unregister<DynamicTableRequiredInitTarget>();
        LuaTypeRegistry.Unregister<DynamicTableConstructorTarget>();
    }

    public override void TearDown()
    {
        LuaTypeRegistry.Unregister<ImplicitConversionTarget>();
        LuaTypeRegistry.Unregister<ImplicitConversionOverloaded>();
        LuaTypeRegistry.Unregister<ImplicitConversionConstructable>();
        LuaTypeRegistry.Unregister<DynamicTableRequiredInitTarget>();
        LuaTypeRegistry.Unregister<DynamicTableConstructorTarget>();
        base.TearDown();
    }

    [Test]
    public void DefaultPolicy_Table_RemainsBehindObjectFallbackDuringOverloadResolution()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch({Name = 'test', Value = 42})");

        // Assert
        Assert.That(result, Does.StartWith("object:"));
    }

    [Test]
    public void DefaultPolicy_Table_BindsToTypedMethodParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:AcceptsTarget({Name = 'test'})");

        // Assert
        Assert.That(result, Is.EqualTo("target:test:0"));
    }

    [Test]
    public void TableToObject_OptIn_AllowsTableMaterialization_ForMethod()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>();
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:AcceptsTarget({Name = 'test', Value = 42})");

        // Assert
        Assert.That(result, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void StringToPrimitive_OptIn_AllowsStringToBindNumericParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default | LuaAllowedValueConversions.StringToPrimitive;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NumericOnly('42')");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void StringToPrimitive_Disabled_StringDoesNotBindNumericOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.StringToPrimitive;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:NumericOnly('42')"));
    }

    [Test]
    public void BestMatch_StringToPrimitive_ExactStringOverloadWinsOverParsedNumeric()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch('42')");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void StringToPrimitive_OptIn_InvalidString_DoesNotBindNumericParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:NumericOnly('abc')"));
    }

    [Test]
    public void StringToPrimitive_OptIn_OutOfRangeString_RejectsNarrowNumericTarget()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:ByteOnly('300')"));
    }

    [Test]
    public void StringToPrimitive_OptIn_UsesFormatProvider_ForCompatibilitySelection()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            // Act
            var result = Lua.Evaluate<string>("return obj:NumericOrObject('42.0')");

            // Assert
            Assert.That(result, Is.EqualTo("double:42"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Test]
    public void BestMatch_OutOfRangeIntegralValue_WiderIntegralOverloadWins()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NarrowOrWide(300)");

        // Assert
        Assert.That(result, Is.EqualTo("long:300"));
    }

    [Test]
    public void BestMatch_StringToPrimitive_OutOfRangeNarrowIntegralOverload_WiderIntegralOverloadWins()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NarrowOrWide('300')");

        // Assert
        Assert.That(result, Is.EqualTo("long:300"));
    }

    [Test]
    public void FunctionToCallableTarget_Default_AllowsManagedDelegateRoundTrip()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        var invocationCount = 0;
        Action callback = () =>
        {
            invocationCount++;
        };

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;
        Lua["callback"] = callback;

        // Act
        var result = Lua.Evaluate<string>("return obj:AcceptsDelegate(callback)");

        // Assert
        Assert.That(result, Is.EqualTo("delegate-called"));
        Assert.That(invocationCount, Is.EqualTo(1));
    }

    [Test]
    public void ObjectFallback_LowerPriority_ThanSpecificOverloads()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void DefaultPolicy_Table_BindsToTypedConstructorParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
        });

        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionConstructable>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.Default;
            options.ArgumentValidation = LuaArgumentValidation.Strict;
        });

        Lua["ImplicitConversionConstructable"] = typeof(ImplicitConversionConstructable);

        // Act
        var result = Lua.Evaluate<ImplicitConversionConstructable>("return ImplicitConversionConstructable({Name = 'test', Value = 42})");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Summary, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void TableToObject_OptIn_AllowsTableConstructor()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionTarget>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionConstructable>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
        });

        Lua["ImplicitConversionConstructable"] = typeof(ImplicitConversionConstructable);

        // Act
        var result = Lua.Evaluate<ImplicitConversionConstructable>("return ImplicitConversionConstructable({Name = 'test', Value = 42})");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Summary, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void FunctionToCallableTarget_Disabled_ManagedDelegateDoesNotBindDelegate()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;
        Lua["callback"] = (Action) (static () => { });

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:AcceptsDelegate(callback)"));
    }

    [Test]
    public void ObjectFallback_Disabled_UnmatchedValueDoesNotBindObjectOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:Dispatch(true)"));
    }

    [Test]
    public void PrimitiveToString_Enabled_NumberBindsToStringParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.PrimitiveToString;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:StringOnly(42)");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void PrimitiveToString_Enabled_BooleanBindsToStringParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.PrimitiveToString;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:StringOnly(true)");

        // Assert
        Assert.That(result, Is.EqualTo("string:true"));
    }

    [Test]
    public void PrimitiveToString_Disabled_NumberDoesNotBindStringParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:StringOnly(42)"));
    }

    [Test]
    public void LuaString_DirectMatch_BindsWithoutImplicitConversionFlag()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:LuaStringOnly('barker')");

        // Assert
        Assert.That(result, Is.EqualTo("luastring:barker"));
    }

    [Test]
    public void LightUserData_DirectMatch_BindsPointerParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;
        SetGlobalLightUserData("pointer", 0x1234);

        // Act
        var result = Lua.Evaluate<string>("return obj:PointerOnly(pointer)");

        // Assert
        Assert.That(result, Is.EqualTo("ptr:0x1234"));
    }

    [Test]
    public void StringToChar_Enabled_SingleCharacterStringBindsCharParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToChar;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:CharOnly('A')");

        // Assert
        Assert.That(result, Is.EqualTo("char:A"));
    }

    [Test]
    public void StringToChar_Disabled_SingleCharacterStringDoesNotBindCharParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.None;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:CharOnly('A')"));
    }

    [Test]
    public void StringToChar_Enabled_MultiCharacterStringDoesNotBindCharParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToChar;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:CharOnly('Alex')"));
    }

    [Test]
    public void StringToChar_Enabled_ExactStringOverloadBeatsCharOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToChar;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:CharOrString('A')");

        // Assert
        Assert.That(result, Is.EqualTo("string:A"));
    }

    [Test]
    public void StringToEnum_Enabled_ExactNameBindsEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:EnumOnly('Beta')");

        // Assert
        Assert.That(result, Is.EqualTo("enum:Beta"));
    }

    [Test]
    public void StringToEnum_Enabled_WrongCaseDoesNotBindEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:EnumOnly('beta')"));
    }

    [Test]
    public void NumberToEnum_Enabled_DefinedValueBindsEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.NumberToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:EnumOnly(2)");

        // Assert
        Assert.That(result, Is.EqualTo("enum:Beta"));
    }

    [Test]
    public void NumberToEnum_Enabled_UndefinedValueDoesNotBindEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.NumberToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:EnumOnly(3)"));
    }

    [Test]
    public void EnumBinding_Enabled_ExactStringOverloadBeatsEnumOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:StringOrEnum('Alpha')");

        // Assert
        Assert.That(result, Is.EqualTo("string:Alpha"));
    }

    [Test]
    public void StringToEnum_Enabled_FlagsCompositeNameDoesNotBindEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.StringToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:FlagsOnly('Read, Write')"));
    }

    [Test]
    public void NumberToEnum_Enabled_FlagsCompositeValueDoesNotBindEnumParameter()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ImplicitConversionOverloaded>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.NumberToEnum;
        });

        var obj = new ImplicitConversionOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:FlagsOnly(3)"));
    }

    private unsafe void SetGlobalLightUserData(string name, IntPtr value)
    {
        lua_pushlightuserdata(L, value.ToPointer());
        lua_setglobal(L, name);
    }
}
