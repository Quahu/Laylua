using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal sealed class LuaMainThread : LuaThread
{
    public override LuaThread MainThread => this;

    public override LuaTable Globals => _lua.Globals;

    internal override LuaMarshaler Marshaler => _lua.Marshaler;

    internal override bool IsDisposed => _lua.IsDisposed;

    private readonly Lua _lua;

    public LuaMainThread(Lua lua)
    {
        _lua = lua;
        Stack = lua.Stack;
        State = lua.State;
        Reference = LuaRegistry.Indices.MainThread;
    }
}
