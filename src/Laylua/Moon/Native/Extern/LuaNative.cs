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
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_State* lua_newstate(LuaAllocFunction f, void* ud);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_close(lua_State* L);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate lua_State* _lua_newthreadDelegate(lua_State* L);

    private static _lua_newthreadDelegate _lua_newthread = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static lua_State* lua_newthread(lua_State* L)
    {
        return _lua_newthread(L);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaStatus lua_resetthread(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaCFunction lua_atpanic(lua_State* L, LuaCFunction panicf);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern delegate* unmanaged[Cdecl]<lua_State*, int> lua_atpanic(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> panicf);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_Number lua_version(lua_State* L);

    /*
     *
     *  Basic stack manipulation
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_absindex(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gettop(lua_State* L);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_settopDelegate(lua_State* L, int idx);

    private static _lua_settopDelegate _lua_settop = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_settop(lua_State* L, int idx)
    {
        _lua_settop(L, idx);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushvalue(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_rotate(lua_State* L, int idx, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_copy(lua_State* L, int fromidx, int toidx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_checkstack(lua_State* L, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_xmove(lua_State* from, lua_State* to, int n);

    /*
     *
     *  Access functions (stack -> C)
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_isnumber(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_isstring(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_iscfunction(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_isinteger(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_isuserdata(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaType lua_type(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    public static extern string lua_typename(lua_State* L, LuaType tp);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_Number lua_tonumberx(lua_State* L, int idx, out bool isnum);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_Integer lua_tointegerx(lua_State* L, int idx, out bool isnum);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_toboolean(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_tolstring))]
    private static extern byte* _lua_tolstring(lua_State* L, int idx, out nuint len);

    public static LuaString lua_tolstring(lua_State* L, int idx, out nuint len)
    {
        var ptr = _lua_tolstring(L, idx, out len);
        return new LuaString(ptr, len);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_Unsigned lua_rawlen(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaCFunction? lua_tocfunction(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void* lua_touserdata(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern lua_State* lua_tothread(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void* lua_topointer(lua_State* L, int idx);

    /*
     *
     *  Comparison and arithmetic functions
     *
     */
    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_arithDelegate(lua_State* L, int idx1, int idx2, LuaOperation op);

    private static _lua_arithDelegate _lua_arith = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_arith(lua_State* L, int idx1, int idx2, LuaOperation op)
    {
        _lua_arith(L, idx1, idx2, op);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_rawequal(lua_State* L, int idx1, int idx2);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate bool _lua_compareDelegate(lua_State* L, int idx1, int idx2, LuaComparison op);

    private static _lua_compareDelegate _lua_compare = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool lua_compare(lua_State* L, int idx1, int idx2, LuaComparison op)
    {
        return _lua_compare(L, idx1, idx2, op);
    }

    /*
     *
     *  Push functions (C -> stack)
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushnil(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushnumber(lua_State* L, lua_Number n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushinteger(lua_State* L, lua_Integer n);

    //[DllImport(DllName, CallingConvention = Cdecl, EntryPoint = "lua_pushlstring")]
    //private static extern byte* _lua_pushlstring(lua_State* L, byte* s, nuint len);

    // [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_pushstring))]
    // private static extern byte* _lua_pushstring(lua_State* L, byte* s);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate byte* _lua_pushlstringDelegate(lua_State* L, byte* s, nuint len);

    private static _lua_pushlstringDelegate _lua_pushlstring = null!;

    [ErrorExport("lua_pushlstring")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte* lua_pushstring(lua_State* L, ReadOnlySpan<char> s)
    {
        using (var bytes = ToLString(s, out var len))
        {
            fixed (byte* ptr = bytes)
            {
                return _lua_pushlstring(L, ptr, len);
            }
        }
    }

    public static byte* lua_pushstring(lua_State* L, LuaString s)
    {
        return _lua_pushlstring(L, s.Pointer, s.Length);
    }

    public static byte* lua_pushstring(lua_State* L, ReadOnlySpan<byte> s)
    {
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

    private static _lua_pushcclosureDelegate _lua_pushcclosure = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_pushcclosure(lua_State* L, LuaCFunction fn, int n)
    {
        var wrapperPtr = LayluaNative.CreateLuaCFunctionWrapper(fn);
#if TRACE_PANIC
        Console.WriteLine("wrapperPtr: 0x{0:X}", wrapperPtr);
#endif
        _lua_pushcclosure(L, wrapperPtr, n);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushboolean(lua_State* L, bool b);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_pushlightuserdata(lua_State* L, void* p);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_pushthread(lua_State* L);

    /*
     *
     *  Get functions (Lua -> stack)
     *
     */
    [UnmanagedFunctionPointer(Cdecl)]
    private delegate LuaType _lua_getglobalDelegate(lua_State* L, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    private static _lua_getglobalDelegate _lua_getglobal = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaType lua_getglobal(lua_State* L, string name)
    {
        return _lua_getglobal(L, name);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate LuaType _lua_gettableDelegate(lua_State* L, int idx);

    private static _lua_gettableDelegate _lua_gettable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaType lua_gettable(lua_State* L, int idx)
    {
        return _lua_gettable(L, idx);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate LuaType _lua_getfieldDelegate(lua_State* L, int idx, byte* k);

    private static _lua_getfieldDelegate _lua_getfield = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaType lua_getfield(lua_State* L, int idx, ReadOnlySpan<char> k)
    {
        using (var bytes = ToCString(k))
        {
            fixed (byte* ptr = bytes)
            {
                return _lua_getfield(L, idx, ptr);
            }
        }
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate LuaType _lua_getiDelegate(lua_State* L, int idx, lua_Integer n);

    private static _lua_getiDelegate _lua_geti = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LuaType lua_geti(lua_State* L, int idx, lua_Integer n)
    {
        return _lua_geti(L, idx, n);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaType lua_rawget(lua_State* L, int idx);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaType lua_rawgeti(lua_State* L, int idx, lua_Integer n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaType lua_rawgetp(lua_State* L, int idx, void* p);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_createtableDelegate(lua_State* L, int narr, int nrec);

    private static _lua_createtableDelegate _lua_createtable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_createtable(lua_State* L, int narr, int nrec)
    {
        _lua_createtable(L, narr, nrec);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void* _lua_newuserdatauvDelegate(lua_State* L, nuint size, int nuvalue);

    private static _lua_newuserdatauvDelegate _lua_newuserdatauv = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* lua_newuserdatauv(lua_State* L, nuint size, int nuvalue)
    {
        return _lua_newuserdatauv(L, size, nuvalue);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_getmetatable(lua_State* L, int objindex);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaType lua_getiuservalue(lua_State* L, int idx, int n);

    /*
     *
     *  Set functions (stack -> Lua)
     *
     */
    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_setglobalDelegate(lua_State* L, byte* name);

    private static _lua_setglobalDelegate _lua_setglobal = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_setglobal(lua_State* L, ReadOnlySpan<char> name)
    {
        using (var bytes = ToCString(name))
        {
            fixed (byte* ptr = bytes)
            {
                _lua_setglobal(L, ptr);
            }
        }
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_settableDelegate(lua_State* L, int idx);

    private static _lua_settableDelegate _lua_settable = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_settable(lua_State* L, int idx)
    {
        _lua_settable(L, idx);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_setfieldDelegate(lua_State* L, int idx, byte* k);

    private static _lua_setfieldDelegate _lua_setfield = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_setfield(lua_State* L, int idx, ReadOnlySpan<char> k)
    {
        using (var bytes = ToCString(k))
        {
            fixed (byte* ptr = bytes)
            {
                _lua_setfield(L, idx, ptr);
            }
        }
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_setiDelegate(lua_State* L, int idx, lua_Integer n);

    private static _lua_setiDelegate _lua_seti = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_seti(lua_State* L, int idx, lua_Integer n)
    {
        _lua_seti(L, idx, n);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_rawsetDelegate(lua_State* L, int idx);

    private static _lua_rawsetDelegate _lua_rawset = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_rawset(lua_State* L, int idx)
    {
        _lua_rawset(L, idx);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_rawsetiDelegate(lua_State* L, int idx, lua_Integer n);

    private static _lua_rawsetiDelegate _lua_rawseti = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_rawseti(lua_State* L, int idx, lua_Integer n)
    {
        _lua_rawseti(L, idx, n);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_rawsetpDelegate(lua_State* L, int idx, void* p);

    private static _lua_rawsetpDelegate _lua_rawsetp = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_rawsetp(lua_State* L, int idx, void* p)
    {
        _lua_rawsetp(L, idx, p);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_setmetatable(lua_State* L, int objindex);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_setiuservalue(lua_State* L, int idx, int n);

    /*
     *
     *  'load' and 'call' functions (load and run Lua code)
     *
     */
    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_callkDelegate(lua_State* L, int nargs, int nresults, void* ctx, LuaKFunction? k);

    private static _lua_callkDelegate _lua_callk = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_callk(lua_State* L, int nargs, int nresults, void* ctx, LuaKFunction? k)
    {
        _lua_callk(L, nargs, nresults, ctx, k);
    }

    public static void lua_call(lua_State* L, int nargs, int nresults)
    {
        lua_callk(L, nargs, nresults, null, null);
    }

    [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_pcallk))]
    private static extern LuaStatus _lua_pcallk(lua_State* L, int nargs, int nresults, int errfunc, void* ctx, IntPtr k);

    public static LuaStatus lua_pcallk(lua_State* L, int nargs, int nresults, int errfunc, void* ctx, LuaKFunction? k)
    {
        using (LayluaState.FromExtraSpace(L).EnterPCallContext())
        {
            var wrapperPtr = k != null
                ? LayluaNative.CreateLuaKFunctionWrapper(k)
                : IntPtr.Zero;

            return _lua_pcallk(L, nargs, nresults, errfunc, ctx, wrapperPtr);
        }
    }

    public static LuaStatus lua_pcall(lua_State* L, int nargs, int nresults, int errfunc)
    {
        return lua_pcallk(L, nargs, nresults, errfunc, null, null);
    }

    [DllImport(DllName, CallingConvention = Cdecl, EntryPoint = nameof(lua_load))]
    private static extern LuaStatus _lua_load(lua_State* L, void* reader, void* data, byte* chunkname, byte* mode);

    public static LuaStatus lua_load(lua_State* L, LuaReaderFunction reader, void* data, ReadOnlySpan<char> chunkname, ReadOnlySpan<char> mode)
    {
        using (var chunkNameBytes = ToCString(chunkname))
        using (var modeBytes = ToCString(mode))
        using (LayluaState.FromExtraSpace(L).EnterPCallContext())
        {
            fixed (byte* chunkNamePtr = chunkNameBytes)
            fixed (byte* modePtr = modeBytes)
            {
                return _lua_load(L, (void*) Marshal.GetFunctionPointerForDelegate(reader), data, chunkNamePtr, modePtr);
            }
        }
    }

    public static LuaStatus lua_load(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, void*, nuint*, byte*> reader, void* data, ReadOnlySpan<char> chunkname, ReadOnlySpan<char> mode)
    {
        using (var chunkNameBytes = ToCString(chunkname))
        using (var modeBytes = ToCString(mode))
        using (LayluaState.FromExtraSpace(L).EnterPCallContext())
        {
            fixed (byte* chunkNamePtr = chunkNameBytes)
            fixed (byte* modePtr = modeBytes)
            {
                return _lua_load(L, reader, data, chunkNamePtr, modePtr);
            }
        }
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_dump(lua_State* L, LuaWriterFunction writer, void* data, bool strip);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_dump(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, void*, nuint, void*, int> writer, void* data, bool strip);

    /*
     *
     *  Coroutine functions
     *
     */
    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _lua_yieldkDelegate(lua_State* L, int nresults, void* ctx, LuaKFunction? k);

    private static _lua_yieldkDelegate _lua_yieldk = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int lua_yieldk(lua_State* L, int nresults, void* ctx, LuaKFunction? k)
    {
        return _lua_yieldk(L, nresults, ctx, k);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaStatus lua_resume(lua_State* L, lua_State* from, int narg, out int nres);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaStatus lua_status(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_isyieldable(lua_State* L);

    public static int lua_yield(lua_State* L, int nresults)
    {
        return lua_yieldk(L, nresults, null, null);
    }

    /*
     *
     *  Warning-related functions
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_setwarnf(lua_State* L, LuaWarnFunction f, void* ud);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_setwarnf(lua_State* L, IntPtr f, void* ud);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_setwarnf(lua_State* L, delegate* unmanaged[Cdecl]<void*, byte*, int, void> f, void* ud);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_warningDelegate(lua_State* L, byte* msg, bool tocont);

    private static _lua_warningDelegate _lua_warning = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void lua_warning(lua_State* L, byte* msg, bool tocont)
    {
        _lua_warning(L, msg, tocont);
    }

    public static void lua_warning(lua_State* L, ReadOnlySpan<char> msg, bool tocont)
    {
        using (var bytes = ToCString(msg))
        {
            fixed (byte* ptr = bytes)
            {
                lua_warning(L, ptr, tocont);
            }
        }
    }

    /*
     *
     * Garbage-collection function and options
     *
     */
    // lua_gc is overloaded because __arglist sucks.
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gc(lua_State* L, LuaGCOperation what);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gc(lua_State* L, LuaGCOperation what, int arg1);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gc(lua_State* L, LuaGCOperation what, int arg1, int arg2);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gc(lua_State* L, LuaGCOperation what, int arg1, int arg2, int arg3);

    /*
     *
     * Miscellaneous functions
     *
     */
    // [DllImport(DllName, CallingConvention = Cdecl)]
    // public static extern int lua_error(lua_State* L);

    // public static delegate* unmanaged[Cdecl]<lua_State*, int> lua_error;

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate int _lua_errorDelegate(lua_State* L);

    private static _lua_errorDelegate _lua_error = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
#nullable disable
    public static int lua_error(lua_State* L)
    {
        return _lua_error(L);
    }
#nullable enable

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate bool _lua_nextDelegate(lua_State* L, int idx);

    private static _lua_nextDelegate _lua_next = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool lua_next(lua_State* L, int idx)
    {
        return _lua_next(L, idx);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_concatDelegate(lua_State* L, int n);

    private static _lua_concatDelegate _lua_concat = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_concat(lua_State* L, int n)
    {
        _lua_concat(L, n);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_lenDelegate(lua_State* L, int idx);

    private static _lua_lenDelegate _lua_len = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_len(lua_State* L, int idx)
    {
        _lua_len(L, idx);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern nuint lua_stringtonumber(lua_State* L, [MarshalAs(UnmanagedType.LPStr)] string s);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaAllocFunction lua_getallocf(lua_State* L, out void* ud);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_setallocf(lua_State* L, LuaAllocFunction f, void* ud);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_tocloseDelegate(lua_State* L, int idx);

    private static _lua_tocloseDelegate _lua_toclose = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_toclose(lua_State* L, int idx)
    {
        _lua_toclose(L, idx);
    }

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate void _lua_closeslotDelegate(lua_State* L, int idx);

    private static _lua_closeslotDelegate? _lua_closeslot = null;

    [ErrorExport, OptionalExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void lua_closeslot(lua_State* L, int idx)
    {
        if (_lua_closeslot == null)
            throw new InvalidOperationException($"{nameof(lua_closeslot)} is not available on this Lua version.");

        _lua_closeslot(L, idx);
    }

    /*
     *
     * Debug functions
     *
     */
    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern bool lua_getstack(lua_State* L, int level, lua_Debug* ar);

    [UnmanagedFunctionPointer(Cdecl)]
    private delegate bool _lua_getinfoDelegate(lua_State* L, byte* what, lua_Debug* ar);

    private static _lua_getinfoDelegate _lua_getinfo = null!;

    [ErrorExport]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool lua_getinfo(lua_State* L, ReadOnlySpan<byte> what, lua_Debug* ar)
    {
        fixed (byte* whatPtr = what)
        {
            return _lua_getinfo(L, whatPtr, ar);
        }
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    public static extern string lua_getlocal(lua_State* L, lua_Debug* ar, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    public static extern string lua_setlocal(lua_State* L, lua_Debug* ar, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    private static extern byte* _lua_getupvalue(lua_State* L, int funcindex, int n);

    public static LuaString lua_getupvalue(lua_State* L, int funcindex, int n)
    {
        var ptr = _lua_getupvalue(L, funcindex, n);
        return new LuaString(ptr);
    }

    [DllImport(DllName, CallingConvention = Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(AnsiStringMarshaler))]
    public static extern string lua_setupvalue(lua_State* L, int funcindex, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void* lua_upvalueid(lua_State* L, int fidx1, int n);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_upvaluejoin(lua_State* L, int fidx1, int n1, int fidx2, int n2);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern void lua_sethook(lua_State* L, LuaHookFunction? func, LuaEventMask mask, int count);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaHookFunction? lua_gethook(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern LuaEventMask lua_gethookmask(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_gethookcount(lua_State* L);

    [DllImport(DllName, CallingConvention = Cdecl)]
    public static extern int lua_setcstacklimit(lua_State* L, uint limit);
}
