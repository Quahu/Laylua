using System;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal sealed unsafe class LuaChildThread : LuaThread
{
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

    internal void Initialize(lua_State* L, int reference)
    {
        Stack = new LuaStack(this);
        State = new LuaState(L);
        Reference = reference;
    }
}
