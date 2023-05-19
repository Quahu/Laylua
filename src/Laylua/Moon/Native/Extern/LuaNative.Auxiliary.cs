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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool _luaL_checkversion_Delegate(lua_State* L, lua_Number ver, nuint sz);

    private static _luaL_checkversion_Delegate _luaL_checkversion_ = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_checkversion_(lua_State* L, lua_Number ver, nuint sz)
    {
        _luaL_checkversion_(L, ver, sz);
    }

    public static void luaL_checkversion(lua_State* L)
    {
        _luaL_checkversion_(L, LuaVersionNumber, LUAL_NUMSIZES);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int luaL_getmetafield(lua_State* L, int obj, [MarshalAs(UnmanagedType.LPStr)] string e);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate bool _luaL_callmetaDelegate(lua_State* L, int obj, [MarshalAs(UnmanagedType.LPStr)] string e);

    private static _luaL_callmetaDelegate _luaL_callmeta = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool luaL_callmeta(lua_State* L, int obj, string e)
    {
        return _luaL_callmeta(L, obj, e);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _luaL_tolstringDelegate(lua_State* L, int idx, out nuint len);

    private static _luaL_tolstringDelegate _luaL_tolstring = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaString luaL_tolstring(lua_State* L, int idx, out nuint len)
    {
        var ptr = _luaL_tolstring(L, idx, out len);
        return new LuaString(ptr, len);
    }

    public static LuaString luaL_tostring(lua_State* L, int idx)
    {
        var ptr = _luaL_tolstring(L, idx, out var len);
        return new LuaString(ptr, len);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _luaL_argerrorDelegate(lua_State* L, int arg, byte* extramsg);

    private static _luaL_argerrorDelegate _luaL_argerror = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
#nullable disable
    public static int luaL_argerror(lua_State* L, int arg, ReadOnlySpan<char> extramsg)
    {
        using (var bytes = ToCString(extramsg))
        {
            fixed (byte* ptr = bytes)
            {
                return _luaL_argerror(L, arg, ptr);
            }
        }
    }
#nullable enable

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _luaL_typeerrorDelegate(lua_State* L, int arg, byte* tname);

    private static _luaL_typeerrorDelegate _luaL_typeerror = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
#nullable disable
    public static int luaL_typeerror(lua_State* L, int arg, ReadOnlySpan<char> tname)
    {
        using (var bytes = ToCString(tname))
        {
            fixed (byte* ptr = bytes)
            {
                return _luaL_typeerror(L, arg, ptr);
            }
        }
    }
#nullable enable

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _luaL_checklstringDelegate(lua_State* L, int arg, out nuint len);

    private static _luaL_checklstringDelegate _luaL_checklstring = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? luaL_checklstring(lua_State* L, int arg, out nuint len)
    {
        var ptr = _luaL_checklstring(L, arg, out len);
        return FromStringPtr(ptr, len);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _luaL_optlstringDelegate(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string def, out nuint len);

    private static _luaL_optlstringDelegate _luaL_optlstring = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? luaL_optlstring(lua_State* L, int arg, string def, out nuint len)
    {
        var ptr = _luaL_optlstring(L, arg, def, out len);
        return FromStringPtr(ptr, len);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate lua_Number _luaL_checknumberDelegate(lua_State* L, int arg);

    private static _luaL_checknumberDelegate _luaL_checknumber = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_Number luaL_checknumber(lua_State* L, int arg)
    {
        return _luaL_checknumber(L, arg);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate lua_Number _luaL_optnumberDelegate(lua_State* L, int arg, lua_Number def);

    private static _luaL_optnumberDelegate _luaL_optnumber = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_Number luaL_optnumber(lua_State* L, int arg, lua_Number def)
    {
        return _luaL_optnumber(L, arg, def);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate lua_Integer _luaL_checkintegerDelegate(lua_State* L, int arg);

    private static _luaL_checkintegerDelegate _luaL_checkinteger = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_Integer luaL_checkinteger(lua_State* L, int arg)
    {
        return _luaL_checkinteger(L, arg);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate lua_Integer _luaL_optintegerDelegate(lua_State* L, int arg, lua_Integer def);

    private static _luaL_optintegerDelegate _luaL_optinteger = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_Integer luaL_optinteger(lua_State* L, int arg, lua_Integer def)
    {
        return _luaL_optinteger(L, arg, def);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _luaL_checkstackDelegate(lua_State* L, int sz, [MarshalAs(UnmanagedType.LPUTF8Str)] string? msg);

    private static _luaL_checkstackDelegate _luaL_checkstack = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_checkstack(lua_State* L, int sz, string? msg)
    {
        _luaL_checkstack(L, sz, msg);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _luaL_checktypeDelegate(lua_State* L, int arg, LuaType type);

    private static _luaL_checktypeDelegate _luaL_checktype = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_checktype(lua_State* L, int arg, LuaType type)
    {
        _luaL_checktype(L, arg, type);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _luaL_checkanyDelegate(lua_State* L, int arg);

    private static _luaL_checkanyDelegate _luaL_checkany = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_checkany(lua_State* L, int arg)
    {
        _luaL_checkany(L, arg);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate bool _luaL_newmetatableDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    private static _luaL_newmetatableDelegate _luaL_newmetatable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool luaL_newmetatable(lua_State* L, string tname)
    {
        return _luaL_newmetatable(L, tname);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _luaL_setmetatableDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    private static _luaL_setmetatableDelegate _luaL_setmetatable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_setmetatable(lua_State* L, string tname)
    {
        _luaL_setmetatable(L, tname);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void* _luaL_testudataDelegate(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    private static _luaL_testudataDelegate _luaL_testudata = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* luaL_testudata(lua_State* L, int arg, string tname)
    {
        return _luaL_testudata(L, arg, tname);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void* _luaL_checkudataDelegate(lua_State* L, int arg, [MarshalAs(UnmanagedType.LPUTF8Str)] string tname);

    private static _luaL_checkudataDelegate _luaL_checkudata = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* luaL_checkudata(lua_State* L, int arg, string tname)
    {
        return _luaL_checkudata(L, arg, tname);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _luaL_whereDelegate(lua_State* L, int lvl);

    private static _luaL_whereDelegate _luaL_where = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_where(lua_State* L, int lvl)
    {
        _luaL_where(L, lvl);
    }

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

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _luaL_fileresultDelegate(lua_State* L, int stat, [MarshalAs(UnmanagedType.LPStr)] string fname);

    private static _luaL_fileresultDelegate _luaL_fileresult = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int luaL_fileresult(lua_State* L, int stat, string fname)
    {
        return _luaL_fileresult(L, stat, fname);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _luaL_execresultDelegate(lua_State* L, int stat);

    private static _luaL_execresultDelegate _luaL_execresult = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int luaL_execresult(lua_State* L, int stat)
    {
        return _luaL_execresult(L, stat);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int _luaL_refDelegate(lua_State* L, int t);

    private static _luaL_refDelegate _luaL_ref = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int luaL_ref(lua_State* L, int t)
    {
        return _luaL_ref(L, t);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_unref(lua_State* L, int t, int @ref);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate LuaStatus _luaL_loadfilexDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename, [MarshalAs(UnmanagedType.LPStr)] string? mode);

    private static _luaL_loadfilexDelegate _luaL_loadfilex = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaStatus luaL_loadfilex(lua_State* L, string filename, string? mode)
    {
        return _luaL_loadfilex(L, filename, mode);
    }

    public static LuaStatus luaL_loadfile(lua_State* L, string filename)
    {
        return _luaL_loadfilex(L, filename, null);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaStatus luaL_loadbufferx(lua_State* L, byte* buff, nuint sz, [MarshalAs(UnmanagedType.LPUTF8Str)] string? name, [MarshalAs(UnmanagedType.LPStr)] string? mode);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaStatus luaL_loadstring(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string s);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_State* luaL_newstate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate lua_Integer _luaL_lenDelegate(lua_State* L, int idx);

    private static _luaL_lenDelegate _luaL_len = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_Integer luaL_len(lua_State* L, int idx)
    {
        return _luaL_len(L, idx);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_addgsub(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] string p, [MarshalAs(UnmanagedType.LPUTF8Str)] string r);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8StringMarshaler))]
    private delegate string _luaL_gsubDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] string p, [MarshalAs(UnmanagedType.LPUTF8Str)] string r);

    private static _luaL_gsubDelegate _luaL_gsub = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string luaL_gsub(lua_State* L, string s, string p, string r)
    {
        return _luaL_gsub(L, s, p, r);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void _luaL_setfuncsDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPArray)] luaL_Reg[] l, int nup);

    private static _luaL_setfuncsDelegate _luaL_setfuncs = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_setfuncs(lua_State* L, luaL_Reg[] l, int nup)
    {
        _luaL_setfuncs(L, l, nup);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool _luaL_getsubtableDelegate(lua_State* L, int idx, [MarshalAs(UnmanagedType.LPUTF8Str)] string fname);

    private static _luaL_getsubtableDelegate _luaL_getsubtable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool luaL_getsubtable(lua_State* L, int idx, string fname)
    {
        return _luaL_getsubtable(L, idx, fname);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void _luaL_tracebackDelegate(lua_State* L, lua_State* L1, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg, int level);

    private static _luaL_tracebackDelegate _luaL_traceback = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_traceback(lua_State* L, lua_State* L1, string msg, int level)
    {
        _luaL_traceback(L, L1, msg, level);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void _luaL_requirefDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string modname, LuaCFunction openf, bool glb);

    private static _luaL_requirefDelegate _luaL_requiref = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void luaL_requiref(lua_State* L, string modname, LuaCFunction openf, bool glb)
    {
        _luaL_requiref(L, modname, openf, glb);
    }

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

    public static string luaL_typename(lua_State* L, int idx)
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

    public static LuaStatus luaL_loadbuffer(lua_State* L, byte* buff, nuint sz, string? name)
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

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_buffinit(lua_State* L, out luaL_Buffer B);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern byte* luaL_prepbuffsize(luaL_Buffer* B, nuint sz);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_addlstring(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s, nuint l);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_addstring(luaL_Buffer* B, [MarshalAs(UnmanagedType.LPUTF8Str)] string s);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_addvalue(luaL_Buffer* B);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_pushresult(luaL_Buffer* B);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void luaL_pushresultsize(luaL_Buffer* B, nuint sz);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern byte* luaL_buffinitsize(lua_State* L, out luaL_Buffer B, nuint sz);

    public static void luaL_prepbuffer(luaL_Buffer* B)
    {
        luaL_prepbuffsize(B, 1024);
    }
}
