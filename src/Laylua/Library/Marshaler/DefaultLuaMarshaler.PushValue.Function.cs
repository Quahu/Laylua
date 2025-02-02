using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    protected static ConditionalWeakTable<Delegate, UserDataDescriptorUtilities.MethodInvokerDelegate> DelegateInvokers { get; } = new();

    private static readonly LuaCFunction DelegateCFunction = static L =>
    {
        if (!UserDataHandle.TryFromStackIndex(L, lua_upvalueindex(1), out var handle))
            return luaL_argerror(L, lua_upvalueindex(1), "Invalid handle.");

        if (!handle.TryGetValue<Delegate>(out var @delegate))
            return luaL_error(L, "Failed to retrieve the delegate from the handle.");

        using var lua = LuaThread.FromExtraSpace(L);
        var top = lua_gettop(L);
        var arguments = top == 0
            ? LuaStackValueRange.Empty
            : lua.Stack.GetRange(1);

        var invoker = DelegateInvokers.GetValue(@delegate, UserDataDescriptorUtilities.CreateDelegateInvoker);
        return invoker(lua, arguments);
    };

    protected virtual void PushDelegate(LuaThread thread, Delegate @delegate)
    {
        var L = thread.State.L;
        Dictionary<(object Value, UserDataDescriptor? Descriptor), UserDataHandle>? userDataHandleCache;
        lock (_userDataHandleCaches)
        {
            var G = thread.MainThread.State.L;
            if (!_userDataHandleCaches.TryGetValue((IntPtr) G, out userDataHandleCache))
            {
                _userDataHandleCaches[(IntPtr) G] = userDataHandleCache = new();
            }
        }

        if (!userDataHandleCache.TryGetValue((@delegate, null), out var handle))
        {
            userDataHandleCache[(@delegate, null)] = handle = new UserDataHandle<Delegate>(thread, @delegate);
        }

        handle.Push(thread);
        lua_pushcclosure(L, DelegateCFunction, 1);
    }
}
