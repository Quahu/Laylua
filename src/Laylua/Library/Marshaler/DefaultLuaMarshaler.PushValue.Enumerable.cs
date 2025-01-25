using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    protected delegate void PushGenericEnumerableDelegate(LuaThread lua, IEnumerable enumerable);

    protected static ConditionalWeakTable<Type, PushGenericEnumerableDelegate?> PushGenericEnumerableDelegateCache { get; } = new();

    protected static PushGenericEnumerableDelegate? GetPushGenericEnumerableDelegate(Type enumerableType)
    {
        return PushGenericEnumerableDelegateCache.GetValue(enumerableType, static enumerableType =>
        {
            var elementTypes = GetElementTypes(enumerableType);
            if (elementTypes == null)
                return null;

            var luaParameterExpression = Expression.Parameter(typeof(LuaThread), "lua");
            var arrayParameterExpression = Expression.Parameter(typeof(IEnumerable), "enumerable");
            var convertArrayExpression = Expression.Convert(arrayParameterExpression, enumerableType);
            var callExpression = Expression.Call(typeof(DefaultLuaMarshaler), nameof(PushGenericEnumerable), elementTypes, luaParameterExpression, convertArrayExpression);
            var lambda = Expression.Lambda<PushGenericEnumerableDelegate>(callExpression, luaParameterExpression, arrayParameterExpression);
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

    protected static void PushGenericEnumerable<T>(LuaThread lua, IEnumerable<T> enumerable)
    {
        lua.Stack.EnsureFreeCapacity(1);

        var L = lua.GetStatePointer();
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
                    lua.Marshaler.PushValue(lua, item);

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
                    lua.Marshaler.PushValue(lua, item);

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
                    lua.Marshaler.PushValue(lua, item);

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
                    lua.Marshaler.PushValue(lua, item);

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

    protected static void PushGenericEnumerable<TKey, TValue>(LuaThread lua, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        where TKey : notnull
    {
        lua.Stack.EnsureFreeCapacity(3);

        var L = lua.GetStatePointer();

        if (enumerable is Dictionary<TKey, TValue> stdDictionary)
        {
            lua_createtable(L, 0, stdDictionary.Count);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in stdDictionary)
                {
                    lua.Marshaler.PushValue(lua, kvp.Key);
                    lua.Marshaler.PushValue(lua, kvp.Value);

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
                    lua.Marshaler.PushValue(lua, kvp.Key);
                    lua.Marshaler.PushValue(lua, kvp.Value);

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

    protected virtual void PushEnumerable(LuaThread lua, IEnumerable enumerable)
    {
        var enumerableType = enumerable.GetType();
        var pushDelegate = GetPushGenericEnumerableDelegate(enumerableType);
        if (pushDelegate != null)
        {
            pushDelegate(lua, enumerable);
            return;
        }

        var L = lua.GetStatePointer();
        switch (enumerable)
        {
            case Array array:
            {
                lua.Stack.EnsureFreeCapacity(1);

                var length = array.Length;
                lua_createtable(L, length, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < length; i++)
                    {
                        var item = array.GetValue(i);
                        PushValue(lua, item);

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
                lua.Stack.EnsureFreeCapacity(2);

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
                            PushValue(lua, entry.Key);
                            PushValue(lua, entry.Value);

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
                lua.Stack.EnsureFreeCapacity(1);

                var count = list.Count;
                lua_createtable(L, count, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var item = list[i];
                        PushValue(lua, item);

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
                lua.Stack.EnsureFreeCapacity(1);

                lua_newtable(L);
                var tableIndex = lua_gettop(L);
                try
                {
                    var i = 1;
                    foreach (var item in enumerable)
                    {
                        PushValue(lua, item);

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
