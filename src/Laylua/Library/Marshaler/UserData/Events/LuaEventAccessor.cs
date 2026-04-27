using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public sealed unsafe class LuaEventAccessor
{
    private const string MetatableName = "Laylua.EventAccessor";

    private static readonly LuaCFunction _indexCallback = IndexCallback;
    private static readonly nint _indexCallbackPtr = CreateSafeCFunction(_indexCallback);

    private static readonly LuaCFunction _addCallback = AddCallback;
    private static readonly nint _addCallbackPtr = CreateSafeCFunction(_addCallback);

    private static readonly LuaCFunction _removeCallback = RemoveCallback;
    private static readonly nint _removeCallbackPtr = CreateSafeCFunction(_removeCallback);

    private static readonly LuaCFunction _gcCallback = GcCallback;
    private static readonly nint _gcCallbackPtr = CreateSafeCFunction(_gcCallback);

    public object LookupKey { get; init; } = null!;

    public object CleanupKey { get; init; } = null!;

    public object? Target { get; init; }

    public EventInfo EventInfo { get; init; } = null!;

    public LuaEventSubscriptionTracker Tracker { get; init; } = null!;

    public bool CanAdd { get; init; }

    public bool CanRemove { get; init; }

    public static void Push(
        lua_State* L,
        object? target,
        EventInfo eventInfo,
        LuaEventSubscriptionTracker tracker,
        int parentStackIndex,
        object? subscriptionKey = null,
        bool canAdd = true,
        bool canRemove = true)
    {
        var parentUserData = lua_touserdata(L, parentStackIndex);
        if (parentUserData == null)
        {
            throw new ArgumentException("Parent stack value is not userdata.", nameof(parentStackIndex));
        }

        var accessor = new LuaEventAccessor
        {
            LookupKey = subscriptionKey ?? (nint) parentUserData,
            CleanupKey = (nint) parentUserData,
            Target = target,
            EventInfo = eventInfo,
            Tracker = tracker,
            CanAdd = canAdd,
            CanRemove = canRemove,
        };

        var ud = (nint*) lua_newuserdatauv(L, (nuint) sizeof(nint), 1);
        var handle = GCHandle.Alloc(accessor);
        *ud = GCHandle.ToIntPtr(handle);

        if (luaL_newmetatable(L, MetatableName))
        {
            lua_pushstring(L, "__index");
            lua_pushcfunction(L, _indexCallbackPtr);
            lua_rawset(L, -3);

            lua_pushstring(L, "__gc");
            lua_pushcfunction(L, _gcCallbackPtr);
            lua_rawset(L, -3);

            lua_pushstring(L, "__metatable");
            lua_pushboolean(L, false);
            lua_rawset(L, -3);
        }

        lua_setmetatable(L, -2);
        lua_pushvalue(L, parentStackIndex);
        lua_setiuservalue(L, -2, 1);
    }

    private static bool TryGetAccessor(lua_State* L, int stackIndex, out LuaEventAccessor? accessor)
    {
        var ud = (nint*) luaL_testudata(L, stackIndex, MetatableName);
        if (ud == null || *ud == 0)
        {
            accessor = null;
            return false;
        }

        accessor = GCHandle.FromIntPtr(*ud).Target as LuaEventAccessor;
        return accessor != null;
    }

    private static bool HasParentUserData(lua_State* L, int stackIndex)
    {
        var valueType = lua_getiuservalue(L, stackIndex, 1);
        var hasParent = !valueType.IsNoneOrNil();
        lua_pop(L, 1);
        return hasParent;
    }

    private static int IndexCallback(lua_State* L)
    {
        if (lua_type(L, 2) != LuaType.String)
        {
            return 0;
        }

        var key = lua_tostring(L, 2).Span;
        if (key.SequenceEqual("add"u8))
        {
            lua_pushcfunction(L, _addCallbackPtr);
            return 1;
        }

        if (key.SequenceEqual("remove"u8))
        {
            lua_pushcfunction(L, _removeCallbackPtr);
            return 1;
        }

        return 0;
    }

    private static int AddCallback(lua_State* L)
    {
        if (!TryGetAccessor(L, 1, out var accessor) || accessor == null)
        {
            luaL_error(L, "invalid event accessor");
            return 0;
        }

        if (!accessor.CanAdd)
        {
            luaL_error(L, "event does not support 'add'");
            return 0;
        }

        if (!HasParentUserData(L, 1))
        {
            luaL_error(L, "event accessor parent userdata is no longer available");
            return 0;
        }

        if (lua_type(L, 2) != LuaType.Function)
        {
            luaL_error(L, "expected a function argument for event 'add'");
            return 0;
        }

        if (!accessor.Tracker.Subscribe(L, accessor.LookupKey, accessor.CleanupKey, accessor.EventInfo.Name, accessor.EventInfo, accessor.Target, 2))
        {
            luaL_error(L, $"failed to subscribe to event '{accessor.EventInfo.Name}'");
        }

        return 0;
    }

    private static int RemoveCallback(lua_State* L)
    {
        if (!TryGetAccessor(L, 1, out var accessor) || accessor == null)
        {
            luaL_error(L, "invalid event accessor");
            return 0;
        }

        if (!accessor.CanRemove)
        {
            luaL_error(L, "event does not support 'remove'");
            return 0;
        }

        if (!HasParentUserData(L, 1))
        {
            luaL_error(L, "event accessor parent userdata is no longer available");
            return 0;
        }

        if (lua_type(L, 2) != LuaType.Function)
        {
            luaL_error(L, "expected a function argument for event 'remove'");
            return 0;
        }

        accessor.Tracker.Unsubscribe(L, accessor.LookupKey, accessor.EventInfo.Name, accessor.EventInfo, accessor.Target, 2);
        return 0;
    }

    private static int GcCallback(lua_State* L)
    {
        var ud = (nint*) lua_touserdata(L, 1);
        if (ud != null && *ud != 0)
        {
            GCHandle.FromIntPtr(*ud).Free();
            *ud = 0;
        }

        return 0;
    }
}
