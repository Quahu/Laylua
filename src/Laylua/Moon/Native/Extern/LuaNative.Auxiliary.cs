using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

public static unsafe partial class LuaNative
{
    private const int POINTER_SIZE = 8;

    public const string LUA_GNAME = "_G";

    public partial struct luaL_Buffer
    {
        public const int LUAL_BUFFERSIZE = 16 * POINTER_SIZE * sizeof(lua_Number);
    }

    public const string LUA_LOADED_TABLE = "_LOADED";

    public const string LUA_PRELOAD_TABLE = "_PRELOAD";

    public struct luaL_Reg
    {
        public static luaL_Reg Null => new(null, null);

        public static luaL_Reg Placeholder(string name)
        {
            return new(name, null);
        }

        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string? name;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public LuaCFunction? func;

        public luaL_Reg(string? name, LuaCFunction? func)
        {
            this.name = name;
            this.func = func;
        }
    }

    /*
     *
     * Auxiliary
     *
     */
    [LuaExport(Throws = true)]
    public static partial void luaL_checkversion_(lua_State* L, lua_Number ver, nuint sz);

    public static void luaL_checkversion(lua_State* L)
    {
        luaL_checkversion_(L, LuaVersionNumber, LUAL_NUMSIZES);
    }

    [LuaExport(Throws = true)]
    public static partial int luaL_getmetafield(lua_State* L, int obj, [MarshalAs(UnmanagedType.LPStr)] string e);

    [LuaExport(Throws = true)]
    public static partial bool luaL_callmeta(lua_State* L, int obj, [MarshalAs(UnmanagedType.LPStr)] string e);

    [LuaExport(Throws = true)]
    public static partial LuaString luaL_tolstring(lua_State* L, int idx, out nuint len);

    public static LuaString luaL_tostring(lua_State* L, int idx)
    {
        return luaL_tolstring(L, idx, out _);
    }

    [LuaExport(Throws = true)]
    [DoesNotReturn]
    public static partial int luaL_argerror(lua_State* L, int arg, ReadOnlySpan<char> extramsg);

    [LuaExport(Throws = true)]
    [DoesNotReturn]
    public static partial int luaL_typeerror(lua_State* L, int arg, ReadOnlySpan<char> tname);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _luaL_checklstringDelegate(lua_State* L, int arg, out nuint len);

