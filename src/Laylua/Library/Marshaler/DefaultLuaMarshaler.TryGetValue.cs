using System;
using System.Buffers;
using Laylua.Moon;
using Qommon;
#if !NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    /// <inheritdoc/>
    public override bool TryGetValue<T>(Lua lua, int stackIndex, out T? obj)
        where T : default
    {
        var L = lua.GetStatePointer();
        var luaType = lua_type(L, stackIndex);
        if (luaType == LuaType.None)
        {
            // No value at the index.
            obj = default;
            return false;
        }

        if (luaType == LuaType.Nil)
        {
            // Null value at the index.
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
                if (clrType == typeof(bool) || clrType == typeof(object))
                {
                    obj = (T) (object) boolValue;
                    return true;
                }

                if (clrType == typeof(string))
                {
                    obj = (T) (object) (boolValue ? "true" : "false");
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.LightUserData:
            {
                if (clrType == typeof(IntPtr) || clrType == typeof(object))
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
                if (lua_isinteger(L, stackIndex))
                {
                    var longValue = lua_tointeger(L, stackIndex);
                    if (clrType == typeof(int))
                    {
                        obj = (T) (object) (int) longValue;
                        return true;
                    }

                    if (clrType == typeof(long) || clrType == typeof(object))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(uint))
                    {
                        obj = (T) (object) (uint) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong))
                    {
                        obj = (T) (object) (ulong) longValue;
                        return true;
                    }

                    if (clrType == typeof(double))
                    {
                        obj = (T) (object) (double) longValue;
                        return true;
                    }

                    if (clrType == typeof(float))
                    {
                        obj = (T) (object) (float) longValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte))
                    {
                        obj = (T) (object) (sbyte) longValue;
                        return true;
                    }

                    if (clrType == typeof(byte))
                    {
                        obj = (T) (object) (byte) longValue;
                        return true;
                    }

                    if (clrType == typeof(short))
                    {
                        obj = (T) (object) (short) longValue;
                        return true;
                    }

                    if (clrType == typeof(ushort))
                    {
                        obj = (T) (object) (ushort) longValue;
                        return true;
                    }

                    if (clrType == typeof(decimal))
                    {
                        obj = (T) (object) (decimal) longValue;
                        return true;
                    }

                    if (clrType == typeof(string))
                    {
                        obj = (T) (object) longValue.ToString(lua.FormatProvider);
                        return true;
                    }
                }
                else
                {
                    var doubleValue = lua_tonumber(L, stackIndex);
                    if (clrType == typeof(int))
                    {
                        obj = (T) (object) (int) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(long))
                    {
                        obj = (T) (object) (long) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(uint))
                    {
                        obj = (T) (object) (uint) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(ulong))
                    {
                        obj = (T) (object) (ulong) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(double) || clrType == typeof(object))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float))
                    {
                        obj = (T) (object) (float) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte))
                    {
                        obj = (T) (object) (sbyte) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(byte))
                    {
                        obj = (T) (object) (byte) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(short))
                    {
                        obj = (T) (object) (short) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(ushort))
                    {
                        obj = (T) (object) (ushort) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(decimal))
                    {
                        obj = (T) (object) (decimal) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(string))
                    {
                        obj = (T) (object) doubleValue.ToString(lua.FormatProvider);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.String:
            {
                var nativeStringValue = lua_tostring(L, stackIndex);
                if (clrType == typeof(string) || clrType == typeof(object))
                {
                    obj = (T) (object) nativeStringValue.ToString();
                    return true;
                }

                if (clrType == typeof(LuaString))
                {
                    obj = (T) (object) nativeStringValue;
                    return true;
                }

                var charCount = nativeStringValue.CharLength;
                char[]? rentedArray = null;
                scoped var charSpan = charCount > 256
                    ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                    : stackalloc char[charCount];

                try
                {
                    nativeStringValue.GetChars(charSpan);
#if NET7_0_OR_GREATER
                    if (clrType == typeof(int) && int.TryParse(charSpan, lua.FormatProvider, out var intValue))
                    {
                        obj = (T) (object) intValue;
                        return true;
                    }

                    if (clrType == typeof(uint) && uint.TryParse(charSpan, lua.FormatProvider, out var uintValue))
                    {
                        obj = (T) (object) uintValue;
                        return true;
                    }

                    if (clrType == typeof(long) && long.TryParse(charSpan, lua.FormatProvider, out var longValue))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong) && ulong.TryParse(charSpan, lua.FormatProvider, out var ulongValue))
                    {
                        obj = (T) (object) ulongValue;
                        return true;
                    }

                    if (clrType == typeof(double) && double.TryParse(charSpan, lua.FormatProvider, out var doubleValue))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float) && float.TryParse(charSpan, lua.FormatProvider, out var floatValue))
                    {
                        obj = (T) (object) floatValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte) && sbyte.TryParse(charSpan, lua.FormatProvider, out var sbyteValue))
                    {
                        obj = (T) (object) sbyteValue;
                        return true;
                    }

                    if (clrType == typeof(byte) && byte.TryParse(charSpan, lua.FormatProvider, out var byteValue))
                    {
                        obj = (T) (object) byteValue;
                        return true;
                    }

                    if (clrType == typeof(short) && short.TryParse(charSpan, lua.FormatProvider, out var shortValue))
                    {
                        obj = (T) (object) shortValue;
                        return true;
                    }

                    if (clrType == typeof(ushort) && ushort.TryParse(charSpan, lua.FormatProvider, out var ushortValue))
                    {
                        obj = (T) (object) ushortValue;
                        return true;
                    }

                    if (clrType == typeof(decimal) && decimal.TryParse(charSpan, lua.FormatProvider, out var decimalValue))
                    {
                        obj = (T) (object) decimalValue;
                        return true;
                    }
