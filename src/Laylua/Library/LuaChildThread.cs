using System;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal sealed unsafe class LuaChildThread : LuaThread
{
    /// <inheritdoc />
    public override LuaState State => _state;

    /// <inheritdoc />
    public override LuaStack Stack { get; }

    /// <inheritdoc/>
    public override LuaThread MainThread => Lua.MainThread;

    /// <inheritdoc />
    public override LuaTable Globals => MainThread.Globals;

    /// <inheritdoc />
    internal override LuaMarshaler Marshaler => MainThread.Marshaler;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override Lua? LuaCore
    {
        get => _lua;
        set => throw new InvalidOperationException();
    }

    private Lua? _lua;
    private readonly LuaChildState _state;

    public LuaChildThread()
    {
        Stack = new LuaStack(this);
        _state = new LuaChildState();
    }

    internal void Initialize(Lua lua, lua_State* L, int reference)
    {
        Reference = reference;

        _lua = lua;
        _state.Initialize(lua.State, L);
    }

    public override void Dispose()
    {
        base.Dispose();

        _lua = null;
        _state.Close();
    }
}
