using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    private const double TwoTo63 = 1UL << 63;
    private const double TwoTo64 = TwoTo63 * 2;

    /// <inheritdoc/>
    public override bool TryGetValue<T>(
        LuaThread thread,
        int stackIndex,
        out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error)
        where T : default
    {
        return TryGetValue(thread, stackIndex, LuaAllowedValueConversions.Default, out obj, out error);
    }

    private bool TryGetValueCore<T>(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions, out T? obj)
    {
        var L = thread.State.L;
        var luaType = lua_type(L, stackIndex);
        if (luaType == LuaType.None)
        {
            obj = default;
            return false;
        }

        if (luaType == LuaType.Nil)
        {
            obj = default;
            return default(T) == null;
        }

        var clrType = typeof(T);
        if (clrType.TryGetNullableUnderlyingType(out var nullableType))
        {
            clrType = nullableType;
        }

        switch (luaType)
        {
            case LuaType.Boolean:
            {
                var boolValue = lua_toboolean(L, stackIndex);
                if (clrType == typeof(bool) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    obj = (T) (object) boolValue;
                    return true;
                }

                if ((allowedConversions & LuaAllowedValueConversions.PrimitiveToString) != 0 && clrType == typeof(string))
                {
                    obj = (T) (object) (boolValue ? "true" : "false");
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.LightUserData:
            {
                if (clrType == typeof(IntPtr) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    obj = (T) (object) (IntPtr) lua_touserdata(L, stackIndex);
                    return true;
                }

                if (clrType == typeof(UIntPtr))
                {
                    obj = (T) (object) (UIntPtr) lua_touserdata(L, stackIndex);
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.Number:
            {
                if ((allowedConversions & LuaAllowedValueConversions.NumberToEnum) != 0
                    && clrType.IsEnum
                    && LuaTypeServices.TryGetDefinedEnumValue(L, stackIndex, clrType, out var enumValue))
                {
                    obj = (T) enumValue!;
                    return true;
                }

                if (lua_isinteger(L, stackIndex))
                {
                    var longValue = lua_tointeger(L, stackIndex);
                    if (TryConvertIntegerValue(longValue, clrType, allowedConversions, out var numericResult))
                    {
                        obj = (T) numericResult!;
                        return true;
                    }

                    if ((allowedConversions & LuaAllowedValueConversions.PrimitiveToString) != 0 && clrType == typeof(string))
                    {
                        obj = (T) (object) longValue.ToString(FormatProvider);
                        return true;
                    }
                }
                else
                {
                    var doubleValue = lua_tonumber(L, stackIndex);
                    if (TryConvertNumberValue(doubleValue, clrType, allowedConversions, out var numericResult))
                    {
                        obj = (T) numericResult!;
                        return true;
                    }

                    if ((allowedConversions & LuaAllowedValueConversions.PrimitiveToString) != 0 && clrType == typeof(string))
                    {
                        obj = (T) (object) doubleValue.ToString(FormatProvider);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.String:
            {
                var nativeStringValue = lua_tostring(L, stackIndex);
                if (clrType == typeof(string) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    obj = (T?) (object?) nativeStringValue.ToString();
                    return true;
                }

                if (clrType == typeof(LuaString))
                {
                    obj = (T) (object) nativeStringValue;
                    return true;
                }

                var charCount = nativeStringValue.CharLength;

                if ((allowedConversions & LuaAllowedValueConversions.StringToChar) != 0 && clrType == typeof(char))
                {
                    if (LuaTypeServices.TryGetCharacter(nativeStringValue, out var charValue))
                    {
                        obj = (T) (object) charValue;
                        return true;
                    }
                }

                if ((allowedConversions & LuaAllowedValueConversions.StringToEnum) != 0 && clrType.IsEnum)
                {
                    if (LuaTypeServices.TryGetDefinedEnumValue(clrType, nativeStringValue, out var enumValue))
                    {
                        obj = (T) enumValue!;
                        return true;
                    }
                }

                if ((allowedConversions & LuaAllowedValueConversions.StringToPrimitive) != 0
                    && (clrType == typeof(bool) || LuaTypeServices.IsNumericType(clrType)))
                {
                    char[]? rentedArray = null;
                    scoped var charSpan = charCount > 256
                        ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                        : stackalloc char[charCount];

                    try
                    {
                        nativeStringValue.GetChars(charSpan);

                        if (clrType == typeof(bool))
                        {
                            if (bool.TryParse(charSpan, out var boolValue))
                            {
                                obj = (T) (object) boolValue;
                                return true;
                            }
                        }
                        else if (TryParseNumericValue(charSpan, clrType, out var numericResult))
                        {
                            obj = (T) numericResult!;
                            return true;
                        }

                        obj = default;
                        return false;
                    }
                    finally
                    {
                        if (rentedArray != null)
                        {
                            ArrayPool<char>.Shared.Return(rentedArray);
                        }
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Table:
            {
                return TryGetTableValue(thread, stackIndex, clrType, allowedConversions, out obj, out _);
            }
            case LuaType.Function:
            {
                if ((allowedConversions & LuaAllowedValueConversions.FunctionToCallableTarget) != 0
                    && (clrType == typeof(MethodInfo) || clrType.BaseType == typeof(MulticastDelegate))
                    && TryGetCallableTargetFromFunction(L, stackIndex, clrType, out var callableTarget))
                {
                    obj = (T) callableTarget!;
                    return true;
                }

                if (clrType.IsAssignableFrom(typeof(LuaFunction)) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    if (TryCreateFunctionReference(thread, stackIndex, out var function))
                    {
                        obj = (T) (object) function;
                        return true;
                    }
                }
                else
                {
                    if (clrType == typeof(LuaWeakReference<LuaFunction>))
                    {
                        if (LuaWeakReference.TryCreate<LuaFunction>(thread, stackIndex, out var weakReference))
                        {
                            obj = (T) (object) weakReference;
                            return true;
                        }
                    }
                    else if (clrType == typeof(LuaWeakReference<LuaReference>))
                    {
                        if (LuaWeakReference.TryCreate<LuaReference>(thread, stackIndex, out var weakReference))
                        {
                            obj = (T) (object) weakReference;
                            return true;
                        }
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.UserData:
            {
                if (AwaitableUserData.TryGetValue(L, stackIndex, out T? awaitableValue))
                {
                    obj = awaitableValue;
                    return true;
                }

                if (LuaTypeRegistry.TryGet(L, stackIndex, typeof(T), out var generatedValue))
                {
                    obj = (T) generatedValue!;
                    return true;
                }

                if (clrType.IsAssignableFrom(typeof(LuaUserData)) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    var pointer = lua_touserdata(L, stackIndex);
                    if (pointer != null && TryCreateUserDataReference(thread, stackIndex, (IntPtr) pointer, out var userData))
                    {
                        obj = (T) (object) userData;
                        return true;
                    }
                }
                else if (clrType == typeof(LuaWeakReference<LuaUserData>))
                {
                    if (LuaWeakReference.TryCreate<LuaUserData>(thread, stackIndex, out var weakReference))
                    {
                        obj = (T) (object) weakReference;
                        return true;
                    }
                }
                else if (clrType == typeof(LuaWeakReference<LuaReference>))
                {
                    if (LuaWeakReference.TryCreate<LuaReference>(thread, stackIndex, out var weakReference))
                    {
                        obj = (T) (object) weakReference;
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Thread:
            {
                if (clrType.IsAssignableFrom(typeof(LuaThread)))
                {
                    var threadPtr = lua_tothread(L, stackIndex);
                    if (threadPtr == thread.MainThread.State.L)
                    {
                        obj = clrType == typeof(Lua)
                            ? (T) (object) Lua.FromThread(thread)
                            : (T) (object) thread.MainThread;

                        return true;
                    }

                    if (TryCreateThreadReference(thread, stackIndex, threadPtr, out var createdThread))
                    {
                        obj = (T) (object) createdThread;
                        return true;
                    }
                }
                else if ((allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    var threadPtr = lua_tothread(L, stackIndex);
                    if (threadPtr == thread.MainThread.State.L)
                    {
                        obj = (T) (object) thread.MainThread;
                        return true;
                    }

                    if (TryCreateThreadReference(thread, stackIndex, threadPtr, out var createdThread))
                    {
                        obj = (T) (object) createdThread;
                        return true;
                    }
                }
                else if (clrType == typeof(LuaWeakReference<LuaThread>) || clrType == typeof(LuaWeakReference<LuaReference>))
                {
                    if (clrType == typeof(LuaWeakReference<LuaThread>))
                    {
                        if (LuaWeakReference.TryCreate<LuaThread>(thread, stackIndex, out var weakReference))
                        {
                            obj = (T) (object) weakReference;
                            return true;
                        }
                    }
                    else if (clrType == typeof(LuaWeakReference<LuaReference>))
                    {
                        if (LuaWeakReference.TryCreate<LuaReference>(thread, stackIndex, out var weakReference))
                        {
                            obj = (T) (object) weakReference;
                            return true;
                        }
                    }
                }

                obj = default;
                return false;
            }
            default:
            {
                throw new ArgumentException("Unsupported Lua type.", nameof(stackIndex));
            }
        }
    }

    /// <inheritdoc/>
    public override bool TryGetValue<T>(
        LuaThread thread,
        int stackIndex,
        LuaAllowedValueConversions allowedConversions,
        out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error)
        where T : default
    {
        var L = thread.State.L;
        var luaType = lua_type(L, stackIndex);
        var clrType = typeof(T);
        if (clrType.TryGetNullableUnderlyingType(out var nullableType))
        {
            clrType = nullableType;
        }

        if (luaType == LuaType.Table)
        {
            return TryGetTableValue(thread, stackIndex, clrType, allowedConversions, out obj, out error);
        }

        if (TryGetValueCore(thread, stackIndex, allowedConversions, out obj))
        {
            error = null;
            return true;
        }

        error = CreateConversionError(L, stackIndex, typeof(T), clrType, allowedConversions);
        return false;
    }

    private bool TryGetTableValue<T>(
        LuaThread thread,
        int stackIndex,
        Type clrType,
        LuaAllowedValueConversions allowedConversions,
        out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error)
    {
        var L = thread.State.L;
        if (clrType.IsAssignableFrom(typeof(LuaTable)) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
        {
            if (TryCreateTableReference(thread, stackIndex, out var table))
            {
                obj = (T) (object) table;
                error = null;
                return true;
            }
        }
        else if (clrType == typeof(LuaWeakReference<LuaTable>) || clrType == typeof(LuaWeakReference<LuaReference>))
        {
            if (clrType == typeof(LuaWeakReference<LuaTable>))
            {
                if (LuaWeakReference.TryCreate<LuaTable>(thread, stackIndex, out var weakReference))
                {
                    obj = (T) (object) weakReference;
                    error = null;
                    return true;
                }
            }
            else if (clrType == typeof(LuaWeakReference<LuaReference>))
            {
                if (LuaWeakReference.TryCreate<LuaReference>(thread, stackIndex, out var weakReference))
                {
                    obj = (T) (object) weakReference;
                    error = null;
                    return true;
                }
            }
        }
        else if ((allowedConversions & LuaAllowedValueConversions.TableToObject) != 0)
        {
            if (LuaTypeRegistry.TryGetFromTable<T>(L, stackIndex, out var tableValue, out error))
            {
                obj = tableValue!;
                return true;
            }

            obj = default;
            return false;
        }

        obj = default;
        if (clrType != typeof(object)
            && !clrType.IsAssignableFrom(typeof(LuaTable))
            && clrType != typeof(LuaWeakReference<LuaTable>)
            && clrType != typeof(LuaWeakReference<LuaReference>))
        {
            error = LuaConversionError.ConversionPolicyDisabled(typeof(T), LuaType.Table, "table-to-object");
            return false;
        }

        error = LuaConversionError.IncompatibleLuaType(typeof(T), LuaType.Table);
        return false;
    }

    private LuaConversionError CreateConversionError(
        lua_State* L,
        int stackIndex,
        Type targetType,
        Type clrType,
        LuaAllowedValueConversions allowedConversions)
    {
        var luaType = lua_type(L, stackIndex);
        switch (luaType)
        {
            case LuaType.None:
            {
                return LuaConversionError.InvalidStackValue(targetType);
            }
            case LuaType.Nil:
            {
                return LuaConversionError.NilCannotBeConverted(targetType);
            }
            case LuaType.Boolean:
            {
                if (clrType == typeof(string))
                {
                    return LuaConversionError.ConversionPolicyDisabled(targetType, luaType, "primitive-to-string");
                }

                return LuaConversionError.IncompatibleLuaType(targetType, luaType);
            }
            case LuaType.LightUserData:
            {
                return LuaConversionError.IncompatibleLuaType(targetType, luaType);
            }
            case LuaType.Number:
            {
                return CreateNumberConversionError(L, stackIndex, targetType, clrType, allowedConversions);
            }
            case LuaType.String:
            {
                return CreateStringConversionError(L, stackIndex, targetType, clrType, allowedConversions);
            }
            case LuaType.Function:
            {
                if (clrType == typeof(MethodInfo) || clrType.BaseType == typeof(MulticastDelegate))
                {
                    if ((allowedConversions & LuaAllowedValueConversions.FunctionToCallableTarget) == 0)
                    {
                        return LuaConversionError.ConversionPolicyDisabled(targetType, luaType, "function-to-callable");
                    }
                }

                return LuaConversionError.IncompatibleLuaType(targetType, luaType);
            }
            case LuaType.UserData:
            case LuaType.Thread:
            {
                return LuaConversionError.IncompatibleLuaType(targetType, luaType);
            }
            default:
            {
                throw new ArgumentException("Unsupported Lua type.", nameof(stackIndex));
            }
        }
    }

    private LuaConversionError CreateNumberConversionError(
        lua_State* L,
        int stackIndex,
        Type targetType,
        Type clrType,
        LuaAllowedValueConversions allowedConversions)
    {
        if (clrType.IsEnum)
        {
            if ((allowedConversions & LuaAllowedValueConversions.NumberToEnum) == 0)
            {
                return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.Number, "number-to-enum");
            }

            if (!lua_isinteger(L, stackIndex))
            {
                return LuaConversionError.EnumRequiresDefinedIntegralValue(targetType);
            }

            return LuaConversionError.UndefinedEnumValue(targetType, lua_tointeger(L, stackIndex));
        }

        if (lua_isinteger(L, stackIndex))
        {
            var integerValue = lua_tointeger(L, stackIndex);
            if (LuaTypeServices.IsIntegerType(clrType)
                && (allowedConversions & LuaAllowedValueConversions.AllowOverflow) == 0
                && !LuaTypeServices.IsIntegerValueInRange(integerValue, clrType))
            {
                return LuaConversionError.NumericValueOutOfRange(targetType);
            }

            if (clrType == typeof(float) && float.IsInfinity((float) integerValue))
            {
                return LuaConversionError.NumericValueOutOfRange(targetType);
            }

            if (clrType == typeof(string))
            {
                return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.Number, "primitive-to-string");
            }

            return LuaConversionError.IncompatibleLuaType(targetType, LuaType.Number);
        }

        var numberValue = lua_tonumber(L, stackIndex);
        if (double.IsNaN(numberValue) || double.IsInfinity(numberValue))
        {
            return LuaConversionError.NonFiniteNumberNotAllowed(targetType);
        }

        if (clrType == typeof(float) && float.IsInfinity((float) numberValue))
        {
            return LuaConversionError.NumericValueOutOfRange(targetType);
        }

        if (clrType == typeof(decimal) && !LuaTypeServices.IsDecimalValueInRange(numberValue))
        {
            return LuaConversionError.NumericValueOutOfRange(targetType);
        }

        if (LuaTypeServices.IsIntegerType(clrType))
        {
            if ((allowedConversions & LuaAllowedValueConversions.TruncateFraction) == 0 && !double.IsInteger(numberValue))
            {
                return LuaConversionError.FractionalNumberNotAllowed(targetType);
            }

            if ((allowedConversions & LuaAllowedValueConversions.AllowOverflow) == 0)
            {
                if (clrType == typeof(ulong) && numberValue is not (>= 0 and < TwoTo64))
                {
                    return LuaConversionError.NumericValueOutOfRange(targetType);
                }

                if (clrType == typeof(uint) && numberValue is not (>= uint.MinValue and <= uint.MaxValue))
                {
                    return LuaConversionError.NumericValueOutOfRange(targetType);
                }

                if (clrType != typeof(ulong)
                    && clrType != typeof(uint)
                    && numberValue is < -TwoTo63 or >= TwoTo63)
                {
                    return LuaConversionError.NumericValueOutOfRange(targetType);
                }

                if (double.IsInteger(numberValue)
                    && numberValue is >= long.MinValue and <= long.MaxValue
                    && !LuaTypeServices.IsIntegerValueInRange(unchecked((long) numberValue), clrType))
                {
                    return LuaConversionError.NumericValueOutOfRange(targetType);
                }
            }
        }

        if (clrType == typeof(string))
        {
            return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.Number, "primitive-to-string");
        }

        return LuaConversionError.IncompatibleLuaType(targetType, LuaType.Number);
    }

    private LuaConversionError CreateStringConversionError(
        lua_State* L,
        int stackIndex,
        Type targetType,
        Type clrType,
        LuaAllowedValueConversions allowedConversions)
    {
        var stringValue = lua_tostring(L, stackIndex).ToString() ?? string.Empty;
        if (clrType == typeof(char))
        {
            if ((allowedConversions & LuaAllowedValueConversions.StringToChar) == 0)
            {
                return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.String, "string-to-char");
            }

            return LuaConversionError.StringMustContainSingleCharacter(targetType);
        }

        if (clrType.IsEnum)
        {
            if ((allowedConversions & LuaAllowedValueConversions.StringToEnum) == 0)
            {
                return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.String, "string-to-enum");
            }

            return LuaConversionError.UndefinedEnumName(targetType, stringValue);
        }

        if (clrType == typeof(bool) || LuaTypeServices.IsNumericType(clrType))
        {
            if ((allowedConversions & LuaAllowedValueConversions.StringToPrimitive) == 0)
            {
                return LuaConversionError.ConversionPolicyDisabled(targetType, LuaType.String, "string-to-primitive");
            }

            if (clrType == typeof(bool))
            {
                return LuaConversionError.InvalidBooleanString(targetType, stringValue);
            }

            return LuaConversionError.InvalidNumericString(targetType, stringValue);
        }

        return LuaConversionError.IncompatibleLuaType(targetType, LuaType.String);
    }

    private static bool TryConvertIntegerValue(long value, Type clrType, LuaAllowedValueConversions allowedConversions, out object? result)
    {
        if (clrType == typeof(long) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
        {
            result = value;
            return true;
        }

        if (LuaTypeServices.IsIntegerType(clrType))
        {
            return TryConvertIntegerTarget(value, clrType, allowedConversions, out result);
        }

        if (clrType == typeof(double))
        {
            result = (double) value;
            return true;
        }

        if (clrType == typeof(float))
        {
            var floatValue = (float) value;
            if (!float.IsInfinity(floatValue))
            {
                result = floatValue;
                return true;
            }
        }
        else if (clrType == typeof(decimal))
        {
            result = (decimal) value;
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryConvertNumberValue(double value, Type clrType, LuaAllowedValueConversions allowedConversions, out object? result)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            result = null;
            return false;
        }

        if (clrType == typeof(double) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
        {
            result = value;
            return true;
        }

        if (clrType == typeof(float))
        {
            var floatValue = (float) value;
            if (!float.IsInfinity(floatValue))
            {
                result = floatValue;
                return true;
            }

            result = null;
            return false;
        }

        if (clrType == typeof(decimal))
        {
            if (LuaTypeServices.IsDecimalValueInRange(value))
            {
                result = (decimal) value;
                return true;
            }

            result = null;
            return false;
        }

        if (LuaTypeServices.IsIntegerType(clrType))
        {
            var allowTruncation = (allowedConversions & LuaAllowedValueConversions.TruncateFraction) != 0;
            var allowOverflow = (allowedConversions & LuaAllowedValueConversions.AllowOverflow) != 0;

            if (!allowTruncation && !double.IsInteger(value))
            {
                result = null;
                return false;
            }

            if (clrType == typeof(ulong))
            {
                if (allowOverflow || value is >= 0 and < TwoTo64)
                {
                    result = unchecked((ulong) value);
                    return true;
                }

                result = null;
                return false;
            }

            if (clrType == typeof(uint))
            {
                if (allowOverflow || value is >= uint.MinValue and <= uint.MaxValue)
                {
                    result = unchecked((uint) value);
                    return true;
                }

                result = null;
                return false;
            }

            if (!allowOverflow && value is < -TwoTo63 or >= TwoTo63)
            {
                result = null;
                return false;
            }

            return TryConvertIntegerTarget(unchecked((long) value), clrType, allowedConversions, out result);
        }

        result = null;
        return false;
    }

    private static bool TryConvertIntegerTarget(long value, Type clrType, LuaAllowedValueConversions allowedConversions, out object? result)
    {
        if ((allowedConversions & LuaAllowedValueConversions.AllowOverflow) == 0
            && !LuaTypeServices.IsIntegerValueInRange(value, clrType))
        {
            result = null;
            return false;
        }

        if (clrType == typeof(int))
        {
            result = (int) value;
            return true;
        }

        if (clrType == typeof(long))
        {
            result = value;
            return true;
        }

        if (clrType == typeof(uint))
        {
            result = (uint) value;
            return true;
        }

        if (clrType == typeof(ulong))
        {
            result = (ulong) value;
            return true;
        }

        if (clrType == typeof(sbyte))
        {
            result = (sbyte) value;
            return true;
        }

        if (clrType == typeof(byte))
        {
            result = (byte) value;
            return true;
        }

        if (clrType == typeof(short))
        {
            result = (short) value;
            return true;
        }

        if (clrType == typeof(ushort))
        {
            result = (ushort) value;
            return true;
        }

        result = null;
        return false;
    }

    private bool TryParseNumericValue(ReadOnlySpan<char> text, Type clrType, out object? result)
    {
        if (clrType == typeof(int) && int.TryParse(text, FormatProvider, out var intValue))
        {
            result = intValue;
            return true;
        }

        if (clrType == typeof(uint) && uint.TryParse(text, FormatProvider, out var uintValue))
        {
            result = uintValue;
            return true;
        }

        if (clrType == typeof(long) && long.TryParse(text, FormatProvider, out var longValue))
        {
            result = longValue;
            return true;
        }

        if (clrType == typeof(ulong) && ulong.TryParse(text, FormatProvider, out var ulongValue))
        {
            result = ulongValue;
            return true;
        }

        if (clrType == typeof(double) && double.TryParse(text, FormatProvider, out var doubleValue))
        {
            result = doubleValue;
            return true;
        }

        if (clrType == typeof(float) && float.TryParse(text, FormatProvider, out var floatValue))
        {
            result = floatValue;
            return true;
        }

        if (clrType == typeof(sbyte) && sbyte.TryParse(text, FormatProvider, out var sbyteValue))
        {
            result = sbyteValue;
            return true;
        }

        if (clrType == typeof(byte) && byte.TryParse(text, FormatProvider, out var byteValue))
        {
            result = byteValue;
            return true;
        }

        if (clrType == typeof(short) && short.TryParse(text, FormatProvider, out var shortValue))
        {
            result = shortValue;
            return true;
        }

        if (clrType == typeof(ushort) && ushort.TryParse(text, FormatProvider, out var ushortValue))
        {
            result = ushortValue;
            return true;
        }

        if (clrType == typeof(decimal) && decimal.TryParse(text, FormatProvider, out var decimalValue))
        {
            result = decimalValue;
            return true;
        }

        result = null;
        return false;
    }
}
