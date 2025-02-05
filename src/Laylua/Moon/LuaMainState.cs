using System;
using System.Globalization;
using Qommon;

namespace Laylua.Moon;

internal sealed unsafe class LuaMainState : LuaState
{
    public override LuaAllocator? Allocator { get; }

    public override LuaGC GC { get; }

    protected override lua_State* LCore => _L;

    internal object? State
    {
        get => _layluaState.State;
        set => _layluaState.State = value;
    }

    private LuaAllocFunction? _safeAllocFunction;

    private LayluaState _layluaState = null!;
    private lua_State* _L;

    internal LuaMainState(LuaAllocator? allocator)
    {
        if (allocator == null)
        {
            _L = luaL_newstate();
        }
        else
        {
            _L = lua_newstate(WrapAllocFunction(allocator), null);
            Allocator = allocator;
        }

        ValidateNewState();
        InitializeLayluaState();

        GC = new(this);
    }

    private void ValidateNewState()
    {
        if (_L == null)
        {
            Throw.InvalidOperationException("Failed to create a new Lua state.");
        }

        var version = lua_version(_L).ToString(CultureInfo.InvariantCulture);
        if (version.Length != 3 || version[0] != LuaVersionMajor[0] || version[2] != LuaVersionMinor[0])
        {
            Throw.InvalidOperationException($"Invalid Lua version loaded. Expected '{LuaVersion}'.");
        }
    }

    private void InitializeLayluaState()
    {
        if (!LayluaState.TryFromExtraSpace(_L, out var state))
        {
            var panicPtr = LayluaNative.InitializePanic();
            state = LayluaState.Create(panicPtr);
            *(IntPtr*) lua_getextraspace(_L) = state.GCPtr;
            lua_atpanic(_L, (delegate* unmanaged[Cdecl]<lua_State*, int>) panicPtr);
        }

        _layluaState = state;
    }

    internal LuaAllocFunction WrapAllocFunction(LuaAllocator allocator)
    {
        Guard.IsNotNull(allocator);

        _safeAllocFunction = (_, ptr, osize, nsize) =>
        {
            return allocator.Allocate(ptr, osize, nsize);
        };

        return _safeAllocFunction;
    }

    internal override void Close()
    {
        if (_L == null)
            return;

        lua_close(_L);
        _layluaState.Dispose();
        _L = null;
    }
}
