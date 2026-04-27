using System;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua.Tests;

public unsafe class LuaTypeServicesTests : LuaTestBase
{
    private sealed class MissingPolicyFlagsMarshaler : DefaultLuaMarshaler
    {
        public override LuaMarshalingCompatibility GetMarshalingCompatibility(LuaThread thread, int stackIndex, Type targetType)
        {
            if (targetType == typeof(int))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.StringToPrimitive);
            }

            if (targetType == typeof(object))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.ObjectFallback);
            }

            return LuaMarshalingCompatibility.Incompatible;
        }
    }

    private static int GetCompatibilityCost(
        Lua lua,
        Type targetType,
        LuaAllowedValueConversions allowedValueConversions,
        bool validateNumericRange = false)
    {
        using var thread = LuaThread.FromExtraSpace(lua.State.L);

        return LuaTypeServices.GetImplicitCompatibilityCost(thread, 1, targetType, allowedValueConversions, validateNumericRange);
    }

    private static bool IsCompatible(
        Lua lua,
        Type targetType,
        LuaAllowedValueConversions allowedValueConversions)
    {
        using var thread = LuaThread.FromExtraSpace(lua.State.L);

        return LuaTypeServices.IsImplicitlyCompatible(thread, 1, targetType, allowedValueConversions);
    }

    [Test]
    public void NewUserData_Always_AllocatesTargetHandleAndSentinelSlotsOnly()
    {
        // Act
        var target = new object();
        var userData = LuaTypeServices.NewUserData(L, target);

        try
        {
            var hasUserData = userData != null;
            var size = lua_rawlen(L, -1);

            // Assert
            Assert.That(hasUserData, Is.True);
            Assert.That(size, Is.EqualTo((ulong) (sizeof(nint) * 2)));
        }
        finally
        {
            LuaTypeServices.TryFreeUserData(userData);
            lua_pop(L, 1);
        }
    }
    [Test]
    public void GetImplicitCompatibilityCost_StringToPrimitive_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(int), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_StringToPrimitive_EnabledPolicy_ReturnsPositiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(int),
            LuaAllowedValueConversions.Default | LuaAllowedValueConversions.StringToPrimitive);

        // Assert
        Assert.That(cost, Is.GreaterThan(0));
    }

    [Test]
    public void GetImplicitCompatibilityCost_ObjectFallback_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(object), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_ObjectFallback_EnabledPolicy_ReturnsHighCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(object), LuaAllowedValueConversions.ObjectFallback);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ObjectFallback));
    }

    [Test]
    public void GetImplicitCompatibilityCost_CustomMarshalerStringParseKindWithoutFlags_DisabledPolicyReturnsIncompatible()
    {
        // Arrange
        using var lua = CreateLua(marshaler: new MissingPolicyFlagsMarshaler());
        using var _ = lua.Stack.SnapshotCount();
        lua.Stack.Push("42");

        // Act
        var cost = GetCompatibilityCost(lua, typeof(int), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_CustomMarshalerObjectFallbackKindWithoutFlags_DisabledPolicyReturnsIncompatible()
    {
        // Arrange
        using var lua = CreateLua(marshaler: new MissingPolicyFlagsMarshaler());
        using var _ = lua.Stack.SnapshotCount();
        lua.Stack.Push(true);

        // Act
        var cost = GetCompatibilityCost(lua, typeof(object), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_BoolToBool_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(bool), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }

    [Test]
    public void GetImplicitCompatibilityCost_IntegerToInt_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(int), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }

    [Test]
    public void GetImplicitCompatibilityCost_StringToString_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("hello");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }
    [Test]
    public void GetImplicitCompatibilityCost_LightUserDataToIntPtr_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushlightuserdata(L, (void*) (nint) 0x1234);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(IntPtr), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }

    [Test]
    public void GetImplicitCompatibilityCost_LightUserDataToUIntPtr_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushlightuserdata(L, (void*) (nint) 0x1234);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(UIntPtr), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }
    [Test]
    public void GetImplicitCompatibilityCost_StringToLuaString_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("hello");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(LuaString), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }
    [Test]
    public void GetImplicitCompatibilityCost_BoolToString_EnabledPolicy_ReturnsPrimitiveToStringCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.PrimitiveToString);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.PrimitiveToString));
    }

    [Test]
    public void GetImplicitCompatibilityCost_BoolToString_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_NumberToString_EnabledPolicy_ReturnsPrimitiveToStringCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.PrimitiveToString);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.PrimitiveToString));
    }

    [Test]
    public void GetImplicitCompatibilityCost_NumberToString_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }
    [Test]
    public void GetImplicitCompatibilityCost_SingleCharStringToChar_EnabledPolicy_ReturnsStringToCharCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("A");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(char), LuaAllowedValueConversions.StringToChar);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.StringToChar));
    }

    [Test]
    public void GetImplicitCompatibilityCost_SingleCharStringToChar_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("A");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(char), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_MultiCharStringToChar_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("AB");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(char), LuaAllowedValueConversions.StringToChar);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }
    [Test]
    public void GetImplicitCompatibilityCost_StringToEnum_ValidName_EnabledPolicy_ReturnsStringToEnumCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.StringToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.StringToEnum));
    }

    [Test]
    public void GetImplicitCompatibilityCost_StringToEnum_InvalidName_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Yellow");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.StringToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_StringToEnum_WrongCase_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("beta");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.StringToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_StringToEnum_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_NumberToEnum_DefinedValue_EnabledPolicy_ReturnsNumberToEnumCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(1);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.NumberToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.NumberToEnum));
    }

    [Test]
    public void GetImplicitCompatibilityCost_NumberToEnum_UndefinedValue_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(99);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.NumberToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_NumberToEnum_DisabledPolicy_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(1);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_FlagsEnum_CompositeNumericValue_ReturnsIncompatible()
    {
        // Arrange
        // 3 = Read | Write, a composite value
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(3);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(ImplicitConversionFlags), LuaAllowedValueConversions.NumberToEnum);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }
    [Test]
    public void GetImplicitCompatibilityCost_OutOfRangeIntegerToByte_WithRangeValidation_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(300);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(byte), LuaAllowedValueConversions.None, validateNumericRange: true);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_InRangeIntegerToByte_WithRangeValidation_ReturnsPositiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(byte), LuaAllowedValueConversions.None, validateNumericRange: true);

        // Assert
        Assert.That(cost, Is.GreaterThan(0));
    }

    [Test]
    public void GetImplicitCompatibilityCost_OutOfRangeStringToByteWithValidation_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("300");

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(byte), LuaAllowedValueConversions.StringToPrimitive, validateNumericRange: true);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }
    [Test]
    public void GetImplicitCompatibilityCost_ExactMatch_LowerCost_ThanAdaptedMatch()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("42");

        // Act
        var exactCost = GetCompatibilityCost(Lua, typeof(string), LuaAllowedValueConversions.None);
        var adaptedCost = GetCompatibilityCost(Lua, typeof(int),
            LuaAllowedValueConversions.StringToPrimitive);

        // Assert
        Assert.That(exactCost, Is.LessThan(adaptedCost));
    }
    [Test]
    public void IsImplicitlyCompatible_CompatibleValue_ReturnsTrue()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(42);

        // Act
        var result = IsCompatible(Lua, typeof(int), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsImplicitlyCompatible_DisallowedConversion_ReturnsFalse()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push(true);

        // Act
        var result = IsCompatible(Lua, typeof(string), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsImplicitlyCompatible_DisallowedStringToEnum_ReturnsFalse()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("Beta");

        // Act
        var compatible = IsCompatible(Lua, typeof(ImplicitConversionStatus), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(compatible, Is.False);
    }
    [Test]
    public void GetImplicitCompatibilityCost_FloatToDecimal_ReturnsWideningCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 3.14);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(decimal), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.NumericWidening));
    }

    [Test]
    public void GetImplicitCompatibilityCost_DecimalBoundaryDouble_WithRangeValidation_ReturnsIncompatible()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, (double) decimal.MaxValue);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(decimal), LuaAllowedValueConversions.None, validateNumericRange: true);

        // Assert
        Assert.That(cost, Is.EqualTo(-1));
    }

    [Test]
    public void GetImplicitCompatibilityCost_FloatToDouble_ReturnsExactPrimitiveCost()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        lua_pushnumber(L, 3.14);

        // Act
        var cost = GetCompatibilityCost(Lua, typeof(double), LuaAllowedValueConversions.None);

        // Assert
        Assert.That(cost, Is.EqualTo((int) LuaMarshalingCompatibilityKind.ExactPrimitive));
    }
}
