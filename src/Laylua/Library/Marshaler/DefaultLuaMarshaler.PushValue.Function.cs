using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    private const string DelegateHandleMetatableName = "__Laylua:DelegateHandle";

    private static readonly ConditionalWeakTable<Delegate, DelegateInvokerFactory.MethodInvokerDelegate> DelegateInvokers = new();

    private static readonly LuaCFunction DelegateCFunction = static L =>
    {
        var ptr = (nint*) lua_touserdata(L, lua_upvalueindex(1));
        if (ptr == null)
        {
            return luaL_argerror(L, lua_upvalueindex(1), "Invalid delegate handle.");
        }

        var handlePtr = *ptr;
        if (handlePtr == 0)
        {
            return luaL_argerror(L, lua_upvalueindex(1), "Invalid delegate handle.");
        }

        var gcHandle = GCHandle.FromIntPtr(handlePtr);
        if (gcHandle.Target is not Delegate @delegate)
        {
            return luaL_error(L, "Failed to retrieve the delegate from the handle.");
        }

        using var lua = LuaThread.FromExtraSpace(L);
        var top = lua_gettop(L);
        var arguments = top == 0
            ? LuaStackValueRange.Empty
            : lua.Stack.GetRange(1);

        var invoker = DelegateInvokers.GetValue(@delegate, DelegateInvokerFactory.CreateInvoker);
        var returnCount = invoker(lua, arguments);
        if (returnCount == 1
            && AwaitableUserData.ShouldImplicitAwait(lua)
            && AwaitableUserData.IsAwaitableType(@delegate.Method.ReturnType))
        {
            return LuaAsyncLibrary.YieldAwaitable(L);
        }

        return returnCount;
    };

    private static readonly LuaCFunction DelegateHandleGcCFunction = static L =>
    {
        var ptr = (nint*) lua_touserdata(L, 1);
        if (ptr == null)
        {
            return 0;
        }

        var handlePtr = *ptr;
        if (handlePtr == 0)
        {
            return 0;
        }

        if (!LayluaState.TryFromExtraSpace(L, out var layluaState) || !layluaState.TryGetLua(out var lua))
        {
            *ptr = 0;
            return 0;
        }

        if (lua.IsDisposing)
        {
            return 0;
        }

        if (TryUntrackDelegateHandle(lua, handlePtr))
        {
            try
            {
                var handle = GCHandle.FromIntPtr(handlePtr);
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            catch (InvalidOperationException)
            {
                // The handle was already released during Lua disposal cleanup.
            }
        }

        *ptr = 0;
        return 0;
    };

    protected virtual void PushDelegate(LuaThread thread, Delegate @delegate)
    {
        var L = thread.State.L;
        var ptr = (nint*) lua_newuserdatauv(L, (nuint) (sizeof(nint) * 2), 0);
        *ptr = 0;
        ptr[1] = 0; // not a Laylua typed userdata

        GCHandle handle = default;
        var handlePtr = (nint) 0;
        var isUserdataOnStack = true;
        var isMetatableOnStack = false;
        var isClosureOnStack = false;
        var lua = Lua.FromThread(thread);
        try
        {
            var createdMetatable = luaL_newmetatable(L, DelegateHandleMetatableName);
            isMetatableOnStack = true;
            if (createdMetatable)
            {
                try
                {
                    lua_pushcfunction(L, DelegateHandleGcCFunction);
                    lua_setfield(L, -2, "__gc");

                    lua_pushboolean(L, false);
                    lua_setfield(L, -2, "__metatable");
                }
                catch
                {
                    lua_pushnil(L);
                    lua_setfield(L, LuaRegistry.Index, DelegateHandleMetatableName);
                    throw;
                }
            }

            handle = GCHandle.Alloc(@delegate);
            handlePtr = GCHandle.ToIntPtr(handle);
            *ptr = handlePtr;

            TrackDelegateHandle(lua, handlePtr);

            lua_setmetatable(L, -2);
            isMetatableOnStack = false;

            lua_pushlightuserdata(L, (void*) LayluaNative.getPotentialPanicPtr);
            lua_pushcclosure(L, DelegateCFunction, 2);
            isUserdataOnStack = false;
            isClosureOnStack = true;

        }
        catch
        {
            _ = TryUntrackDelegateHandle(lua, handlePtr);

            if (handle.IsAllocated)
            {
                handle.Free();
            }

            *ptr = 0;
            if (isMetatableOnStack)
            {
                lua_pop(L);
            }

            if (isUserdataOnStack)
            {
                lua_pop(L);
            }

            if (isClosureOnStack)
            {
                lua_pop(L);
            }

            throw;
        }
    }
}
