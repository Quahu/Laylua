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
    public override LuaThread MainThread => Lua.FromExtraSpace(State.L).MainThread;

    /// <inheritdoc />
    public override LuaTable Globals => MainThread.Globals;

    /// <inheritdoc />
    internal override LuaMarshaler Marshaler => MainThread.Marshaler;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override Lua? LuaCore
    {
        get => Lua.FromExtraSpace(State.L);
        set => throw new InvalidOperationException();
    }

    private readonly LuaChildState _state;

    public LuaChildThread()
    {
        Stack = new LuaStack(this);
        _state = new LuaChildState();
    }

    internal void Initialize(lua_State* L, int reference)
    {
        Reference = reference;

        _state.Initialize(Lua.FromExtraSpace(L).State, L);
    }

    public override void Dispose()
    {
        base.Dispose();

        _state.Close();
    }
}
