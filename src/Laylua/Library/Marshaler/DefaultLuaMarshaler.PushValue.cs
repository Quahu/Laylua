using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;
#if !NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    /// <inheritdoc/>
    [SkipLocalsInit]
    public override void PushValue<T>(Lua lua, T obj)
    {
        var L = lua.GetStatePointer();
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
#if NET7_0_OR_GREATER
                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
#else
                lua_pushstring(L, MemoryMarshal.CreateReadOnlySpan(ref charValue, 1));
#endif
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
                LuaStackValue.ValidateOwnership(lua, (LuaStackValue) (object) obj);
                ((LuaStackValue) (object) obj).PushValue();
                return;
            }
            case LuaReference:
            {
                LuaReference.ValidateOwnership(lua, (LuaReference) (object) obj);
                LuaReference.PushValue((LuaReference) (object) obj);
                return;
            }
            case UserDataHandle:
            {
                ((UserDataHandle) (object) obj).Push();
                return;
            }
            default:
            {
                if (obj is DescribedUserData)
                {
                    (obj as DescribedUserData)!.CreateUserDataHandle(lua).Push();
                    return;
                }

                if (!typeof(T).IsValueType)
                {
                    if (UserDataDescriptorProvider.TryGetDescriptor<T>(obj, out var descriptor))
                    {
                        Dictionary<(object Value, UserDataDescriptor? Descriptor), UserDataHandle>? userDataHandleCache;
                        lock (_userDataHandleCaches)
                        {
                            if (!_userDataHandleCaches.TryGetValue((IntPtr) lua.MainThread.L, out userDataHandleCache))
                            {
                                _userDataHandleCaches[(IntPtr) lua.MainThread.L] = userDataHandleCache = new();
                            }
                        }

                        if (userDataHandleCache.TryGetValue((obj, descriptor), out var handle))
                        {
                            handle.Push();
                            return;
                        }

                        Type clrType;
                        if (typeof(T).IsSealed || (clrType = obj.GetType()) == typeof(T))
                        {
                            handle = new DescriptorUserDataHandle<T>(lua, obj, descriptor);
                        }
                        else
                        {
                            // TODO: possibly improve this in the future
                            var userDataHandleType = typeof(DescriptorUserDataHandle<>).MakeGenericType(clrType);
                            var constructor = userDataHandleType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
                            handle = (UserDataHandle) constructor.Invoke([lua, obj, descriptor]);
                        }

                        handle.Push();
                        userDataHandleCache[(obj, descriptor)] = handle;
                        return;
                    }
                }
                else
                {
                    if (UserDataDescriptorProvider.TryGetDescriptor<T>(obj, out var descriptor))
                    {
                        new DescriptorUserDataHandle<T>(lua, obj, descriptor).Push();
                        return;
                    }
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
                            PushDelegate(lua, (Delegate) (object) obj);
                        }

                        return;
                    }
                    case IEnumerable:
                    {
                        PushEnumerable(lua, (IEnumerable) obj);
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
                            case TypeCode.UInt64:
                            {
                                lua_pushinteger(L, ((IConvertible) obj).ToInt64(FormatProvider));
                                return;
                            }
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                            {
                                if (typeof(lua_Number) == typeof(double))
                                {
                                    lua_pushnumber(L, ((IConvertible) obj).ToDouble(FormatProvider));
                                }
                                else
                                {
                                    lua_pushnumber(L, (lua_Number) ((IConvertible) obj).ToType(typeof(lua_Number), FormatProvider));
                                }

                                return;
                            }
                            case TypeCode.Char:
                            {
                                var charValue = ((IConvertible) obj).ToChar(FormatProvider);
#if NET7_0_OR_GREATER
                                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
#else
                                lua_pushstring(L, MemoryMarshal.CreateReadOnlySpan(ref charValue, 1));
#endif
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
                        Throw.ArgumentException($"The object type {obj.GetType()} cannot be marshaled.", nameof(obj));
                        return;
                    }
                }
            }
        }
    }
}
