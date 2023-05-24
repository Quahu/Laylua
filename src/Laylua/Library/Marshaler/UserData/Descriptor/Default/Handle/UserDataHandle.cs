using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

internal unsafe class UserDataHandle
{
    public const string UserDataTableName = "__laylua__internal_userdatacache";
    public const string WeakValueModeMetatableName = "__laylua__internal_weakvaluemode";

    public static readonly nuint Size = (nuint) IntPtr.Size * 2;

    public static IntPtr IdentifierPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LayluaNative.getPotentialPanicPtr;
    }

    public readonly Lua Lua;

    public readonly object Target;

    public readonly UserDataDescriptor Descriptor;

    public IntPtr GCPtr => (IntPtr) _gcHandle;

    private GCHandle _gcHandle;
    private bool _created;

    private static UserDataHandle _(lua_State* L)
    {
        var ptr = lua_touserdata(L, 1);
        return FromUserDataPointer(ptr);
    }

    private static readonly LuaCFunction __gcDelegate = L =>
    {
        var handle = _(L);
        handle._gcHandle.Free();
        handle.Lua.Marshaler.RemoveUserDataHandle(handle);
        return 0;
    };

    public UserDataHandle(Lua lua, object obj, UserDataDescriptor descriptor)
    {
        Guard.IsNotNull(lua);
        Guard.IsNotNull(obj);
        Guard.IsNotNull(descriptor);

        Lua = lua;
        Target = obj;
        Descriptor = descriptor;

        _gcHandle = GCHandle.Alloc(this);
    }

    public void Push()
    {
        var L = Lua.GetStatePointer();
        var top = lua_gettop(L);

        if (_created)
        {
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
        }

        try
        {
            var ptr = (IntPtr*) lua_newuserdata(L, Size);
            ptr[0] = GCPtr;
            ptr[1] = IdentifierPtr;

            PushMetatable();
            lua_setmetatable(L, -2);

            using (Lua.Stack.SnapshotCount())
            {
                if (!luaL_getsubtable(L, LuaRegistry.Index, UserDataTableName))
                {
                    if (luaL_getmetatable(L, WeakValueModeMetatableName).IsNoneOrNil())
                    {
                        lua_pop(L, 1);
                        lua_createtable(L, 0, 1);

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

        _created = true;
    }

    private void PushMetatable()
    {
        var L = Lua.GetStatePointer();
        var metatableName = $"__laylua_{Descriptor.MetatableName}";
        if (!luaL_getmetatable(L, metatableName).IsNoneOrNil())
            return;

        lua_pop(L, 1);
        lua_createtable(L, 0, 16);

        Descriptor.OnMetatableCreated(Lua, Lua.Stack[-1]);

        lua_pushstring(L, LuaMetatableKeysUtf8.__gc);
        lua_pushcfunction(L, __gcDelegate);
        lua_rawset(L, -3);

        lua_pushstring(L, LuaMetatableKeysUtf8.__metadata);
        lua_pushboolean(L, false);
        lua_rawset(L, -3);

        lua_pushvalue(L, -1);
        lua_setfield(L, LuaRegistry.Index, metatableName);
    }

    public static UserDataHandle FromUserDataPointer(void* ptr)
    {
        if (TryFromUserDataPointer(ptr, out var handle))
        {
            return handle;
        }

        throw new InvalidOperationException("The user data pointer is invalid.");
    }

    public static bool TryFromUserDataPointer(void* ptr, [MaybeNullWhen(false)] out UserDataHandle handle)
    {
        if (ptr == null)
        {
            handle = null;
            return false;
        }

        var gcPtr = *(IntPtr*) ptr;
        var gcHandle = GCHandle.FromIntPtr(gcPtr);
        handle = Unsafe.As<UserDataHandle>(gcHandle.Target)!;
        return true;
    }
}
