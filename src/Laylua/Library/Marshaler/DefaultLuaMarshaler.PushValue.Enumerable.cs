using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    protected delegate void PushGenericEnumerableDelegate(LuaThread thread, IEnumerable enumerable);

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

    protected static void PushGenericEnumerable<T>(LuaThread thread, IEnumerable<T> enumerable)
    {
        thread.Stack.EnsureFreeCapacity(1);

        var L = thread.State.L;
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
                    thread.Stack.Push(item);

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
                    thread.Stack.Push(item);

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
                    thread.Stack.Push(item);

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
                    thread.Stack.Push(item);

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

    protected static void PushGenericEnumerable<TKey, TValue>(LuaThread thread, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        where TKey : notnull
    {
        thread.Stack.EnsureFreeCapacity(3);

        var L = thread.State.L;
        if (enumerable is Dictionary<TKey, TValue> stdDictionary)
        {
            lua_createtable(L, 0, stdDictionary.Count);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in stdDictionary)
                {
                    thread.Stack.Push(kvp.Key);
                    thread.Stack.Push(kvp.Value);

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
                    thread.Stack.Push(kvp.Key);
                    thread.Stack.Push(kvp.Value);

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

    protected virtual void PushEnumerable(LuaThread thread, IEnumerable enumerable)
    {
        var enumerableType = enumerable.GetType();
        var pushDelegate = GetPushGenericEnumerableDelegate(enumerableType);
        if (pushDelegate != null)
        {
            pushDelegate(thread, enumerable);
            return;
        }

        var L = thread.State.L;
        switch (enumerable)
        {
            case Array array:
            {
                thread.Stack.EnsureFreeCapacity(1);

                var length = array.Length;
                lua_createtable(L, length, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < length; i++)
                    {
                        var item = array.GetValue(i);
                        thread.Stack.Push(item);

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
                thread.Stack.EnsureFreeCapacity(2);

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
                            thread.Stack.Push(entry.Key);
                            thread.Stack.Push(entry.Value);

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
                thread.Stack.EnsureFreeCapacity(1);

                var count = list.Count;
                lua_createtable(L, count, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var item = list[i];
                        thread.Stack.Push(item);

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
                thread.Stack.EnsureFreeCapacity(1);

                lua_newtable(L);
                var tableIndex = lua_gettop(L);
                try
                {
                    var i = 1;
                    foreach (var item in enumerable)
                    {
                        thread.Stack.Push(item);

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
