using Laylua.Moon;

namespace Laylua;

internal sealed unsafe class LuaChildThread : LuaThread
{
    /// <inheritdoc/>
    public override Lua MainThread => _mainThread;

    private Lua _mainThread = null!;

    internal void Initialize(lua_State* L, int reference)
    {
        Stack = new LuaStack(this);
        State = new LuaState(L);
        Reference = reference;

        _mainThread = Laylua.Lua.FromExtraSpace(L);
    }
}
