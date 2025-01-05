using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

internal static unsafe partial class LayluaNative
{
    private static readonly ConditionalWeakTable<Delegate, Delegate> FunctionWrappers = new();

    public static IntPtr CreateLuaKFunctionWrapper(LuaKFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaKFunction) ((L, status, ctx) =>
        {
            try
            {
#if TRACE_PANIC
                Console.WriteLine("LuaKFunction: calling delegate...");
#endif
                Unsafe.As<LuaKFunction>(function)(L, status, ctx);
            }
            catch (Exception ex)
            {
#if TRACE_PANIC
                Console.WriteLine("LuaKFunction: caught exception...");
#endif
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaKFunction: setting panic exception: {ex}");
#endif
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaKFunction: panic is null, throwing exception: {ex}");
#endif
                    luaL_error(L, ex.Message);
                }
            }
#if TRACE_PANIC
            Console.WriteLine("LuaKFunction: returning...");
#endif
        }));

        WriteFunctionWrapperPointers(asmSpan,
#if TRACE_PANIC
            asmPtr,
#endif
            function, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaCFunctionWrapper(LuaCFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaCFunction) (L =>
        {
            try
            {
#if TRACE_PANIC
                Console.WriteLine("LuaCFunction: calling delegate...");
#endif
                return Unsafe.As<LuaCFunction>(function)(L);
            }
            catch (Exception ex)
            {
#if TRACE_PANIC
                Console.WriteLine("LuaCFunction: caught exception...");
#endif
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaCFunction: setting panic exception: {ex}");
#endif
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaCFunction: panic is null, throwing exception: {ex}");
#endif
                    luaL_error(L, ex.Message);
                }
            }
#if TRACE_PANIC
            Console.WriteLine("LuaCFunction: returning...");
#endif
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan,
#if TRACE_PANIC
            asmPtr,
#endif
            function, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaReaderFunctionWrapper(LuaReaderFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaReaderFunction) ((lua_State* L, void* ud, out nuint sz) =>
        {
            try
            {
#if TRACE_PANIC
                Console.WriteLine("LuaReaderFunction: calling delegate...");
#endif
                return Unsafe.As<LuaReaderFunction>(function)(L, ud, out sz);
            }
            catch (Exception ex)
            {
#if TRACE_PANIC
                Console.WriteLine("LuaReaderFunction: caught exception...");
#endif
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaReaderFunction: setting panic exception: {ex}");
#endif
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaReaderFunction: panic is null, throwing exception: {ex}");
#endif
                    luaL_error(L, ex.Message);
                }
            }
#if TRACE_PANIC
            Console.WriteLine("LuaReaderFunction: returning...");
#endif
            sz = default;
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan,
#if TRACE_PANIC
            asmPtr,
#endif
            function, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaWriterFunctionWrapper(LuaWriterFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaWriterFunction) ((L, p, sz, ud) =>
        {
            try
            {
#if TRACE_PANIC
                Console.WriteLine("LuaWriterFunction: calling delegate...");
#endif
                return Unsafe.As<LuaWriterFunction>(function)(L, p, sz, ud);
            }
            catch (Exception ex)
            {
#if TRACE_PANIC
                Console.WriteLine("LuaWriterFunction: caught exception...");
#endif
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaWriterFunction: setting panic exception: {ex}");
#endif
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaWriterFunction: panic is null, throwing exception: {ex}");
#endif
                    luaL_error(L, ex.Message);
                }
            }
#if TRACE_PANIC
            Console.WriteLine("LuaWriterFunction: returning...");
#endif
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan,
#if TRACE_PANIC
            asmPtr,
#endif
            function, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaHookFunctionWrapper(LuaHookFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaHookFunction) ((L, ar) =>
        {
            try
            {
#if TRACE_PANIC
                Console.WriteLine("LuaHookFunction: calling delegate...");
#endif
                Unsafe.As<LuaHookFunction>(function)(L, ar);
            }
            catch (Exception ex)
            {
#if TRACE_PANIC
                Console.WriteLine("LuaHookFunction: caught exception...");
#endif
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaHookFunction: setting panic exception: {ex}");
#endif
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
#if TRACE_PANIC
                    Console.WriteLine($"LuaHookFunction: panic is null, throwing exception: {ex}");
#endif
                    luaL_error(L, ex.Message);
                }
            }
#if TRACE_PANIC
            Console.WriteLine("LuaHookFunction: returning...");
#endif
        }));

        WriteFunctionWrapperPointers(asmSpan,
#if TRACE_PANIC
            asmPtr,
#endif
            function, functionWrapper);

        return asmPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<byte> AllocFunctionWrapperAsm(out IntPtr asmPtr)
    {
        var asmSpan = Alloc(ManagedPanicAsmBytes, out asmPtr);
        return asmSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFunctionWrapperPointers(Span<byte> asmSpan,
#if TRACE_PANIC
        IntPtr asmPtr,
#endif
        Delegate function, Delegate functionWrapper
#if TRACE_PANIC
        , [CallerMemberName] string? callerName = null
#endif
    )
    {
#if TRACE_PANIC
        var functionPtr = Marshal.GetFunctionPointerForDelegate(function);
#endif
        var functionWrapperPtr = Marshal.GetFunctionPointerForDelegate(functionWrapper);

        // MemoryMarshal.Write(asmSpan.Slice(21), ref functionPtr);
        MemoryMarshal.Write(asmSpan.Slice(40 - 13), ref functionWrapperPtr);
        MemoryMarshal.Write(asmSpan.Slice(71 - 13), ref getPotentialPanicPtr);

        // Console.WriteLine("Stack State at 0x{0:X}", (IntPtr) stackState);

#if TRACE_PANIC
        Console.WriteLine("ManagedPanic for {0} asmPtr: 0x{1:X}\nGetPotentialPanicPtr: 0x{2:X}\nCallLuaCFunction: 0x{3:X} (for 0x{4:X})", callerName, asmPtr, getPotentialPanicPtr, functionWrapperPtr, functionPtr);
#endif
    }
}
