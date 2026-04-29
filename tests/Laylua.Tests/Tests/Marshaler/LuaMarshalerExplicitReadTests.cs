using System;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua.Tests;

public unsafe class LuaMarshalerExplicitReadTests : LuaTestBase
{
    private static readonly LuaCFunction FakeDelegateHandleClosure = static _ =>
    {
        return 0;
    };

    [Test]
    public void TryGetValue_StringToInt_WithStringToPrimitive_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToPrimitive, out int value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TryGetValue_StringToInt_WithoutStringToPrimitive_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out int _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_BoolToString_WithPrimitiveToString_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.PrimitiveToString, out string? value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("true"));
    }

    [Test]
    public void TryGetValue_BoolToString_WithoutPrimitiveToString_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out string? _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_IntegerToString_WithPrimitiveToString_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.PrimitiveToString, out string? value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("42"));
    }

    [Test]
    public void TryGetValue_IntegerToString_WithoutPrimitiveToString_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out string? _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_DoubleToString_WithPrimitiveToString_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 3.14);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.PrimitiveToString, out string? value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void TryGetValue_DecimalBoundaryDouble_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, (double) decimal.MaxValue);

        var result = false;

        // Act & Assert
        Assert.That(() => result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out decimal _), Throws.Nothing);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_SingleCharString_WithStringToChar_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("A");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToChar, out char value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo('A'));
    }

    [Test]
    public void TryGetValue_SingleCharString_WithoutStringToChar_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("A");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out char _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_MultiCharString_WithStringToChar_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("AB");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToChar, out char _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_EmptyString_WithStringToChar_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToChar, out char _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_StringToEnum_ValidName_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToEnum, out ImplicitConversionStatus value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(ImplicitConversionStatus.Beta));
    }

    [Test]
    public void TryGetValue_StringToEnum_InvalidName_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Yellow");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToEnum, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_StringToEnum_WrongCase_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("beta");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToEnum, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_StringToEnum_WithoutStringToEnum_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_IntegerToEnum_DefinedValue_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(1);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.NumberToEnum, out ImplicitConversionStatus value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(ImplicitConversionStatus.Alpha));
    }

    [Test]
    public void TryGetValue_IntegerToEnum_UndefinedValue_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(99);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.NumberToEnum, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_IntegerToEnum_WithoutNumberToEnum_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(1);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_FlagsEnum_CompositeNumericValue_Fails()
    {
        // Arrange - 3 = Read | Write
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(3);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.NumberToEnum, out ImplicitConversionFlags _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_FlagsEnum_CompositeName_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Read, Write");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToEnum, out ImplicitConversionFlags _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_FlagsEnum_SingleDefinedName_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Read");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.StringToEnum, out ImplicitConversionFlags value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(ImplicitConversionFlags.Read));
    }

    [Test]
    public void TryGetValue_StringToLuaString_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("hello");

        // Act
        var result = Lua.Stack[1].TryGetValue(out LuaString value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void TryGetValue_LightUserDataToIntPtr_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushlightuserdata(L, ((IntPtr) 0x1234).ToPointer());

        // Act
        var result = Lua.Stack[1].TryGetValue(out IntPtr value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo((IntPtr) 0x1234));
    }

    [Test]
    public void TryGetValue_LightUserDataToUIntPtr_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushlightuserdata(L, ((IntPtr) 0x1234).ToPointer());

        // Act
        var result = Lua.Stack[1].TryGetValue(out UIntPtr value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo((UIntPtr) 0x1234));
    }

    [Test]
    public void TryGetValue_DefaultRules_StringToPrimitive_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var result = Lua.Stack[1].TryGetValue(out int value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TryGetValue_IntegerToByte_OutOfRange_WithNarrowingEnabled_Wraps()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(256);

        // Act
        var result = Lua.Stack[1].TryGetValue(out byte value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(unchecked((byte) 256)));
    }

    [Test]
    public void TryGetValue_IntegerToByte_OutOfRange_WithOverflowDisabled_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(256);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.AllowOverflow, out byte _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_NumberToInt_NonIntegral_Default_Truncates()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 1.5);

        // Act
        var result = Lua.Stack[1].TryGetValue(out int value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void TryGetValue_NumberToInt_NonIntegral_WithTruncateFractionDisabled_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 1.5);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.TruncateFraction, out int _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_NumberToUInt64_AboveRange_WithOverflowDisabled_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 1e25);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.AllowOverflow, out ulong _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_NumberToUInt32_Negative_WithOverflowDisabled_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, -1.0);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.AllowOverflow, out uint _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_IntegerToUInt64_NegativeRoundtrip_WithOverflow_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(-1L);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.AllowOverflow, out ulong value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void TryGetValue_NumberToUInt32_Negative_Default_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, -1.0);

        // Act
        var result = Lua.Stack[1].TryGetValue(out uint _);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void PushValue_UInt64AboveInt64Max_RoundTripsWithoutWrap()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        const ulong value = (ulong) long.MaxValue + 1;

        // Act
        Lua.Stack.Push(value);
        var result = Lua.Stack[1].GetValue<ulong>();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void PushValue_UInt64Max_RoundTripsWithoutWrap()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        const ulong value = ulong.MaxValue;

        // Act
        Lua.Stack.Push(value);
        var result = Lua.Stack[1].GetValue<ulong>();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void GetMarshalingCompatibility_StringOutOfRangeForUInt16_IsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("70000");

        // Act
        var compatibility = Marshaler.GetMarshalingCompatibility(Lua, 1, typeof(ushort));

        // Assert
        Assert.That(compatibility.IsCompatible, Is.False);
    }

    [Test]
    public void TryGetValue_DefaultRules_BoolToString_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var result = Lua.Stack[1].TryGetValue(out string? value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("true"));
    }

    [Test]
    public void TryGetValue_DefaultRules_StringToEnum_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var result = Lua.Stack[1].TryGetValue(out ImplicitConversionStatus value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(ImplicitConversionStatus.Beta));
    }

    [Test]
    public void TryGetValue_DefaultRules_NumberToUndefinedEnum_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(3);

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.NumberToEnum, out ImplicitConversionStatus _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetValue_FunctionToCallableTarget_WithPlainLuaFunction_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Execute("callback = function() end");
        lua_getglobal(L, "callback");

        // Act
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.FunctionToCallableTarget, out Action? callback);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(callback, Is.Null);
    }

    [Test]
    public void TryGetValue_FunctionToCallableTarget_WithManagedDelegateRoundTrip_ReturnsOriginalDelegate()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        var invocationCount = 0;
        Action callback = () =>
        {
            invocationCount++;
        };

        Lua.SetGlobal("callback", callback);
        Lua.Execute("roundTrip = callback");
        lua_getglobal(L, "roundTrip");

        // Act
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.FunctionToCallableTarget, out Action? roundTrip);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(roundTrip, Is.SameAs(callback));

        roundTrip!();
        Assert.That(invocationCount, Is.EqualTo(1));
    }

    [Test]
    public void TryGetValue_FunctionToCallableTarget_WithNonDelegateCClosureUsingDelegateHandle_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Action callback = static () => { };

        Lua.SetGlobal("callback", callback);
        lua_getglobal(L, "callback");
        Assert.That((nint) lua_getupvalue(L, -1, 1).Pointer, Is.Not.EqualTo(0));
        lua_remove(L, -2);
        lua_pushcclosure(L, FakeDelegateHandleClosure, 1);

        // Act
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.FunctionToCallableTarget, out Action? roundTrip);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(roundTrip, Is.Null);
    }

    [Test]
    public void TryGetValue_FunctionToCallableTarget_WithoutFunctionToCallableTarget_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Execute("callback = function() end");
        lua_getglobal(L, "callback");

        // Act
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.None, out Action? callback);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(callback, Is.Null);
    }

    [Test]
    public void TryGetValue_ObjectFallback_WithObjectFallback_Succeeds()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("hello");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.ObjectFallback, out object? value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public void TryGetValue_ObjectFallback_WithoutObjectFallback_Fails()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("hello");

        // Act
        var result = Lua.Stack[1].TryGetValue(LuaAllowedValueConversions.None, out object? value);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }
}
