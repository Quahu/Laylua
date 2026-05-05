using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

/// <summary>
///     Represents the unmanaged methods and fields
///     used for interacting with Lua.
/// </summary>
public static unsafe partial class LuaNative
{
    /*
     *
     *  State manipulation
     *
     */
    [LuaExport]
    public static partial lua_State* lua_newstate(LuaAllocFunction f, void* ud);

    [LuaExport]
    public static partial void lua_close(lua_State* L);

    [LuaExport(Throws = true)]
    public static partial lua_State* lua_newthread(lua_State* L);

    [LuaExport]
    public static partial LuaStatus lua_resetthread(lua_State* L);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr _lua_atpanicDelegate(lua_State* L, IntPtr panicf);

    [LuaExport]
    private static _lua_atpanicDelegate _lua_atpanic = null!;

    public static LuaCFunction lua_atpanic(lua_State* L, LuaCFunction panicf)
    {
        var resultPtr = _lua_atpanic(L, Marshal.GetFunctionPointerForDelegate(panicf));
        return Marshal.GetDelegateForFunctionPointer<LuaCFunction>(resultPtr);
    }

    public static delegate* unmanaged[Cdecl]<lua_State*, int> lua_atpanic(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> panicf)
    {
        return (delegate* unmanaged[Cdecl]<lua_State*, int>) (void*) _lua_atpanic(L, (IntPtr) panicf);
    }

    [LuaExport]
    public static partial lua_Number lua_version(lua_State* L);

    /*
     *
     *  Basic stack manipulation
     *
     */
    [LuaExport]
    public static partial int lua_absindex(lua_State* L, int idx);

    [LuaExport]
    public static partial int lua_gettop(lua_State* L);

    [LuaExport(Throws = true)]
    public static partial void lua_settop(lua_State* L, int idx);

    [LuaExport]
    public static partial void lua_pushvalue(lua_State* L, int idx);

    [LuaExport]
    public static partial void lua_rotate(lua_State* L, int idx, int n);

    [LuaExport]
    public static partial void lua_copy(lua_State* L, int fromidx, int toidx);

    [LuaExport]
    public static partial bool lua_checkstack(lua_State* L, int n);

    [LuaExport]
    public static partial void lua_xmove(lua_State* from, lua_State* to, int n);

    /*
     *
     *  Access functions (stack -> C)
     *
     */
    [LuaExport]
    public static partial bool lua_isnumber(lua_State* L, int idx);

    [LuaExport]
    public static partial bool lua_isstring(lua_State* L, int idx);

    [LuaExport]
    public static partial bool lua_iscfunction(lua_State* L, int idx);

    [LuaExport]
    public static partial bool lua_isinteger(lua_State* L, int idx);

    [LuaExport]
    public static partial bool lua_isuserdata(lua_State* L, int idx);

    [LuaExport]
    public static partial LuaType lua_type(lua_State* L, int idx);

    [LuaExport]
    public static partial LuaString lua_typename(lua_State* L, LuaType tp);

    [LuaExport]
    public static partial lua_Number lua_tonumberx(lua_State* L, int idx, out bool isnum);

    [LuaExport]
    public static partial lua_Integer lua_tointegerx(lua_State* L, int idx, out bool isnum);

    [LuaExport]
    public static partial bool lua_toboolean(lua_State* L, int idx);

    [LuaExport]
    public static partial LuaString lua_tolstring(lua_State* L, int idx, out nuint len);

    [LuaExport]
    public static partial lua_Unsigned lua_rawlen(lua_State* L, int idx);

    [LuaExport]
    public static partial LuaCFunction? lua_tocfunction(lua_State* L, int idx);

    [LuaExport]
    public static partial void* lua_touserdata(lua_State* L, int idx);

    [LuaExport]
    public static partial lua_State* lua_tothread(lua_State* L, int idx);

    [LuaExport]
    public static partial void* lua_topointer(lua_State* L, int idx);

    /*
     *
     *  Comparison and arithmetic functions
     *
     */
    [LuaExport(Throws = true)]
    public static partial void lua_arith(lua_State* L, LuaOperation op);

    [LuaExport]
    public static partial bool lua_rawequal(lua_State* L, int idx1, int idx2);

    [LuaExport(Throws = true)]
    public static partial bool lua_compare(lua_State* L, int idx1, int idx2, LuaComparison op);

