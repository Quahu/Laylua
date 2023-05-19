using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents a low-level Lua state.
/// </summary>
/// <remarks>
///     Note that the low-level state features unsafe components
///     and operations which must be used carefully.
/// </remarks>
public unsafe class LuaState : IDisposable, ISpanFormattable
{
    /// <summary>
    ///     Gets the pointer of this state.
    /// </summary>
    public lua_State* L
    {
        get
        {
            ThrowIfDisposed();
            return _l;
        }
    }
    private lua_State* _l;

    /// <summary>
    ///     Gets the pointer to the extra space of the Lua state.
    /// </summary>
    public void* ExtraSpace
    {
        get
        {
            ThrowIfDisposed();
            return lua_getextraspace(_l);
        }
    }

    /// <summary>
    ///     Gets or sets the allocator of this state.
    /// </summary>
    public LuaAllocator? Allocator => _allocator;

    private readonly LuaAllocator? _allocator;

    /// <summary>
    ///     Gets or sets the hook of this state.
    /// </summary>
    public LuaHook? Hook
    {
        get => _hook;
        set
        {
            if (_hook == value)
                return;

            if (_hook != null)
            {
                _hook.Dispose();
            }

            if (value == null)
            {
                _innerHookFunction = null;
                _safeHookFunction = null;
                lua_sethook(_l, null, 0, 0);
                return;
            }

            lua_sethook(_l, WrapHookFunction(value), value.EventMask, value.InstructionCount);
            _hook = value;
        }
    }
    private LuaHook? _hook;

    /// <summary>
    ///     Fired when Lua panics.
    /// </summary>
    /// <remarks>
    ///     Subscribed event handlers must not throw any exceptions.
    /// </remarks>
    public event EventHandler<LuaPanickedEventArgs>? Panicked;

    /// <summary>
    ///     Gets whether this instance is disposed.
    /// </summary>
    public bool IsDisposed => _l == null;

    internal object? State
    {
        get => _state.State;
        set => _state.State = value;
    }

    private LuaAllocFunction? _safeAllocFunction;
    private LuaHookFunction? _innerHookFunction;
    private LuaHookFunction? _safeHookFunction;

    private LayluaState _state = null!;

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/> with
    ///     a new default state from the Lua auxiliary library.
    /// </summary>
    public LuaState()
    {
        _l = luaL_newstate();

        ValidateCtor();
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/>
    ///     with a new state created with the specified allocator.
    /// </summary>
    /// <param name="allocator"> The allocator. </param>
    public LuaState(LuaAllocator allocator)
    {
        _l = lua_newstate(WrapAllocFunction(allocator), null /*allocator.UserDataPtr*/);
        _allocator = allocator;

        ValidateCtor();
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaState"/>
    ///     with the specified pointer to an existing state.
    /// </summary>
    /// <param name="L"> The state pointer. </param>
    public LuaState(lua_State* L)
    {
        _l = L;

        ValidateCtor();
    }

    ~LuaState()
    {
        Dispose(false);
    }

    private void ValidateCtor()
    {
        if (_l == null)
            throw new InvalidOperationException("Failed to create a new Lua state.");

        var version = lua_version(_l).ToString(CultureInfo.InvariantCulture);
        if (version.Length != 3 || version[0] != LuaVersionMajor[0] || version[2] != LuaVersionMinor[0])
            throw new InvalidOperationException($"Invalid Lua version loaded. Expected '{LuaVersion}'.");

        var panicPtr = LayluaNative.InitializePanic();
        _state = LayluaState.Create(panicPtr);
        *(IntPtr*) lua_getextraspace(_l) = _state.GCPtr;
        lua_atpanic(_l, (delegate* unmanaged[Cdecl]<lua_State*, int>) panicPtr);
    }

    private void ThrowIfDisposed()
    {
        if (_l == null)
            throw new ObjectDisposedException(GetType().FullName);
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

    // If renamed, rename it in LayluaNative.InitializePanic() as well.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void* Panic(lua_State* L)
    {
        var laylua = LayluaState.FromExtraSpace(L);
        string? message = null;
        var hasDupedStackMessage = false;
        if (lua_isstring(L, -1))
        {
            // On Lua compiled with GCC there's some issue here with the error message
            // appearing twice on the stack, so this code checks for it.
            if (lua_gettop(L) > 1 && lua_isstring(L, -2) && lua_rawequal(L, -1, -2))
            {
                hasDupedStackMessage = true;
            }

            message = lua_tostring(L, -1).ToString();
            lua_pop(L, hasDupedStackMessage ? 2 : 1);
        }

        ExceptionDispatchInfo exception;
        var lua = (laylua.State as Lua);
        var panic = laylua.Panic;

        message = message != null
            ? $"Unhandled error: '{message}'."
            : "Unhandled error.";

        if (panic == null)
        {
            exception = ExceptionDispatchInfo.Capture(new LuaPanicException(lua!, message));
        }
        else
        {
            if (panic.Exception == null)
            {
                var ex = new LuaPanicException(lua!, message);
                panic.Exception = ExceptionDispatchInfo.Capture(ex);
                lua?.State.Panicked?.Invoke(lua.State, new(ex));
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

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var ptr = (IntPtr) _l;
        return $"Lua: {ptr.ToString(format, formatProvider)}";
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < 6)
        {
            charsWritten = 0;
            return false;
        }

        var ptr = (IntPtr) _l;
        if (!ptr.TryFormat(destination[5..], out charsWritten, format, provider))
        {
            return false;
        }

        "Lua: ".CopyTo(destination);
        charsWritten += 5;
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_l == null)
            return;

        lua_close(_l);
        _l = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
