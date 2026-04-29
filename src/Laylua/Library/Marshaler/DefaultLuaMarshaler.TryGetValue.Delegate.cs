using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    internal static bool CanConvertFunctionToDelegate(lua_State* L, int stackIndex, Type delegateType)
    {
        if (delegateType.BaseType != typeof(MulticastDelegate))
        {
            return false;
        }

        return TryGetCallableTargetFromFunction(L, stackIndex, delegateType, out _);
    }

    internal static bool CanConvertFunctionToMethodInfo(lua_State* L, int stackIndex)
    {
        return TryGetMethodInfoFromFunction(L, stackIndex, typeof(MethodInfo), out _);
    }

    internal static bool TryGetCallableTargetFromFunction(lua_State* L, int stackIndex, Type targetType, out object? result)
    {
        if (TryGetMethodInfoFromFunction(L, stackIndex, targetType, out result))
        {
            return true;
        }

        if (targetType.BaseType == typeof(MulticastDelegate) && TryGetManagedDelegateFromFunction(L, stackIndex, targetType, out var @delegate))
        {
            result = @delegate;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    ///     Attempts to resolve a typed userdata method CFunction to a <see cref="MethodInfo"/>
    ///     or an open instance delegate. Returns <c>false</c> for plain Lua functions.
    /// </summary>
    private static bool TryGetMethodInfoFromFunction(lua_State* L, int stackIndex, Type targetType, out object? result)
    {
        var rawPtr = (nint) lua_topointer(L, stackIndex);
        if (rawPtr == 0 || !LuaTypeRegistry.TryGetMethodPointerInfo(rawPtr, out var info))
        {
            result = null;
            return false;
        }

        var methods = info.Methods;
        if (methods.Count == 0)
        {
            result = null;
            return false;
        }

        if (targetType == typeof(MethodInfo))
        {
            if (methods.Count == 1)
            {
                result = methods[0];
                return true;
            }

            result = null;
            return false;
        }

        // Specific delegate type -> create open delegate via signature matching
        if (targetType.IsSubclassOf(typeof(MulticastDelegate))
            && info.TryGetOpenDelegateMethod(targetType, out var matchingMethod)
            && matchingMethod != null)
        {
            var @delegate = Delegate.CreateDelegate(targetType, null, matchingMethod, throwOnBindFailure: false);
            if (@delegate != null)
            {
                result = @delegate;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static bool TryGetManagedDelegateFromFunction(lua_State* L, int stackIndex, Type delegateType, out Delegate? result)
    {
        stackIndex = lua_absindex(L, stackIndex);
        var top = lua_gettop(L);
        try
        {
            if (!IsManagedDelegateClosure(L, stackIndex))
            {
                result = null;
                return false;
            }

            if (lua_getupvalue(L, stackIndex, 1).Pointer == null)
            {
                result = null;
                return false;
            }

            if (lua_type(L, -1) != LuaType.UserData || !IsDelegateHandleUserData(L, -1))
            {
                result = null;
                return false;
            }

            var ptr = (nint*) lua_touserdata(L, -1);
            if (ptr == null || *ptr == 0)
            {
                result = null;
                return false;
            }

            var handlePtr = *ptr;
            if (!IsDelegateHandleTracked(Lua.FromExtraSpace(L), handlePtr))
            {
                result = null;
                return false;
            }

            var handle = GCHandle.FromIntPtr(handlePtr);
            if (handle.Target is Delegate @delegate && delegateType.IsInstanceOfType(@delegate))
            {
                result = @delegate;
                return true;
            }

            result = null;
            return false;
        }
        finally
        {
            lua_settop(L, top);
        }
    }

    private static bool IsManagedDelegateClosure(lua_State* L, int stackIndex)
    {
        if (!lua_iscfunction(L, stackIndex))
        {
            return false;
        }

        if (lua_getupvalue(L, stackIndex, 2).Pointer == null)
        {
            return false;
        }

        if (lua_type(L, -1) != LuaType.LightUserData
            || (nint) lua_touserdata(L, -1) != (nint) LayluaNative.getPotentialPanicPtr)
        {
            lua_pop(L, 1);
            return false;
        }

        lua_pop(L, 1);
        return true;
    }

    private static bool IsDelegateHandleUserData(lua_State* L, int stackIndex)
    {
        stackIndex = lua_absindex(L, stackIndex);
        if (!lua_getmetatable(L, stackIndex))
        {
            return false;
        }

        if (luaL_getmetatable(L, DelegateHandleMetatableName).IsNoneOrNil())
        {
            lua_pop(L, 2);
            return false;
        }

        var isDelegateHandle = lua_rawequal(L, -1, -2);
        lua_pop(L, 2);
        return isDelegateHandle;
    }

}
