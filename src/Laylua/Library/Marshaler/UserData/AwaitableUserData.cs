using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Laylua.Moon;

namespace Laylua.Marshaling;

internal static unsafe class AwaitableUserData
{
    private const string ImplicitAwaitMetatableName = "laylua.awaitable";

    private static readonly ConcurrentDictionary<Type, DynamicUserDataHandler> ReturnAwaitableHandlers = new();

    private static readonly LuaCFunction GcCFunction = static L =>
    {
        var ptr = (nint*) lua_touserdata(L, 1);
        LuaTypeServices.TryFreeUserData(ptr);
        return 0;
    };

    public static bool IsAwaitableType(Type type)
    {
        return TryGetAwaitableBoundaryType(type, out _);
    }

    public static bool ShouldImplicitAwait(LuaThread thread)
    {
        return Lua.FromThread(thread).Marshaler is not DefaultLuaMarshaler defaultMarshaler
            || defaultMarshaler.AwaitableReturnHandling == LuaAwaitableReturnHandling.ImplicitAwait;
    }

    public static bool TryPushAwaitable<T>(lua_State* L, T value)
    {
        if (!TryGetAwaitableValue(value, out var target, out var boundaryType))
        {
            return false;
        }

        if (!LuaTypeRegistry.TryPush(L, boundaryType, target))
        {
            LuaTypeRegistry.GetOrCreate(boundaryType).Push(L, target);
        }

        return true;
    }

    public static void PushReturnAwaitable<T>(lua_State* L, T value)
    {
        if (!TryGetAwaitableValue(value, out var target, out var boundaryType))
        {
            lua_pushnil(L);
            return;
        }

        var handler = ReturnAwaitableHandlers.GetOrAdd(boundaryType, static type =>
        {
            var options = LuaTypeRegistry.GetDynamicOptions(type).Clone();
            return new DynamicUserDataHandler(type, options);
        });

        handler.Push(L, target);
    }

    public static void PushImplicitAwaitable<T>(lua_State* L, T value)
    {
        if (!TryGetAwaitableValue(value, out var target, out _))
        {
            lua_pushnil(L);
            return;
        }

        EnsureImplicitAwaitMetatable(L);

        var ptr = LuaTypeServices.NewUserData(L, target);
        var metatablePushed = false;

        try
        {
            if (luaL_getmetatable(L, ImplicitAwaitMetatableName) != LuaType.Table)
            {
                lua_pop(L);
                LuaThread.ThrowLuaException("Failed to retrieve the awaitable metatable.");
            }

            metatablePushed = true;
            lua_setmetatable(L, -2);
            metatablePushed = false;
        }
        catch
        {
            LuaTypeServices.TryFreeUserData(ptr);
            if (metatablePushed)
            {
                lua_pop(L);
            }

            lua_pop(L);
            throw;
        }
    }

    public static bool IsAwaitable(lua_State* L, int index)
    {
        return TryGetAwaitableTarget(L, index, out _, out _);
    }

    public static bool TryGetBox(lua_State* L, int index, out AwaitableBox? box)
    {
        if (!TryGetAwaitableTarget(L, index, out var target, out var boundaryType))
        {
            box = null;
            return false;
        }

        box = new AwaitableBox(target, boundaryType);
        return true;
    }

    public static bool TryGetValue<T>(lua_State* L, int index, out T? value)
    {
        if (TryGetAwaitableTarget(L, index, out var target, out _) && target is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetAwaitableValue<T>(T value, out object target, out Type boundaryType)
    {
        if (TryGetAwaitableBoundaryType(typeof(T), out boundaryType))
        {
            target = value!;
            return true;
        }

        if (value is object boxedValue && TryGetAwaitableBoundaryType(boxedValue.GetType(), out boundaryType))
        {
            target = boxedValue;
            return true;
        }

        target = null!;
        return false;
    }

    private static bool TryGetAwaitableTarget(lua_State* L, int index, out object target, out Type boundaryType)
    {
        if (lua_type(L, index) != LuaType.UserData)
        {
            target = null!;
            boundaryType = null!;
            return false;
        }

        var userData = (nint*) lua_touserdata(L, index);
        if (!LuaTypeServices.TryGetUserDataTarget(userData, out var candidateTarget))
        {
            target = null!;
            boundaryType = null!;
            return false;
        }

        target = candidateTarget;

        if (!TryGetAwaitableBoundaryType(target.GetType(), out boundaryType))
        {
            target = null!;
            return false;
        }

        return true;
    }

    private static bool TryGetAwaitableBoundaryType(Type type, out Type boundaryType)
    {
        if (typeof(Task).IsAssignableFrom(type))
        {
            boundaryType = GetTaskBoundaryType(type);
            return true;
        }

        if (type == typeof(ValueTask))
        {
            boundaryType = type;
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            boundaryType = type;
            return true;
        }

        boundaryType = null!;
        return false;
    }

    private static Type GetTaskBoundaryType(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            if (current == typeof(Task))
            {
                return current;
            }

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = current.GetGenericArguments()[0];
                return resultType.FullName == "System.Threading.Tasks.VoidTaskResult"
                    ? typeof(Task)
                    : current;
            }
        }

        throw new InvalidOperationException($"Unsupported awaitable type '{type}'.");
    }

    private static void EnsureImplicitAwaitMetatable(lua_State* L)
    {
        if (luaL_newmetatable(L, ImplicitAwaitMetatableName))
        {
            try
            {
                lua_pushcfunction(L, GcCFunction);
                lua_setfield(L, -2, "__gc");

                lua_pushboolean(L, false);
                lua_setfield(L, -2, "__metatable");
            }
            catch
            {
                lua_pushnil(L);
                lua_setfield(L, LuaRegistry.Index, ImplicitAwaitMetatableName);
                lua_pop(L);
                throw;
            }
        }

        lua_pop(L);
    }
}
