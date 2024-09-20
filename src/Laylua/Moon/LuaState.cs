using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents a low-level Lua state.
/// </summary>
/// <remarks>
///     Exercise caution when using the low-level state directly.
///     <para/>
///     This type is not thread-safe; it is not suitable for concurrent use.
/// </remarks>
public sealed unsafe class LuaState : ISpanFormattable
{
    /// <summary>
    ///     Gets the pointer of this state.
    /// </summary>
    public lua_State* L
    {
        get
        {
            ThrowIfDisposed();
            return _L;
        }
    }
    private lua_State* _L;

    /// <summary>
    ///     Gets the allocator of this state.
    /// </summary>
    public LuaAllocator? Allocator
    {
        get
        {
            ThrowIfDisposed();
            return _allocator;
        }
    }
    private readonly LuaAllocator? _allocator;

    /// <summary>
    ///     Gets or sets the hook of this state.
    /// </summary>
    public LuaHook? Hook
    {
        get
        {
            ThrowIfDisposed();
            return _hook;
        }
        set
        {
            ThrowIfDisposed();

            if (_hook == value)
            {
                return;
            }

            if (value == null)
            {
                _innerHookFunction = null;
                _safeHookFunction = null;
                lua_sethook(_L, null, 0, 0);
                return;
            }

            lua_sethook(_L, WrapHookFunction(value), value.EventMask, value.InstructionCount);
            _hook = value;
        }
    }
    private LuaHook? _hook;

    /// <summary>
    ///     Gets an object that controls the Lua garbage collector.
    /// </summary>
    public LuaGC GC
    {
        get
        {
            ThrowIfDisposed();
            return new LuaGC(this);
        }
    }

    internal bool IsDisposed => _L == null;

    internal object? State
    {
        get => _layluaState.State;
        set => _layluaState.State = value;
    }

    private LuaAllocFunction? _safeAllocFunction;
    private LuaHookFunction? _innerHookFunction;
    private LuaHookFunction? _safeHookFunction;

    private LayluaState _layluaState = null!;
    private readonly int _threadReference;

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/> with
    ///     a new default state from the Lua auxiliary library.
    /// </summary>
    internal LuaState()
        : this(null)
    { }

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/>
    ///     with a new state created with the specified allocator.
    /// </summary>
    /// <param name="allocator"> The allocator. </param>
    internal LuaState(LuaAllocator? allocator)
    {
        if (allocator == null)
        {
            _L = luaL_newstate();
        }
        else
        {
            _L = lua_newstate(WrapAllocFunction(allocator), null);
            _allocator = allocator;
        }

        ValidateNewState();
        InitializeState();
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/>
    ///     with the specified pointer to a Lua thread.
    /// </summary>
    /// <param name="L"> The state pointer. </param>
    /// <param name="threadReference"> The Lua reference to the thread. </param>
    internal LuaState(lua_State* L, int threadReference)
    {
        _L = L;
        _threadReference = threadReference;

        InitializeState();
    }

    private void ValidateNewState()
    {
        if (_L == null)
        {
            throw new InvalidOperationException("Failed to create a new Lua state.");
        }

        var version = lua_version(_L).ToString(CultureInfo.InvariantCulture);
        if (version.Length != 3 || version[0] != LuaVersionMajor[0] || version[2] != LuaVersionMinor[0])
        {
            throw new InvalidOperationException($"Invalid Lua version loaded. Expected '{LuaVersion}'.");
        }
    }

    private void InitializeState()
    {
        if (!LayluaState.TryFromExtraSpace(_L, out var state))
        {
            var panicPtr = LayluaNative.InitializePanic();
            state = LayluaState.Create(panicPtr);
            *(IntPtr*) lua_getextraspace(_L) = state.GCPtr;
            lua_atpanic(_L, (delegate* unmanaged[Cdecl]<lua_State*, int>) panicPtr);
        }

        _layluaState = state;
    }

    private void ThrowIfDisposed()
    {
        if (_L == null)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    internal LuaAllocFunction WrapAllocFunction(LuaAllocator allocator)
    {
        Guard.IsNotNull(allocator);

        _safeAllocFunction = (_, ptr, osize, nsize) =>
        {
            return allocator.Allocate(ptr, osize, nsize);
        };

        return _safeAllocFunction;
    }

    internal LuaHookFunction WrapHookFunction(LuaHook hook)
    {
        Guard.IsNotNull(hook);

        _innerHookFunction = hook.Execute;
        _safeHookFunction = Marshal.GetDelegateForFunctionPointer<LuaHookFunction>(LayluaNative.CreateLuaHookFunctionWrapper(_innerHookFunction));

        return _safeHookFunction;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var ptr = (IntPtr) _L;
        return $"Lua: {ptr.ToString(format ?? "X16", formatProvider)}";
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < 6)
        {
            charsWritten = 0;
            return false;
        }

        var ptr = (IntPtr) _L;
        if (!ptr.TryFormat(destination[5..], out charsWritten, format, provider))
        {
            return false;
        }

        "Lua: ".CopyTo(destination);
        charsWritten += 5;
        return true;
    }

    internal void Close()
    {
        if (_L == null)
            return;

        if (_threadReference == 0)
        {
            lua_close(_L);
            _layluaState.Dispose();
        }
        else
        {
            luaL_unref(_L, LuaRegistry.Index, _threadReference);
        }

        _L = null;
    }
}