    /*
     *
     *  Push functions (C -> stack)
     *
     */
    [LuaExport]
    public static partial void lua_pushnil(lua_State* L);

    [LuaExport]
    public static partial void lua_pushnumber(lua_State* L, lua_Number n);

    [LuaExport]
    public static partial void lua_pushinteger(lua_State* L, lua_Integer n);

    //[DllImport(DllName, CallingConvention = Cdecl, EntryPoint = "lua_pushlstring")]
    //private static extern byte* _lua_pushlstring(lua_State* L, byte* s, nuint len);

    // [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_pushstring))]
    // private static extern byte* _lua_pushstring(lua_State* L, byte* s);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _lua_pushlstringDelegate(lua_State* L, byte* s, nuint len);

    [LuaExport("lua_pushlstring", Throws = true)]
    private static _lua_pushlstringDelegate _lua_pushlstring = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte* lua_pushstring(lua_State* L, string? s)
    {
        if (s == null)
        {
            lua_pushnil(L);
            return null;
        }

        return lua_pushstring(L, s.AsSpan());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte* lua_pushstring(lua_State* L, ReadOnlySpan<char> s)
    {
        if (s.Length == 0)
        {
            byte empty = 0;
            var emptyResult = _lua_pushlstring(L, &empty, 0);
            LayluaNative.ThrowIfPendingPanic();
            return emptyResult;
        }

        using (var bytes = ToLString(s, out var len))
        {
            fixed (byte* ptr = bytes)
            {
                var result = _lua_pushlstring(L, ptr, len);
                LayluaNative.ThrowIfPendingPanic();
                return result;
            }
        }
    }

    public static byte* lua_pushstring(lua_State* L, LuaString s)
    {
        return _lua_pushlstring(L, s.Pointer, s.Length);
    }

    public static byte* lua_pushstring(lua_State* L, ReadOnlySpan<byte> s)
    {
        if (s.Length == 0)
        {
            byte empty = 0;
            return _lua_pushlstring(L, &empty, 0);
        }

        fixed (byte* ptr = s)
        {
            return _lua_pushlstring(L, ptr, (nuint) s.Length);
        }
    }

    // REASON: __arglist sucks
    // [DllImport(DllName, CallingConvention = CConv)]
    // [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    // public static extern string lua_pushvfstring(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string fmt, __arglist);

    // REASON: __arglist sucks
    // [DllImport(DllName, CallingConvention = CConv)]
    // [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    // public static extern string lua_pushfstring(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string fmt, __arglist);

    // [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_pushcclosure))]
    // private static extern void _lua_pushcclosure(lua_State* L, IntPtr fn, int n);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_pushcclosureDelegate(lua_State* L, IntPtr fn, int n);

    [LuaExport(Throws = true)]
    private static _lua_pushcclosureDelegate _lua_pushcclosure = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_pushcclosure(lua_State* L, LuaCFunction fn, int n)
    {
        var wrapperPtr = LayluaNative.CreateLuaCFunctionWrapper(fn);
        _lua_pushcclosure(L, wrapperPtr, n);
        LayluaNative.ThrowIfPendingPanic();
    }

    [LuaExport]
    public static partial void lua_pushboolean(lua_State* L, bool b);

    [LuaExport]
    public static partial void lua_pushlightuserdata(lua_State* L, void* p);

    [LuaExport]
    public static partial bool lua_pushthread(lua_State* L);

    /*
     *
     *  Get functions (Lua -> stack)
     *
     */
    [LuaExport(Throws = true)]
    public static partial LuaType lua_getglobal(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [LuaExport(Throws = true)]
    public static partial LuaType lua_gettable(lua_State* L, int idx);

    [LuaExport(Throws = true)]
    public static partial LuaType lua_getfield(lua_State* L, int idx, ReadOnlySpan<char> k);

    [LuaExport(Throws = true)]
    public static partial LuaType lua_geti(lua_State* L, int idx, lua_Integer n);

    [LuaExport]
    public static partial LuaType lua_rawget(lua_State* L, int idx);

    [LuaExport]
    public static partial LuaType lua_rawgeti(lua_State* L, int idx, lua_Integer n);

    [LuaExport]
    public static partial LuaType lua_rawgetp(lua_State* L, int idx, void* p);

    [LuaExport(Throws = true)]
    public static partial void lua_createtable(lua_State* L, int narr, int nrec);

    [LuaExport(Throws = true)]
    public static partial void* lua_newuserdatauv(lua_State* L, nuint size, int nuvalue);

    [LuaExport]
    public static partial bool lua_getmetatable(lua_State* L, int objindex);

    [LuaExport]
    public static partial LuaType lua_getiuservalue(lua_State* L, int idx, int n);

    /*
     *
     *  Set functions (stack -> Lua)
     *
     */
    [LuaExport(Throws = true)]
    public static partial void lua_setglobal(lua_State* L, ReadOnlySpan<char> name);

    [LuaExport(Throws = true)]
    public static partial void lua_settable(lua_State* L, int idx);

    [LuaExport(Throws = true)]
    public static partial void lua_setfield(lua_State* L, int idx, ReadOnlySpan<char> k);

    [LuaExport(Throws = true)]
    public static partial void lua_seti(lua_State* L, int idx, lua_Integer n);

    [LuaExport(Throws = true)]
    public static partial void lua_rawset(lua_State* L, int idx);

    [LuaExport(Throws = true)]
    public static partial void lua_rawseti(lua_State* L, int idx, lua_Integer n);

    [LuaExport(Throws = true)]
    public static partial void lua_rawsetp(lua_State* L, int idx, void* p);

    [LuaExport]
    public static partial void lua_setmetatable(lua_State* L, int objindex);

    [LuaExport]
    public static partial bool lua_setiuservalue(lua_State* L, int idx, int n);

    /*
     *
     *  'load' and 'call' functions (load and run Lua code)
     *
     */
    [LuaExport(Throws = true)]
    public static partial void lua_callk(lua_State* L, int nargs, int nresults, void* ctx, LuaKFunction? k);

    public static void lua_call(lua_State* L, int nargs, int nresults)
    {
        lua_callk(L, nargs, nresults, null, null);
    }

    [LuaExport("lua_pcallk", PCall = true)]
    private static partial LuaStatus lua_pcallk_raw(lua_State* L, int nargs, int nresults, int errfunc, void* ctx, IntPtr k);

    public static LuaStatus lua_pcallk(lua_State* L, int nargs, int nresults, int errfunc, void* ctx, LuaKFunction? k)
    {
        var wrapperPtr = k != null
            ? LayluaNative.CreateLuaKFunctionWrapper(k)
            : IntPtr.Zero;

        return lua_pcallk_raw(L, nargs, nresults, errfunc, ctx, wrapperPtr);
    }

    public static LuaStatus lua_pcall(lua_State* L, int nargs, int nresults, int errfunc)
    {
        return lua_pcallk(L, nargs, nresults, errfunc, null, null);
    }

    [LuaExport("lua_load", PCall = true)]
    private static partial LuaStatus lua_load_raw(lua_State* L, void* reader, void* data, ReadOnlySpan<char> chunkname, ReadOnlySpan<char> mode);

    public static LuaStatus lua_load(lua_State* L, LuaReaderFunction reader, void* data, ReadOnlySpan<char> chunkname, ReadOnlySpan<char> mode)
    {
        return lua_load_raw(L, (void*) Marshal.GetFunctionPointerForDelegate(reader), data, chunkname, mode);
    }

    public static LuaStatus lua_load(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, void*, nuint*, byte*> reader, void* data, ReadOnlySpan<char> chunkname, ReadOnlySpan<char> mode)
    {
        return lua_load_raw(L, reader, data, chunkname, mode);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int _lua_dumpDelegate(lua_State* L, IntPtr writer, void* data, bool strip);

    [LuaExport]
    private static _lua_dumpDelegate _lua_dump = null!;

    public static int lua_dump(lua_State* L, LuaWriterFunction writer, void* data, bool strip)
    {
        return _lua_dump(L, Marshal.GetFunctionPointerForDelegate(writer), data, strip);
    }

    public static int lua_dump(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, void*, nuint, void*, int> writer, void* data, bool strip)
    {
        return _lua_dump(L, (IntPtr) writer, data, strip);
    }

    /*
     *
     *  Coroutine functions
     *
     */
    [LuaExport(Throws = true)]
    public static partial int lua_yieldk(lua_State* L, int nresults, void* ctx, LuaKFunction? k);

    [LuaExport(PCall = true)]
    public static partial LuaStatus lua_resume(lua_State* L, lua_State* from, int narg, out int nres);

    [LuaExport]
    public static partial LuaStatus lua_status(lua_State* L);

    [LuaExport]
    public static partial bool lua_isyieldable(lua_State* L);

    public static int lua_yield(lua_State* L, int nresults)
    {
        return lua_yieldk(L, nresults, null, null);
    }

    /*
     *
     *  Warning-related functions
     *
     */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void _lua_setwarnfDelegate(lua_State* L, IntPtr f, void* ud);

    [LuaExport]
    private static _lua_setwarnfDelegate _lua_setwarnf = null!;

    public static void lua_setwarnf(lua_State* L, LuaWarnFunction f, void* ud)
    {
        _lua_setwarnf(L, Marshal.GetFunctionPointerForDelegate(f), ud);
    }

    public static void lua_setwarnf(lua_State* L, IntPtr f, void* ud)
    {
        _lua_setwarnf(L, f, ud);
    }

    public static void lua_setwarnf(lua_State* L, delegate* unmanaged[Cdecl]<void*, byte*, int, void> f, void* ud)
    {
        _lua_setwarnf(L, (IntPtr) f, ud);
    }

    [LuaExport(Throws = true)]
    public static partial void lua_warning(lua_State* L, ReadOnlySpan<char> msg, bool tocont);

    /*
     *
     * Garbage-collection function and options
     *
     */
    [LuaExport("lua_gc", PCall = true)]
    public static partial int lua_gc(lua_State* L, LuaGCOperation what, int arg1 = 0, int arg2 = 0, int arg3 = 0);

    /*
     *
     * Miscellaneous functions
     *
     */
    // [DllImport(DllName, CallingConvention = Cdecl)]
    // public static extern int lua_error(lua_State* L);

    // public static delegate* unmanaged[Cdecl]<lua_State*, int> lua_error;

    [LuaExport(Throws = true)]
    [DoesNotReturn]
    public static partial int lua_error(lua_State* L);

    [LuaExport(Throws = true)]
    public static partial bool lua_next(lua_State* L, int idx);

    [LuaExport(Throws = true)]
    public static partial void lua_concat(lua_State* L, int n);

    [LuaExport(Throws = true)]
    public static partial void lua_len(lua_State* L, int idx);

    [LuaExport]
    public static partial nuint lua_stringtonumber(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string s);

    [LuaExport]
    public static partial LuaAllocFunction lua_getallocf(lua_State* L, out void* ud);

    [LuaExport]
    public static partial void lua_setallocf(lua_State* L, LuaAllocFunction f, void* ud);

    [LuaExport(Throws = true)]
    public static partial void lua_toclose(lua_State* L, int idx);

    [LuaExport(Throws = true), LuaOptional]
    public static partial void lua_closeslot(lua_State* L, int idx);

    /*
     *
     * Debug functions
     *
     */
    [LuaExport]
    public static partial bool lua_getstack(lua_State* L, int level, lua_Debug* ar);

    [LuaExport(Throws = true)]
    public static partial bool lua_getinfo(lua_State* L, ReadOnlySpan<byte> what, lua_Debug* ar);

    [LuaExport]
    public static partial LuaString lua_getlocal(lua_State* L, lua_Debug* ar, int n);

    [LuaExport]
    public static partial LuaString lua_setlocal(lua_State* L, lua_Debug* ar, int n);

    [LuaExport]
    public static partial LuaString lua_getupvalue(lua_State* L, int funcindex, int n);

    [LuaExport]
    public static partial LuaString lua_setupvalue(lua_State* L, int funcindex, int n);

    [LuaExport]
    public static partial void* lua_upvalueid(lua_State* L, int fidx1, int n);

    [LuaExport]
    public static partial void lua_upvaluejoin(lua_State* L, int fidx1, int n1, int fidx2, int n2);

    [LuaExport]
    public static partial void lua_sethook(lua_State* L, LuaHookFunction? func, LuaEventMask mask, int count);

    [LuaExport]
    public static partial LuaHookFunction? lua_gethook(lua_State* L);

    [LuaExport]
    public static partial LuaEventMask lua_gethookmask(lua_State* L);

    [LuaExport]
    public static partial int lua_gethookcount(lua_State* L);

    [LuaExport]
    public static partial int lua_setcstacklimit(lua_State* L, uint limit);
}
