﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

/// <summary>
///     Represents a high-level Lua state.
/// </summary>
/// <remarks>
///     This type is not thread-safe; operations on it are not thread-safe.
/// </remarks>
public sealed unsafe partial class Lua : LuaThread, ISpanFormattable
{
    /// <inheritdoc/>
    public override LuaState State { get; }

    /// <inheritdoc/>
    public override LuaStack Stack { get; }

    /// <inheritdoc/>
    public override LuaThread MainThread { get; }

    /// <inheritdoc/>
    public override LuaTable Globals { get; }

    internal override LuaMarshaler Marshaler { get; }

    internal override bool IsDisposed => State.IsDisposed;

    private readonly ConcurrentStack<int> _leakedReferences = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override Lua? LuaCore
    {
        get => this;
        set => throw new InvalidOperationException();
    }

    public Lua()
        : this(LuaMarshaler.Default)
    { }

    public Lua(LuaMarshaler marshaler)
        : this(null!, marshaler)
    { }

    public Lua(LuaAllocator allocator)
        : this(allocator, LuaMarshaler.Default)
    { }

    public Lua(LuaAllocator allocator, LuaMarshaler marshaler)
    {
        Stack = new LuaStack(this);
        State = new LuaMainState(allocator)
        {
            State = this
        };

        Reference = LuaRegistry.Indices.MainThread;
        MainThread = new LuaMainThread(this);
        Globals = LuaTable.CreateGlobalsTable(this);
        Marshaler = marshaler;
        _openLibraries = [];

        lua_setwarnf(State.L, _warningHandlerWrapperPtr, State.L);
    }

    ~Lua()
    {
        Dispose();
    }

    static Lua()
    {
        _warningHandlerWrapperPtr = LayluaNative.CreateLuaWarnFunctionWrapper(_warningHandler);
    }

    internal void PushLeakedReference(int reference)
    {
        _leakedReferences.Push(reference);
    }

    internal void UnrefLeakedReferences()
    {
        while (_leakedReferences.TryPop(out var reference))
        {
            luaL_unref(State.L, LuaRegistry.Index, reference);
        }
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return State.ToString(format, formatProvider);
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return (State as ISpanFormattable).TryFormat(destination, out charsWritten, format, provider);
    }

    public override void Dispose()
    {
        if (IsDisposed)
            return;

        _leakedReferences.Clear();
        _openLibraries.Clear();

        try
        {
            Marshaler.OnLuaDisposing(this);
        }
        finally
        {
            State.Close();
        }

        GC.SuppressFinalize(this);
    }

    public static Lua FromThread(LuaThread thread)
    {
        if (thread is Lua lua)
        {
            return lua;
        }

        return FromExtraSpace(thread.State.L);
    }

    public new static Lua FromExtraSpace(lua_State* L)
    {
        var lua = LayluaState.FromExtraSpace(L).State as Lua;
        if (lua == null)
        {
            luaL_error(L, "Laylua is not attached to this Lua state.");
        }

        return lua;
    }
}
