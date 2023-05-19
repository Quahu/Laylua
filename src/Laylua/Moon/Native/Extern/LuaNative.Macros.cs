namespace Laylua.Moon;

public static unsafe partial class LuaNative
{
    public static void* lua_getextraspace(lua_State* L)
    {
        return (byte*) L - sizeof(void*);
    }

    public static lua_Number lua_tonumber(lua_State* L, int idx)
    {
        return lua_tonumberx(L, idx, out _);
    }

    public static lua_Integer lua_tointeger(lua_State* L, int idx)
    {
        return lua_tointegerx(L, idx, out _);
    }

    public static void lua_pop(lua_State* L, int n)
    {
        lua_settop(L, -n - 1);
    }

    public static void lua_newtable(lua_State* L)
    {
        lua_createtable(L, 0, 0);
    }

    public static void lua_register(lua_State* L, string name, LuaCFunction fn)
    {
        lua_pushcfunction(L, fn);
        lua_setglobal(L, name);
    }

    public static void lua_pushcfunction(lua_State* L, LuaCFunction fn)
    {
        lua_pushcclosure(L, fn, 0);
    }

    public static bool lua_isfunction(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.Function;
    }

    public static bool lua_istable(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.Table;
    }

    public static bool lua_islightuserdata(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.LightUserData;
    }

    public static bool lua_isnil(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.Nil;
    }

    public static bool lua_isboolean(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.Boolean;
    }

    public static bool lua_isthread(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.Thread;
    }

    public static bool lua_isnone(lua_State* L, int idx)
    {
        return lua_type(L, idx) == LuaType.None;
    }

    public static bool lua_isnoneornil(lua_State* L, int idx)
    {
        return lua_type(L, idx) <= LuaType.Nil;
    }

    public static void lua_pushglobaltable(lua_State* L)
    {
        lua_rawgeti(L, LuaRegistry.Index, LuaRegistry.Indices.Globals);
    }

    public static LuaString lua_tostring(lua_State* L, int idx)
    {
        var ptr = _lua_tolstring(L, idx, out var len);
        return new LuaString(ptr, len);
    }

    public static void lua_insert(lua_State* L, int idx)
    {
        lua_rotate(L, idx, 1);
    }

    public static void lua_remove(lua_State* L, int idx)
    {
        lua_rotate(L, idx, -1);
        lua_pop(L, 1);
    }

    public static void lua_replace(lua_State* L, int idx)
    {
        lua_copy(L, -1, idx);
        lua_pop(L, 1);
    }

    /*
     *
     *  Compatibility
     *
     */
    public static void* lua_newuserdata(lua_State* L, nuint size)
    {
        return lua_newuserdatauv(L, size, 1);
    }

    public static LuaType lua_getuservalue(lua_State* L, int idx)
    {
        return lua_getiuservalue(L, idx, 1);
    }

    public static bool lua_setuservalue(lua_State* L, int idx)
    {
        return lua_setiuservalue(L, idx, 1);
    }
}
