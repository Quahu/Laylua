using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

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
            case UserDataHandle:
            {
                ((UserDataHandle) (object) obj).Push(thread);
                return;
            }
            default:
            {
                if (obj is DescribedUserData)
                {
                    (obj as DescribedUserData)!.CreateUserDataHandle(thread).Push(thread);
                    return;
                }

                if (!typeof(T).IsValueType)
                {
                    if (UserDataDescriptorProvider.TryGetDescriptor<T>(obj, out var descriptor))
                    {
                        Dictionary<(object Value, UserDataDescriptor? Descriptor), UserDataHandle>? userDataHandleCache;
                        lock (_userDataHandleCaches)
                        {
                            if (!_userDataHandleCaches.TryGetValue((IntPtr) thread.MainThread.State.L, out userDataHandleCache))
                            {
                                _userDataHandleCaches[(IntPtr) thread.MainThread.State.L] = userDataHandleCache = new();
                            }
                        }

                        if (userDataHandleCache.TryGetValue((obj, descriptor), out var handle))
                        {
                            handle.Push(thread);
                            return;
                        }

                        Type clrType;
                        if (typeof(T).IsSealed || (clrType = obj.GetType()) == typeof(T))
                        {
                            handle = new DescriptorUserDataHandle<T>(thread, obj, descriptor);
                        }
                        else
                        {
                            // TODO: possibly improve this in the future
                            var userDataHandleType = typeof(DescriptorUserDataHandle<>).MakeGenericType(clrType);
                            var constructor = userDataHandleType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
                            handle = (UserDataHandle) constructor.Invoke([thread, obj, descriptor]);
                        }

                        handle.Push(thread);
                        userDataHandleCache[(obj, descriptor)] = handle;
                        return;
                    }
                }
                else
                {
                    if (UserDataDescriptorProvider.TryGetDescriptor<T>(obj, out var descriptor))
                    {
                        new DescriptorUserDataHandle<T>(thread, obj, descriptor).Push(thread);
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
                            case TypeCode.UInt64:
                            {
                                lua_pushinteger(L, ((IConvertible) obj).ToInt64(FormatProvider));
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
                        Throw.ArgumentException($"The object type {obj.GetType()} cannot be marshaled.", nameof(obj));
                        return;
                    }
                }
            }
        }
    }
}
