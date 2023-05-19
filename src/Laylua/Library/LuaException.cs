using System;
using System.Diagnostics.CodeAnalysis;
using Laylua.Moon;

namespace Laylua;

public unsafe class LuaException : Exception
{
    public Lua Lua { get; }

    public LuaStatus? Status { get; }

    public LuaException(Lua lua)
        : this(lua, PopError(lua))
    { }

    public LuaException(Lua lua, string? message)
        : this(lua, null, message)
    { }

    public LuaException(Lua lua, LuaStatus? status)
        : this(lua, PopError(lua))
    {
        Status = status;
    }

    public LuaException(Lua lua, LuaStatus? status, string? message)
        : base(message)
    {
        Lua = lua;
        Status = status;
    }

    private static string? PopError(Lua lua)
    {
        return TryPopError(lua, out var error) ? error : null;
    }

    private static bool TryPopError(Lua lua, [MaybeNullWhen(false)] out string error)
    {
        var L = lua.GetStatePointer();
        if (!lua_isstring(L, -1))
        {
            error = null;
            return false;
        }

        error = lua_tostring(L, -1).ToString().Replace("\r", "").Replace("\n", "");
        lua_pop(L);
        return true;
    }
}
