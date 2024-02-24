using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    protected delegate void PushGenericEnumerableDelegate(DefaultLuaMarshaler marshaler, IEnumerable enumerable);

    protected static ConditionalWeakTable<Type, PushGenericEnumerableDelegate?> PushGenericEnumerableDelegateCache { get; } = new();

    protected static PushGenericEnumerableDelegate? GetPushGenericEnumerableDelegate(Type enumerableType)
    {
        return PushGenericEnumerableDelegateCache.GetValue(enumerableType, static enumerableType =>
        {
            var elementTypes = GetElementTypes(enumerableType);
            if (elementTypes == null)
                return null;

            var marshalerParameterExpression = Expression.Parameter(typeof(DefaultLuaMarshaler), "marshaler");
            var arrayParameterExpression = Expression.Parameter(typeof(IEnumerable), "enumerable");
            var convertArrayExpression = Expression.Convert(arrayParameterExpression, enumerableType);
            var callExpression = Expression.Call(typeof(DefaultLuaMarshaler), nameof(PushGenericEnumerable), elementTypes, marshalerParameterExpression, convertArrayExpression);
            var lambda = Expression.Lambda<PushGenericEnumerableDelegate>(callExpression, marshalerParameterExpression, arrayParameterExpression);
            return lambda.Compile();

            static Type[]? GetElementTypes(Type enumerableType)
            {
                if (enumerableType.IsArray)
                {
                    if (enumerableType.GetArrayRank() != 1)
                        throw new RankException("Marshaling multidimensional arrays is not supported.");

                    var elementType = enumerableType.GetElementType();
                    if (elementType != null && elementType.IsValueType)
                        return [elementType];

                    return null;
                }

                var interfaces = enumerableType.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var innerType = @interface.GenericTypeArguments[0];
                        if (innerType.IsValueType)
                        {
                            if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {
                                var typeArguments = innerType.GenericTypeArguments;
                                return [typeArguments[0], typeArguments[1]];
                            }

                            return [innerType];
                        }
                    }
                }

                return null;
            }
        });
    }

    protected static void PushGenericEnumerable<T>(DefaultLuaMarshaler marshaler, IEnumerable<T> enumerable)
    {
        marshaler.Lua.Stack.EnsureFreeCapacity(1);

        var L = marshaler.Lua.GetStatePointer();
        if (enumerable is T[] array)
        {
            var length = array.Length;
            lua_createtable(L, length, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < length; i++)
                {
                    var item = array[i];
                    marshaler.PushValue(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else if (enumerable is List<T> stdList)
        {
            var count = stdList.Count;
            lua_createtable(L, count, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var item = stdList[i];
                    marshaler.PushValue(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else if (enumerable is IList<T> list)
        {
            var count = list.Count;
            lua_createtable(L, count, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var item = list[i];
                    marshaler.PushValue(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else
        {
            lua_createtable(L, enumerable.TryGetNonEnumeratedCount(out var count) ? count : 0, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                var i = 1;
                foreach (var item in enumerable)
                {
                    marshaler.PushValue(item);

                    lua_rawseti(L, tableIndex, i++);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
    }

    protected static void PushGenericEnumerable<TKey, TValue>(DefaultLuaMarshaler marshaler, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        where TKey : notnull
    {
        marshaler.Lua.Stack.EnsureFreeCapacity(3);

        var L = marshaler.Lua.GetStatePointer();

        if (enumerable is Dictionary<TKey, TValue> stdDictionary)
        {
            lua_createtable(L, 0, stdDictionary.Count);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in stdDictionary)
                {
                    marshaler.PushValue(kvp.Key);
                    marshaler.PushValue(kvp.Value);

                    lua_rawset(L, tableIndex);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else
        {
            lua_createtable(L, 0, enumerable.TryGetNonEnumeratedCount(out var count) ? count : 0);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in enumerable)
                {
                    marshaler.PushValue(kvp.Key);
                    marshaler.PushValue(kvp.Value);

                    lua_rawset(L, tableIndex);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
    }

    protected virtual void PushEnumerable(IEnumerable enumerable)
    {
        var enumerableType = enumerable.GetType();
        var pushDelegate = GetPushGenericEnumerableDelegate(enumerableType);
        if (pushDelegate != null)
        {
            pushDelegate(this, enumerable);
            return;
        }

        var L = Lua.GetStatePointer();
        switch (enumerable)
        {
            case Array array:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                var length = array.Length;
                lua_createtable(L, length, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < length; i++)
                    {
                        var item = array.GetValue(i);
                        PushValue(item);

                        lua_rawseti(L, tableIndex, i + 1);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            case IDictionary dictionary:
            {
                Lua.Stack.EnsureFreeCapacity(2);

                lua_createtable(L, 0, dictionary.Count);
                var tableIndex = lua_gettop(L);
                try
                {
                    var enumerator = dictionary.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            var entry = enumerator.Entry;
                            PushValue(entry.Key);
                            PushValue(entry.Value);

                            lua_rawset(L, tableIndex);
                        }
                    }
                    finally
                    {
                        (enumerator as IDisposable)?.Dispose();
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            case IList list:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                var count = list.Count;
                lua_createtable(L, count, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var item = list[i];
                        PushValue(item);

                        lua_rawseti(L, tableIndex, i + 1);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            default:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                lua_newtable(L);
                var tableIndex = lua_gettop(L);
                try
                {
                    var i = 1;
                    foreach (var item in enumerable)
                    {
                        PushValue(item);

                        lua_rawseti(L, tableIndex, i++);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
        }
    }
}
