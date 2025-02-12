﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public unsafe class UserDataHandle
{
    internal static IntPtr IdentifierPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LayluaNative.getPotentialPanicPtr;
    }

    internal const string UserDataTableName = "__laylua__internal_userdatacache";
    internal const string WeakValueModeMetatableName = "__laylua__internal_weakvaluemode";
    internal const string SharedMetatableName = "__laylua__internal_shared"; // shared metatable for when no descriptor is set

    internal LuaThread MainThread { get; }

    internal IntPtr GCPtr => (IntPtr) _gcHandle;

    private GCHandle _gcHandle;

    internal UserDataHandle(LuaThread thread)
    {
        MainThread = thread.MainThread;
        _gcHandle = GCHandle.Alloc(this);
    }

    private static readonly LuaCFunction __gc = L =>
    {
        var handle = FromStackIndex(L, 1);
        handle._gcHandle.Free();
        handle.MainThread.Marshaler.RemoveUserDataHandle(handle);
        return 0;
    };

    /// <summary>
    ///     Attempts to get the type of the value of this handle.
    /// </summary>
    public virtual bool TryGetType([MaybeNullWhen(false)] out Type type)
    {
        type = default;
        return false;
    }

    /// <summary>
    ///     Attempts to get the value of this handle.
    /// </summary>
    public virtual bool TryGetValue<TTarget>([MaybeNullWhen(false)] out TTarget value)
    {
        value = default;
        return false;
    }

    internal void Push(LuaThread thread)
    {
        var L = thread.State.L;
        var top = lua_gettop(L);

        try
        {
            luaL_getsubtable(L, LuaRegistry.Index, UserDataTableName);

            if (!lua_rawgetp(L, -1, GCPtr.ToPointer()).IsNoneOrNil())
            {
                lua_remove(L, -2);
                return;
            }

            lua_settop(L, top);
        }
        catch
        {
            lua_settop(L, top);
            throw;
        }

        try
        {
            lua_newuserdatauv(L, 0, 2);

            lua_pushlightuserdata(L, (void*) IdentifierPtr);
            if (!lua_setiuservalue(L, -2, 1))
            {
                Throw.InvalidOperationException($"Failed to set the userdata {nameof(IdentifierPtr)}.");
            }

            lua_pushlightuserdata(L, (void*) GCPtr);
            if (!lua_setiuservalue(L, -2, 2))
            {
                Throw.InvalidOperationException($"Failed to set the userdata {nameof(GCPtr)}.");
            }

            PushMetatable(thread);
            lua_setmetatable(L, -2);

            using (thread.Stack.SnapshotCount())
            {
                if (!luaL_getsubtable(L, LuaRegistry.Index, UserDataTableName))
                {
                    if (luaL_newmetatable(L, WeakValueModeMetatableName))
                    {
                        lua_pushstring(L, LuaMetatableKeysUtf8.__mode);
                        lua_pushstring(L, "v"u8);
                        lua_rawset(L, -3);
                    }

                    lua_setmetatable(L, -2);
                }

                lua_pushvalue(L, -2);
                lua_rawsetp(L, -2, GCPtr.ToPointer());
            }
        }
        catch
        {
            lua_settop(L, top);
            throw;
        }
    }

    private void PushMetatable(LuaThread thread)
    {
        var L = thread.State.L;
        var descriptor = GetDescriptor();
        if (descriptor != null)
        {
            var metatableName = $"__laylua_userdata_{descriptor.MetatableName}";
            if (luaL_getmetatable(L, metatableName).IsNoneOrNil())
            {
                lua_pop(L, 1);
                lua_createtable(L, 0, 16);

                descriptor.OnMetatableCreated(thread, thread.Stack[-1]);

                lua_pushvalue(L, -1);
                lua_setfield(L, LuaRegistry.Index, metatableName);
            }
        }
        else
        {
            luaL_newmetatable(L, SharedMetatableName);
        }

        lua_pushstring(L, LuaMetatableKeysUtf8.__gc);
        lua_pushcfunction(L, __gc);
        lua_rawset(L, -3);

        lua_pushstring(L, LuaMetatableKeysUtf8.__metadata);
        lua_pushboolean(L, false);
        lua_rawset(L, -3);
    }

    internal virtual UserDataDescriptor? GetDescriptor()
    {
        return null;
    }

    internal static UserDataHandle
        FromStackIndex(lua_State* L, int stackIndex)
    {
        if (!TryFromStackIndex(L, stackIndex, out var handle))
        {
            Throw.ArgumentException("The value is not a valid userdata handle.", nameof(stackIndex));
        }

        return handle;
    }

    internal static bool TryFromStackIndex(lua_State* L, int stackIndex, [MaybeNullWhen(false)] out UserDataHandle handle)
    {
        stackIndex = lua_absindex(L, stackIndex);
        var top = lua_gettop(L);
        try
        {
            if (lua_type(L, stackIndex) == LuaType.UserData
                && lua_getiuservalue(L, stackIndex, 1) == LuaType.LightUserData
                && lua_touserdata(L, -1) == (void*) IdentifierPtr
                && lua_getiuservalue(L, stackIndex, 2) == LuaType.LightUserData)
            {
                var gcPtr = lua_touserdata(L, -1);
                var gcHandle = GCHandle.FromIntPtr((IntPtr) gcPtr);
                handle = Unsafe.As<UserDataHandle>(gcHandle.Target)!;
                return true;
            }
        }
        finally
        {
            lua_settop(L, top);
        }

        handle = null;
        return false;
    }
}
