using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

internal static unsafe partial class LayluaNative
{
    private sealed class FunctionWrapperEntry
    {
        public Delegate Wrapper = null!;
        public IntPtr AsmPtr;

        ~FunctionWrapperEntry()
        {
            if (AsmPtr != IntPtr.Zero)
            {
                NativeMemory.AlignedFree((void*) AsmPtr);
                AsmPtr = IntPtr.Zero;
            }
        }
    }

    private static readonly ConditionalWeakTable<Delegate, FunctionWrapperEntry> FunctionWrappers = new();
    private const string ManagedCodeExecutionErrorMessage = "An exception occurred while executing managed code.";

    private static (string? Message, Exception? InnerException) ExtractErrorInfo(Exception ex, string genericMessage)
    {
        if (ex is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null)
        {
            ex = targetInvocationException.InnerException;
        }

        if (ex is LuaRaisedException raised)
        {
            return (raised.LuaMessage, raised.LuaInnerException);
        }

        return (genericMessage, ex);
    }

    public static IntPtr CreateLuaKFunctionWrapper(LuaKFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaKFunction) ((L, status, ctx) =>
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
                        var (msg, inner) = ExtractErrorInfo(ex, ManagedCodeExecutionErrorMessage);
                        LuaException.PrepareErrorInfo(L, msg, inner);
                        SetPendingError(L);
                    }
                }
                return default;
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    public static IntPtr CreateLuaCFunctionWrapper(LuaCFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaCFunction) (L =>
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
                        var (msg, inner) = ExtractErrorInfo(ex, ManagedCodeExecutionErrorMessage);
                        LuaException.PrepareErrorInfo(L, msg, inner);
                        SetPendingError(L);
                    }
                }
                return default;
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    public static IntPtr CreateLuaReaderFunctionWrapper(LuaReaderFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaReaderFunction) ((L, ud, out sz) =>
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
                        var (msg, inner) = ExtractErrorInfo(ex, ManagedCodeExecutionErrorMessage);
                        LuaException.PrepareErrorInfo(L, msg, inner);
                        SetPendingError(L);
                    }
                }
                sz = default;
                return default;
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    public static IntPtr CreateLuaWriterFunctionWrapper(LuaWriterFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaWriterFunction) ((L, p, sz, ud) =>
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
                        var (msg, inner) = ExtractErrorInfo(ex, ManagedCodeExecutionErrorMessage);
                        LuaException.PrepareErrorInfo(L, msg, inner);
                        SetPendingError(L);
                    }
                }
                return default;
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    public static IntPtr CreateLuaWarnFunctionWrapper(LuaWarnFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaWarnFunction) ((ud, msg, tocont) =>
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
                        var (errorMessage, inner) = ExtractErrorInfo(ex, ManagedCodeExecutionErrorMessage);
                        LuaException.PrepareErrorInfo(L, errorMessage, inner);
                        SetPendingError(L);
                    }
                }
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    public static IntPtr CreateLuaHookFunctionWrapper(LuaHookFunction function)
    {
        var entry = FunctionWrappers.GetValue(function, static function =>
        {
            var asmSpan = AllocFunctionWrapperAsm(out var asmPtr);

            var wrapper = (LuaHookFunction) ((L, ar) =>
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
            });

            WriteFunctionWrapperPointers(asmSpan, wrapper);

            return new FunctionWrapperEntry
            {
                Wrapper = wrapper,
                AsmPtr = asmPtr
            };
        });

        return entry.AsmPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<byte> AllocFunctionWrapperAsm(out IntPtr asmPtr)
    {
        var bytes = OperatingSystem.IsWindows() ? ManagedPanicAsmBytes_Windows : ManagedPanicAsmBytes_Linux;
        var asmSpan = Alloc(bytes, out asmPtr);
        return asmSpan;
    }

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
