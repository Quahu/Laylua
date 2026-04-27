using System;
using System.Buffers;
using System.Reflection;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    private const double TwoTo63 = 1UL << 63;
    private const double TwoTo64 = TwoTo63 * 2;

    /// <inheritdoc/>
    public override bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj)
        where T : default
    {
        return TryGetValue(thread, stackIndex, LuaAllowedValueConversions.Default, out obj);
    }

    /// <inheritdoc/>
    public override bool TryGetValue<T>(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions, out T? obj)
        where T : default
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
                if (clrType.IsAssignableFrom(typeof(LuaTable)) || (allowedConversions & LuaAllowedValueConversions.ObjectFallback) != 0 && clrType == typeof(object))
                {
                    if (TryCreateTableReference(thread, stackIndex, out var table))
                    {
                        obj = (T) (object) table;
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
                else if ((allowedConversions & LuaAllowedValueConversions.TableToObject) != 0
                    && LuaTypeRegistry.TryGetFromTable<T>(L, stackIndex, out var tableValue))
                {
                    obj = tableValue!;
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.Function:
            {
                if ((allowedConversions & LuaAllowedValueConversions.FunctionToCallableTarget) != 0
                    && (clrType == typeof(MethodInfo) || clrType.BaseType == typeof(MulticastDelegate))
                    && TryGetMethodInfoFromFunction(L, stackIndex, clrType, out var methodResult))
                {
                    obj = (T) methodResult!;
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
                else if ((allowedConversions & LuaAllowedValueConversions.FunctionToCallableTarget) != 0 && clrType.BaseType == typeof(MulticastDelegate))
                {
                    if (TryCreateFunctionReference(thread, stackIndex, out var function))
                    {
                        var keepFunctionReference = false;
                        try
                        {
                            var @delegate = CreateDelegateFromFunction(function, clrType);
                            if (@delegate != null)
                            {
                                keepFunctionReference = true;
                                obj = (T) (object) @delegate;
                                return true;
                            }
                        }
                        finally
                        {
                            if (!keepFunctionReference)
                            {
                                function.Dispose();
                            }
                        }
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
            if (value is >= (double) decimal.MinValue and <= (double) decimal.MaxValue)
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
