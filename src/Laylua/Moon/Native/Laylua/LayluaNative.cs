using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

[AttributeUsage(AttributeTargets.Method)]
internal class ErrorExportAttribute : Attribute
{
    public string? ExportName { get; }

    public ErrorExportAttribute()
    { }

    public ErrorExportAttribute(string exportName)
    {
        ExportName = exportName;
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal class OptionalExportAttribute : Attribute;

internal static unsafe partial class LayluaNative
{
    static LayluaNative()
    {
        InitializePanicHooks();
    }

    private static Span<byte> Alloc(ReadOnlySpan<byte> bytes, out IntPtr ptr)
    {
        var alignment = OperatingSystem.IsWindows()
            ? 16
            : (nuint) getpagesize();

        var size = (nuint) bytes.Length;
        if ((size & (alignment - 1)) != 0)
        {
            size &= ~(alignment - 1);
            size += alignment;
        }

        ptr = (IntPtr) NativeMemory.AlignedAlloc(size, alignment);
        if (OperatingSystem.IsWindows())
        {
            if (!VirtualProtect(ptr, size, MemoryProtection.ExecuteReadWrite, out _))
            {
                throw new InvalidOperationException($"{nameof(VirtualProtect)} failed (error code is {Marshal.GetLastPInvokeError()}).");
            }
        }
        else
        {
            if (mprotect(ptr, size, MmapProts.PROT_READ | MmapProts.PROT_WRITE | MmapProts.PROT_EXEC) != 0)
            {
                throw new InvalidOperationException($"{nameof(mprotect)} failed (error code is {Marshal.GetLastPInvokeError()}).");
            }
        }

        var span = new Span<byte>((void*) ptr, (int) size);
        span.Fill(0x90);
        bytes.CopyTo(span);
        return span;
    }

    private static void* _panicPtr = null;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void* Panic(lua_State* L)
    {
        var laylua = LayluaState.FromExtraSpace(L);
        var hasDupedStackMessage = false;
        if (LuaException.TryGetError(L, out var error))
        {
            // On Lua compiled with GCC there's some issue here with the error message
            // appearing twice on the stack, so this code checks for it.
            if (lua_gettop(L) > 1 && lua_rawequal(L, -1, -2))
            {
                hasDupedStackMessage = true;
            }

            lua_pop(L, hasDupedStackMessage ? 2 : 1);
        }

        ExceptionDispatchInfo exception;
        var panic = laylua.Panic;

        error.Message ??= "An unhandled error occurred.";

        if (panic == null)
        {
            exception = ExceptionDispatchInfo.Capture(new LuaPanicException(error.Message, error.Exception));
        }
        else
        {
            if (panic.Exception == null)
            {
                var ex = new LuaPanicException(error.Message, error.Exception);
                panic.Exception = ExceptionDispatchInfo.Capture(ex);
            }

            exception = panic.Exception;
        }

        if (panic == null)
        {
            if (OperatingSystem.IsWindows())
            {
                exception.Throw();
            }

            Environment.FailFast("Lua panicked which would have aborted the process.", exception.SourceException);
        }

        return panic.StackStatePtr;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void* InitializePanic()
    {
        if (_panicPtr == null)
        {
            var panicMethodHandle = typeof(LayluaNative).GetMethod(nameof(Panic), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle;
            var asmSpan = Alloc(PanicAsmBytes, out var asmPtr);

            var returnAddr = asmPtr + 0x1A;
            MemoryMarshal.Write(asmSpan[3..], ref returnAddr);

            RuntimeHelpers.PrepareMethod(panicMethodHandle);
            var panicPtr = panicMethodHandle.GetFunctionPointer();
            MemoryMarshal.Write(asmSpan[18..], ref panicPtr);

#if TRACE_PANIC
            // Console.WriteLine("Stack State at 0x{0:X}", (IntPtr) stackState);
            Console.WriteLine("asmPtr: 0x{0:X}\nmPtr: 0x{1:X}", asmPtr, panicPtr);
#endif
            _panicPtr = (void*) asmPtr;
        }

        return _panicPtr;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static IntPtr GetPotentialPanicPtr(lua_State* L)
    {
        var state = LayluaState.FromExtraSpace(L);
        var result = state.Panic?.Exception != null
            ? (IntPtr) state.PanicPtr
            : IntPtr.Zero;

        return result;
    }

    // private static IntPtr callLuaCPtr = typeof(LayluaNative).GetMethod(nameof(CallLuaCFunction), BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
    internal static IntPtr getPotentialPanicPtr = typeof(LayluaNative).GetMethod(nameof(GetPotentialPanicPtr), BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void* SetPanicJump(lua_State* L, void* retAddress)
    {
#if TRACE_PANIC
        Console.WriteLine($"{nameof(SetPanicJump)} L: 0x{(IntPtr) L:X}");
#endif
        var state = LayluaState.FromExtraSpace(L);
        if (state.IsPCall)
            return null;

        var panic = state.PushPanic();
        var stackState = panic.StackStatePtr;

        //if (!panic.HasStackState)
        *(void**) stackState = retAddress;

#if TRACE_PANIC
        Console.WriteLine("Lua 0x{0:X} set panic jump address to 0x{1:X}", (IntPtr) L, (IntPtr) retAddress);
#endif
        return stackState;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static void PopPanic(lua_State* L)
    {
#if TRACE_PANIC
        Console.WriteLine($"{nameof(PopPanic)} L: 0x{(IntPtr) L:X}");
#endif
        var state = LayluaState.FromExtraSpace(L);
        var panic = state.PopPanic();
        if (panic != null)
        {
            state.ResetPanic(panic);
        }
    }

    //[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowPanicException(lua_State* L)
    {
#if TRACE_PANIC
        Console.WriteLine($"{nameof(ThrowPanicException)} L: 0x{(IntPtr) L:X}");
        Console.WriteLine("Throwing panic exception...");
#endif
        var state = LayluaState.FromExtraSpace(L);
        var panic = state.PopPanic();
        if (panic != null)
        {
            try
            {
                panic.Exception!.Throw();

                //throw new LuaPanicException(null!, "test");
            }
            finally
            {
                state.ResetPanic(panic);
            }
        }
    }

    private static void InitializePanicHooks()
    {
        static IntPtr PrepareHookAsm(IntPtr export, IntPtr throwPanicExceptionPtr)
        {
            delegate* unmanaged[Cdecl]<lua_State*, void*, void*> mPtrDel = &SetPanicJump;
            var mPtr = (IntPtr) mPtrDel;

            delegate* unmanaged[Cdecl]<lua_State*, void> popPanicDel = &PopPanic;
            var popPanicPtr = (IntPtr) popPanicDel;

            var asmSpan = Alloc(HookAsmBytes, out var asmPtr);

            var panicJmpAddr = asmPtr + 202 + 10 + 216 * 2 + 136 + 50 + 19;
            MemoryMarshal.Write(asmSpan.Slice(29 + 216), ref panicJmpAddr);
            MemoryMarshal.Write(asmSpan.Slice(29 + 10 + 216), ref panicJmpAddr);
            MemoryMarshal.Write(asmSpan.Slice(45 + 10 + 216), ref mPtr);
            MemoryMarshal.Write(asmSpan.Slice(533), ref export);
            MemoryMarshal.Write(asmSpan.Slice(161 + 10 + 216 * 2 + 136 + 50 + 19), ref export);
            MemoryMarshal.Write(asmSpan.Slice(188 + 10 + 216 * 2 + 136 + 50 + 19), ref popPanicPtr);
            MemoryMarshal.Write(asmSpan.Slice(275 + 10 + 216 * 2 + 136 * 2 + 50 + 19), ref throwPanicExceptionPtr);

#if TRACE_PANIC
            Console.WriteLine("SetPanicJump: 0x{0:X}\nlua_error hook 1: 0x{1:X}\nlongjmp panic addr: 0x{2:X}\npopPanicPtr: 0x{3:X}\nthrowPanicExceptionPtr: 0x{4:X}", mPtr, asmPtr, panicJmpAddr, popPanicPtr, throwPanicExceptionPtr);
#endif

            return asmPtr;
        }

        var throwPanicExceptionPtr = typeof(LayluaNative).GetMethod(nameof(ThrowPanicException), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();
        foreach (var method in typeof(LuaNative).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var canErrorAttribute = method.GetCustomAttribute<ErrorExportAttribute>();
            if (canErrorAttribute == null)
                continue;

            var exportName = canErrorAttribute.ExportName ?? method.Name;
            var fieldName = $"_{exportName}";
            var field = typeof(LuaNative).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException($"No matching panic field '{fieldName}' found.");

            var delegateName = $"{fieldName}Delegate";
            var delegateType = typeof(LuaNative).GetNestedType(delegateName, BindingFlags.NonPublic);
            if (delegateType == null)
                throw new InvalidOperationException($"No matching panic delegate '{delegateName}' found.");

            if (!NativeLibrary.TryGetExport(NativeLibrary.Load(OperatingSystem.IsWindows() ? "lua54" : "liblua54.so"), exportName, out var exportPtr))
            {
                if (method.GetCustomAttribute<OptionalExportAttribute>() == null)
                    throw new InvalidOperationException($"No export '{exportName}' found.");
            }

            if (exportPtr == IntPtr.Zero)
                continue;

            var asmPtr = PrepareHookAsm(exportPtr, throwPanicExceptionPtr);

            // var asmPtr = exportPtr;
            var @delegate = Marshal.GetDelegateForFunctionPointer(asmPtr, delegateType);
#if TRACE_PANIC
            Console.WriteLine($"Error export: {method.Name} ({fieldName} / {delegateName})\n\tNativePtr: 0x{exportPtr:X}\n\tAsmPtr: 0x{asmPtr:X}");
#endif
            field.SetValue(null, @delegate);
        }
    }
}
