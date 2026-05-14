using System;
using System.Globalization;

namespace Laylua.Tests;

public class TypedImplicitConversionTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void DefaultPolicy_Table_RemainsBehindObjectFallbackDuringOverloadResolution()
    {
        // Arrange
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch({ Name = 'test', Value = 42 })");

        // Assert
        Assert.That(result, Does.StartWith("object:"));
    }

    [Test]
    public void DefaultPolicy_Table_BindsToTypedMethodParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:AcceptsTarget({ Name = 'test', Value = 42 })");

        // Assert
        Assert.That(result, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void TableToObject_OptIn_AllowsTableMaterialization_ForMethod()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:AcceptsTarget({ Name = 'test', Value = 42 })");

        // Assert
        Assert.That(result, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void TableToObject_OptIn_SelectedMethodArgumentConversionFailure_RaisesDetailedLuaError()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var exception = Assert.Throws<LuaException>(() =>
            Lua.Execute("obj:AcceptsTarget({ Name = { }, Value = 42 })"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("AcceptsTarget"));
            Assert.That(exception.Message, Does.Contain("argument 'target'"));
            Assert.That(exception.Message, Does.Contain("$.Name"));
            Assert.That(exception.Message, Does.Contain("failed to convert member 'Name'"));
        });
    }

    [Test]
    public void DefaultPolicy_Table_BindsToTypedConstructorParameter()
    {
        // Arrange
        Lua["TypedImplicitConversionConstructableDefault"] = typeof(TypedImplicitConversionConstructableDefault);

        // Act
        var result = Lua.Evaluate<TypedImplicitConversionConstructableDefault>("return TypedImplicitConversionConstructableDefault({ Name = 'test', Value = 42 })");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Summary, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void TableToObject_OptIn_AllowsTableConstructor()
    {
        // Arrange
        Lua["TypedImplicitConversionConstructable"] = typeof(TypedImplicitConversionConstructable);

        // Act
        var result = Lua.Evaluate<TypedImplicitConversionConstructable>("return TypedImplicitConversionConstructable({ Name = 'test', Value = 42 })");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Summary, Is.EqualTo("target:test:42"));
    }

    [Test]
    public void StringToPrimitive_OptIn_AllowsStringToBindNumericParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NumericOnly('42')");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void DefaultPolicy_StringToPrimitive_AllowsStringToBindNumericParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NumericOnly('42')");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void StringToPrimitive_OptIn_InvalidString_DoesNotBindNumericParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:NumericOnly('abc')"));
    }

    [Test]
    public void BestMatch_OutOfRangeIntegralValue_WiderIntegralOverloadWins()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:NarrowOrWide('300')");

        // Assert
        Assert.That(result, Is.EqualTo("long:300"));
    }

    [Test]
    public void Nil_DoesNotSelectObjectFallbackOverload()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("obj:NilOrObject(nil)"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("incompatible argument types"));
    }

    [Test]
    public void StringToPrimitive_OptIn_UsesFormatProvider_ForCompatibilitySelection()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
    public void BestMatch_StringToPrimitive_ExactStringOverloadWinsOverParsedNumeric()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch('42')");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void FunctionToCallableTarget_Default_AllowsManagedDelegateRoundTrip()
    {
        // Arrange
        var invocationCount = 0;
        Action callback = () =>
        {
            invocationCount++;
        };

        var obj = new TypedImplicitConversionDefault();
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
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Dispatch(42)");

        // Assert
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void FunctionToCallableTarget_Disabled_ManagedDelegateDoesNotBindDelegate()
    {
        // Arrange
        var obj = new TypedImplicitConversionNone();
        Lua["obj"] = obj;
        Lua["callback"] = (Action) (static () => { });

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:AcceptsDelegate(callback)"));
    }

    [Test]
    public void ObjectFallback_Disabled_UnmatchedValueDoesNotBindObjectOverload()
    {
        // Arrange
        var obj = new TypedImplicitConversionNone();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:Dispatch(true)"));
    }

    [Test]
    public void PrimitiveToString_Enabled_NumberBindsToStringParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:StringOnly(true)");

        // Assert
        Assert.That(result, Is.EqualTo("string:true"));
    }

    [Test]
    public void DefaultPolicy_PrimitiveToString_AllowsNumberToBindStringParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:StringOnly(42)");

        // Assert
        Assert.That(result, Is.EqualTo("string:42"));
    }

    [Test]
    public void LuaString_DirectMatch_BindsWithoutImplicitConversionFlag()
    {
        // Arrange
        var obj = new TypedImplicitConversionNone();
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
        var obj = new TypedImplicitConversionNone();
        Lua["obj"] = obj;
        SetGlobalLightUserData("pointer", (IntPtr) 0x1234);

        // Act
        var result = Lua.Evaluate<string>("return obj:PointerOnly(pointer)");

        // Assert
        Assert.That(result, Is.EqualTo("ptr:0x1234"));
    }

    [Test]
    public void StringToChar_Enabled_SingleCharacterStringBindsCharParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:CharOnly('A')");

        // Assert
        Assert.That(result, Is.EqualTo("char:A"));
    }

    [Test]
    public void DefaultPolicy_StringToChar_AllowsSingleCharStringToBindCharParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionDefault();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:CharOnly('A')");

        // Assert
        Assert.That(result, Is.EqualTo("char:A"));
    }

    [Test]
    public void StringToChar_Enabled_MultiCharacterStringDoesNotBindCharParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:CharOnly('Alex')"));
    }

    [Test]
    public void StringToChar_Enabled_ExactStringOverloadBeatsCharOverload()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:EnumOnly('beta')"));
    }

    [Test]
    public void NumberToEnum_Enabled_DefinedValueBindsEnumParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:EnumOnly(3)"));
    }

    [Test]
    public void EnumBinding_Enabled_ExactStringOverloadBeatsEnumOverload()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
        var obj = new TypedImplicitConversionAll();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:FlagsOnly('Read, Write')"));
    }

    [Test]
    public void NumberToEnum_Enabled_FlagsCompositeValueDoesNotBindEnumParameter()
    {
        // Arrange
        var obj = new TypedImplicitConversionAll();
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