    [LuaExport(Throws = true)]
    private static _luaL_checklstringDelegate _luaL_checklstring = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? luaL_checklstring(lua_State* L, int arg, out nuint len)
    {
        var ptr = _luaL_checklstring(L, arg, out len);
        LayluaNative.ThrowIfPendingPanic();
        return FromStringPtr(ptr, len);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _luaL_optlstringDelegate(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string def, out nuint len);

    [LuaExport(Throws = true)]
    private static _luaL_optlstringDelegate _luaL_optlstring = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? luaL_optlstring(lua_State* L, int arg, string def, out nuint len)
    {
        var ptr = _luaL_optlstring(L, arg, def, out len);
        LayluaNative.ThrowIfPendingPanic();
        return FromStringPtr(ptr, len);
    }

    [LuaExport(Throws = true)]
    public static partial lua_Number luaL_checknumber(lua_State* L, int arg);

    [LuaExport(Throws = true)]
    public static partial lua_Number luaL_optnumber(lua_State* L, int arg, lua_Number def);

    [LuaExport(Throws = true)]
    public static partial lua_Integer luaL_checkinteger(lua_State* L, int arg);

    [LuaExport(Throws = true)]
    public static partial lua_Integer luaL_optinteger(lua_State* L, int arg, lua_Integer def);

    [LuaExport(Throws = true)]
    public static partial void luaL_checkstack(lua_State* L, int sz, [MarshalAs(UnmanagedType.LPUTF8Str)] string? msg);

    [LuaExport(Throws = true)]
    public static partial void luaL_checktype(lua_State* L, int arg, LuaType type);

    [LuaExport(Throws = true)]
    public static partial void luaL_checkany(lua_State* L, int arg);

    [LuaExport(Throws = true)]
    public static partial bool luaL_newmetatable(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    [LuaExport(Throws = true)]
    public static partial void luaL_setmetatable(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    [LuaExport(Throws = true)]
    public static partial void* luaL_testudata(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    [LuaExport(Throws = true)]
    public static partial void* luaL_checkudata(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    [LuaExport(Throws = true)]
    public static partial void luaL_where(lua_State* L, int lvl);

    // [DllImport(DllName, CallingConvention = CConv, EntryPoint = "luaL_error")]
    // public static extern int luaL_error(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string fmt, __arglist);

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    public static int luaL_error(lua_State* L, ReadOnlySpan<char> msg)
    {
        luaL_where(L, 1);
        lua_pushstring(L, msg);
        lua_concat(L, 2);
        return lua_error(L);
    }

    // TODO: string marshaling
    // [DllImport(DllName, CallingConvention = Cdecl)]
    // public static extern int luaL_checkoption(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string def, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPUTF8Str)] string[] lst);

    [LuaExport(Throws = true)]
    public static partial int luaL_fileresult(lua_State* L, int stat, [MarshalAs(UnmanagedType.LPStr)] string fname);

    [LuaExport(Throws = true)]
    public static partial int luaL_execresult(lua_State* L, int stat);

    [LuaExport(Throws = true)]
    public static partial int luaL_ref(lua_State* L, int t);

    [LuaExport]
    public static partial void luaL_unref(lua_State* L, int t, int reference);

    [LuaExport(Throws = true)]
    public static partial LuaStatus luaL_loadfilex(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename, [MarshalAs(UnmanagedType.LPStr)] string? mode);

    public static LuaStatus luaL_loadfile(lua_State* L, string filename)
    {
        return luaL_loadfilex(L, filename, null);
    }

    [LuaExport]
    public static partial LuaStatus luaL_loadbufferx(lua_State* L, byte* buff, nuint sz, ReadOnlySpan<char> name, ReadOnlySpan<char> mode);

    [LuaExport]
    public static partial LuaStatus luaL_loadstring(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string s);

    [LuaExport]
    public static partial lua_State* luaL_newstate();

    [LuaExport(Throws = true)]
    public static partial lua_Integer luaL_len(lua_State* L, int idx);

    [LuaExport]
    public static partial void luaL_addgsub(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] string p, [MarshalAs(UnmanagedType.LPUTF8Str)] string r);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshaler))]
    private delegate string _luaL_gsubDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] string p, [MarshalAs(UnmanagedType.LPUTF8Str)] string r);

    [LuaExport(Throws = true)]
    private static _luaL_gsubDelegate _luaL_gsub = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string luaL_gsub(lua_State* L, string s, string p, string r)
    {
        var result = _luaL_gsub(L, s, p, r);
        LayluaNative.ThrowIfPendingPanic();
        return result;
    }

    [LuaExport(Throws = true)]
    public static partial void luaL_setfuncs(lua_State* L, [MarshalAs(UnmanagedType.LPArray)] luaL_Reg[] l, int nup);

    [LuaExport(Throws = true)]
    public static partial bool luaL_getsubtable(lua_State* L, int idx, [MarshalAs(UnmanagedType.LPUTF8Str)] string fname);

    [LuaExport(Throws = true)]
    public static partial void luaL_traceback(lua_State* L, lua_State* L1, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg, int level);

    [LuaExport(Throws = true)]
    public static partial void luaL_requiref(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string modname, LuaCFunction openf, bool glb);

    /*
     *
     * Some useful macros
     *
     */
    public static void luaL_newlibtable(lua_State* L, luaL_Reg[] l)
    {
        lua_createtable(L, 0, l.Length - 1);
    }

    public static void luaL_newlib(lua_State* L, luaL_Reg[] l)
    {
        luaL_newlibtable(L, l);
        luaL_setfuncs(L, l, 0);
    }

    public static string? luaL_checkstring(lua_State* L, int arg)
    {
        return luaL_checklstring(L, arg, out _);
    }

    public static string? luaL_optstring(lua_State* L, int arg, string def)
    {
        return luaL_optlstring(L, arg, def, out _);
    }

    public static LuaString luaL_typename(lua_State* L, int idx)
    {
        return lua_typename(L, lua_type(L, idx));
    }

    public static LuaStatus luaL_dofile(lua_State* L, string filename)
    {
        var status = luaL_loadfile(L, filename);
        if (status != LuaStatus.Ok)
            return status;

        return lua_pcall(L, 0, LUA_MULTRET, 0);
    }

    public static LuaStatus luaL_dostring(lua_State* L, string s)
    {
        var status = luaL_loadstring(L, s);
        if (status != LuaStatus.Ok)
            return status;

        return lua_pcall(L, 0, LUA_MULTRET, 0);
    }

    public static LuaType luaL_getmetatable(lua_State* L, ReadOnlySpan<char> n)
    {
        return lua_getfield(L, LuaRegistry.Index, n);
    }

    public static LuaStatus luaL_loadbuffer(lua_State* L, byte* buff, nuint sz, ReadOnlySpan<char> name)
    {
        return luaL_loadbufferx(L, buff, sz, name, null);
    }

    /*
     *
     * Generic Buffer manipulation
     *
     */
    [StructLayout(LayoutKind.Sequential, Pack = POINTER_SIZE)]
    public partial struct luaL_Buffer
    {
        public byte* b;
        public nuint size;
        public nuint n;
        public lua_State* L;

        public fixed byte _b[LUAL_BUFFERSIZE];
    }

    public static nuint luaL_bufflen(luaL_Buffer* B)
    {
        return B->n;
    }

    public static byte* luaL_buffaddr(luaL_Buffer* B)
    {
        return B->b;
    }

    public static void luaL_addchar(luaL_Buffer* B, byte c)
    {
        if (B->n >= B->size)
            luaL_prepbuffsize(B, 1);

        B->b[B->n++] = c;
    }

    public static nuint luaL_addsize(luaL_Buffer* B, nuint sz)
    {
        return B->n += sz;
    }

    public static nuint luaL_buffsub(luaL_Buffer* B, nuint sz)
    {
        return B->n -= sz;
    }

    [LuaExport]
    public static partial void luaL_buffinit(lua_State* L, out luaL_Buffer B);

    [LuaExport]
    public static partial byte* luaL_prepbuffsize(luaL_Buffer* B, nuint sz);

    [LuaExport]
    public static partial void luaL_addlstring(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, nuint l);

    [LuaExport]
    public static partial void luaL_addstring(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s);

    [LuaExport]
    public static partial void luaL_addvalue(luaL_Buffer* B);

    [LuaExport]
    public static partial void luaL_pushresult(luaL_Buffer* B);

    [LuaExport]
    public static partial void luaL_pushresultsize(luaL_Buffer* B, nuint sz);

    [LuaExport]
    public static partial byte* luaL_buffinitsize(lua_State* L, out luaL_Buffer B, nuint sz);

    public static void luaL_prepbuffer(luaL_Buffer* B)
    {
        luaL_prepbuffsize(B, 1024);
    }
}
