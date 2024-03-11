using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Qommon;
using Qommon.Disposal;
using Qommon.Pooling;

namespace Laylua.Moon;

internal sealed unsafe class LayluaState : IDisposable
{
    private static readonly ConcurrentDictionary<IntPtr, GCHandle> GCHandlePointers = new();

    public sealed class PanicInfo : IDisposable
    {
        private const int StackStateSize = 16 * 8 + 8 + 16 * 16;

        public PanicInfo? Parent;

        public bool IsRoot => Parent == null;

        public ExceptionDispatchInfo? Exception;

        public readonly void* StackStatePtr = NativeMemory.AllocZeroed(StackStateSize);

        public bool HasStackState => *(void**) StackStatePtr != null;

        public PanicInfo()
        { }

        public void Reset()
        {
            Parent = null;
            Exception = null!;
            new Span<byte>(StackStatePtr, StackStateSize).Clear();
        }

        public void Dispose()
        {
            NativeMemory.Free(StackStatePtr);
        }

        public override string ToString()
        {
            return $"PanicInfo:{{Parent:{{{Parent?.ToString() ?? "null"}}}, Exception:{{{Exception}}}, StackState:0x{(IntPtr) StackStatePtr:X}}}";
        }
    }

    public PanicInfo? Panic;

    public void* PanicPtr;

    public bool IsPCall;

    public IntPtr GCPtr => (IntPtr) _gcHandle;

    public object? State;

    private GCHandle _gcHandle;
    private readonly ObjectPool<PanicInfo> _panicInfoPool;

    private LayluaState(void* panicPtr)
    {
        PanicPtr = panicPtr;
        _gcHandle = GCHandle.Alloc(this);
        _panicInfoPool = ObjectPool.Create(PanicInfoPooledObjectPolicy.Instance);
    }

    public PanicInfo PushPanic()
    {
        var previousPanic = Panic;
        var currentPanic = Panic = _panicInfoPool.Rent();
        currentPanic.Parent = previousPanic;
#if TRACE_PANIC
        Console.WriteLine($"Pushed panic with stack state 0x{(IntPtr) currentPanic.StackStatePtr:X}, parent is {(currentPanic.Parent == null ? "null" : $"0x{(IntPtr) currentPanic.Parent.StackStatePtr:X}")}");
#endif
        return currentPanic;
    }

    public PanicInfo? PopPanic()
    {
        var panic = Panic;
        if (panic != null)
        {
            Panic = panic.Parent;
#if TRACE_PANIC
            Console.WriteLine($"Popped panic with stack state 0x{(IntPtr) panic.StackStatePtr:X}, new panic is {(Panic == null ? "null" : $"0x{(IntPtr) Panic.StackStatePtr:X}")}");
#endif
        }
        else
        {
#if TRACE_PANIC
            Console.WriteLine("There was no panic to pop...");
#endif
        }

        return panic;
    }

    public void ResetPanic(PanicInfo panic)
    {
        _panicInfoPool.Return(panic);
#if TRACE_PANIC
        Console.WriteLine($"Disposed and returned panic with stack state 0x{(IntPtr) panic.StackStatePtr:X}, new panic is {(Panic == null ? "null" : $"0x{(IntPtr) Panic.StackStatePtr:X}")}");
#endif
    }

    public void Dispose()
    {
        if (!_gcHandle.IsAllocated)
            return;

        Debug.Assert(Panic == null);

        PanicPtr = null;
        IsPCall = false;
        GCHandlePointers.TryRemove(GCPtr, out _);
        _gcHandle.Free();
        RuntimeDisposal.Dispose(_panicInfoPool);
    }

    public static LayluaState Create(void* panicPtr)
    {
        var state = new LayluaState(panicPtr);
        GCHandlePointers.TryAdd(state.GCPtr, state._gcHandle);

        return state;
    }

    public static bool TryFromExtraSpace(lua_State* L, [MaybeNullWhen((false))] out LayluaState state)
    {
        if (L != null)
        {
            var gcHandlePtr = *(IntPtr*) lua_getextraspace(L);
            if (gcHandlePtr != IntPtr.Zero)
            {
                if (GCHandlePointers.TryGetValue(gcHandlePtr, out var gcHandle))
                {
                    if (gcHandle.IsAllocated)
                    {
                        state = Unsafe.As<LayluaState>(gcHandle.Target!);
                        return true;
                    }
                }
            }
        }

        state = default;
        return false;
    }

    public static LayluaState FromExtraSpace(lua_State* L)
    {
        if (!TryFromExtraSpace(L, out var state))
        {
            Throw.ArgumentException("Laylua is not attached to this Lua state.", nameof(L));
        }

        return state;
    }

    private sealed class PanicInfoPooledObjectPolicy : DefaultPooledObjectPolicy<PanicInfo>
    {
        public static readonly PanicInfoPooledObjectPolicy Instance = new();

        private PanicInfoPooledObjectPolicy()
        { }

        public override bool OnReturn(PanicInfo obj)
        {
            obj.Reset();
            return true;
        }
    }
}
