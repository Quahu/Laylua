using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    /// <inheritdoc/>
    [SkipLocalsInit]
    public override void PushValue<T>(LuaThread thread, T obj)
    {
        var L = thread.State.L;
        switch (obj)
        {
            case null:
            {
                lua_pushnil(L);
                return;
            }
            case bool:
            {
                lua_pushboolean(L, (bool) (object) obj);
                return;
            }
            case sbyte:
            {
                lua_pushinteger(L, (sbyte) (object) obj);
                return;
            }
            case byte:
            {
                lua_pushinteger(L, (byte) (object) obj);
                return;
            }
            case short:
            {
                lua_pushinteger(L, (short) (object) obj);
                return;
            }
            case ushort:
            {
                lua_pushinteger(L, (ushort) (object) obj);
                return;
            }
            case int:
            {
                lua_pushinteger(L, (int) (object) obj);
                return;
            }
            case uint:
            {
                lua_pushinteger(L, (uint) (object) obj);
                return;
            }
            case long:
            {
                lua_pushinteger(L, (long) (object) obj);
                return;
            }
            case ulong:
            {
                lua_pushinteger(L, (long) (ulong) (object) obj);
                return;
            }
            case float:
            {
                lua_pushnumber(L, (float) (object) obj);
                return;
            }
            case double:
            {
                lua_pushnumber(L, (double) (object) obj);
                return;
            }
            case decimal:
            {
                lua_pushnumber(L, (lua_Number) (decimal) (object) obj);
                return;
            }
            case char:
            {
                var charValue = (char) (object) obj;
                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
                return;
            }
            case string:
            {
                lua_pushstring(L, (string) (object) obj);
                return;
            }
            case ReadOnlyMemory<char>:
            {
                lua_pushstring(L, ((ReadOnlyMemory<char>) (object) obj).Span);
                return;
            }
            case IntPtr:
            {
                lua_pushlightuserdata(L, ((IntPtr) (object) obj).ToPointer());
                return;
            }
            case UIntPtr:
            {
                lua_pushlightuserdata(L, ((UIntPtr) (object) obj).ToPointer());
                return;
            }
            case LuaStackValue:
            {
                LuaStackValue.ValidateOwnership(thread, (LuaStackValue) (object) obj);
                lua_pushvalue(L, ((LuaStackValue) (object) obj).Index);
                return;
            }
            case LuaReference reference:
            {
                thread.ThrowIfInvalid();
                reference.ThrowIfInvalid();
                LuaReference.ValidateOwnership(thread, reference);

                if (lua_rawgeti(L, LuaRegistry.Index, LuaReference.GetReference(reference)).IsNoneOrNil())
                {
                    lua_pop(L);
                    LuaThread.ThrowLuaException("Failed to push the value of the Lua reference.");
                }

                return;
            }
            default:
            {
                if (obj is Type typeValue)
                {
                    if (LuaTypeRegistry.TryPushDefinition(thread.State.L, typeValue))
                    {
                        return;
                    }

                    if (AllowsDynamicMarshaling)
                    {
                        LuaTypeRegistry.PushDynamicDefinition(thread.State.L, typeValue);
                        return;
                    }

                    throw new InvalidOperationException($"No Lua type handler is registered for '{typeValue}'. " +
                        $"Annotate it with {nameof(LuaTypeAttribute)} or set {nameof(AllowsDynamicMarshaling)} to {bool.TrueString}.");
                }

                var lua = Lua.FromExtraSpace(L);
                var type = obj.GetType();

                if (!type.IsValueType && TryPushCachedIdentity(L, lua, obj))
                {
                    return;
                }

                if (LuaTypeRegistry.TryPush(thread.State.L, type, obj))
                {
                    if (!type.IsValueType)
                    {
                        CacheIdentity(L, lua, obj);
                    }

                    return;
                }

                if (AwaitableUserData.TryPushAwaitable(L, obj))
                {
                    if (!type.IsValueType)
                    {
                        CacheIdentity(L, lua, obj);
                    }

                    return;
                }

                switch (obj)
                {
                    case Delegate:
                    {
                        if (obj is LuaCFunction)
                        {
                            lua_pushcfunction(L, (LuaCFunction) (object) obj);
                        }
                        else
                        {
                            PushDelegate(thread, (Delegate) (object) obj);
                        }

                        return;
                    }
                    case IEnumerable:
                    {
                        PushEnumerable(thread, (IEnumerable) obj);
                        return;
                    }
                    case IConvertible:
                    {
                        switch (((IConvertible) obj).GetTypeCode())
                        {
                            case TypeCode.Boolean:
                            {
                                lua_pushboolean(L, ((IConvertible) obj).ToBoolean(FormatProvider));
                                return;
                            }
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            {
                                lua_pushinteger(L, ((IConvertible) obj).ToInt64(FormatProvider));
                                return;
                            }
                            case TypeCode.UInt64:
                            {
                                var uint64Value = ((IConvertible) obj).ToUInt64(FormatProvider);
                                if (uint64Value <= long.MaxValue)
                                {
                                    lua_pushinteger(L, (long) uint64Value);
                                }
                                else
                                {
                                    lua_pushnumber(L, uint64Value);
                                }

                                return;
                            }
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                            {
                                lua_pushnumber(L, ((IConvertible) obj).ToDouble(FormatProvider));
                                return;
                            }
                            case TypeCode.Char:
                            {
                                var charValue = ((IConvertible) obj).ToChar(FormatProvider);
                                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
                                return;
                            }
                            case TypeCode.String:
                            {
                                lua_pushstring(L, ((IConvertible) obj).ToString(FormatProvider));
                                return;
                            }
                            default:
                            {
                                throw new ArgumentOutOfRangeException(nameof(obj), $"The convertible object type '{((IConvertible) obj).GetTypeCode()}' cannot be marshaled.");
                            }
                        }
                    }
                    default:
                    {
                        if (!AllowsDynamicMarshaling)
                        {
                            throw new InvalidOperationException($"No Lua type handler is registered for '{type}'. " +
                                $"Annotate it with {nameof(LuaTypeAttribute)} or set {nameof(AllowsDynamicMarshaling)} to {bool.TrueString}.");
                        }

                        var handler = LuaTypeRegistry.GetOrCreate(type);
                        handler.Push(thread.State.L, obj);

                        if (!type.IsValueType)
                        {
                            CacheIdentity(L, lua, obj);
                        }

                        return;
                    }
                }
            }
        }
    }
}
