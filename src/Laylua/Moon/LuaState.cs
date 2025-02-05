using System;
using System.Diagnostics;
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
public abstract unsafe class LuaState : ISpanFormattable
{
    /// <summary>
    ///     Gets the pointer of this state.
    /// </summary>
    public lua_State* L
    {
        get
        {
            ThrowIfDisposed();
            return LCore;
        }
    }

    /// <summary>
    ///     Gets the allocator of this state.
    /// </summary>
    public abstract LuaAllocator? Allocator { get; }

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
                lua_sethook(L, null, 0, 0);
                return;
            }

            lua_sethook(L, WrapHookFunction(value), value.EventMask, value.InstructionCount);
            _hook = value;
        }
    }

    /// <summary>
    ///     Gets an object that controls the Lua garbage collector.
    /// </summary>
    public abstract LuaGC GC { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected abstract lua_State* LCore { get; }

    internal bool IsDisposed => LCore == null;

    private LuaHook? _hook;
    private LuaHookFunction? _innerHookFunction;
    private LuaHookFunction? _safeHookFunction;

    internal LuaState()
    { }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(LCore == null, this);
    }

    internal LuaHookFunction WrapHookFunction(LuaHook hook)
    {
        Guard.IsNotNull(hook);

        _innerHookFunction = (state, ar) =>
        {
            try
            {
                using (var thread = LuaThread.FromExtraSpace(state))
                {
                    var debug = new LuaDebug(thread.State.L, ar);
                    hook.Execute(thread, ar->@event, ref debug);
                }
            }
            catch (Exception ex)
            {
                LuaException.RaiseErrorInfo(state, "An exception occurred while executing the hook.", ex);
            }
        };

        _safeHookFunction = Marshal.GetDelegateForFunctionPointer<LuaHookFunction>(LayluaNative.CreateLuaHookFunctionWrapper(_innerHookFunction));

        return _safeHookFunction;
    }

    internal void CopyHook(LuaState mainState)
    {
        _hook = mainState._hook;
        _safeHookFunction = mainState._safeHookFunction;
        _innerHookFunction = mainState._innerHookFunction;
    }

    internal abstract void Close();

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var ptr = (IntPtr) LCore;
        return $"Lua: {ptr.ToString(format ?? "X16", formatProvider)}";
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < 6)
        {
            charsWritten = 0;
            return false;
        }

        var ptr = (IntPtr) LCore;
        if (!ptr.TryFormat(destination[5..], out charsWritten, format, provider))
        {
            return false;
        }

        "Lua: ".CopyTo(destination);
        charsWritten += 5;
        return true;
    }
}
