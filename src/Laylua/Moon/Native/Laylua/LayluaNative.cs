using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

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

    // Called by PanicAsm on lua_error with no pcall to catch it
    // Wraps the error in LuaPanicException and stashes it in the HookAsm panic frame
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* Panic(lua_State* L)
    {
        var laylua = LayluaState.FromExtraSpace(L);
        var panic = laylua.Panic;
        if (LuaException.TryGetError(L, out var error))
        {
            // On Lua compiled with GCC, there's some issue here with the error message
            // appearing twice on the stack, so this code checks for it.
            var hasDupedStackMessage = lua_gettop(L) > 1 && lua_rawequal(L, -1, -2);
            lua_pop(L, hasDupedStackMessage ? 2 : 1);
        }
        else if (laylua.Panic?.Exception != null)
        {
            // Exception from wrapper catch block - unwrap LuaRaisedException if present
            var ex = laylua.Panic.Exception.SourceException;
            if (ex is LuaRaisedException raised)
            {
                error.Message = raised.LuaMessage;
                error.Exception = raised.LuaInnerException;
            }
            else
            {
                error.Message = ex.Message;
                error.Exception = ex;
            }
        }
        else
        {
            error.Message = "An unhandled error occurred.";
        }

        var exception = ExceptionDispatchInfo.Capture(new LuaPanicException(error.Message, error.Exception));
        if (panic == null)
        {
            Debug.Assert(laylua.IsPCall);
        }
        else
        {
            panic.Exception = exception;
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
            var panicFnPtr = (IntPtr) (delegate* unmanaged[Cdecl]<lua_State*, void*>) &Panic;
            var asmSpan = Alloc(PanicAsmBytes, out var asmPtr);

            var returnAddr = asmPtr + 0x1A;
            MemoryMarshal.Write(asmSpan[3..], in returnAddr);

            MemoryMarshal.Write(asmSpan[18..], in panicFnPtr);

            _panicPtr = (void*) asmPtr;
        }

        return _panicPtr;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr GetPotentialPanicPtr(lua_State* L)
    {
        var state = LayluaState.FromExtraSpace(L);
        var result = state.Panic?.Exception != null
            ? (IntPtr) state.PanicPtr
            : IntPtr.Zero;

        return result;
    }

    internal static IntPtr getPotentialPanicPtr = (IntPtr) (delegate* unmanaged[Cdecl]<lua_State*, IntPtr>) &GetPotentialPanicPtr;
    internal static IntPtr realLuaErrorPtr;

    // Per-state native struct, pointer lives in Lua's extra space
    // Asm reads PendingErrorFlag at [L-8] -> ptr -> offset 8
    // Child threads get the pointer for free (Lua memcpys extra space)
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExtraSpaceData
    {
        public IntPtr GCHandlePtr;
        public int PendingErrorFlag;
    }

    internal static ExtraSpaceData* AllocExtraSpaceData(IntPtr gcHandlePtr)
    {
        var data = (ExtraSpaceData*) NativeMemory.AllocZeroed((nuint) sizeof(ExtraSpaceData));
        data->GCHandlePtr = gcHandlePtr;
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ExtraSpaceData* GetExtraSpaceData(lua_State* L)
    {
        return (ExtraSpaceData*) (*(IntPtr*) lua_getextraspace(L));
    }

    // Sets the flag that tells ManagedPanicAsm to call lua_error after the wrapper returns
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetPendingError(lua_State* L)
    {
        GetExtraSpaceData(L)->PendingErrorFlag = 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* SetPanicJump(lua_State* L, void* retAddress)
    {
        var state = LayluaState.FromExtraSpace(L);
        if (state.IsPCall)
        {
            return null;
        }

        var panic = state.PushPanic();
        var stackState = panic.StackStatePtr;

        *(void**) stackState = retAddress;

        return stackState;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PopPanic(lua_State* L)
    {
        var state = LayluaState.FromExtraSpace(L);
        var panic = state.PopPanic();
        if (panic != null)
        {
            state.ResetPanic(panic);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowPanicException(lua_State* L)
    {
        // .NET 8 crashes if exceptions escape [UnmanagedCallersOnly]
        // Stash it and let the ErrorExport wrapper throw from managed code
        var state = LayluaState.FromExtraSpace(L);
        var panic = state.PopPanic();
        if (panic != null)
        {
            _pendingPanicException = panic.Exception;
            state.ResetPanic(panic);
        }
    }

    [ThreadStatic]
    private static ExceptionDispatchInfo? _pendingPanicException;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfPendingPanic()
    {
        var ex = _pendingPanicException;
        if (ex != null)
        {
            _pendingPanicException = null;
            ex.Throw();
        }
    }

    private static void InitializePanicHooks()
    {
        var libraryHandle = NativeLibrary.Load("lua54", typeof(LayluaNative).Assembly, null);

        // Get the native lua_error address for ManagedPanicAsm to call directly
        if (NativeLibrary.TryGetExport(libraryHandle, "lua_error", out var luaErrorExport))
        {
            realLuaErrorPtr = luaErrorExport;
        }

        if (OperatingSystem.IsWindows() && IntPtr.Size == 8)
        {
            HookLuaLongjmp(libraryHandle);
        }

        var throwPanicExceptionPtr = (IntPtr) (delegate* unmanaged[Cdecl]<lua_State*, void>) &ThrowPanicException;
        InitializeExports(libraryHandle, throwPanicExceptionPtr);
    }

    // Wraps a native export in HookAsm that handles panic flag + longjmp coordination
    internal static IntPtr PrepareHookAsm(IntPtr export, IntPtr throwPanicExceptionPtr)
    {
        delegate* unmanaged[Cdecl]<lua_State*, void*, void*> mPtrDel = &SetPanicJump;
        var mPtr = (IntPtr) mPtrDel;

        delegate* unmanaged[Cdecl]<lua_State*, void> popPanicDel = &PopPanic;
        var popPanicPtr = (IntPtr) popPanicDel;

        var asmSpan = Alloc(HookAsmBytes, out var asmPtr);

        var panicJmpAddr = asmPtr + 202 + 10 + 216 * 2 + 136 + 50 + 19;
        MemoryMarshal.Write(asmSpan.Slice(29 + 216), in panicJmpAddr);
        MemoryMarshal.Write(asmSpan.Slice(29 + 10 + 216), in panicJmpAddr);
        MemoryMarshal.Write(asmSpan.Slice(45 + 10 + 216), in mPtr);
        MemoryMarshal.Write(asmSpan.Slice(533), in export);
        MemoryMarshal.Write(asmSpan.Slice(161 + 10 + 216 * 2 + 136 + 50 + 19), in export);
        MemoryMarshal.Write(asmSpan.Slice(188 + 10 + 216 * 2 + 136 + 50 + 19), in popPanicPtr);
        MemoryMarshal.Write(asmSpan.Slice(275 + 10 + 216 * 2 + 136 * 2 + 50 + 19), in throwPanicExceptionPtr);

        return asmPtr;
    }

    // Hooks longjmp in lua54.dll's IAT with our custom one that skips RtlUnwindEx
    // Win only - linux longjmp doesn't go through RtlUnwindEx
    private static void HookLuaLongjmp(IntPtr libraryHandle)
    {
        var baseAddr = (byte*) libraryHandle;

        // Parse PE headers
        var e_lfanew = *(int*) (baseAddr + 0x3C);
        var optHeader = baseAddr + e_lfanew + 4 + 20;
        var magic = *(ushort*) optHeader;
        if (magic != 0x20B) // PE32+
        {
            return;
        }

        // Import dir RVA (data directory index 1, offset 120 in PE32+ opt header)
        var importDirRva = *(uint*) (optHeader + 120);
        if (importDirRva == 0)
        {
            return;
        }

        // Allocate custom longjmp
        Alloc(CustomLongjmpAsmBytes, out var customLongjmpPtr);

        // Walk IMAGE_IMPORT_DESCRIPTORs (20 bytes each)
        var importDesc = baseAddr + importDirRva;
        while (true)
        {
            var iltRva = *(uint*) importDesc;         // OriginalFirstThunk
            var nameRva = *(uint*) (importDesc + 12); // Name
            var iatRva = *(uint*) (importDesc + 16);  // FirstThunk

            if (iltRva == 0 && nameRva == 0)
            {
                break;
            }

            var dllName = Marshal.PtrToStringAnsi((IntPtr) (baseAddr + nameRva));
            if (string.Equals(dllName, "VCRUNTIME140.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dllName, "VCRUNTIME140D.dll", StringComparison.OrdinalIgnoreCase))
            {
                // Walk ILT to find longjmp
                var ilt = baseAddr + iltRva;
                var iat = baseAddr + iatRva;

                for (var i = 0;; i++)
                {
                    var entry = *(ulong*) (ilt + i * 8);
                    if (entry == 0)
                    {
                        break;
                    }

                    // Ordinal import, skip
                    if ((entry & (1UL << 63)) != 0)
                    {
                        continue;
                    }

                    // IMAGE_IMPORT_BY_NAME: hint(2) + name
                    var hintNameRva = (uint) (entry & 0x7FFFFFFF);
                    var funcName = Marshal.PtrToStringAnsi((IntPtr) (baseAddr + hintNameRva + 2));
                    if (funcName == "longjmp")
                    {
                        var iatEntryPtr = (IntPtr*) (iat + i * 8);

                        // Patch IAT entry
                        if (!VirtualProtect((IntPtr) iatEntryPtr, (nuint) sizeof(IntPtr), MemoryProtection.ReadWrite, out var oldProtect))
                        {
                            throw new InvalidOperationException($"VirtualProtect failed for IAT entry (error {Marshal.GetLastPInvokeError()}).");
                        }

                        *iatEntryPtr = customLongjmpPtr;

                        VirtualProtect((IntPtr) iatEntryPtr, (nuint) sizeof(IntPtr), oldProtect, out _);
                        return;
                    }
                }
            }

            importDesc += 20;
        }
    }
}
