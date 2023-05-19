using System.Runtime.InteropServices;

namespace Laylua.Moon;

public static unsafe partial class LuaNative
{
    /*
     *
     * Libs
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_base(lua_State* L);

    public const string LUA_COLIBNAME = "coroutine";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_coroutine(lua_State* L);

    public const string LUA_TABLIBNAME = "table";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_table(lua_State* L);

    public const string LUA_IOLIBNAME = "io";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_io(lua_State* L);

    public const string LUA_OSLIBNAME = "os";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_os(lua_State* L);

    public const string LUA_STRLIBNAME = "string";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_string(lua_State* L);

    public const string LUA_UTF8LIBNAME = "utf8";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_utf8(lua_State* L);

    public const string LUA_MATHLIBNAME = "math";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_math(lua_State* L);

    public const string LUA_DBLIBNAME = "debug";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_debug(lua_State* L);

    public const string LUA_LOADLIBNAME = "package";

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaopen_package(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_openlibs(lua_State* L);
}
