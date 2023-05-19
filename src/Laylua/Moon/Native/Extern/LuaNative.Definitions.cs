using System.Runtime.InteropServices;

namespace Laylua.Moon;

public static partial class LuaNative
{
    public const lua_Number LuaVersionNumber = 504;

    public const string LuaVersionMajor = "5";

    public const string LuaVersionMinor = "4";

    public const string LuaVersion = "Lua " + LuaVersionMajor + "." + LuaVersionMinor;

    public const string DllName = "lua" + LuaVersionMajor + LuaVersionMinor;

    private const CallingConvention Cdecl = CallingConvention.Cdecl;

    public const int LUAI_MAXSTACK = 1000000;

    public const int LUAL_NUMSIZES = sizeof(lua_Integer) * 16 + sizeof(lua_Number);

    public const int LUA_MULTRET = -1;

    public static int lua_upvalueindex(int i)
    {
        return LuaRegistry.Index - i;
    }

    public const int LUA_MINSTACK = 20;

    public const int LUA_RIDX_LAST = LuaRegistry.Indices.Globals;

    public const int LUA_IDSIZE = 60;

    public const int LUA_NOREF = -2;
    public const int LUA_REFNIL = -1;
}
