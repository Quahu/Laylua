using System.Runtime.InteropServices;

namespace Laylua.Moon;

public static unsafe partial class LuaNative
{
    /*
     *
     * Libs
     *
     */
    [LuaExport]
    public static partial int luaopen_base(lua_State* L);

    public const string LUA_COLIBNAME = "coroutine";

    [LuaExport]
    public static partial int luaopen_coroutine(lua_State* L);

    public const string LUA_TABLIBNAME = "table";

    [LuaExport]
    public static partial int luaopen_table(lua_State* L);

    public const string LUA_IOLIBNAME = "io";

    [LuaExport]
    public static partial int luaopen_io(lua_State* L);

    public const string LUA_OSLIBNAME = "os";

    [LuaExport]
    public static partial int luaopen_os(lua_State* L);

    public const string LUA_STRLIBNAME = "string";

    [LuaExport]
    public static partial int luaopen_string(lua_State* L);

    public const string LUA_UTF8LIBNAME = "utf8";

    [LuaExport]
    public static partial int luaopen_utf8(lua_State* L);

    public const string LUA_MATHLIBNAME = "math";

    [LuaExport]
    public static partial int luaopen_math(lua_State* L);

    public const string LUA_DBLIBNAME = "debug";

    [LuaExport]
    public static partial int luaopen_debug(lua_State* L);

    public const string LUA_LOADLIBNAME = "package";

    [LuaExport]
    public static partial int luaopen_package(lua_State* L);

    [LuaExport]
    public static partial void luaL_openlibs(lua_State* L);
}