#else
                    if (clrType == typeof(int) && int.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var intValue))
                    {
                        obj = (T) (object) intValue;
                        return true;
                    }

                    if (clrType == typeof(uint) && uint.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var uintValue))
                    {
                        obj = (T) (object) uintValue;
                        return true;
                    }

                    if (clrType == typeof(long) && long.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var longValue))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong) && ulong.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var ulongValue))
                    {
                        obj = (T) (object) ulongValue;
                        return true;
                    }

                    if (clrType == typeof(double) && double.TryParse(charSpan, NumberStyles.Float | NumberStyles.AllowThousands, lua.FormatProvider, out var doubleValue))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float) && float.TryParse(charSpan, NumberStyles.Float | NumberStyles.AllowThousands, lua.FormatProvider, out var floatValue))
                    {
                        obj = (T) (object) floatValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte) && sbyte.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var sbyteValue))
                    {
                        obj = (T) (object) sbyteValue;
                        return true;
                    }

                    if (clrType == typeof(byte) && byte.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var byteValue))
                    {
                        obj = (T) (object) byteValue;
                        return true;
                    }

                    if (clrType == typeof(short) && short.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var shortValue))
                    {
                        obj = (T) (object) shortValue;
                        return true;
                    }

                    if (clrType == typeof(ushort) && ushort.TryParse(charSpan, NumberStyles.Integer, lua.FormatProvider, out var ushortValue))
                    {
                        obj = (T) (object) ushortValue;
                        return true;
                    }

                    if (clrType == typeof(decimal) && decimal.TryParse(charSpan, NumberStyles.Number, lua.FormatProvider, out var decimalValue))
                    {
                        obj = (T) (object) decimalValue;
                        return true;
                    }
#endif

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
            case LuaType.Table:
            {
                if (typeof(LuaTable).IsAssignableTo(clrType) || clrType == typeof(object))
                {
                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateTable(lua, reference);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Function:
            {
                if (typeof(LuaFunction).IsAssignableTo(clrType) || clrType == typeof(object))
                {
                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateFunction(lua, reference);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.UserData:
            {
                if (UserDataHandle.TryFromStackIndex(L, stackIndex, out var handle))
                {
                    if (handle.TryGetValue(out obj))
                    {
                        return true;
                    }

                    if (handle is T)
                    {
                        obj = (T) (object) handle;
                        return true;
                    }
                }

                if (typeof(LuaUserData).IsAssignableTo(clrType) || clrType == typeof(object))
                {
                    var ptr = lua_touserdata(L, stackIndex);
                    if (ptr != null && LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateUserData(lua, reference, (IntPtr) ptr);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Thread:
            {
                if (clrType.IsAssignableTo(typeof(LuaReference)) || clrType == typeof(object))
                {
                    var threadPtr = lua_tothread(L, stackIndex);
                    if (threadPtr == lua.MainThread.L)
                    {
                        obj = (T) (object) lua.MainThread;
                        return true;
                    }

                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateThread(lua, reference, threadPtr);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(luaType), luaType, "Unsupported Lua type.");
            }
        }
    }
}
