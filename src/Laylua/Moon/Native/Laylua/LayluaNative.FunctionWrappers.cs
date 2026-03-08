using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

// ManagedPanicAsm wraps every C# delegate callable from Lua
// Catch blocks here are the last stop - LuaRaisedException preserves the user's
// error message, everything else gets a generic message. See LayluaNative.Asm.cs

internal static unsafe partial class LayluaNative
{
    private static readonly ConditionalWeakTable<Delegate, Delegate> FunctionWrappers = new();

    // Pulls the error message + inner exception out of a caught exception
    // LuaRaisedException gets unwrapped; everything else is treated as unexpected
    private static (string? Message, Exception? InnerException) ExtractErrorInfo(Exception ex, string genericMessage)
    {
        if (ex is LuaRaisedException raised)
            return (raised.LuaMessage, raised.LuaInnerException);

        return (genericMessage, ex);
    }

    public static IntPtr CreateLuaKFunctionWrapper(LuaKFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaKFunction) ((L, status, ctx) =>
        {
            try
            {
                return Unsafe.As<LuaKFunction>(function)(L, status, ctx);
            }
            catch (Exception ex)
            {
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (msg, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the delegate.");
                    LuaException.PrepareErrorInfo(L, msg, inner);
                    SetPendingError(L);
                }
            }
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaCFunctionWrapper(LuaCFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaCFunction) (L =>
        {
            try
            {
                return Unsafe.As<LuaCFunction>(function)(L);
            }
            catch (Exception ex)
            {
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (msg, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the delegate.");
                    LuaException.PrepareErrorInfo(L, msg, inner);
                    SetPendingError(L);
                }
            }
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaReaderFunctionWrapper(LuaReaderFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaReaderFunction) ((lua_State* L, void* ud, out nuint sz) =>
        {
            try
            {
                return Unsafe.As<LuaReaderFunction>(function)(L, ud, out sz);
            }
            catch (Exception ex)
            {
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (msg, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the delegate.");
                    LuaException.PrepareErrorInfo(L, msg, inner);
                    SetPendingError(L);
                }
            }
            sz = default;
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaWriterFunctionWrapper(LuaWriterFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaWriterFunction) ((L, p, sz, ud) =>
        {
            try
            {
                return Unsafe.As<LuaWriterFunction>(function)(L, p, sz, ud);
            }
            catch (Exception ex)
            {
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (msg, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the delegate.");
                    LuaException.PrepareErrorInfo(L, msg, inner);
                    SetPendingError(L);
                }
            }
            return default;
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaWarnFunctionWrapper(LuaWarnFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaWarnFunction) ((ud, msg, tocont) =>
        {
            try
            {
                Unsafe.As<LuaWarnFunction>(function)(ud, msg, tocont);
            }
            catch (Exception ex)
            {
                var L = (lua_State*) ud;
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (errorMessage, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the delegate.");
                    LuaException.PrepareErrorInfo(L, errorMessage, inner);
                    SetPendingError(L);
                }
            }
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    public static IntPtr CreateLuaHookFunctionWrapper(LuaHookFunction function)
    {
        var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

        var functionWrapper = FunctionWrappers.GetValue(function, static function => (LuaHookFunction) ((L, ar) =>
        {
            try
            {
                Unsafe.As<LuaHookFunction>(function)(L, ar);
            }
            catch (Exception ex)
            {
                var laylua = LayluaState.FromExtraSpace(L);
                if (laylua.Panic != null)
                {
                    laylua.Panic.Exception = ExceptionDispatchInfo.Capture(ex);
                }
                else
                {
                    var (msg, inner) = ExtractErrorInfo(ex, "An exception occurred while executing the hook.");
                    LuaException.PrepareErrorInfo(L, msg, inner);
                    SetPendingError(L);
                }
            }
        }));

        WriteFunctionWrapperPointers(asmSpan, functionWrapper);

        return asmPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<byte> AllocFunctionWrapperAsm(out IntPtr asmPtr)
    {
        var bytes = OperatingSystem.IsWindows() ? ManagedPanicAsmBytes_Windows : ManagedPanicAsmBytes_Linux;
        var asmSpan = Alloc(bytes, out asmPtr);
        return asmSpan;
    }

    // Offsets differ per platform (different asm sizes)
    private static readonly int _dataSlot1Offset = OperatingSystem.IsWindows() ? 112 : 87;
    private static readonly int _dataSlot2Offset = OperatingSystem.IsWindows() ? 120 : 95;
    private static readonly int _dataSlot3Offset = OperatingSystem.IsWindows() ? 128 : 103;

    private static void WriteFunctionWrapperPointers(Span<byte> asmSpan, Delegate functionWrapper)
    {
        var functionWrapperPtr = Marshal.GetFunctionPointerForDelegate(functionWrapper);
        MemoryMarshal.Write(asmSpan.Slice(_dataSlot1Offset), in functionWrapperPtr);
        MemoryMarshal.Write(asmSpan.Slice(_dataSlot2Offset), in realLuaErrorPtr);
        MemoryMarshal.Write(asmSpan.Slice(_dataSlot3Offset), in getPotentialPanicPtr);
    }
}
