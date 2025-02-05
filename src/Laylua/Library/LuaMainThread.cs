using System;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal sealed class LuaMainThread : LuaThread
{
    /// <inheritdoc />
    public override LuaState State => _lua.State;

    /// <inheritdoc />
    public override LuaStack Stack => _lua.Stack;

    public override LuaThread MainThread => this;

    public override LuaTable Globals => _lua.Globals;

    internal override LuaMarshaler Marshaler => _lua.Marshaler;

    internal override bool IsDisposed => _lua.IsDisposed;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override Lua? LuaCore
    {
        get => _lua;
        set => throw new InvalidOperationException();
    }

    private readonly Lua _lua;

    public LuaMainThread(Lua lua)
    {
        _lua = lua;
        Reference = LuaRegistry.Indices.MainThread;
    }
}
